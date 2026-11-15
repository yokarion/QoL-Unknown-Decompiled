using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Expected O, but got Unknown
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Expected O, but got Unknown
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0115: Unknown result type (might be due to invalid IL or missing references)
		//IL_011a: Unknown result type (might be due to invalid IL or missing references)
		//IL_012c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0132: Unknown result type (might be due to invalid IL or missing references)
		//IL_0138: Expected O, but got Unknown
		//IL_0139: Unknown result type (might be due to invalid IL or missing references)
		//IL_0144: Unknown result type (might be due to invalid IL or missing references)
		//IL_0159: Unknown result type (might be due to invalid IL or missing references)
		//IL_0164: Unknown result type (might be due to invalid IL or missing references)
		//IL_0176: Unknown result type (might be due to invalid IL or missing references)
		//IL_0185: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c8: Expected O, but got Unknown
		//IL_01de: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e8: Expected O, but got Unknown
		//IL_01f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0208: Unknown result type (might be due to invalid IL or missing references)
		//IL_0222: Unknown result type (might be due to invalid IL or missing references)
		//IL_0232: Unknown result type (might be due to invalid IL or missing references)
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
		LayoutElement obj = val.AddComponent<LayoutElement>();
		obj.minHeight = 80f;
		obj.flexibleHeight = 1f;
		Scrollbar val2 = CreateScrollbar(Root);
		float num = (((Object)(object)val2 != (Object)null) ? (((RectTransform)((Component)val2).transform).sizeDelta.x + 6f) : 0f);
		GameObject val3 = new GameObject("Viewport", new Type[3]
		{
			typeof(RectTransform),
			typeof(Image),
			typeof(RectMask2D)
		});
		val3.transform.SetParent((Transform)(object)Root, false);
		RectTransform val4 = (RectTransform)val3.transform;
		val4.anchorMin = Vector2.zero;
		val4.anchorMax = Vector2.one;
		val4.pivot = new Vector2(0f, 1f);
		val4.offsetMin = Vector2.zero;
		val4.offsetMax = new Vector2(0f - num, 0f);
		((Graphic)val3.GetComponent<Image>()).color = Color.clear;
		GameObject val5 = new GameObject("Content", new Type[3]
		{
			typeof(RectTransform),
			typeof(VerticalLayoutGroup),
			typeof(ContentSizeFitter)
		});
		val5.transform.SetParent((Transform)(object)val4, false);
		content = (RectTransform)val5.transform;
		content.anchorMin = new Vector2(0f, 1f);
		content.anchorMax = Vector2.one;
		content.pivot = new Vector2(0.5f, 1f);
		content.sizeDelta = Vector2.zero;
		VerticalLayoutGroup component = val5.GetComponent<VerticalLayoutGroup>();
		((HorizontalOrVerticalLayoutGroup)component).spacing = 8f;
		((HorizontalOrVerticalLayoutGroup)component).childControlHeight = true;
		((HorizontalOrVerticalLayoutGroup)component).childControlWidth = true;
		((HorizontalOrVerticalLayoutGroup)component).childForceExpandHeight = false;
		((HorizontalOrVerticalLayoutGroup)component).childForceExpandWidth = true;
		val5.GetComponent<ContentSizeFitter>().verticalFit = (FitMode)2;
		ScrollRect component2 = val.GetComponent<ScrollRect>();
		component2.viewport = val4;
		component2.content = content;
		component2.horizontal = false;
		component2.vertical = true;
		component2.movementType = (MovementType)2;
		component2.scrollSensitivity = 30f;
		if ((Object)(object)val2 != (Object)null)
		{
			component2.verticalScrollbar = val2;
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

	public void ScrollToTop()
	{
		ScrollRect component = ((Component)Root).GetComponent<ScrollRect>();
		if ((Object)(object)component != (Object)null)
		{
			component.verticalNormalizedPosition = 1f;
		}
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
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Expected O, but got Unknown
		GameObject val = Resources.Load<GameObject>("Special/SettingsMenu");
		Transform val2 = (((Object)(object)val != (Object)null) ? val.transform.Find("Panel/Scroll/Scrollbar Vertical") : null);
		if ((Object)(object)val2 == (Object)null)
		{
			return null;
		}
		GameObject obj = Object.Instantiate<GameObject>(((Component)val2).gameObject, (Transform)(object)parent, false);
		((Object)obj).name = "Scrollbar";
		RectTransform val3 = (RectTransform)obj.transform;
		val3.anchorMin = new Vector2(1f, 0f);
		val3.anchorMax = Vector2.one;
		val3.pivot = new Vector2(1f, 1f);
		val3.anchoredPosition = Vector2.zero;
		val3.sizeDelta = new Vector2(val3.sizeDelta.x, 0f);
		Scrollbar component = obj.GetComponent<Scrollbar>();
		component.onValueChanged = new ScrollEvent();
		component.direction = (Direction)2;
		return component;
	}

	private Row CreateRow(int slot, Action<int> rowSave = null, Action<int> rowLoad = null, Action<int> rowDelete = null, bool excludeSave = false)
	{
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Expected O, but got Unknown
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0117: Unknown result type (might be due to invalid IL or missing references)
		//IL_011d: Expected O, but got Unknown
		//IL_0136: Unknown result type (might be due to invalid IL or missing references)
		//IL_0141: Unknown result type (might be due to invalid IL or missing references)
		//IL_014b: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_020f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0216: Unknown result type (might be due to invalid IL or missing references)
		//IL_0221: Unknown result type (might be due to invalid IL or missing references)
		//IL_022b: Unknown result type (might be due to invalid IL or missing references)
		//IL_024e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0270: Unknown result type (might be due to invalid IL or missing references)
		//IL_0277: Expected O, but got Unknown
		//IL_0292: Unknown result type (might be due to invalid IL or missing references)
		//IL_029d: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_02be: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c8: Expected O, but got Unknown
		//IL_030d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0314: Expected O, but got Unknown
		//IL_0385: Unknown result type (might be due to invalid IL or missing references)
		//IL_038c: Expected O, but got Unknown
		//IL_0489: Unknown result type (might be due to invalid IL or missing references)
		//IL_0490: Expected O, but got Unknown
		//IL_0513: Unknown result type (might be due to invalid IL or missing references)
		//IL_051d: Expected O, but got Unknown
		//IL_0558: Unknown result type (might be due to invalid IL or missing references)
		//IL_0562: Expected O, but got Unknown
		//IL_0588: Unknown result type (might be due to invalid IL or missing references)
		//IL_0592: Expected O, but got Unknown
		//IL_05b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_05c2: Expected O, but got Unknown
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
		LayoutElement obj = val.AddComponent<LayoutElement>();
		obj.minHeight = 80f;
		obj.preferredHeight = 80f;
		GameObject val2 = new GameObject("Background", new Type[3]
		{
			typeof(RectTransform),
			typeof(Image),
			typeof(AspectRatioFitter)
		});
		val2.transform.SetParent(val.transform, false);
		RectTransform component = val2.GetComponent<RectTransform>();
		component.anchorMin = Vector2.zero;
		component.anchorMax = Vector2.one;
		component.sizeDelta = Vector2.zero;
		row.Background = val2.GetComponent<Image>();
		if (Object.op_Implicit((Object)(object)uiSprite))
		{
			row.Background.sprite = uiSprite;
		}
		row.Background.type = (Type)1;
		((Graphic)row.Background).color = new Color(0f, 0f, 0f, 0.5f);
		row.BackgroundFitter = val2.GetComponent<AspectRatioFitter>();
		row.BackgroundFitter.aspectMode = (AspectMode)4;
		((Behaviour)row.BackgroundFitter).enabled = false;
		GameObject val3 = new GameObject("Dimmer", new Type[2]
		{
			typeof(RectTransform),
			typeof(Image)
		});
		val3.transform.SetParent(val.transform, false);
		RectTransform component2 = val3.GetComponent<RectTransform>();
		component2.anchorMin = Vector2.zero;
		component2.anchorMax = Vector2.one;
		component2.sizeDelta = Vector2.zero;
		((Graphic)val3.GetComponent<Image>()).color = new Color(0f, 0f, 0f, 0.6f);
		GameObject val4 = new GameObject("Content", new Type[1] { typeof(RectTransform) });
		val4.transform.SetParent(val.transform, false);
		RectTransform component3 = val4.GetComponent<RectTransform>();
		component3.anchorMin = Vector2.zero;
		component3.anchorMax = Vector2.one;
		component3.sizeDelta = Vector2.zero;
		HorizontalLayoutGroup obj2 = val4.AddComponent<HorizontalLayoutGroup>();
		((LayoutGroup)obj2).padding = new RectOffset(10, 5, 5, 5);
		((HorizontalOrVerticalLayoutGroup)obj2).spacing = 10f;
		((HorizontalOrVerticalLayoutGroup)obj2).childControlWidth = true;
		((HorizontalOrVerticalLayoutGroup)obj2).childForceExpandWidth = false;
		((HorizontalOrVerticalLayoutGroup)obj2).childControlHeight = true;
		((HorizontalOrVerticalLayoutGroup)obj2).childForceExpandHeight = false;
		((LayoutGroup)obj2).childAlignment = (TextAnchor)3;
		GameObject val5 = new GameObject("Info", new Type[1] { typeof(RectTransform) });
		val5.transform.SetParent(val4.transform, false);
		VerticalLayoutGroup obj3 = val5.AddComponent<VerticalLayoutGroup>();
		((LayoutGroup)obj3).childAlignment = (TextAnchor)3;
		((HorizontalOrVerticalLayoutGroup)obj3).childControlWidth = true;
		((HorizontalOrVerticalLayoutGroup)obj3).childControlHeight = true;
		((HorizontalOrVerticalLayoutGroup)obj3).childForceExpandWidth = true;
		((HorizontalOrVerticalLayoutGroup)obj3).childForceExpandHeight = false;
		LayoutElement obj4 = val5.AddComponent<LayoutElement>();
		obj4.minWidth = 320f;
		obj4.flexibleWidth = 1f;
		GameObject val6 = new GameObject("Title", new Type[1] { typeof(RectTransform) });
		val6.transform.SetParent(val5.transform, false);
		HorizontalLayoutGroup obj5 = val6.AddComponent<HorizontalLayoutGroup>();
		((HorizontalOrVerticalLayoutGroup)obj5).spacing = 6f;
		((LayoutGroup)obj5).childAlignment = (TextAnchor)3;
		((HorizontalOrVerticalLayoutGroup)obj5).childControlWidth = true;
		((HorizontalOrVerticalLayoutGroup)obj5).childControlHeight = true;
		((HorizontalOrVerticalLayoutGroup)obj5).childForceExpandWidth = false;
		((HorizontalOrVerticalLayoutGroup)obj5).childForceExpandHeight = false;
		LayoutElement obj6 = val6.AddComponent<LayoutElement>();
		obj6.minHeight = 24f;
		obj6.preferredHeight = 24f;
		row.NameField = CreateNameField(val6.transform, slot, out row.NameLayout);
		row.Tags = CreateText(val6.transform, "Tags");
		((TMP_Text)row.Tags).enableWordWrapping = false;
		((TMP_Text)row.Tags).overflowMode = (TextOverflowModes)1;
		((Component)row.Tags).gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
		row.Details = CreateText(val5.transform, "Details");
		GameObject val7 = new GameObject("Buttons", new Type[1] { typeof(RectTransform) });
		val7.transform.SetParent(val4.transform, false);
		HorizontalLayoutGroup obj7 = val7.AddComponent<HorizontalLayoutGroup>();
		((HorizontalOrVerticalLayoutGroup)obj7).spacing = 5f;
		((LayoutGroup)obj7).childAlignment = (TextAnchor)5;
		((HorizontalOrVerticalLayoutGroup)obj7).childForceExpandWidth = false;
		((HorizontalOrVerticalLayoutGroup)obj7).childControlWidth = false;
		((HorizontalOrVerticalLayoutGroup)obj7).childControlHeight = false;
		LayoutElement obj8 = val7.AddComponent<LayoutElement>();
		obj8.minWidth = 118f;
		obj8.preferredWidth = 118f;
		if (modsTipFor != null)
		{
			row.ModsTooltip = CreateIconButton(val7.transform, "question", "Mods", "Click to activate mods.", (UnityAction)delegate
			{
				onStageMods?.Invoke(slot);
			});
			row.ModsButton = ((Component)row.ModsTooltip).gameObject;
		}
		if (onSave != null)
		{
			CreateIconButton(val7.transform, "save", "Save", string.Empty, (UnityAction)delegate
			{
				onSave(slot);
			});
		}
		if (onLoad != null)
		{
			CreateIconButton(val7.transform, "load", "Load", string.Empty, (UnityAction)delegate
			{
				onLoad(slot);
			});
		}
		if (onDelete != null)
		{
			CreateIconButton(val7.transform, "delete", "Delete", string.Empty, (UnityAction)delegate
			{
				onDelete(slot);
			});
		}
		return row;
	}

	private TMP_InputField CreateNameField(Transform parent, int slot, out LayoutElement nameLayout)
	{
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Expected O, but got Unknown
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Expected O, but got Unknown
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Expected O, but got Unknown
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_012d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0138: Unknown result type (might be due to invalid IL or missing references)
		//IL_0143: Unknown result type (might be due to invalid IL or missing references)
		//IL_014d: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ef: Unknown result type (might be due to invalid IL or missing references)
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
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Expected O, but got Unknown
		float x = input.textComponent.GetPreferredValues(input.text + "_").x;
		layout.preferredWidth = Mathf.Clamp(x, 24f, 260f);
		LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)((Component)layout).transform.parent);
	}

	private TextMeshProUGUI CreateText(Transform parent, string name)
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
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
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_010c: Unknown result type (might be due to invalid IL or missing references)
		GameObject val = new GameObject("Btn_" + spriteName, new Type[3]
		{
			typeof(RectTransform),
			typeof(Image),
			typeof(Button)
		});
		val.transform.SetParent(parent, false);
		float num = 36f;
		val.GetComponent<RectTransform>().sizeDelta = new Vector2(num, num);
		LayoutElement obj = val.AddComponent<LayoutElement>();
		obj.minWidth = num;
		obj.minHeight = num;
		obj.preferredWidth = num;
		obj.preferredHeight = num;
		Image component = val.GetComponent<Image>();
		component.sprite = Helper.LoadEmbeddedSprite(spriteName);
		component.preserveAspect = true;
		((Graphic)component).color = Color.white;
		Button component2 = val.GetComponent<Button>();
		ColorBlock colors = ((Selectable)component2).colors;
		((ColorBlock)(ref colors)).normalColor = Color.white;
		((ColorBlock)(ref colors)).highlightedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
		((ColorBlock)(ref colors)).pressedColor = new Color(0.6f, 0.6f, 0.6f, 1f);
		((ColorBlock)(ref colors)).colorMultiplier = 1f;
		((Selectable)component2).colors = colors;
		((UnityEvent)component2.onClick).AddListener(action);
		UITooltip obj2 = val.AddComponent<UITooltip>();
		obj2.skipLocale = true;
		obj2.tipName = tipName;
		obj2.tipDesc = tipDesc;
		return obj2;
	}

	private void UpdateRow(int slot, Row row)
	{
		//IL_0308: Unknown result type (might be due to invalid IL or missing references)
		//IL_0320: Unknown result type (might be due to invalid IL or missing references)
		//IL_0325: Unknown result type (might be due to invalid IL or missing references)
		//IL_033d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0342: Unknown result type (might be due to invalid IL or missing references)
		//IL_034d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0352: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
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
				Match match = Regex.Match(SaveSystem.Unzip(array), "\"runTime\":\\s*([0-9.]+)");
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
					Match match2 = Regex.Match(SaveSystem.Unzip(array2), "\"totalTraveled\":\\s*(\\d+)");
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
			row.ModsTooltip.tipDesc = ((text8 != null) ? (text8 + "\n\nClick to activate mods.") : "Click to activate mods.");
			row.ModsButton.SetActive(!string.IsNullOrEmpty(text8));
		}
	}
}
