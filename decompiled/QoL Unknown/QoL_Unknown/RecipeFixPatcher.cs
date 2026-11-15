using System.Collections.Generic;
using System.Text;
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
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		Logger = logger;
		new Harmony("org.bepinex.plugins.qol.recipefix").PatchAll(typeof(RecipeFixPatcher));
	}

	[HarmonyPatch(typeof(RecipeItem), "UseItem")]
	[HarmonyPostfix]
	private static void UseItem_Postfix(RecipeItem __instance, Item item)
	{
		if ((Object)(object)item == (Object)null || !__instance.destroyItem || __instance.isLiquid)
		{
			return;
		}
		if (consumedFrame != Time.frameCount)
		{
			consumedThisFrame.Clear();
			consumedFrame = Time.frameCount;
		}
		consumedThisFrame.Add(item);
		Container val = default(Container);
		if ((Object)(object)((Component)item).transform.parent != (Object)null && item.TryGetParentContainer(ref val))
		{
			val.UnloadItem(item, (Body)null);
		}
		if (__instance.destroyItem || !(item.condition <= 0f) || !item.Stats.destroyAtZeroCondition || !((Object)(object)((Component)item).transform.parent != (Object)null))
		{
			return;
		}
		Container val2 = default(Container);
		if (item.TryGetParentContainer(ref val2))
		{
			val2.UnloadItem(item, (Body)null);
			return;
		}
		PlayerCamera main = PlayerCamera.main;
		if (main != null)
		{
			main.body.DropItem(item);
		}
	}

	[HarmonyPatch(typeof(Body), "AutoPickUpItem")]
	[HarmonyPostfix]
	private static void AutoPickUpItem_Diagnose(Body __instance, Item item)
	{
		Container val = default(Container);
		if ((Object)(object)item == (Object)null || (Object)(object)__instance == (Object)null || item.Stats == null || item.Stats.wearable || item.Stats.HasTag("noautopickup") || __instance.HoldingItem(item) || item.TryGetParentContainer(ref val))
		{
			return;
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("[CraftPlace] f").Append(Time.frameCount).Append(' ')
			.Append(item.id)
			.Append(" left loose. slots:");
		for (int i = 0; i < __instance.slots.Length; i++)
		{
			InventorySlot val2 = __instance.slots[i];
			stringBuilder.Append(' ').Append(i).Append(':');
			if (!val2.canPickUp)
			{
				stringBuilder.Append("noPickup");
				continue;
			}
			if (((Component)val2).transform.childCount == 0)
			{
				stringBuilder.Append("empty");
				continue;
			}
			Item component = ((Component)((Component)val2).transform.GetChild(0)).GetComponent<Item>();
			stringBuilder.Append(((Object)(object)component != (Object)null) ? (component.id + "@" + component.condition.ToString("0.##")) : "nonItem");
		}
		foreach (Item surfaceInventoryItem in __instance.GetSurfaceInventoryItems())
		{
			if (!((Object)(object)surfaceInventoryItem == (Object)null) && Object.op_Implicit((Object)(object)surfaceInventoryItem.container))
			{
				stringBuilder.Append(" | ").Append(surfaceInventoryItem.id).Append(" w=")
					.Append(surfaceInventoryItem.container.GetHoldingWeight().ToString("0.##"))
					.Append('/')
					.Append(surfaceInventoryItem.container.maxWeight.ToString("0.##"))
					.Append(" canHold=")
					.Append(surfaceInventoryItem.container.CanHoldItem(item));
			}
		}
		Logger.LogInfo((object)stringBuilder.ToString());
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
					if (val2.AmountOf(__instance.specificId) >= __instance.minimumCondition)
					{
						__result = item2;
						return false;
					}
					continue;
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
					if (item2.id == __instance.specificId && item2.condition >= __instance.minimumCondition && (!__instance.noMaxCondition || item2.condition < 0.991f) && (!item2.Stats.destroyAtZeroCondition || item2.condition > 0f))
					{
						__result = item2;
						return false;
					}
				}
				else if (item2.condition >= __instance.minimumCondition && (!__instance.noMaxCondition || item2.condition < 0.991f) && (!item2.Stats.destroyAtZeroCondition || item2.condition > 0f) && Item.GetQualityThatMeetsCriteria(__instance.quality, item2.Stats.qualities) != null)
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
		if (!(text == "hammering"))
		{
			return text == "cutting";
		}
		return true;
	}

	private static float GetMatchedQualityAmount(RecipeItem ingredient, Item item)
	{
		return ((((item == null) ? null : item.Stats?.qualities) == null) ? null : Item.GetQualityThatMeetsCriteria(ingredient.quality, item.Stats.qualities))?.amount ?? float.MinValue;
	}
}
