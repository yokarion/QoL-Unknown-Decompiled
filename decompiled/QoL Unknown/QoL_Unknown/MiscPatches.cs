using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace QoL_Unknown;

internal class MiscPatches
{
	public static ManualLogSource Logger;

	private static readonly MethodInfo OpenCraftScreenMethod = AccessTools.Method(typeof(PlayerCamera), "OpenCraftScreen", (Type[])null, (Type[])null);

	private static readonly MethodInfo SeeRecipesWithItemMethod = AccessTools.Method(typeof(PlayerCamera), "SeeRecipesWithItem", (Type[])null, (Type[])null);

	private static bool hasLoggedCraftMethodMissing;

	private static Item pendingCraftDropFilterItem;

	private static int pendingCraftDropFilterFrame;

	private const float WoundItemYOffsetHeightFactor = 0.5f;

	private const float TooltipScreenMargin = 12f;

	private const float TooltipHeaderWidthPadding = 1f;

	private static readonly Vector2 TooltipCursorPivot = new Vector2(0.51f, 0.54f);

	private static readonly Vector3[] tooltipWorldCorners = (Vector3[])(object)new Vector3[4];

	private static readonly Vector3[] tooltipNameCorners = (Vector3[])(object)new Vector3[4];

	private static readonly Vector3[] tooltipDescCorners = (Vector3[])(object)new Vector3[4];

	private static readonly Dictionary<RectTransform, Vector2> baseTooltipPivots = new Dictionary<RectTransform, Vector2>();

	private static readonly HashSet<int> clickThroughTooltipRoots = new HashSet<int>();

	private static readonly Dictionary<Graphic, bool> baseTooltipRaycastTargets = new Dictionary<Graphic, bool>();

	private static readonly Dictionary<RectTransform, Vector2> baseWoundItemAnchoredPositions = new Dictionary<RectTransform, Vector2>();

	private static readonly Dictionary<TextMeshProUGUI, string> lastHeaderTexts = new Dictionary<TextMeshProUGUI, string>();

	private static readonly List<RaycastResult> cachedUiRaycastResults = new List<RaycastResult>(64);

	private static PointerEventData cachedPointerEventData;

	private static EventSystem cachedPointerEventSystem;

	private static int cachedUiRaycastFrame = -1;

	private static Vector2 cachedUiRaycastPosition;

	private static string lastTooltipName = string.Empty;

	private static string lastTooltipDesc = string.Empty;

	private static float lastTooltipUiScale = -1f;

	private static bool skippingFlashlightStrobe;

	private const string MegalovaniaCode = "2296";

	private static bool wasConsoleActive = false;

	private static int framesToClearConsole = 0;

	public static void Initialize(ManualLogSource logger)
	{
		Logger = logger;
		bool flag = RuntimePatchRegistry.PatchAllOnce("org.bepinex.plugins.qol.misc", typeof(MiscPatches), logger);
		if (!(flag | RuntimePatchRegistry.PatchAllOnce("org.bepinex.plugins.qol.misc.recipefilter", typeof(RecipeFilterPatch), logger)))
		{
		}
	}

	[HarmonyPatch(typeof(UIUtil), "GetEventSystemRaycastResults")]
	[HarmonyPrefix]
	private static bool UIUtil_GetEventSystemRaycastResults_Prefix(Vector2? pos, ref List<RaycastResult> __result)
	{
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Expected O, but got Unknown
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		EventSystem current = EventSystem.current;
		if ((Object)(object)current == (Object)null)
		{
			__result = new List<RaycastResult>();
			return false;
		}
		Vector2 val = (Vector2)(((_003F?)pos) ?? Vector2.op_Implicit(Input.mousePosition));
		if ((Object)(object)cachedPointerEventSystem != (Object)(object)current || cachedPointerEventData == null)
		{
			cachedPointerEventSystem = current;
			cachedPointerEventData = new PointerEventData(current);
			cachedUiRaycastFrame = -1;
		}
		if (cachedUiRaycastFrame != Time.frameCount || cachedUiRaycastPosition != val)
		{
			cachedUiRaycastFrame = Time.frameCount;
			cachedUiRaycastPosition = val;
			cachedUiRaycastResults.Clear();
			((AbstractEventData)cachedPointerEventData).Reset();
			cachedPointerEventData.position = val;
			current.RaycastAll(cachedPointerEventData, cachedUiRaycastResults);
		}
		__result = new List<RaycastResult>(cachedUiRaycastResults);
		return false;
	}

	[HarmonyPatch(typeof(WorldGeneration), "Update")]
	[HarmonyPostfix]
	private static void Update_Postfix(WorldGeneration __instance)
	{
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		if (__instance.worldExists && !((Object)(object)PlayerCamera.main == (Object)null) && !((Object)(object)PlayerCamera.main.body == (Object)null) && !MultiplayerCompat.IsActive && __instance.savePanel.activeSelf)
		{
			PlayerCamera.main.body.forceWalk = false;
			float num = (float)(0L - (long)__instance.halfHeight) + 3.1f;
			float y = ((Component)PlayerCamera.main.body).transform.position.y;
			if (y > num + 0.5f)
			{
				__instance.savePanel.SetActive(false);
			}
		}
	}

	[HarmonyPatch(typeof(PlayerCamera), "SetTimeScale", new Type[]
	{
		typeof(SpeedType),
		typeof(bool),
		typeof(bool)
	})]
	[HarmonyPrefix]
	private static bool PlayerCamera_SetTimeScale_Prefix(SpeedType speed)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Invalid comparison between Unknown and I4
		if (MultiplayerCompat.IsActive && (int)speed == 6)
		{
			Time.timeScale = 1f;
			return false;
		}
		return true;
	}

	[HarmonyPatch(typeof(PlayerCamera), "Update")]
	[HarmonyPostfix]
	private static void PlayerCamera_Update_Postfix(PlayerCamera __instance)
	{
		if ((Object)(object)__instance.tradeMenu != (Object)null && __instance.tradeMenu.activeSelf && Input.GetKeyDown((KeyCode)9))
		{
			__instance.ToggleTradeMenu();
		}
	}

	[HarmonyPatch(typeof(PlayerCamera), "HandleWhileDragging")]
	[HarmonyPostfix]
	private static void PlayerCamera_HandleWhileDragging_Postfix(PlayerCamera __instance)
	{
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Invalid comparison between Unknown and I4
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)__instance == (Object)null || (Object)(object)__instance.dragItem == (Object)null || (Object)(object)__instance.dragImage == (Object)null || !((Behaviour)__instance.dragImage).enabled || !EnhancedMenuController.NeedsScreenSpaceUiCompatibility())
		{
			return;
		}
		Image dragImage = __instance.dragImage;
		((Graphic)dragImage).raycastTarget = false;
		RectTransform rectTransform = ((Graphic)dragImage).rectTransform;
		if (!((Object)(object)rectTransform == (Object)null))
		{
			Transform parent = ((Transform)rectTransform).parent;
			RectTransform val = (RectTransform)(object)((parent is RectTransform) ? parent : null);
			Camera val2 = null;
			Canvas mainCanvas = __instance.mainCanvas;
			if ((Object)(object)mainCanvas != (Object)null && (int)mainCanvas.renderMode > 0)
			{
				val2 = mainCanvas.worldCamera;
			}
			Vector3 position = default(Vector3);
			if ((Object)(object)val != (Object)null && RectTransformUtility.ScreenPointToWorldPointInRectangle(val, Vector2.op_Implicit(Input.mousePosition), val2, ref position))
			{
				((Transform)rectTransform).position = position;
			}
			else
			{
				((Transform)rectTransform).position = Input.mousePosition;
			}
			((Transform)rectTransform).localScale = Vector3.one;
		}
	}

	[HarmonyPatch(typeof(PlayerCamera), "LateUpdate")]
	[HarmonyPostfix]
	private static void PlayerCamera_LateUpdate_Postfix(PlayerCamera __instance)
	{
		StabilizeSelectedItemOverlay(__instance);
	}

	[HarmonyPatch(typeof(GlobalDark), "get_uiScale")]
	[HarmonyPostfix]
	private static void GlobalDark_get_uiScale_Postfix(ref float __result)
	{
		if ((Object)(object)PlayerCamera.main != (Object)null && EnhancedMenuController.NeedsScreenSpaceUiCompatibility())
		{
			__result = Mathf.Max(0.01f, PlayerCamera.uiScale);
		}
	}

	[HarmonyPatch(typeof(GlobalDark), "HandleTooltip")]
	[HarmonyPrefix]
	private static bool GlobalDark_HandleTooltip_Prefix(GlobalDark __instance)
	{
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Invalid comparison between Unknown and I4
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_0118: Unknown result type (might be due to invalid IL or missing references)
		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
		//IL_057f: Unknown result type (might be due to invalid IL or missing references)
		//IL_018c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0191: Unknown result type (might be due to invalid IL or missing references)
		//IL_0197: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_02da: Unknown result type (might be due to invalid IL or missing references)
		//IL_039e: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0455: Unknown result type (might be due to invalid IL or missing references)
		//IL_0465: Unknown result type (might be due to invalid IL or missing references)
		//IL_0478: Unknown result type (might be due to invalid IL or missing references)
		//IL_0488: Unknown result type (might be due to invalid IL or missing references)
		//IL_04a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_04b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_04c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_04d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_04f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_050c: Unknown result type (might be due to invalid IL or missing references)
		//IL_050e: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_051c: Unknown result type (might be due to invalid IL or missing references)
		//IL_051e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0520: Unknown result type (might be due to invalid IL or missing references)
		//IL_0525: Unknown result type (might be due to invalid IL or missing references)
		//IL_052e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0533: Unknown result type (might be due to invalid IL or missing references)
		//IL_0535: Unknown result type (might be due to invalid IL or missing references)
		//IL_053a: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)__instance == (Object)null || (Object)(object)__instance.tooltipRect == (Object)null)
		{
			return true;
		}
		if (!EnhancedMenuController.NeedsScreenSpaceUiCompatibility())
		{
			RestoreTooltipVanillaState(__instance);
			return true;
		}
		float num = (((Object)(object)PlayerCamera.main != (Object)null) ? Mathf.Max(0.01f, PlayerCamera.uiScale) : ((float)Screen.height / 1080f));
		Transform parent = ((Transform)__instance.tooltipRect).parent;
		RectTransform val = (RectTransform)(object)((parent is RectTransform) ? parent : null);
		Camera val2 = null;
		if ((Object)(object)__instance.canvas != (Object)null && (int)__instance.canvas.renderMode > 0)
		{
			val2 = __instance.canvas.worldCamera;
		}
		EnsureTooltipDisplayIgnoresRaycasts(__instance);
		CacheTooltipPivot(__instance.tooltipRect);
		__instance.tooltipRect.pivot = TooltipCursorPivot;
		Vector3 position = default(Vector3);
		if ((Object)(object)val != (Object)null && RectTransformUtility.ScreenPointToWorldPointInRectangle(val, Vector2.op_Implicit(Input.mousePosition), val2, ref position))
		{
			((Transform)__instance.tooltipRect).position = position;
		}
		else
		{
			((Transform)__instance.tooltipRect).position = Input.mousePosition;
		}
		(string, string) uITooltip = UIUtil.GetUITooltip(UIUtil.GetEventSystemRaycastResults((Vector2?)null));
		if (!string.IsNullOrEmpty(uITooltip.Item1))
		{
			__instance.tooltipText = uITooltip;
		}
		((Component)__instance.tooltipRect).gameObject.SetActive(__instance.showTooltip);
		if (__instance.showTooltip)
		{
			((Transform)__instance.tooltipRect).localScale = Vector3.Lerp(((Transform)__instance.tooltipRect).localScale, Vector3.one * num, Time.unscaledDeltaTime * 24f);
			bool flag = !string.Equals(lastTooltipName, __instance.tooltipText.Item1, StringComparison.Ordinal) || !string.Equals(lastTooltipDesc, __instance.tooltipText.Item2, StringComparison.Ordinal) || Mathf.Abs(lastTooltipUiScale - num) > 0.001f;
			if (__instance.distortionAmount > 0f)
			{
				__instance.tooltipText.Item1 = __instance.DistortStr(__instance.tooltipText.Item1);
				__instance.tooltipText.Item2 = __instance.DistortStr(__instance.tooltipText.Item2);
				flag = true;
			}
			if (flag)
			{
				((TMP_Text)__instance.tooltipNameObj).text = __instance.tooltipText.Item1;
				((TMP_Text)__instance.tooltipDescObj).text = __instance.tooltipText.Item2;
				FixTooltipHeaderWidth(__instance.tooltipNameObj, __instance.hoverNameBackground);
				((TMP_Text)__instance.tooltipDescObj).ForceMeshUpdate(false, true);
				__instance.hoverDescriptionBackground.sizeDelta = new Vector2(Mathf.Min(((TMP_Text)__instance.tooltipDescObj).preferredWidth, ((TMP_Text)__instance.tooltipDescObj).rectTransform.sizeDelta.x), ((TMP_Text)__instance.tooltipDescObj).preferredHeight);
				lastTooltipName = __instance.tooltipText.Item1 ?? string.Empty;
				lastTooltipDesc = __instance.tooltipText.Item2 ?? string.Empty;
				lastTooltipUiScale = num;
			}
			float num2 = 12f * num;
			float num3 = (((Object)(object)__instance.tooltipDescObj != (Object)null) ? ((TMP_Text)__instance.tooltipDescObj).preferredWidth : 0f);
			float num4 = (((Object)(object)__instance.tooltipDescObj != (Object)null) ? ((TMP_Text)__instance.tooltipDescObj).preferredHeight : 0f);
			float num5;
			Rect rect;
			if (!((Object)(object)__instance.tooltipDescObj != (Object)null) || !((Object)(object)((TMP_Text)__instance.tooltipDescObj).rectTransform != (Object)null))
			{
				num5 = 0f;
			}
			else
			{
				rect = ((TMP_Text)__instance.tooltipDescObj).rectTransform.rect;
				num5 = ((Rect)(ref rect)).width;
			}
			float num6 = num5;
			float num7;
			if (!((Object)(object)__instance.tooltipDescObj != (Object)null) || !((Object)(object)((TMP_Text)__instance.tooltipDescObj).rectTransform != (Object)null))
			{
				num7 = 0f;
			}
			else
			{
				rect = ((TMP_Text)__instance.tooltipDescObj).rectTransform.rect;
				num7 = ((Rect)(ref rect)).height;
			}
			float num8 = num7;
			float num9 = Mathf.Max(0f, num3 - num6);
			float num10 = Mathf.Max(0f, num4 - num8);
			float num11 = (4f + num9 * 0.12f) * num;
			float num12 = (4f + num10 * 0.18f) * num;
			float num13 = num2;
			float num14 = num2 + num11;
			float num15 = num2;
			float num16 = num2 + num12;
			Vector2 val3 = default(Vector2);
			((Vector2)(ref val3))._002Ector(Mathf.Max(((Transform)__instance.hoverNameBackground).position.x + __instance.hoverNameBackground.sizeDelta.x * num, ((Transform)__instance.hoverDescriptionBackground).position.x + __instance.hoverDescriptionBackground.sizeDelta.x * num), Mathf.Min(((Transform)__instance.hoverDescriptionBackground).position.y, ((Transform)__instance.hoverNameBackground).position.y - __instance.hoverDescriptionBackground.sizeDelta.y * num));
			Vector2 val4 = default(Vector2);
			((Vector2)(ref val4))._002Ector(Mathf.Clamp(val3.x, num13, (float)Screen.width - num14), Mathf.Clamp(val3.y, num16, (float)Screen.height - num15));
			if (val3 != val4)
			{
				Vector2 val5 = val4 - val3;
				RectTransform tooltipRect = __instance.tooltipRect;
				((Transform)tooltipRect).position = ((Transform)tooltipRect).position + Vector2.op_Implicit(val5);
			}
			ClampTooltipContentToScreen(__instance.tooltipRect, __instance.hoverNameBackground, __instance.hoverDescriptionBackground, num13, num14, num15, num16);
		}
		else
		{
			((Transform)__instance.tooltipRect).localScale = new Vector3(0.1f, 0.1f, 1f);
		}
		return false;
	}

	[HarmonyPatch(typeof(GlobalDark), "HandleTooltip")]
	[HarmonyPostfix]
	private static void GlobalDark_HandleTooltip_Postfix(GlobalDark __instance)
	{
		ApplyTooltipFitting(__instance);
	}

	private static void ApplyTooltipFitting(GlobalDark globalDark)
	{
		if (!((Object)(object)globalDark == (Object)null) && globalDark.showTooltip && !((Object)(object)globalDark.tooltipRect == (Object)null) && ((Component)globalDark.tooltipRect).gameObject.activeInHierarchy)
		{
			FixTooltipHeaderWidth(globalDark.tooltipNameObj, globalDark.hoverNameBackground);
		}
	}

	private static void FixTooltipHeaderWidth(TextMeshProUGUI headerText, RectTransform headerBackground)
	{
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)headerText == (Object)null || (Object)(object)headerBackground == (Object)null)
		{
			return;
		}
		string text = ((TMP_Text)headerText).text ?? string.Empty;
		if (!lastHeaderTexts.TryGetValue(headerText, out var value) || !string.Equals(value, text, StringComparison.Ordinal))
		{
			lastHeaderTexts[headerText] = text;
			((TMP_Text)headerText).enableAutoSizing = false;
			((TMP_Text)headerText).enableWordWrapping = false;
			((TMP_Text)headerText).overflowMode = (TextOverflowModes)0;
			((TMP_Text)headerText).maxVisibleCharacters = int.MaxValue;
			((TMP_Text)headerText).maxVisibleWords = int.MaxValue;
			((TMP_Text)headerText).maxVisibleLines = int.MaxValue;
			float num = Mathf.Max(0f, ((TMP_Text)headerText).GetPreferredValues(text, 0f, 0f).x) + 1f;
			headerBackground.sizeDelta = new Vector2(num, headerBackground.sizeDelta.y);
			RectTransform rectTransform = ((TMP_Text)headerText).rectTransform;
			if ((Object)(object)rectTransform != (Object)null)
			{
				rectTransform.SetSizeWithCurrentAnchors((Axis)0, num);
			}
			((TMP_Text)headerText).ForceMeshUpdate(false, true);
		}
	}

	private static void StabilizeSelectedItemOverlay(PlayerCamera playerCamera)
	{
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Invalid comparison between Unknown and I4
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)playerCamera == (Object)null || (Object)(object)playerCamera.selectedItemOverlay == (Object)null || !EnhancedMenuController.NeedsScreenSpaceUiCompatibility())
		{
			return;
		}
		Image selectedItemOverlay = playerCamera.selectedItemOverlay;
		((Graphic)selectedItemOverlay).raycastTarget = false;
		if (((Graphic)selectedItemOverlay).color.a <= 0.001f)
		{
			return;
		}
		RectTransform rectTransform = ((Graphic)selectedItemOverlay).rectTransform;
		object obj;
		if (!((Object)(object)rectTransform != (Object)null))
		{
			obj = null;
		}
		else
		{
			Transform parent = ((Transform)rectTransform).parent;
			obj = ((parent is RectTransform) ? parent : null);
		}
		RectTransform val = (RectTransform)obj;
		if (!((Object)(object)rectTransform == (Object)null) && !((Object)(object)val == (Object)null))
		{
			Camera val2 = null;
			Canvas mainCanvas = playerCamera.mainCanvas;
			if ((Object)(object)mainCanvas != (Object)null && (int)mainCanvas.renderMode > 0)
			{
				val2 = mainCanvas.worldCamera;
			}
			Vector2 val3 = default(Vector2);
			if (RectTransformUtility.ScreenPointToLocalPointInRectangle(val, Vector2.op_Implicit(Input.mousePosition), val2, ref val3))
			{
				rectTransform.anchoredPosition = val3 + new Vector2(32f, -32f);
			}
		}
	}

	private static void EnsureTooltipDisplayIgnoresRaycasts(GlobalDark globalDark)
	{
		RectTransform val = globalDark?.tooltipRect;
		if ((Object)(object)val == (Object)null)
		{
			return;
		}
		int instanceID = ((Object)val).GetInstanceID();
		if (clickThroughTooltipRoots.Add(instanceID))
		{
			CacheAndSetTooltipRaycastTargets(val, raycastTarget: false);
			CacheAndSetTooltipRaycastTargets(globalDark.hoverNameBackground, raycastTarget: false);
			CacheAndSetTooltipRaycastTargets(globalDark.hoverDescriptionBackground, raycastTarget: false);
			if ((Object)(object)globalDark.tooltipNameObj != (Object)null)
			{
				CacheTooltipRaycastTarget((Graphic)(object)globalDark.tooltipNameObj);
				((Graphic)globalDark.tooltipNameObj).raycastTarget = false;
			}
			if ((Object)(object)globalDark.tooltipDescObj != (Object)null)
			{
				CacheTooltipRaycastTarget((Graphic)(object)globalDark.tooltipDescObj);
				((Graphic)globalDark.tooltipDescObj).raycastTarget = false;
			}
		}
	}

	private static void RestoreTooltipVanillaState(GlobalDark globalDark)
	{
		RectTransform val = globalDark?.tooltipRect;
		RestoreTooltipPivot(val);
		RestoreTooltipRaycastTargets();
		if ((Object)(object)val != (Object)null)
		{
			clickThroughTooltipRoots.Remove(((Object)val).GetInstanceID());
		}
	}

	private static void CacheAndSetTooltipRaycastTargets(RectTransform root, bool raycastTarget)
	{
		if ((Object)(object)root == (Object)null)
		{
			return;
		}
		Graphic[] componentsInChildren = ((Component)root).GetComponentsInChildren<Graphic>(true);
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			if ((Object)(object)componentsInChildren[i] != (Object)null)
			{
				CacheTooltipRaycastTarget(componentsInChildren[i]);
				componentsInChildren[i].raycastTarget = raycastTarget;
			}
		}
	}

	private static void CacheTooltipRaycastTarget(Graphic graphic)
	{
		if ((Object)(object)graphic != (Object)null && !baseTooltipRaycastTargets.ContainsKey(graphic))
		{
			baseTooltipRaycastTargets[graphic] = graphic.raycastTarget;
		}
	}

	private static void RestoreTooltipRaycastTargets()
	{
		if (baseTooltipRaycastTargets.Count == 0)
		{
			return;
		}
		List<Graphic> list = null;
		foreach (KeyValuePair<Graphic, bool> baseTooltipRaycastTarget in baseTooltipRaycastTargets)
		{
			Graphic key = baseTooltipRaycastTarget.Key;
			if ((Object)(object)key == (Object)null)
			{
				if (list == null)
				{
					list = new List<Graphic>();
				}
				list.Add(key);
			}
			else
			{
				key.raycastTarget = baseTooltipRaycastTarget.Value;
			}
		}
		if (list != null)
		{
			for (int i = 0; i < list.Count; i++)
			{
				baseTooltipRaycastTargets.Remove(list[i]);
			}
		}
	}

	private static void CacheTooltipPivot(RectTransform tooltipRect)
	{
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)tooltipRect != (Object)null && !baseTooltipPivots.ContainsKey(tooltipRect))
		{
			baseTooltipPivots[tooltipRect] = tooltipRect.pivot;
		}
	}

	private static void RestoreTooltipPivot(RectTransform tooltipRect)
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)tooltipRect != (Object)null && baseTooltipPivots.TryGetValue(tooltipRect, out var value))
		{
			tooltipRect.pivot = value;
		}
	}

	private static void ClampTooltipContentToScreen(RectTransform tooltipRect, RectTransform nameRect, RectTransform descRect, float leftPadding, float rightPadding, float topPadding, float bottomPadding)
	{
		//IL_02c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_02da: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)tooltipRect == (Object)null)
		{
			return;
		}
		float num = Mathf.Max(0f, leftPadding);
		float num2 = Mathf.Max(num, (float)Screen.width - Mathf.Max(0f, rightPadding));
		float num3 = Mathf.Max(0f, bottomPadding);
		float num4 = Mathf.Max(num3, (float)Screen.height - Mathf.Max(0f, topPadding));
		for (int i = 0; i < 4; i++)
		{
			tooltipRect.GetWorldCorners(tooltipWorldCorners);
			float num5 = tooltipWorldCorners[0].x;
			float num6 = tooltipWorldCorners[2].x;
			float num7 = tooltipWorldCorners[0].y;
			float num8 = tooltipWorldCorners[2].y;
			if ((Object)(object)nameRect != (Object)null)
			{
				nameRect.GetWorldCorners(tooltipNameCorners);
				num5 = Mathf.Min(num5, tooltipNameCorners[0].x);
				num6 = Mathf.Max(num6, tooltipNameCorners[2].x);
				num7 = Mathf.Min(num7, tooltipNameCorners[0].y);
				num8 = Mathf.Max(num8, tooltipNameCorners[2].y);
			}
			if ((Object)(object)descRect != (Object)null)
			{
				descRect.GetWorldCorners(tooltipDescCorners);
				num5 = Mathf.Min(num5, tooltipDescCorners[0].x);
				num6 = Mathf.Max(num6, tooltipDescCorners[2].x);
				num7 = Mathf.Min(num7, tooltipDescCorners[0].y);
				num8 = Mathf.Max(num8, tooltipDescCorners[2].y);
			}
			float num9 = num6 - num5;
			float num10 = num8 - num7;
			float num11 = 0f;
			float num12 = 0f;
			float num13 = Mathf.Max(0.01f, num2 - num);
			if (num9 >= num13)
			{
				num11 = (num + num2) * 0.5f - (num5 + num6) * 0.5f;
			}
			else if (num6 > num2)
			{
				num11 = num2 - num6;
			}
			else if (num5 < num)
			{
				num11 = num - num5;
			}
			float num14 = Mathf.Max(0.01f, num4 - num3);
			if (num10 >= num14)
			{
				num12 = (num3 + num4) * 0.5f - (num7 + num8) * 0.5f;
			}
			else if (num8 > num4)
			{
				num12 = num4 - num8;
			}
			else if (num7 < num3)
			{
				num12 = num3 - num7;
			}
			if (Mathf.Abs(num11) <= 0.01f && Mathf.Abs(num12) <= 0.01f)
			{
				break;
			}
			((Transform)tooltipRect).position = ((Transform)tooltipRect).position + new Vector3(num11, num12, 0f);
		}
	}

	[HarmonyPatch(typeof(WoundView), "Update")]
	[HarmonyPostfix]
	private static void WoundView_Update_Postfix(WoundView __instance)
	{
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_0138: Unknown result type (might be due to invalid IL or missing references)
		//IL_0140: Unknown result type (might be due to invalid IL or missing references)
		//IL_0145: Unknown result type (might be due to invalid IL or missing references)
		//IL_011f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0124: Unknown result type (might be due to invalid IL or missing references)
		//IL_012d: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)__instance == (Object)null || (Object)(object)((Component)__instance).transform == (Object)null)
		{
			return;
		}
		if (!EnhancedMenuController.IsCustomUiScaleActive())
		{
			RestoreWoundItemPositions();
			return;
		}
		float savedUiScale = EnhancedMenuController.GetSavedUiScale();
		float num = Screen.height;
		Transform transform = ((Component)__instance).transform;
		RectTransform val = (RectTransform)(object)((transform is RectTransform) ? transform : null);
		if ((Object)(object)val != (Object)null)
		{
			Rect rect = val.rect;
			if (((Rect)(ref rect)).height > 0.01f)
			{
				rect = val.rect;
				num = ((Rect)(ref rect)).height;
			}
		}
		float num2 = (1f - savedUiScale) * num * 0.5f;
		int childCount = ((Component)__instance).transform.childCount;
		for (int i = 0; i < childCount; i++)
		{
			Transform child = ((Component)__instance).transform.GetChild(i);
			if ((Object)(object)child == (Object)null || !IsWoundItemTransform(child))
			{
				continue;
			}
			RectTransform val2 = (RectTransform)(object)((child is RectTransform) ? child : null);
			if (!((Object)(object)val2 == (Object)null))
			{
				if (!baseWoundItemAnchoredPositions.TryGetValue(val2, out var value))
				{
					value = val2.anchoredPosition;
					baseWoundItemAnchoredPositions[val2] = value;
				}
				val2.anchoredPosition = value + new Vector2(0f, num2);
			}
		}
		if (baseWoundItemAnchoredPositions.Count <= 64)
		{
			return;
		}
		List<RectTransform> list = null;
		foreach (KeyValuePair<RectTransform, Vector2> baseWoundItemAnchoredPosition in baseWoundItemAnchoredPositions)
		{
			if ((Object)(object)baseWoundItemAnchoredPosition.Key == (Object)null)
			{
				if (list == null)
				{
					list = new List<RectTransform>();
				}
				list.Add(baseWoundItemAnchoredPosition.Key);
			}
		}
		if (list != null)
		{
			for (int j = 0; j < list.Count; j++)
			{
				baseWoundItemAnchoredPositions.Remove(list[j]);
			}
		}
	}

	private static bool IsWoundItemTransform(Transform child)
	{
		if ((Object)(object)child == (Object)null)
		{
			return false;
		}
		string name = ((Object)child).name;
		return string.Equals(name, "WoundItem", StringComparison.Ordinal) || string.Equals(name, "WoundItem (1)", StringComparison.Ordinal) || string.Equals(name, "WoundItem (2)", StringComparison.Ordinal) || string.Equals(name, "WoundItem (3)", StringComparison.Ordinal) || string.Equals(name, "WoundItem (4)", StringComparison.Ordinal) || string.Equals(name, "WoundItem (5)", StringComparison.Ordinal);
	}

	private static void RestoreWoundItemPositions()
	{
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		if (baseWoundItemAnchoredPositions.Count == 0)
		{
			return;
		}
		List<RectTransform> list = null;
		foreach (KeyValuePair<RectTransform, Vector2> baseWoundItemAnchoredPosition in baseWoundItemAnchoredPositions)
		{
			RectTransform key = baseWoundItemAnchoredPosition.Key;
			if ((Object)(object)key == (Object)null)
			{
				if (list == null)
				{
					list = new List<RectTransform>();
				}
				list.Add(key);
			}
			else
			{
				key.anchoredPosition = baseWoundItemAnchoredPosition.Value;
			}
		}
		if (list != null)
		{
			for (int i = 0; i < list.Count; i++)
			{
				baseWoundItemAnchoredPositions.Remove(list[i]);
			}
		}
	}

	[HarmonyPatch(typeof(PlayerCamera), "TryPerformSpecialUIAction")]
	[HarmonyPrefix]
	private static bool TryPerformSpecialUIAction_Prefix(PlayerCamera __instance, RaycastResult hit, ref bool __result)
	{
		if ((Object)(object)__instance == (Object)null || (Object)(object)((RaycastResult)(ref hit)).gameObject == (Object)null || (Object)(object)__instance.dragItem == (Object)null || (Object)(object)__instance.craftButton == (Object)null)
		{
			return true;
		}
		if ((Object)(object)((RaycastResult)(ref hit)).gameObject == (Object)(object)__instance.craftButton)
		{
			return true;
		}
		Transform transform = __instance.craftButton.transform;
		if ((Object)(object)transform == (Object)null || !((RaycastResult)(ref hit)).gameObject.transform.IsChildOf(transform))
		{
			return true;
		}
		if (OpenCraftScreenMethod == null || SeeRecipesWithItemMethod == null)
		{
			if (!hasLoggedCraftMethodMissing)
			{
				ManualLogSource logger = Logger;
				if (logger != null)
				{
					logger.LogWarning((object)"Craft drop fallback disabled: OpenCraftScreen/SeeRecipesWithItem method not found.");
				}
				hasLoggedCraftMethodMissing = true;
			}
			return true;
		}
		Item dragItem = __instance.dragItem;
		if ((Object)(object)dragItem == (Object)null)
		{
			return true;
		}
		if (!__instance.craftingPanel.activeSelf)
		{
			OpenCraftScreenMethod.Invoke(__instance, null);
			pendingCraftDropFilterItem = dragItem;
			pendingCraftDropFilterFrame = Time.frameCount;
		}
		SeeRecipesWithItemMethod.Invoke(__instance, new object[1] { dragItem });
		__result = true;
		return false;
	}

	[HarmonyPatch(typeof(PlayerCamera), "SeeRecipesWithItem")]
	[HarmonyPostfix]
	private static void SeeRecipesWithItem_Postfix(PlayerCamera __instance, Item item)
	{
		if (!((Object)(object)__instance == (Object)null) && !((Object)(object)item == (Object)null))
		{
			RecipeFilterPatch.RefreshRecipeListForItem(__instance, item);
		}
	}

	[HarmonyPatch(typeof(PlayerCamera), "HandleReleaseDragging")]
	[HarmonyPrefix]
	[HarmonyPriority(800)]
	private static bool HandleReleaseDragging_CraftDrop_Prefix(PlayerCamera __instance, List<RaycastResult> uiCasts)
	{
		if ((Object)(object)__instance == (Object)null || (Object)(object)__instance.dragItem == (Object)null || (Object)(object)__instance.craftButton == (Object)null || uiCasts == null)
		{
			return true;
		}
		if (!IsCraftButtonHit(__instance, uiCasts))
		{
			return true;
		}
		if ((Object)(object)__instance.dragImage != (Object)null)
		{
			((Behaviour)__instance.dragImage).enabled = false;
		}
		Item dragItem = __instance.dragItem;
		if ((Object)(object)__instance.body != (Object)null && !__instance.body.DoPickupCheck(dragItem, false))
		{
			__instance.dragItem = null;
			return false;
		}
		if (OpenCraftScreenMethod == null || SeeRecipesWithItemMethod == null)
		{
			if (!hasLoggedCraftMethodMissing)
			{
				ManualLogSource logger = Logger;
				if (logger != null)
				{
					logger.LogWarning((object)"Craft drop fallback disabled: OpenCraftScreen/SeeRecipesWithItem method not found.");
				}
				hasLoggedCraftMethodMissing = true;
			}
			return true;
		}
		bool flag = (Object)(object)__instance.craftingPanel != (Object)null && __instance.craftingPanel.activeSelf;
		if (!flag)
		{
			OpenCraftScreenMethod.Invoke(__instance, null);
		}
		SeeRecipesWithItemMethod.Invoke(__instance, new object[1] { dragItem });
		if ((Object)(object)__instance.currentContainer != (Object)null)
		{
			__instance.RepopulateContainer();
		}
		__instance.UpdateWearables();
		if (flag)
		{
			__instance.PlayUISound((UISoundType)0, 1f);
		}
		__instance.dragItem = null;
		return false;
	}

	private static bool IsCraftButtonHit(PlayerCamera camera, List<RaycastResult> uiCasts)
	{
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		Transform transform = camera.craftButton.transform;
		if ((Object)(object)transform == (Object)null)
		{
			return false;
		}
		foreach (RaycastResult uiCast in uiCasts)
		{
			RaycastResult current = uiCast;
			if ((Object)(object)((RaycastResult)(ref current)).gameObject == (Object)null || (!((Object)(object)((RaycastResult)(ref current)).gameObject == (Object)(object)camera.craftButton) && !((RaycastResult)(ref current)).gameObject.transform.IsChildOf(transform)))
			{
				continue;
			}
			return true;
		}
		return false;
	}

	[HarmonyPatch(typeof(PlayerCamera), "LateUpdate")]
	[HarmonyPostfix]
	private static void ApplyPendingCraftDropFilter_Postfix(PlayerCamera __instance)
	{
		if (!((Object)(object)pendingCraftDropFilterItem == (Object)null) && !((Object)(object)__instance == (Object)null) && !(SeeRecipesWithItemMethod == null) && Time.frameCount > pendingCraftDropFilterFrame)
		{
			Item val = pendingCraftDropFilterItem;
			pendingCraftDropFilterItem = null;
			if ((Object)(object)__instance.craftingPanel != (Object)null && __instance.craftingPanel.activeSelf)
			{
				SeeRecipesWithItemMethod.Invoke(__instance, new object[1] { val });
			}
		}
	}

	[HarmonyPatch(typeof(ConsoleScript), "HandleConsoleActiveState")]
	[HarmonyPostfix]
	private static void HandleConsoleActiveState_Postfix(ConsoleScript __instance)
	{
		if (__instance.active && !wasConsoleActive)
		{
			framesToClearConsole = 2;
		}
		if (framesToClearConsole > 0 && __instance.active)
		{
			framesToClearConsole--;
			if (framesToClearConsole == 0)
			{
				__instance.input.text = "";
				__instance.input.caretPosition = 0;
				((Selectable)__instance.input).Select();
				__instance.input.ActivateInputField();
			}
		}
		wasConsoleActive = __instance.active;
	}

	[HarmonyPatch(typeof(CustomItemBehaviour), "Update")]
	[HarmonyPostfix]
	private static void CustomItemBehaviour_Update_Postfix(CustomItemBehaviour __instance)
	{
		if ((Object)(object)__instance == (Object)null || __instance.state != 3 || !AntiGlarePatcher.GetAntiEpilepsyEnabled())
		{
			return;
		}
		Item component = ((Component)__instance).GetComponent<Item>();
		if ((Object)(object)component == (Object)null || component.id != "flashlight")
		{
			return;
		}
		Transform val = ((((Component)__instance).transform.childCount > 0) ? ((Component)__instance).transform.GetChild(0) : null);
		if (!((Object)(object)val == (Object)null))
		{
			Light2D component2 = ((Component)val).GetComponent<Light2D>();
			if ((Object)(object)component2 != (Object)null)
			{
				component2.intensity = 0.5f;
			}
		}
	}

	[HarmonyPatch(typeof(Body), "UseItem")]
	[HarmonyPostfix]
	private static void Body_UseItem_Postfix(Body __instance, Item item)
	{
		if (skippingFlashlightStrobe || !AntiGlarePatcher.GetAntiEpilepsyEnabled() || (Object)(object)__instance == (Object)null || (Object)(object)item == (Object)null || item.id != "flashlight")
		{
			return;
		}
		CustomItemBehaviour component = ((Component)item).GetComponent<CustomItemBehaviour>();
		if ((Object)(object)component == (Object)null || component.state != 3)
		{
			return;
		}
		try
		{
			skippingFlashlightStrobe = true;
			__instance.UseItem(item);
		}
		finally
		{
			skippingFlashlightStrobe = false;
		}
	}

	[HarmonyPatch(typeof(KeypadMinigame), "CheckForMegalovania")]
	[HarmonyPrefix]
	private static bool KeypadMinigame_CheckForMegalovania_Prefix(KeypadMinigame __instance)
	{
		if (__instance == null || string.IsNullOrEmpty(__instance.current) || string.IsNullOrEmpty(__instance.match))
		{
			return true;
		}
		string current = __instance.current;
		int num = 0;
		bool flag = false;
		while (num <= current.Length - "2296".Length)
		{
			int num2 = current.IndexOf("2296", num, StringComparison.Ordinal);
			if (num2 < 0)
			{
				break;
			}
			flag = true;
			if (!CodeContainsMegalovaniaAt(__instance.match, num2))
			{
				return true;
			}
			num = num2 + 1;
		}
		return !flag;
	}

	private static bool CodeContainsMegalovaniaAt(string code, int index)
	{
		return !string.IsNullOrEmpty(code) && index >= 0 && index <= code.Length - "2296".Length && string.Compare(code, index, "2296", 0, "2296".Length, StringComparison.Ordinal) == 0;
	}

	public static bool ContainsIgnoreCase(string source, string value)
	{
		if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(value))
		{
			return false;
		}
		return source.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
	}

	public static bool RecipeMatchesFilter(Recipe recipe, string filter)
	{
		if (string.IsNullOrEmpty(filter))
		{
			return true;
		}
		if (ContainsIgnoreCase(recipe.simpleName, filter))
		{
			return true;
		}
		foreach (RecipeItem item in recipe.items)
		{
			if (item.specific && !string.IsNullOrEmpty(item.specificId))
			{
				string source = (item.isLiquid ? Locale.GetOther(item.specificId) : Locale.GetItem(item.specificId));
				if (ContainsIgnoreCase(source, filter))
				{
					return true;
				}
				if (ContainsIgnoreCase(item.specificId, filter))
				{
					return true;
				}
			}
			if (item.quality != null)
			{
				if (!string.IsNullOrEmpty(item.quality.id) && ContainsIgnoreCase(item.quality.id, filter))
				{
					return true;
				}
				if (!string.IsNullOrEmpty(item.quality.LocaleName) && ContainsIgnoreCase(item.quality.LocaleName, filter))
				{
					return true;
				}
			}
		}
		return false;
	}
}
