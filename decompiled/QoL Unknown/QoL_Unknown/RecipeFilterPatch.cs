using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace QoL_Unknown;

[HarmonyPatch]
internal class RecipeFilterPatch
{
	private static readonly FieldInfo RecipeObjectsField = AccessTools.Field(typeof(PlayerCamera), "recipeObjects");

	private static readonly FieldInfo SelectedRecipeCategoryField = AccessTools.Field(typeof(PlayerCamera), "selectedRecipeCategory");

	private static readonly FieldInfo RecipeItemFilterField = AccessTools.Field(typeof(PlayerCamera), "recipeItemFilter");

	private static readonly Color ClearerRecipeAvailableColor = new Color(0.5f, 1f, 0.5f);

	private static readonly Color ClearerRecipeUnavailableColor = new Color(0.5f, 0f, 0f);

	[HarmonyPatch(typeof(PlayerCamera), "RefreshRecipeList")]
	[HarmonyPrefix]
	[HarmonyBefore(new string[] { "org.bepinex.plugins.qol.bulkcrafting" })]
	private static bool RefreshRecipeList_Prefix(PlayerCamera __instance)
	{
		if (QoLModuleManager.IsEnabled("qol_craft_bulk"))
		{
			return true;
		}
		if (RecipeObjectsField == null || SelectedRecipeCategoryField == null || RecipeItemFilterField == null)
		{
			return true;
		}
		object? value = RecipeItemFilterField.GetValue(__instance);
		Item recipeItemFilter = (Item)((value is Item) ? value : null);
		RebuildRecipeList(__instance, recipeItemFilter);
		return false;
	}

	internal static bool RefreshRecipeListForItem(PlayerCamera camera, Item itemFilter)
	{
		if (RecipeObjectsField == null || SelectedRecipeCategoryField == null || RecipeItemFilterField == null || (Object)(object)camera == (Object)null || (Object)(object)itemFilter == (Object)null)
		{
			return false;
		}
		RecipeItemFilterField.SetValue(camera, itemFilter);
		camera.RefreshRecipeList();
		return true;
	}

	private static void RebuildRecipeList(PlayerCamera __instance, Item recipeItemFilter)
	{
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
		//IL_0172: Unknown result type (might be due to invalid IL or missing references)
		//IL_0179: Unknown result type (might be due to invalid IL or missing references)
		//IL_0457: Unknown result type (might be due to invalid IL or missing references)
		//IL_0289: Unknown result type (might be due to invalid IL or missing references)
		//IL_02cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_031e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0365: Unknown result type (might be due to invalid IL or missing references)
		//IL_0398: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_03dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e6: Expected O, but got Unknown
		List<GameObject> list = RecipeObjectsField.GetValue(__instance) as List<GameObject>;
		if (list == null)
		{
			list = new List<GameObject>();
			RecipeObjectsField.SetValue(__instance, list);
		}
		for (int i = 0; i < list.Count; i++)
		{
			Object.Destroy((Object)(object)list[i]);
		}
		list.Clear();
		RecipeCategory? val = (RecipeCategory?)SelectedRecipeCategoryField.GetValue(__instance);
		string recipeFilter = __instance.recipeFilter;
		bool flag = (Object)(object)recipeItemFilter != (Object)null;
		bool flag2 = !flag && !string.IsNullOrEmpty(recipeFilter);
		bool flag3 = val.HasValue && !flag2 && !flag;
		Color val2 = (PlayerCamera.clearerRecipes ? ClearerRecipeAvailableColor : Color.white);
		Color val3 = (PlayerCamera.clearerRecipes ? ClearerRecipeUnavailableColor : Color.gray);
		List<Recipe> list2 = new List<Recipe>(Recipes.GetVisibleRecipes(recipeItemFilter));
		list2.Sort((Recipe a, Recipe b) => a.INT.CompareTo(b.INT));
		List<(Recipe, bool)> list3 = new List<(Recipe, bool)>(list2.Count);
		List<(Recipe, bool)> list4 = new List<(Recipe, bool)>();
		for (int num = 0; num < list2.Count; num++)
		{
			Recipe val4 = list2[num];
			if (val4 != null && (!flag3 || val4.category == val.Value) && (!flag2 || MiscPatches.RecipeMatchesFilter(val4, recipeFilter)))
			{
				bool flag4 = val4.GetItemsForRecipe() != null;
				if (flag4)
				{
					list3.Add((val4, flag4));
				}
				else
				{
					list4.Add((val4, flag4));
				}
			}
		}
		list3.AddRange(list4);
		int num2 = 0;
		for (int num3 = 0; num3 < list3.Count; num3++)
		{
			Recipe item = list3[num3].Item1;
			bool item2 = list3[num3].Item2;
			if (item.visible)
			{
				GameObject val5 = Utils.Create("Special/RecipeBox", (Transform)(object)__instance.recipeListContent);
				val5.GetComponent<RectTransform>().anchoredPosition = new Vector2(-9f, (float)(-num2 * 64));
				((TMP_Text)((Component)val5.transform.GetChild(0)).GetComponent<TextMeshProUGUI>()).text = item.fullName;
				((Graphic)((Component)val5.transform.GetChild(0)).GetComponent<TextMeshProUGUI>()).color = (item2 ? val2 : val3);
				((TMP_Text)((Component)val5.transform.GetChild(1)).GetComponent<TextMeshProUGUI>()).text = item.INT.ToString();
				((Graphic)((Component)val5.transform.GetChild(1)).GetComponent<TextMeshProUGUI>()).color = __instance.RecipeINTToColor(item.INT);
				(Sprite, Color) resultSprite = item.resultSprite;
				((Component)val5.transform.GetChild(2)).GetComponent<Image>().sprite = resultSprite.Item1;
				((Graphic)((Component)val5.transform.GetChild(2)).GetComponent<Image>()).color = resultSprite.Item2;
				((Component)val5.transform.GetChild(2)).GetComponent<RectTransform>().sizeDelta = PlayerCamera.ImageSizeDelta((Texture)(object)resultSprite.Item1.texture, 3f, 42f);
				((Graphic)val5.GetComponent<Image>()).color = (item2 ? val2 : val3);
				int toIndex = item.index;
				((UnityEvent)val5.GetComponent<Button>().onClick).AddListener((UnityAction)delegate
				{
					__instance.SelectRecipe(toIndex);
				});
				UITooltip val6 = val5.AddComponent<UITooltip>();
				val6.skipLocale = true;
				val6.tipName = item.simpleName;
				val6.tipDesc = item.description;
				list.Add(val5);
				num2++;
			}
		}
		__instance.recipeListContent.sizeDelta = new Vector2(1f, (float)num2 * 64f);
		RecipeItemFilterField.SetValue(__instance, null);
		__instance.RefreshCurrentlySelectedRecipe();
	}
}
