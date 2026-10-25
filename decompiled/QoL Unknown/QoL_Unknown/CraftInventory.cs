using System;
using System.Collections.Generic;
using UnityEngine;

namespace QoL_Unknown;

internal sealed class CraftInventory
{
	private readonly List<CraftItem> items = new List<CraftItem>();

	private readonly List<int> all = new List<int>();

	private readonly Dictionary<string, List<int>> byItemId = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);

	private readonly Dictionary<string, List<int>> byQualityId = new Dictionary<string, List<int>>(StringComparer.Ordinal);

	private readonly HashSet<int> claimed = new HashSet<int>();

	private static readonly List<int> NoCandidates = new List<int>();

	public int Revision { get; private set; }

	public static CraftInventory Capture(Body body)
	{
		List<Item> list = ReachableItems.Gather(body);
		CraftInventory craftInventory = new CraftInventory();
		for (int i = 0; i < list.Count; i++)
		{
			Item val = list[i];
			if ((Object)(object)val != (Object)null)
			{
				craftInventory.Add(CraftItem.FromItem(val));
			}
		}
		craftInventory.Revision = ComputeRevision(craftInventory.items);
		return craftInventory;
	}

	public CraftInventory Clone()
	{
		CraftInventory craftInventory = new CraftInventory
		{
			Revision = Revision
		};
		for (int i = 0; i < items.Count; i++)
		{
			craftInventory.Add(items[i].Clone());
		}
		return craftInventory;
	}

	public void AdoptFrom(CraftInventory trial)
	{
		items.Clear();
		all.Clear();
		byItemId.Clear();
		byQualityId.Clear();
		for (int i = 0; i < trial.items.Count; i++)
		{
			Add(trial.items[i]);
		}
	}

	public bool MatchesDirectly(Recipe recipe)
	{
		if (recipe?.items == null)
		{
			return false;
		}
		claimed.Clear();
		for (int i = 0; i < recipe.items.Count; i++)
		{
			RecipeItem val = recipe.items[i];
			int num = Find(val);
			if (num < 0)
			{
				claimed.Clear();
				return false;
			}
			if (val.destroyItem && !val.isLiquid)
			{
				claimed.Add(num);
			}
		}
		claimed.Clear();
		return true;
	}

	public bool TryConsume(RecipeItem ingredient)
	{
		int num = Find(ingredient);
		if (num < 0)
		{
			return false;
		}
		Use(ingredient, num);
		return true;
	}

	public bool TryRepairInto(RecipeItem ingredient, List<CraftCombine> combines)
	{
		if (ingredient == null || ingredient.isLiquid)
		{
			return false;
		}
		int num = FindRepairTarget(ingredient);
		if (num < 0)
		{
			return false;
		}
		CraftItem craftItem = items[num];
		for (int i = 0; i < items.Count; i++)
		{
			if (!(1f - craftItem.Condition > 0f))
			{
				break;
			}
			CraftItem craftItem2 = items[i];
			if (i == num || !CanCombine(craftItem, craftItem2))
			{
				continue;
			}
			float num2 = Mathf.Min(1f - craftItem.Condition, craftItem2.Condition);
			if (!(num2 <= 0f))
			{
				craftItem.Condition += num2;
				craftItem2.Condition -= num2;
				if (combines != null && (Object)(object)craftItem.Actual != (Object)null && (Object)(object)craftItem2.Actual != (Object)null)
				{
					combines.Add(new CraftCombine(craftItem.Actual, craftItem2.Actual));
				}
			}
		}
		return Satisfies(ingredient, num);
	}

	public void Produce(Recipe recipe)
	{
		RecipeResult val = recipe?.result;
		if (val == null)
		{
			return;
		}
		if (val.isLiquid)
		{
			Add(CraftItem.FromLiquidResult(val.id, val.resultCondition));
			return;
		}
		int num = Mathf.Max(1, val.amount);
		for (int i = 0; i < num; i++)
		{
			Add(CraftItem.FromResult(val.id, val.resultCondition));
		}
	}

	private void Add(CraftItem item)
	{
		items.Add(item);
		int item2 = items.Count - 1;
		all.Add(item2);
		if (!string.IsNullOrEmpty(item.Id))
		{
			Bucket(byItemId, item.Id).Add(item2);
		}
		for (int i = 0; i < item.Qualities.Count; i++)
		{
			string id = item.Qualities[i].id;
			if (!string.IsNullOrEmpty(id))
			{
				Bucket(byQualityId, id).Add(item2);
			}
		}
	}

	private static List<int> Bucket(Dictionary<string, List<int>> buckets, string key)
	{
		if (!buckets.TryGetValue(key, out var value))
		{
			value = (buckets[key] = new List<int>());
		}
		return value;
	}

	private int Find(RecipeItem ingredient)
	{
		if (ingredient == null)
		{
			return -1;
		}
		List<int> list = Candidates(ingredient);
		bool flag = PlayerPrefs.GetInt("Setting_BestToolByEfficiency", 1) == 1 && !ingredient.isLiquid && !ingredient.specific && (ingredient.quality?.id == "hammering" || ingredient.quality?.id == "cutting");
		int result = -1;
		float num = float.MinValue;
		float num2 = float.MinValue;
		for (int i = 0; i < list.Count; i++)
		{
			int num3 = list[i];
			CraftItem craftItem = items[num3];
			if (IsUnavailable(num3))
			{
				continue;
			}
			if (ingredient.isLiquid)
			{
				if (MatchesLiquid(ingredient, craftItem))
				{
					return num3;
				}
			}
			else if (craftItem.ContainerItemCount <= 0 && SatisfiesAtLeast(ingredient, craftItem))
			{
				if (ingredient.specific || !flag)
				{
					return num3;
				}
				float num4 = MatchedQualityAmount(ingredient, craftItem);
				if (num4 > num || (Mathf.Approximately(num4, num) && craftItem.Condition > num2))
				{
					result = num3;
					num = num4;
					num2 = craftItem.Condition;
				}
			}
		}
		return result;
	}

	private List<int> Candidates(RecipeItem ingredient)
	{
		if (ingredient.isLiquid)
		{
			return all;
		}
		List<int> value;
		if (ingredient.specific)
		{
			return byItemId.TryGetValue(ingredient.specificId, out value) ? value : NoCandidates;
		}
		if (ingredient.quality == null || string.IsNullOrEmpty(ingredient.quality.id))
		{
			return NoCandidates;
		}
		List<int> value2;
		return byQualityId.TryGetValue(ingredient.quality.id, out value2) ? value2 : NoCandidates;
	}

	private int FindRepairTarget(RecipeItem ingredient)
	{
		List<int> list = Candidates(ingredient);
		int num = -1;
		for (int i = 0; i < list.Count; i++)
		{
			int num2 = list[i];
			CraftItem craftItem = items[num2];
			if (!IsUnavailable(num2) && !((Object)(object)craftItem.Actual == (Object)null) && craftItem.ContainerItemCount <= 0 && !craftItem.IsLiquidContainer && !(craftItem.Condition <= 0f) && !(craftItem.Condition >= ingredient.minimumCondition) && (ingredient.specific ? string.Equals(craftItem.Id, ingredient.specificId, StringComparison.OrdinalIgnoreCase) : (MatchedQuality(ingredient, craftItem) != null)) && (num < 0 || craftItem.Condition > items[num].Condition))
			{
				num = num2;
			}
		}
		return num;
	}

	private bool IsUnavailable(int index)
	{
		CraftItem craftItem = items[index];
		return craftItem.Consumed || craftItem.Favourited || claimed.Contains(index);
	}

	private bool Satisfies(RecipeItem ingredient, int index)
	{
		CraftItem craftItem = items[index];
		if (craftItem.Consumed || craftItem.Favourited)
		{
			return false;
		}
		return ingredient.isLiquid ? MatchesLiquid(ingredient, craftItem) : (craftItem.ContainerItemCount == 0 && SatisfiesAtLeast(ingredient, craftItem));
	}

	private static bool SatisfiesAtLeast(RecipeItem ingredient, CraftItem item)
	{
		if (item.Condition < ingredient.minimumCondition)
		{
			return false;
		}
		return ingredient.specific ? string.Equals(item.Id, ingredient.specificId, StringComparison.OrdinalIgnoreCase) : (MatchedQuality(ingredient, item) != null);
	}

	private static bool MatchesLiquid(RecipeItem ingredient, CraftItem item)
	{
		if (!item.IsLiquidContainer)
		{
			return false;
		}
		if (ingredient.specific)
		{
			return item.AmountOfLiquid(ingredient.specificId) >= ingredient.minimumCondition;
		}
		if (ingredient.quality == null)
		{
			return false;
		}
		for (int i = 0; i < item.Liquids.Count; i++)
		{
			CraftLiquid craftLiquid = item.Liquids[i];
			if (Liquids.Registry.TryGetValue(craftLiquid.Id, out var value) && MatchedQuality(ingredient.quality, value.GetScaledQualities(craftLiquid.Amount)) != null)
			{
				return true;
			}
		}
		return false;
	}

	private static CraftingQuality MatchedQuality(RecipeItem ingredient, CraftItem item)
	{
		return (ingredient?.quality == null) ? null : MatchedQuality(ingredient.quality, item.Qualities);
	}

	private static CraftingQuality MatchedQuality(CraftingQuality target, List<CraftingQuality> qualities)
	{
		for (int i = 0; i < qualities.Count; i++)
		{
			if (qualities[i].id == target.id && qualities[i].amount >= target.amount)
			{
				return qualities[i];
			}
		}
		return null;
	}

	private static float MatchedQualityAmount(RecipeItem ingredient, CraftItem item)
	{
		return MatchedQuality(ingredient, item)?.amount ?? float.MinValue;
	}

	private void Use(RecipeItem ingredient, int index)
	{
		CraftItem craftItem = items[index];
		if (ingredient.isLiquid)
		{
			UseLiquid(ingredient, craftItem);
			return;
		}
		if (ingredient.specific)
		{
			craftItem.Condition -= ingredient.minimumCondition;
		}
		else
		{
			CraftingQuality val = MatchedQuality(ingredient, craftItem);
			if (val != null && val.amount > 0f)
			{
				craftItem.Condition -= ingredient.quality.amount / val.amount;
			}
		}
		if (ingredient.destroyItem)
		{
			craftItem.Consumed = true;
		}
	}

	private static void UseLiquid(RecipeItem ingredient, CraftItem item)
	{
		if (ingredient.specific)
		{
			item.DrainLiquid(ingredient.specificId, ingredient.minimumCondition);
			return;
		}
		for (int i = 0; i < item.Liquids.Count; i++)
		{
			CraftLiquid craftLiquid = item.Liquids[i];
			if (Liquids.Registry.TryGetValue(craftLiquid.Id, out var value))
			{
				CraftingQuality val = MatchedQuality(ingredient.quality, value.GetScaledQualities(craftLiquid.Amount));
				if (val != null && !(val.amount <= 0f))
				{
					item.DrainLiquid(craftLiquid.Id, craftLiquid.Amount * (ingredient.quality.amount / val.amount));
					break;
				}
			}
		}
	}

	private static bool CanCombine(CraftItem target, CraftItem source)
	{
		if ((Object)(object)target.Actual == (Object)null || (Object)(object)source.Actual == (Object)null || target == source)
		{
			return false;
		}
		if (source.Consumed || source.Favourited || source.ContainerItemCount > 0 || source.Condition <= 0f)
		{
			return false;
		}
		if (target.IsLiquidContainer || source.IsLiquidContainer)
		{
			return false;
		}
		if (!string.Equals(target.Id, source.Id, StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		return target.Actual.Stats.combineable && source.Actual.Stats.combineable;
	}

	private static int ComputeRevision(List<CraftItem> items)
	{
		int num = items.Count;
		for (int i = 0; i < items.Count; i++)
		{
			CraftItem craftItem = items[i];
			num = num * 31 + StableHash(craftItem.Id);
			num = num * 31 + craftItem.Condition.GetHashCode();
			num = num * 31 + (craftItem.Consumed ? 7 : 0) + (craftItem.Favourited ? 13 : 0) + craftItem.ContainerItemCount;
			for (int j = 0; j < craftItem.Qualities.Count; j++)
			{
				num = num * 31 + craftItem.Qualities[j].amount.GetHashCode();
			}
			for (int k = 0; k < craftItem.Liquids.Count; k++)
			{
				num = num * 31 + craftItem.Liquids[k].Amount.GetHashCode();
			}
		}
		return (num == 0) ? 1 : num;
	}

	private static int StableHash(string value)
	{
		if (string.IsNullOrEmpty(value))
		{
			return 0;
		}
		int num = 23;
		for (int i = 0; i < value.Length; i++)
		{
			num = num * 31 + char.ToLowerInvariant(value[i]);
		}
		return num;
	}
}
