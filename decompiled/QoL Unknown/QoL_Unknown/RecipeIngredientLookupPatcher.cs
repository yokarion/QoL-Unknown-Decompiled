using System.Collections.Generic;
using BepInEx.Logging;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace QoL_Unknown;

internal static class RecipeIngredientLookupPatcher
{
	private sealed class IngredientClickHandler : MonoBehaviour, IPointerClickHandler, IEventSystemHandler
	{
		private const float HoverGrey = 0.8f;

		private TextMeshProUGUI label;

		private Color baseColor = Color.white;

		private bool hovered;

		private void Awake()
		{
			label = ((Component)this).GetComponent<TextMeshProUGUI>();
		}

		private void Update()
		{
			//IL_009f: Unknown result type (might be due to invalid IL or missing references)
			//IL_003e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0043: Unknown result type (might be due to invalid IL or missing references)
			//IL_008c: Unknown result type (might be due to invalid IL or missing references)
			if ((Object)(object)label == (Object)null)
			{
				return;
			}
			bool flag = QoLModuleManager.IsEnabled("qol_ui_preview") && IsPointerOver(label);
			if (flag != hovered)
			{
				if (flag)
				{
					baseColor = ((Graphic)label).color;
					((Graphic)label).color = new Color(baseColor.r * 0.8f, baseColor.g * 0.8f, baseColor.b * 0.8f, baseColor.a);
				}
				else
				{
					((Graphic)label).color = baseColor;
				}
				hovered = flag;
			}
		}

		private static bool IsPointerOver(TextMeshProUGUI target)
		{
			//IL_0011: Unknown result type (might be due to invalid IL or missing references)
			//IL_0028: Unknown result type (might be due to invalid IL or missing references)
			//IL_002d: Unknown result type (might be due to invalid IL or missing references)
			Canvas canvas = ((Graphic)target).canvas;
			Camera val = (((Object)(object)canvas != (Object)null && (int)canvas.renderMode != 0) ? canvas.worldCamera : null);
			return RectTransformUtility.RectangleContainsScreenPoint(((TMP_Text)target).rectTransform, Vector2.op_Implicit(Input.mousePosition), val);
		}

		public void OnPointerClick(PointerEventData eventData)
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0068: Unknown result type (might be due to invalid IL or missing references)
			//IL_006d: Unknown result type (might be due to invalid IL or missing references)
			if ((int)eventData.button != 0 || !QoLModuleManager.IsEnabled("qol_ui_preview"))
			{
				return;
			}
			PlayerCamera main = PlayerCamera.main;
			if ((Object)(object)main == (Object)null || (Object)(object)main.dragItem != (Object)null || Recipes.recipes == null || main.selectedRecipe < 0 || main.selectedRecipe >= Recipes.recipes.Count)
			{
				return;
			}
			TextMeshProUGUI component = ((Component)this).GetComponent<TextMeshProUGUI>();
			if ((Object)(object)component == (Object)null)
			{
				return;
			}
			int num = TMP_TextUtilities.FindIntersectingCharacter((TMP_Text)(object)component, Vector2.op_Implicit(eventData.position), eventData.pressEventCamera, true);
			if (num < 0)
			{
				return;
			}
			int num2 = IngredientIndexAt(component, num);
			Recipe val = Recipes.recipes[main.selectedRecipe];
			if (num2 < 0 || val.items == null || num2 >= val.items.Count)
			{
				return;
			}
			RecipeItem val2 = val.items[num2];
			if (!val2.specific)
			{
				return;
			}
			if (Input.GetKey((KeyCode)304) || Input.GetKey((KeyCode)303))
			{
				Item val3 = ResolveFilterItem(val, num2, val2);
				if ((Object)(object)val3 == (Object)null || !RecipeFilterPatch.RefreshRecipeListForItem(main, val3))
				{
					main.PlayUISound((UISoundType)6, 1f);
				}
			}
			else
			{
				Recipe val4 = FindProducingRecipe(val2);
				if (val4 != null)
				{
					main.SelectRecipe(val4.index);
				}
				else
				{
					main.PlayUISound((UISoundType)6, 1f);
				}
			}
		}
	}

	public static void Initialize(ManualLogSource logger)
	{
		RuntimePatchRegistry.PatchAllOnce("org.bepinex.plugins.qol.ingredientlookup", typeof(RecipeIngredientLookupPatcher), logger);
	}

	[HarmonyPatch(typeof(PlayerCamera), "RefreshCurrentlySelectedRecipe")]
	[HarmonyPostfix]
	private static void RefreshCurrentlySelectedRecipe_Postfix(PlayerCamera __instance)
	{
		if (!((Object)(object)__instance == (Object)null) && !((Object)(object)__instance.craftingPanel == (Object)null) && __instance.craftingPanel.transform.childCount > 8)
		{
			GameObject gameObject = ((Component)__instance.craftingPanel.transform.GetChild(8)).gameObject;
			if ((Object)(object)gameObject.GetComponent<IngredientClickHandler>() == (Object)null)
			{
				gameObject.AddComponent<IngredientClickHandler>();
			}
		}
	}

	private static int IngredientIndexAt(TextMeshProUGUI text, int charIndex)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Invalid comparison between Unknown and I4
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		TMP_TextInfo textInfo = ((TMP_Text)text).textInfo;
		int num = -1;
		for (int i = 0; i <= charIndex && i < textInfo.characterCount; i++)
		{
			TMP_CharacterInfo val = textInfo.characterInfo[i];
			if ((int)val.elementType == 1 && (val.spriteIndex == 17 || val.spriteIndex == 23 || val.spriteIndex == 24))
			{
				num++;
			}
		}
		return num;
	}

	private static Item ResolveFilterItem(Recipe recipe, int ingredientIndex, RecipeItem ingredient)
	{
		if (ingredient.isLiquid)
		{
			List<Item> itemsForRecipeThorough = recipe.GetItemsForRecipeThorough();
			if (itemsForRecipeThorough == null || ingredientIndex >= itemsForRecipeThorough.Count)
			{
				return null;
			}
			return itemsForRecipeThorough[ingredientIndex];
		}
		GameObject val = Resources.Load<GameObject>(ingredient.specificId);
		if (!((Object)(object)val != (Object)null))
		{
			return null;
		}
		return val.GetComponent<Item>();
	}

	private static Recipe FindProducingRecipe(RecipeItem ingredient)
	{
		for (int i = 0; i < Recipes.recipes.Count; i++)
		{
			Recipe val = Recipes.recipes[i];
			if (val != null && !val.isRepair && val.result != null && val.result.isLiquid == ingredient.isLiquid && val.result.id == ingredient.specificId && val.visible)
			{
				return val;
			}
		}
		return null;
	}
}
