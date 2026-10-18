using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace QoL_Unknown;

internal sealed class SettingsMenuSearchController : MonoBehaviour
{
	[HarmonyPatch(typeof(SettingsMenu), "Start")]
	private static class SettingsMenuStartPatch
	{
		[HarmonyPostfix]
		[HarmonyAfter(new string[] { "net.cucorelib" })]
		private static void Postfix(SettingsMenu __instance)
		{
			EnsureAttached(__instance);
			(((Object)(object)__instance != (Object)null) ? ((Component)__instance).GetComponent<SettingsMenuSearchController>() : null)?.QueueRefresh();
		}
	}

	[HarmonyPatch(typeof(SettingsMenu), "SelectTab", new Type[] { typeof(SettingCategory) })]
	private static class SettingsMenuSelectTabPatch
	{
		[HarmonyPostfix]
		[HarmonyAfter(new string[] { "net.cucorelib" })]
		private static void Postfix(SettingsMenu __instance, SettingCategory category)
		{
			//IL_002d: Unknown result type (might be due to invalid IL or missing references)
			if (!suppressSearchRefresh)
			{
				EnsureAttached(__instance);
				(((Object)(object)__instance != (Object)null) ? ((Component)__instance).GetComponent<SettingsMenuSearchController>() : null)?.OnTabSelected(category);
			}
		}
	}

	[HarmonyPatch(typeof(SettingsMenu), "ResetToDefault")]
	private static class SettingsMenuResetPatch
	{
		[HarmonyPostfix]
		private static void Postfix(SettingsMenu __instance)
		{
			EnsureAttached(__instance);
			(((Object)(object)__instance != (Object)null) ? ((Component)__instance).GetComponent<SettingsMenuSearchController>() : null)?.QueueRefresh();
		}
	}

	private sealed class SearchResultDefinition
	{
		public Setting Setting;

		public ModCustomSettingDefinition CustomDefinition;

		public string CategoryName;

		public string SortLabel;
	}

	private const string HarmonyId = "org.bepinex.plugins.qol_unknown.settingssearch";

	private const string SearchRootName = "QoL_SettingsSearchRoot";

	private const string SearchInputName = "QoL_SettingsSearchInput";

	private const string SearchEmptyName = "QoL_SettingsSearchEmpty";

	private const string HeaderPrefix = "QoL_SettingsSearchHeader_";

	private const string SearchPrompt = "Search settings...";

	private const float HeaderHeight = 28f;

	private static readonly FieldRef<SettingsMenu, List<GameObject>> SpawnedSettingsRef = AccessTools.FieldRefAccess<SettingsMenu, List<GameObject>>("spawnedSettings");

	private static readonly Dictionary<int, string> BuiltInCategoryNames = new Dictionary<int, string>
	{
		{ 2, "Gameplay" },
		{ 0, "Video" },
		{ 1, "Audio" },
		{ 3, "Controls" },
		{ 4, "Language" }
	};

	private static ManualLogSource logger;

	private static bool initialized;

	private static bool suppressSearchRefresh;

	private static string searchText = string.Empty;

	private static MethodInfo cuCoreLibTryGetOwnedCustomCategoryMethod;

	private static PropertyInfo cuCoreLibDisplayNameProperty;

	private SettingsMenu menu;

	private TMP_InputField inputField;

	private TextMeshProUGUI emptyStateText;

	private int activeCategoryIndex = 0;

	private bool searchModeActive;

	private bool refreshQueued;

	internal static void Initialize(ManualLogSource log)
	{
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Expected O, but got Unknown
		logger = log;
		if (!initialized)
		{
			initialized = true;
			CacheCuCoreLibReflection();
			Harmony val = new Harmony("org.bepinex.plugins.qol_unknown.settingssearch");
			val.PatchAll(typeof(SettingsMenuSearchController));
		}
	}

	private void LateUpdate()
	{
		if (refreshQueued)
		{
			refreshQueued = false;
			RefreshSearchState();
		}
	}

	internal static void EnsureAttached(SettingsMenu menu)
	{
		if (!((Object)(object)menu == (Object)null))
		{
			SettingsMenuSearchController settingsMenuSearchController = ((Component)menu).GetComponent<SettingsMenuSearchController>();
			if ((Object)(object)settingsMenuSearchController == (Object)null)
			{
				settingsMenuSearchController = ((Component)menu).gameObject.AddComponent<SettingsMenuSearchController>();
			}
			settingsMenuSearchController.InitializeInstance(menu);
		}
	}

	internal static void RefreshLiveMenu()
	{
		if (!((Object)(object)SettingsMenu.instance == (Object)null))
		{
			EnsureAttached(SettingsMenu.instance);
			((Component)SettingsMenu.instance).GetComponent<SettingsMenuSearchController>()?.QueueRefresh();
		}
	}

	private static void CacheCuCoreLibReflection()
	{
		Type type = AccessTools.TypeByName("CUCoreLib.Registries.ModOptionsRegistry");
		Type type2 = AccessTools.TypeByName("CUCoreLib.Registries.ModOptionCategoryEntry");
		cuCoreLibTryGetOwnedCustomCategoryMethod = ((type != null) ? AccessTools.Method(type, "TryGetOwnedCustomCategory", new Type[2]
		{
			typeof(SettingCategory),
			type2?.MakeByRefType()
		}, (Type[])null) : null);
		cuCoreLibDisplayNameProperty = type2?.GetProperty("DisplayName", AccessTools.all);
	}

	private void InitializeInstance(SettingsMenu settingsMenu)
	{
		menu = settingsMenu;
		EnsureSearchUi();
		SyncInputText();
	}

	private void QueueRefresh()
	{
		refreshQueued = true;
	}

	private void RefreshSearchState()
	{
		if ((Object)(object)menu == (Object)null || (Object)(object)menu.content == (Object)null)
		{
			refreshQueued = (Object)(object)menu != (Object)null;
			return;
		}
		EnsureSearchUi();
		SyncInputText();
		string text = (searchText ?? string.Empty).Trim();
		if (string.IsNullOrEmpty(text))
		{
			if (searchModeActive)
			{
				RestoreNormalTab();
			}
			SetEmptyStateVisible(visible: false);
		}
		else
		{
			BuildSearchResults(text);
		}
	}

	private void RestoreNormalTab()
	{
		if ((Object)(object)menu == (Object)null)
		{
			return;
		}
		searchModeActive = false;
		suppressSearchRefresh = true;
		try
		{
			menu.SelectTab((SettingCategory)activeCategoryIndex);
		}
		finally
		{
			suppressSearchRefresh = false;
		}
	}

	private void EnsureSearchUi()
	{
		if ((Object)(object)menu == (Object)null)
		{
			return;
		}
		Transform val = ((Component)menu).transform.Find("QoL_SettingsSearchRoot");
		if ((Object)(object)val == (Object)null)
		{
			CreateSearchUi();
			val = ((Component)menu).transform.Find("QoL_SettingsSearchRoot");
		}
		if (!((Object)(object)val == (Object)null))
		{
			inputField = ((Component)val).GetComponentInChildren<TMP_InputField>(true);
			object obj;
			if (!((Object)(object)menu.content != (Object)null))
			{
				obj = null;
			}
			else
			{
				Transform obj2 = ((Transform)menu.content).Find("QoL_SettingsSearchEmpty");
				obj = ((obj2 != null) ? ((Component)obj2).GetComponent<TextMeshProUGUI>() : null);
			}
			emptyStateText = (TextMeshProUGUI)obj;
			if ((Object)(object)inputField != (Object)null)
			{
				((UnityEvent<string>)(object)inputField.onValueChanged).RemoveListener((UnityAction<string>)OnSearchValueChanged);
				((UnityEvent<string>)(object)inputField.onValueChanged).AddListener((UnityAction<string>)OnSearchValueChanged);
			}
		}
	}

	private void CreateSearchUi()
	{
		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00eb: Expected O, but got Unknown
		//IL_0117: Unknown result type (might be due to invalid IL or missing references)
		//IL_012e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0145: Unknown result type (might be due to invalid IL or missing references)
		//IL_015c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0173: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_0200: Unknown result type (might be due to invalid IL or missing references)
		//IL_0207: Expected O, but got Unknown
		//IL_0226: Unknown result type (might be due to invalid IL or missing references)
		//IL_0233: Unknown result type (might be due to invalid IL or missing references)
		//IL_024a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0261: Unknown result type (might be due to invalid IL or missing references)
		//IL_028b: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_02cb: Expected O, but got Unknown
		//IL_02eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0305: Unknown result type (might be due to invalid IL or missing references)
		//IL_0312: Unknown result type (might be due to invalid IL or missing references)
		//IL_033e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0370: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_043d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0444: Expected O, but got Unknown
		//IL_0472: Unknown result type (might be due to invalid IL or missing references)
		//IL_0489: Unknown result type (might be due to invalid IL or missing references)
		//IL_04a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_04b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_04ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_0528: Unknown result type (might be due to invalid IL or missing references)
		//IL_055e: Unknown result type (might be due to invalid IL or missing references)
		//IL_056d: Unknown result type (might be due to invalid IL or missing references)
		//IL_057c: Unknown result type (might be due to invalid IL or missing references)
		//IL_058b: Unknown result type (might be due to invalid IL or missing references)
		//IL_059a: Unknown result type (might be due to invalid IL or missing references)
		//IL_05a9: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)menu == (Object)null) && !((Object)(object)menu.content == (Object)null))
		{
			Button val = ((menu.buttons != null) ? menu.buttons.FirstOrDefault((Button button) => (Object)(object)button != (Object)null) : null);
			object obj;
			if (!((Object)(object)val != (Object)null))
			{
				obj = ResourceLookupCache.FindFontContains("Retro GamingPix");
			}
			else
			{
				TextMeshProUGUI componentInChildren = ((Component)val).GetComponentInChildren<TextMeshProUGUI>(true);
				obj = ((componentInChildren != null) ? ((TMP_Text)componentInChildren).font : null);
			}
			TMP_FontAsset font = (TMP_FontAsset)obj;
			object obj2;
			if (!((Object)(object)val != (Object)null))
			{
				obj2 = ResourceLookupCache.FindSpriteContains("uiBlockNano");
			}
			else
			{
				Image component = ((Component)val).GetComponent<Image>();
				obj2 = ((component != null) ? component.sprite : null);
			}
			Sprite val2 = (Sprite)obj2;
			GameObject val3 = new GameObject("QoL_SettingsSearchRoot", new Type[2]
			{
				typeof(RectTransform),
				typeof(Image)
			});
			val3.transform.SetParent(((Component)menu).transform, false);
			RectTransform component2 = val3.GetComponent<RectTransform>();
			component2.anchorMin = new Vector2(0.5f, 1f);
			component2.anchorMax = new Vector2(0.5f, 1f);
			component2.pivot = new Vector2(0.5f, 1f);
			component2.sizeDelta = new Vector2(470f, 36f);
			component2.anchoredPosition = new Vector2(0f, -92f);
			Image component3 = val3.GetComponent<Image>();
			component3.sprite = val2;
			component3.type = (Type)(((Object)(object)val2 != (Object)null) ? 1 : 0);
			((Graphic)component3).color = new Color(1f, 1f, 1f, 0.96f);
			((Graphic)component3).raycastTarget = false;
			GameObject val4 = new GameObject("QoL_SettingsSearchInput", new Type[3]
			{
				typeof(RectTransform),
				typeof(Image),
				typeof(TMP_InputField)
			});
			val4.transform.SetParent(val3.transform, false);
			RectTransform component4 = val4.GetComponent<RectTransform>();
			component4.anchorMin = Vector2.zero;
			component4.anchorMax = Vector2.one;
			component4.offsetMin = new Vector2(12f, 4f);
			component4.offsetMax = new Vector2(-12f, -4f);
			Image component5 = val4.GetComponent<Image>();
			((Graphic)component5).color = new Color(0f, 0f, 0f, 0.18f);
			((Graphic)component5).raycastTarget = true;
			GameObject val5 = new GameObject("Text Area", new Type[2]
			{
				typeof(RectTransform),
				typeof(RectMask2D)
			});
			val5.transform.SetParent(val4.transform, false);
			RectTransform component6 = val5.GetComponent<RectTransform>();
			component6.anchorMin = Vector2.zero;
			component6.anchorMax = Vector2.one;
			component6.offsetMin = Vector2.zero;
			component6.offsetMax = Vector2.zero;
			TextMeshProUGUI textComponent = CreateInputText(val5.transform, "Text", string.Empty, font, new Color(0.18f, 0.2f, 0.24f));
			TextMeshProUGUI placeholder = CreateInputText(val5.transform, "Placeholder", "Search settings...", font, new Color(0.18f, 0.2f, 0.24f, 0.5f));
			TMP_InputField component7 = val4.GetComponent<TMP_InputField>();
			component7.textViewport = component6;
			component7.textComponent = (TMP_Text)(object)textComponent;
			component7.placeholder = (Graphic)(object)placeholder;
			((Selectable)component7).targetGraphic = (Graphic)(object)component5;
			component7.lineType = (LineType)0;
			component7.characterLimit = 80;
			component7.selectionColor = new Color(0.35f, 0.62f, 1f, 0.32f);
			component7.caretColor = Color.white;
			component7.customCaretColor = true;
			((UnityEvent<string>)(object)component7.onValueChanged).AddListener((UnityAction<string>)OnSearchValueChanged);
			inputField = component7;
			GameObject val6 = new GameObject("QoL_SettingsSearchEmpty", new Type[2]
			{
				typeof(RectTransform),
				typeof(TextMeshProUGUI)
			});
			val6.transform.SetParent((Transform)(object)menu.content, false);
			RectTransform component8 = val6.GetComponent<RectTransform>();
			component8.anchorMin = new Vector2(0f, 1f);
			component8.anchorMax = new Vector2(1f, 1f);
			component8.pivot = new Vector2(0.5f, 1f);
			component8.sizeDelta = new Vector2(0f, 40f);
			component8.anchoredPosition = new Vector2(0f, -20f);
			TextMeshProUGUI component9 = val6.GetComponent<TextMeshProUGUI>();
			((TMP_Text)component9).font = font;
			((TMP_Text)component9).fontSize = 16f;
			((TMP_Text)component9).alignment = (TextAlignmentOptions)514;
			((TMP_Text)component9).text = "No settings match this search.";
			((Graphic)component9).color = new Color(1f, 1f, 1f, 0.8f);
			((Graphic)component9).raycastTarget = false;
			val6.SetActive(false);
			emptyStateText = component9;
			RectTransform content = menu.content;
			content.anchorMin = new Vector2(content.anchorMin.x, 0f);
			content.anchorMax = new Vector2(content.anchorMax.x, 1f);
			content.offsetMax = new Vector2(content.offsetMax.x, -52f);
		}
	}

	private static TextMeshProUGUI CreateInputText(Transform parent, string name, string value, TMP_FontAsset font, Color color)
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Expected O, but got Unknown
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		GameObject val = new GameObject(name, new Type[2]
		{
			typeof(RectTransform),
			typeof(TextMeshProUGUI)
		});
		val.transform.SetParent(parent, false);
		RectTransform component = val.GetComponent<RectTransform>();
		component.anchorMin = Vector2.zero;
		component.anchorMax = Vector2.one;
		component.offsetMin = Vector2.zero;
		component.offsetMax = Vector2.zero;
		TextMeshProUGUI component2 = val.GetComponent<TextMeshProUGUI>();
		((TMP_Text)component2).text = value;
		((TMP_Text)component2).font = font;
		((TMP_Text)component2).fontSize = 16f;
		((TMP_Text)component2).alignment = (TextAlignmentOptions)4097;
		((TMP_Text)component2).enableWordWrapping = false;
		((Graphic)component2).color = color;
		((Graphic)component2).raycastTarget = false;
		return component2;
	}

	private void SyncInputText()
	{
		if ((Object)(object)inputField != (Object)null && inputField.text != searchText)
		{
			inputField.SetTextWithoutNotify(searchText ?? string.Empty);
		}
	}

	private void OnSearchValueChanged(string value)
	{
		string b = value ?? string.Empty;
		if (!string.Equals(searchText, b, StringComparison.Ordinal))
		{
			searchText = b;
			QueueRefresh();
		}
	}

	internal void OnTabSelected(SettingCategory category)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected I4, but got Unknown
		activeCategoryIndex = (int)category;
		QueueRefresh();
	}

	private void BuildSearchResults(string query)
	{
		//IL_017b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0196: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_0114: Unknown result type (might be due to invalid IL or missing references)
		List<SearchResultDefinition> list = FindMatches(query);
		List<GameObject> list2 = SpawnedSettingsRef.Invoke(menu);
		if (list2 == null)
		{
			return;
		}
		ClearSpawnedRows(list2);
		searchModeActive = true;
		float num = 0f;
		string a = null;
		for (int i = 0; i < list.Count; i++)
		{
			SearchResultDefinition searchResultDefinition = list[i];
			if (searchResultDefinition == null)
			{
				continue;
			}
			if (!string.Equals(a, searchResultDefinition.CategoryName, StringComparison.Ordinal))
			{
				GameObject val = CreateCategoryHeader(searchResultDefinition.CategoryName);
				if ((Object)(object)val != (Object)null)
				{
					PositionRow(val, num);
					num += val.GetComponent<RectTransform>().sizeDelta.y;
					list2.Add(val);
				}
				a = searchResultDefinition.CategoryName;
			}
			GameObject val2 = ((searchResultDefinition.CustomDefinition != null) ? CreateCustomRow(searchResultDefinition.CustomDefinition) : CreateStandardRow(searchResultDefinition.Setting));
			if (!((Object)(object)val2 == (Object)null))
			{
				PositionRow(val2, num);
				num += val2.GetComponent<RectTransform>().sizeDelta.y;
				list2.Add(val2);
			}
		}
		FixDropdownsInContent((Transform)(object)menu.content);
		SetEmptyStateVisible(list.Count == 0);
		menu.content.sizeDelta = new Vector2(menu.content.sizeDelta.x, (list.Count > 0) ? num : 48f);
		menu.content.anchoredPosition = new Vector2(menu.content.anchoredPosition.x, 0f);
	}

	private List<SearchResultDefinition> FindMatches(string query)
	{
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d3: Unknown result type (might be due to invalid IL or missing references)
		string[] tokens = (from token in query.Split(new char[1] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
			select token.Trim().ToLowerInvariant() into token
			where token.Length > 0
			select token).ToArray();
		List<SearchResultDefinition> list = new List<SearchResultDefinition>();
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		IEnumerable<Setting> allSettings = Settings.GetAllSettings();
		IEnumerable<Setting> enumerable = allSettings ?? Enumerable.Empty<Setting>();
		foreach (Setting item in enumerable)
		{
			if (item != null)
			{
				string settingLabel = GetSettingLabel(item);
				string settingDescription = GetSettingDescription(item);
				string categoryDisplayName = GetCategoryDisplayName(item.category);
				string haystack = string.Join(" ", new string[4] { item.name, settingLabel, settingDescription, categoryDisplayName }.Where((string part) => !string.IsNullOrWhiteSpace(part))).ToLowerInvariant();
				if (MatchesAllTokens(haystack, tokens) && hashSet.Add(item.name))
				{
					list.Add(new SearchResultDefinition
					{
						Setting = item,
						CategoryName = categoryDisplayName,
						SortLabel = (settingLabel ?? item.name)
					});
				}
			}
		}
		foreach (ModCustomSettingDefinition visibleCustomDefinition in ModOptionsMenu.GetVisibleCustomDefinitions())
		{
			if (visibleCustomDefinition != null)
			{
				string categoryDisplayName2 = GetCategoryDisplayName(visibleCustomDefinition.Category);
				string haystack2 = string.Join(" ", new string[4] { visibleCustomDefinition.Id, visibleCustomDefinition.Label, visibleCustomDefinition.Description, categoryDisplayName2 }.Where((string part) => !string.IsNullOrWhiteSpace(part))).ToLowerInvariant();
				if (MatchesAllTokens(haystack2, tokens) && hashSet.Add(visibleCustomDefinition.Id))
				{
					list.Add(new SearchResultDefinition
					{
						CustomDefinition = visibleCustomDefinition,
						CategoryName = categoryDisplayName2,
						SortLabel = (visibleCustomDefinition.Label ?? visibleCustomDefinition.Id)
					});
				}
			}
		}
		return list.OrderBy<SearchResultDefinition, string>((SearchResultDefinition result) => result.CategoryName, StringComparer.OrdinalIgnoreCase).ThenBy<SearchResultDefinition, string>((SearchResultDefinition result) => result.SortLabel, StringComparer.OrdinalIgnoreCase).ToList();
	}

	private static bool MatchesAllTokens(string haystack, IEnumerable<string> tokens)
	{
		if (string.IsNullOrWhiteSpace(haystack))
		{
			return false;
		}
		foreach (string token in tokens)
		{
			if (haystack.IndexOf(token, StringComparison.Ordinal) < 0)
			{
				return false;
			}
		}
		return true;
	}

	private static void ClearSpawnedRows(List<GameObject> spawnedSettings)
	{
		for (int i = 0; i < spawnedSettings.Count; i++)
		{
			if ((Object)(object)spawnedSettings[i] != (Object)null)
			{
				Object.Destroy((Object)(object)spawnedSettings[i]);
			}
		}
		spawnedSettings.Clear();
	}

	private GameObject CreateCategoryHeader(string categoryName)
	{
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Expected O, but got Unknown
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		//IL_015c: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)menu == (Object)null || (Object)(object)menu.content == (Object)null)
		{
			return null;
		}
		TMP_FontAsset font = (((Object)(object)inputField != (Object)null && (Object)(object)inputField.textComponent != (Object)null) ? inputField.textComponent.font : ResourceLookupCache.FindFontContains("Retro GamingPix"));
		GameObject val = new GameObject("QoL_SettingsSearchHeader_" + categoryName, new Type[2]
		{
			typeof(RectTransform),
			typeof(TextMeshProUGUI)
		});
		val.transform.SetParent((Transform)(object)menu.content, false);
		RectTransform component = val.GetComponent<RectTransform>();
		component.anchorMin = new Vector2(0f, 1f);
		component.anchorMax = new Vector2(1f, 1f);
		component.pivot = new Vector2(0.5f, 1f);
		component.sizeDelta = new Vector2(0f, 28f);
		TextMeshProUGUI component2 = val.GetComponent<TextMeshProUGUI>();
		((TMP_Text)component2).font = font;
		((TMP_Text)component2).fontSize = 15f;
		((TMP_Text)component2).text = categoryName;
		((TMP_Text)component2).alignment = (TextAlignmentOptions)4097;
		((Graphic)component2).color = new Color(1f, 0.92f, 0.55f, 1f);
		((Graphic)component2).raycastTarget = false;
		return val;
	}

	private GameObject CreateStandardRow(Setting setting)
	{
		//IL_0507: Unknown result type (might be due to invalid IL or missing references)
		//IL_0511: Expected O, but got Unknown
		//IL_0315: Unknown result type (might be due to invalid IL or missing references)
		//IL_031f: Expected O, but got Unknown
		if ((Object)(object)menu == (Object)null || (Object)(object)menu.content == (Object)null || setting == null)
		{
			return null;
		}
		GameObject val = null;
		bool flag = true;
		Setting obj = setting;
		SettingFloat settingFloat = (SettingFloat)(object)((obj is SettingFloat) ? obj : null);
		if (settingFloat != null)
		{
			val = Utils.Create("Special/GameSettingFloat", (Transform)(object)menu.content);
			Slider val2 = (((Object)(object)val != (Object)null) ? ((Component)val.transform.GetChild(1)).GetComponent<Slider>() : null);
			if ((Object)(object)val2 != (Object)null)
			{
				val2.minValue = settingFloat.min;
				val2.maxValue = settingFloat.max;
				val2.SetValueWithoutNotify(settingFloat.value);
				TextMeshProUGUI valueText = ((Component)val.transform.GetChild(2)).GetComponent<TextMeshProUGUI>();
				if ((Object)(object)valueText != (Object)null)
				{
					((TMP_Text)valueText).text = settingFloat.GetDisplayString();
				}
				((UnityEvent<float>)(object)val2.onValueChanged).AddListener((UnityAction<float>)delegate(float value)
				{
					setting.SetValue((object)value);
					setting.Apply();
					if ((Object)(object)valueText != (Object)null)
					{
						((TMP_Text)valueText).text = settingFloat.GetDisplayString();
					}
				});
			}
		}
		else
		{
			Setting obj2 = setting;
			SettingInt settingInt = (SettingInt)(object)((obj2 is SettingInt) ? obj2 : null);
			if (settingInt != null)
			{
				val = Utils.Create("Special/GameSettingInt", (Transform)(object)menu.content);
				TMP_InputField input = (((Object)(object)val != (Object)null) ? ((Component)val.transform.GetChild(1)).GetComponent<TMP_InputField>() : null);
				if ((Object)(object)input != (Object)null)
				{
					input.SetTextWithoutNotify(settingInt.value.ToString());
					((UnityEvent<string>)(object)input.onEndEdit).AddListener((UnityAction<string>)delegate(string value)
					{
						if (!int.TryParse(value, out var result))
						{
							result = settingInt.value;
						}
						result = Mathf.Clamp(result, settingInt.min, settingInt.max);
						input.SetTextWithoutNotify(result.ToString());
						setting.SetValue((object)result);
						setting.Apply();
					});
				}
			}
			else
			{
				Setting obj3 = setting;
				SettingDropdown val3 = (SettingDropdown)(object)((obj3 is SettingDropdown) ? obj3 : null);
				if (val3 != null)
				{
					val = Utils.Create("Special/GameSettingDropdown", (Transform)(object)menu.content);
					TMP_Dropdown val4 = (((Object)(object)val != (Object)null) ? ((Component)val.transform.GetChild(1)).GetComponent<TMP_Dropdown>() : null);
					if ((Object)(object)val4 != (Object)null)
					{
						List<OptionData> list = new List<OptionData>();
						string[] array = val3.choices ?? Array.Empty<string>();
						for (int num = 0; num < array.Length; num++)
						{
							list.Add(new OptionData(Locale.GetOther("gameset" + setting.name + array[num])));
						}
						val4.ClearOptions();
						val4.AddOptions(list);
						val4.SetValueWithoutNotify(val3.value);
						((UnityEvent<int>)(object)val4.onValueChanged).AddListener((UnityAction<int>)delegate(int value)
						{
							setting.SetValue((object)value);
							setting.Apply();
						});
					}
				}
				else if (setting is SettingBool)
				{
					val = Utils.Create("Special/GameSettingBool", (Transform)(object)menu.content);
					Toggle val5 = (((Object)(object)val != (Object)null) ? ((Component)val.transform.GetChild(1)).GetComponent<Toggle>() : null);
					if ((Object)(object)val5 != (Object)null)
					{
						val5.SetIsOnWithoutNotify((bool)setting.GetValue());
						((UnityEvent<bool>)(object)val5.onValueChanged).AddListener((UnityAction<bool>)delegate(bool value)
						{
							setting.SetValue((object)value);
							setting.Apply();
						});
					}
				}
				else
				{
					Setting obj4 = setting;
					SettingKeybind keybind = (SettingKeybind)(object)((obj4 is SettingKeybind) ? obj4 : null);
					if (keybind != null)
					{
						val = Utils.Create("Special/GameSettingInput", (Transform)(object)menu.content);
						Button val6 = (((Object)(object)val != (Object)null) ? ((Component)val.transform.GetChild(1)).GetComponent<Button>() : null);
						if ((Object)(object)val6 != (Object)null)
						{
							TextMeshProUGUI text = ((Component)val6).GetComponentInChildren<TextMeshProUGUI>();
							if ((Object)(object)text != (Object)null)
							{
								((TMP_Text)text).text = keybind.GetDisplayString();
							}
							((UnityEvent)val6.onClick).AddListener((UnityAction)delegate
							{
								((MonoBehaviour)this).StartCoroutine(WaitForKeyPress(keybind, text));
							});
						}
						flag = false;
					}
				}
			}
		}
		if ((Object)(object)val == (Object)null)
		{
			return null;
		}
		ConfigureRowText(val, setting.name, GetSettingLabel(setting), flag ? GetSettingDescription(setting) : string.Empty);
		ModManagementSettings.PrepareSpawnedRow(val, setting);
		return val;
	}

	private GameObject CreateCustomRow(ModCustomSettingDefinition definition)
	{
		if ((Object)(object)menu == (Object)null || (Object)(object)menu.content == (Object)null || definition == null)
		{
			return null;
		}
		GameObject val = Utils.Create(definition.TemplateResourcePath, (Transform)(object)menu.content);
		if ((Object)(object)val == (Object)null)
		{
			ManualLogSource obj = logger;
			if (obj != null)
			{
				obj.LogWarning((object)("Failed to instantiate custom search row for '" + definition.Id + "'."));
			}
			return null;
		}
		((Object)val).name = definition.Id;
		ConfigureRowText(val, definition.Id, definition.Label, definition.Description);
		definition.Binder?.Invoke(val);
		return val;
	}

	private static void ConfigureRowText(GameObject row, string id, string label, string description)
	{
		if ((Object)(object)row == (Object)null)
		{
			return;
		}
		((Object)row).name = id;
		if (row.transform.childCount != 0)
		{
			Transform child = row.transform.GetChild(0);
			TextMeshProUGUI component = ((Component)child).GetComponent<TextMeshProUGUI>();
			if ((Object)(object)component != (Object)null)
			{
				((TMP_Text)component).text = label ?? id;
			}
			UITooltip val = ((Component)child).GetComponent<UITooltip>();
			if ((Object)(object)val == (Object)null)
			{
				val = ((Component)child).gameObject.AddComponent<UITooltip>();
			}
			val.skipLocale = true;
			val.tipName = label ?? id;
			val.tipDesc = description ?? string.Empty;
		}
	}

	private IEnumerator WaitForKeyPress(SettingKeybind keybind, TextMeshProUGUI text)
	{
		return KeybindCombos.Capture(keybind, text);
	}

	private static void PositionRow(GameObject row, float currentHeight)
	{
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)row == (Object)null))
		{
			RectTransform component = row.GetComponent<RectTransform>();
			if (!((Object)(object)component == (Object)null))
			{
				component.anchoredPosition = new Vector2(0f, 0f - currentHeight - component.sizeDelta.y * 0.5f);
			}
		}
	}

	private void FixDropdownsInContent(Transform content)
	{
		if (!((Object)(object)content == (Object)null))
		{
			TMP_Dropdown[] componentsInChildren = ((Component)content).GetComponentsInChildren<TMP_Dropdown>(true);
			foreach (TMP_Dropdown dropdown in componentsInChildren)
			{
				FixDropdown(dropdown);
			}
		}
	}

	private static void FixDropdown(TMP_Dropdown dropdown)
	{
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)dropdown == (Object)null || (Object)(object)dropdown.template == (Object)null)
		{
			return;
		}
		Canvas component = ((Component)dropdown.template).GetComponent<Canvas>();
		if ((Object)(object)component != (Object)null)
		{
			component.overrideSorting = true;
		}
		ScrollRect val = ((Component)dropdown.template).GetComponent<ScrollRect>();
		if ((Object)(object)val == (Object)null)
		{
			val = ((Component)dropdown.template).gameObject.AddComponent<ScrollRect>();
		}
		Transform val2 = ((Transform)dropdown.template).Find("Viewport");
		if ((Object)(object)val2 != (Object)null)
		{
			val.viewport = (RectTransform)(object)((val2 is RectTransform) ? val2 : null);
			RectTransform val3 = (RectTransform)(object)((val2 is RectTransform) ? val2 : null);
			if ((Object)(object)val3 != (Object)null)
			{
				val3.sizeDelta = new Vector2(val3.sizeDelta.x, 200f);
			}
			Transform val4 = val2.Find("Content");
			if ((Object)(object)val4 != (Object)null)
			{
				val.content = (RectTransform)(object)((val4 is RectTransform) ? val4 : null);
			}
		}
		val.horizontal = false;
		val.vertical = true;
		val.movementType = (MovementType)2;
		val.scrollSensitivity = 24f;
	}

	private void SetEmptyStateVisible(bool visible)
	{
		if ((Object)(object)emptyStateText != (Object)null)
		{
			((Component)emptyStateText).gameObject.SetActive(visible);
		}
	}

	private static string GetSettingLabel(Setting setting)
	{
		if (setting == null)
		{
			return string.Empty;
		}
		try
		{
			return Locale.GetOther("gameset" + setting.name);
		}
		catch
		{
			return setting.name;
		}
	}

	private static string GetSettingDescription(Setting setting)
	{
		if (setting == null)
		{
			return string.Empty;
		}
		try
		{
			string other = Locale.GetOther("gameset" + setting.name + "dsc");
			return string.Equals(other, "gameset" + setting.name + "dsc", StringComparison.Ordinal) ? string.Empty : other;
		}
		catch
		{
			return string.Empty;
		}
	}

	private unsafe static string GetCategoryDisplayName(SettingCategory category)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Expected I4, but got Unknown
		if (ModOptionsMenu.TryGetCustomCategoryDisplayName(category, out var displayName) && !string.IsNullOrWhiteSpace(displayName))
		{
			return displayName;
		}
		if (TryGetCuCoreLibCategoryDisplayName(category, out var displayName2) && !string.IsNullOrWhiteSpace(displayName2))
		{
			return displayName2;
		}
		if (BuiltInCategoryNames.TryGetValue((int)category, out var value))
		{
			return value;
		}
		return ((object)(*(SettingCategory*)(&category))/*cast due to constrained. prefix*/).ToString();
	}

	private static bool TryGetCuCoreLibCategoryDisplayName(SettingCategory category, out string displayName)
	{
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		displayName = null;
		if (cuCoreLibTryGetOwnedCustomCategoryMethod == null || cuCoreLibDisplayNameProperty == null)
		{
			return false;
		}
		try
		{
			object[] array = new object[2] { category, null };
			if (!(bool)cuCoreLibTryGetOwnedCustomCategoryMethod.Invoke(null, array) || array[1] == null)
			{
				return false;
			}
			displayName = cuCoreLibDisplayNameProperty.GetValue(array[1], null) as string;
			return !string.IsNullOrWhiteSpace(displayName);
		}
		catch
		{
			return false;
		}
	}
}
