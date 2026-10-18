using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace QoL_Unknown;

internal class ExitConfirmationPatcher
{
	private const string SkipExitAnimationPrefKey = "Setting_SkipExitAnimation";

	private static ManualLogSource Logger;

	public static void Initialize(ManualLogSource logger)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Expected O, but got Unknown
		Logger = logger;
		Harmony val = new Harmony("org.bepinex.plugins.qol.exitconfirm");
		val.PatchAll(typeof(ExitConfirmationPatcher));
	}

	internal static bool IsSkipExitAnimationEnabled()
	{
		return PlayerPrefs.GetInt("Setting_SkipExitAnimation", 0) == 1;
	}

	internal static void SetSkipExitAnimationEnabled(bool enabled)
	{
		PlayerPrefs.SetInt("Setting_SkipExitAnimation", enabled ? 1 : 0);
		PlayerPrefs.Save();
	}

	private static bool ShouldSkipExitConfirmation()
	{
		return IsSkipExitAnimationEnabled() || Input.GetKey((KeyCode)304) || Input.GetKey((KeyCode)303);
	}

	[HarmonyPatch(typeof(PauseHandler), "TryQuit")]
	[HarmonyPrefix]
	private static bool TryQuit_Prefix(PauseHandler __instance)
	{
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Invalid comparison between Unknown and I4
		if (!ShouldSkipExitConfirmation())
		{
			return true;
		}
		if ((Object)(object)WorldGeneration.world != (Object)null && (int)WorldGeneration.world.biomeOverride == 1)
		{
			return true;
		}
		if (__instance != null)
		{
			__instance.ResetQuitPressed();
		}
		PlayerCamera main = PlayerCamera.main;
		if (main != null)
		{
			main.ToMainMenu();
		}
		return false;
	}
}
