using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using BepInEx.Logging;
using Newtonsoft.Json;
using QoL_Unknown.Images;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace QoL_Unknown;

internal sealed class SaveSlotListView
{
	private sealed class Row
	{
		public GameObject Root;

		public Image Background;

		public AspectRatioFitter BackgroundFitter;

		public TMP_InputField NameField;

		public LayoutElement NameLayout;

		public TextMeshProUGUI Tags;

		public TextMeshProUGUI Details;

		public GameObject ModsButton;

		public UITooltip ModsTooltip;
	}

	private const float RowHeight = 80f;

	private const float RowSpacing = 8f;

	private const float ScrollbarGap = 6f;

	private const float NameMinWidth = 24f;

	private const float NameMaxWidth = 260f;

	private readonly TMP_FontAsset font;

	private readonly Sprite uiSprite;

	private readonly Action<int> onSave;

	private readonly Action<int> onLoad;

	private readonly Action<int> onDelete;

	private readonly Func<int, string> modsTipFor;

	private readonly Action<int> onStageMods;

	private readonly RectTransform content;

	private readonly Dictionary<int, Row> rows = new Dictionary<int, Row>();

	private Row autosaveRow;

	public RectTransform Root { get; }

	public SaveSlotListView(Transform parent, TMP_FontAsset font, Sprite uiSprite, Action<int> onSave, Action<int> onLoad, Action<int> onDelete, Func<int, string> modsTipFor = null, Action<int> onStageMods = null)
	{
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Expected O, but got Unknown
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Expected O, but got Unknown
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_011c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0123: Expected O, but got Unknown
		//IL_013e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0145: Expected O, but got Unknown
		//IL_0147: Unknown result type (might be due to invalid IL or missing references)
		//IL_0154: Unknown result type (might be due to invalid IL or missing references)
		//IL_016b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0178: Unknown result type (might be due to invalid IL or missing references)
		//IL_018c: Unknown result type (might be due to invalid IL or missing references)
		//IL_019e: Unknown result type (might be due to invalid IL or missing references)
		//IL_01db: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e2: Expected O, but got Unknown
		//IL_01fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0204: Expected O, but got Unknown
		//IL_0214: Unknown result type (might be due to invalid IL or missing references)
		//IL_0225: Unknown result type (might be due to invalid IL or missing references)
		//IL_0240: Unknown result type (might be due to invalid IL or missing references)
		//IL_0251: Unknown result type (might be due to invalid IL or missing references)
		this.font = font;
		this.uiSprite = uiSprite;
		this.onSave = onSave;
		this.onLoad = onLoad;
		this.onDelete = onDelete;
		this.modsTipFor = modsTipFor;
		this.onStageMods = onStageMods;
		GameObject val = new GameObject("SlotScroll", new Type[2]
		{
			typeof(RectTransform),
			typeof(ScrollRect)
		});
		val.transform.SetParent(parent, false);
		Root = (RectTransform)val.transform;
		LayoutElement val2 = val.AddComponent<LayoutElement>();
		val2.minHeight = 80f;
		val2.flexibleHeight = 1f;
		Scrollbar val3 = CreateScrollbar(Root);
		float num = (((Object)(object)val3 != (Object)null) ? (((RectTransform)((Component)val3).transform).sizeDelta.x + 6f) : 0f);
		GameObject val4 = new GameObject("Viewport", new Type[3]
		{
			typeof(RectTransform),
			typeof(Image),
			typeof(RectMask2D)
		});
		val4.transform.SetParent((Transform)(object)Root, false);
		RectTransform val5 = (RectTransform)val4.transform;
		val5.anchorMin = Vector2.zero;
		val5.anchorMax = Vector2.one;
		val5.pivot = new Vector2(0f, 1f);
		val5.offsetMin = Vector2.zero;
		val5.offsetMax = new Vector2(0f - num, 0f);
		((Graphic)val4.GetComponent<Image>()).color = Color.clear;
		GameObject val6 = new GameObject("Content", new Type[3]
		{
			typeof(RectTransform),
			typeof(VerticalLayoutGroup),
			typeof(ContentSizeFitter)
		});
		val6.transform.SetParent((Transform)(object)val5, false);
		content = (RectTransform)val6.transform;
		content.anchorMin = new Vector2(0f, 1f);
		content.anchorMax = Vector2.one;
		content.pivot = new Vector2(0.5f, 1f);
		content.sizeDelta = Vector2.zero;
		VerticalLayoutGroup component = val6.GetComponent<VerticalLayoutGroup>();
		((HorizontalOrVerticalLayoutGroup)component).spacing = 8f;
		((HorizontalOrVerticalLayoutGroup)component).childControlHeight = true;
		((HorizontalOrVerticalLayoutGroup)component).childControlWidth = true;
		((HorizontalOrVerticalLayoutGroup)component).childForceExpandHeight = false;
		((HorizontalOrVerticalLayoutGroup)component).childForceExpandWidth = true;
		val6.GetComponent<ContentSizeFitter>().verticalFit = (FitMode)2;
		ScrollRect component2 = val.GetComponent<ScrollRect>();
		component2.viewport = val5;
		component2.content = content;
		component2.horizontal = false;
		component2.vertical = true;
		component2.movementType = (MovementType)2;
		component2.scrollSensitivity = 30f;
		if ((Object)(object)val3 != (Object)null)
		{
			component2.verticalScrollbar = val3;
			component2.verticalScrollbarVisibility = (ScrollbarVisibility)0;
		}
	}

	public void AddAutosaveRow(string filename, Action<int> onSave, Action<int> onLoad, Action<int> onDelete)
	{
		if (autosaveRow != null)
		{
			autosaveRow.Root.SetActive(false);
			Object.Destroy((Object)(object)autosaveRow.Root);
		}
		autosaveRow = CreateRow(0, onSave, onLoad, onDelete, excludeSave: true);
		autosaveRow.Root.SetActive(true);
	}

	public void Refresh()
	{
		List<int> slots = SaveSlotRegistry.GetSlots();
		foreach (int item in rows.Keys.Where((int slot) => !slots.Contains(slot)).ToList())
		{
			rows[item].Root.SetActive(false);
			Object.Destroy((Object)(object)rows[item].Root);
			rows.Remove(item);
		}
		for (int num = 0; num < slots.Count; num++)
		{
			int num2 = slots[num];
			if (!rows.TryGetValue(num2, out var value))
			{
				value = CreateRow(num2);
				rows[num2] = value;
			}
			value.Root.transform.SetSiblingIndex(num);
			UpdateRow(num2, value);
		}
		if (autosaveRow != null)
		{
			autosaveRow.Root.transform.SetSiblingIndex(slots.Count);
			UpdateRow(0, autosaveRow);
		}
	}

	private static Scrollbar CreateScrollbar(RectTransform parent)
	{
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Expected O, but got Unknown
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ef: Expected O, but got Unknown
		GameObject val = Resources.Load<GameObject>("Special/SettingsMenu");
		Transform val2 = (((Object)(object)val != (Object)null) ? val.transform.Find("Panel/Scroll/Scrollbar Vertical") : null);
		if ((Object)(object)val2 == (Object)null)
		{
			ManualLogSource logger = SaveSlotRegistry.Logger;
			if (logger != null)
			{
				logger.LogWarning((object)"Settings menu scrollbar not found; save slot list will scroll without a scrollbar.");
			}
			return null;
		}
		GameObject val3 = Object.Instantiate<GameObject>(((Component)val2).gameObject, (Transform)(object)parent, false);
		((Object)val3).name = "Scrollbar";
		RectTransform val4 = (RectTransform)val3.transform;
		val4.anchorMin = new Vector2(1f, 0f);
		val4.anchorMax = Vector2.one;
		val4.pivot = new Vector2(1f, 1f);
		val4.anchoredPosition = Vector2.zero;
		val4.sizeDelta = new Vector2(val4.sizeDelta.x, 0f);
		Scrollbar component = val3.GetComponent<Scrollbar>();
		component.onValueChanged = new ScrollEvent();
		component.direction = (Direction)2;
		return component;
	}

	private Row CreateRow(int slot, Action<int> rowSave = null, Action<int> rowLoad = null, Action<int> rowDelete = null, bool excludeSave = false)
	{
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Expected O, but got Unknown
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0124: Unknown result type (might be due to invalid IL or missing references)
		//IL_012b: Expected O, but got Unknown
		//IL_014a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0157: Unknown result type (might be due to invalid IL or missing references)
		//IL_0164: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_021d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0224: Expected O, but got Unknown
		//IL_0243: Unknown result type (might be due to invalid IL or missing references)
		//IL_0250: Unknown result type (might be due to invalid IL or missing references)
		//IL_025d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0283: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ad: Expected O, but got Unknown
		//IL_02cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0301: Unknown result type (might be due to invalid IL or missing references)
		//IL_030b: Expected O, but got Unknown
		//IL_035e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0365: Expected O, but got Unknown
		//IL_03eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f2: Expected O, but got Unknown
		//IL_0509: Unknown result type (might be due to invalid IL or missing references)
		//IL_0510: Expected O, but got Unknown
		//IL_05b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_05ba: Expected O, but got Unknown
		//IL_05fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0607: Expected O, but got Unknown
		//IL_0634: Unknown result type (might be due to invalid IL or missing references)
		//IL_063e: Expected O, but got Unknown
		//IL_066b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0675: Expected O, but got Unknown
		Action<int> onSave = rowSave ?? this.onSave;
		Action<int> onLoad = rowLoad ?? this.onLoad;
		Action<int> onDelete = rowDelete ?? this.onDelete;
		if (excludeSave)
		{
			onSave = null;
		}
		Row row = new Row();
		GameObject val = new GameObject($"Slot_{slot}", new Type[3]
		{
			typeof(RectTransform),
			typeof(Image),
			typeof(RectMask2D)
		});
		val.transform.SetParent((Transform)(object)content, false);
		((Graphic)val.GetComponent<Image>()).color = Color.clear;
		row.Root = val;
		LayoutElement val2 = val.AddComponent<LayoutElement>();
		val2.minHeight = 80f;
		val2.preferredHeight = 80f;
		GameObject val3 = new GameObject("Background", new Type[3]
		{
			typeof(RectTransform),
			typeof(Image),
			typeof(AspectRatioFitter)
		});
		val3.transform.SetParent(val.transform, false);
		RectTransform component = val3.GetComponent<RectTransform>();
		component.anchorMin = Vector2.zero;
		component.anchorMax = Vector2.one;
		component.sizeDelta = Vector2.zero;
		row.Background = val3.GetComponent<Image>();
		if (Object.op_Implicit((Object)(object)uiSprite))
		{
			row.Background.sprite = uiSprite;
		}
		row.Background.type = (Type)1;
		((Graphic)row.Background).color = new Color(0f, 0f, 0f, 0.5f);
		row.BackgroundFitter = val3.GetComponent<AspectRatioFitter>();
		row.BackgroundFitter.aspectMode = (AspectMode)4;
		((Behaviour)row.BackgroundFitter).enabled = false;
		GameObject val4 = new GameObject("Dimmer", new Type[2]
		{
			typeof(RectTransform),
			typeof(Image)
		});
		val4.transform.SetParent(val.transform, false);
		RectTransform component2 = val4.GetComponent<RectTransform>();
		component2.anchorMin = Vector2.zero;
		component2.anchorMax = Vector2.one;
		component2.sizeDelta = Vector2.zero;
		((Graphic)val4.GetComponent<Image>()).color = new Color(0f, 0f, 0f, 0.6f);
		GameObject val5 = new GameObject("Content", new Type[1] { typeof(RectTransform) });
		val5.transform.SetParent(val.transform, false);
		RectTransform component3 = val5.GetComponent<RectTransform>();
		component3.anchorMin = Vector2.zero;
		component3.anchorMax = Vector2.one;
		component3.sizeDelta = Vector2.zero;
		HorizontalLayoutGroup val6 = val5.AddComponent<HorizontalLayoutGroup>();
		((LayoutGroup)val6).padding = new RectOffset(10, 5, 5, 5);
		((HorizontalOrVerticalLayoutGroup)val6).spacing = 10f;
		((HorizontalOrVerticalLayoutGroup)val6).childControlWidth = true;
		((HorizontalOrVerticalLayoutGroup)val6).childForceExpandWidth = false;
		((HorizontalOrVerticalLayoutGroup)val6).childControlHeight = true;
		((HorizontalOrVerticalLayoutGroup)val6).childForceExpandHeight = false;
		((LayoutGroup)val6).childAlignment = (TextAnchor)3;
		GameObject val7 = new GameObject("Info", new Type[1] { typeof(RectTransform) });
		val7.transform.SetParent(val5.transform, false);
		VerticalLayoutGroup val8 = val7.AddComponent<VerticalLayoutGroup>();
		((LayoutGroup)val8).childAlignment = (TextAnchor)3;
		((HorizontalOrVerticalLayoutGroup)val8).childControlWidth = true;
		((HorizontalOrVerticalLayoutGroup)val8).childControlHeight = true;
		((HorizontalOrVerticalLayoutGroup)val8).childForceExpandWidth = true;
		((HorizontalOrVerticalLayoutGroup)val8).childForceExpandHeight = false;
		LayoutElement val9 = val7.AddComponent<LayoutElement>();
		val9.minWidth = 320f;
		val9.flexibleWidth = 1f;
		GameObject val10 = new GameObject("Title", new Type[1] { typeof(RectTransform) });
		val10.transform.SetParent(val7.transform, false);
		HorizontalLayoutGroup val11 = val10.AddComponent<HorizontalLayoutGroup>();
		((HorizontalOrVerticalLayoutGroup)val11).spacing = 6f;
		((LayoutGroup)val11).childAlignment = (TextAnchor)3;
		((HorizontalOrVerticalLayoutGroup)val11).childControlWidth = true;
		((HorizontalOrVerticalLayoutGroup)val11).childControlHeight = true;
		((HorizontalOrVerticalLayoutGroup)val11).childForceExpandWidth = false;
		((HorizontalOrVerticalLayoutGroup)val11).childForceExpandHeight = false;
		LayoutElement val12 = val10.AddComponent<LayoutElement>();
		val12.minHeight = 24f;
		val12.preferredHeight = 24f;
		row.NameField = CreateNameField(val10.transform, slot, out row.NameLayout);
		row.Tags = CreateText(val10.transform, "Tags");
		((TMP_Text)row.Tags).enableWordWrapping = false;
		((TMP_Text)row.Tags).overflowMode = (TextOverflowModes)1;
		((Component)row.Tags).gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
		row.Details = CreateText(val7.transform, "Details");
		GameObject val13 = new GameObject("Buttons", new Type[1] { typeof(RectTransform) });
		val13.transform.SetParent(val5.transform, false);
		HorizontalLayoutGroup val14 = val13.AddComponent<HorizontalLayoutGroup>();
		((HorizontalOrVerticalLayoutGroup)val14).spacing = 5f;
		((LayoutGroup)val14).childAlignment = (TextAnchor)5;
		((HorizontalOrVerticalLayoutGroup)val14).childForceExpandWidth = false;
		((HorizontalOrVerticalLayoutGroup)val14).childControlWidth = false;
		((HorizontalOrVerticalLayoutGroup)val14).childControlHeight = false;
		LayoutElement val15 = val13.AddComponent<LayoutElement>();
		val15.minWidth = 118f;
		val15.preferredWidth = 118f;
		if (modsTipFor != null)
		{
			row.ModsTooltip = CreateIconButton(val13.transform, "question", string.Empty, "Click to activate mods.", (UnityAction)delegate
			{
				onStageMods?.Invoke(slot);
			});
			row.ModsButton = ((Component)row.ModsTooltip).gameObject;
		}
		if (onSave != null)
		{
			CreateIconButton(val13.transform, "save", "Save", string.Empty, (UnityAction)delegate
			{
				onSave(slot);
			});
		}
		if (onLoad != null)
		{
			CreateIconButton(val13.transform, "load", "Load", string.Empty, (UnityAction)delegate
			{
				onLoad(slot);
			});
		}
		if (onDelete != null)
		{
			CreateIconButton(val13.transform, "delete", "Delete", string.Empty, (UnityAction)delegate
			{
				onDelete(slot);
			});
		}
		return row;
	}

	private TMP_InputField CreateNameField(Transform parent, int slot, out LayoutElement nameLayout)
	{
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Expected O, but got Unknown
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Expected O, but got Unknown
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Expected O, but got Unknown
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0101: Unknown result type (might be due to invalid IL or missing references)
		//IL_013c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0149: Unknown result type (might be due to invalid IL or missing references)
		//IL_0156: Unknown result type (might be due to invalid IL or missing references)
		//IL_0163: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_020f: Unknown result type (might be due to invalid IL or missing references)
		GameObject val = new GameObject("Name", new Type[3]
		{
			typeof(RectTransform),
			typeof(Image),
			typeof(UITooltip)
		});
		val.SetActive(false);
		val.transform.SetParent(parent, false);
		((Graphic)val.GetComponent<Image>()).color = Color.clear;
		LayoutElement layout = val.AddComponent<LayoutElement>();
		layout.preferredHeight = 24f;
		nameLayout = layout;
		GameObject val2 = new GameObject("Text Area", new Type[2]
		{
			typeof(RectTransform),
			typeof(RectMask2D)
		});
		val2.transform.SetParent(val.transform, false);
		RectTransform val3 = (RectTransform)val2.transform;
		val3.anchorMin = Vector2.zero;
		val3.anchorMax = Vector2.one;
		val3.offsetMin = Vector2.zero;
		val3.offsetMax = Vector2.zero;
		TextMeshProUGUI val4 = CreateText(val2.transform, "Text");
		((TMP_Text)val4).fontStyle = (FontStyles)1;
		((TMP_Text)val4).richText = false;
		RectTransform rectTransform = ((TMP_Text)val4).rectTransform;
		rectTransform.anchorMin = Vector2.zero;
		rectTransform.anchorMax = Vector2.one;
		rectTransform.offsetMin = Vector2.zero;
		rectTransform.offsetMax = Vector2.zero;
		TMP_InputField input = val.AddComponent<TMP_InputField>();
		input.textViewport = val3;
		input.textComponent = (TMP_Text)(object)val4;
		input.lineType = (LineType)0;
		input.richText = false;
		input.characterLimit = 32;
		((Selectable)input).transition = (Transition)0;
		input.onFocusSelectAll = true;
		input.selectionColor = new Color(0.25f, 0.55f, 1f, 0.6f);
		input.customCaretColor = true;
		input.caretColor = Color.white;
		input.caretWidth = 2;
		input.SetTextWithoutNotify(SaveSlotRegistry.GetName(slot));
		((UnityEvent<string>)(object)input.onValueChanged).AddListener((UnityAction<string>)delegate
		{
			FitNameWidth(input, layout);
		});
		((UnityEvent<string>)(object)input.onEndEdit).AddListener((UnityAction<string>)delegate(string value)
		{
			SaveSlotRegistry.SetName(slot, value);
			input.SetTextWithoutNotify(SaveSlotRegistry.GetName(slot));
			FitNameWidth(input, layout);
		});
		FitNameWidth(input, layout);
		UITooltip component = val.GetComponent<UITooltip>();
		component.skipLocale = true;
		component.tipName = "Rename";
		component.tipDesc = "Click and type to rename this slot.";
		val.SetActive(true);
		return input;
	}

	private static void FitNameWidth(TMP_InputField input, LayoutElement layout)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Expected O, but got Unknown
		float x = input.textComponent.GetPreferredValues(input.text + "_").x;
		layout.preferredWidth = Mathf.Clamp(x, 24f, 260f);
		LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)((Component)layout).transform.parent);
	}

	private TextMeshProUGUI CreateText(Transform parent, string name)
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Expected O, but got Unknown
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		GameObject val = new GameObject(name, new Type[2]
		{
			typeof(RectTransform),
			typeof(TextMeshProUGUI)
		});
		val.transform.SetParent(parent, false);
		TextMeshProUGUI component = val.GetComponent<TextMeshProUGUI>();
		if (Object.op_Implicit((Object)(object)font))
		{
			((TMP_Text)component).font = font;
		}
		((TMP_Text)component).fontSize = 18f;
		((Graphic)component).color = Color.white;
		((TMP_Text)component).alignment = (TextAlignmentOptions)4097;
		((TMP_Text)component).richText = true;
		return component;
	}

	private static UITooltip CreateIconButton(Transform parent, string spriteName, string tipName, string tipDesc, UnityAction action)
	{
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Expected O, but got Unknown
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_0110: Unknown result type (might be due to invalid IL or missing references)
		//IL_012a: Unknown result type (might be due to invalid IL or missing references)
		GameObject val = new GameObject("Btn_" + spriteName, new Type[3]
		{
			typeof(RectTransform),
			typeof(Image),
			typeof(Button)
		});
		val.transform.SetParent(parent, false);
		float num = 36f;
		RectTransform component = val.GetComponent<RectTransform>();
		component.sizeDelta = new Vector2(num, num);
		LayoutElement val2 = val.AddComponent<LayoutElement>();
		val2.minWidth = num;
		val2.minHeight = num;
		val2.preferredWidth = num;
		val2.preferredHeight = num;
		Image component2 = val.GetComponent<Image>();
		component2.sprite = Helper.LoadEmbeddedSprite(spriteName);
		component2.preserveAspect = true;
		((Graphic)component2).color = Color.white;
		Button component3 = val.GetComponent<Button>();
		ColorBlock colors = ((Selectable)component3).colors;
		((ColorBlock)(ref colors)).normalColor = Color.white;
		((ColorBlock)(ref colors)).highlightedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
		((ColorBlock)(ref colors)).pressedColor = new Color(0.6f, 0.6f, 0.6f, 1f);
		((ColorBlock)(ref colors)).colorMultiplier = 1f;
		((Selectable)component3).colors = colors;
		((UnityEvent)component3.onClick).AddListener(action);
		UITooltip val3 = val.AddComponent<UITooltip>();
		val3.skipLocale = true;
		val3.tipName = tipName;
		val3.tipDesc = tipDesc;
		return val3;
	}

	private void UpdateRow(int slot, Row row)
	{
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_039a: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_03dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ec: Unknown result type (might be due to invalid IL or missing references)
		if (!row.NameField.isFocused)
		{
			row.NameField.SetTextWithoutNotify((slot == 0) ? SaveSlotRegistry.DefaultName(0) : SaveSlotRegistry.GetName(slot));
			FitNameWidth(row.NameField, row.NameLayout);
		}
		string path = ((slot == 0) ? "autosave.sv" : SaveSlotRegistry.FileName(slot));
		string text = Path.Combine(SaveManager.SaveFolder, path);
		string path2 = text + ".meta";
		if (!File.Exists(text))
		{
			if (Object.op_Implicit((Object)(object)uiSprite))
			{
				row.Background.sprite = uiSprite;
			}
			row.Background.type = (Type)1;
			((Graphic)row.Background).color = new Color(0f, 0f, 0f, 0.5f);
			((Behaviour)row.BackgroundFitter).enabled = false;
			((TMP_Text)row.Tags).text = string.Empty;
			((TMP_Text)row.Details).text = "<size=70%><color=grey>Empty</color></size>";
			return;
		}
		string text2 = "Unknown";
		string text3 = "";
		string text4 = "";
		string text5 = "";
		string text6 = "";
		int key = -1;
		string text7 = string.Empty;
		try
		{
			byte[] array = File.ReadAllBytes(text);
			if (array.Length != 0)
			{
				string input = SaveSystem.Unzip(array);
				Match match = Regex.Match(input, "\"runTime\":\\s*([0-9.]+)");
				if (match.Success && float.TryParse(match.Groups[1].Value, out var result))
				{
					TimeSpan timeSpan = TimeSpan.FromSeconds(result);
					text6 = $"{timeSpan:hh\\:mm\\:ss}";
				}
			}
		}
		catch
		{
		}
		if (File.Exists(path2))
		{
			try
			{
				SaveMetadata saveMetadata = JsonConvert.DeserializeObject<SaveMetadata>(File.ReadAllText(path2));
				text2 = saveMetadata.LocationName;
				text5 = saveMetadata.Timestamp;
				key = saveMetadata.BiomeIndex;
				if (!string.IsNullOrWhiteSpace(saveMetadata.SeedInput))
				{
					text3 = " <color=yellow>[ID: " + saveMetadata.SeedInput.Trim() + "]</color>";
				}
				else if (saveMetadata.SeedValue != 0)
				{
					text3 = $" <color=yellow>[ID: {saveMetadata.SeedValue}]</color>";
				}
				if (saveMetadata.SaveKind == "MultiplayerHostBundle")
				{
					text4 = "<color=#66ccff>[MP Host]</color>";
				}
				else if (saveMetadata.SaveKind == "MultiplayerClientInventory")
				{
					text4 = "<color=#66ccff>[MP Client]</color>";
				}
				else if (saveMetadata.SavedDuringMultiplayer)
				{
					text4 = "<color=#66ccff>[MP]</color>";
				}
				text7 = (saveMetadata.LoadedFromMenu ? " *" : string.Empty);
			}
			catch
			{
			}
		}
		else
		{
			try
			{
				byte[] array2 = File.ReadAllBytes(text);
				if (array2.Length != 0)
				{
					string input2 = SaveSystem.Unzip(array2);
					Match match2 = Regex.Match(input2, "\"totalTraveled\":\\s*(\\d+)");
					if (match2.Success)
					{
						text2 = match2.Groups[1].Value + "m deep";
					}
				}
				text5 = new FileInfo(text).LastWriteTime.ToString("g");
			}
			catch
			{
				text2 = "Corrupt";
			}
		}
		bool flag = false;
		if (SaveSaveMenuPatcher.layerBackgrounds.TryGetValue(key, out var value) && (Object)(object)value != (Object)null)
		{
			row.Background.sprite = value;
			row.Background.type = (Type)0;
			((Graphic)row.Background).color = Color.white;
			((Behaviour)row.BackgroundFitter).enabled = true;
			Rect rect = value.rect;
			if (((Rect)(ref rect)).height > 0f)
			{
				AspectRatioFitter backgroundFitter = row.BackgroundFitter;
				rect = value.rect;
				float width = ((Rect)(ref rect)).width;
				rect = value.rect;
				backgroundFitter.aspectRatio = width / ((Rect)(ref rect)).height;
			}
			flag = true;
		}
		if (!flag)
		{
			if (Object.op_Implicit((Object)(object)uiSprite))
			{
				row.Background.sprite = uiSprite;
			}
			row.Background.type = (Type)1;
			((Behaviour)row.BackgroundFitter).enabled = false;
		}
		((TMP_Text)row.Tags).text = text4;
		((TMP_Text)row.Details).text = "<size=70%>" + text2 + " | " + text6 + " run time</size>\n<size=60%><color=grey>" + text5 + text7 + text3 + "</color></size>";
		if ((Object)(object)row.ModsTooltip != (Object)null)
		{
			string text8 = modsTipFor(slot);
			row.ModsTooltip.tipName = text8 ?? string.Empty;
			row.ModsButton.SetActive(!string.IsNullOrEmpty(text8));
		}
	}
}
