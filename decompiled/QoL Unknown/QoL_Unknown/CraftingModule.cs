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

internal static class CraftingModule
{
	private sealed class ShiftClickHandler : MonoBehaviour, IPointerClickHandler, IEventSystemHandler
	{
		public Action<bool> OnClick;

		public void OnPointerClick(PointerEventData eventData)
		{
			OnClick?.Invoke(Input.GetKey((KeyCode)304) || Input.GetKey((KeyCode)303));
		}
	}

	private const string SecondaryCraftMarker = "<color=#ffa500><sprite index=17 tint=1></color>";

	private const string BlockedSecondaryCraftMarker = "<color=#ff4500><sprite index=17 tint=1></color>";

	private static readonly Color ClearerRecipeAvailableColor = new Color(0.5f, 1f, 0.5f);

	private static readonly Color ClearerRecipeUnavailableColor = new Color(0.5f, 0f, 0f);

	private static readonly FieldInfo RecipeObjectsField = AccessTools.Field(typeof(PlayerCamera), "recipeObjects");

	private static readonly FieldInfo RecipeItemFilterField = AccessTools.Field(typeof(PlayerCamera), "recipeItemFilter");

	private static readonly FieldInfo SelectedRecipeCategoryField = AccessTools.Field(typeof(PlayerCamera), "selectedRecipeCategory");

	private static ManualLogSource Logger;

	private static CraftInventory cachedInventory;

	private static int cachedInventoryFrame = -1;

	private static Body cachedInventoryBody;

	private static int selectedAmount = 1;

	private static int selectedAmountRecipeIndex = -1;

	private static PlayerCamera refreshScheduledFor;

	private static readonly Color FavouriteBorderDim = new Color(0.45f, 0.42f, 0.2f);

	private static readonly Color FavouriteBorderBright = CraftFavourites.BookmarkColor;

	public static void Initialize(ManualLogSource logger)
	{
		Logger = logger;
		CraftRunner.Logger = logger;
		if (!RuntimePatchRegistry.PatchAllOnce("org.bepinex.plugins.qol.bulkcrafting", typeof(CraftingModule), logger))
		{
		}
	}

	public static void ScheduleRefresh(PlayerCamera camera)
	{
		if (!((Object)(object)camera == (Object)null) && ((Behaviour)camera).isActiveAndEnabled && refreshScheduledFor != camera)
		{
			refreshScheduledFor = camera;
			((MonoBehaviour)camera).StartCoroutine(RefreshNextFrame(camera));
		}
	}

	internal static void ForceRefresh(PlayerCamera camera)
	{
		if (!((Object)(object)camera == (Object)null) && ((Behaviour)camera).isActiveAndEnabled)
		{
			refreshScheduledFor = null;
			ScheduleRefresh(camera);
		}
	}

	internal static void InvalidateInventoryCache()
	{
		cachedInventoryFrame = -1;
		cachedInventoryBody = null;
	}

	private static IEnumerator RefreshNextFrame(PlayerCamera camera)
	{
		yield return null;
		if (refreshScheduledFor == camera)
		{
			refreshScheduledFor = null;
		}
		if ((Object)(object)camera != (Object)null && ((Behaviour)camera).isActiveAndEnabled)
		{
			camera.RefreshRecipeList();
		}
	}

	private static CraftInventory LiveInventory(Body body)
	{
		if ((Object)(object)body == (Object)null)
		{
			return null;
		}
		if (cachedInventory != null && cachedInventoryFrame == Time.frameCount && (Object)(object)cachedInventoryBody == (Object)(object)body)
		{
			return cachedInventory;
		}
		cachedInventoryFrame = Time.frameCount;
		cachedInventoryBody = body;
		cachedInventory = CraftInventory.Capture(body);
		return cachedInventory;
	}

	[HarmonyPatch(typeof(Recipe), "GetItemsForRecipe")]
	[HarmonyPrefix]
	private static bool GetItemsForRecipe_Prefix(Recipe __instance, ref List<Item> __result, bool __runOriginal)
	{
		Body val = PlayerCamera.main?.body;
		if (__runOriginal && __instance?.items != null && !((Object)(object)val == (Object)null))
		{
			RecipeResult result = __instance.result;
			if (result == null || !result.isLiquid)
			{
				__result = ReachableItems.MatchIngredients(__instance, val, tolerateMissing: false);
				return false;
			}
		}
		return __runOriginal;
	}

	[HarmonyPatch(typeof(Recipe), "GetItemsForRecipeThorough")]
	[HarmonyPrefix]
	private static bool GetItemsForRecipeThorough_Prefix(Recipe __instance, ref List<Item> __result, bool __runOriginal)
	{
		Body val = PlayerCamera.main?.body;
		if (__runOriginal && __instance?.items != null && !((Object)(object)val == (Object)null))
		{
			RecipeResult result = __instance.result;
			if (result == null || !result.isLiquid)
			{
				__result = ReachableItems.MatchIngredients(__instance, val, tolerateMissing: true);
				return false;
			}
		}
		return __runOriginal;
	}

	[HarmonyPatch(typeof(PlayerCamera), "TryCraft")]
	[HarmonyPrefix]
	[HarmonyBefore(new string[] { "CasualtiesMP" })]
	[HarmonyPriority(800)]
	private static bool TryCraft_Prefix(PlayerCamera __instance)
	{
		bool flag = HandleCraftClick(__instance);
		if (!flag)
		{
			MultiplayerCompat.MarkCraftHandled();
		}
		return flag;
	}

	private static bool HandleCraftClick(PlayerCamera __instance)
	{
		if ((Object)(object)__instance == (Object)null || !IsRecipeSelected(__instance))
		{
			return false;
		}
		bool isActive = MultiplayerCompat.IsActive;
		if (isActive && CraftRunner.IsMultiplayerCraftRunning)
		{
			CraftRunner.CancelMultiplayerCraft();
			__instance.DoAlert("Autocraft cancelled. An already-sent craft may still finish.", false);
			return false;
		}
		Recipe val = Recipes.recipes[__instance.selectedRecipe];
		EnsureAmountBelongsToRecipe(__instance.selectedRecipe);
		if (isActive && selectedAmount == 1)
		{
			RecipeResult result = val.result;
			if (result != null && result.isLiquid)
			{
				return true;
			}
		}
		CraftInventory inventory = LiveInventory(__instance.body);
		CraftPlan craftPlan = CraftPlanner.Build(val, selectedAmount, inventory);
		selectedAmount = Mathf.Max(1, craftPlan.RequestedAmount);
		if (isActive && craftPlan.TotalSteps <= 1 && !craftPlan.HasPreparations)
		{
			return true;
		}
		if (!CraftRunner.Run(__instance, craftPlan))
		{
			__instance.PlayUISound((UISoundType)6, 1f);
			__instance.RefreshRecipeList();
		}
		return false;
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
			RebuildRecipeList(__instance);
			return false;
		}
		catch (Exception ex)
		{
			ManualLogSource logger = Logger;
			if (logger != null)
			{
				logger.LogWarning((object)("[Crafting] Falling back to the vanilla recipe list: " + ex.Message));
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
		__result = BuildIngredientText(recipe, its);
		return false;
	}

	[HarmonyPatch(typeof(PlayerCamera), "RefreshCurrentlySelectedRecipe")]
	[HarmonyPostfix]
	private static void RefreshCurrentlySelectedRecipe_Postfix(PlayerCamera __instance)
	{
		try
		{
			UpdateAmountControls(__instance);
		}
		catch (Exception ex)
		{
			ManualLogSource logger = Logger;
			if (logger != null)
			{
				logger.LogWarning((object)("[Crafting] Failed to refresh the amount controls: " + ex.Message));
			}
		}
	}

	private static void RebuildRecipeList(PlayerCamera camera)
	{
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		//IL_02af: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f4: Unknown result type (might be due to invalid IL or missing references)
		if (RecipeObjectsField?.GetValue(camera) is List<GameObject> list)
		{
			foreach (GameObject item in list)
			{
				Object.Destroy((Object)(object)item);
			}
		}
		List<GameObject> list2 = new List<GameObject>();
		RecipeObjectsField?.SetValue(camera, list2);
		object? obj = RecipeItemFilterField?.GetValue(camera);
		Item val = (Item)((obj is Item) ? obj : null);
		RecipeCategory? selectedCategory = SelectedRecipeCategoryField?.GetValue(camera) as RecipeCategory?;
		CraftInventory inventory = LiveInventory(camera.body);
		Color availableColor = (PlayerCamera.clearerRecipes ? ClearerRecipeAvailableColor : Color.white);
		Color unavailableColor = (PlayerCamera.clearerRecipes ? ClearerRecipeUnavailableColor : Color.gray);
		IEnumerable<Recipe> enumerable = from rec in Recipes.GetVisibleRecipes(val)
			orderby rec.INT
			select rec;
		if (selectedCategory.HasValue && string.IsNullOrEmpty(camera.recipeFilter) && (Object)(object)val == (Object)null)
		{
			enumerable = enumerable.Where((Recipe rec) => rec.category == selectedCategory.Value);
		}
		if (!string.IsNullOrEmpty(camera.recipeFilter) && (Object)(object)val == (Object)null)
		{
			enumerable = enumerable.Where((Recipe rec) => rec.simpleName.IndexOf(camera.recipeFilter, StringComparison.OrdinalIgnoreCase) >= 0);
		}
		List<Recipe> list3 = new List<Recipe>();
		List<Recipe> list4 = new List<Recipe>();
		List<Recipe> list5 = new List<Recipe>();
		List<Recipe> list6 = new List<Recipe>();
		foreach (Recipe item2 in enumerable)
		{
			bool flag = CraftPlanner.IsCraftable(item2, inventory);
			bool flag2 = CraftFavourites.IsFavourited(item2);
			if (flag2 && flag)
			{
				list3.Add(item2);
			}
			else if (flag2)
			{
				list4.Add(item2);
			}
			else if (flag)
			{
				list5.Add(item2);
			}
			else
			{
				list6.Add(item2);
			}
		}
		List<Recipe> list7 = new List<Recipe>();
		list7.AddRange(list3);
		list7.AddRange(list4);
		list7.AddRange(list5);
		list7.AddRange(list6);
		for (int num = 0; num < list7.Count; num++)
		{
			list2.Add(CreateRecipeRow(camera, list7[num], num, availableColor, unavailableColor));
		}
		camera.recipeListContent.sizeDelta = new Vector2(1f, (float)list7.Count * 64f);
		RecipeItemFilterField?.SetValue(camera, null);
		camera.RefreshCurrentlySelectedRecipe();
	}

	private static GameObject CreateRecipeRow(PlayerCamera camera, Recipe recipe, int row, Color availableColor, Color unavailableColor)
	{
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0102: Unknown result type (might be due to invalid IL or missing references)
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		//IL_014a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0143: Unknown result type (might be due to invalid IL or missing references)
		//IL_013d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0139: Unknown result type (might be due to invalid IL or missing references)
		//IL_0173: Unknown result type (might be due to invalid IL or missing references)
		//IL_017d: Expected O, but got Unknown
		bool flag = CraftPlanner.IsCraftable(recipe, LiveInventory(camera.body));
		bool flag2 = CraftFavourites.IsFavourited(recipe);
		GameObject val = Utils.Create("Special/RecipeBox", (Transform)(object)camera.recipeListContent);
		val.GetComponent<RectTransform>().anchoredPosition = new Vector2(-9f, (float)(-row * 64));
		TextMeshProUGUI component = ((Component)val.transform.GetChild(0)).GetComponent<TextMeshProUGUI>();
		((TMP_Text)component).text = recipe.fullName;
		((Graphic)component).color = (flag ? availableColor : unavailableColor);
		TextMeshProUGUI component2 = ((Component)val.transform.GetChild(1)).GetComponent<TextMeshProUGUI>();
		((TMP_Text)component2).text = recipe.INT.ToString();
		((Graphic)component2).color = camera.RecipeINTToColor(recipe.INT);
		ValueTuple<Sprite, Color> resultSprite = recipe.resultSprite;
		Sprite item = resultSprite.Item1;
		Color item2 = resultSprite.Item2;
		Image component3 = ((Component)val.transform.GetChild(2)).GetComponent<Image>();
		component3.sprite = item;
		((Graphic)component3).color = item2;
		((Component)component3).GetComponent<RectTransform>().sizeDelta = PlayerCamera.ImageSizeDelta((Texture)(object)item.texture, 3f, 42f);
		((Graphic)val.GetComponent<Image>()).color = ((!flag2) ? (flag ? availableColor : unavailableColor) : (flag ? FavouriteBorderBright : FavouriteBorderDim));
		int recipeIndex = recipe.index;
		((UnityEvent)val.GetComponent<Button>().onClick).AddListener((UnityAction)delegate
		{
			camera.SelectRecipe(recipeIndex);
		});
		UITooltip val2 = val.AddComponent<UITooltip>();
		val2.skipLocale = true;
		val2.tipName = recipe.simpleName;
		val2.tipDesc = recipe.description;
		return val;
	}

	private static string BuildIngredientText(Recipe recipe, List<Item> its)
	{
		CraftInventory working = LiveInventory(PlayerCamera.main?.body)?.Clone();
		HashSet<string> coveredAutoCrafts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		if (its == null)
		{
			its = recipe.GetItemsForRecipeThorough();
		}
		string text = "";
		for (int i = 0; i < recipe.items.Count; i++)
		{
			RecipeItem ingredient = recipe.items[i];
			text += "<color=#FFFFFF>";
			text += AvailabilityMarker(ingredient, working, coveredAutoCrafts);
			text = ((!ingredient.specific) ? ((!ingredient.isLiquid) ? (text + Locale.GetOther((ingredient.quality.id == "hammering" || ingredient.quality.id == "cutting") ? "craftanytool" : "craftanyitem") + "\n") : (text + Locale.GetOther("craftanyliquid") + "\n")) : ((!ingredient.isLiquid) ? (text + Locale.GetItem(ingredient.specificId) + "\n") : (text + Locale.GetOther(ingredient.specificId) + "\n")));
			text += "<color=#666666>";
			if (ingredient.isLiquid)
			{
				if (!ingredient.specific)
				{
					text = text + Locale.GetOther("craftliquidquality").Replace("<1>", ingredient.quality.amount.ToString("0.#")).Replace("<2>", ingredient.quality.LocaleName) + "\n";
				}
				else if (ingredient.minimumCondition > 0f)
				{
					text = text + Locale.GetOther("craftml").Replace("<>", ingredient.minimumCondition.ToString("0.#")) + "\n";
				}
				continue;
			}
			if (!ingredient.specific)
			{
				text += Locale.GetOther("craftitemquality").Replace("<1>", ingredient.quality.amount.ToString("0.#")).Replace("<2>", ingredient.quality.LocaleName);
				KeyValuePair<CraftingQuality, string> keyValuePair = Recipes.QualityExamples.FirstOrDefault((KeyValuePair<CraftingQuality, string> x) => x.Key.id == ingredient.quality.id && x.Key.amount == ingredient.quality.amount);
				if (keyValuePair.Value != null)
				{
					text += Locale.GetOther("craftexample").Replace("<>", Locale.GetItem(keyValuePair.Value));
				}
				text += "\n";
			}
			if (ingredient.minimumCondition > 0f)
			{
				text = text + Locale.GetOther("craftcondition").Replace("<>", (ingredient.minimumCondition * 100f).ToString("0.#")) + "\n";
			}
		}
		return text;
	}

	private static string AvailabilityMarker(RecipeItem ingredient, CraftInventory working, HashSet<string> coveredAutoCrafts)
	{
		switch (CraftPlanner.InspectIngredient(ingredient, working, coveredAutoCrafts))
		{
		case CraftIngredientStatus.Available:
			return "<sprite index=23>";
		case CraftIngredientStatus.AutoCraftable:
			return "<color=#ffa500><sprite index=17 tint=1></color>";
		case CraftIngredientStatus.AutoCraftableButBlocked:
		{
			string text = CraftPlanner.AutoCraftKey(ingredient);
			return (!string.IsNullOrEmpty(text) && coveredAutoCrafts.Contains(text)) ? "<color=#ff4500><sprite index=17 tint=1></color>" : "<sprite index=24>";
		}
		default:
			return "<sprite index=24>";
		}
	}

	private static void UpdateAmountControls(PlayerCamera camera)
	{
		if (!((Object)(object)camera == (Object)null) && !((Object)(object)camera.craftingPanel == (Object)null) && IsRecipeSelected(camera))
		{
			Recipe recipe = Recipes.recipes[camera.selectedRecipe];
			EnsureAmountBelongsToRecipe(camera.selectedRecipe);
			int num = Mathf.Max(1, CraftPlanner.MaxCraftable(recipe, LiveInventory(camera.body)));
			selectedAmount = Mathf.Clamp(selectedAmount, 1, num);
			Transform transform = camera.craftingPanel.transform;
			UpdateFillerCraftText(transform, selectedAmount);
			GameObject val = EnsureAmountControls(transform);
			val.SetActive(num > 1);
			UpdateButtons(val, num);
		}
	}

	private static bool IsRecipeSelected(PlayerCamera camera)
	{
		return camera.selectedRecipe >= 0 && Recipes.recipes != null && camera.selectedRecipe < Recipes.recipes.Count;
	}

	private static void EnsureAmountBelongsToRecipe(int recipeIndex)
	{
		if (selectedAmountRecipeIndex != recipeIndex)
		{
			selectedAmountRecipeIndex = recipeIndex;
			selectedAmount = 1;
		}
	}

	private static GameObject EnsureAmountControls(Transform panel)
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
			SetSelectedAmount(shift ? CurrentMax() : (selectedAmount + 1), panel);
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
		val.GetComponent<ShiftClickHandler>().OnClick = onClick;
	}

	private static void UpdateButtons(GameObject root, int max)
	{
		if (!((Object)(object)root == (Object)null))
		{
			Transform val = root.transform.Find("Decrease");
			Transform val2 = root.transform.Find("Increase");
			if ((Object)(object)val != (Object)null)
			{
				((Selectable)((Component)val).GetComponent<Button>()).interactable = selectedAmount > 1;
			}
			if ((Object)(object)val2 != (Object)null)
			{
				((Selectable)((Component)val2).GetComponent<Button>()).interactable = selectedAmount < max;
			}
		}
	}

	private static void UpdateFillerCraftText(Transform panel, int amount)
	{
		Transform obj = panel.Find("Filler1");
		TextMeshProUGUI val = ((obj != null) ? ((Component)obj).GetComponent<TextMeshProUGUI>() : null);
		if ((Object)(object)val != (Object)null)
		{
			((TMP_Text)val).text = ((amount > 1) ? $"Crafting {amount}x" : "Crafting");
		}
	}

	private static void SetSelectedAmount(int amount, Transform panel)
	{
		selectedAmount = Mathf.Clamp(amount, 1, CurrentMax());
		selectedAmountRecipeIndex = (((Object)(object)PlayerCamera.main != (Object)null) ? PlayerCamera.main.selectedRecipe : (-1));
		UpdateFillerCraftText(panel, selectedAmount);
		Transform obj = panel.Find("Filler1/QoL_BulkCraftTextControls");
		GameObject val = ((obj != null) ? ((Component)obj).gameObject : null);
		if ((Object)(object)val != (Object)null)
		{
			UpdateButtons(val, CurrentMax());
		}
	}

	private static int CurrentMax()
	{
		PlayerCamera main = PlayerCamera.main;
		if ((Object)(object)main == (Object)null || !IsRecipeSelected(main))
		{
			return 1;
		}
		return Mathf.Max(1, CraftPlanner.MaxCraftable(Recipes.recipes[main.selectedRecipe], LiveInventory(main.body)));
	}
}
