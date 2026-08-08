using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

namespace QoL_Unknown;

internal static class BulkCraftingPatcher
{
	private sealed class ShiftClickHandler : MonoBehaviour, IPointerClickHandler, IEventSystemHandler
	{
		public Action<bool> OnClick;

		public void OnPointerClick(PointerEventData eventData)
		{
			bool obj = Input.GetKey((KeyCode)304) || Input.GetKey((KeyCode)303);
			OnClick?.Invoke(obj);
		}
	}

	private static ManualLogSource Logger;

	private static int selectedAmount = 1;

	private static int selectedAmountRecipeIndex = -1;

	private static bool mpClientCraftInProgress;

	private const float MpClientCraftSyncTimeout = 3f;

	private const float MpClientCraftStepMinDelay = 0.08f;

	private const float MpClientCraftStepMaxDelay = 0.5f;

	private const string SecondaryCraftMarker = "<color=#ffa500><sprite index=17 tint=1></color>";

	private const string InsufficientSecondaryCraftMarker = "<color=#ff4500><sprite index=17 tint=1></color>";

	private static readonly Color ClearerRecipeAvailableColor = new Color(0.5f, 1f, 0.5f);

	private static readonly Color ClearerRecipeUnavailableColor = new Color(0.5f, 0f, 0f);

	private static readonly FieldInfo RecipeObjectsField = AccessTools.Field(typeof(PlayerCamera), "recipeObjects");

	private static readonly FieldInfo RecipeItemFilterField = AccessTools.Field(typeof(PlayerCamera), "recipeItemFilter");

	private static readonly FieldInfo SelectedRecipeCategoryField = AccessTools.Field(typeof(PlayerCamera), "selectedRecipeCategory");

	private static bool ShowIntermediateStatus => false;

	public static void Initialize(ManualLogSource logger)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Expected O, but got Unknown
		Logger = logger;
		Harmony val = new Harmony("org.bepinex.plugins.qol.bulkcrafting");
		val.PatchAll(typeof(BulkCraftingPatcher));
		Logger.LogInfo((object)"BulkCraftingPatcher initialized");
	}

	[HarmonyPatch(typeof(PlayerCamera), "RefreshRecipeList")]
	[HarmonyPrefix]
	private static bool RefreshRecipeList_Prefix(PlayerCamera __instance)
	{
		if ((Object)(object)__instance == (Object)null || (Object)(object)__instance.recipeListContent == (Object)null)
		{
			return true;
		}
		try
		{
			RefreshRecipeListWithSecondaryCrafts(__instance);
			return false;
		}
		catch (Exception ex)
		{
			ManualLogSource logger = Logger;
			if (logger != null)
			{
				logger.LogWarning((object)("[BulkCrafting] Falling back to vanilla recipe list: " + ex.Message));
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(PlayerCamera), "IngredientTextForRecipe")]
	[HarmonyPrefix]
	private static bool IngredientTextForRecipe_Prefix(Recipe recipe, List<Item> its, ref string __result)
	{
		if (recipe == null)
		{
			return true;
		}
		__result = BuildIngredientTextWithSecondaryCrafts(recipe, its);
		return false;
	}

	[HarmonyPatch(typeof(PlayerCamera), "TryCraft")]
	[HarmonyPrefix]
	[HarmonyBefore(new string[] { "KrokoshaCasualtiesMP" })]
	[HarmonyPriority(800)]
	private static bool TryCraft_Prefix(PlayerCamera __instance)
	{
		if ((Object)(object)__instance == (Object)null || __instance.selectedRecipe < 0 || __instance.selectedRecipe >= Recipes.recipes.Count)
		{
			return true;
		}
		Recipe val = Recipes.recipes[__instance.selectedRecipe];
		EnsureAmountBelongsToRecipe(__instance.selectedRecipe);
		CraftingPlan craftingPlan = CraftingPlanService.BuildExecutionPlan(val, selectedAmount);
		selectedAmount = Mathf.Max(1, craftingPlan.RequestedAmount);
		if (!craftingPlan.CanCraft)
		{
			__instance.PlayUISound((UISoundType)6, 1f);
			__instance.RefreshRecipeList();
			return false;
		}
		if (KrokoshaMpCompat.IsActive)
		{
			if (KrokoshaMpCompat.IsClient)
			{
				if (mpClientCraftInProgress)
				{
					__instance.PlayUISound((UISoundType)6, 1f);
					return false;
				}
				using (KrokoshaMpCompat.SuppressKrokoshaTryCraftPrefix())
				{
					((MonoBehaviour)__instance).StartCoroutine(BeginMpClientCraftSequenceNextFrame(__instance, val.index, selectedAmount));
				}
				return false;
			}
			bool flag;
			using (KrokoshaMpCompat.SuppressKrokoshaTryCraftPrefix())
			{
				flag = KrokoshaMpCompat.TryExecuteBulkCraftViaKrokosha(__instance, val, craftingPlan);
			}
			if (!flag)
			{
				__instance.PlayUISound((UISoundType)6, 1f);
				__instance.RefreshRecipeList();
				return false;
			}
			RefreshRecipeListAfterInventorySettles(__instance);
			return false;
		}
		if (!ExecuteSingleplayerPlan(val, craftingPlan))
		{
			__instance.PlayUISound((UISoundType)6, 1f);
			__instance.RefreshRecipeList();
			return false;
		}
		RefreshRecipeListAfterInventorySettles(__instance);
		return false;
	}

	private static IEnumerator ExecuteMpClientCraftSequence(PlayerCamera camera, int finalRecipeIndex, int amount)
	{
		mpClientCraftInProgress = true;
		ManualLogSource logger = Logger;
		if (logger != null)
		{
			logger.LogInfo((object)$"[BulkCrafting][MP Client] Starting queued craft for recipeIndex={finalRecipeIndex}, amount={amount}.");
		}
		try
		{
			int remaining = Mathf.Max(1, amount);
			string previousPlanSignature = null;
			Recipe previousStepRecipe = null;
			int previousStepPlannedCount = 0;
			bool previousStepRequiresPlanAdvance = false;
			while (remaining > 0 && KrokoshaMpCompat.IsActive && KrokoshaMpCompat.IsClient)
			{
				if ((Object)(object)camera == (Object)null || (Object)(object)camera.body == (Object)null || finalRecipeIndex < 0 || finalRecipeIndex >= Recipes.recipes.Count)
				{
					ManualLogSource logger2 = Logger;
					if (logger2 != null)
					{
						logger2.LogWarning((object)"[BulkCrafting][MP Client] Aborting craft queue because camera/body/recipe index became invalid.");
					}
					break;
				}
				Recipe finalRecipe = Recipes.recipes[finalRecipeIndex];
				CraftingPlan plan = CraftingPlanService.BuildExecutionPlan(finalRecipe, 1, camera.body);
				if (previousStepRequiresPlanAdvance && previousPlanSignature != null && !HasClientPlanAdvanced(plan, previousPlanSignature, previousStepRecipe, previousStepPlannedCount))
				{
					ManualLogSource logger3 = Logger;
					if (logger3 != null)
					{
						logger3.LogInfo((object)"[BulkCrafting][MP Client] Waiting for client craft state to advance before selecting the next step.");
					}
					yield return WaitForMpCraftReadiness(() => HasClientPlanAdvanced(CraftingPlanService.BuildExecutionPlan(finalRecipe, 1, camera.body), previousPlanSignature, previousStepRecipe, previousStepPlannedCount));
					plan = CraftingPlanService.BuildExecutionPlan(finalRecipe, 1, camera.body);
					if (!HasClientPlanAdvanced(plan, previousPlanSignature, previousStepRecipe, previousStepPlannedCount))
					{
						ManualLogSource logger4 = Logger;
						if (logger4 != null)
						{
							logger4.LogWarning((object)"[BulkCrafting][MP Client] Craft state did not advance after the previous step.");
						}
						camera.PlayUISound((UISoundType)6, 1f);
						break;
					}
				}
				previousPlanSignature = null;
				previousStepRecipe = null;
				previousStepPlannedCount = 0;
				ManualLogSource logger5 = Logger;
				if (logger5 != null)
				{
					logger5.LogInfo((object)$"[BulkCrafting][MP Client] Planned final recipe '{finalRecipe.simpleName}' CanCraft={plan.CanCraft} CombineOps={plan.CombineOperations.Count} Intermediates={plan.IntermediateOperations.Count} Remaining={remaining}.");
				}
				if (!plan.CanCraft)
				{
					ManualLogSource logger6 = Logger;
					if (logger6 != null)
					{
						logger6.LogInfo((object)("[BulkCrafting][MP Client] Waiting for final recipe '" + finalRecipe.simpleName + "' to become craftable."));
					}
					yield return WaitForMpCraftReadiness(() => CraftingPlanService.BuildExecutionPlan(finalRecipe, 1, camera.body).CanCraft);
					plan = CraftingPlanService.BuildExecutionPlan(finalRecipe, 1, camera.body);
					if (!plan.CanCraft)
					{
						ManualLogSource logger7 = Logger;
						if (logger7 != null)
						{
							logger7.LogWarning((object)("[BulkCrafting][MP Client] Final recipe '" + finalRecipe.simpleName + "' never became craftable."));
						}
						camera.PlayUISound((UISoundType)6, 1f);
						break;
					}
					ManualLogSource logger8 = Logger;
					if (logger8 != null)
					{
						logger8.LogInfo((object)("[BulkCrafting][MP Client] Final recipe '" + finalRecipe.simpleName + "' became craftable after waiting."));
					}
				}
				CraftingCombineOperation combine = plan.CombineOperations.FirstOrDefault();
				if (combine != null)
				{
					ManualLogSource logger9 = Logger;
					if (logger9 != null)
					{
						logger9.LogInfo((object)("[BulkCrafting][MP Client] Attempting combine step target='" + (combine.Target?.id ?? "null") + "' source='" + (combine.Source?.id ?? "null") + "'."));
					}
					if (!KrokoshaMpCompat.AreItemsSynced(combine.Target, combine.Source))
					{
						ManualLogSource logger10 = Logger;
						if (logger10 != null)
						{
							logger10.LogInfo((object)"[BulkCrafting][MP Client] Waiting for combine items to become synced.");
						}
						yield return WaitForMpCraftReadiness(() => KrokoshaMpCompat.AreItemsSynced(combine.Target, combine.Source));
					}
					if ((Object)(object)combine.Target == (Object)null || (Object)(object)combine.Source == (Object)null || !camera.body.CanCombine(combine.Target, combine.Source) || !KrokoshaMpCompat.AreItemsSynced(combine.Target, combine.Source))
					{
						ManualLogSource logger11 = Logger;
						if (logger11 != null)
						{
							logger11.LogWarning((object)string.Format("[BulkCrafting][MP Client] Combine step denied target='{0}' source='{1}' synced={2} canCombine={3}.", combine.Target?.id ?? "null", combine.Source?.id ?? "null", KrokoshaMpCompat.AreItemsSynced(combine.Target, combine.Source), (Object)(object)combine.Target != (Object)null && (Object)(object)combine.Source != (Object)null && camera.body.CanCombine(combine.Target, combine.Source)));
						}
						camera.PlayUISound((UISoundType)6, 1f);
						break;
					}
					camera.body.CombineItems(combine.Target, combine.Source);
					ManualLogSource logger12 = Logger;
					if (logger12 != null)
					{
						logger12.LogInfo((object)"[BulkCrafting][MP Client] Combine step sent successfully.");
					}
					previousPlanSignature = BuildPlanSignature(plan);
					previousStepRequiresPlanAdvance = true;
					yield return WaitAfterMpClientCraftStep(camera, finalRecipeIndex);
					continue;
				}
				Recipe nextRecipe = NextRecipeForClientStep(plan, finalRecipe, camera.body);
				if (nextRecipe == null)
				{
					ManualLogSource logger13 = Logger;
					if (logger13 != null)
					{
						logger13.LogWarning((object)("[BulkCrafting][MP Client] No directly craftable next recipe found for final '" + finalRecipe.simpleName + "'."));
					}
					camera.PlayUISound((UISoundType)6, 1f);
					break;
				}
				ManualLogSource logger14 = Logger;
				if (logger14 != null)
				{
					logger14.LogInfo((object)$"[BulkCrafting][MP Client] Chose next recipe '{nextRecipe.simpleName}' plannerCraftable={CraftingPlanService.CanCraftRecipe(nextRecipe, camera.body)} directKrokoshaCraftable={KrokoshaMpCompat.CanDirectCraftViaKrokosha(camera.body, nextRecipe)}.");
				}
				bool executed = KrokoshaMpCompat.TryExecuteSingleCraftViaKrokosha(camera, nextRecipe);
				ManualLogSource logger15 = Logger;
				if (logger15 != null)
				{
					logger15.LogInfo((object)$"[BulkCrafting][MP Client] Execute step '{nextRecipe.simpleName}' executed={executed}.");
				}
				if (!executed)
				{
					ManualLogSource logger16 = Logger;
					if (logger16 != null)
					{
						logger16.LogWarning((object)("[BulkCrafting][MP Client] Craft step denied for '" + nextRecipe.simpleName + "'."));
					}
					camera.PlayUISound((UISoundType)6, 1f);
					break;
				}
				previousPlanSignature = BuildPlanSignature(plan);
				previousStepRecipe = nextRecipe;
				previousStepPlannedCount = GetPlannedCount(plan, nextRecipe);
				previousStepRequiresPlanAdvance = nextRecipe != finalRecipe || plan.HasPreparations;
				if (nextRecipe == finalRecipe)
				{
					remaining--;
					ManualLogSource logger17 = Logger;
					if (logger17 != null)
					{
						logger17.LogInfo((object)$"[BulkCrafting][MP Client] Final recipe step completed. Remaining={remaining}.");
					}
				}
				yield return WaitAfterMpClientCraftStep(camera, finalRecipeIndex);
			}
		}
		finally
		{
			mpClientCraftInProgress = false;
			ManualLogSource logger18 = Logger;
			if (logger18 != null)
			{
				logger18.LogInfo((object)"[BulkCrafting][MP Client] Craft queue finished.");
			}
			RefreshRecipeListAfterInventorySettles(camera);
			RestoreFinalRecipeSelection(camera, finalRecipeIndex);
		}
	}

	private static IEnumerator BeginMpClientCraftSequenceNextFrame(PlayerCamera camera, int finalRecipeIndex, int amount)
	{
		yield return null;
		if ((Object)(object)camera == (Object)null)
		{
			mpClientCraftInProgress = false;
		}
		else
		{
			yield return ExecuteMpClientCraftSequence(camera, finalRecipeIndex, amount);
		}
	}

	private static Recipe NextRecipeForClientStep(CraftingPlan plan, Recipe finalRecipe, Body crafter)
	{
		if (plan == null)
		{
			return null;
		}
		if (KrokoshaMpCompat.CanDirectCraftViaKrokosha(crafter, finalRecipe))
		{
			return finalRecipe;
		}
		foreach (CraftingPlanOperation intermediateOperation in plan.IntermediateOperations)
		{
			if (intermediateOperation != null && intermediateOperation.Count > 0 && intermediateOperation.Recipe != null && KrokoshaMpCompat.CanDirectCraftViaKrokosha(crafter, intermediateOperation.Recipe))
			{
				return intermediateOperation.Recipe;
			}
		}
		return null;
	}

	private static IEnumerator WaitForMpCraftReadiness(Func<bool> ready)
	{
		float start = Time.realtimeSinceStartup;
		while (KrokoshaMpCompat.IsActive && KrokoshaMpCompat.IsClient && ready != null && !ready() && Time.realtimeSinceStartup - start < 3f)
		{
			yield return null;
		}
	}

	private static IEnumerator WaitAfterMpClientCraftStep(PlayerCamera camera, int finalRecipeIndex)
	{
		RefreshRecipeListAfterInventorySettles(camera);
		RestoreFinalRecipeSelection(camera, finalRecipeIndex);
		float start = Time.realtimeSinceStartup;
		while (Time.realtimeSinceStartup - start < 0.08f)
		{
			yield return null;
		}
		RestoreFinalRecipeSelection(camera, finalRecipeIndex);
	}

	private static bool HasClientPlanAdvanced(CraftingPlan currentPlan, string previousPlanSignature, Recipe previousStepRecipe, int previousStepPlannedCount)
	{
		if (currentPlan == null)
		{
			return false;
		}
		if (!string.Equals(BuildPlanSignature(currentPlan), previousPlanSignature, StringComparison.Ordinal))
		{
			return true;
		}
		if (previousStepRecipe == null)
		{
			return false;
		}
		return GetPlannedCount(currentPlan, previousStepRecipe) < previousStepPlannedCount;
	}

	private static int GetPlannedCount(CraftingPlan plan, Recipe recipe)
	{
		if (plan == null || recipe == null)
		{
			return 0;
		}
		int num = 0;
		foreach (CraftingPlanOperation intermediateOperation in plan.IntermediateOperations)
		{
			if (intermediateOperation?.Recipe == recipe)
			{
				num += intermediateOperation.Count;
			}
		}
		if (recipe == Recipes.recipes.ElementAtOrDefault(recipe.index))
		{
			num += plan.RequestedAmount;
		}
		return num;
	}

	private static string BuildPlanSignature(CraftingPlan plan)
	{
		if (plan == null)
		{
			return "<null>";
		}
		IEnumerable<string> values = plan.CombineOperations.Select((CraftingCombineOperation operation) => (operation?.Target?.id ?? "null") + "->" + (operation?.Source?.id ?? "null"));
		IEnumerable<string> values2 = plan.IntermediateOperations.Select((CraftingPlanOperation operation) => $"{operation?.Recipe?.index ?? (-1)}:{operation?.Count ?? 0}");
		return string.Join("|", plan.CanCraft ? "1" : "0", plan.RequestedAmount.ToString(), plan.MaxCraftable.ToString(), string.Join(",", values), string.Join(",", values2));
	}

	private static void RestoreFinalRecipeSelection(PlayerCamera camera, int finalRecipeIndex)
	{
		if (!((Object)(object)camera == (Object)null) && finalRecipeIndex >= 0 && Recipes.recipes != null && finalRecipeIndex < Recipes.recipes.Count && camera.selectedRecipe != finalRecipeIndex)
		{
			camera.SelectRecipe(finalRecipeIndex);
		}
	}

	[HarmonyPatch(typeof(PlayerCamera), "RefreshCurrentlySelectedRecipe")]
	[HarmonyPostfix]
	private static void RefreshCurrentlySelectedRecipe_Postfix(PlayerCamera __instance)
	{
		try
		{
			RefreshBulkCraftingUi(__instance);
		}
		catch (Exception ex)
		{
			ManualLogSource logger = Logger;
			if (logger != null)
			{
				logger.LogWarning((object)("[BulkCrafting] Failed to refresh UI: " + ex.Message));
			}
		}
	}

	private static void RefreshRecipeListWithSecondaryCrafts(PlayerCamera camera)
	{
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_010a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		//IL_010f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0297: Unknown result type (might be due to invalid IL or missing references)
		//IL_044b: Unknown result type (might be due to invalid IL or missing references)
		//IL_02db: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_032c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0346: Unknown result type (might be due to invalid IL or missing references)
		//IL_034b: Unknown result type (might be due to invalid IL or missing references)
		//IL_036d: Unknown result type (might be due to invalid IL or missing references)
		//IL_038d: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_03db: Expected O, but got Unknown
		if (RecipeObjectsField?.GetValue(camera) is List<GameObject> list)
		{
			foreach (GameObject item5 in list)
			{
				Object.Destroy((Object)(object)item5);
			}
		}
		List<GameObject> list2 = new List<GameObject>();
		RecipeObjectsField?.SetValue(camera, list2);
		object? obj = RecipeItemFilterField?.GetValue(camera);
		Item val = (Item)((obj is Item) ? obj : null);
		RecipeCategory? selectedCategory = SelectedRecipeCategoryField?.GetValue(camera) as RecipeCategory?;
		CraftingPlanService.CraftingInventoryContext context = CraftingPlanService.CaptureContext(camera.body);
		Color val2 = (PlayerCamera.clearerRecipes ? ClearerRecipeAvailableColor : Color.white);
		Color val3 = (PlayerCamera.clearerRecipes ? ClearerRecipeUnavailableColor : Color.gray);
		List<(Recipe, bool)> source = (from rec in Recipes.GetVisibleRecipes(val)
			orderby rec.INT
			select (rec: rec, CraftingPlanService.CanCraftRecipe(rec, context))).ToList();
		List<(Recipe, bool)> list3 = source.Where<(Recipe, bool)>(((Recipe rec, bool craftable) x) => x.craftable).ToList();
		list3.AddRange(source.Where<(Recipe, bool)>(((Recipe rec, bool craftable) x) => !x.craftable));
		if (selectedCategory.HasValue && string.IsNullOrEmpty(camera.recipeFilter) && (Object)(object)val == (Object)null)
		{
			list3 = list3.Where<(Recipe, bool)>(((Recipe rec, bool craftable) r) => r.rec.category == selectedCategory.Value).ToList();
		}
		if (!string.IsNullOrEmpty(camera.recipeFilter) && (Object)(object)val == (Object)null)
		{
			list3 = list3.Where<(Recipe, bool)>(((Recipe rec, bool craftable) r) => r.rec.simpleName.IndexOf(camera.recipeFilter, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
		}
		for (int num = 0; num < list3.Count; num++)
		{
			(Recipe, bool) tuple = list3[num];
			Recipe item = tuple.Item1;
			bool item2 = tuple.Item2;
			GameObject val4 = Utils.Create("Special/RecipeBox", (Transform)(object)camera.recipeListContent);
			val4.GetComponent<RectTransform>().anchoredPosition = new Vector2(-9f, (float)(-num * 64));
			((TMP_Text)((Component)val4.transform.GetChild(0)).GetComponent<TextMeshProUGUI>()).text = item.fullName;
			((Graphic)((Component)val4.transform.GetChild(0)).GetComponent<TextMeshProUGUI>()).color = (item2 ? val2 : val3);
			((TMP_Text)((Component)val4.transform.GetChild(1)).GetComponent<TextMeshProUGUI>()).text = item.INT.ToString();
			((Graphic)((Component)val4.transform.GetChild(1)).GetComponent<TextMeshProUGUI>()).color = camera.RecipeINTToColor(item.INT);
			ValueTuple<Sprite, Color> resultSprite = item.resultSprite;
			Sprite item3 = resultSprite.Item1;
			Color item4 = resultSprite.Item2;
			Image component = ((Component)val4.transform.GetChild(2)).GetComponent<Image>();
			component.sprite = item3;
			((Graphic)component).color = item4;
			((Component)component).GetComponent<RectTransform>().sizeDelta = PlayerCamera.ImageSizeDelta((Texture)(object)item3.texture, 3f, 42f);
			((Graphic)val4.GetComponent<Image>()).color = (item2 ? val2 : val3);
			int toIndex = item.index;
			((UnityEvent)val4.GetComponent<Button>().onClick).AddListener((UnityAction)delegate
			{
				camera.SelectRecipe(toIndex);
			});
			UITooltip val5 = val4.AddComponent<UITooltip>();
			val5.skipLocale = true;
			val5.tipName = item.simpleName;
			val5.tipDesc = item.description;
			list2.Add(val4);
		}
		camera.recipeListContent.sizeDelta = new Vector2(1f, (float)list3.Count * 64f);
		RecipeItemFilterField?.SetValue(camera, null);
		camera.RefreshCurrentlySelectedRecipe();
	}

	private static string BuildIngredientTextWithSecondaryCrafts(Recipe recipe, List<Item> its)
	{
		string text = "";
		CraftingPlanService.CraftingInventoryContext context = CraftingPlanService.CreateMutableContext(PlayerCamera.main?.body);
		HashSet<string> satisfiedAutoCraftKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		if (its == null)
		{
			its = recipe.GetItemsForRecipeThorough();
		}
		for (int i = 0; i < recipe.items.Count; i++)
		{
			RecipeItem recit = recipe.items[i];
			text += "<color=#FFFFFF>";
			text += IngredientAvailabilityMarker(recit, CraftingPlanService.TryConsumeIngredientForDisplay(recit, context), satisfiedAutoCraftKeys);
			text = ((!recit.specific) ? ((!recit.isLiquid) ? (text + Locale.GetOther((recit.quality.id == "hammering" || recit.quality.id == "cutting") ? "craftanytool" : "craftanyitem") + "\n") : (text + Locale.GetOther("craftanyliquid") + "\n")) : ((!recit.isLiquid) ? (text + Locale.GetItem(recit.specificId) + "\n") : (text + Locale.GetOther(recit.specificId) + "\n")));
			text += "<color=#666666>";
			if (recit.isLiquid)
			{
				if (!recit.specific)
				{
					text = text + Locale.GetOther("craftliquidquality").Replace("<1>", recit.quality.amount.ToString("0.#")).Replace("<2>", recit.quality.LocaleName) + "\n";
				}
				else if (recit.minimumCondition > 0f)
				{
					text = text + Locale.GetOther("craftml").Replace("<>", recit.minimumCondition.ToString("0.#")) + "\n";
				}
				continue;
			}
			if (!recit.specific)
			{
				text += Locale.GetOther("craftitemquality").Replace("<1>", recit.quality.amount.ToString("0.#")).Replace("<2>", recit.quality.LocaleName);
				KeyValuePair<CraftingQuality, string> keyValuePair = Recipes.QualityExamples.FirstOrDefault((KeyValuePair<CraftingQuality, string> x) => x.Key.id == recit.quality.id && x.Key.amount == recit.quality.amount);
				if (keyValuePair.Value != null)
				{
					text += Locale.GetOther("craftexample").Replace("<>", Locale.GetItem(keyValuePair.Value));
				}
				text += "\n";
			}
			if (recit.minimumCondition > 0f)
			{
				text = text + Locale.GetOther("craftcondition").Replace("<>", (recit.minimumCondition * 100f).ToString("0.#")) + "\n";
			}
		}
		return text;
	}

	private static string IngredientAvailabilityMarker(RecipeItem ingredient, CraftingIngredientAvailability availability, HashSet<string> satisfiedAutoCraftKeys)
	{
		switch (availability)
		{
		case CraftingIngredientAvailability.Available:
			return "<sprite index=23>";
		case CraftingIngredientAvailability.AutoCraftable:
			TrackSatisfiedAutoCraft(ingredient, satisfiedAutoCraftKeys);
			return "<color=#ffa500><sprite index=17 tint=1></color>";
		case CraftingIngredientAvailability.AutoCraftableButMissingInputs:
			return HasSatisfiedAutoCraft(ingredient, satisfiedAutoCraftKeys) ? "<color=#ff4500><sprite index=17 tint=1></color>" : "<sprite index=24>";
		default:
			return "<sprite index=24>";
		}
	}

	private static void TrackSatisfiedAutoCraft(RecipeItem ingredient, HashSet<string> satisfiedAutoCraftKeys)
	{
		string autoCraftKey = CraftingPlanService.GetAutoCraftKey(ingredient);
		if (!string.IsNullOrEmpty(autoCraftKey))
		{
			satisfiedAutoCraftKeys.Add(autoCraftKey);
		}
	}

	private static bool HasSatisfiedAutoCraft(RecipeItem ingredient, HashSet<string> satisfiedAutoCraftKeys)
	{
		string autoCraftKey = CraftingPlanService.GetAutoCraftKey(ingredient);
		return !string.IsNullOrEmpty(autoCraftKey) && satisfiedAutoCraftKeys.Contains(autoCraftKey);
	}

	private static bool ExecuteSingleplayerPlan(Recipe finalRecipe, CraftingPlan plan)
	{
		if (!ExecuteCombineOperations(PlayerCamera.main?.body, plan))
		{
			return false;
		}
		foreach (CraftingPlanOperation intermediateOperation in plan.IntermediateOperations)
		{
			for (int i = 0; i < intermediateOperation.Count; i++)
			{
				intermediateOperation.Recipe.TryMake();
			}
		}
		for (int j = 0; j < plan.RequestedAmount; j++)
		{
			finalRecipe.TryMake();
		}
		return true;
	}

	private static bool ExecuteCombineOperations(Body body, CraftingPlan plan)
	{
		if ((Object)(object)body == (Object)null || plan == null)
		{
			return false;
		}
		foreach (CraftingCombineOperation combineOperation in plan.CombineOperations)
		{
			if ((Object)(object)combineOperation?.Target == (Object)null || (Object)(object)combineOperation.Source == (Object)null || !body.CanCombine(combineOperation.Target, combineOperation.Source))
			{
				return false;
			}
			body.CombineItems(combineOperation.Target, combineOperation.Source);
		}
		return true;
	}

	private static void RefreshRecipeListAfterInventorySettles(PlayerCamera camera)
	{
		if (!((Object)(object)camera == (Object)null) && ((Behaviour)camera).isActiveAndEnabled)
		{
			((MonoBehaviour)camera).StartCoroutine(RefreshRecipeListNextFrame(camera));
		}
	}

	private static IEnumerator RefreshRecipeListNextFrame(PlayerCamera camera)
	{
		yield return null;
		if ((Object)(object)camera != (Object)null && ((Behaviour)camera).isActiveAndEnabled)
		{
			camera.RefreshRecipeList();
		}
	}

	private static void RefreshBulkCraftingUi(PlayerCamera camera)
	{
		if (!((Object)(object)camera == (Object)null) && !((Object)(object)camera.craftingPanel == (Object)null) && camera.selectedRecipe >= 0 && camera.selectedRecipe < Recipes.recipes.Count)
		{
			Recipe recipe = Recipes.recipes[camera.selectedRecipe];
			EnsureAmountBelongsToRecipe(camera.selectedRecipe);
			CraftingPlan craftingPlan = CraftingPlanService.BuildPlan(recipe, selectedAmount);
			selectedAmount = ((craftingPlan.MaxCraftable <= 0) ? 1 : Mathf.Clamp(selectedAmount, 1, craftingPlan.MaxCraftable));
			Transform transform = camera.craftingPanel.transform;
			bool flag = craftingPlan.MaxCraftable > 1;
			UpdateFillerCraftText(transform, (!flag) ? 1 : selectedAmount);
			GameObject val = EnsureFillerControls(transform);
			val.SetActive(flag);
			UpdateControls(val, craftingPlan.MaxCraftable);
			UpdateIntermediateStatus(transform, ShowIntermediateStatus ? CraftingPlanService.BuildPlan(recipe, selectedAmount) : null);
		}
	}

	private static void EnsureAmountBelongsToRecipe(int recipeIndex)
	{
		if (selectedAmountRecipeIndex != recipeIndex)
		{
			selectedAmountRecipeIndex = recipeIndex;
			selectedAmount = 1;
		}
	}

	private static GameObject EnsureFillerControls(Transform panel)
	{
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Expected O, but got Unknown
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Expected O, but got Unknown
		//IL_0101: Unknown result type (might be due to invalid IL or missing references)
		//IL_010e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0125: Unknown result type (might be due to invalid IL or missing references)
		//IL_0132: Unknown result type (might be due to invalid IL or missing references)
		//IL_013f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0165: Unknown result type (might be due to invalid IL or missing references)
		//IL_0174: Unknown result type (might be due to invalid IL or missing references)
		//IL_0183: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d3: Unknown result type (might be due to invalid IL or missing references)
		Transform val = panel.Find("Filler1/QoL_BulkCraftTextControls");
		if ((Object)(object)val != (Object)null)
		{
			return ((Component)val).gameObject;
		}
		Transform val2 = panel.Find("Filler1");
		if ((Object)(object)val2 == (Object)null)
		{
			GameObject val3 = new GameObject("QoL_BulkCraftTextControls", new Type[1] { typeof(RectTransform) });
			val3.transform.SetParent(panel, false);
			return val3;
		}
		Transform obj = panel.Find("QoL_BulkCraftControls");
		GameObject val4 = ((obj != null) ? ((Component)obj).gameObject : null);
		if ((Object)(object)val4 != (Object)null)
		{
			Object.Destroy((Object)(object)val4);
		}
		GameObject val5 = new GameObject("QoL_BulkCraftTextControls", new Type[1] { typeof(RectTransform) });
		val5.transform.SetParent(val2, false);
		RectTransform component = val5.GetComponent<RectTransform>();
		component.anchorMin = Vector2.zero;
		component.anchorMax = Vector2.one;
		component.pivot = new Vector2(0.5f, 0.5f);
		component.offsetMin = Vector2.zero;
		component.offsetMax = Vector2.zero;
		CreateTextButton(val5.transform, "Decrease", "-", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(22f, 0f), delegate(bool shift)
		{
			SetSelectedAmount(shift ? 1 : (selectedAmount - 1), panel);
		});
		CreateTextButton(val5.transform, "Increase", "+", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-22f, 0f), delegate(bool shift)
		{
			SetSelectedAmount(shift ? GetCurrentMaxCraftable() : (selectedAmount + 1), panel);
		});
		return val5;
	}

	private static void CreateTextButton(Transform parent, string name, string label, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Action<bool> onClick)
	{
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Expected O, but got Unknown
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		GameObject val = new GameObject(name, new Type[4]
		{
			typeof(RectTransform),
			typeof(TextMeshProUGUI),
			typeof(Button),
			typeof(ShiftClickHandler)
		});
		val.transform.SetParent(parent, false);
		RectTransform component = val.GetComponent<RectTransform>();
		component.anchorMin = anchorMin;
		component.anchorMax = anchorMax;
		component.pivot = new Vector2(0.5f, 0.5f);
		component.anchoredPosition = anchoredPosition;
		component.sizeDelta = new Vector2(28f, 26f);
		TextMeshProUGUI component2 = val.GetComponent<TextMeshProUGUI>();
		((TMP_Text)component2).text = label;
		((TMP_Text)component2).fontSize = 18f;
		((TMP_Text)component2).alignment = (TextAlignmentOptions)514;
		((Graphic)component2).color = Color.white;
		((Graphic)component2).raycastTarget = true;
		Transform parent2 = parent.parent;
		TextMeshProUGUI val2 = ((parent2 != null) ? ((Component)parent2).GetComponent<TextMeshProUGUI>() : null);
		if ((Object)(object)val2 != (Object)null)
		{
			((TMP_Text)component2).font = ((TMP_Text)val2).font;
			((TMP_Text)component2).fontMaterial = ((TMP_Text)val2).fontMaterial;
			((TMP_Text)component2).fontSize = ((TMP_Text)val2).fontSize;
		}
		Button component3 = val.GetComponent<Button>();
		((Selectable)component3).targetGraphic = (Graphic)(object)component2;
		((Selectable)component3).transition = (Transition)0;
		ShiftClickHandler component4 = val.GetComponent<ShiftClickHandler>();
		component4.OnClick = onClick;
	}

	private static void UpdateControls(GameObject root, int maxCraftable)
	{
		if (!((Object)(object)root == (Object)null))
		{
			selectedAmount = Mathf.Clamp(selectedAmount, 1, Mathf.Max(1, maxCraftable));
			Transform obj = root.transform.Find("Decrease");
			Button val = ((obj != null) ? ((Component)obj).GetComponent<Button>() : null);
			Transform obj2 = root.transform.Find("Increase");
			Button val2 = ((obj2 != null) ? ((Component)obj2).GetComponent<Button>() : null);
			if ((Object)(object)val != (Object)null)
			{
				((Selectable)val).interactable = selectedAmount > 1;
			}
			if ((Object)(object)val2 != (Object)null)
			{
				((Selectable)val2).interactable = selectedAmount < maxCraftable;
			}
		}
	}

	private static void UpdateFillerCraftText(Transform panel, int amount)
	{
		Transform obj = panel.Find("Filler1");
		TextMeshProUGUI val = ((obj != null) ? ((Component)obj).GetComponent<TextMeshProUGUI>() : null);
		if (!((Object)(object)val == (Object)null))
		{
			((TMP_Text)val).text = ((amount > 1) ? $"Crafting {amount}x" : "Crafting");
		}
	}

	private static void UpdateIntermediateStatus(Transform panel, CraftingPlan plan)
	{
		if (!ShowIntermediateStatus)
		{
			Transform val = panel.Find("QoL_BulkCraftStatus");
			if ((Object)(object)val != (Object)null)
			{
				((Component)val).gameObject.SetActive(false);
			}
			return;
		}
		GameObject val2 = EnsureStatus(panel);
		string text = CraftingPlanService.BuildIntermediateSummary(plan);
		val2.SetActive(!string.IsNullOrEmpty(text));
		if (!string.IsNullOrEmpty(text))
		{
			((TMP_Text)val2.GetComponent<TextMeshProUGUI>()).text = text;
		}
	}

	private static GameObject EnsureStatus(Transform panel)
	{
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Expected O, but got Unknown
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_010a: Unknown result type (might be due to invalid IL or missing references)
		Transform val = panel.Find("QoL_BulkCraftStatus");
		if ((Object)(object)val != (Object)null)
		{
			return ((Component)val).gameObject;
		}
		GameObject val2 = new GameObject("QoL_BulkCraftStatus", new Type[2]
		{
			typeof(RectTransform),
			typeof(TextMeshProUGUI)
		});
		val2.transform.SetParent(panel, false);
		RectTransform component = val2.GetComponent<RectTransform>();
		component.anchorMin = new Vector2(0.5f, 0f);
		component.anchorMax = new Vector2(0.5f, 0f);
		component.pivot = new Vector2(0.5f, 0f);
		component.anchoredPosition = new Vector2(0f, 72f);
		component.sizeDelta = new Vector2(360f, 42f);
		TextMeshProUGUI component2 = val2.GetComponent<TextMeshProUGUI>();
		((TMP_Text)component2).fontSize = 13f;
		((TMP_Text)component2).alignment = (TextAlignmentOptions)514;
		((Graphic)component2).color = new Color(0.78f, 0.95f, 1f, 1f);
		Transform child = panel.GetChild(10);
		TextMeshProUGUI val3 = ((child != null) ? ((Component)child).GetComponent<TextMeshProUGUI>() : null);
		if ((Object)(object)val3 != (Object)null)
		{
			((TMP_Text)component2).font = ((TMP_Text)val3).font;
			((TMP_Text)component2).fontMaterial = ((TMP_Text)val3).fontMaterial;
		}
		return val2;
	}

	private static void SetSelectedAmount(int amount, Transform panel)
	{
		Recipe val = CurrentRecipe();
		int num = 1;
		if (val != null)
		{
			EnsureAmountBelongsToRecipe(PlayerCamera.main.selectedRecipe);
			num = Mathf.Max(1, CraftingPlanService.BuildPlan(val, amount).MaxCraftable);
		}
		selectedAmount = Mathf.Clamp(amount, 1, num);
		UpdateFillerCraftText(panel, selectedAmount);
		Transform obj = panel.Find("Filler1/QoL_BulkCraftTextControls");
		GameObject val2 = ((obj != null) ? ((Component)obj).gameObject : null);
		if ((Object)(object)val2 != (Object)null)
		{
			UpdateControls(val2, num);
		}
		if ((Object)(object)PlayerCamera.main != (Object)null)
		{
			UpdateIntermediateStatus(panel, ShowIntermediateStatus ? CraftingPlanService.BuildPlan(val, selectedAmount) : null);
		}
	}

	private static Recipe CurrentRecipe()
	{
		if ((Object)(object)PlayerCamera.main == (Object)null || PlayerCamera.main.selectedRecipe < 0 || PlayerCamera.main.selectedRecipe >= Recipes.recipes.Count)
		{
			return null;
		}
		return Recipes.recipes[PlayerCamera.main.selectedRecipe];
	}

	private static int GetCurrentMaxCraftable()
	{
		Recipe val = CurrentRecipe();
		return (val == null) ? 1 : Mathf.Max(1, CraftingPlanService.BuildPlan(val, selectedAmount).MaxCraftable);
	}
}
