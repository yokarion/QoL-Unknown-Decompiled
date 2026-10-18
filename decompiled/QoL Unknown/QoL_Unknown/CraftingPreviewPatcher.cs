using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx.Logging;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace QoL_Unknown;

internal class CraftingPreviewPatcher
{
	private static ManualLogSource Logger;

	private static readonly FieldInfo RecipeItemPreviewObjectsField = AccessTools.Field(typeof(PlayerCamera), "recipeItemPreviewObjects");

	public static void Initialize(ManualLogSource logger)
	{
		Logger = logger;
		RuntimePatchRegistry.PatchAllOnce("org.bepinex.plugins.qol.craftingpreview", typeof(CraftingPreviewPatcher), logger);
	}

	[HarmonyPatch(typeof(PlayerCamera), "RefreshCurrentlySelectedRecipe")]
	[HarmonyPostfix]
	private static void RefreshCurrentlySelectedRecipe_Postfix(PlayerCamera __instance)
	{
		//IL_0139: Unknown result type (might be due to invalid IL or missing references)
		//IL_0204: Unknown result type (might be due to invalid IL or missing references)
		//IL_0209: Unknown result type (might be due to invalid IL or missing references)
		//IL_020b: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)__instance == (Object)null || RecipeItemPreviewObjectsField == null || (Object)(object)__instance.craftingPanel == (Object)null || !(RecipeItemPreviewObjectsField.GetValue(__instance) is List<GameObject> previewObjects) || __instance.craftingPanel.transform.childCount <= 8 || __instance.selectedRecipe < 0 || __instance.selectedRecipe >= Recipes.recipes.Count)
		{
			return;
		}
		Recipe val = Recipes.recipes[__instance.selectedRecipe];
		List<Item> itemsForRecipeThorough = val.GetItemsForRecipeThorough();
		TextMeshProUGUI component = ((Component)__instance.craftingPanel.transform.GetChild(8)).GetComponent<TextMeshProUGUI>();
		RebuildMissingPreviewObjects(__instance, component, itemsForRecipeThorough, previewObjects);
		if (!(RecipeItemPreviewObjectsField.GetValue(__instance) is List<GameObject> { Count: not 0 } list))
		{
			return;
		}
		bool anyStacked = false;
		for (int i = 0; i < list.Count; i++)
		{
			GameObject val2 = list[i];
			if ((Object)(object)val2 == (Object)null)
			{
				continue;
			}
			RectTransform component2 = val2.GetComponent<RectTransform>();
			if (!((Object)(object)component2 == (Object)null))
			{
				float x = component2.anchoredPosition.x;
				if (x < -21f)
				{
					anyStacked = true;
					break;
				}
			}
		}
		int j = 0;
		for (int k = 0; k < list.Count; k++)
		{
			GameObject val3 = list[k];
			if (!((Object)(object)val3 == (Object)null))
			{
				for (; j < itemsForRecipeThorough.Count && (Object)(object)itemsForRecipeThorough[j] == (Object)null; j++)
				{
				}
				if (j >= itemsForRecipeThorough.Count)
				{
					break;
				}
				Item item = itemsForRecipeThorough[j++];
				RectTransform component3 = val3.GetComponent<RectTransform>();
				if (!((Object)(object)component3 == (Object)null))
				{
					Vector2 anchoredPosition = component3.anchoredPosition;
					bool isStacked = anchoredPosition.x < -21f;
					EnhancePreviewObject(val3, item, isStacked, anyStacked);
				}
			}
		}
	}

	private static void RebuildMissingPreviewObjects(PlayerCamera camera, TextMeshProUGUI ingredientText, List<Item> recipeItems, List<GameObject> previewObjects)
	{
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
		//IL_0102: Unknown result type (might be due to invalid IL or missing references)
		//IL_0108: Invalid comparison between Unknown and I4
		//IL_010a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0174: Unknown result type (might be due to invalid IL or missing references)
		//IL_0176: Unknown result type (might be due to invalid IL or missing references)
		//IL_018c: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_027c: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b0: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)camera == (Object)null || (Object)(object)ingredientText == (Object)null || recipeItems == null || previewObjects == null)
		{
			return;
		}
		int num = 0;
		for (int i = 0; i < recipeItems.Count; i++)
		{
			if ((Object)(object)recipeItems[i] != (Object)null)
			{
				num++;
			}
		}
		if (previewObjects.Count == num)
		{
			return;
		}
		foreach (GameObject previewObject in previewObjects)
		{
			if ((Object)(object)previewObject != (Object)null)
			{
				Object.Destroy((Object)(object)previewObject);
			}
		}
		previewObjects.Clear();
		((TMP_Text)ingredientText).ForceMeshUpdate(false, false);
		TMP_TextInfo textInfo = ((TMP_Text)ingredientText).textInfo;
		int num2 = 0;
		float num3 = 9999f;
		bool flag = false;
		Vector2 val4 = default(Vector2);
		for (int j = 0; j < textInfo.characterCount; j++)
		{
			TMP_CharacterInfo val = textInfo.characterInfo[j];
			if (!val.isVisible || (int)val.elementType != 1 || !IsIngredientMarker(val.spriteIndex))
			{
				continue;
			}
			if (num2 >= recipeItems.Count)
			{
				break;
			}
			Item val2 = recipeItems[num2];
			if ((Object)(object)val2 != (Object)null)
			{
				GameObject val3 = Utils.Create("Special/RecipeItemPreview", ((TMP_Text)ingredientText).transform);
				((Vector2)(ref val4))._002Ector(-20f, val.topLeft.y - 20f);
				if (num3 - val4.y < 32f)
				{
					val4.x += 28f * (flag ? 0f : (-1f));
					flag = !flag;
				}
				else
				{
					flag = false;
				}
				num3 = val4.y;
				RectTransform val5 = (((Object)(object)val3 != (Object)null) ? val3.GetComponent<RectTransform>() : null);
				Image val6 = (((Object)(object)val3 != (Object)null && val3.transform.childCount > 0) ? ((Component)val3.transform.GetChild(0)).GetComponent<Image>() : null);
				SpriteRenderer component = ((Component)val2).GetComponent<SpriteRenderer>();
				if ((Object)(object)val5 == (Object)null || (Object)(object)val6 == (Object)null || (Object)(object)component == (Object)null || (Object)(object)component.sprite == (Object)null)
				{
					if ((Object)(object)val3 != (Object)null)
					{
						Object.Destroy((Object)(object)val3);
					}
					num2++;
					continue;
				}
				val5.anchoredPosition = val4;
				val6.sprite = component.sprite;
				((Graphic)val6).rectTransform.sizeDelta = PlayerCamera.ImageSizeDelta((Texture)(object)val6.sprite.texture, 4f, 20f);
				previewObjects.Add(val3);
			}
			num2++;
		}
	}

	private static bool IsIngredientMarker(int spriteIndex)
	{
		return spriteIndex == 17 || spriteIndex == 23 || spriteIndex == 24;
	}

	private static void EnhancePreviewObject(GameObject previewObj, Item item, bool isStacked, bool anyStacked)
	{
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)previewObj == (Object)null || (Object)(object)item == (Object)null)
		{
			return;
		}
		if ((Object)(object)previewObj.transform.Find("ConditionBar") == (Object)null)
		{
			CreateConditionBar(previewObj.transform, item.condition, new Vector2(0f, -18f), new Vector2(24f, 4f));
		}
		if ((Object)(object)((Component)item).transform.parent != (Object)null)
		{
			Container component = ((Component)((Component)item).transform.parent).GetComponent<Container>();
			if ((Object)(object)component != (Object)null && (Object)(object)((Component)component).GetComponent<Body>() == (Object)null && (Object)(object)previewObj.transform.Find("ContainerPreview") == (Object)null)
			{
				CreateContainerSideDisplay(previewObj, component, isStacked, anyStacked);
			}
		}
	}

	private static void CreateContainerSideDisplay(GameObject mainPreviewObj, Container container, bool isStacked, bool anyStacked)
	{
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Expected O, but got Unknown
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_0101: Unknown result type (might be due to invalid IL or missing references)
		//IL_0118: Unknown result type (might be due to invalid IL or missing references)
		//IL_012c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0143: Unknown result type (might be due to invalid IL or missing references)
		//IL_0173: Unknown result type (might be due to invalid IL or missing references)
		//IL_017a: Expected O, but got Unknown
		//IL_0199: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0234: Unknown result type (might be due to invalid IL or missing references)
		//IL_0243: Unknown result type (might be due to invalid IL or missing references)
		Image val = (((Object)(object)mainPreviewObj != (Object)null) ? mainPreviewObj.GetComponent<Image>() : null);
		if (!((Object)(object)val == (Object)null) && !((Object)(object)container == (Object)null))
		{
			GameObject val2 = new GameObject("ContainerPreview", new Type[2]
			{
				typeof(RectTransform),
				typeof(Image)
			});
			val2.transform.SetParent(mainPreviewObj.transform, false);
			Sprite sprite = val.sprite;
			Image component = val2.GetComponent<Image>();
			component.sprite = sprite;
			((Graphic)component).color = new Color(0.7f, 0.7f, 0.7f, 1f);
			((Graphic)component).raycastTarget = false;
			float num = (isStacked ? (-20f) : (-30f));
			if (!anyStacked)
			{
				num += 28f;
			}
			RectTransform component2 = val2.GetComponent<RectTransform>();
			component2.anchorMin = new Vector2(0f, 0.5f);
			component2.anchorMax = new Vector2(0f, 0.5f);
			component2.pivot = new Vector2(1f, 0.5f);
			component2.anchoredPosition = new Vector2(num, 0f);
			component2.sizeDelta = new Vector2(24f, 24f);
			GameObject val3 = new GameObject("Icon", new Type[2]
			{
				typeof(RectTransform),
				typeof(Image)
			});
			val3.transform.SetParent(val2.transform, false);
			RectTransform component3 = val3.GetComponent<RectTransform>();
			component3.anchorMin = Vector2.zero;
			component3.anchorMax = Vector2.one;
			component3.sizeDelta = new Vector2(-8f, -8f);
			Image component4 = val3.GetComponent<Image>();
			SpriteRenderer val4 = default(SpriteRenderer);
			if (((Component)container).TryGetComponent<SpriteRenderer>(ref val4))
			{
				component4.sprite = val4.sprite;
			}
			component4.preserveAspect = true;
			((Graphic)component4).raycastTarget = false;
			float condition = 0f;
			Item val5 = default(Item);
			if (((Component)container).TryGetComponent<Item>(ref val5))
			{
				condition = val5.condition;
			}
			CreateConditionBar(val2.transform, condition, new Vector2(0f, -14f), new Vector2(20f, 3f));
		}
	}

	private static void CreateConditionBar(Transform parent, float condition, Vector2 anchoredPos, Vector2 size)
	{
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Expected O, but got Unknown
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_0102: Unknown result type (might be due to invalid IL or missing references)
		//IL_0108: Expected O, but got Unknown
		//IL_0125: Unknown result type (might be due to invalid IL or missing references)
		//IL_0138: Unknown result type (might be due to invalid IL or missing references)
		//IL_0145: Unknown result type (might be due to invalid IL or missing references)
		//IL_0152: Unknown result type (might be due to invalid IL or missing references)
		//IL_0168: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)parent == (Object)null))
		{
			condition = Mathf.Clamp01(condition);
			GameObject val = new GameObject("ConditionBar", new Type[2]
			{
				typeof(RectTransform),
				typeof(Image)
			});
			val.transform.SetParent(parent, false);
			RectTransform component = val.GetComponent<RectTransform>();
			component.anchorMin = new Vector2(0.5f, 0.5f);
			component.anchorMax = new Vector2(0.5f, 0.5f);
			component.pivot = new Vector2(0.5f, 0.5f);
			component.sizeDelta = size;
			component.anchoredPosition = anchoredPos;
			Image component2 = val.GetComponent<Image>();
			((Graphic)component2).color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
			((Graphic)component2).raycastTarget = false;
			GameObject val2 = new GameObject("Fill", new Type[2]
			{
				typeof(RectTransform),
				typeof(Image)
			});
			val2.transform.SetParent(val.transform, false);
			RectTransform component3 = val2.GetComponent<RectTransform>();
			component3.anchorMin = Vector2.zero;
			component3.anchorMax = new Vector2(condition, 1f);
			component3.offsetMin = Vector2.zero;
			component3.offsetMax = Vector2.zero;
			Image component4 = val2.GetComponent<Image>();
			((Graphic)component4).color = PlayerCamera.ConditionToColor(condition);
			((Graphic)component4).raycastTarget = false;
		}
	}

	[HarmonyPatch(typeof(PlayerCamera), "TryCraft")]
	[HarmonyPostfix]
	private static void TryCraft_Postfix(PlayerCamera __instance)
	{
		if ((Object)(object)__instance != (Object)null)
		{
			__instance.RefreshCurrentlySelectedRecipe();
		}
	}

	[HarmonyPatch(typeof(PlayerCamera), "RefreshCurrentlySelectedRecipe")]
	[HarmonyTranspiler]
	private static IEnumerable<CodeInstruction> RefreshCurrentlySelectedRecipe_Transpiler(IEnumerable<CodeInstruction> instructions)
	{
		List<CodeInstruction> list = new List<CodeInstruction>(instructions);
		for (int i = 0; i < list.Count - 1; i++)
		{
			if (!(list[i].opcode == OpCodes.Ldc_R4) || !(list[i].operand is float num) || !(Math.Abs(num - 20f) < 0.001f))
			{
				continue;
			}
			bool flag = false;
			for (int j = i + 1; j < Math.Min(i + 10, list.Count); j++)
			{
				if (list[j].opcode == OpCodes.Ldc_R4 && list[j].operand is float num2 && Math.Abs(num2 - -1f) < 0.001f)
				{
					flag = true;
					break;
				}
			}
			if (flag)
			{
				list[i].operand = 28f;
				break;
			}
		}
		return list;
	}
}
