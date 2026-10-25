using System.Collections.Generic;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace QoL_Unknown;

internal class RecipeFixPatcher
{
	private static ManualLogSource Logger;

	private static readonly HashSet<Item> consumedThisFrame = new HashSet<Item>();

	private static int consumedFrame = -1;

	public static void Initialize(ManualLogSource logger)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Expected O, but got Unknown
		Logger = logger;
		Harmony val = new Harmony("org.bepinex.plugins.qol.recipefix");
		val.PatchAll(typeof(RecipeFixPatcher));
	}

	[HarmonyPatch(typeof(RecipeItem), "UseItem")]
	[HarmonyPostfix]
	private static void UseItem_Postfix(RecipeItem __instance, Item item)
	{
		if (!((Object)(object)item == (Object)null) && __instance.destroyItem && !__instance.isLiquid)
		{
			if (consumedFrame != Time.frameCount)
			{
				consumedThisFrame.Clear();
				consumedFrame = Time.frameCount;
			}
			consumedThisFrame.Add(item);
		}
	}

	[HarmonyPatch(typeof(PlayerCamera), "RefreshRecipeList")]
	[HarmonyPrefix]
	[HarmonyPriority(800)]
	private static void RefreshRecipeList_Prefix()
	{
		ReachableItems.BeginRefresh();
	}

	[HarmonyPatch(typeof(PlayerCamera), "RefreshRecipeList")]
	[HarmonyFinalizer]
	private static void RefreshRecipeList_Finalizer()
	{
		ReachableItems.EndRefresh();
	}

	[HarmonyPatch(typeof(Recipe), "GetItemsForRecipe")]
	[HarmonyPrefix]
	[HarmonyPriority(0)]
	private static bool GetItemsForRecipe_Prefix(Recipe __instance, ref List<Item> __result, bool __runOriginal)
	{
		return MatchFromRefreshPool(__instance, ref __result, __runOriginal, tolerateMissing: false);
	}

	[HarmonyPatch(typeof(Recipe), "GetItemsForRecipeThorough")]
	[HarmonyPrefix]
	[HarmonyPriority(0)]
	private static bool GetItemsForRecipeThorough_Prefix(Recipe __instance, ref List<Item> __result, bool __runOriginal)
	{
		return MatchFromRefreshPool(__instance, ref __result, __runOriginal, tolerateMissing: true);
	}

	private static bool MatchFromRefreshPool(Recipe recipe, ref List<Item> result, bool runOriginal, bool tolerateMissing)
	{
		Body val = PlayerCamera.main?.body;
		if (!runOriginal || !ReachableItems.Refreshing || (Object)(object)val == (Object)null || recipe?.items == null)
		{
			return runOriginal;
		}
		result = ReachableItems.MatchIngredients(recipe, val, tolerateMissing);
		return false;
	}

	[HarmonyPatch(typeof(RecipeItem), "GetMatchingItem")]
	[HarmonyPrefix]
	private static bool GetMatchingItem_Prefix(RecipeItem __instance, List<Item> items, List<Item> exclusions, ref Item __result)
	{
		if (consumedFrame == Time.frameCount && exclusions != null)
		{
			foreach (Item item in consumedThisFrame)
			{
				if (!exclusions.Contains(item))
				{
					exclusions.Add(item);
				}
			}
		}
		bool flag = !__instance.isLiquid && !__instance.specific && ShouldPrioritizeBestToolQuality(__instance);
		if (!flag && (!__instance.isLiquid || __instance.specificId != "biochem" || (MultiplayerCompat.IsActive && MultiplayerCompat.IsClient)))
		{
			return true;
		}
		Item val = null;
		float num = float.MinValue;
		float num2 = float.MinValue;
		WaterContainerItem val2 = default(WaterContainerItem);
		Container val3 = default(Container);
		foreach (Item item2 in items)
		{
			if (item2.favourited || (item2.id == __instance.ignoredId && !__instance.isLiquid) || exclusions.Contains(item2))
			{
				continue;
			}
			if (__instance.isLiquid)
			{
				if (!((Component)item2).TryGetComponent<WaterContainerItem>(ref val2))
				{
					continue;
				}
				if (__instance.specific)
				{
					if (!(val2.AmountOf(__instance.specificId) >= __instance.minimumCondition))
					{
						continue;
					}
					__result = item2;
					return false;
				}
				foreach (LiquidStack item3 in val2.stack)
				{
					if (Liquids.Registry.TryGetValue(item3.liquidId, out var value) && Item.GetQualityThatMeetsCriteria(__instance.quality, value.GetScaledQualities(item3.amount)) != null)
					{
						__result = item2;
						return false;
					}
				}
			}
			else
			{
				if (((Component)item2).TryGetComponent<Container>(ref val3) && val3.itemCount > 0)
				{
					continue;
				}
				if (__instance.specific)
				{
					if (item2.id == __instance.specificId && item2.condition >= __instance.minimumCondition)
					{
						__result = item2;
						return false;
					}
				}
				else if (item2.condition >= __instance.minimumCondition && Item.GetQualityThatMeetsCriteria(__instance.quality, item2.Stats.qualities) != null)
				{
					if (!flag)
					{
						__result = item2;
						return false;
					}
					float matchedQualityAmount = GetMatchedQualityAmount(__instance, item2);
					if (matchedQualityAmount > num || (Mathf.Approximately(matchedQualityAmount, num) && item2.condition > num2))
					{
						val = item2;
						num = matchedQualityAmount;
						num2 = item2.condition;
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
