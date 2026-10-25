using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using HarmonyLib;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

namespace QoL_Unknown;

internal static class ModManagementSettings
{
	private sealed class ModEntry
	{
		public string Guid;

		public string Name;

		public string PluginPath;

		public bool IsEnabled;

		public bool IsCore;

		public bool IsSelf;

		public string SettingId;

		public string Version;

		public bool HasUpdate;

		public bool HasVersionMismatch;

		public string LatestVersion;

		public bool CanToggle => !IsCore && !IsSelf && !string.IsNullOrWhiteSpace(PluginPath);
	}

	private sealed class PendingModState
	{
		public string PluginPath;

		public string Guid;

		public bool DesiredEnabled;
	}

	private sealed class ModPresetSlot
	{
		public List<ModPresetEntry> Entries { get; set; }
	}

	private sealed class ModPresetEntry
	{
		public string Guid { get; set; }

		public string PluginPath { get; set; }

		public bool Enabled { get; set; }
	}

	private sealed class ModPresetClickHandler : MonoBehaviour, IPointerClickHandler, IEventSystemHandler
	{
		private int slot;

		public void Configure(int newSlot)
		{
			slot = newSlot;
		}

		public void OnPointerClick(PointerEventData eventData)
		{
			//IL_0005: Unknown result type (might be due to invalid IL or missing references)
			//IL_000b: Invalid comparison between Unknown and I4
			if (eventData != null && (int)eventData.button == 1)
			{
				SavePreset(slot);
			}
		}
	}

	private sealed class CachedVersionEntry
	{
		public string PluginPath { get; set; }

		public string DllName { get; set; }

		public string Guid { get; set; }

		public string Version { get; set; }
	}

	[Serializable]
	[CompilerGenerated]
	private sealed class _003C_003Ec
	{
		public static readonly _003C_003Ec _003C_003E9 = new _003C_003Ec();

		public static Func<ModPresetEntry, bool> _003C_003E9__31_1;

		public static Func<Image, bool> _003C_003E9__33_0;

		public static Func<Button, bool> _003C_003E9__35_0;

		public static Func<UILocalizer, bool> _003C_003E9__35_1;

		public static Func<ModEntry, bool> _003C_003E9__38_0;

		public static Func<ModEntry, ModPresetEntry> _003C_003E9__38_1;

		public static Func<KeyValuePair<string, PluginInfo>, string> _003C_003E9__40_0;

		public static Func<SerialMod, bool> _003C_003E9__41_0;

		public static Func<SerialMod, string> _003C_003E9__41_1;

		public static UnityAction _003C_003E9__47_0;

		public static UnityAction _003C_003E9__48_0;

		public static Func<KeyValuePair<string, PluginInfo>, string> _003C_003E9__63_1;

		public static Comparison<ModEntry> _003C_003E9__63_0;

		public static Func<string, string> _003C_003E9__69_0;

		public static Func<ModEntry, bool> _003C_003E9__80_0;

		public static Func<Button, bool> _003C_003E9__83_0;

		public static Func<CachedVersionEntry, bool> _003C_003E9__94_0;

		public static Func<CachedVersionEntry, string> _003C_003E9__94_1;

		public static Func<IGrouping<string, CachedVersionEntry>, CachedVersionEntry> _003C_003E9__94_2;

		public static Func<CachedVersionEntry, string> _003C_003E9__94_3;

		public static Func<char, char> _003C_003E9__95_0;

		internal bool _003CRefreshPresetButtons_003Eb__31_1(ModPresetEntry entry)
		{
			return entry.Enabled;
		}

		internal bool _003CFindSlotSpriteSource_003Eb__33_0(Image candidate)
		{
			return (Object)(object)candidate != (Object)null && (Object)(object)candidate.sprite != (Object)null;
		}

		internal bool _003CFindBackButtonRect_003Eb__35_0(Button button)
		{
			return (Object)(object)button != (Object)null && string.Equals(((Object)button).name, "Back", StringComparison.Ordinal);
		}

		internal bool _003CFindBackButtonRect_003Eb__35_1(UILocalizer candidate)
		{
			return (Object)(object)candidate != (Object)null && string.Equals(candidate.key, "back", StringComparison.OrdinalIgnoreCase);
		}

		internal bool _003CSavePreset_003Eb__38_0(ModEntry entry)
		{
			return entry?.CanToggle ?? false;
		}

		internal ModPresetEntry _003CSavePreset_003Eb__38_1(ModEntry entry)
		{
			return new ModPresetEntry
			{
				Guid = entry.Guid,
				PluginPath = entry.PluginPath,
				Enabled = GetDesiredEnabled(entry)
			};
		}

		internal string _003CCaptureLoadedMods_003Eb__40_0(KeyValuePair<string, PluginInfo> pair)
		{
			return pair.Value.Metadata.Name;
		}

		internal bool _003CDescribeMods_003Eb__41_0(SerialMod mod)
		{
			return mod != null;
		}

		internal string _003CDescribeMods_003Eb__41_1(SerialMod mod)
		{
			return mod.Name;
		}

		internal void _003CPrepareSpawnedRow_003Eb__47_0()
		{
			Application.OpenURL("https://www.nexusmods.com/scavprototype/mods");
		}

		internal void _003CEnsureOpenNexusButton_003Eb__48_0()
		{
			Application.OpenURL("https://www.nexusmods.com/scavprototype/mods");
		}

		internal string _003CRebuildCache_003Eb__63_1(KeyValuePair<string, PluginInfo> pair)
		{
			return pair.Value.Metadata.Name;
		}

		internal int _003CRebuildCache_003Eb__63_0(ModEntry a, ModEntry b)
		{
			return string.Compare(a?.Name, b?.Name, StringComparison.OrdinalIgnoreCase);
		}

		internal string _003CGetNexusAutofillEntries_003Eb__69_0(string value)
		{
			return value;
		}

		internal bool _003CHasVisibleEntries_003Eb__80_0(ModEntry entry)
		{
			return entry != null && !entry.IsSelf && !entry.IsCore;
		}

		internal bool _003CFindResetButton_003Eb__83_0(Button button)
		{
			return (Object)(object)button != (Object)null && string.Equals(((Object)button).name, "Reset", StringComparison.Ordinal);
		}

		internal bool _003CSaveVersionCache_003Eb__94_0(CachedVersionEntry entry)
		{
			return entry != null && !string.IsNullOrWhiteSpace(entry.Version);
		}

		internal string _003CSaveVersionCache_003Eb__94_1(CachedVersionEntry entry)
		{
			return entry.PluginPath ?? string.Empty;
		}

		internal CachedVersionEntry _003CSaveVersionCache_003Eb__94_2(IGrouping<string, CachedVersionEntry> group)
		{
			return group.First();
		}

		internal string _003CSaveVersionCache_003Eb__94_3(CachedVersionEntry entry)
		{
			return entry.DllName;
		}

		internal char _003CBuildSettingId_003Eb__95_0(char ch)
		{
			return (char.IsLetterOrDigit(ch) || ch == '.') ? char.ToLowerInvariant(ch) : '_';
		}
	}

	private const uint SteamAppIdDemo = 4576510u;

	private const uint SteamAppIdPlaytest = 4584420u;

	private const string CategoryName = "Mods";

	private const string DisabledExtension = ".disabled";

	private const string RestartButtonName = "QoL_ModRestartButton";

	private const string PresetButtonNamePrefix = "QoL_ModPresetButton_";

	private const int PresetSlotCount = 3;

	private const string PresetTooltipDescription = "Click to load, right click to save current list as preset";

	private static readonly Color PresetSlotEmptyColor = new Color(0.5f, 0.5f, 0.5f, 1f);

	private const string RowNexusButtonName = "QoL_OpenNexusButton";

	internal const string NexusVersionsUrl = "https://jimmyking9999999.github.io/Metadata-generator/nexusmods-versions.json";

	private const string NexusModsListingUrl = "https://www.nexusmods.com/scavprototype/mods";

	private static readonly string VersionCachePath = Path.Combine(Application.persistentDataPath, "qol-mod-plugin-version-cache.json");

	private static readonly string PresetCachePath = Path.Combine(Application.persistentDataPath, "qol-mod-presets.json");

	private static List<ModPresetSlot> loadedPresets;

	private static readonly Dictionary<string, PendingModState> PendingStates = new Dictionary<string, PendingModState>(StringComparer.OrdinalIgnoreCase);

	private static readonly List<ModEntry> CachedEntries = new List<ModEntry>();

	private static readonly HashSet<string> RegisteredSettingIds = new HashSet<string>(StringComparer.Ordinal);

	private static readonly Dictionary<string, string> LatestVersionByGuid = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

	private static readonly Dictionary<string, CachedVersionEntry> VersionCacheByPluginPath = new Dictionary<string, CachedVersionEntry>(StringComparer.OrdinalIgnoreCase);

	private static readonly Dictionary<string, CachedVersionEntry> VersionCacheByDllName = new Dictionary<string, CachedVersionEntry>(StringComparer.OrdinalIgnoreCase);

	private static readonly Dictionary<string, string> HardcodedVersionOverrides = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { { "multiupdater", "0.0.1" } };

	private static readonly object VersionsSync = new object();

	private static ManualLogSource logger;

	private static bool initialized;

	private static bool pendingApply;

	private static bool versionsFetchStarted;

	private static bool versionsLoaded;

	internal static void Initialize(ManualLogSource log)
	{
		logger = log;
		if (!initialized)
		{
			initialized = true;
			LoadVersionCache();
			RebuildCache();
			BeginVersionsFetch();
		}
	}

	internal static void RegisterSettings()
	{
		RebuildCache();
		if (!HasVisibleEntries())
		{
			return;
		}
		for (int i = 0; i < CachedEntries.Count; i++)
		{
			ModEntry modEntry = CachedEntries[i];
			if (modEntry != null && !modEntry.IsSelf && RegisteredSettingIds.Add(modEntry.SettingId))
			{
				string name = modEntry.Name;
				string guid = modEntry.Guid;
				bool isEnabled = modEntry.IsEnabled;
				string settingId = modEntry.SettingId;
				ModOptionsMenu.Register(ModOptionDefinition.Bool(settingId, name, guid, "Mods", isEnabled).WithoutSavedValueRestore());
			}
		}
	}

	internal static void RefreshMenuAffordances(SettingsMenu menu, SettingCategory category)
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Invalid comparison between Unknown and I4
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Expected I4, but got Unknown
		//IL_01c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cc: Expected O, but got Unknown
		//IL_01da: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e4: Expected O, but got Unknown
		//IL_0136: Unknown result type (might be due to invalid IL or missing references)
		//IL_0145: Unknown result type (might be due to invalid IL or missing references)
		//IL_0154: Unknown result type (might be due to invalid IL or missing references)
		//IL_0163: Unknown result type (might be due to invalid IL or missing references)
		//IL_0172: Unknown result type (might be due to invalid IL or missing references)
		//IL_0179: Unknown result type (might be due to invalid IL or missing references)
		//IL_018f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0194: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)menu == (Object)null)
		{
			return;
		}
		Transform val = FindChildRecursive(((Component)menu).transform, "QoL_ModRestartButton");
		if (!HasVisibleEntries() || (int)category != GetCategoryIndex())
		{
			ManualLogSource obj = logger;
			if (obj != null)
			{
				obj.LogDebug((object)$"Hiding Mods affordances: category={(int)category} modsIndex={GetCategoryIndex()} visibleEntries={HasVisibleEntries()}");
			}
			SetAffordancesVisible(menu, visible: false);
			return;
		}
		Button val2 = FindResetButton(menu);
		if ((Object)(object)val2 == (Object)null)
		{
			ManualLogSource obj2 = logger;
			if (obj2 != null)
			{
				obj2.LogWarning((object)"Mods tab could not find the SettingsMenu Reset button to clone.");
			}
			return;
		}
		Button val3 = (((Object)(object)val != (Object)null) ? ((Component)val).GetComponent<Button>() : null);
		if ((Object)(object)val3 == (Object)null)
		{
			GameObject val4 = Object.Instantiate<GameObject>(((Component)val2).gameObject, ((Component)val2).transform.parent, false);
			((Object)val4).name = "QoL_ModRestartButton";
			val3 = val4.GetComponent<Button>();
			RectTransform component = ((Component)val2).GetComponent<RectTransform>();
			RectTransform component2 = val4.GetComponent<RectTransform>();
			if ((Object)(object)component != (Object)null && (Object)(object)component2 != (Object)null)
			{
				component2.anchorMin = component.anchorMin;
				component2.anchorMax = component.anchorMax;
				component2.pivot = component.pivot;
				component2.sizeDelta = component.sizeDelta;
				component2.anchoredPosition = component.anchoredPosition + new Vector2(0f - (component.sizeDelta.x + 20f), 0f);
			}
		}
		if ((Object)(object)val3 == (Object)null)
		{
			return;
		}
		((Component)val3).gameObject.SetActive(true);
		val3.onClick = new ButtonClickedEvent();
		((UnityEvent)val3.onClick).AddListener(new UnityAction(RestartAndApply));
		TextMeshProUGUI componentInChildren = ((Component)val3).GetComponentInChildren<TextMeshProUGUI>(true);
		if ((Object)(object)componentInChildren != (Object)null)
		{
			((TMP_Text)componentInChildren).text = (pendingApply ? "Restart Apply" : "Restart Game");
			Component component3 = ((Component)componentInChildren).GetComponent("UILocalizer");
			if ((Object)(object)component3 != (Object)null)
			{
				Object.Destroy((Object)(object)component3);
			}
		}
		UITooltip val5 = ((Component)val3).GetComponent<UITooltip>();
		if ((Object)(object)val5 == (Object)null)
		{
			val5 = ((Component)val3).gameObject.AddComponent<UITooltip>();
		}
		val5.skipLocale = true;
		if (pendingApply)
		{
			val5.tipName = "Restart Apply";
			val5.tipDesc = "Fully restarts the game, enabling/disabling the toggled mods. Click on this to apply your changes.";
		}
		else
		{
			val5.tipName = "Restart Game";
			val5.tipDesc = "Fully restarts the game without changing any mods";
		}
		RefreshPresetButtons(menu, val2);
	}

	private static void RefreshPresetButtons(SettingsMenu menu, Button resetButton)
	{
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
		//IL_0113: Unknown result type (might be due to invalid IL or missing references)
		//IL_0126: Unknown result type (might be due to invalid IL or missing references)
		//IL_0130: Unknown result type (might be due to invalid IL or missing references)
		//IL_013e: Unknown result type (might be due to invalid IL or missing references)
		//IL_014a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0156: Unknown result type (might be due to invalid IL or missing references)
		//IL_0173: Unknown result type (might be due to invalid IL or missing references)
		//IL_0189: Unknown result type (might be due to invalid IL or missing references)
		//IL_018e: Unknown result type (might be due to invalid IL or missing references)
		//IL_019b: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a5: Expected O, but got Unknown
		//IL_01be: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c8: Expected O, but got Unknown
		//IL_0294: Unknown result type (might be due to invalid IL or missing references)
		//IL_028d: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)menu == (Object)null || (Object)(object)resetButton == (Object)null)
		{
			return;
		}
		RectTransform val = FindBackButtonRect(menu, resetButton);
		if ((Object)(object)val == (Object)null)
		{
			ManualLogSource obj = logger;
			if (obj != null)
			{
				obj.LogWarning((object)"Mods tab could not find the SettingsMenu Back button to anchor preset slots to.");
			}
			return;
		}
		List<ModPresetSlot> list = LoadPresets();
		Button component = ((Component)val).GetComponent<Button>();
		for (int i = 0; i < 3; i++)
		{
			string text = "QoL_ModPresetButton_" + i;
			Transform val2 = FindChildRecursive(((Component)menu).transform, text);
			Button val3 = (((Object)(object)val2 != (Object)null) ? ((Component)val2).GetComponent<Button>() : null);
			if ((Object)(object)val3 == (Object)null)
			{
				val3 = CreatePresetSlotButton(((Component)resetButton).transform.parent, text, component);
			}
			if ((Object)(object)val3 == (Object)null)
			{
				continue;
			}
			((Component)val3).gameObject.SetActive(true);
			RectTransform component2 = ((Component)val3).GetComponent<RectTransform>();
			component2.anchorMin = val.anchorMin;
			component2.anchorMax = val.anchorMax;
			component2.pivot = val.pivot;
			component2.sizeDelta = new Vector2(44f, val.sizeDelta.y);
			component2.anchoredPosition = val.anchoredPosition + new Vector2((1f - component2.pivot.x) * val.sizeDelta.x + 8f + (float)i * 52f + component2.pivot.x * 44f, 0f);
			val3.onClick = new ButtonClickedEvent();
			int slotIndex = i;
			((UnityEvent)val3.onClick).AddListener((UnityAction)delegate
			{
				ApplyPreset(slotIndex);
			});
			ModPresetClickHandler modPresetClickHandler = ((Component)val3).GetComponent<ModPresetClickHandler>();
			if ((Object)(object)modPresetClickHandler == (Object)null)
			{
				modPresetClickHandler = ((Component)val3).gameObject.AddComponent<ModPresetClickHandler>();
			}
			modPresetClickHandler.Configure(slotIndex);
			TextMeshProUGUI componentInChildren = ((Component)val3).GetComponentInChildren<TextMeshProUGUI>(true);
			if ((Object)(object)componentInChildren != (Object)null)
			{
				((TMP_Text)componentInChildren).text = (i + 1).ToString();
				((TMP_Text)componentInChildren).enableWordWrapping = false;
				Component component3 = ((Component)componentInChildren).GetComponent("UILocalizer");
				if ((Object)(object)component3 != (Object)null)
				{
					Object.Destroy((Object)(object)component3);
				}
			}
			Image component4 = ((Component)val3).GetComponent<Image>();
			if ((Object)(object)component4 != (Object)null)
			{
				((Graphic)component4).color = ((list[i]?.Entries != null) ? Color.white : PresetSlotEmptyColor);
			}
			UITooltip val4 = ((Component)val3).GetComponent<UITooltip>();
			if ((Object)(object)val4 == (Object)null)
			{
				val4 = ((Component)val3).gameObject.AddComponent<UITooltip>();
			}
			val4.skipLocale = true;
			val4.tipName = $"Mod Presets {i + 1}";
			val4.tipDesc = ((list[i]?.Entries != null) ? string.Format("{0}\nSaved: {1} enabled / {2} mods", "Click to load, right click to save current list as preset", list[i].Entries.Count((ModPresetEntry entry) => entry.Enabled), list[i].Entries.Count) : "Click to load, right click to save current list as preset");
		}
	}

	private static Button CreatePresetSlotButton(Transform parent, string buttonName, Button reference)
	{
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Expected O, but got Unknown
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0113: Unknown result type (might be due to invalid IL or missing references)
		//IL_011a: Expected O, but got Unknown
		//IL_0147: Unknown result type (might be due to invalid IL or missing references)
		//IL_0154: Unknown result type (might be due to invalid IL or missing references)
		//IL_0161: Unknown result type (might be due to invalid IL or missing references)
		//IL_016e: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a7: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)parent == (Object)null)
		{
			return null;
		}
		GameObject val = new GameObject(buttonName, new Type[3]
		{
			typeof(RectTransform),
			typeof(Image),
			typeof(Button)
		});
		val.transform.SetParent(parent, false);
		val.layer = ((Component)parent).gameObject.layer;
		Image component = val.GetComponent<Image>();
		Image val2 = (((Object)(object)reference != (Object)null) ? FindSlotSpriteSource(reference) : null);
		if ((Object)(object)val2 != (Object)null)
		{
			component.sprite = val2.sprite;
			component.type = val2.type;
		}
		else
		{
			component.sprite = ResourceLookupCache.FindSpriteContains("uiBlockNano");
		}
		((Graphic)component).color = Color.white;
		((Graphic)component).raycastTarget = true;
		Button component2 = val.GetComponent<Button>();
		((Selectable)component2).transition = (Transition)1;
		((Selectable)component2).targetGraphic = (Graphic)(object)component;
		GameObject val3 = new GameObject("Label", new Type[2]
		{
			typeof(RectTransform),
			typeof(TextMeshProUGUI)
		});
		val3.transform.SetParent(val.transform, false);
		val3.layer = val.layer;
		RectTransform component3 = val3.GetComponent<RectTransform>();
		component3.anchorMin = Vector2.zero;
		component3.anchorMax = Vector2.one;
		component3.offsetMin = Vector2.zero;
		component3.offsetMax = Vector2.zero;
		TextMeshProUGUI component4 = val3.GetComponent<TextMeshProUGUI>();
		((TMP_Text)component4).fontSize = 18f;
		((TMP_Text)component4).alignment = (TextAlignmentOptions)514;
		((TMP_Text)component4).enableWordWrapping = false;
		((Graphic)component4).color = Color.white;
		((Graphic)component4).raycastTarget = false;
		TextMeshProUGUI val4 = (((Object)(object)reference != (Object)null) ? ((Component)reference).GetComponentInChildren<TextMeshProUGUI>(true) : null);
		TMP_FontAsset val5 = (((Object)(object)val4 != (Object)null && (Object)(object)((TMP_Text)val4).font != (Object)null) ? ((TMP_Text)val4).font : ResourceLookupCache.FindFontContains("Retro GamingPix"));
		if ((Object)(object)val5 != (Object)null)
		{
			((TMP_Text)component4).font = val5;
		}
		return component2;
	}

	private static Image FindSlotSpriteSource(Button reference)
	{
		Image component = ((Component)reference).GetComponent<Image>();
		if ((Object)(object)component != (Object)null && (Object)(object)component.sprite != (Object)null)
		{
			return component;
		}
		return ((Component)reference).GetComponentsInChildren<Image>(true).FirstOrDefault((Image candidate) => (Object)(object)candidate != (Object)null && (Object)(object)candidate.sprite != (Object)null);
	}

	private static void SetAffordancesVisible(SettingsMenu menu, bool visible)
	{
		if ((Object)(object)menu == (Object)null)
		{
			return;
		}
		Transform val = FindChildRecursive(((Component)menu).transform, "QoL_ModRestartButton");
		if ((Object)(object)val != (Object)null)
		{
			((Component)val).gameObject.SetActive(visible);
		}
		for (int i = 0; i < 3; i++)
		{
			Transform val2 = FindChildRecursive(((Component)menu).transform, "QoL_ModPresetButton_" + i);
			if ((Object)(object)val2 != (Object)null)
			{
				((Component)val2).gameObject.SetActive(visible);
			}
		}
	}

	private static RectTransform FindBackButtonRect(SettingsMenu menu, Button resetButton)
	{
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)menu == (Object)null)
		{
			return null;
		}
		Transform val = (((Object)(object)resetButton != (Object)null) ? ((Component)resetButton).transform.parent : null);
		if ((Object)(object)val != (Object)null)
		{
			Button val2 = null;
			for (int i = 0; i < val.childCount; i++)
			{
				Transform child = val.GetChild(i);
				if (!((Object)(object)child == (Object)null) && !((Object)child).name.StartsWith("QoL_ModRestartButton", StringComparison.Ordinal) && !((Object)child).name.StartsWith("QoL_ModPresetButton_", StringComparison.Ordinal))
				{
					Button component = ((Component)child).GetComponent<Button>();
					if (!((Object)(object)component == (Object)null) && !((Object)(object)component == (Object)(object)resetButton) && ((Object)(object)val2 == (Object)null || ((Component)component).GetComponent<RectTransform>().anchoredPosition.x < ((Component)val2).GetComponent<RectTransform>().anchoredPosition.x))
					{
						val2 = component;
					}
				}
			}
			if ((Object)(object)val2 != (Object)null)
			{
				return ((Component)val2).GetComponent<RectTransform>();
			}
		}
		Transform val3 = FindChildRecursive(((Component)menu).transform, "Back");
		Button val4 = (((Object)(object)val3 != (Object)null) ? ((Component)val3).GetComponent<Button>() : null);
		if ((Object)(object)val4 == (Object)null)
		{
			val4 = ((Component)menu).GetComponentsInChildren<Button>(true).FirstOrDefault((Button button) => (Object)(object)button != (Object)null && string.Equals(((Object)button).name, "Back", StringComparison.Ordinal));
		}
		if ((Object)(object)val4 == (Object)null)
		{
			UILocalizer val5 = ((Component)menu).GetComponentsInChildren<UILocalizer>(true).FirstOrDefault((UILocalizer candidate) => (Object)(object)candidate != (Object)null && string.Equals(candidate.key, "back", StringComparison.OrdinalIgnoreCase));
			val4 = (((Object)(object)val5 != (Object)null) ? ((Component)val5).GetComponentInParent<Button>() : null);
		}
		return ((Object)(object)val4 != (Object)null) ? ((Component)val4).GetComponent<RectTransform>() : null;
	}

	private static List<ModPresetSlot> LoadPresets()
	{
		if (loadedPresets != null)
		{
			return loadedPresets;
		}
		List<ModPresetSlot> list = new List<ModPresetSlot>();
		try
		{
			if (File.Exists(PresetCachePath))
			{
				List<ModPresetSlot> list2 = JsonConvert.DeserializeObject<List<ModPresetSlot>>(File.ReadAllText(PresetCachePath));
				if (list2 != null)
				{
					list = list2;
				}
			}
		}
		catch (Exception ex)
		{
			ManualLogSource obj = logger;
			if (obj != null)
			{
				obj.LogWarning((object)("Failed to load mod presets: " + ex.Message));
			}
		}
		while (list.Count < 3)
		{
			list.Add(null);
		}
		loadedPresets = list;
		return loadedPresets;
	}

	private static void SavePresets(List<ModPresetSlot> presets)
	{
		try
		{
			string directoryName = Path.GetDirectoryName(PresetCachePath);
			if (!string.IsNullOrWhiteSpace(directoryName))
			{
				Directory.CreateDirectory(directoryName);
			}
			File.WriteAllText(PresetCachePath, JsonConvert.SerializeObject((object)presets, (Formatting)1));
		}
		catch (Exception ex)
		{
			ManualLogSource obj = logger;
			if (obj != null)
			{
				obj.LogWarning((object)("Failed to save mod presets: " + ex.Message));
			}
		}
	}

	private static void SavePreset(int slot)
	{
		if (slot >= 0 && slot < 3)
		{
			RebuildCache();
			List<ModPresetEntry> entries = (from entry in CachedEntries
				where entry?.CanToggle ?? false
				select new ModPresetEntry
				{
					Guid = entry.Guid,
					PluginPath = entry.PluginPath,
					Enabled = GetDesiredEnabled(entry)
				}).ToList();
			List<ModPresetSlot> list = LoadPresets();
			list[slot] = new ModPresetSlot
			{
				Entries = entries
			};
			SavePresets(list);
			if ((Object)(object)SettingsMenu.instance != (Object)null)
			{
				RefreshPresetButtons(SettingsMenu.instance, FindResetButton(SettingsMenu.instance));
			}
		}
	}

	private static void ApplyPreset(int slot)
	{
		if (slot < 0 || slot >= 3)
		{
			return;
		}
		ModPresetSlot modPresetSlot = LoadPresets()[slot];
		if (modPresetSlot == null || modPresetSlot.Entries == null)
		{
			return;
		}
		RebuildCache();
		foreach (ModEntry entry in CachedEntries)
		{
			if (entry != null && entry.CanToggle)
			{
				ModPresetEntry modPresetEntry = modPresetSlot.Entries.FirstOrDefault((ModPresetEntry candidate) => MatchesPresetEntry(candidate, entry));
				if (modPresetEntry != null)
				{
					SetEntryDesiredState(entry, modPresetEntry.Enabled, refresh: false);
				}
			}
		}
		SettingsMenuCategoryExtender.RefreshLiveMenu();
	}

	internal static List<SerialMod> CaptureLoadedMods()
	{
		List<SerialMod> list = new List<SerialMod>();
		foreach (KeyValuePair<string, PluginInfo> item in Chainloader.PluginInfos.OrderBy<KeyValuePair<string, PluginInfo>, string>((KeyValuePair<string, PluginInfo> pair) => pair.Value.Metadata.Name, StringComparer.OrdinalIgnoreCase))
		{
			list.Add(new SerialMod
			{
				Guid = item.Key,
				Name = (SafeReadMetadataName(item.Value) ?? item.Key)
			});
		}
		return list;
	}

	internal static string DescribeMods(List<SerialMod> mods)
	{
		if (mods == null || mods.Count == 0)
		{
			return "Mods: none";
		}
		return "Mods: " + string.Join(", ", from mod in mods
			where mod != null
			select mod.Name);
	}

	internal static void StageModsForSave(List<SerialMod> mods)
	{
		RebuildCache();
		foreach (ModEntry entry in CachedEntries)
		{
			if (entry != null && entry.CanToggle)
			{
				bool enabled = mods?.Any((SerialMod saved) => MatchesSavedMod(saved, entry)) ?? false;
				SetEntryDesiredState(entry, enabled, refresh: false);
			}
		}
		SettingsMenuCategoryExtender.RefreshLiveMenu();
	}

	internal static void OpenModsTab()
	{
		int categoryIndex = GetCategoryIndex();
		Transform val = (((Object)(object)PreRunScript.instance != (Object)null) ? ((Component)PreRunScript.instance.mainCanvas).transform : null);
		SettingsMenu.OpenMenu(val);
		if (!((Object)(object)SettingsMenu.instance == (Object)null) && categoryIndex >= 0)
		{
			SharedCoroutineStarter.StartCoroutine(SelectTabWhenStarted(categoryIndex));
		}
	}

	private static IEnumerator SelectTabWhenStarted(int categoryIndex)
	{
		yield return null;
		if ((Object)(object)SettingsMenu.instance != (Object)null)
		{
			SettingsMenu.instance.SelectTab(categoryIndex);
		}
	}

	private static bool MatchesSavedMod(SerialMod saved, ModEntry entry)
	{
		if (saved == null || entry == null)
		{
			return false;
		}
		if (!string.IsNullOrWhiteSpace(saved.Guid) && string.Equals(saved.Guid, entry.Guid, StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		return !string.IsNullOrWhiteSpace(saved.Name) && string.Equals(saved.Name, entry.Name, StringComparison.OrdinalIgnoreCase);
	}

	private static bool MatchesPresetEntry(ModPresetEntry candidate, ModEntry entry)
	{
		if (candidate == null)
		{
			return false;
		}
		if (!string.IsNullOrWhiteSpace(candidate.PluginPath) && string.Equals(candidate.PluginPath, entry.PluginPath, StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		return !string.IsNullOrWhiteSpace(candidate.Guid) && string.Equals(candidate.Guid, entry.Guid, StringComparison.OrdinalIgnoreCase);
	}

	internal static void PrepareSpawnedRow(GameObject row, Setting setting)
	{
		//IL_026d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0272: Unknown result type (might be due to invalid IL or missing references)
		//IL_0278: Expected O, but got Unknown
		if ((Object)(object)row == (Object)null || setting == null || !TryGetEntryBySettingId(setting.name, out var entry))
		{
			return;
		}
		TextMeshProUGUI component = ((Component)row.transform.GetChild(0)).GetComponent<TextMeshProUGUI>();
		if ((Object)(object)component != (Object)null)
		{
			bool flag = IsEnabledOnDisk(entry);
			bool desiredEnabled = GetDesiredEnabled(entry);
			bool flag2 = flag != desiredEnabled;
			string text = (flag ? "Enabled" : "Disabled");
			string text2 = (flag ? "#FFEA00" : "#FF5A5A");
			string text3 = (string.IsNullOrWhiteSpace(entry.Version) ? string.Empty : (" <color=#9a9a9a>(v" + entry.Version + ")</color>"));
			string text4 = ((entry.HasUpdate && !string.IsNullOrWhiteSpace(entry.LatestVersion)) ? (" <color=#E6A15A>(v" + entry.LatestVersion + " Nexus version)</color>") : string.Empty);
			string text5 = (flag2 ? ("  <color=#7CFF7C>Queued: " + (desiredEnabled ? "Enable" : "Disable") + "</color>") : string.Empty);
			((TMP_Text)component).text = entry.Name + text3 + text4 + "\n<size=80%><color=#b6b6b6>" + entry.Guid + "</color>  <color=" + text2 + ">" + text + "</color>" + text5 + "</size>";
			Component component2 = ((Component)component).GetComponent("UILocalizer");
			if ((Object)(object)component2 != (Object)null)
			{
				Object.Destroy((Object)(object)component2);
			}
		}
		Button val = ((Component)row.transform.GetChild(0)).GetComponent<Button>();
		if ((Object)(object)val == (Object)null)
		{
			val = ((Component)row.transform.GetChild(0)).gameObject.AddComponent<Button>();
		}
		((Selectable)val).transition = (Transition)0;
		((UnityEventBase)val.onClick).RemoveAllListeners();
		((Selectable)val).interactable = entry.HasUpdate;
		if (((Selectable)val).interactable)
		{
			ButtonClickedEvent onClick = val.onClick;
			object obj = _003C_003Ec._003C_003E9__47_0;
			if (obj == null)
			{
				UnityAction val2 = delegate
				{
					Application.OpenURL("https://www.nexusmods.com/scavprototype/mods");
				};
				_003C_003Ec._003C_003E9__47_0 = val2;
				obj = (object)val2;
			}
			((UnityEvent)onClick).AddListener((UnityAction)obj);
		}
		UITooltip val3 = ((Component)row.transform.GetChild(0)).GetComponent<UITooltip>();
		if ((Object)(object)val3 == (Object)null)
		{
			val3 = ((Component)row.transform.GetChild(0)).gameObject.AddComponent<UITooltip>();
		}
		val3.skipLocale = true;
		val3.tipName = entry.Name;
		val3.tipDesc = (entry.HasUpdate ? (entry.Guid + "\nUpdate available! click to open Nexus page") : entry.Guid);
		EnsureOpenNexusButton(row);
		Toggle component3 = ((Component)row.transform.GetChild(1)).GetComponent<Toggle>();
		if (!((Object)(object)component3 != (Object)null))
		{
			return;
		}
		bool canToggle = entry.CanToggle;
		bool desiredEnabled2 = GetDesiredEnabled(entry);
		((Selectable)component3).interactable = canToggle;
		((UnityEventBase)component3.onValueChanged).RemoveAllListeners();
		component3.SetIsOnWithoutNotify(desiredEnabled2);
		TextMeshProUGUI componentInChildren = ((Component)component3).GetComponentInChildren<TextMeshProUGUI>(true);
		if ((Object)(object)componentInChildren != (Object)null)
		{
			((TMP_Text)componentInChildren).text = (canToggle ? string.Empty : "Locked");
		}
		SettingBool val4 = Settings.Get<SettingBool>(entry.SettingId);
		if (val4 != null)
		{
			val4.value = desiredEnabled2;
		}
		if (canToggle)
		{
			((UnityEvent<bool>)(object)component3.onValueChanged).AddListener((UnityAction<bool>)delegate(bool isOn)
			{
				SetEntryDesiredState(entry, isOn);
			});
		}
	}

	private static void EnsureOpenNexusButton(GameObject row)
	{
		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0101: Unknown result type (might be due to invalid IL or missing references)
		//IL_0118: Unknown result type (might be due to invalid IL or missing references)
		//IL_0126: Unknown result type (might be due to invalid IL or missing references)
		//IL_0135: Unknown result type (might be due to invalid IL or missing references)
		//IL_013a: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fb: Expected O, but got Unknown
		if ((Object)(object)row == (Object)null)
		{
			return;
		}
		Transform val = row.transform.Find("QoL_OpenNexusButton");
		Toggle component = ((Component)row.transform.GetChild(1)).GetComponent<Toggle>();
		if ((Object)(object)component == (Object)null)
		{
			if ((Object)(object)val != (Object)null)
			{
				((Component)val).gameObject.SetActive(false);
			}
			return;
		}
		Button val2 = (((Object)(object)val != (Object)null) ? ((Component)val).GetComponent<Button>() : null);
		if ((Object)(object)val2 == (Object)null)
		{
			val2 = CreateOpenNexusButton(row.transform);
		}
		if ((Object)(object)val2 == (Object)null)
		{
			return;
		}
		((Object)((Component)val2).gameObject).name = "QoL_OpenNexusButton";
		RectTransform component2 = ((Component)component).GetComponent<RectTransform>();
		RectTransform component3 = ((Component)val2).GetComponent<RectTransform>();
		if ((Object)(object)component2 != (Object)null && (Object)(object)component3 != (Object)null)
		{
			component3.anchorMin = component2.anchorMin;
			component3.anchorMax = component2.anchorMax;
			component3.pivot = component2.pivot;
			component3.sizeDelta = new Vector2(132f, 26f);
			component3.anchoredPosition = component2.anchoredPosition + new Vector2(-128f, 0f);
		}
		TextMeshProUGUI componentInChildren = ((Component)val2).GetComponentInChildren<TextMeshProUGUI>(true);
		if ((Object)(object)componentInChildren != (Object)null)
		{
			((TMP_Text)componentInChildren).text = "Open Nexus";
			((TMP_Text)componentInChildren).fontSize = 16f;
			((TMP_Text)componentInChildren).alignment = (TextAlignmentOptions)514;
			((TMP_Text)componentInChildren).enableWordWrapping = false;
			Component component4 = ((Component)componentInChildren).GetComponent("UILocalizer");
			if ((Object)(object)component4 != (Object)null)
			{
				Object.Destroy((Object)(object)component4);
			}
		}
		((UnityEventBase)val2.onClick).RemoveAllListeners();
		((Selectable)val2).interactable = true;
		((Component)val2).gameObject.SetActive(true);
		ButtonClickedEvent onClick = val2.onClick;
		object obj = _003C_003Ec._003C_003E9__48_0;
		if (obj == null)
		{
			UnityAction val3 = delegate
			{
				Application.OpenURL("https://www.nexusmods.com/scavprototype/mods");
			};
			_003C_003Ec._003C_003E9__48_0 = val3;
			obj = (object)val3;
		}
		((UnityEvent)onClick).AddListener((UnityAction)obj);
	}

	private static Button CreateOpenNexusButton(Transform parent)
	{
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Expected O, but got Unknown
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0113: Expected O, but got Unknown
		//IL_0130: Unknown result type (might be due to invalid IL or missing references)
		//IL_013d: Unknown result type (might be due to invalid IL or missing references)
		//IL_014a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0157: Unknown result type (might be due to invalid IL or missing references)
		GameObject val = new GameObject("QoL_OpenNexusButton", new Type[3]
		{
			typeof(RectTransform),
			typeof(Image),
			typeof(Button)
		});
		val.transform.SetParent(parent, false);
		RectTransform component = val.GetComponent<RectTransform>();
		component.anchorMin = new Vector2(1f, 0.5f);
		component.anchorMax = new Vector2(1f, 0.5f);
		component.pivot = new Vector2(1f, 0.5f);
		component.sizeDelta = new Vector2(132f, 26f);
		component.anchoredPosition = new Vector2(-128f, 0f);
		Image component2 = val.GetComponent<Image>();
		component2.sprite = ResourceLookupCache.FindSpriteContains("uiBlockNano");
		component2.type = (Type)1;
		((Graphic)component2).color = Color.white;
		GameObject val2 = new GameObject("Label", new Type[2]
		{
			typeof(RectTransform),
			typeof(TextMeshProUGUI)
		});
		val2.transform.SetParent(val.transform, false);
		RectTransform component3 = val2.GetComponent<RectTransform>();
		component3.anchorMin = Vector2.zero;
		component3.anchorMax = Vector2.one;
		component3.offsetMin = Vector2.zero;
		component3.offsetMax = Vector2.zero;
		TextMeshProUGUI component4 = val2.GetComponent<TextMeshProUGUI>();
		((TMP_Text)component4).text = "Open Nexus";
		((TMP_Text)component4).fontSize = 16f;
		((TMP_Text)component4).alignment = (TextAlignmentOptions)514;
		((Graphic)component4).raycastTarget = false;
		((TMP_Text)component4).enableWordWrapping = false;
		TMP_FontAsset val3 = ResourceLookupCache.FindFontContains("Retro GamingPix");
		if ((Object)(object)val3 != (Object)null)
		{
			((TMP_Text)component4).font = val3;
		}
		return val.GetComponent<Button>();
	}

	private static void SetEntryDesiredState(ModEntry entry, bool enabled, bool refresh = true)
	{
		if (entry == null || !entry.CanToggle || string.IsNullOrWhiteSpace(entry.PluginPath))
		{
			return;
		}
		bool flag = IsEnabledOnDisk(entry);
		if (enabled == flag)
		{
			PendingStates.Remove(entry.PluginPath);
			pendingApply = PendingStates.Count > 0;
			if (refresh)
			{
				SettingsMenuCategoryExtender.RefreshLiveMenu();
			}
			return;
		}
		PendingStates[entry.PluginPath] = new PendingModState
		{
			PluginPath = entry.PluginPath,
			Guid = entry.Guid,
			DesiredEnabled = enabled
		};
		pendingApply = true;
		if (refresh)
		{
			SettingsMenuCategoryExtender.RefreshLiveMenu();
		}
	}

	private static void RestartAndApply()
	{
		QueueApplyAndRelaunch();
		PreRunScript instance = PreRunScript.instance;
		if ((Object)(object)instance != (Object)null)
		{
			((MonoBehaviour)instance).StartCoroutine(instance.QuitCoroutine());
		}
		else
		{
			Application.Quit();
		}
	}

	private static void QueueApplyAndRelaunch()
	{
		try
		{
			List<string> list = BuildDelayedApplyAndRelaunchScriptLines();
			if (list == null || list.Count == 0)
			{
				ManualLogSource obj = logger;
				if (obj != null)
				{
					obj.LogWarning((object)"Restart Apply did not have any queued work.");
				}
				return;
			}
			string text = WriteApplyScript(list);
			if (string.IsNullOrWhiteSpace(text) || !File.Exists(text))
			{
				ManualLogSource obj2 = logger;
				if (obj2 != null)
				{
					obj2.LogWarning((object)"Failed to create mod apply + relaunch script.");
				}
			}
			else
			{
				Process.Start(new ProcessStartInfo
				{
					FileName = text,
					UseShellExecute = true,
					WindowStyle = ProcessWindowStyle.Hidden
				});
			}
		}
		catch (Exception arg)
		{
			ManualLogSource obj3 = logger;
			if (obj3 != null)
			{
				obj3.LogError((object)$"Failed to queue mod apply + relaunch: {arg}");
			}
		}
	}

	private static List<string> BuildDelayedApplyAndRelaunchScriptLines()
	{
		List<string> list = BuildRenameCommands();
		List<string> list2 = BuildRelaunchCommands();
		if (list.Count == 0 && list2.Count == 0)
		{
			return null;
		}
		int id = Process.GetCurrentProcess().Id;
		List<string> list3 = new List<string>
		{
			"@echo off",
			"setlocal EnableExtensions",
			$":wait_for_exit_{id}",
			$"tasklist /FI \"PID eq {id}\" | find \"{id}\" >nul",
			"if not errorlevel 1 (",
			"    timeout /t 1 /nobreak >nul",
			$"    goto wait_for_exit_{id}",
			")",
			"timeout /t 2 /nobreak >nul"
		};
		list3.AddRange(list);
		list3.Add("timeout /t 2 /nobreak >nul");
		list3.AddRange(list2);
		list3.Add("endlocal");
		return list3;
	}

	private static string WriteApplyScript(List<string> scriptLines)
	{
		try
		{
			string text = Path.Combine(Path.GetTempPath(), $"qol-mod-apply-{Guid.NewGuid():N}.bat");
			File.WriteAllLines(text, scriptLines);
			return text;
		}
		catch (Exception arg)
		{
			ManualLogSource obj = logger;
			if (obj != null)
			{
				obj.LogError((object)$"Failed to write mod apply script: {arg}");
			}
			return null;
		}
	}

	private static List<string> BuildRenameCommands()
	{
		List<string> list = new List<string>();
		if (PendingStates.Count == 0)
		{
			pendingApply = false;
			return list;
		}
		foreach (PendingModState item in PendingStates.Values.ToList())
		{
			if (item != null && !string.IsNullOrWhiteSpace(item.PluginPath))
			{
				string pluginPath = item.PluginPath;
				string text = pluginPath + ".disabled";
				if (item.DesiredEnabled)
				{
					list.Add("if exist \"" + text + "\" move /Y \"" + text + "\" \"" + pluginPath + "\" >nul");
				}
				else
				{
					list.Add("if exist \"" + pluginPath + "\" move /Y \"" + pluginPath + "\" \"" + text + "\" >nul");
				}
			}
		}
		PendingStates.Clear();
		pendingApply = false;
		return list;
	}

	private static List<string> BuildRelaunchCommands()
	{
		List<string> list = new List<string>();
		uint? num = TryGetSteamAppId();
		string exePath = TryGetCurrentExePath();
		if (num.HasValue && IsSteamInstallExe(exePath) && (num.Value == 4576510 || num.Value == 4584420))
		{
			list.Add($"start \"\" \"steam://run/{num.Value}\"");
		}
		return list;
	}

	private static string TryGetCurrentExePath()
	{
		try
		{
			return (Process.GetCurrentProcess().MainModule != null) ? Process.GetCurrentProcess().MainModule.FileName : null;
		}
		catch (Exception ex)
		{
			ManualLogSource obj = logger;
			if (obj != null)
			{
				obj.LogWarning((object)("Failed to read current exe path for relaunch: " + ex.Message));
			}
			return null;
		}
	}

	private static uint? TryGetSteamAppId()
	{
		try
		{
			if ((AccessTools.TypeByName("Together.KSteam")?.GetProperty("STEAM_APPID", BindingFlags.Static | BindingFlags.Public))?.GetValue(null, null) is uint value)
			{
				return value;
			}
		}
		catch (Exception ex)
		{
			ManualLogSource obj = logger;
			if (obj != null)
			{
				obj.LogWarning((object)("Failed to read Steam app id for relaunch: " + ex.Message));
			}
		}
		return null;
	}

	private static void BeginVersionsFetch()
	{
		if (!versionsFetchStarted)
		{
			versionsFetchStarted = true;
			Task versionsTask = FetchNexusVersionsAsync();
			SharedCoroutineStarter.StartCoroutine(WaitForVersionsFetchCoroutine(versionsTask));
		}
	}

	private static IEnumerator WaitForVersionsFetchCoroutine(Task versionsTask)
	{
		if (versionsTask != null)
		{
			while (!versionsTask.IsCompleted)
			{
				yield return null;
			}
			RebuildCache();
			ConsoleScriptPatcher.RefreshNexusCommandAutofill();
			SettingsMenuCategoryExtender.RefreshLiveMenu();
		}
	}

	private static async Task FetchNexusVersionsAsync()
	{
		try
		{
			HttpClient client = new HttpClient();
			try
			{
				client.Timeout = TimeSpan.FromSeconds(20.0);
				client.DefaultRequestHeaders.UserAgent.ParseAdd("QoLUnknownMod");
				HttpResponseMessage response = await client.GetAsync("https://jimmyking9999999.github.io/Metadata-generator/nexusmods-versions.json").ConfigureAwait(continueOnCapturedContext: false);
				byte[] bytes = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(continueOnCapturedContext: false);
				string responseText = Encoding.UTF8.GetString(bytes);
				if (!response.IsSuccessStatusCode)
				{
					ManualLogSource obj = logger;
					if (obj != null)
					{
						obj.LogWarning((object)$"Failed to fetch Nexus mod versions: {(int)response.StatusCode} {response.ReasonPhrase}");
					}
					return;
				}
				Dictionary<string, string> versions = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseText);
				lock (VersionsSync)
				{
					LatestVersionByGuid.Clear();
					if (versions != null)
					{
						foreach (KeyValuePair<string, string> pair in versions)
						{
							if (!string.IsNullOrWhiteSpace(pair.Key) && !string.IsNullOrWhiteSpace(pair.Value))
							{
								LatestVersionByGuid[pair.Key.Trim()] = pair.Value.Trim();
							}
						}
					}
				}
				versionsLoaded = true;
			}
			finally
			{
				((IDisposable)client)?.Dispose();
			}
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			ManualLogSource obj2 = logger;
			if (obj2 != null)
			{
				obj2.LogWarning((object)("Failed to fetch or parse Nexus mod versions: " + ex2.Message));
			}
		}
	}

	private static bool IsSteamInstallExe(string exePath)
	{
		if (string.IsNullOrWhiteSpace(exePath))
		{
			return false;
		}
		try
		{
			string value = (AccessTools.TypeByName("Together.KSteam")?.GetMethod("GetAppInstallDir", BindingFlags.Static | BindingFlags.Public))?.Invoke(null, null) as string;
			if (string.IsNullOrWhiteSpace(value))
			{
				return false;
			}
			return exePath.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
		}
		catch (Exception ex)
		{
			ManualLogSource obj = logger;
			if (obj != null)
			{
				obj.LogWarning((object)("Failed to verify Steam install path for relaunch: " + ex.Message));
			}
			return false;
		}
	}

	private static void RebuildCache()
	{
		CachedEntries.Clear();
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (KeyValuePair<string, PluginInfo> item in Chainloader.PluginInfos.OrderBy<KeyValuePair<string, PluginInfo>, string>((KeyValuePair<string, PluginInfo> pair) => pair.Value.Metadata.Name, StringComparer.OrdinalIgnoreCase))
		{
			object value = item.Value;
			if (value != null)
			{
				string key = item.Key;
				string name = SafeReadMetadataName(value) ?? key;
				string text = ResolvePluginDllPath(SafeReadLocation(value));
				string version = SafeReadMetadataVersion(value);
				if (!string.IsNullOrWhiteSpace(text))
				{
					hashSet.Add(text);
				}
				bool isCore = IsCorePlugin(text);
				bool isSelf = IsSelf(key);
				bool isEnabled = !string.IsNullOrWhiteSpace(text) && File.Exists(text);
				ModEntry modEntry = new ModEntry
				{
					Guid = key,
					Name = name,
					PluginPath = text,
					IsEnabled = isEnabled,
					IsCore = isCore,
					IsSelf = isSelf,
					SettingId = BuildSettingId(key),
					Version = version
				};
				ApplyCachedVersion(modEntry);
				UpdateCachedVersion(modEntry);
				ApplyLatestVersion(modEntry);
				CachedEntries.Add(modEntry);
			}
		}
		string pluginsDirectory = GetPluginsDirectory();
		if (!string.IsNullOrWhiteSpace(pluginsDirectory) && Directory.Exists(pluginsDirectory))
		{
			foreach (string item2 in Directory.EnumerateFiles(pluginsDirectory, "*.dll.disabled", SearchOption.AllDirectories))
			{
				string enabledPath = item2.Substring(0, item2.Length - ".disabled".Length);
				if (!hashSet.Contains(enabledPath) && !CachedEntries.Any((ModEntry entry) => entry != null && string.Equals(entry.PluginPath, enabledPath, StringComparison.OrdinalIgnoreCase)))
				{
					string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(enabledPath);
					ModEntry modEntry2 = new ModEntry
					{
						Guid = fileNameWithoutExtension,
						Name = fileNameWithoutExtension,
						PluginPath = enabledPath,
						IsEnabled = false,
						IsCore = IsCorePlugin(enabledPath),
						IsSelf = IsSelfPathOrName(enabledPath, fileNameWithoutExtension),
						SettingId = BuildSettingId(fileNameWithoutExtension),
						Version = null
					};
					ApplyCachedVersion(modEntry2);
					ApplyLatestVersion(modEntry2);
					CachedEntries.Add(modEntry2);
				}
			}
		}
		CachedEntries.Sort((ModEntry a, ModEntry b) => string.Compare(a?.Name, b?.Name, StringComparison.OrdinalIgnoreCase));
	}

	private static void ApplyCachedVersion(ModEntry entry)
	{
		if (entry == null || !string.IsNullOrWhiteSpace(entry.Version) || TryApplyHardcodedVersionOverride(entry))
		{
			return;
		}
		CachedVersionEntry value = null;
		if (!string.IsNullOrWhiteSpace(entry.PluginPath))
		{
			VersionCacheByPluginPath.TryGetValue(entry.PluginPath, out value);
		}
		if (value == null)
		{
			string text = ((!string.IsNullOrWhiteSpace(entry.PluginPath)) ? Path.GetFileName(entry.PluginPath) : null);
			if (!string.IsNullOrWhiteSpace(text))
			{
				VersionCacheByDllName.TryGetValue(text, out value);
			}
		}
		if (value != null)
		{
			if (!string.IsNullOrWhiteSpace(value.Version))
			{
				entry.Version = value.Version;
			}
			if (!string.IsNullOrWhiteSpace(value.Guid))
			{
				entry.Guid = value.Guid;
			}
		}
	}

	private static bool TryApplyHardcodedVersionOverride(ModEntry entry)
	{
		if (entry == null)
		{
			return false;
		}
		if (!string.IsNullOrWhiteSpace(entry.Guid) && HardcodedVersionOverrides.TryGetValue(entry.Guid, out var value))
		{
			entry.Version = value;
			return true;
		}
		if (!string.IsNullOrWhiteSpace(entry.Name) && HardcodedVersionOverrides.TryGetValue(entry.Name, out value))
		{
			entry.Version = value;
			return true;
		}
		string text = ((!string.IsNullOrWhiteSpace(entry.PluginPath)) ? Path.GetFileNameWithoutExtension(entry.PluginPath) : null);
		if (!string.IsNullOrWhiteSpace(text) && HardcodedVersionOverrides.TryGetValue(text, out value))
		{
			entry.Version = value;
			return true;
		}
		return false;
	}

	private static void UpdateCachedVersion(ModEntry entry)
	{
		if (entry != null && !string.IsNullOrWhiteSpace(entry.PluginPath) && !string.IsNullOrWhiteSpace(entry.Version))
		{
			string fileName = Path.GetFileName(entry.PluginPath);
			CachedVersionEntry value = new CachedVersionEntry
			{
				PluginPath = entry.PluginPath,
				DllName = fileName,
				Guid = entry.Guid,
				Version = entry.Version
			};
			VersionCacheByPluginPath[entry.PluginPath] = value;
			if (!string.IsNullOrWhiteSpace(fileName))
			{
				VersionCacheByDllName[fileName] = value;
			}
			SaveVersionCache();
		}
	}

	private static void ApplyLatestVersion(ModEntry entry)
	{
		if (entry == null || !versionsLoaded || string.IsNullOrWhiteSpace(entry.PluginPath))
		{
			return;
		}
		string value = null;
		if (!string.IsNullOrWhiteSpace(entry.Guid))
		{
			lock (VersionsSync)
			{
				LatestVersionByGuid.TryGetValue(entry.Guid, out value);
			}
		}
		if (IsUsableDisplayVersion(value))
		{
			if (!IsUsableDisplayVersion(entry.Version))
			{
				entry.Version = value;
			}
			entry.LatestVersion = value;
			entry.HasVersionMismatch = IsVersionMismatch(entry.Version, value);
			entry.HasUpdate = IsUpdateAvailable(entry.Version, value);
		}
	}

	internal static bool TryGetNexusPageUrl(string alias, out string nexusPageUrl)
	{
		nexusPageUrl = null;
		if (string.IsNullOrWhiteSpace(alias))
		{
			return false;
		}
		Dictionary<string, string> dictionary = BuildNexusAliasMap();
		return dictionary.TryGetValue(alias.Trim(), out nexusPageUrl) && !string.IsNullOrWhiteSpace(nexusPageUrl);
	}

	internal static List<string> GetNexusAutofillEntries()
	{
		return BuildNexusAliasMap().Keys.OrderBy<string, string>((string value) => value, StringComparer.OrdinalIgnoreCase).ToList();
	}

	private static Dictionary<string, string> BuildNexusAliasMap()
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		for (int i = 0; i < CachedEntries.Count; i++)
		{
			ModEntry modEntry = CachedEntries[i];
			if (modEntry != null)
			{
				AddNexusAlias(dictionary, modEntry.Name, "https://www.nexusmods.com/scavprototype/mods");
				AddNexusAlias(dictionary, modEntry.Guid, "https://www.nexusmods.com/scavprototype/mods");
			}
		}
		lock (VersionsSync)
		{
			foreach (string key in LatestVersionByGuid.Keys)
			{
				AddNexusAlias(dictionary, key, "https://www.nexusmods.com/scavprototype/mods");
			}
		}
		return dictionary;
	}

	private static void AddNexusAlias(Dictionary<string, string> aliases, string alias, string nexusPageUrl)
	{
		if (aliases != null && !string.IsNullOrWhiteSpace(alias) && !string.IsNullOrWhiteSpace(nexusPageUrl))
		{
			string key = alias.Trim();
			if (!aliases.ContainsKey(key))
			{
				aliases[key] = nexusPageUrl;
			}
		}
	}

	private static bool IsUpdateAvailable(string installedVersion, string latestVersion)
	{
		if (string.IsNullOrWhiteSpace(installedVersion) || string.IsNullOrWhiteSpace(latestVersion))
		{
			return false;
		}
		int num = CompareVersionStrings(installedVersion, latestVersion);
		return num < 0;
	}

	private static bool IsVersionMismatch(string installedVersion, string latestVersion)
	{
		if (string.IsNullOrWhiteSpace(installedVersion) || string.IsNullOrWhiteSpace(latestVersion))
		{
			return false;
		}
		return CompareVersionStrings(installedVersion, latestVersion) != 0;
	}

	private static int CompareVersionStrings(string leftVersion, string rightVersion)
	{
		string text = NormalizeVersionText(leftVersion);
		string text2 = NormalizeVersionText(rightVersion);
		if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(text2))
		{
			return string.Compare(text, text2, StringComparison.OrdinalIgnoreCase);
		}
		if (TryParseComparableVersion(text, out var parts) && TryParseComparableVersion(text2, out var parts2))
		{
			int num = Math.Max(parts.Count, parts2.Count);
			for (int i = 0; i < num; i++)
			{
				int num2 = ((i < parts.Count) ? parts[i] : 0);
				int value = ((i < parts2.Count) ? parts2[i] : 0);
				int num3 = num2.CompareTo(value);
				if (num3 != 0)
				{
					return num3;
				}
			}
			return 0;
		}
		return string.Compare(text, text2, StringComparison.OrdinalIgnoreCase);
	}

	private static string NormalizeVersionText(string version)
	{
		return string.IsNullOrWhiteSpace(version) ? null : version.Trim().TrimStart('v', 'V');
	}

	private static bool IsUsableDisplayVersion(string version)
	{
		string text = NormalizeVersionText(version);
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		if (TryParseComparableVersion(text, out var parts))
		{
			for (int i = 0; i < parts.Count; i++)
			{
				if (parts[i] != 0)
				{
					return true;
				}
			}
			return false;
		}
		return true;
	}

	private static bool TryParseComparableVersion(string version, out List<int> parts)
	{
		parts = null;
		if (string.IsNullOrWhiteSpace(version))
		{
			return false;
		}
		string[] array = version.Split(new char[1] { '.' });
		List<int> list = new List<int>(array.Length);
		for (int i = 0; i < array.Length; i++)
		{
			if (!int.TryParse(array[i], out var result))
			{
				return false;
			}
			list.Add(result);
		}
		int num = list.Count - 1;
		while (num > 0 && list[num] == 0)
		{
			list.RemoveAt(num);
			num--;
		}
		parts = list;
		return true;
	}

	private static bool TryGetEntryBySettingId(string settingId, out ModEntry entry)
	{
		entry = CachedEntries.FirstOrDefault((ModEntry candidate) => candidate != null && string.Equals(candidate.SettingId, settingId, StringComparison.Ordinal));
		return entry != null;
	}

	private static int GetCategoryIndex()
	{
		List<ModOptionCategoryEntry> customCategories = ModOptionsMenu.GetCustomCategories();
		for (int i = 0; i < customCategories.Count; i++)
		{
			ModOptionCategoryEntry modOptionCategoryEntry = customCategories[i];
			if (modOptionCategoryEntry != null && string.Equals(modOptionCategoryEntry.DisplayName, "Mods", StringComparison.OrdinalIgnoreCase))
			{
				return modOptionCategoryEntry.CategoryIndex;
			}
		}
		return -1;
	}

	private static bool HasVisibleEntries()
	{
		return CachedEntries.Any((ModEntry entry) => entry != null && !entry.IsSelf && !entry.IsCore);
	}

	private static bool GetDesiredEnabled(ModEntry entry)
	{
		PendingModState value;
		return (entry != null && !string.IsNullOrWhiteSpace(entry.PluginPath) && PendingStates.TryGetValue(entry.PluginPath, out value)) ? value.DesiredEnabled : (entry?.IsEnabled ?? false);
	}

	private static bool IsEnabledOnDisk(ModEntry entry)
	{
		return entry != null && !string.IsNullOrWhiteSpace(entry.PluginPath) && File.Exists(entry.PluginPath);
	}

	private static Button FindResetButton(SettingsMenu menu)
	{
		if ((Object)(object)menu == (Object)null)
		{
			return null;
		}
		Transform val = FindChildRecursive(((Component)menu).transform, "Reset");
		Button val2 = (((Object)(object)val != (Object)null) ? ((Component)val).GetComponent<Button>() : null);
		if ((Object)(object)val2 != (Object)null)
		{
			return val2;
		}
		return ((Component)menu).GetComponentsInChildren<Button>(true).FirstOrDefault((Button button) => (Object)(object)button != (Object)null && string.Equals(((Object)button).name, "Reset", StringComparison.Ordinal));
	}

	private static Transform FindChildRecursive(Transform root, string childName)
	{
		if ((Object)(object)root == (Object)null || string.IsNullOrEmpty(childName))
		{
			return null;
		}
		for (int i = 0; i < root.childCount; i++)
		{
			Transform child = root.GetChild(i);
			if (string.Equals(((Object)child).name, childName, StringComparison.Ordinal))
			{
				return child;
			}
			Transform val = FindChildRecursive(child, childName);
			if ((Object)(object)val != (Object)null)
			{
				return val;
			}
		}
		return null;
	}

	private static string SafeReadMetadataName(object pluginInfo)
	{
		try
		{
			object obj = pluginInfo.GetType().GetProperty("Metadata")?.GetValue(pluginInfo, null);
			return (obj?.GetType().GetProperty("Name"))?.GetValue(obj, null) as string;
		}
		catch
		{
			return null;
		}
	}

	private static string SafeReadMetadataVersion(object pluginInfo)
	{
		try
		{
			object obj = pluginInfo.GetType().GetProperty("Metadata")?.GetValue(pluginInfo, null);
			return ((obj?.GetType().GetProperty("Version"))?.GetValue(obj, null))?.ToString();
		}
		catch
		{
			return null;
		}
	}

	private static string SafeReadLocation(object pluginInfo)
	{
		try
		{
			return pluginInfo.GetType().GetProperty("Location")?.GetValue(pluginInfo, null) as string;
		}
		catch
		{
			return null;
		}
	}

	private static string ResolvePluginDllPath(string location)
	{
		if (string.IsNullOrWhiteSpace(location))
		{
			return null;
		}
		if (File.Exists(location))
		{
			return location;
		}
		if (location.EndsWith(".disabled", StringComparison.OrdinalIgnoreCase))
		{
			string text = location.Substring(0, location.Length - ".disabled".Length);
			if (File.Exists(text))
			{
				return text;
			}
		}
		string path = location + ".disabled";
		if (File.Exists(path))
		{
			return location;
		}
		return location;
	}

	private static bool IsCorePlugin(string pluginPath)
	{
		if (string.IsNullOrWhiteSpace(pluginPath))
		{
			return true;
		}
		string text = pluginPath.Replace('/', '\\');
		return text.IndexOf("\\BepInEx\\core\\", StringComparison.OrdinalIgnoreCase) >= 0;
	}

	private static bool IsSelf(string guid)
	{
		return string.Equals(guid, "org.bepinex.plugins.qol_unknown", StringComparison.Ordinal);
	}

	private static bool IsSelfPathOrName(string pluginPath, string name)
	{
		if (string.Equals(name, "QoL Unknown", StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		if (!string.IsNullOrWhiteSpace(pluginPath))
		{
			string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(pluginPath);
			if (string.Equals(fileNameWithoutExtension, "QoL Unknown", StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}
		return false;
	}

	private static string GetPluginsDirectory()
	{
		foreach (KeyValuePair<string, PluginInfo> pluginInfo in Chainloader.PluginInfos)
		{
			string text = SafeReadLocation(pluginInfo.Value);
			if (string.IsNullOrWhiteSpace(text))
			{
				continue;
			}
			try
			{
				for (DirectoryInfo directoryInfo = new FileInfo(text).Directory; directoryInfo != null; directoryInfo = directoryInfo.Parent)
				{
					if (string.Equals(directoryInfo.Name, "plugins", StringComparison.OrdinalIgnoreCase))
					{
						return directoryInfo.FullName;
					}
				}
			}
			catch
			{
			}
		}
		return null;
	}

	private static void LoadVersionCache()
	{
		VersionCacheByPluginPath.Clear();
		VersionCacheByDllName.Clear();
		try
		{
			if (!File.Exists(VersionCachePath))
			{
				return;
			}
			List<CachedVersionEntry> list = JsonConvert.DeserializeObject<List<CachedVersionEntry>>(File.ReadAllText(VersionCachePath));
			if (list == null)
			{
				return;
			}
			for (int i = 0; i < list.Count; i++)
			{
				CachedVersionEntry cachedVersionEntry = list[i];
				if (cachedVersionEntry != null && !string.IsNullOrWhiteSpace(cachedVersionEntry.Version))
				{
					if (!string.IsNullOrWhiteSpace(cachedVersionEntry.PluginPath))
					{
						VersionCacheByPluginPath[cachedVersionEntry.PluginPath] = cachedVersionEntry;
					}
					if (!string.IsNullOrWhiteSpace(cachedVersionEntry.DllName))
					{
						VersionCacheByDllName[cachedVersionEntry.DllName] = cachedVersionEntry;
					}
				}
			}
		}
		catch (Exception ex)
		{
			ManualLogSource obj = logger;
			if (obj != null)
			{
				obj.LogWarning((object)("Failed to load mod version cache: " + ex.Message));
			}
		}
	}

	private static void SaveVersionCache()
	{
		try
		{
			string directoryName = Path.GetDirectoryName(VersionCachePath);
			if (!string.IsNullOrWhiteSpace(directoryName))
			{
				Directory.CreateDirectory(directoryName);
			}
			List<CachedVersionEntry> list = (from @group in VersionCacheByPluginPath.Values.Where((CachedVersionEntry entry) => entry != null && !string.IsNullOrWhiteSpace(entry.Version)).GroupBy<CachedVersionEntry, string>((CachedVersionEntry entry) => entry.PluginPath ?? string.Empty, StringComparer.OrdinalIgnoreCase)
				select @group.First()).OrderBy<CachedVersionEntry, string>((CachedVersionEntry entry) => entry.DllName, StringComparer.OrdinalIgnoreCase).ToList();
			File.WriteAllText(VersionCachePath, JsonConvert.SerializeObject((object)list, (Formatting)1));
		}
		catch (Exception ex)
		{
			ManualLogSource obj = logger;
			if (obj != null)
			{
				obj.LogWarning((object)("Failed to save mod version cache: " + ex.Message));
			}
		}
	}

	private static string BuildSettingId(string guid)
	{
		char[] value = guid.Select((char ch) => (char.IsLetterOrDigit(ch) || ch == '.') ? char.ToLowerInvariant(ch) : '_').ToArray();
		return "mods." + new string(value);
	}
}
