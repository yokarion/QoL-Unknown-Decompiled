using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using BepInEx.Logging;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace QoL_Unknown;

internal static class MainMenuSavePanel
{
	private sealed class PanelAnimation : MonoBehaviour
	{
		public float duration = 0.25f;

		public float progress;

		public bool closing;

		private void Update()
		{
			//IL_0053: Unknown result type (might be due to invalid IL or missing references)
			//IL_0059: Unknown result type (might be due to invalid IL or missing references)
			//IL_0094: Unknown result type (might be due to invalid IL or missing references)
			progress += Time.unscaledDeltaTime / Mathf.Max(0.01f, duration);
			float num = Mathf.Clamp01(progress);
			float num2 = (closing ? (1f - Utils.OutCubic(num)) : Utils.OutCubic(num));
			((Component)this).transform.localScale = Vector3.one * num2;
			if (!(num < 1f))
			{
				if (closing)
				{
					overlayObj.SetActive(false);
				}
				else
				{
					((Component)this).transform.localScale = Vector3.one;
				}
			}
		}
	}

	[Serializable]
	[CompilerGenerated]
	private sealed class _003C_003Ec
	{
		public static readonly _003C_003Ec _003C_003E9 = new _003C_003Ec();

		public static Action<int> _003C_003E9__20_0;

		public static Action<int> _003C_003E9__20_1;

		public static UnityAction _003C_003E9__27_0;

		internal void _003CBuild_003Eb__20_0(int slot)
		{
			SaveSaveMenuPatcher.DoLoad(SaveSlotRegistry.FileName(slot), slot);
		}

		internal void _003CBuild_003Eb__20_1(int slot)
		{
			SaveSaveMenuPatcher.DoLoad("autosave.sv", slot);
		}

		internal void _003CBuildContinueButton_003Eb__27_0()
		{
			if ((Object)(object)PreRunScript.instance != (Object)null)
			{
				PreRunScript.instance.LoadRun();
			}
		}
	}

	internal static ManualLogSource Logger;

	private const float PANEL_WIDTH = 640f;

	private const float PANEL_HEIGHT = 520f;

	private const float CLOSE_SIZE = 36f;

	private const float OPEN_DURATION = 0.25f;

	private const float CLOSE_DURATION = 0.18f;

	private const float BORDER_THICKNESS = 2f;

	private const float PANEL_GAP = 12f;

	private const float CONTINUE_INSET = 8f;

	private const float CONTINUE_HEIGHT_FRACTION = 0.2f;

	private const int BODY_PADDING = 14;

	private static readonly Color PANEL_COLOR = new Color(0f, 0f, 0f, 0.98f);

	private static readonly Color BORDER_COLOR = Color.white;

	private static GameObject overlayObj;

	private static GameObject panelObj;

	private static SaveSlotListView slotList;

	private static RectTransform continueButton;

	public static void Show()
	{
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)panelObj == (Object)null) || Build())
		{
			slotList.Refresh();
			SaveSystem.loadedRun = false;
			overlayObj.SetActive(true);
			PositionPanel();
			panelObj.transform.localScale = Vector3.zero;
			PanelAnimation panelAnimation = panelObj.GetComponent<PanelAnimation>();
			if ((Object)(object)panelAnimation == (Object)null)
			{
				panelAnimation = panelObj.AddComponent<PanelAnimation>();
			}
			panelAnimation.duration = 0.25f;
			panelAnimation.closing = false;
			panelAnimation.progress = 0f;
		}
	}

	public static void Hide()
	{
		if (!((Object)(object)panelObj == (Object)null) && !((Object)(object)overlayObj == (Object)null) && overlayObj.activeSelf)
		{
			PanelAnimation component = panelObj.GetComponent<PanelAnimation>();
			if ((Object)(object)component == (Object)null)
			{
				overlayObj.SetActive(false);
				return;
			}
			component.duration = 0.18f;
			component.closing = true;
			component.progress = 0f;
		}
	}

	private static void PositionPanel()
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Expected O, but got Unknown
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Expected O, but got Unknown
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		//IL_011a: Unknown result type (might be due to invalid IL or missing references)
		//IL_012b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0133: Unknown result type (might be due to invalid IL or missing references)
		//IL_0142: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)continueButton == (Object)null))
		{
			RectTransform val = (RectTransform)panelObj.transform;
			RectTransform val2 = (RectTransform)overlayObj.transform;
			RectTransform obj = continueButton;
			Rect rect = continueButton.rect;
			Vector2 val3 = Vector2.op_Implicit(((Transform)val2).InverseTransformPoint(((Transform)obj).TransformPoint(Vector2.op_Implicit(((Rect)(ref rect)).center))));
			RectTransform obj2 = continueButton;
			rect = continueButton.rect;
			float x = ((Rect)(ref rect)).center.x;
			rect = continueButton.rect;
			Vector2 val4 = Vector2.op_Implicit(((Transform)val2).InverseTransformPoint(((Transform)obj2).TransformPoint(Vector2.op_Implicit(new Vector2(x, ((Rect)(ref rect)).yMax)))));
			rect = val.rect;
			float num = ((Rect)(ref rect)).width * 0.5f;
			rect = val.rect;
			float num2 = ((Rect)(ref rect)).height * 0.5f;
			rect = val2.rect;
			Vector2 val5 = ((Rect)(ref rect)).size * 0.5f;
			val.anchoredPosition = new Vector2(Mathf.Clamp(val3.x, num - val5.x, val5.x - num), Mathf.Clamp(val4.y + 12f + num2, num2 - val5.y, val5.y - num2));
		}
	}

	private static bool Build()
	{
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Expected O, but got Unknown
		//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0102: Expected O, but got Unknown
		//IL_0104: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_011e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0133: Unknown result type (might be due to invalid IL or missing references)
		//IL_017d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0184: Expected O, but got Unknown
		//IL_01a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01aa: Expected O, but got Unknown
		//IL_01ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0210: Unknown result type (might be due to invalid IL or missing references)
		//IL_021a: Expected O, but got Unknown
		//IL_0276: Unknown result type (might be due to invalid IL or missing references)
		//IL_0280: Expected O, but got Unknown
		//IL_02a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ac: Expected O, but got Unknown
		//IL_02b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_02fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0316: Unknown result type (might be due to invalid IL or missing references)
		//IL_0369: Unknown result type (might be due to invalid IL or missing references)
		//IL_0370: Expected O, but got Unknown
		//IL_038f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0396: Expected O, but got Unknown
		//IL_0398: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_03bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_03da: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f1: Expected O, but got Unknown
		//IL_0448: Unknown result type (might be due to invalid IL or missing references)
		//IL_044f: Expected O, but got Unknown
		//IL_04ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_0556: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			GameObject val = GameObject.Find("Canvas");
			Transform val2 = (((Object)(object)val != (Object)null) ? val.transform.Find("RunSettings") : null);
			object obj;
			if (!((Object)(object)val2 != (Object)null))
			{
				obj = null;
			}
			else
			{
				Transform obj2 = val2.Find("Presets/Load");
				obj = ((obj2 is RectTransform) ? obj2 : null);
			}
			continueButton = (RectTransform)obj;
			if ((Object)(object)continueButton == (Object)null)
			{
				ManualLogSource logger = Logger;
				if (logger != null)
				{
					logger.LogWarning((object)"[MainMenuSavePanel] Continue button not found; cannot build the load menu.");
				}
				return false;
			}
			TextMeshProUGUI componentInChildren = ((Component)continueButton).GetComponentInChildren<TextMeshProUGUI>(true);
			TMP_FontAsset val3 = ((componentInChildren != null) ? ((TMP_Text)componentInChildren).font : null);
			Sprite uiSprite = ResourceLookupCache.FindSpriteContains("uiBlockNano");
			overlayObj = new GameObject("QoLSaveMenuOverlay", new Type[3]
			{
				typeof(RectTransform),
				typeof(CanvasRenderer),
				typeof(Image)
			});
			overlayObj.transform.SetParent(val.transform, false);
			RectTransform val4 = (RectTransform)overlayObj.transform;
			val4.anchorMin = Vector2.zero;
			val4.anchorMax = Vector2.one;
			val4.sizeDelta = Vector2.zero;
			((Graphic)overlayObj.GetComponent<Image>()).color = Color.clear;
			GameObject val5 = new GameObject("Scrim", new Type[4]
			{
				typeof(RectTransform),
				typeof(Image),
				typeof(Button),
				typeof(UITooltip)
			});
			val5.transform.SetParent(overlayObj.transform, false);
			RectTransform val6 = (RectTransform)val5.transform;
			val6.anchorMin = Vector2.zero;
			val6.anchorMax = Vector2.one;
			val6.sizeDelta = Vector2.zero;
			Image component = val5.GetComponent<Image>();
			((Graphic)component).color = Color.clear;
			((Graphic)component).raycastTarget = true;
			Button component2 = val5.GetComponent<Button>();
			((Selectable)component2).transition = (Transition)0;
			((UnityEvent)component2.onClick).AddListener(new UnityAction(Hide));
			UITooltip component3 = val5.GetComponent<UITooltip>();
			component3.skipLocale = true;
			component3.tipName = string.Empty;
			component3.tipDesc = string.Empty;
			panelObj = new GameObject("QoLSaveMenuPanel", new Type[3]
			{
				typeof(RectTransform),
				typeof(CanvasRenderer),
				typeof(Image)
			});
			panelObj.transform.SetParent(overlayObj.transform, false);
			RectTransform val7 = (RectTransform)panelObj.transform;
			val7.anchorMin = new Vector2(0.5f, 0.5f);
			val7.anchorMax = new Vector2(0.5f, 0.5f);
			val7.pivot = new Vector2(0.5f, 0.5f);
			val7.sizeDelta = new Vector2(640f, 520f);
			Image component4 = panelObj.GetComponent<Image>();
			((Graphic)component4).color = PANEL_COLOR;
			((Graphic)component4).raycastTarget = true;
			BuildFrame(panelObj.transform, 640f, 520f);
			GameObject val8 = new GameObject("Body", new Type[2]
			{
				typeof(RectTransform),
				typeof(VerticalLayoutGroup)
			});
			val8.transform.SetParent(panelObj.transform, false);
			RectTransform val9 = (RectTransform)val8.transform;
			val9.anchorMin = Vector2.zero;
			val9.anchorMax = Vector2.one;
			val9.sizeDelta = Vector2.zero;
			Vector2 val10 = ContinueButtonSize();
			VerticalLayoutGroup component5 = val8.GetComponent<VerticalLayoutGroup>();
			((LayoutGroup)component5).padding = new RectOffset(14, 14, 14, Mathf.CeilToInt(22f + val10.y));
			((HorizontalOrVerticalLayoutGroup)component5).spacing = 8f;
			((HorizontalOrVerticalLayoutGroup)component5).childControlHeight = true;
			((HorizontalOrVerticalLayoutGroup)component5).childControlWidth = true;
			((HorizontalOrVerticalLayoutGroup)component5).childForceExpandHeight = false;
			((HorizontalOrVerticalLayoutGroup)component5).childForceExpandWidth = true;
			GameObject val11 = new GameObject("Header", new Type[2]
			{
				typeof(RectTransform),
				typeof(TextMeshProUGUI)
			});
			val11.transform.SetParent(val8.transform, false);
			TextMeshProUGUI component6 = val11.GetComponent<TextMeshProUGUI>();
			if (Object.op_Implicit((Object)(object)val3))
			{
				((TMP_Text)component6).font = val3;
			}
			((TMP_Text)component6).text = "Load Save";
			((TMP_Text)component6).fontSize = 24f;
			((TMP_Text)component6).alignment = (TextAlignmentOptions)514;
			((Graphic)component6).color = Color.yellow;
			LayoutElement val12 = val11.AddComponent<LayoutElement>();
			val12.minHeight = 38f;
			val12.preferredHeight = 38f;
			slotList = new SaveSlotListView(val8.transform, val3, uiSprite, null, delegate(int slot)
			{
				SaveSaveMenuPatcher.DoLoad(SaveSlotRegistry.FileName(slot), slot);
			}, null, DescribeSlotMods, StageSlotMods);
			slotList.AddAutosaveRow("autosave.sv", null, delegate(int slot)
			{
				SaveSaveMenuPatcher.DoLoad("autosave.sv", slot);
			}, null);
			BuildContinueButton(val10);
			BuildCloseButton(panelObj.transform, val3);
			overlayObj.SetActive(false);
			return true;
		}
		catch (Exception arg)
		{
			ManualLogSource logger2 = Logger;
			if (logger2 != null)
			{
				logger2.LogError((object)$"[MainMenuSavePanel] Failed to build: {arg}");
			}
			return false;
		}
	}

	private static List<SerialMod> SlotMods(int slot)
	{
		string text = Path.Combine(SaveManager.SaveFolder, (slot == 0) ? "autosave.sv" : SaveSlotRegistry.FileName(slot));
		if (!File.Exists(text))
		{
			return null;
		}
		try
		{
			string path = text + ".meta";
			if (File.Exists(path))
			{
				SaveMetadata saveMetadata = JsonConvert.DeserializeObject<SaveMetadata>(File.ReadAllText(path));
				if (saveMetadata?.Mods != null)
				{
					return saveMetadata.Mods;
				}
			}
		}
		catch (Exception ex)
		{
			ManualLogSource logger = Logger;
			if (logger != null)
			{
				logger.LogWarning((object)("[MainMenuSavePanel] Could not read the mods of " + text + ": " + ex.Message));
			}
		}
		return ModManagementSettings.CaptureLoadedMods();
	}

	private static string DescribeSlotMods(int slot)
	{
		List<SerialMod> list = SlotMods(slot);
		return (list == null) ? null : ModManagementSettings.DescribeMods(list);
	}

	private static void StageSlotMods(int slot)
	{
		List<SerialMod> list = SlotMods(slot);
		if (list != null)
		{
			ModManagementSettings.StageModsForSave(list);
			Hide();
			ModManagementSettings.OpenModsTab();
		}
	}

	private static void BuildFrame(Transform parent, float width, float height)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Expected O, but got Unknown
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Expected O, but got Unknown
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
		//IL_010c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0127: Unknown result type (might be due to invalid IL or missing references)
		//IL_0136: Unknown result type (might be due to invalid IL or missing references)
		//IL_0141: Unknown result type (might be due to invalid IL or missing references)
		//IL_0146: Unknown result type (might be due to invalid IL or missing references)
		GameObject val = new GameObject("Frame", new Type[1] { typeof(RectTransform) });
		val.transform.SetParent(parent, false);
		val.transform.SetAsFirstSibling();
		RectTransform val2 = (RectTransform)val.transform;
		val2.anchorMin = Vector2.zero;
		val2.anchorMax = Vector2.one;
		val2.sizeDelta = Vector2.zero;
		CreateBar(val.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(width, 2f), Vector2.zero);
		CreateBar(val.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(width, 2f), Vector2.zero);
		CreateBar(val.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(2f, height), Vector2.zero);
		CreateBar(val.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(2f, height), Vector2.zero);
	}

	private static void CreateBar(Transform parent, Vector2 anchor, Vector2 pivot, Vector2 size, Vector2 position)
	{
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Expected O, but got Unknown
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Expected O, but got Unknown
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		GameObject val = new GameObject("Bar", new Type[3]
		{
			typeof(RectTransform),
			typeof(CanvasRenderer),
			typeof(Image)
		});
		val.transform.SetParent(parent, false);
		RectTransform val2 = (RectTransform)val.transform;
		val2.anchorMin = anchor;
		val2.anchorMax = anchor;
		val2.pivot = pivot;
		val2.sizeDelta = size;
		val2.anchoredPosition = position;
		Image component = val.GetComponent<Image>();
		((Graphic)component).color = BORDER_COLOR;
		((Graphic)component).raycastTarget = false;
	}

	private static Vector2 ContinueButtonSize()
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		Rect rect = continueButton.rect;
		float num = Mathf.Max(1f, ((Rect)(ref rect)).width);
		rect = continueButton.rect;
		float num2 = Mathf.Max(1f, ((Rect)(ref rect)).height);
		float num3 = Mathf.Min(104f / num2, 608f / num);
		return new Vector2(num * num3, num2 * num3);
	}

	private static void BuildContinueButton(Vector2 size)
	{
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Expected O, but got Unknown
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f9: Expected O, but got Unknown
		//IL_0114: Unknown result type (might be due to invalid IL or missing references)
		//IL_0119: Unknown result type (might be due to invalid IL or missing references)
		//IL_011f: Expected O, but got Unknown
		GameObject val = Object.Instantiate<GameObject>(((Component)continueButton).gameObject, panelObj.transform);
		((Object)val).name = "ContinueVanilla";
		RectTransform val2 = (RectTransform)val.transform;
		val2.anchorMin = new Vector2(0.5f, 0f);
		val2.anchorMax = new Vector2(0.5f, 0f);
		val2.pivot = new Vector2(0.5f, 0.5f);
		Rect rect = continueButton.rect;
		val2.sizeDelta = ((Rect)(ref rect)).size;
		Vector3 one = Vector3.one;
		float x = size.x;
		rect = continueButton.rect;
		((Transform)val2).localScale = one * (x / Mathf.Max(1f, ((Rect)(ref rect)).width));
		val2.anchoredPosition = new Vector2(0f, size.y * 0.5f + 8f);
		Button component = val.GetComponent<Button>();
		component.onClick = new ButtonClickedEvent();
		ButtonClickedEvent onClick = component.onClick;
		object obj = _003C_003Ec._003C_003E9__27_0;
		if (obj == null)
		{
			UnityAction val3 = delegate
			{
				if ((Object)(object)PreRunScript.instance != (Object)null)
				{
					PreRunScript.instance.LoadRun();
				}
			};
			_003C_003Ec._003C_003E9__27_0 = val3;
			obj = (object)val3;
		}
		((UnityEvent)onClick).AddListener((UnityAction)obj);
		((Selectable)component).interactable = SaveSystem.HasSave();
		UITooltip val4 = val.GetComponent<UITooltip>();
		if ((Object)(object)val4 == (Object)null)
		{
			val4 = val.AddComponent<UITooltip>();
		}
		val4.skipLocale = true;
		val4.tipName = "Continue vanilla run";
		val4.tipDesc = "Resume the run the game itself last saved.";
	}

	private static void BuildCloseButton(Transform parent, TMP_FontAsset font)
	{
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Expected O, but got Unknown
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Expected O, but got Unknown
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		//IL_0124: Unknown result type (might be due to invalid IL or missing references)
		//IL_0145: Unknown result type (might be due to invalid IL or missing references)
		//IL_0151: Unknown result type (might be due to invalid IL or missing references)
		//IL_0165: Unknown result type (might be due to invalid IL or missing references)
		//IL_016f: Expected O, but got Unknown
		//IL_0195: Unknown result type (might be due to invalid IL or missing references)
		//IL_019c: Expected O, but got Unknown
		//IL_01f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0209: Unknown result type (might be due to invalid IL or missing references)
		//IL_0210: Expected O, but got Unknown
		//IL_0212: Unknown result type (might be due to invalid IL or missing references)
		//IL_021f: Unknown result type (might be due to invalid IL or missing references)
		//IL_022c: Unknown result type (might be due to invalid IL or missing references)
		GameObject val = new GameObject("Close", new Type[4]
		{
			typeof(RectTransform),
			typeof(Image),
			typeof(Button),
			typeof(UITooltip)
		});
		val.transform.SetParent(parent, false);
		RectTransform val2 = (RectTransform)val.transform;
		val2.anchorMin = new Vector2(1f, 1f);
		val2.anchorMax = new Vector2(1f, 1f);
		val2.pivot = new Vector2(1f, 1f);
		val2.sizeDelta = new Vector2(36f, 36f);
		val2.anchoredPosition = new Vector2(-6f, -6f);
		((Graphic)val.GetComponent<Image>()).color = new Color(1f, 1f, 1f, 0f);
		Button component = val.GetComponent<Button>();
		ColorBlock colors = ((Selectable)component).colors;
		((ColorBlock)(ref colors)).normalColor = Color.white;
		((ColorBlock)(ref colors)).highlightedColor = new Color(1f, 0.75f, 0.75f, 1f);
		((ColorBlock)(ref colors)).pressedColor = new Color(0.7f, 0.5f, 0.5f, 1f);
		((Selectable)component).colors = colors;
		((UnityEvent)component.onClick).AddListener(new UnityAction(Hide));
		GameObject val3 = new GameObject("X", new Type[2]
		{
			typeof(RectTransform),
			typeof(TextMeshProUGUI)
		});
		val3.transform.SetParent(val.transform, false);
		TextMeshProUGUI component2 = val3.GetComponent<TextMeshProUGUI>();
		if (Object.op_Implicit((Object)(object)font))
		{
			((TMP_Text)component2).font = font;
		}
		((TMP_Text)component2).text = "X";
		((TMP_Text)component2).fontSize = 22f;
		((TMP_Text)component2).alignment = (TextAlignmentOptions)514;
		((Graphic)component2).color = Color.white;
		RectTransform val4 = (RectTransform)val3.transform;
		val4.anchorMin = Vector2.zero;
		val4.anchorMax = Vector2.one;
		val4.sizeDelta = Vector2.zero;
		UITooltip component3 = val.GetComponent<UITooltip>();
		component3.skipLocale = true;
		component3.tipName = "Close";
		component3.tipDesc = "Return to the run settings.";
	}
}
