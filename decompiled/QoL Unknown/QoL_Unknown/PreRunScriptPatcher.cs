using System;
using BepInEx.Logging;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

namespace QoL_Unknown;

internal class PreRunScriptPatcher
{
	private sealed class CustomPresetButtonClickHandler : MonoBehaviour, IPointerClickHandler, IEventSystemHandler
	{
		private PreRunScript context;

		private int presetIndex;

		public void Configure(PreRunScript newContext, int newPresetIndex)
		{
			context = newContext;
			presetIndex = newPresetIndex;
		}

		public void OnPointerClick(PointerEventData eventData)
		{
			//IL_0005: Unknown result type (might be due to invalid IL or missing references)
			//IL_000b: Invalid comparison between Unknown and I4
			if (eventData != null && (int)eventData.button == 1)
			{
				DeletePreset(presetIndex);
				RebuildCustomPresetButtons(context);
			}
		}
	}

	private sealed class CustomSettingsHeaderRenameHandler : MonoBehaviour, IPointerClickHandler, IEventSystemHandler
	{
		private TextMeshProUGUI headerText;

		private TMP_InputField input;

		private string currentName = "Custom settings";

		public void Configure(TextMeshProUGUI newHeaderText)
		{
			headerText = newHeaderText;
			if (!((Object)(object)headerText == (Object)null))
			{
				((Graphic)headerText).raycastTarget = true;
				if (string.IsNullOrWhiteSpace(currentName) || currentName == "Type to rename")
				{
					currentName = (string.IsNullOrWhiteSpace(((TMP_Text)headerText).text) ? "Custom settings" : ((TMP_Text)headerText).text);
				}
				EnsureInput();
				if ((Object)(object)input != (Object)null && string.IsNullOrWhiteSpace(input.text))
				{
					input.SetTextWithoutNotify(currentName);
				}
				else if (string.IsNullOrWhiteSpace(((TMP_Text)headerText).text))
				{
					((TMP_Text)headerText).text = currentName;
				}
			}
		}

		public void OnPointerClick(PointerEventData eventData)
		{
			//IL_0005: Unknown result type (might be due to invalid IL or missing references)
			//IL_000b: Invalid comparison between Unknown and I4
			if (eventData != null && (int)eventData.button <= 0)
			{
				BeginRename();
			}
		}

		private void EnsureInput()
		{
			//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
			//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
			if (!((Object)(object)input != (Object)null) && !((Object)(object)headerText == (Object)null))
			{
				input = ((Component)this).gameObject.GetComponent<TMP_InputField>();
				if ((Object)(object)input == (Object)null)
				{
					input = ((Component)this).gameObject.AddComponent<TMP_InputField>();
				}
				input.textComponent = (TMP_Text)(object)headerText;
				input.textViewport = ((TMP_Text)headerText).rectTransform;
				input.lineType = (LineType)0;
				input.characterLimit = 48;
				input.caretColor = Color.white;
				input.selectionColor = new Color(0.35f, 0.62f, 1f, 0.45f);
				((UnityEvent<string>)(object)input.onEndEdit).RemoveListener((UnityAction<string>)FinishRename);
				((UnityEvent<string>)(object)input.onEndEdit).AddListener((UnityAction<string>)FinishRename);
			}
		}

		private void BeginRename()
		{
			EnsureInput();
			if (!((Object)(object)input == (Object)null) && !((Object)(object)headerText == (Object)null))
			{
				currentName = ((string.IsNullOrWhiteSpace(((TMP_Text)headerText).text) || string.Equals(((TMP_Text)headerText).text.Trim(), "Type to rename", StringComparison.OrdinalIgnoreCase)) ? "Custom settings" : ((TMP_Text)headerText).text);
				input.SetTextWithoutNotify("Type to rename");
				input.ActivateInputField();
				((Selectable)input).Select();
				input.caretPosition = 0;
				input.selectionAnchorPosition = 0;
				input.selectionFocusPosition = "Type to rename".Length;
			}
		}

		private void FinishRename(string value)
		{
			if (!((Object)(object)headerText == (Object)null))
			{
				string text = ((string.IsNullOrWhiteSpace(value) || string.Equals(value.Trim(), "Type to rename", StringComparison.OrdinalIgnoreCase)) ? currentName : value.Trim());
				currentName = (string.IsNullOrWhiteSpace(text) ? "Custom settings" : text);
				if ((Object)(object)input != (Object)null)
				{
					input.SetTextWithoutNotify(currentName);
				}
				else
				{
					((TMP_Text)headerText).text = currentName;
				}
			}
		}
	}

	private static ManualLogSource Logger;

	private const string AlwaysSkipLorePrefKey = "AlwaysSkipLore";

	private const string PresetCountPrefKey = "QoL_CustomSettingsPreset_Count";

	private const string PresetSlotPrefKeyPrefix = "QoL_CustomSettingsPreset_";

	private const string PresetNamePrefKeyPrefix = "QoL_CustomSettingsPresetName_";

	private const string PresetButtonRootName = "QoLCustomSettingsPresetButtons";

	private const string DefaultCustomSettingsHeader = "Custom settings";

	private const string DefaultPresetNamePrefix = "Custom Setting ";

	private const string HeaderRenamePrompt = "Type to rename";

	private const float PresetColumnXOffset = 325f;

	private const int VisiblePresetSlots = 5;

	private static int presetPageStartIndex;

	private static bool alwaysSkipLore;

	internal static bool IsAlwaysSkipLoreEnabled => alwaysSkipLore;

	internal static bool ToggleAlwaysSkipLore()
	{
		alwaysSkipLore = !alwaysSkipLore;
		PlayerPrefs.SetInt("AlwaysSkipLore", alwaysSkipLore ? 1 : 0);
		PlayerPrefs.Save();
		if (alwaysSkipLore)
		{
			ScrollableText.ForceClose();
		}
		return alwaysSkipLore;
	}

	public static void Initialize(ManualLogSource logger)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Expected O, but got Unknown
		Logger = logger;
		Harmony val = new Harmony("org.bepinex.plugins.qol.prerun");
		val.PatchAll(typeof(PreRunScriptPatcher));
		alwaysSkipLore = PlayerPrefs.GetInt("AlwaysSkipLore", 0) == 1;
		Logger.LogInfo((object)"PreRunScriptPatcher initialized");
	}

	[HarmonyPatch(typeof(Locale), "GetString")]
	[HarmonyPrefix]
	private static bool GetString_Prefix(string str, ref string __result)
	{
		if (str == "clicktocontinue")
		{
			__result = "Click to continue...\n Or, click RMB/Esc to instantly close.";
			return false;
		}
		return true;
	}

	[HarmonyPatch(typeof(ScrollableText), "Update")]
	[HarmonyPrefix]
	private static bool Update_Prefix(ScrollableText __instance)
	{
		if (Input.GetKeyDown((KeyCode)27) || Input.GetMouseButtonDown(1))
		{
			Object.Destroy((Object)(object)((Component)__instance).gameObject);
			return false;
		}
		return true;
	}

	[HarmonyPatch(typeof(PreRunScript), "TryLore")]
	[HarmonyPrefix]
	private static bool TryLore_Prefix()
	{
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		GameObject val = GameObject.Find("GlobalDark(Clone)/betabuild");
		if ((Object)(object)val != (Object)null)
		{
			TMP_Text component = val.GetComponent<TMP_Text>();
			if ((Object)(object)component != (Object)null)
			{
				string text = component.text ?? string.Empty;
				if (!text.Contains(" modded"))
				{
					component.text = text.Replace("This is a ", "This is a modded ");
				}
				Color color = ((Graphic)component).color;
				color.a = 0.0227f;
				color.r = Mathf.Min(1f, color.r + 0.08f);
				((Graphic)component).color = color;
			}
		}
		if (alwaysSkipLore)
		{
			return false;
		}
		return true;
	}

	[HarmonyPatch(typeof(PreRunScript), "StartRun")]
	[HarmonyPrefix]
	private static void StartRun_Prefix(PreRunScript __instance)
	{
		if (!SeedManager.IsSeeded)
		{
			SeedManager.SetSeed(Random.Range(int.MinValue, int.MaxValue).ToString());
			SeedManager.InputString = "";
			SeedManager.IsSeeded = true;
			Logger.LogInfo((object)$"Random seeded run started with seed: {SeedManager.CurrentSeed}");
		}
	}

	[HarmonyPatch(typeof(PreRunScript), "Start")]
	[HarmonyPostfix]
	private static void ExtendSliders_Postfix(PreRunScript __instance)
	{
		foreach (RunSettingDisplay runSettingObject in __instance.runSettingObjects)
		{
			string name = runSettingObject.associated.name;
			if (!(name != "baselootdensity") || !(name != "basetrapdensity") || !(name != "timelimit"))
			{
				Slider component = ((Component)((Component)runSettingObject).transform.GetChild(1)).GetComponent<Slider>();
				if (name == "timelimit")
				{
					component.minValue = 1f;
					continue;
				}
				component.minValue = 0f;
				component.maxValue = 5f;
			}
		}
		RebuildCustomPresetButtons(__instance);
		EnsureEditableCustomSettingsHeader(__instance);
	}

	[HarmonyPatch(typeof(PreRunScript), "CopyToClipboard")]
	[HarmonyPostfix]
	private static void CopyToClipboard_Postfix(PreRunScript __instance)
	{
		string systemCopyBuffer = GUIUtility.systemCopyBuffer;
		if (!string.IsNullOrWhiteSpace(systemCopyBuffer))
		{
			AppendPreset(systemCopyBuffer, GetCurrentCustomSettingsHeader(__instance));
			RebuildCustomPresetButtons(__instance);
		}
	}

	private static void AppendPreset(string code, string presetName)
	{
		int num = Mathf.Max(0, PlayerPrefs.GetInt("QoL_CustomSettingsPreset_Count", 0));
		PlayerPrefs.SetString(GetPresetSlotKey(num), code);
		PlayerPrefs.SetString(GetPresetNameKey(num), NormalizePresetName(presetName, num));
		PlayerPrefs.SetInt("QoL_CustomSettingsPreset_Count", num + 1);
		PlayerPrefs.Save();
		presetPageStartIndex = Mathf.Max(0, num - 5 + 1);
	}

	private static void DeletePreset(int index)
	{
		int num = Mathf.Max(0, PlayerPrefs.GetInt("QoL_CustomSettingsPreset_Count", 0));
		if (index >= 0 && index < num)
		{
			for (int i = index; i < num - 1; i++)
			{
				PlayerPrefs.SetString(GetPresetSlotKey(i), PlayerPrefs.GetString(GetPresetSlotKey(i + 1), string.Empty));
				PlayerPrefs.SetString(GetPresetNameKey(i), PlayerPrefs.GetString(GetPresetNameKey(i + 1), string.Empty));
			}
			PlayerPrefs.DeleteKey(GetPresetSlotKey(num - 1));
			PlayerPrefs.DeleteKey(GetPresetNameKey(num - 1));
			PlayerPrefs.SetInt("QoL_CustomSettingsPreset_Count", num - 1);
			PlayerPrefs.Save();
			presetPageStartIndex = Mathf.Clamp(presetPageStartIndex, 0, Mathf.Max(0, num - 1 - 5));
		}
	}

	private static void ApplyPreset(PreRunScript context, int index)
	{
		if (!((Object)(object)context == (Object)null))
		{
			string text = PlayerPrefs.GetString(GetPresetSlotKey(index), string.Empty);
			if (!string.IsNullOrWhiteSpace(text))
			{
				string systemCopyBuffer = GUIUtility.systemCopyBuffer;
				GUIUtility.systemCopyBuffer = text;
				context.PasteFromClipboard();
				GUIUtility.systemCopyBuffer = systemCopyBuffer;
			}
		}
	}

	private static string GetPresetSlotKey(int index)
	{
		return "QoL_CustomSettingsPreset_" + index;
	}

	private static string GetPresetNameKey(int index)
	{
		return "QoL_CustomSettingsPresetName_" + index;
	}

	private static string GetCurrentCustomSettingsHeader(PreRunScript context)
	{
		TextMeshProUGUI val = FindCustomSettingsHeaderText(context);
		return ((Object)(object)val != (Object)null) ? ((TMP_Text)val).text : "Custom settings";
	}

	private static string NormalizePresetName(string presetName, int index)
	{
		string text = (string.IsNullOrWhiteSpace(presetName) ? string.Empty : presetName.Trim());
		if (string.IsNullOrWhiteSpace(text) || string.Equals(text, "Type to rename", StringComparison.OrdinalIgnoreCase))
		{
			return "Custom Setting " + (index + 1);
		}
		return text;
	}

	private static void EnsureEditableCustomSettingsHeader(PreRunScript context)
	{
		TextMeshProUGUI val = FindCustomSettingsHeaderText(context);
		if ((Object)(object)val == (Object)null)
		{
			return;
		}
		RectTransform component = ((Component)val).GetComponent<RectTransform>();
		if (!((Object)(object)component == (Object)null))
		{
			CustomSettingsHeaderRenameHandler customSettingsHeaderRenameHandler = ((Component)val).GetComponent<CustomSettingsHeaderRenameHandler>();
			if ((Object)(object)customSettingsHeaderRenameHandler == (Object)null)
			{
				customSettingsHeaderRenameHandler = ((Component)val).gameObject.AddComponent<CustomSettingsHeaderRenameHandler>();
			}
			customSettingsHeaderRenameHandler.Configure(val);
		}
	}

	private static TextMeshProUGUI FindCustomSettingsHeaderText(PreRunScript context)
	{
		if ((Object)(object)context == (Object)null || (Object)(object)context.customSettingsMenu == (Object)null)
		{
			return null;
		}
		Transform val = ((Transform)context.customSettingsMenu).Find("RunSettings");
		if ((Object)(object)val == (Object)null)
		{
			return null;
		}
		return ((Component)val).GetComponent<TextMeshProUGUI>() ?? ((Component)val).GetComponentInChildren<TextMeshProUGUI>(true);
	}

	private static void RebuildCustomPresetButtons(PreRunScript context)
	{
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Expected O, but got Unknown
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_010a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0127: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)context == (Object)null || (Object)(object)context.customSettingsMenu == (Object)null)
		{
			return;
		}
		Transform val = FindExistingPresetButtonRoot(context);
		if ((Object)(object)val != (Object)null)
		{
			Object.Destroy((Object)(object)((Component)val).gameObject);
		}
		int num = Mathf.Max(0, PlayerPrefs.GetInt("QoL_CustomSettingsPreset_Count", 0));
		if (num > 0)
		{
			presetPageStartIndex = Mathf.Clamp(presetPageStartIndex, 0, Mathf.Max(0, num - 5));
			GameObject val2 = new GameObject("QoLCustomSettingsPresetButtons", new Type[1] { typeof(RectTransform) });
			val2.transform.SetParent((Transform)(object)context.customSettingsMenu, false);
			RectTransform component = val2.GetComponent<RectTransform>();
			component.anchorMin = new Vector2(0f, 1f);
			component.anchorMax = new Vector2(0f, 1f);
			component.pivot = new Vector2(0.5f, 1f);
			component.sizeDelta = new Vector2(64f, 384f);
			component.anchoredPosition = new Vector2(GetCustomSettingsPresetColumnCenterX(context) + 325f, -10f);
			Sprite buttonSprite = FindPresetButtonSprite(context);
			TMP_FontAsset font = FindPresetButtonFont(context);
			int num2 = 0;
			if (presetPageStartIndex > 0)
			{
				CreatePresetControl(context, val2.transform, buttonSprite, font, num2, "^", -1, isPageControl: true);
				num2++;
			}
			int num3 = Mathf.Min(5, num - presetPageStartIndex);
			bool flag = presetPageStartIndex + num3 < num;
			for (int i = 0; i < num3; i++)
			{
				int num4 = presetPageStartIndex + i;
				CreatePresetControl(context, val2.transform, buttonSprite, font, num2, (num4 + 1).ToString(), num4, isPageControl: false);
				num2++;
			}
			if (flag)
			{
				CreatePresetControl(context, val2.transform, buttonSprite, font, num2, "v", -2, isPageControl: true);
			}
		}
	}

	private static Transform FindExistingPresetButtonRoot(PreRunScript context)
	{
		if ((Object)(object)context == (Object)null || (Object)(object)context.customSettingsMenu == (Object)null)
		{
			return null;
		}
		Transform val = ((Transform)context.customSettingsMenu).Find("QoLCustomSettingsPresetButtons");
		if ((Object)(object)val != (Object)null)
		{
			return val;
		}
		Transform[] componentsInChildren = ((Component)context.customSettingsMenu).GetComponentsInChildren<Transform>(true);
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			if ((Object)(object)componentsInChildren[i] != (Object)null && ((Object)componentsInChildren[i]).name == "QoLCustomSettingsPresetButtons")
			{
				return componentsInChildren[i];
			}
		}
		return null;
	}

	private static float GetCustomSettingsPresetColumnCenterX(PreRunScript context)
	{
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		RectTransform customSettingsMenu = context.customSettingsMenu;
		Transform obj = ((Transform)context.customSettingsMenu).Find("Panel/Arrow");
		object obj2 = ((obj is RectTransform) ? obj : null);
		if (obj2 == null)
		{
			Transform obj3 = ((Transform)context.customSettingsMenu).Find("Panel");
			obj2 = ((obj3 is RectTransform) ? obj3 : null);
		}
		RectTransform val = (RectTransform)obj2;
		if ((Object)(object)customSettingsMenu == (Object)null || (Object)(object)val == (Object)null)
		{
			return 0f;
		}
		Vector3[] array = (Vector3[])(object)new Vector3[4];
		val.GetWorldCorners(array);
		Vector3 val2 = (array[0] + array[2]) * 0.5f;
		return ((Transform)customSettingsMenu).InverseTransformPoint(val2).x;
	}

	private static Sprite FindPresetButtonSprite(PreRunScript context)
	{
		if (context.presetRects != null)
		{
			for (int i = 0; i < context.presetRects.Count; i++)
			{
				Image val = (((Object)(object)context.presetRects[i] != (Object)null) ? ((Component)context.presetRects[i]).GetComponent<Image>() : null);
				if ((Object)(object)val != (Object)null && (Object)(object)val.sprite != (Object)null)
				{
					return val.sprite;
				}
			}
		}
		return ResourceLookupCache.FindSpriteContains("uiBlockNano");
	}

	private static TMP_FontAsset FindPresetButtonFont(PreRunScript context)
	{
		if (context.presetRects != null)
		{
			for (int i = 0; i < context.presetRects.Count; i++)
			{
				TextMeshProUGUI val = (((Object)(object)context.presetRects[i] != (Object)null) ? ((Component)context.presetRects[i]).GetComponentInChildren<TextMeshProUGUI>(true) : null);
				if ((Object)(object)val != (Object)null && (Object)(object)((TMP_Text)val).font != (Object)null)
				{
					return ((TMP_Text)val).font;
				}
			}
		}
		return ResourceLookupCache.FindFontContains("Retro GamingPix");
	}

	private static void CreatePresetControl(PreRunScript context, Transform parent, Sprite buttonSprite, TMP_FontAsset font, int row, string text, int presetIndex, bool isPageControl)
	{
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Expected O, but got Unknown
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Expected O, but got Unknown
		//IL_0121: Unknown result type (might be due to invalid IL or missing references)
		//IL_012e: Unknown result type (might be due to invalid IL or missing references)
		//IL_013b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0148: Unknown result type (might be due to invalid IL or missing references)
		//IL_0193: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0207: Unknown result type (might be due to invalid IL or missing references)
		//IL_021e: Unknown result type (might be due to invalid IL or missing references)
		//IL_023a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0247: Unknown result type (might be due to invalid IL or missing references)
		//IL_029e: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a8: Expected O, but got Unknown
		//IL_027e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0288: Expected O, but got Unknown
		GameObject val = new GameObject("QoLCustomSettingsPresetButton", new Type[3]
		{
			typeof(RectTransform),
			typeof(Image),
			typeof(Button)
		});
		val.transform.SetParent(parent, false);
		((Object)val).name = (isPageControl ? "QoLCustomSettingsPresetPage" : ("QoLCustomSettingsPreset_" + presetIndex));
		val.layer = ((Component)parent).gameObject.layer;
		Image component = val.GetComponent<Image>();
		component.sprite = buttonSprite;
		component.type = (Type)(((Object)(object)buttonSprite != (Object)null) ? 1 : 0);
		((Graphic)component).color = Color.white;
		((Graphic)component).raycastTarget = true;
		GameObject val2 = new GameObject("Label", new Type[2]
		{
			typeof(RectTransform),
			typeof(TextMeshProUGUI)
		});
		val2.transform.SetParent(val.transform, false);
		val2.layer = val.layer;
		RectTransform component2 = val2.GetComponent<RectTransform>();
		component2.anchorMin = Vector2.zero;
		component2.anchorMax = Vector2.one;
		component2.offsetMin = Vector2.zero;
		component2.offsetMax = Vector2.zero;
		TextMeshProUGUI component3 = val2.GetComponent<TextMeshProUGUI>();
		((TMP_Text)component3).text = text;
		((TMP_Text)component3).font = font;
		((TMP_Text)component3).fontSize = 24f;
		((TMP_Text)component3).enableAutoSizing = false;
		((TMP_Text)component3).alignment = (TextAlignmentOptions)514;
		((Graphic)component3).color = Color.white;
		((Graphic)component3).raycastTarget = false;
		if (!isPageControl)
		{
			SaveSlotButtonTextEnforcer saveSlotButtonTextEnforcer = val2.AddComponent<SaveSlotButtonTextEnforcer>();
			saveSlotButtonTextEnforcer.textToEnforce = text;
		}
		RectTransform component4 = val.GetComponent<RectTransform>();
		component4.anchorMin = new Vector2(0.5f, 1f);
		component4.anchorMax = new Vector2(0.5f, 1f);
		component4.pivot = new Vector2(0.5f, 1f);
		component4.sizeDelta = new Vector2(56f, 56f);
		component4.anchoredPosition = new Vector2(0f, (float)(-row) * 64f);
		((Transform)component4).localScale = Vector3.one;
		Button component5 = val.GetComponent<Button>();
		((UnityEventBase)component5.onClick).RemoveAllListeners();
		if (isPageControl)
		{
			((UnityEvent)component5.onClick).AddListener((UnityAction)delegate
			{
				if (presetIndex == -1)
				{
					presetPageStartIndex = Mathf.Max(0, presetPageStartIndex - 5 + 1);
				}
				else
				{
					presetPageStartIndex += 4;
				}
				RebuildCustomPresetButtons(context);
			});
			return;
		}
		((UnityEvent)component5.onClick).AddListener((UnityAction)delegate
		{
			ApplyPreset(context, presetIndex);
		});
		CustomPresetButtonClickHandler customPresetButtonClickHandler = val.GetComponent<CustomPresetButtonClickHandler>();
		if ((Object)(object)customPresetButtonClickHandler == (Object)null)
		{
			customPresetButtonClickHandler = val.AddComponent<CustomPresetButtonClickHandler>();
		}
		customPresetButtonClickHandler.Configure(context, presetIndex);
		UITooltip val3 = val.GetComponent<UITooltip>();
		if ((Object)(object)val3 == (Object)null)
		{
			val3 = val.AddComponent<UITooltip>();
		}
		val3.skipLocale = true;
		val3.tipName = PlayerPrefs.GetString(GetPresetNameKey(presetIndex), $"Custom Settings Preset {presetIndex + 1}");
		val3.tipDesc = "Right click to delete";
	}
}
