using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using HarmonyLib;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;
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

		public bool HasDllMetadataMismatch;

		public string LatestVersion;

		public string NexusPageUrl;

		public bool CanToggle => !IsCore && !IsSelf && !string.IsNullOrWhiteSpace(PluginPath);
	}

	private sealed class PendingModState
	{
		public string PluginPath;

		public string Guid;

		public bool DesiredEnabled;
	}

	private sealed class CachedVersionEntry
	{
		public string PluginPath { get; set; }

		public string DllName { get; set; }

		public string Version { get; set; }
	}

	private sealed class NexusMetadataEntry
	{
		[JsonProperty("Version")]
		public string Version { get; set; }

		[JsonProperty("bepinexVersion")]
		public string BepinexVersion { get; set; }

		[JsonProperty("DownloadUrl")]
		public string DownloadUrl { get; set; }

		[JsonProperty("dllNames")]
		public List<string> DllNames { get; set; }

		[JsonProperty("dllVersions")]
		public Dictionary<string, string> DllVersions { get; set; }

		[JsonProperty("Links")]
		public NexusMetadataLinks Links { get; set; }

		[JsonProperty("NexusModId")]
		public int NexusModId { get; set; }
	}

	private sealed class NexusMetadataLinks
	{
		[JsonProperty("NexusMods")]
		public string NexusMods { get; set; }
	}

	private const uint SteamAppIdDemo = 4576510u;

	private const uint SteamAppIdPlaytest = 4584420u;

	private const string CategoryName = "Mods";

	private const string DisabledExtension = ".disabled";

	private const string RestartButtonName = "QoL_ModRestartButton";

	private const string RowNexusButtonName = "QoL_OpenNexusButton";

	private const string NexusMetadataUrl = "https://raw.githubusercontent.com/jimmyking9999999/Metadata-generator/main/nexusmods.json";

	private const string NexusModPageFormat = "https://www.nexusmods.com/scavprototype/mods/{0}";

	private const string NexusModsListingUrl = "https://www.nexusmods.com/scavprototype/mods";

	private static readonly string VersionCachePath = Path.Combine(Application.persistentDataPath, "qol-mod-version-cache.json");

	private static readonly Dictionary<string, PendingModState> PendingStates = new Dictionary<string, PendingModState>(StringComparer.OrdinalIgnoreCase);

	private static readonly List<ModEntry> CachedEntries = new List<ModEntry>();

	private static readonly HashSet<string> RegisteredSettingIds = new HashSet<string>(StringComparer.Ordinal);

	private static readonly Dictionary<string, NexusMetadataEntry> MetadataByDllName = new Dictionary<string, NexusMetadataEntry>(StringComparer.OrdinalIgnoreCase);

	private static readonly Dictionary<string, CachedVersionEntry> VersionCacheByPluginPath = new Dictionary<string, CachedVersionEntry>(StringComparer.OrdinalIgnoreCase);

	private static readonly Dictionary<string, CachedVersionEntry> VersionCacheByDllName = new Dictionary<string, CachedVersionEntry>(StringComparer.OrdinalIgnoreCase);

	private static readonly Dictionary<string, string> HardcodedVersionOverrides = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { { "multiupdater", "0.0.1" } };

	private static readonly object MetadataSync = new object();

	private static ManualLogSource logger;

	private static bool initialized;

	private static bool pendingApply;

	private static bool metadataFetchStarted;

	private static bool metadataLoaded;

	internal static void Initialize(ManualLogSource log)
	{
		logger = log;
		if (!initialized)
		{
			initialized = true;
			LoadVersionCache();
			RebuildCache();
			BeginMetadataFetch();
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
		//IL_01a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01aa: Expected O, but got Unknown
		//IL_01b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c2: Expected O, but got Unknown
		//IL_0114: Unknown result type (might be due to invalid IL or missing references)
		//IL_0123: Unknown result type (might be due to invalid IL or missing references)
		//IL_0132: Unknown result type (might be due to invalid IL or missing references)
		//IL_0141: Unknown result type (might be due to invalid IL or missing references)
		//IL_0150: Unknown result type (might be due to invalid IL or missing references)
		//IL_0157: Unknown result type (might be due to invalid IL or missing references)
		//IL_016d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0172: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)menu == (Object)null)
		{
			return;
		}
		Transform val = FindChildRecursive(((Component)menu).transform, "QoL_ModRestartButton");
		if (!HasVisibleEntries() || (int)category != GetCategoryIndex())
		{
			if ((Object)(object)val != (Object)null)
			{
				Object.Destroy((Object)(object)((Component)val).gameObject);
			}
			return;
		}
		Button val2 = FindResetButton(menu);
		if ((Object)(object)val2 == (Object)null)
		{
			ManualLogSource obj = logger;
			if (obj != null)
			{
				obj.LogWarning((object)"Mods tab could not find the SettingsMenu Reset button to clone.");
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
	}

	internal static void PrepareSpawnedRow(GameObject row, Setting setting)
	{
		//IL_0292: Unknown result type (might be due to invalid IL or missing references)
		//IL_029c: Expected O, but got Unknown
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
			string text4 = ((entry.HasDllMetadataMismatch && !string.IsNullOrWhiteSpace(entry.LatestVersion)) ? (" <color=#E6A15A>(v" + entry.LatestVersion + " Nexus version)</color>") : string.Empty);
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
		((Selectable)val).interactable = entry.HasUpdate && !string.IsNullOrWhiteSpace(entry.NexusPageUrl);
		if (((Selectable)val).interactable)
		{
			string nexusPageUrl = entry.NexusPageUrl;
			((UnityEvent)val.onClick).AddListener((UnityAction)delegate
			{
				Application.OpenURL(nexusPageUrl);
			});
		}
		UITooltip val2 = ((Component)row.transform.GetChild(0)).GetComponent<UITooltip>();
		if ((Object)(object)val2 == (Object)null)
		{
			val2 = ((Component)row.transform.GetChild(0)).gameObject.AddComponent<UITooltip>();
		}
		val2.skipLocale = true;
		val2.tipName = entry.Name;
		val2.tipDesc = ((entry.HasUpdate && !string.IsNullOrWhiteSpace(entry.NexusPageUrl)) ? (entry.Guid + "\nUpdate available! click to open Nexus page") : entry.Guid);
		EnsureOpenNexusButton(row, entry);
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
		SettingBool val3 = Settings.Get<SettingBool>(entry.SettingId);
		if (val3 != null)
		{
			val3.value = desiredEnabled2;
		}
		if (canToggle)
		{
			((UnityEvent<bool>)(object)component3.onValueChanged).AddListener((UnityAction<bool>)delegate(bool isOn)
			{
				SetEntryDesiredState(entry, isOn);
			});
		}
	}

	private static void EnsureOpenNexusButton(GameObject row, ModEntry entry)
	{
		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_010c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0123: Unknown result type (might be due to invalid IL or missing references)
		//IL_0132: Unknown result type (might be due to invalid IL or missing references)
		//IL_0141: Unknown result type (might be due to invalid IL or missing references)
		//IL_0146: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0205: Expected O, but got Unknown
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
		string nexusPageUrl = GetOpenNexusUrl(entry);
		((Selectable)val2).interactable = true;
		((Component)val2).gameObject.SetActive(true);
		((UnityEvent)val2.onClick).AddListener((UnityAction)delegate
		{
			Application.OpenURL(nexusPageUrl);
		});
	}

	private static string GetOpenNexusUrl(ModEntry entry)
	{
		if (entry != null && !string.IsNullOrWhiteSpace(entry.NexusPageUrl))
		{
			return entry.NexusPageUrl;
		}
		return "https://www.nexusmods.com/scavprototype/mods";
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

	private static void SetEntryDesiredState(ModEntry entry, bool enabled)
	{
		if (entry != null && entry.CanToggle && !string.IsNullOrWhiteSpace(entry.PluginPath))
		{
			bool flag = IsEnabledOnDisk(entry);
			if (enabled == flag)
			{
				PendingStates.Remove(entry.PluginPath);
				pendingApply = PendingStates.Count > 0;
				SettingsMenuCategoryExtender.RefreshLiveMenu();
				return;
			}
			PendingStates[entry.PluginPath] = new PendingModState
			{
				PluginPath = entry.PluginPath,
				Guid = entry.Guid,
				DesiredEnabled = enabled
			};
			pendingApply = true;
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
			if ((AccessTools.TypeByName("KrokoshaCasualtiesMP.KSteam")?.GetProperty("STEAM_APPID", BindingFlags.Static | BindingFlags.Public))?.GetValue(null, null) is uint value)
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

	private static void BeginMetadataFetch()
	{
		if (!metadataFetchStarted)
		{
			metadataFetchStarted = true;
			Task metadataTask = FetchNexusMetadataAsync();
			SharedCoroutineStarter.StartCoroutine(WaitForMetadataFetchCoroutine(metadataTask));
		}
	}

	private static IEnumerator WaitForMetadataFetchCoroutine(Task metadataTask)
	{
		if (metadataTask != null)
		{
			while (!metadataTask.IsCompleted)
			{
				yield return null;
			}
			RebuildCache();
			SettingsMenuCategoryExtender.RefreshLiveMenu();
		}
	}

	private static async Task FetchNexusMetadataAsync()
	{
		try
		{
			HttpClient client = new HttpClient();
			try
			{
				client.Timeout = TimeSpan.FromSeconds(20.0);
				client.DefaultRequestHeaders.UserAgent.ParseAdd("QoLUnknownMod");
				HttpResponseMessage response = await client.GetAsync("https://raw.githubusercontent.com/jimmyking9999999/Metadata-generator/main/nexusmods.json").ConfigureAwait(continueOnCapturedContext: false);
				byte[] bytes = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(continueOnCapturedContext: false);
				string responseText = Encoding.UTF8.GetString(bytes);
				if (!response.IsSuccessStatusCode)
				{
					ManualLogSource obj = logger;
					if (obj != null)
					{
						obj.LogWarning((object)$"Failed to fetch Nexus mod metadata: {(int)response.StatusCode} {response.ReasonPhrase}");
					}
					return;
				}
				List<NexusMetadataEntry> entries = JsonConvert.DeserializeObject<List<NexusMetadataEntry>>(responseText);
				Dictionary<string, NexusMetadataEntry> mappedEntries = new Dictionary<string, NexusMetadataEntry>(StringComparer.OrdinalIgnoreCase);
				if (entries != null)
				{
					for (int i = 0; i < entries.Count; i++)
					{
						NexusMetadataEntry entry = entries[i];
						if (entry?.DllNames == null)
						{
							continue;
						}
						for (int j = 0; j < entry.DllNames.Count; j++)
						{
							string dllName = entry.DllNames[j];
							if (!string.IsNullOrWhiteSpace(dllName))
							{
								mappedEntries[dllName.Trim()] = entry;
							}
						}
					}
				}
				lock (MetadataSync)
				{
					MetadataByDllName.Clear();
					foreach (KeyValuePair<string, NexusMetadataEntry> pair in mappedEntries)
					{
						MetadataByDllName[pair.Key] = pair.Value;
					}
				}
				metadataLoaded = true;
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
				obj2.LogWarning((object)("Failed to fetch or parse Nexus mod metadata: " + ex2.Message));
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
			string value = (AccessTools.TypeByName("KrokoshaCasualtiesMP.KSteam")?.GetMethod("GetAppInstallDir", BindingFlags.Static | BindingFlags.Public))?.Invoke(null, null) as string;
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
				string version = ResolveLoadedPluginVersion(value, text);
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
				ApplyMetadata(modEntry);
				UpdateCachedVersion(modEntry);
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
					ApplyMetadata(modEntry2);
					CachedEntries.Add(modEntry2);
				}
			}
		}
		CachedEntries.Sort((ModEntry a, ModEntry b) => string.Compare(a?.Name, b?.Name, StringComparison.OrdinalIgnoreCase));
	}

	private static void ApplyCachedVersion(ModEntry entry)
	{
		if (entry == null || !string.IsNullOrWhiteSpace(entry.Version) || TryApplyHardcodedVersionOverride(entry) || TryApplyVersionFromDisk(entry))
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
		if (value != null && !string.IsNullOrWhiteSpace(value.Version))
		{
			entry.Version = value.Version;
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

	private static bool TryApplyVersionFromDisk(ModEntry entry)
	{
		if (entry == null)
		{
			return false;
		}
		string text = TryReadAssemblyVersion(entry.PluginPath);
		if (string.IsNullOrWhiteSpace(text) && !string.IsNullOrWhiteSpace(entry.PluginPath))
		{
			text = TryReadAssemblyVersion(entry.PluginPath + ".disabled");
		}
		if (!IsUsableDisplayVersion(text))
		{
			return false;
		}
		entry.Version = text;
		return true;
	}

	private static string TryReadAssemblyVersion(string path)
	{
		if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
		{
			return null;
		}
		try
		{
			Version version = AssemblyName.GetAssemblyName(path)?.Version;
			return (version != null) ? version.ToString() : null;
		}
		catch
		{
			return null;
		}
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

	private static void ApplyMetadata(ModEntry entry)
	{
		if (entry == null || !metadataLoaded || string.IsNullOrWhiteSpace(entry.PluginPath))
		{
			return;
		}
		string fileName = Path.GetFileName(entry.PluginPath);
		NexusMetadataEntry value = null;
		if (!string.IsNullOrWhiteSpace(fileName))
		{
			lock (MetadataSync)
			{
				MetadataByDllName.TryGetValue(fileName, out value);
			}
		}
		if (!string.IsNullOrWhiteSpace(fileName) && value != null)
		{
			string latestMetadataVersion = GetLatestMetadataVersion(value, fileName);
			if (!IsUsableDisplayVersion(entry.Version) && IsUsableDisplayVersion(latestMetadataVersion))
			{
				entry.Version = latestMetadataVersion;
			}
			entry.NexusPageUrl = BuildNexusPageUrl(value.NexusModId) ?? ((value.Links != null) ? value.Links.NexusMods : null);
			entry.LatestVersion = latestMetadataVersion;
			entry.HasDllMetadataMismatch = IsDllMetadataMismatch(entry.Version, latestMetadataVersion);
			entry.HasUpdate = IsUpdateAvailable(entry.Version, latestMetadataVersion);
		}
	}

	private static string GetLatestMetadataVersion(NexusMetadataEntry metadata, string dllName)
	{
		if (metadata == null)
		{
			return null;
		}
		if (!string.IsNullOrWhiteSpace(metadata.BepinexVersion))
		{
			return metadata.BepinexVersion;
		}
		if (!string.IsNullOrWhiteSpace(dllName) && metadata.DllVersions != null && metadata.DllVersions.TryGetValue(dllName, out var value) && !string.IsNullOrWhiteSpace(value))
		{
			return value;
		}
		return metadata.Version;
	}

	private static string BuildNexusPageUrl(int nexusModId)
	{
		return (nexusModId > 0) ? $"https://www.nexusmods.com/scavprototype/mods/{nexusModId}" : null;
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

	private static bool IsDllMetadataMismatch(string installedVersion, string latestVersion)
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

	private static string ResolveLoadedPluginVersion(object pluginInfo, string pluginPath)
	{
		string text = SafeReadLoadedAssemblyVersion(pluginInfo);
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		string text2 = TryReadAssemblyVersion(pluginPath);
		if (!string.IsNullOrWhiteSpace(text2))
		{
			return text2;
		}
		return SafeReadMetadataVersion(pluginInfo);
	}

	private static string SafeReadLoadedAssemblyVersion(object pluginInfo)
	{
		try
		{
			return (((pluginInfo.GetType().GetProperty("Instance")?.GetValue(pluginInfo, null))?.GetType().Assembly)?.GetName().Version)?.ToString();
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
