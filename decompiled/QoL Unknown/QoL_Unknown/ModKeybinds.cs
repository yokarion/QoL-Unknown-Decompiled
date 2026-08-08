using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using UnityEngine;

namespace QoL_Unknown;

internal static class ModKeybinds
{
	public const string QuickStashAction = "quickstash";

	public const string InventoryInteractionAction = "inventoryinteraction";

	private const string QuickStashSettingId = "qol.input.quickstash";

	private const string InventoryInteractionSettingId = "qol.input.inventoryinteraction";

	private static readonly List<ModKeybindDefinition> definitions = new List<ModKeybindDefinition>
	{
		new ModKeybindDefinition("quickstash", "qol.keybind.quick_stash.name", "qol.keybind.quick_stash.description", (KeyCode)325),
		new ModKeybindDefinition("inventoryinteraction", "qol.keybind.inventory_interaction.name", "qol.keybind.inventory_interaction.description", (KeyCode)308)
	};

	private static ManualLogSource Logger;

	public static IEnumerable<ModKeybindDefinition> Definitions => definitions;

	public static void Initialize(ManualLogSource logger)
	{
		Logger = logger;
		EnsureDefaultsPresent();
	}

	public static KeyCode GetKey(string action)
	{
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		ModKeybindDefinition modKeybindDefinition = definitions.FirstOrDefault((ModKeybindDefinition def) => string.Equals(def.Action, action, StringComparison.OrdinalIgnoreCase));
		if (modKeybindDefinition == null)
		{
			return (KeyCode)0;
		}
		return EnsureSettingAvailable(modKeybindDefinition)?.value ?? modKeybindDefinition.DefaultKey;
	}

	public static string GetKeyName(string action)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Invalid comparison between Unknown and I4
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		KeyCode key = GetKey(action);
		if ((int)key == 0)
		{
			return Locale.GetOther("keybindnotset");
		}
		return InputDisplayHelper.GetFriendlyName(key);
	}

	public static bool GetActionDown(string action)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Expected I4, but got Unknown
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		KeyCode key = GetKey(action);
		KeyCode val = key;
		KeyCode val2 = val;
		if ((int)val2 != 0)
		{
			return (val2 - 323) switch
			{
				0 => Input.GetMouseButtonDown(0), 
				1 => Input.GetMouseButtonDown(1), 
				2 => Input.GetMouseButtonDown(2), 
				3 => Input.GetMouseButtonDown(3), 
				4 => Input.GetMouseButtonDown(4), 
				5 => Input.GetMouseButtonDown(5), 
				6 => Input.GetMouseButtonDown(6), 
				_ => Input.GetKeyDown(key), 
			};
		}
		return false;
	}

	public static bool GetAction(string action)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Expected I4, but got Unknown
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		KeyCode key = GetKey(action);
		KeyCode val = key;
		KeyCode val2 = val;
		if ((int)val2 != 0)
		{
			return (val2 - 323) switch
			{
				0 => Input.GetMouseButton(0), 
				1 => Input.GetMouseButton(1), 
				2 => Input.GetMouseButton(2), 
				3 => Input.GetMouseButton(3), 
				4 => Input.GetMouseButton(4), 
				5 => Input.GetMouseButton(5), 
				6 => Input.GetMouseButton(6), 
				_ => Input.GetKey(key), 
			};
		}
		return false;
	}

	public static void EnsureDefaultsPresent()
	{
		foreach (ModKeybindDefinition definition in definitions)
		{
			EnsureSettingRegistered(definition);
		}
	}

	public static void ResetModDefaults()
	{
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		foreach (ModKeybindDefinition definition in definitions)
		{
			SettingKeybind val = Settings.Get<SettingKeybind>(GetSettingId(definition.Action));
			if (val == null)
			{
				ModOptionsMenu.EnsureRegisteredDefinitionsInLiveSettings(restoreSavedValues: false, applyAddedSettings: false);
				EnsureSettingRegistered(definition);
				val = FindSetting(GetSettingId(definition.Action));
			}
			if (val != null)
			{
				((Setting)val).SetValue((object)definition.DefaultKey);
				((Setting)val).Apply();
			}
		}
		Settings.SaveSettings();
	}

	private static void EnsureSettingRegistered(ModKeybindDefinition definition)
	{
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		string settingId = GetSettingId(definition.Action);
		if (FindSetting(settingId) != null)
		{
			return;
		}
		ModOptionsMenu.Register(ModOptionDefinition.Keybind(settingId, definition.DisplayName, definition.Description, (SettingCategory)3, definition.DefaultKey, delegate(KeyCode key)
		{
			//IL_001c: Unknown result type (might be due to invalid IL or missing references)
			ManualLogSource logger = Logger;
			if (logger != null)
			{
				logger.LogDebug((object)$"Applied mod keybind '{definition.Action}' = {key}");
			}
			Settings.SaveSettings();
		}));
	}

	private static SettingKeybind EnsureSettingAvailable(ModKeybindDefinition definition)
	{
		if (definition == null)
		{
			return null;
		}
		string settingId = GetSettingId(definition.Action);
		SettingKeybind val = FindSetting(settingId);
		if (val != null || Settings.settings == null)
		{
			return val;
		}
		ModOptionsMenu.EnsureRegisteredDefinitionsInLiveSettings(restoreSavedValues: true, applyAddedSettings: true);
		val = FindSetting(settingId);
		if (val != null)
		{
			return val;
		}
		EnsureSettingRegistered(definition);
		val = FindSetting(settingId);
		if (val != null)
		{
			((Setting)val).Apply();
		}
		return val;
	}

	private static string GetSettingId(string action)
	{
		if (action == "quickstash")
		{
			return "qol.input.quickstash";
		}
		if (action == "inventoryinteraction")
		{
			return "qol.input.inventoryinteraction";
		}
		return "qol.input." + action;
	}

	private static SettingKeybind FindSetting(string settingId)
	{
		if (Settings.settings == null)
		{
			return null;
		}
		for (int i = 0; i < Settings.settings.Count; i++)
		{
			Setting val = Settings.settings[i];
			if (val != null && val.name == settingId)
			{
				return (SettingKeybind)(object)((val is SettingKeybind) ? val : null);
			}
		}
		return null;
	}
}
