using System;
using System.Collections;
using BepInEx.Logging;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace QoL_Unknown;

internal class KeybindsPatcher
{
	private static ManualLogSource Logger;

	public static void Initialize(ManualLogSource logger)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Expected O, but got Unknown
		Logger = logger;
		ModKeybinds.Initialize(logger);
		Harmony val = new Harmony("org.bepinex.plugins.qol.keybinds");
		val.PatchAll(typeof(KeybindsPatcher));
	}

	[HarmonyPatch(typeof(Settings), "LoadSettings")]
	[HarmonyPostfix]
	private static void LoadSettings_Postfix()
	{
		ModKeybinds.EnsureDefaultsPresent();
	}

	[HarmonyPatch(typeof(Input), "GetKey", new Type[] { typeof(KeyCode) })]
	[HarmonyPrefix]
	private static bool GetKey_Prefix(KeyCode key, ref bool __result)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		if (!KeybindCombos.IsCombo(key))
		{
			return true;
		}
		__result = KeybindCombos.GetKey(key);
		return false;
	}

	[HarmonyPatch(typeof(Input), "GetKeyDown", new Type[] { typeof(KeyCode) })]
	[HarmonyPrefix]
	private static bool GetKeyDown_Prefix(KeyCode key, ref bool __result)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		if (!KeybindCombos.IsCombo(key))
		{
			return true;
		}
		__result = KeybindCombos.GetKeyDown(key);
		return false;
	}

	[HarmonyPatch(typeof(Input), "GetKeyUp", new Type[] { typeof(KeyCode) })]
	[HarmonyPrefix]
	private static bool GetKeyUp_Prefix(KeyCode key, ref bool __result)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		if (!KeybindCombos.IsCombo(key))
		{
			return true;
		}
		__result = KeybindCombos.GetKeyUp(key);
		return false;
	}

	[HarmonyPatch(typeof(SettingKeybind), "GetDisplayString")]
	[HarmonyPostfix]
	private static void GetDisplayString_Postfix(SettingKeybind __instance, ref string __result)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		if (KeybindCombos.IsCombo(__instance.value))
		{
			__result = KeybindCombos.Format(__instance.value);
		}
	}

	[HarmonyPatch(typeof(KeyBinds), "GetBindName")]
	[HarmonyPrefix]
	private static bool GetBindName_Prefix(string action, ref string __result)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		KeyCode bind = KeyBinds.GetBind(action);
		if (!KeybindCombos.IsCombo(bind))
		{
			return true;
		}
		__result = KeybindCombos.Format(bind);
		return false;
	}

	[HarmonyPatch(typeof(SettingsMenu), "WaitForKeyPress", new Type[]
	{
		typeof(SettingKeybind),
		typeof(TextMeshProUGUI)
	})]
	[HarmonyPrefix]
	private static bool WaitForKeyPress_Prefix(SettingKeybind keybind, TextMeshProUGUI txt, ref IEnumerator __result)
	{
		__result = KeybindCombos.Capture(keybind, txt);
		return false;
	}
}
