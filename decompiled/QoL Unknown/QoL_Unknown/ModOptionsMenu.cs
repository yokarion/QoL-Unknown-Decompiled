using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;

namespace QoL_Unknown;

public static class ModOptionsMenu
{
	private const string HarmonyId = "org.bepinex.plugins.qol_unknown.modoptionsmenu";

	private const int CustomCategoryBaseIndex = 100;

	private static readonly List<ModOptionDefinition> Definitions = new List<ModOptionDefinition>();

	private static readonly Dictionary<string, ModOptionDefinition> DefinitionsById = new Dictionary<string, ModOptionDefinition>(StringComparer.Ordinal);

	private static readonly Dictionary<string, string> LiteralLocale = new Dictionary<string, string>(StringComparer.Ordinal);

	private static readonly List<ModCustomSettingDefinition> CustomDefinitions = new List<ModCustomSettingDefinition>();

	private static readonly HashSet<string> RegisteredIds = new HashSet<string>(StringComparer.Ordinal);

	private static readonly List<ModOptionCategoryEntry> CustomCategories = new List<ModOptionCategoryEntry>();

	private static readonly Dictionary<string, ModOptionCategoryEntry> CustomCategoriesByKey = new Dictionary<string, ModOptionCategoryEntry>(StringComparer.Ordinal);

	private static ManualLogSource Logger;

	private static bool initialized;

	public static void Initialize(ManualLogSource logger)
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Expected O, but got Unknown
		Logger = logger;
		if (initialized)
		{
			return;
		}
		initialized = true;
		try
		{
			Harmony harmony = new Harmony("org.bepinex.plugins.qol_unknown.modoptionsmenu");
			PatchExplicit(harmony);
		}
		catch (Exception arg)
		{
			ManualLogSource logger2 = Logger;
			if (logger2 != null)
			{
				logger2.LogError((object)$"Failed to initialize mod options menu patches: {arg}");
			}
		}
	}

	public static bool Register(ModOptionDefinition option)
	{
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		if (option == null || string.IsNullOrWhiteSpace(option.Id) || string.IsNullOrWhiteSpace(option.Label))
		{
			ManualLogSource logger = Logger;
			if (logger != null)
			{
				logger.LogError((object)"Skipping invalid mod option registration.");
			}
			return false;
		}
		if (!RegisteredIds.Add(option.Id))
		{
			ManualLogSource logger2 = Logger;
			if (logger2 != null)
			{
				logger2.LogError((object)("Skipping duplicate mod option '" + option.Id + "'."));
			}
			return false;
		}
		if (option.UsesCustomCategory)
		{
			option.SetResolvedCategory(ResolveCustomCategory(option.CustomCategory));
		}
		Definitions.Add(option);
		DefinitionsById[option.Id] = option;
		RegisterLiteralLocale(option.Id, option.Label, option.Description);
		if (option.Kind == ModOptionKind.Dropdown && option.Choices != null)
		{
			for (int i = 0; i < option.Choices.Length; i++)
			{
				ModDropdownChoice modDropdownChoice = option.Choices[i];
				if (modDropdownChoice != null)
				{
					LiteralLocale["gameset" + option.Id + modDropdownChoice.Key] = modDropdownChoice.Label;
				}
			}
		}
		SyncRegisteredDefinitionsIntoLoadedSettings(restoreSavedValues: true, applyAddedSettings: true);
		return true;
	}

	public static bool RegisterCustom(ModCustomSettingDefinition definition)
	{
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		if (definition == null || string.IsNullOrWhiteSpace(definition.Id) || string.IsNullOrWhiteSpace(definition.Label))
		{
			ManualLogSource logger = Logger;
			if (logger != null)
			{
				logger.LogError((object)"Skipping invalid custom mod option registration.");
			}
			return false;
		}
		if (!RegisteredIds.Add(definition.Id))
		{
			ManualLogSource logger2 = Logger;
			if (logger2 != null)
			{
				logger2.LogError((object)("Skipping duplicate custom mod option '" + definition.Id + "'."));
			}
			return false;
		}
		if (!string.IsNullOrWhiteSpace(definition.CustomCategory))
		{
			definition.Category = ResolveCustomCategory(definition.CustomCategory);
		}
		CustomDefinitions.Add(definition);
		RegisterLiteralLocale(definition.Id, definition.Label, definition.Description);
		SettingsMenuCategoryExtender.RefreshLiveMenu();
		return true;
	}

	internal static List<ModOptionCategoryEntry> GetCustomCategories()
	{
		return CustomCategories.ToList();
	}

	internal static List<ModCustomSettingDefinition> GetVisibleCustomDefinitions(SettingCategory? category = null)
	{
		return CustomDefinitions.Where((ModCustomSettingDefinition definition) => definition != null && (!category.HasValue || definition.Category == category.Value) && (definition.IsVisible == null || definition.IsVisible())).ToList();
	}

	internal static bool TryGetCustomCategoryDisplayName(SettingCategory category, out string displayName)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		ModOptionCategoryEntry modOptionCategoryEntry = CustomCategories.FirstOrDefault((ModOptionCategoryEntry candidate) => candidate != null && candidate.CategoryIndex == (int)category);
		if (modOptionCategoryEntry != null)
		{
			displayName = modOptionCategoryEntry.DisplayName;
			return true;
		}
		displayName = null;
		return false;
	}

	internal static bool TryGetLiteralLocale(string key, out string value)
	{
		return LiteralLocale.TryGetValue(key, out value);
	}

	internal static bool IsRegistered(string id)
	{
		return !string.IsNullOrEmpty(id) && RegisteredIds.Contains(id);
	}

	private static void RegisterLiteralLocale(string id, string label, string description)
	{
		LiteralLocale["gameset" + id] = label;
		if (!string.IsNullOrWhiteSpace(description))
		{
			LiteralLocale["gameset" + id + "dsc"] = description;
		}
	}

	internal static void EnsureRegisteredDefinitionsInLiveSettings(bool restoreSavedValues, bool applyAddedSettings)
	{
		SyncRegisteredDefinitionsIntoLoadedSettings(restoreSavedValues, applyAddedSettings);
	}

	private static void SyncRegisteredDefinitionsIntoLoadedSettings(bool restoreSavedValues, bool applyAddedSettings)
	{
		if (Settings.settings == null)
		{
			return;
		}
		bool flag = false;
		for (int i = 0; i < Definitions.Count; i++)
		{
			ModOptionDefinition definition = Definitions[i];
			if (definition != null && !Settings.settings.Any((Setting setting) => setting != null && setting.name == definition.Id))
			{
				Setting val = definition.CreateSetting();
				if (restoreSavedValues && definition.RestoreSavedValue)
				{
					RestoreSavedSettingValue(val);
				}
				Settings.settings.Add(val);
				if (applyAddedSettings)
				{
					val.Apply();
				}
				flag = true;
			}
		}
		if (flag)
		{
			ManualLogSource logger = Logger;
			if (logger != null)
			{
				logger.LogDebug((object)$"Merged {Definitions.Count} registered mod settings into the live Settings list.");
			}
		}
	}

	private static void RestoreSavedSettingValue(Setting setting)
	{
		if (setting == null || string.IsNullOrEmpty(setting.name))
		{
			return;
		}
		try
		{
			string path = Path.Combine(Application.persistentDataPath, "settings.json");
			if (!File.Exists(path))
			{
				return;
			}
			List<SettingSaveData> list = JsonConvert.DeserializeObject<List<SettingSaveData>>(File.ReadAllText(path));
			if (list == null)
			{
				return;
			}
			for (int i = 0; i < list.Count; i++)
			{
				SettingSaveData val = list[i];
				if (val != null && !(val.name != setting.name))
				{
					if (setting is SettingFloat)
					{
						setting.SetValue((object)Convert.ToSingle(val.value));
					}
					else if (setting is SettingInt || setting is SettingDropdown)
					{
						setting.SetValue((object)Convert.ToInt32(val.value));
					}
					else if (setting is SettingBool)
					{
						setting.SetValue((object)Convert.ToBoolean(val.value));
					}
					else if (setting is SettingKeybind)
					{
						setting.SetValue((object)(KeyCode)Convert.ToInt32(val.value));
					}
					break;
				}
			}
		}
		catch (Exception ex)
		{
			ManualLogSource logger = Logger;
			if (logger != null)
			{
				logger.LogWarning((object)("Failed to restore saved mod setting '" + setting.name + "': " + ex.Message));
			}
		}
	}

	internal static void RebuildLiveSettingsAfterReset()
	{
		Settings.settings = Settings.DefaultSettings();
		QoLModSettings.ResetCustomSettingsToDefaults();
		SyncRegisteredDefinitionsIntoLoadedSettings(restoreSavedValues: false, applyAddedSettings: false);
		Settings.ApplyAll();
		Settings.SaveSettings();
	}

	private static void AppendCustomSettings(SettingsMenu menu, SettingCategory category)
	{
		//IL_0243: Unknown result type (might be due to invalid IL or missing references)
		//IL_024e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ff: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)menu == (Object)null || (Object)(object)menu.content == (Object)null || CustomDefinitions.Count == 0)
		{
			return;
		}
		List<GameObject> list = AccessTools.Field(typeof(SettingsMenu), "spawnedSettings")?.GetValue(menu) as List<GameObject>;
		RemoveExistingCustomRows(list);
		float num = GetSpawnedSettingsHeight(list);
		for (int i = 0; i < CustomDefinitions.Count; i++)
		{
			ModCustomSettingDefinition modCustomSettingDefinition = CustomDefinitions[i];
			if (modCustomSettingDefinition == null || modCustomSettingDefinition.Category != category || (modCustomSettingDefinition.IsVisible != null && !modCustomSettingDefinition.IsVisible()))
			{
				continue;
			}
			GameObject val = Utils.Create(modCustomSettingDefinition.TemplateResourcePath, (Transform)(object)menu.content);
			if ((Object)(object)val == (Object)null)
			{
				ManualLogSource logger = Logger;
				if (logger != null)
				{
					logger.LogWarning((object)("Failed to instantiate custom row for '" + modCustomSettingDefinition.Id + "'."));
				}
				continue;
			}
			((Object)val).name = modCustomSettingDefinition.Id;
			TextMeshProUGUI component = ((Component)val.transform.GetChild(0)).GetComponent<TextMeshProUGUI>();
			if ((Object)(object)component != (Object)null)
			{
				((TMP_Text)component).text = modCustomSettingDefinition.Label;
			}
			UITooltip val2 = ((Component)val.transform.GetChild(0)).GetComponent<UITooltip>();
			if ((Object)(object)val2 == (Object)null)
			{
				val2 = ((Component)val.transform.GetChild(0)).gameObject.AddComponent<UITooltip>();
			}
			val2.skipLocale = true;
			val2.tipName = modCustomSettingDefinition.Label;
			val2.tipDesc = modCustomSettingDefinition.Description ?? string.Empty;
			modCustomSettingDefinition.Binder?.Invoke(val);
			RectTransform component2 = val.GetComponent<RectTransform>();
			component2.anchoredPosition = new Vector2(0f, 0f - num - component2.sizeDelta.y * 0.5f);
			num += component2.sizeDelta.y;
			list?.Add(val);
		}
		menu.content.sizeDelta = new Vector2(menu.content.sizeDelta.x, num);
	}

	private static void RemoveExistingCustomRows(List<GameObject> spawnedSettings)
	{
		if (spawnedSettings == null || spawnedSettings.Count == 0)
		{
			return;
		}
		for (int num = spawnedSettings.Count - 1; num >= 0; num--)
		{
			GameObject val = spawnedSettings[num];
			if (!((Object)(object)val == (Object)null) && RegisteredIds.Contains(((Object)val).name))
			{
				spawnedSettings.RemoveAt(num);
				Object.Destroy((Object)(object)val);
			}
		}
	}

	private static void RelabelStandardRows(SettingsMenu menu, SettingCategory category)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)menu == (Object)null || !(AccessTools.Field(typeof(SettingsMenu), "spawnedSettings")?.GetValue(menu) is List<GameObject> { Count: not 0 } list))
		{
			return;
		}
		List<Setting> list2 = (from setting in Settings.GetAllSettings()
			where setting != null && setting.category == category
			select setting).ToList();
		int num = Math.Min(list.Count, list2.Count);
		for (int num2 = 0; num2 < num; num2++)
		{
			GameObject val = list[num2];
			Setting val2 = list2[num2];
			if (!((Object)(object)val == (Object)null) && val2 != null && DefinitionsById.TryGetValue(val2.name, out var value))
			{
				TextMeshProUGUI component = ((Component)val.transform.GetChild(0)).GetComponent<TextMeshProUGUI>();
				if ((Object)(object)component != (Object)null)
				{
					((TMP_Text)component).text = value.Label;
				}
				UITooltip val3 = ((Component)val.transform.GetChild(0)).GetComponent<UITooltip>();
				if ((Object)(object)val3 == (Object)null)
				{
					val3 = ((Component)val.transform.GetChild(0)).gameObject.AddComponent<UITooltip>();
				}
				val3.skipLocale = true;
				val3.tipName = value.Label;
				val3.tipDesc = value.Description ?? string.Empty;
				ModManagementSettings.PrepareSpawnedRow(val, val2);
			}
		}
	}

	private static void RefreshSettingsMenu(SettingsMenu menu, SettingCategory category)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		RelabelStandardRows(menu, category);
		AppendCustomSettings(menu, category);
	}

	private static void PatchExplicit(Harmony harmony)
	{
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Expected O, but got Unknown
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Expected O, but got Unknown
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Expected O, but got Unknown
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0105: Expected O, but got Unknown
		//IL_0151: Unknown result type (might be due to invalid IL or missing references)
		//IL_015e: Expected O, but got Unknown
		//IL_01ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_01dc: Expected O, but got Unknown
		//IL_0198: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a5: Expected O, but got Unknown
		harmony.Patch((MethodBase)AccessTools.Method(typeof(Settings), "DefaultSettings", (Type[])null, (Type[])null), (HarmonyMethod)null, new HarmonyMethod(typeof(ModOptionsMenu), "DefaultSettingsPostfix", (Type[])null), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
		harmony.Patch((MethodBase)AccessTools.Method(typeof(Settings), "LoadSettings", (Type[])null, (Type[])null), (HarmonyMethod)null, new HarmonyMethod(typeof(ModOptionsMenu), "LoadSettingsPostfix", (Type[])null), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
		harmony.Patch((MethodBase)AccessTools.Method(typeof(Locale), "GetOther", (Type[])null, (Type[])null), new HarmonyMethod(typeof(ModOptionsMenu), "LocaleGetOtherPrefix", (Type[])null), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
		MethodInfo methodInfo = AccessTools.Method(typeof(Locale), "GetString", new Type[2]
		{
			typeof(string),
			typeof(int)
		}, (Type[])null);
		if (methodInfo != null)
		{
			harmony.Patch((MethodBase)methodInfo, new HarmonyMethod(typeof(ModOptionsMenu), "LocaleGetStringPrefix", (Type[])null), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
		}
		MethodInfo methodInfo2 = AccessTools.Method(typeof(SettingsMenu), "SelectTab", new Type[1] { typeof(SettingCategory) }, (Type[])null);
		if (methodInfo2 != null)
		{
			harmony.Patch((MethodBase)methodInfo2, (HarmonyMethod)null, new HarmonyMethod(typeof(ModOptionsMenu), "SettingsMenuSelectTabCategoryPostfix", (Type[])null), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
		}
		MethodInfo methodInfo3 = AccessTools.Method(typeof(SettingsMenu), "Start", (Type[])null, (Type[])null);
		if (methodInfo3 != null)
		{
			harmony.Patch((MethodBase)methodInfo3, (HarmonyMethod)null, new HarmonyMethod(typeof(ModOptionsMenu), "SettingsMenuStartPostfix", (Type[])null), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
		}
		harmony.Patch((MethodBase)AccessTools.Method(typeof(SettingsMenu), "ResetToDefault", (Type[])null, (Type[])null), new HarmonyMethod(typeof(ModOptionsMenu), "SettingsMenuResetToDefaultPrefix", (Type[])null), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
	}

	private static float GetSpawnedSettingsHeight(List<GameObject> spawnedSettings)
	{
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		if (spawnedSettings == null || spawnedSettings.Count == 0)
		{
			return 0f;
		}
		float num = 0f;
		for (int i = 0; i < spawnedSettings.Count; i++)
		{
			GameObject val = spawnedSettings[i];
			if (!((Object)(object)val == (Object)null) && !RegisteredIds.Contains(((Object)val).name))
			{
				RectTransform component = val.GetComponent<RectTransform>();
				if ((Object)(object)component != (Object)null)
				{
					num += component.sizeDelta.y;
				}
			}
		}
		return num;
	}

	private static SettingCategory ResolveCustomCategory(string customCategory)
	{
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		string key = NormalizeCategoryKey(customCategory);
		if (!CustomCategoriesByKey.TryGetValue(key, out var value))
		{
			value = new ModOptionCategoryEntry(customCategory.Trim(), 100 + CustomCategories.Count);
			CustomCategories.Add(value);
			CustomCategoriesByKey.Add(key, value);
		}
		return (SettingCategory)value.CategoryIndex;
	}

	private static string NormalizeCategoryKey(string category)
	{
		return (category ?? string.Empty).Trim().ToLowerInvariant();
	}

	public static void DefaultSettingsPostfix(List<Setting> __result)
	{
		if (__result == null)
		{
			return;
		}
		for (int i = 0; i < Definitions.Count; i++)
		{
			ModOptionDefinition definition = Definitions[i];
			if (definition != null && !__result.Any((Setting setting) => setting != null && setting.name == definition.Id))
			{
				__result.Add(definition.CreateSetting());
			}
		}
	}

	public static void LoadSettingsPostfix()
	{
		SyncRegisteredDefinitionsIntoLoadedSettings(restoreSavedValues: false, applyAddedSettings: false);
	}

	public static bool LocaleGetOtherPrefix(string str, ref string __result)
	{
		if (!string.IsNullOrEmpty(str) && TryGetLiteralLocale(str, out var value))
		{
			__result = value;
			return false;
		}
		return true;
	}

	public static bool LocaleGetStringPrefix(string str, int type, ref string __result)
	{
		if (type == 3 && !string.IsNullOrEmpty(str) && TryGetLiteralLocale(str, out var value))
		{
			__result = value;
			return false;
		}
		return true;
	}

	public static void SettingsMenuStartPostfix(SettingsMenu __instance)
	{
		ModManagementSettings.RegisterSettings();
		EnsureRegisteredDefinitionsInLiveSettings(restoreSavedValues: false, applyAddedSettings: false);
		SettingsMenuCategoryExtender.EnsureAttached(__instance);
		ModManagementSettings.RefreshMenuAffordances(__instance, (SettingCategory)0);
	}

	public static void SettingsMenuSelectTabCategoryPostfix(SettingsMenu __instance, SettingCategory category)
	{
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		ModManagementSettings.RegisterSettings();
		EnsureRegisteredDefinitionsInLiveSettings(restoreSavedValues: false, applyAddedSettings: false);
		SettingsMenuCategoryExtender.EnsureAttached(__instance);
		(((Object)(object)__instance != (Object)null) ? ((Component)__instance).GetComponent<SettingsMenuCategoryExtender>() : null)?.OnTabSelected(category);
		RefreshSettingsMenu(__instance, category);
		ModManagementSettings.RefreshMenuAffordances(__instance, category);
	}

	public static bool SettingsMenuResetToDefaultPrefix(SettingsMenu __instance)
	{
		RebuildLiveSettingsAfterReset();
		if ((Object)(object)__instance != (Object)null)
		{
			__instance.SelectTab(0);
		}
		return false;
	}
}
