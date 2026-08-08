using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace QoL_Unknown;

internal static class DiscordPresenceEditor
{
	[Serializable]
	[CompilerGenerated]
	private sealed class _003C_003Ec
	{
		public static readonly _003C_003Ec _003C_003E9 = new _003C_003Ec();

		public static UnityAction<string> _003C_003E9__6_0;

		public static UnityAction _003C_003E9__6_1;

		public static UnityAction _003C_003E9__6_2;

		public static UnityAction<string> _003C_003E9__7_0;

		internal void _003CBuildPanel_003Eb__6_0(string _)
		{
			RefreshPreview();
		}

		internal void _003CBuildPanel_003Eb__6_1()
		{
			ResetField(DiscordPresenceField.State, "State");
		}

		internal void _003CBuildPanel_003Eb__6_2()
		{
			panel.SetActive(false);
		}

		internal void _003CCreateCompactInputRow_003Eb__7_0(string _)
		{
			RefreshPreview();
		}
	}

	private static GameObject panel;

	private static readonly Dictionary<DiscordPresenceField, TMP_InputField> Inputs = new Dictionary<DiscordPresenceField, TMP_InputField>();

	private static TextMeshProUGUI previewText;

	private static TextMeshProUGUI statusText;

	private static readonly string RuleVariableSummary = "Variables: InWorld, InTutorial, Alive, Depth, BiomeDepth, Unchipped, BrainHealth, Consciousness, Oxygen, BloodVolume, BloodPressure, HeartRate, InCardiacArrest, FibrillationProgress, FibrillationRising, StrokeAmount, BleedRatePerMinute, Hunger, Thirst, Energy, Stamina, Happiness, TotalHappiness, AveragePain, SepticShock, RadiationSickness, Temperature, SicknessAmount, InWater, HorrifiedLevel, MindWipeActive, ThoraxMuscleHealth, HasBrokenLimb, HasShrapnel, HasDislocation, InfectedLimbCount, MaxLimbInfection.";

	public static void Open()
	{
		if ((Object)(object)panel != (Object)null)
		{
			panel.SetActive(true);
			RefreshInputs();
			RefreshPreview();
			return;
		}
		Canvas val = Object.FindObjectOfType<Canvas>();
		if ((Object)(object)val == (Object)null)
		{
			PlayerCamera main = PlayerCamera.main;
			if (main != null)
			{
				main.DoAlert("Discord Presence editor unavailable: canvas not found.", false);
			}
		}
		else
		{
			BuildPanel(((Component)val).transform);
			RefreshInputs();
			RefreshPreview();
		}
	}

	private static void BuildPanel(Transform parent)
	{
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Expected O, but got Unknown
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0115: Unknown result type (might be due to invalid IL or missing references)
		//IL_011a: Unknown result type (might be due to invalid IL or missing references)
		//IL_014e: Unknown result type (might be due to invalid IL or missing references)
		//IL_015d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0191: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_01de: Unknown result type (might be due to invalid IL or missing references)
		//IL_0240: Unknown result type (might be due to invalid IL or missing references)
		//IL_024f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0298: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_02cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0305: Unknown result type (might be due to invalid IL or missing references)
		//IL_032f: Unknown result type (might be due to invalid IL or missing references)
		//IL_033e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0372: Unknown result type (might be due to invalid IL or missing references)
		//IL_0381: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0406: Unknown result type (might be due to invalid IL or missing references)
		//IL_0415: Unknown result type (might be due to invalid IL or missing references)
		//IL_0444: Unknown result type (might be due to invalid IL or missing references)
		//IL_0453: Unknown result type (might be due to invalid IL or missing references)
		//IL_048b: Unknown result type (might be due to invalid IL or missing references)
		//IL_049a: Unknown result type (might be due to invalid IL or missing references)
		//IL_04a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_04b1: Expected O, but got Unknown
		//IL_04d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_04df: Unknown result type (might be due to invalid IL or missing references)
		//IL_04ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_04f6: Expected O, but got Unknown
		//IL_0515: Unknown result type (might be due to invalid IL or missing references)
		//IL_0524: Unknown result type (might be due to invalid IL or missing references)
		//IL_0531: Unknown result type (might be due to invalid IL or missing references)
		//IL_053b: Expected O, but got Unknown
		//IL_055a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0569: Unknown result type (might be due to invalid IL or missing references)
		//IL_0269: Unknown result type (might be due to invalid IL or missing references)
		//IL_026e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0274: Expected O, but got Unknown
		//IL_0583: Unknown result type (might be due to invalid IL or missing references)
		//IL_0588: Unknown result type (might be due to invalid IL or missing references)
		//IL_058e: Expected O, but got Unknown
		Inputs.Clear();
		panel = new GameObject("QoLDiscordPresenceEditor", new Type[2]
		{
			typeof(RectTransform),
			typeof(Image)
		});
		panel.transform.SetParent(parent, false);
		panel.transform.SetAsLastSibling();
		RectTransform component = panel.GetComponent<RectTransform>();
		component.anchorMin = new Vector2(0.5f, 0.5f);
		component.anchorMax = new Vector2(0.5f, 0.5f);
		component.pivot = new Vector2(0.5f, 0.5f);
		component.sizeDelta = new Vector2(1260f, 820f);
		component.anchoredPosition = Vector2.zero;
		Image component2 = panel.GetComponent<Image>();
		component2.sprite = ResourceLookupCache.FindSpriteContains("uiBlockNano");
		component2.type = (Type)1;
		((Graphic)component2).color = Color.white;
		CreateOpaqueInset(panel.transform, "PanelFill", new Vector2(1250f, 810f), Color.black);
		TMP_FontAsset font = ResourceLookupCache.FindFontContains("Retro GamingPix");
		CreateText(panel.transform, "Title", "Discord Presence", new Vector2(-365f, 374f), new Vector2(450f, 38f), 28f, (TextAlignmentOptions)4097, font);
		CreateText(panel.transform, "StateLabel", "State rules", new Vector2(-270f, 320f), new Vector2(650f, 28f), 18f, (TextAlignmentOptions)4097, font);
		TMP_InputField val = CreateInput(panel.transform, "StateRulesInput", new Vector2(-270f, 142f), new Vector2(650f, 280f), font);
		((UnityEvent<string>)(object)val.onValueChanged).AddListener((UnityAction<string>)delegate
		{
			RefreshPreview();
		});
		Inputs[DiscordPresenceField.State] = val;
		Transform transform = panel.transform;
		Vector2 anchoredPosition = new Vector2(-15f, -27f);
		Vector2 size = new Vector2(140f, 34f);
		object obj = _003C_003Ec._003C_003E9__6_1;
		if (obj == null)
		{
			UnityAction val2 = delegate
			{
				ResetField(DiscordPresenceField.State, "State");
			};
			_003C_003Ec._003C_003E9__6_1 = val2;
			obj = (object)val2;
		}
		CreateButton(transform, "StateReset", "Reset State", anchoredPosition, size, font, (UnityAction)obj);
		CreateText(panel.transform, "OtherLabel", "Other fields", new Vector2(-270f, -70f), new Vector2(650f, 28f), 18f, (TextAlignmentOptions)4097, font);
		CreateCompactInputRow(DiscordPresenceField.Details, "Details", new Vector2(-270f, -122f), font);
		CreateCompactInputRow(DiscordPresenceField.LargeImageText, "Large tooltip", new Vector2(-270f, -182f), font);
		CreateCompactInputRow(DiscordPresenceField.SmallImageText, "Small tooltip", new Vector2(-270f, -242f), font);
		CreateText(panel.transform, "PreviewLabel", "Live preview", new Vector2(365f, 320f), new Vector2(430f, 28f), 18f, (TextAlignmentOptions)4097, font);
		previewText = CreatePanelText(panel.transform, "", string.Empty, new Vector2(365f, 198f), new Vector2(430f, 205f), 14f, font);
		CreateText(panel.transform, "VariablesLabel", "Variables", new Vector2(365f, 64f), new Vector2(430f, 28f), 18f, (TextAlignmentOptions)4097, font);
		CreatePanelText(panel.transform, "Variables", RuleVariableSummary + "\n\nTokens: " + DiscordPresenceSettings.TokensDisplay, new Vector2(365f, -104f), new Vector2(430f, 290f), 11f, font);
		statusText = CreateText(panel.transform, "Status", string.Empty, new Vector2(0f, -324f), new Vector2(900f, 24f), 13f, (TextAlignmentOptions)514, font);
		CreateButton(panel.transform, "Apply", "Apply", new Vector2(-280f, -370f), new Vector2(120f, 38f), font, new UnityAction(Apply));
		CreateButton(panel.transform, "PreviewButton", "Preview", new Vector2(-140f, -370f), new Vector2(120f, 38f), font, new UnityAction(RefreshPreview));
		CreateButton(panel.transform, "ResetAll", "Reset All", new Vector2(20f, -370f), new Vector2(140f, 38f), font, new UnityAction(ResetAll));
		Transform transform2 = panel.transform;
		Vector2 anchoredPosition2 = new Vector2(420f, -370f);
		Vector2 size2 = new Vector2(120f, 38f);
		object obj2 = _003C_003Ec._003C_003E9__6_2;
		if (obj2 == null)
		{
			UnityAction val3 = delegate
			{
				panel.SetActive(false);
			};
			_003C_003Ec._003C_003E9__6_2 = val3;
			obj2 = (object)val3;
		}
		CreateButton(transform2, "Close", "Close", anchoredPosition2, size2, font, (UnityAction)obj2);
	}

	private static void CreateCompactInputRow(DiscordPresenceField field, string label, Vector2 anchoredPosition, TMP_FontAsset font)
	{
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0125: Unknown result type (might be due to invalid IL or missing references)
		//IL_0131: Unknown result type (might be due to invalid IL or missing references)
		//IL_0137: Unknown result type (might be due to invalid IL or missing references)
		//IL_0146: Unknown result type (might be due to invalid IL or missing references)
		//IL_0153: Unknown result type (might be due to invalid IL or missing references)
		//IL_015d: Expected O, but got Unknown
		CreateText(panel.transform, label + "Label", label, new Vector2(anchoredPosition.x - 255f, anchoredPosition.y), new Vector2(130f, 26f), 14f, (TextAlignmentOptions)4100, font);
		TMP_InputField val = CreateInput(panel.transform, field.ToString() + "Input", new Vector2(anchoredPosition.x + 45f, anchoredPosition.y), new Vector2(450f, 42f), font);
		((UnityEvent<string>)(object)val.onValueChanged).AddListener((UnityAction<string>)delegate
		{
			RefreshPreview();
		});
		Inputs[field] = val;
		CreateButton(panel.transform, field.ToString() + "Reset", "Reset", new Vector2(anchoredPosition.x + 320f, anchoredPosition.y), new Vector2(86f, 30f), font, (UnityAction)delegate
		{
			ResetField(field, label);
		});
	}

	private static void ResetField(DiscordPresenceField field, string label)
	{
		DiscordPresenceSettings.ResetTemplate(field);
		DiscordPresenceSettings.Save();
		if (Inputs.TryGetValue(field, out var value))
		{
			value.SetTextWithoutNotify(DiscordPresenceSettings.GetTemplate(field));
		}
		RefreshPreview();
		SetStatus("Reset " + label + ".");
		DiscordManager.Instance?.UpdatePresence();
	}

	private static TMP_InputField CreateInput(Transform parent, string name, Vector2 anchoredPosition, Vector2 size, TMP_FontAsset font)
	{
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Expected O, but got Unknown
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0104: Unknown result type (might be due to invalid IL or missing references)
		//IL_0134: Unknown result type (might be due to invalid IL or missing references)
		//IL_013a: Expected O, but got Unknown
		//IL_0157: Unknown result type (might be due to invalid IL or missing references)
		//IL_0164: Unknown result type (might be due to invalid IL or missing references)
		//IL_017b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0192: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0209: Unknown result type (might be due to invalid IL or missing references)
		//IL_0236: Unknown result type (might be due to invalid IL or missing references)
		//IL_023b: Unknown result type (might be due to invalid IL or missing references)
		//IL_025e: Unknown result type (might be due to invalid IL or missing references)
		//IL_026b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0278: Unknown result type (might be due to invalid IL or missing references)
		//IL_0285: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_031e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0334: Unknown result type (might be due to invalid IL or missing references)
		GameObject val = new GameObject(name, new Type[3]
		{
			typeof(RectTransform),
			typeof(Image),
			typeof(TMP_InputField)
		});
		val.transform.SetParent(parent, false);
		RectTransform component = val.GetComponent<RectTransform>();
		component.anchorMin = new Vector2(0.5f, 0.5f);
		component.anchorMax = new Vector2(0.5f, 0.5f);
		component.pivot = new Vector2(0.5f, 0.5f);
		component.sizeDelta = size;
		component.anchoredPosition = anchoredPosition;
		Image component2 = val.GetComponent<Image>();
		component2.sprite = ResourceLookupCache.FindSpriteContains("uiBlockNano");
		component2.type = (Type)1;
		((Graphic)component2).color = Color.white;
		CreateOpaqueInset(val.transform, "Fill", new Vector2(Mathf.Max(0f, size.x - 8f), Mathf.Max(0f, size.y - 8f)), Color.black);
		GameObject val2 = new GameObject("Text Area", new Type[2]
		{
			typeof(RectTransform),
			typeof(RectMask2D)
		});
		val2.transform.SetParent(val.transform, false);
		RectTransform component3 = val2.GetComponent<RectTransform>();
		component3.anchorMin = Vector2.zero;
		component3.anchorMax = Vector2.one;
		component3.offsetMin = new Vector2(10f, 8f);
		component3.offsetMax = new Vector2(-10f, -8f);
		TextMeshProUGUI val3 = CreateText(val2.transform, "Text", string.Empty, Vector2.zero, Vector2.zero, 15f, (TextAlignmentOptions)257, font);
		RectTransform component4 = ((Component)val3).GetComponent<RectTransform>();
		component4.anchorMin = Vector2.zero;
		component4.anchorMax = Vector2.one;
		component4.offsetMin = Vector2.zero;
		component4.offsetMax = Vector2.zero;
		((Graphic)val3).color = Color.white;
		((TMP_Text)val3).enableWordWrapping = true;
		((Graphic)val3).raycastTarget = true;
		TextMeshProUGUI val4 = CreateText(val2.transform, "Placeholder", "Template text...", Vector2.zero, Vector2.zero, 15f, (TextAlignmentOptions)257, font);
		RectTransform component5 = ((Component)val4).GetComponent<RectTransform>();
		component5.anchorMin = Vector2.zero;
		component5.anchorMax = Vector2.one;
		component5.offsetMin = Vector2.zero;
		component5.offsetMax = Vector2.zero;
		((Graphic)val4).color = new Color(1f, 1f, 1f, 0.35f);
		((TMP_Text)val4).enableWordWrapping = true;
		((Graphic)val4).raycastTarget = false;
		TMP_InputField component6 = val.GetComponent<TMP_InputField>();
		component6.textViewport = component3;
		component6.textComponent = (TMP_Text)(object)val3;
		component6.placeholder = (Graphic)(object)val4;
		component6.lineType = (LineType)2;
		component6.characterLimit = 6000;
		((Selectable)component6).targetGraphic = (Graphic)(object)component2;
		component6.selectionColor = new Color(0.35f, 0.62f, 1f, 0.55f);
		component6.customCaretColor = true;
		component6.caretColor = Color.white;
		component6.caretWidth = 3;
		component6.caretBlinkRate = 0.65f;
		val2.transform.SetAsLastSibling();
		return component6;
	}

	private static TextMeshProUGUI CreateText(Transform parent, string name, string text, Vector2 anchoredPosition, Vector2 size, float fontSize, TextAlignmentOptions alignment, TMP_FontAsset font)
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Expected O, but got Unknown
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		GameObject val = new GameObject(name, new Type[2]
		{
			typeof(RectTransform),
			typeof(TextMeshProUGUI)
		});
		val.transform.SetParent(parent, false);
		RectTransform component = val.GetComponent<RectTransform>();
		component.anchorMin = new Vector2(0.5f, 0.5f);
		component.anchorMax = new Vector2(0.5f, 0.5f);
		component.pivot = new Vector2(0.5f, 0.5f);
		component.sizeDelta = size;
		component.anchoredPosition = anchoredPosition;
		TextMeshProUGUI component2 = val.GetComponent<TextMeshProUGUI>();
		((TMP_Text)component2).text = text;
		((TMP_Text)component2).fontSize = fontSize;
		((TMP_Text)component2).alignment = alignment;
		((TMP_Text)component2).enableWordWrapping = true;
		((Graphic)component2).raycastTarget = false;
		((Graphic)component2).color = Color.white;
		if ((Object)(object)font != (Object)null)
		{
			((TMP_Text)component2).font = font;
		}
		return component2;
	}

	private static TextMeshProUGUI CreatePanelText(Transform parent, string name, string text, Vector2 anchoredPosition, Vector2 size, float fontSize, TMP_FontAsset font)
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Expected O, but got Unknown
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
		//IL_0112: Unknown result type (might be due to invalid IL or missing references)
		//IL_0129: Unknown result type (might be due to invalid IL or missing references)
		//IL_0140: Unknown result type (might be due to invalid IL or missing references)
		//IL_0160: Unknown result type (might be due to invalid IL or missing references)
		GameObject val = new GameObject(name + "Panel", new Type[2]
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
		component.anchoredPosition = anchoredPosition;
		Image component2 = val.GetComponent<Image>();
		component2.sprite = ResourceLookupCache.FindSpriteContains("uiBlockNano");
		component2.type = (Type)1;
		((Graphic)component2).color = new Color(0f, 0f, 0f, 0.26f);
		TextMeshProUGUI val2 = CreateText(val.transform, name, text, Vector2.zero, Vector2.zero, fontSize, (TextAlignmentOptions)257, font);
		RectTransform component3 = ((Component)val2).GetComponent<RectTransform>();
		component3.anchorMin = Vector2.zero;
		component3.anchorMax = Vector2.one;
		component3.offsetMin = new Vector2(12f, 10f);
		component3.offsetMax = new Vector2(-12f, -10f);
		((Graphic)val2).color = new Color(0.9f, 0.94f, 1f, 1f);
		((TMP_Text)val2).enableWordWrapping = true;
		return val2;
	}

	private static Button CreateButton(Transform parent, string name, string label, Vector2 anchoredPosition, Vector2 size, TMP_FontAsset font, UnityAction onClick)
	{
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Expected O, but got Unknown
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0102: Unknown result type (might be due to invalid IL or missing references)
		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
		//IL_011e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0123: Unknown result type (might be due to invalid IL or missing references)
		//IL_0144: Unknown result type (might be due to invalid IL or missing references)
		//IL_0151: Unknown result type (might be due to invalid IL or missing references)
		//IL_015e: Unknown result type (might be due to invalid IL or missing references)
		//IL_016b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0177: Unknown result type (might be due to invalid IL or missing references)
		GameObject val = new GameObject(name, new Type[3]
		{
			typeof(RectTransform),
			typeof(Image),
			typeof(Button)
		});
		val.transform.SetParent(parent, false);
		RectTransform component = val.GetComponent<RectTransform>();
		component.anchorMin = new Vector2(0.5f, 0.5f);
		component.anchorMax = new Vector2(0.5f, 0.5f);
		component.pivot = new Vector2(0.5f, 0.5f);
		component.sizeDelta = size;
		component.anchoredPosition = anchoredPosition;
		Image component2 = val.GetComponent<Image>();
		component2.sprite = ResourceLookupCache.FindSpriteContains("uiBlockNano");
		component2.type = (Type)1;
		((Graphic)component2).color = Color.white;
		CreateOpaqueInset(val.transform, "Fill", new Vector2(Mathf.Max(0f, size.x - 8f), Mathf.Max(0f, size.y - 8f)), Color.black);
		TextMeshProUGUI val2 = CreateText(val.transform, "Label", label, Vector2.zero, Vector2.zero, 16f, (TextAlignmentOptions)514, font);
		RectTransform component3 = ((Component)val2).GetComponent<RectTransform>();
		component3.anchorMin = Vector2.zero;
		component3.anchorMax = Vector2.one;
		component3.offsetMin = Vector2.zero;
		component3.offsetMax = Vector2.zero;
		((Graphic)val2).color = Color.white;
		Button component4 = val.GetComponent<Button>();
		((UnityEventBase)component4.onClick).RemoveAllListeners();
		((UnityEvent)component4.onClick).AddListener(onClick);
		return component4;
	}

	private static void CreateOpaqueInset(Transform parent, string name, Vector2 size, Color color)
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Expected O, but got Unknown
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		GameObject val = new GameObject(name, new Type[2]
		{
			typeof(RectTransform),
			typeof(Image)
		});
		val.transform.SetParent(parent, false);
		val.transform.SetAsFirstSibling();
		RectTransform component = val.GetComponent<RectTransform>();
		component.anchorMin = new Vector2(0.5f, 0.5f);
		component.anchorMax = new Vector2(0.5f, 0.5f);
		component.pivot = new Vector2(0.5f, 0.5f);
		component.sizeDelta = size;
		component.anchoredPosition = Vector2.zero;
		Image component2 = val.GetComponent<Image>();
		((Graphic)component2).color = color;
		((Graphic)component2).raycastTarget = false;
	}

	private static void RefreshInputs()
	{
		foreach (KeyValuePair<DiscordPresenceField, TMP_InputField> input in Inputs)
		{
			input.Value.SetTextWithoutNotify(DiscordPresenceSettings.GetTemplate(input.Key));
		}
	}

	private static void Apply()
	{
		foreach (KeyValuePair<DiscordPresenceField, TMP_InputField> input in Inputs)
		{
			DiscordPresenceSettings.SetTemplate(input.Key, input.Value.text);
		}
		DiscordPresenceSettings.Save();
		RefreshPreview();
		DiscordManager.Instance?.UpdatePresence();
		SetStatus("Applied Discord Presence templates.");
	}

	private static void ResetAll()
	{
		DiscordPresenceSettings.ResetAll();
		RefreshInputs();
		RefreshPreview();
		DiscordManager.Instance?.UpdatePresence();
		SetStatus("Reset all Discord Presence templates.");
	}

	private static void RefreshPreview()
	{
		if (!((Object)(object)previewText == (Object)null))
		{
			DiscordPresenceContext discordPresenceContext = DiscordManager.BuildPresenceContext();
			string ruleError;
			DiscordPresencePreview discordPresencePreview = new DiscordPresencePreview
			{
				Details = DiscordPresenceSettings.RenderTemplate(GetInputText(DiscordPresenceField.Details), discordPresenceContext, discordPresenceContext.DefaultDetails),
				State = DiscordPresenceSettings.RenderStateTemplate(GetInputText(DiscordPresenceField.State), discordPresenceContext, discordPresenceContext.DefaultState, out ruleError),
				LargeImageText = DiscordPresenceSettings.RenderTemplate(GetInputText(DiscordPresenceField.LargeImageText), discordPresenceContext, discordPresenceContext.DefaultLargeImageText),
				SmallImageText = DiscordPresenceSettings.RenderTemplate(GetInputText(DiscordPresenceField.SmallImageText), discordPresenceContext, discordPresenceContext.DefaultSmallImageText)
			};
			((TMP_Text)previewText).text = "Preview\nDetails: " + discordPresencePreview.Details + "\nState: " + discordPresencePreview.State + "\nLarge tooltip: " + discordPresencePreview.LargeImageText + "\nSmall tooltip: " + discordPresencePreview.SmallImageText;
			if (!string.IsNullOrEmpty(ruleError))
			{
				SetStatus("State rule error: " + ruleError);
			}
			else
			{
				SetStatus(string.Empty);
			}
		}
	}

	private static string GetInputText(DiscordPresenceField field)
	{
		TMP_InputField value;
		return Inputs.TryGetValue(field, out value) ? value.text : DiscordPresenceSettings.GetTemplate(field);
	}

	private static void SetStatus(string message)
	{
		if ((Object)(object)statusText != (Object)null)
		{
			((TMP_Text)statusText).text = message;
		}
	}
}
