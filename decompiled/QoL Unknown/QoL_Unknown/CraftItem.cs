using System;
using System.Collections.Generic;
using UnityEngine;

namespace QoL_Unknown;

internal sealed class CraftItem
{
	public Item Actual;

	public string Id;

	public float Condition;

	public bool Consumed;

	public bool Favourited;

	public int ContainerItemCount;

	public bool IsLiquidContainer;

	public readonly List<CraftingQuality> Qualities = new List<CraftingQuality>();

	public readonly List<CraftLiquid> Liquids = new List<CraftLiquid>();

	public static CraftItem FromItem(Item item)
	{
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Expected O, but got Unknown
		CraftItem craftItem = new CraftItem
		{
			Actual = item,
			Id = item.id,
			Condition = item.condition,
			Favourited = item.favourited
		};
		List<CraftingQuality> list = item.Stats?.qualities;
		if (list != null)
		{
			for (int i = 0; i < list.Count; i++)
			{
				craftItem.Qualities.Add(new CraftingQuality(list[i].id, list[i].amount));
			}
		}
		Container val = default(Container);
		if (((Component)item).TryGetComponent<Container>(ref val))
		{
			craftItem.ContainerItemCount = val.itemCount;
		}
		WaterContainerItem val2 = default(WaterContainerItem);
		if (((Component)item).TryGetComponent<WaterContainerItem>(ref val2))
		{
			craftItem.IsLiquidContainer = true;
			List<LiquidStack> stack = val2.stack;
			for (int j = 0; j < stack.Count; j++)
			{
				craftItem.Liquids.Add(new CraftLiquid(stack[j].liquidId, stack[j].amount));
			}
		}
		return craftItem;
	}

	public static CraftItem FromResult(string id, float condition)
	{
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Expected O, but got Unknown
		CraftItem craftItem = new CraftItem
		{
			Id = id,
			Condition = condition
		};
		if (Item.GlobalItems.TryGetValue(id, out var value) && value.qualities != null)
		{
			for (int i = 0; i < value.qualities.Count; i++)
			{
				craftItem.Qualities.Add(new CraftingQuality(value.qualities[i].id, value.qualities[i].amount));
			}
		}
		return craftItem;
	}

	public static CraftItem FromLiquidResult(string id, float amount)
	{
		CraftItem craftItem = new CraftItem
		{
			Id = id,
			Condition = 1f,
			IsLiquidContainer = true
		};
		if (amount > 0f)
		{
			craftItem.Liquids.Add(new CraftLiquid(id, amount));
		}
		return craftItem;
	}

	public CraftItem Clone()
	{
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Expected O, but got Unknown
		CraftItem craftItem = new CraftItem
		{
			Actual = Actual,
			Id = Id,
			Condition = Condition,
			Consumed = Consumed,
			Favourited = Favourited,
			ContainerItemCount = ContainerItemCount,
			IsLiquidContainer = IsLiquidContainer
		};
		for (int i = 0; i < Qualities.Count; i++)
		{
			craftItem.Qualities.Add(new CraftingQuality(Qualities[i].id, Qualities[i].amount));
		}
		for (int j = 0; j < Liquids.Count; j++)
		{
			craftItem.Liquids.Add(new CraftLiquid(Liquids[j].Id, Liquids[j].Amount));
		}
		return craftItem;
	}

	public float AmountOfLiquid(string id)
	{
		for (int i = 0; i < Liquids.Count; i++)
		{
			if (string.Equals(Liquids[i].Id, id, StringComparison.OrdinalIgnoreCase))
			{
				return Liquids[i].Amount;
			}
		}
		return 0f;
	}

	public void DrainLiquid(string id, float amount)
	{
		if (amount <= 0f)
		{
			return;
		}
		for (int num = Liquids.Count - 1; num >= 0; num--)
		{
			if (string.Equals(Liquids[num].Id, id, StringComparison.OrdinalIgnoreCase))
			{
				float num2 = Liquids[num].Amount - Mathf.Min(Liquids[num].Amount, amount);
				if (num2 < 0.5f)
				{
					Liquids.RemoveAt(num);
				}
				else
				{
					Liquids[num] = new CraftLiquid(id, num2);
				}
			}
		}
		if (!IsLiquidContainer || (Object)(object)Actual == (Object)null)
		{
			return;
		}
		ItemInfo stats = Actual.Stats;
		LiquidItemInfo val = (LiquidItemInfo)(object)((stats is LiquidItemInfo) ? stats : null);
		if (val != null && !(val.capacity <= 0f))
		{
			float num3 = 0f;
			for (int i = 0; i < Liquids.Count; i++)
			{
				num3 += Liquids[i].Amount;
			}
			Condition = num3 / val.capacity;
		}
	}
}
