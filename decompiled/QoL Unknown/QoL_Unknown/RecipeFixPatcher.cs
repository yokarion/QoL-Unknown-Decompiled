using System.Collections.Generic;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace QoL_Unknown;

internal class RecipeFixPatcher
{
	private static ManualLogSource Logger;

	public static void Initialize(ManualLogSource logger)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Expected O, but got Unknown
		Logger = logger;
		Harmony val = new Harmony("org.bepinex.plugins.qol.recipefix");
		val.PatchAll(typeof(RecipeFixPatcher));
	}

	[HarmonyPatch(typeof(RecipeItem), "GetMatchingItem")]
	[HarmonyPrefix]
	private static bool GetMatchingItem_Prefix(RecipeItem __instance, List<Item> items, List<Item> exclusions, ref Item __result)
	{
		if (!__instance.isLiquid || __instance.specificId != "biochem" || (MultiplayerCompat.IsActive && MultiplayerCompat.IsClient))
		{
			return true;
		}
		bool flag = ShouldPrioritizeBestToolQuality(__instance);
		Item val = null;
		float num = float.MinValue;
		float num2 = float.MinValue;
		WaterContainerItem val2 = default(WaterContainerItem);
		Container val3 = default(Container);
		foreach (Item item in items)
		{
			if (item.favourited || (item.id == __instance.ignoredId && !__instance.isLiquid) || exclusions.Contains(item))
			{
				continue;
			}
			if (__instance.isLiquid)
			{
				if (!((Component)item).TryGetComponent<WaterContainerItem>(ref val2))
				{
					continue;
				}
				if (__instance.specific)
				{
					if (!(val2.AmountOf(__instance.specificId) >= __instance.minimumCondition))
					{
						continue;
					}
					__result = item;
					return false;
				}
				foreach (LiquidStack item2 in val2.stack)
				{
					if (Liquids.Registry.TryGetValue(item2.liquidId, out var value) && Item.GetQualityThatMeetsCriteria(__instance.quality, value.GetScaledQualities(item2.amount)) != null)
					{
						__result = item;
						return false;
					}
				}
			}
			else
			{
				if (((Component)item).TryGetComponent<Container>(ref val3) && val3.itemCount > 0)
				{
					continue;
				}
				if (__instance.specific)
				{
					if (item.id == __instance.specificId && item.condition >= __instance.minimumCondition)
					{
						__result = item;
						return false;
					}
				}
				else if (item.condition >= __instance.minimumCondition && Item.GetQualityThatMeetsCriteria(__instance.quality, item.Stats.qualities) != null)
				{
					if (!flag)
					{
						__result = item;
						return false;
					}
					float matchedQualityAmount = GetMatchedQualityAmount(__instance, item);
					if (matchedQualityAmount > num || (Mathf.Approximately(matchedQualityAmount, num) && item.condition > num2))
					{
						val = item;
						num = matchedQualityAmount;
						num2 = item.condition;
					}
				}
			}
		}
		__result = val;
		return false;
	}

	private static bool ShouldPrioritizeBestToolQuality(RecipeItem ingredient)
	{
		if (PlayerPrefs.GetInt("Setting_BestToolByEfficiency", 1) != 1)
		{
			return false;
		}
		string text = ingredient?.quality?.id;
		return text == "hammering" || text == "cutting";
	}

	private static float GetMatchedQualityAmount(RecipeItem ingredient, Item item)
	{
		return ((((item == null) ? null : item.Stats?.qualities) == null) ? null : Item.GetQualityThatMeetsCriteria(ingredient.quality, item.Stats.qualities))?.amount ?? float.MinValue;
	}
}
