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
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		Logger = logger;
		new Harmony("org.bepinex.plugins.qol.exitconfirm").PatchAll(typeof(ExitConfirmationPatcher));
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
		if (!IsSkipExitAnimationEnabled() && !Input.GetKey((KeyCode)304))
		{
			return Input.GetKey((KeyCode)303);
		}
		return true;
	}

	[HarmonyPatch(typeof(PauseHandler), "TryQuit")]
	[HarmonyPrefix]
	private static bool TryQuit_Prefix(PauseHandler __instance)
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Invalid comparison between Unknown and I4
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
