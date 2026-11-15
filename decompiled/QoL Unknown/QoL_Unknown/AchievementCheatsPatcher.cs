using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace QoL_Unknown;

internal static class AchievementCheatsPatcher
{
	internal const string PrefKey = "qol.game.achievementswithcheats";

	private static ManualLogSource Logger;

	private static bool initialized;

	public static void Initialize(ManualLogSource logger)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		Logger = logger;
		if (!initialized)
		{
			initialized = true;
			new Harmony("org.bepinex.plugins.qol.achievementswithcheats").PatchAll(typeof(AchievementCheatsPatcher));
		}
	}

	internal static bool IsEnabled()
	{
		return PlayerPrefs.GetInt("qol.game.achievementswithcheats", 0) == 1;
	}

	internal static void SetEnabled(bool enabled)
	{
		PlayerPrefs.SetInt("qol.game.achievementswithcheats", enabled ? 1 : 0);
		PlayerPrefs.Save();
		if (enabled)
		{
			SteamManager.disableAchievements = false;
		}
	}

	[HarmonyPatch(typeof(ConsoleScript), "CheckForCheats")]
	[HarmonyPrefix]
	private static bool CheckForCheats_Prefix()
	{
		if (!IsEnabled())
		{
			return true;
		}
		SteamManager.disableAchievements = false;
		return false;
	}
}
