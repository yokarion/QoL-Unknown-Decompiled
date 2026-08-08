using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace QoL_Unknown;

internal class InventorySortPatcher
{
	private static ManualLogSource Logger;

	private const float WeightEpsilon = 0.0001f;

	public static void Initialize(ManualLogSource logger)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Expected O, but got Unknown
		Logger = logger;
		Harmony val = new Harmony("org.bepinex.plugins.qol.inventorysort");
		val.PatchAll(typeof(InventorySortPatcher));
		Logger.LogInfo((object)"InventorySortPatcher initialized");
	}

	[HarmonyPatch(typeof(ConsoleScript), "RegisterAllCommands")]
	[HarmonyPostfix]
	private static void RegisterCommands_Postfix()
	{
	}

	public static void RunSort(bool safeMode)
	{
		//IL_0213: Unknown result type (might be due to invalid IL or missing references)
		Body body = PlayerCamera.main.body;
		if ((Object)(object)body == (Object)null)
		{
			return;
		}
		List<Item> list = body.GetAllItemsThorough().ToList();
		List<Item> list2 = new List<Item>();
		List<Container> list3 = new List<Container>();
		if (KrokoshaMpCompat.IsActive && list.Any((Item val3) => (Object)(object)val3 != (Object)null && !KrokoshaMpCompat.CanMoveItemNow(val3)))
		{
			PlayerCamera.main.PlayUISound((UISoundType)6, 1f);
			return;
		}
		using (KrokoshaMpCompat.BeginInventoryMutation(body))
		{
			HashSet<Item> hashSet = new HashSet<Item>();
			if (safeMode)
			{
				for (int num = 0; num < 3; num++)
				{
					Item item = body.GetItem(num);
					if ((Object)(object)item != (Object)null)
					{
						hashSet.Add(item);
					}
				}
			}
			foreach (Item item2 in list)
			{
				if ((Object)(object)item2 == (Object)null)
				{
					continue;
				}
				Container component = ((Component)item2).GetComponent<Container>();
				if ((Object)(object)component != (Object)null)
				{
					if ((!((Object)(object)((Component)item2).transform.parent != (Object)null) || !((Object)(object)((Component)((Component)item2).transform.parent).GetComponent<Container>() != (Object)null)) && KrokoshaMpCompat.CanMoveItemNow(item2))
					{
						list3.Add(component);
					}
				}
				else
				{
					if (hashSet.Contains(item2) || body.GetAllWearables().Contains(item2) || item2.favourited)
					{
						continue;
					}
					if ((Object)(object)((Component)item2).transform.parent != (Object)null)
					{
						Container component2 = ((Component)((Component)item2).transform.parent).GetComponent<Container>();
						if ((Object)(object)component2 != (Object)null)
						{
							component2.UnloadItem(item2, (Body)null);
						}
					}
					if (body.HoldingItem(item2))
					{
						body.DropItem(item2);
					}
					item2.rb.simulated = false;
					((Component)item2).transform.position = ((Component)body).transform.position;
					list2.Add(item2);
				}
			}
			float num2 = list2.Sum((Item i) => i.totalWeight);
			bool flag = num2 < 6f;
			list2 = ((!flag) ? list2.OrderByDescending((Item i) => i.totalWeight).ToList() : list2.OrderBy((Item i) => i.totalWeight).ToList());
			foreach (Item it in list2.ToList())
			{
				Container component3 = ((Component)it).GetComponent<Container>();
				bool flag2 = (Object)(object)component3 != (Object)null && ((Component)component3).transform.childCount == 0;
				List<Container> list4 = list3.Where((Container c) => c.CanHoldItem(it) && KrokoshaMpCompat.CanMoveItemNow(((Component)c).GetComponent<Item>()) && CanContainerTakeWeight(c, it)).ToList();
				if ((Object)(object)component3 != (Object)null && !flag2)
				{
					list4.Clear();
				}
				if (list4.Count > 0)
				{
					Container val = ((!flag) ? list4.OrderByDescending(IsMaterialPouch).ThenBy((Container c) => c.encumberanceMult).ThenBy((Container c) => c.maxWeightPerItem)
						.First() : list4.OrderByDescending(IsMaterialPouch).ThenByDescending((Container c) => c.maxWeight - c.GetHoldingWeight()).ThenBy((Container c) => c.encumberanceMult)
						.ThenBy((Container c) => c.maxWeightPerItem)
						.First());
					val.LoadItem(it);
					list2.Remove(it);
					if (flag2)
					{
						list3.Remove(component3);
					}
				}
			}
			for (int num3 = 0; num3 < body.slots.Length; num3++)
			{
				if (list2.Count == 0)
				{
					break;
				}
				if (!body.slots[num3].canPickUp || body.HoldingItem(num3))
				{
					continue;
				}
				Item val2 = null;
				foreach (Item item3 in list2)
				{
					if ((!item3.Stats.onlyHoldInHands || body.slots[num3].isHand) && !item3.Stats.wearable)
					{
						val2 = item3;
						break;
					}
				}
				if ((Object)(object)val2 != (Object)null)
				{
					body.PickUpItem(val2, num3, true);
					list2.Remove(val2);
				}
			}
			foreach (Item item4 in list2.ToList())
			{
				if (item4.Stats.wearable && (Object)(object)body.GetWearableBySlotID(item4.Stats.wearSlotId) == (Object)null)
				{
					body.WearWearable(item4);
					list2.Remove(item4);
				}
			}
			foreach (Item item5 in list2)
			{
				item5.rb.simulated = true;
				((Component)item5).transform.parent = null;
			}
			ReorderContainerContentsByCraftingPriority(body);
			foreach (Item item6 in list)
			{
				KrokoshaMpCompat.MarkItemMoved(item6, containerChanged: true);
			}
			PlayerCamera.main.UpdateWearables();
			if (PlayerCamera.main.radialOpen)
			{
				PlayerCamera.main.RepopulateContainer();
			}
		}
	}

	private static bool IsMaterialPouch(Container container)
	{
		Item val = (((Object)(object)container != (Object)null) ? ((Component)container).GetComponent<Item>() : null);
		return (Object)(object)val != (Object)null && string.Equals(val.id, "materialpouch", StringComparison.OrdinalIgnoreCase);
	}

	private static bool CanContainerTakeWeight(Container container, Item item)
	{
		if ((Object)(object)container == (Object)null || (Object)(object)item == (Object)null)
		{
			return false;
		}
		float totalWeight = item.totalWeight;
		if (totalWeight > container.maxWeightPerItem + 0.0001f)
		{
			return false;
		}
		float num = container.GetHoldingWeight() + totalWeight;
		return num <= container.maxWeight + 0.0001f;
	}

	private static void ReorderContainerContentsByCraftingPriority(Body body)
	{
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Expected O, but got Unknown
		Container val = default(Container);
		foreach (Item item in body.GetAllItemsThorough())
		{
			if ((Object)(object)item == (Object)null || !((Component)item).TryGetComponent<Container>(ref val))
			{
				continue;
			}
			List<Item> list = new List<Item>();
			foreach (Transform item2 in ((Component)val).transform)
			{
				Transform val2 = item2;
				Item component = ((Component)val2).GetComponent<Item>();
				if (!((Object)(object)component == (Object)null) && !component.favourited)
				{
					list.Add(component);
				}
			}
			if (list.Count < 2)
			{
				continue;
			}
			List<Item> list2 = (from i in list
				orderby GetCraftingQualityScore(i, "cutting") descending, GetCraftingQualityScore(i, "hammering") descending, i.totalWeight descending
				select i).ToList();
			bool flag = true;
			for (int num = 0; num < list.Count; num++)
			{
				if ((Object)(object)list[num] != (Object)(object)list2[num])
				{
					flag = false;
					break;
				}
			}
			if (flag)
			{
				continue;
			}
			foreach (Item item3 in list)
			{
				val.UnloadItem(item3, (Body)null);
			}
			foreach (Item item4 in list2)
			{
				if (CanContainerTakeWeight(val, item4))
				{
					val.LoadItem(item4);
					continue;
				}
				item4.rb.simulated = true;
				((Component)item4).transform.parent = null;
			}
		}
	}

	private static float GetCraftingQualityScore(Item item, string qualityId)
	{
		if ((Object)(object)item == (Object)null || item.Stats == null || item.Stats.qualities == null || string.IsNullOrEmpty(qualityId))
		{
			return 0f;
		}
		foreach (CraftingQuality quality in item.Stats.qualities)
		{
			if (quality != null)
			{
				string a = TryReadStringMember(quality, "id");
				if (string.Equals(a, qualityId, StringComparison.OrdinalIgnoreCase))
				{
					return (TryReadFloatMember(quality, "amount") ?? TryReadFloatMember(quality, "value") ?? TryReadFloatMember(quality, "num") ?? TryReadFloatMember(quality, "quality")).GetValueOrDefault();
				}
			}
		}
		return 0f;
	}

	private static string TryReadStringMember(object instance, string memberName)
	{
		Type type = instance.GetType();
		FieldInfo fieldInfo = AccessTools.Field(type, memberName);
		if (fieldInfo != null && fieldInfo.FieldType == typeof(string))
		{
			return fieldInfo.GetValue(instance) as string;
		}
		PropertyInfo propertyInfo = AccessTools.Property(type, memberName);
		if (propertyInfo != null && propertyInfo.PropertyType == typeof(string))
		{
			return propertyInfo.GetValue(instance) as string;
		}
		return null;
	}

	private static float? TryReadFloatMember(object instance, string memberName)
	{
		Type type = instance.GetType();
		FieldInfo fieldInfo = AccessTools.Field(type, memberName);
		if (fieldInfo != null)
		{
			return ConvertToFloat(fieldInfo.GetValue(instance));
		}
		PropertyInfo propertyInfo = AccessTools.Property(type, memberName);
		if (propertyInfo != null)
		{
			return ConvertToFloat(propertyInfo.GetValue(instance));
		}
		return null;
	}

	private static float? ConvertToFloat(object value)
	{
		if (value == null)
		{
			return null;
		}
		if (value is float value2)
		{
			return value2;
		}
		if (value is int num)
		{
			return num;
		}
		if (value is double num2)
		{
			return (float)num2;
		}
		return null;
	}
}
