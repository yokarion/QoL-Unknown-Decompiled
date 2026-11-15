using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
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

		public bool CanToggle
		{
			get
			{
				if (!IsCore && !IsSelf)
				{
					return !string.IsNullOrWhiteSpace(PluginPath);
				}
				return false;
			}
		}
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
			//IL_0004: Unknown result type (might be due to invalid IL or missing references)
			//IL_000a: Invalid comparison between Unknown and I4
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

		public static Func<ModPresetEntry, bool> _003C_003E9__28_1;

		public static Func<Image, bool> _003C_003E9__30_0;

		public static Func<Button, bool> _003C_003E9__32_0;

		public static Func<UILocalizer, bool> _003C_003E9__32_1;

		public static Func<ModEntry, bool> _003C_003E9__35_0;

		public static Func<ModEntry, ModPresetEntry> _003C_003E9__35_1;

		public static Func<KeyValuePair<string, PluginInfo>, string> _003C_003E9__37_0;

		public static Func<SerialMod, bool> _003C_003E9__38_0;

		public static Func<SerialMod, string> _003C_003E9__38_1;

		public static UnityAction _003C_003E9__44_0;

		public static UnityAction _003C_003E9__45_0;

		public static Func<KeyValuePair<string, PluginInfo>, string> _003C_003E9__54_1;

		public static Comparison<ModEntry> _003C_003E9__54_0;

		public static Func<string, string> _003C_003E9__60_0;

		public static Func<ModEntry, bool> _003C_003E9__71_0;

		public static Func<Button, bool> _003C_003E9__74_0;

		public static Func<CachedVersionEntry, bool> _003C_003E9__85_0;

		public static Func<CachedVersionEntry, string> _003C_003E9__85_1;

		public static Func<IGrouping<string, CachedVersionEntry>, CachedVersionEntry> _003C_003E9__85_2;

		public static Func<CachedVersionEntry, string> _003C_003E9__85_3;

		public static Func<char, char> _003C_003E9__86_0;

		internal bool _003CRefreshPresetButtons_003Eb__28_1(ModPresetEntry entry)
		{
			return entry.Enabled;
		}

		internal bool _003CFindSlotSpriteSource_003Eb__30_0(Image candidate)
		{
			if ((Object)(object)candidate != (Object)null)
			{
				return (Object)(object)candidate.sprite != (Object)null;
			}
			return false;
		}

		internal bool _003CFindBackButtonRect_003Eb__32_0(Button button)
		{
			if ((Object)(object)button != (Object)null)
			{
				return string.Equals(((Object)button).name, "Back", StringComparison.Ordinal);
			}
			return false;
		}

		internal bool _003CFindBackButtonRect_003Eb__32_1(UILocalizer candidate)
		{
			if ((Object)(object)candidate != (Object)null)
			{
				return string.Equals(candidate.key, "back", StringComparison.OrdinalIgnoreCase);
			}
			return false;
		}

		internal bool _003CSavePreset_003Eb__35_0(ModEntry entry)
		{
			return entry?.CanToggle ?? false;
		}

		internal ModPresetEntry _003CSavePreset_003Eb__35_1(ModEntry entry)
		{
			return new ModPresetEntry
			{
				Guid = entry.Guid,
				PluginPath = entry.PluginPath,
				Enabled = GetDesiredEnabled(entry)
			};
		}

		internal string _003CCaptureLoadedMods_003Eb__37_0(KeyValuePair<string, PluginInfo> pair)
		{
			return SafeReadMetadataName(pair.Value) ?? pair.Key;
		}

		internal bool _003CDescribeMods_003Eb__38_0(SerialMod mod)
		{
			return mod != null;
		}

		internal string _003CDescribeMods_003Eb__38_1(SerialMod mod)
		{
			return mod.Name;
		}

		internal void _003CPrepareSpawnedRow_003Eb__44_0()
		{
			Application.OpenURL("https://www.nexusmods.com/scavprototype/mods");
		}

		internal void _003CEnsureOpenNexusButton_003Eb__45_0()
		{
			Application.OpenURL("https://www.nexusmods.com/scavprototype/mods");
		}

		internal string _003CRebuildCache_003Eb__54_1(KeyValuePair<string, PluginInfo> pair)
		{
			return SafeReadMetadataName(pair.Value) ?? pair.Key;
		}

		internal int _003CRebuildCache_003Eb__54_0(ModEntry a, ModEntry b)
		{
			return string.Compare(a?.Name, b?.Name, StringComparison.OrdinalIgnoreCase);
		}

		internal string _003CGetNexusAutofillEntries_003Eb__60_0(string value)
		{
			return value;
		}

		internal bool _003CHasVisibleEntries_003Eb__71_0(ModEntry entry)
		{
			if (entry != null && !entry.IsSelf)
			{
				return !entry.IsCore;
			}
			return false;
		}

		internal bool _003CFindResetButton_003Eb__74_0(Button button)
		{
			if ((Object)(object)button != (Object)null)
			{
				return string.Equals(((Object)button).name, "Reset", StringComparison.Ordinal);
			}
			return false;
		}

		internal bool _003CSaveVersionCache_003Eb__85_0(CachedVersionEntry entry)
		{
			if (entry != null)
			{
				return !string.IsNullOrWhiteSpace(entry.Version);
			}
			return false;
		}

		internal string _003CSaveVersionCache_003Eb__85_1(CachedVersionEntry entry)
		{
			return entry.PluginPath ?? string.Empty;
		}

		internal CachedVersionEntry _003CSaveVersionCache_003Eb__85_2(IGrouping<string, CachedVersionEntry> group)
		{
			return group.First();
		}

		internal string _003CSaveVersionCache_003Eb__85_3(CachedVersionEntry entry)
		{
			return entry.DllName;
		}

		internal char _003CBuildSettingId_003Eb__86_0(char ch)
		{
			if (!char.IsLetterOrDigit(ch) && ch != '.')
			{
				return '_';
			}
			return char.ToLowerInvariant(ch);
		}
	}

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
				ModOptionsMenu.Register(ModOptionDefinition.Bool(modEntry.SettingId, name, guid, "Mods", isEnabled).WithoutSavedValueRestore());
			}
		}
	}

	internal static void RefreshMenuAffordances(SettingsMenu menu, SettingCategory category)
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Invalid comparison between Unknown and I4
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Expected I4, but got Unknown
		//IL_0175: Unknown result type (might be due to invalid IL or missing references)
		//IL_017f: Expected O, but got Unknown
		//IL_018c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0196: Expected O, but got Unknown
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0108: Unknown result type (might be due to invalid IL or missing references)
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
		//IL_0124: Unknown result type (might be due to invalid IL or missing references)
		//IL_0132: Unknown result type (might be due to invalid IL or missing references)
		//IL_0139: Unknown result type (might be due to invalid IL or missing references)
		//IL_014f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0154: Unknown result type (might be due to invalid IL or missing references)
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
			GameObject obj3 = Object.Instantiate<GameObject>(((Component)val2).gameObject, ((Component)val2).transform.parent, false);
			((Object)obj3).name = "QoL_ModRestartButton";
			val3 = obj3.GetComponent<Button>();
			RectTransform component = ((Component)val2).GetComponent<RectTransform>();
			RectTransform component2 = obj3.GetComponent<RectTransform>();
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
		((UnityEvent)val3.onClick).AddListener(new UnityAction(CloseGame));
		TextMeshProUGUI componentInChildren = ((Component)val3).GetComponentInChildren<TextMeshProUGUI>(true);
		if ((Object)(object)componentInChildren != (Object)null)
		{
			((TMP_Text)componentInChildren).text = "Close Game";
			Component component3 = ((Component)componentInChildren).GetComponent("UILocalizer");
			if ((Object)(object)component3 != (Object)null)
			{
				Object.Destroy((Object)(object)component3);
			}
		}
		UITooltip val4 = ((Component)val3).GetComponent<UITooltip>();
		if ((Object)(object)val4 == (Object)null)
		{
			val4 = ((Component)val3).gameObject.AddComponent<UITooltip>();
		}
		val4.skipLocale = true;
		val4.tipName = "Close Game";
		val4.tipDesc = "Closes the game and applies any queued mod toggles. Relaunch to load the new set.";
		RefreshPresetButtons(menu, val2);
	}

	private static void RefreshPresetButtons(SettingsMenu menu, Button resetButton)
	{
		//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
		//IL_0114: Unknown result type (might be due to invalid IL or missing references)
		//IL_0120: Unknown result type (might be due to invalid IL or missing references)
		//IL_012c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0148: Unknown result type (might be due to invalid IL or missing references)
		//IL_015e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0163: Unknown result type (might be due to invalid IL or missing references)
		//IL_016f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0179: Expected O, but got Unknown
		//IL_0190: Unknown result type (might be due to invalid IL or missing references)
		//IL_019a: Expected O, but got Unknown
		//IL_0248: Unknown result type (might be due to invalid IL or missing references)
		//IL_0241: Unknown result type (might be due to invalid IL or missing references)
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
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Expected O, but got Unknown
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0119: Unknown result type (might be due to invalid IL or missing references)
		//IL_0120: Unknown result type (might be due to invalid IL or missing references)
		//IL_012b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0136: Unknown result type (might be due to invalid IL or missing references)
		//IL_0140: Unknown result type (might be due to invalid IL or missing references)
		//IL_0173: Unknown result type (might be due to invalid IL or missing references)
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
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
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
		if (!((Object)(object)val4 != (Object)null))
		{
			return null;
		}
		return ((Component)val4).GetComponent<RectTransform>();
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
		foreach (KeyValuePair<string, PluginInfo> item in Chainloader.PluginInfos.OrderBy<KeyValuePair<string, PluginInfo>, string>((KeyValuePair<string, PluginInfo> pair) => SafeReadMetadataName(pair.Value) ?? pair.Key, StringComparer.OrdinalIgnoreCase))
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
		SettingsMenu.OpenMenu(((Object)(object)PreRunScript.instance != (Object)null) ? ((Component)PreRunScript.instance.mainCanvas).transform : null);
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
		if (!string.IsNullOrWhiteSpace(saved.Name))
		{
			return string.Equals(saved.Name, entry.Name, StringComparison.OrdinalIgnoreCase);
		}
		return false;
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
		if (!string.IsNullOrWhiteSpace(candidate.Guid))
		{
			return string.Equals(candidate.Guid, entry.Guid, StringComparison.OrdinalIgnoreCase);
		}
		return false;
	}

	internal static void PrepareSpawnedRow(GameObject row, Setting setting)
	{
		//IL_0232: Unknown result type (might be due to invalid IL or missing references)
		//IL_0237: Unknown result type (might be due to invalid IL or missing references)
		//IL_023d: Expected O, but got Unknown
		if ((Object)(object)row == (Object)null || setting == null || !TryGetEntryBySettingId(setting.name, out var entry))
		{
			return;
		}
		TextMeshProUGUI component = ((Component)row.transform.GetChild(0)).GetComponent<TextMeshProUGUI>();
		if ((Object)(object)component != (Object)null)
		{
			bool num = IsEnabledOnDisk(entry);
			bool desiredEnabled = GetDesiredEnabled(entry);
			bool flag = num != desiredEnabled;
			string text = (num ? "Enabled" : "Disabled");
			string text2 = (num ? "#FFEA00" : "#FF5A5A");
			string text3 = (string.IsNullOrWhiteSpace(entry.Version) ? string.Empty : (" <color=#9a9a9a>(v" + entry.Version + ")</color>"));
			string text4 = ((entry.HasUpdate && !string.IsNullOrWhiteSpace(entry.LatestVersion)) ? (" <color=#E6A15A>(v" + entry.LatestVersion + " Nexus version)</color>") : string.Empty);
			string text5 = (flag ? ("  <color=#7CFF7C>Queued: " + (desiredEnabled ? "Enable" : "Disable") + "</color>") : string.Empty);
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
			object obj = _003C_003Ec._003C_003E9__44_0;
			if (obj == null)
			{
				UnityAction val2 = delegate
				{
					Application.OpenURL("https://www.nexusmods.com/scavprototype/mods");
				};
				_003C_003Ec._003C_003E9__44_0 = val2;
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
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0104: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01af: Expected O, but got Unknown
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
		object obj = _003C_003Ec._003C_003E9__45_0;
		if (obj == null)
		{
			UnityAction val3 = delegate
			{
				Application.OpenURL("https://www.nexusmods.com/scavprototype/mods");
			};
			_003C_003Ec._003C_003E9__45_0 = val3;
			obj = (object)val3;
		}
		((UnityEvent)onClick).AddListener((UnityAction)obj);
	}

	private static Button CreateOpenNexusButton(Transform parent)
	{
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Expected O, but got Unknown
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0104: Unknown result type (might be due to invalid IL or missing references)
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
		//IL_011d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0128: Unknown result type (might be due to invalid IL or missing references)
		//IL_0133: Unknown result type (might be due to invalid IL or missing references)
		//IL_013d: Unknown result type (might be due to invalid IL or missing references)
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
			entry.IsEnabled = enabled;
			if (refresh)
			{
				SettingsMenuCategoryExtender.RefreshLiveMenu();
			}
			return;
		}
		PendingModState pendingModState = new PendingModState
		{
			PluginPath = entry.PluginPath,
			Guid = entry.Guid,
			DesiredEnabled = enabled
		};
		if (TryApplyPendingRename(pendingModState, out var error))
		{
			PendingStates.Remove(entry.PluginPath);
			entry.IsEnabled = enabled;
		}
		else
		{
			PendingStates[entry.PluginPath] = pendingModState;
			ManualLogSource obj = logger;
			if (obj != null)
			{
				obj.LogWarning((object)("Could not toggle '" + entry.Guid + "' now: " + error + ". It will be retried when the game closes."));
			}
		}
		if (refresh)
		{
			SettingsMenuCategoryExtender.RefreshLiveMenu();
		}
	}

	private static bool TryApplyPendingRename(PendingModState pendingState, out string error)
	{
		error = null;
		if (pendingState == null || string.IsNullOrWhiteSpace(pendingState.PluginPath))
		{
			return true;
		}
		string pluginPath = pendingState.PluginPath;
		string text = pluginPath + ".disabled";
		try
		{
			if (pendingState.DesiredEnabled)
			{
				if (File.Exists(pluginPath))
				{
					return true;
				}
				if (!File.Exists(text))
				{
					error = "the mod file is missing";
					return false;
				}
				File.Move(text, pluginPath);
				return true;
			}
			if (!File.Exists(pluginPath))
			{
				return true;
			}
			if (File.Exists(text))
			{
				File.Delete(text);
			}
			File.Move(pluginPath, text);
			return true;
		}
		catch (Exception ex)
		{
			error = ex.Message;
			return false;
		}
	}

	private static void FlushPendingStates(string context)
	{
		if (PendingStates.Count == 0)
		{
			return;
		}
		List<string> list = new List<string>();
		foreach (KeyValuePair<string, PendingModState> pendingState in PendingStates)
		{
			PendingModState value = pendingState.Value;
			if (TryApplyPendingRename(value, out var error))
			{
				list.Add(pendingState.Key);
				continue;
			}
			ManualLogSource obj = logger;
			if (obj != null)
			{
				obj.LogWarning((object)("Could not apply the " + (value.DesiredEnabled ? "enable" : "disable") + " for '" + value.Guid + "' during " + context + ": " + error));
			}
		}
		foreach (string item in list)
		{
			PendingStates.Remove(item);
		}
	}

	private static void CloseGame()
	{
		FlushPendingStates("game close");
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

	private static void BeginVersionsFetch()
	{
		if (!versionsFetchStarted)
		{
			versionsFetchStarted = true;
			SharedCoroutineStarter.StartCoroutine(WaitForVersionsFetchCoroutine(FetchNexusVersionsAsync()));
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
		_ = 1;
		try
		{
			HttpClient client = new HttpClient();
			try
			{
				client.Timeout = TimeSpan.FromSeconds(20.0);
				client.DefaultRequestHeaders.UserAgent.ParseAdd("QoLUnknownMod");
				HttpResponseMessage response = await client.GetAsync("https://jimmyking9999999.github.io/Metadata-generator/nexusmods-versions.json").ConfigureAwait(continueOnCapturedContext: false);
				byte[] bytes = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(continueOnCapturedContext: false);
				string text = Encoding.UTF8.GetString(bytes);
				if (!response.IsSuccessStatusCode)
				{
					ManualLogSource obj = logger;
					if (obj != null)
					{
						obj.LogWarning((object)$"Failed to fetch Nexus mod versions: {(int)response.StatusCode} {response.ReasonPhrase}");
					}
					return;
				}
				Dictionary<string, string> dictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(text);
				lock (VersionsSync)
				{
					LatestVersionByGuid.Clear();
					if (dictionary != null)
					{
						foreach (KeyValuePair<string, string> item in dictionary)
						{
							if (!string.IsNullOrWhiteSpace(item.Key) && !string.IsNullOrWhiteSpace(item.Value))
							{
								LatestVersionByGuid[item.Key.Trim()] = item.Value.Trim();
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
			ManualLogSource obj2 = logger;
			if (obj2 != null)
			{
				obj2.LogWarning((object)("Failed to fetch or parse Nexus mod versions: " + ex.Message));
			}
		}
	}

	private static void RebuildCache()
	{
		CachedEntries.Clear();
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (KeyValuePair<string, PluginInfo> item in Chainloader.PluginInfos.OrderBy<KeyValuePair<string, PluginInfo>, string>((KeyValuePair<string, PluginInfo> pair) => SafeReadMetadataName(pair.Value) ?? pair.Key, StringComparer.OrdinalIgnoreCase))
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
			IEnumerable<string> enumerable;
			try
			{
				enumerable = Directory.EnumerateFiles(pluginsDirectory, "*.dll.disabled", SearchOption.AllDirectories).ToList();
			}
			catch (Exception ex)
			{
				ManualLogSource obj = logger;
				if (obj != null)
				{
					obj.LogWarning((object)("Failed to scan for disabled mods in " + pluginsDirectory + ": " + ex.Message));
				}
				enumerable = Enumerable.Empty<string>();
			}
			foreach (string item2 in enumerable)
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
		if (BuildNexusAliasMap().TryGetValue(alias.Trim(), out nexusPageUrl))
		{
			return !string.IsNullOrWhiteSpace(nexusPageUrl);
		}
		return false;
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
			return dictionary;
		}
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
		return CompareVersionStrings(installedVersion, latestVersion) < 0;
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
		if (!string.IsNullOrWhiteSpace(version))
		{
			return version.Trim().TrimStart('v', 'V');
		}
		return null;
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
		if (entry == null || string.IsNullOrWhiteSpace(entry.PluginPath) || !PendingStates.TryGetValue(entry.PluginPath, out var value))
		{
			return entry?.IsEnabled ?? false;
		}
		return value.DesiredEnabled;
	}

	private static bool IsEnabledOnDisk(ModEntry entry)
	{
		if (entry != null && !string.IsNullOrWhiteSpace(entry.PluginPath))
		{
			return File.Exists(entry.PluginPath);
		}
		return false;
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
		File.Exists(location + ".disabled");
		return location;
	}

	private static bool IsCorePlugin(string pluginPath)
	{
		if (string.IsNullOrWhiteSpace(pluginPath))
		{
			return true;
		}
		return pluginPath.Replace('/', '\\').IndexOf("\\BepInEx\\core\\", StringComparison.OrdinalIgnoreCase) >= 0;
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
		if (!string.IsNullOrWhiteSpace(pluginPath) && string.Equals(Path.GetFileNameWithoutExtension(pluginPath), "QoL Unknown", StringComparison.OrdinalIgnoreCase))
		{
			return true;
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
		char[] value = guid.Select((char ch) => (!char.IsLetterOrDigit(ch) && ch != '.') ? '_' : char.ToLowerInvariant(ch)).ToArray();
		return "mods." + new string(value);
	}
}
