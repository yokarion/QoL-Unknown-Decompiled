using System;
using System.Collections.Generic;
using System.Linq;
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

	public static void Initialize(ManualLogSource logger)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Expected O, but got Unknown
		Logger = logger;
		Harmony val = new Harmony("org.bepinex.plugins.qol.craftingpreview");
		val.PatchAll(typeof(CraftingPreviewPatcher));
	}

	[HarmonyPatch(typeof(PlayerCamera), "RefreshCurrentlySelectedRecipe")]
	[HarmonyPostfix]
	private static void RefreshCurrentlySelectedRecipe_Postfix(PlayerCamera __instance)
	{
		//IL_011d: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a8: Unknown result type (might be due to invalid IL or missing references)
		FieldInfo fieldInfo = AccessTools.Field(typeof(PlayerCamera), "recipeItemPreviewObjects");
		if (fieldInfo == null)
		{
			return;
		}
		List<GameObject> previewObjects = fieldInfo.GetValue(__instance) as List<GameObject>;
		if (__instance.selectedRecipe < 0 || __instance.selectedRecipe >= Recipes.recipes.Count)
		{
			return;
		}
		Recipe val = Recipes.recipes[__instance.selectedRecipe];
		List<Item> itemsForRecipeThorough = val.GetItemsForRecipeThorough();
		TextMeshProUGUI component = ((Component)__instance.craftingPanel.transform.GetChild(8)).GetComponent<TextMeshProUGUI>();
		RebuildMissingPreviewObjects(__instance, component, itemsForRecipeThorough, previewObjects);
		if (!(fieldInfo.GetValue(__instance) is List<GameObject> { Count: not 0 } list))
		{
			return;
		}
		List<Item> list2 = itemsForRecipeThorough.Where((Item val4) => (Object)(object)val4 != (Object)null).ToList();
		bool anyStacked = false;
		for (int num = 0; num < list.Count; num++)
		{
			GameObject val2 = list[num];
			if (!((Object)(object)val2 == (Object)null))
			{
				RectTransform component2 = val2.GetComponent<RectTransform>();
				float x = component2.anchoredPosition.x;
				if (x < -21f)
				{
					anyStacked = true;
					break;
				}
			}
		}
		for (int num2 = 0; num2 < list.Count; num2++)
		{
			GameObject val3 = list[num2];
			if (!((Object)(object)val3 == (Object)null))
			{
				if (num2 >= list2.Count)
				{
					break;
				}
				Item item = list2[num2];
				RectTransform component3 = val3.GetComponent<RectTransform>();
				Vector2 anchoredPosition = component3.anchoredPosition;
				bool isStacked = anchoredPosition.x < -21f;
				EnhancePreviewObject(val3, item, isStacked, anyStacked);
			}
		}
	}

	private static void RebuildMissingPreviewObjects(PlayerCamera camera, TextMeshProUGUI ingredientText, List<Item> recipeItems, List<GameObject> previewObjects)
	{
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Invalid comparison between Unknown and I4
		//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0163: Unknown result type (might be due to invalid IL or missing references)
		//IL_0165: Unknown result type (might be due to invalid IL or missing references)
		//IL_017b: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_021d: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)camera == (Object)null || (Object)(object)ingredientText == (Object)null || recipeItems == null || previewObjects == null)
		{
			return;
		}
		int num = recipeItems.Count((Item item) => (Object)(object)item != (Object)null);
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
		for (int num4 = 0; num4 < textInfo.characterCount; num4++)
		{
			TMP_CharacterInfo val = textInfo.characterInfo[num4];
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
				val3.GetComponent<RectTransform>().anchoredPosition = val4;
				Image component = ((Component)val3.transform.GetChild(0)).GetComponent<Image>();
				component.sprite = ((Component)val2).GetComponent<SpriteRenderer>().sprite;
				((Graphic)component).rectTransform.sizeDelta = PlayerCamera.ImageSizeDelta((Texture)(object)component.sprite.texture, 4f, 20f);
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
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
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
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Expected O, but got Unknown
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0112: Unknown result type (might be due to invalid IL or missing references)
		//IL_0142: Unknown result type (might be due to invalid IL or missing references)
		//IL_0149: Expected O, but got Unknown
		//IL_0168: Unknown result type (might be due to invalid IL or missing references)
		//IL_0175: Unknown result type (might be due to invalid IL or missing references)
		//IL_018c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0203: Unknown result type (might be due to invalid IL or missing references)
		//IL_0212: Unknown result type (might be due to invalid IL or missing references)
		GameObject val = new GameObject("ContainerPreview", new Type[2]
		{
			typeof(RectTransform),
			typeof(Image)
		});
		val.transform.SetParent(mainPreviewObj.transform, false);
		Sprite sprite = mainPreviewObj.GetComponent<Image>().sprite;
		Image component = val.GetComponent<Image>();
		component.sprite = sprite;
		((Graphic)component).color = new Color(0.7f, 0.7f, 0.7f, 1f);
		((Graphic)component).raycastTarget = false;
		float num = (isStacked ? (-20f) : (-30f));
		if (!anyStacked)
		{
			num += 28f;
		}
		RectTransform component2 = val.GetComponent<RectTransform>();
		component2.anchorMin = new Vector2(0f, 0.5f);
		component2.anchorMax = new Vector2(0f, 0.5f);
		component2.pivot = new Vector2(1f, 0.5f);
		component2.anchoredPosition = new Vector2(num, 0f);
		component2.sizeDelta = new Vector2(24f, 24f);
		GameObject val2 = new GameObject("Icon", new Type[2]
		{
			typeof(RectTransform),
			typeof(Image)
		});
		val2.transform.SetParent(val.transform, false);
		RectTransform component3 = val2.GetComponent<RectTransform>();
		component3.anchorMin = Vector2.zero;
		component3.anchorMax = Vector2.one;
		component3.sizeDelta = new Vector2(-8f, -8f);
		Image component4 = val2.GetComponent<Image>();
		SpriteRenderer val3 = default(SpriteRenderer);
		if (((Component)container).TryGetComponent<SpriteRenderer>(ref val3))
		{
			component4.sprite = val3.sprite;
		}
		component4.preserveAspect = true;
		((Graphic)component4).raycastTarget = false;
		float condition = 0f;
		Item val4 = default(Item);
		if (((Component)container).TryGetComponent<Item>(ref val4))
		{
			condition = val4.condition;
		}
		CreateConditionBar(val.transform, condition, new Vector2(0f, -14f), new Vector2(20f, 3f));
	}

	private static void CreateConditionBar(Transform parent, float condition, Vector2 anchoredPos, Vector2 size)
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Expected O, but got Unknown
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Expected O, but got Unknown
		//IL_010a: Unknown result type (might be due to invalid IL or missing references)
		//IL_011d: Unknown result type (might be due to invalid IL or missing references)
		//IL_012a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0137: Unknown result type (might be due to invalid IL or missing references)
		//IL_014d: Unknown result type (might be due to invalid IL or missing references)
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

	[HarmonyPatch(typeof(PlayerCamera), "TryCraft")]
	[HarmonyPostfix]
	private static void TryCraft_Postfix(PlayerCamera __instance)
	{
		__instance.RefreshCurrentlySelectedRecipe();
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
