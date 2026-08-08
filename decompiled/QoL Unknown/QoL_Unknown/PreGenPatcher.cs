using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace QoL_Unknown;

internal class PreGenPatcher
{
	private const int MenuFrameRateCap = 120;

	private const int MenuVSyncFrameRateCap = 60;

	private static ManualLogSource Logger;

	private static bool _hasRunOnce;

	public static void Initialize(ManualLogSource logger)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Expected O, but got Unknown
		Logger = logger;
		Harmony val = new Harmony("org.bepinex.plugins.qol.pregen_ui");
		val.PatchAll(typeof(PreGenPatcher));
		Logger.LogInfo((object)"PreGenPatcher initialized");
	}

	[HarmonyPatch(typeof(PreRunScript), "Start")]
	[HarmonyPostfix]
	private static void Start_Postfix(PreRunScript __instance)
	{
		QualitySettings.vSyncCount = 0;
		ApplyMenuFrameRateLimit();
	}

	[HarmonyPatch(typeof(WorldGeneration), "Start")]
	[HarmonyPrefix]
	private static void WorldGen_Start_Prefix()
	{
		SettingInt val = Settings.Get<SettingInt>("framerate");
		if (val != null)
		{
			Application.targetFrameRate = ((val.value == 0) ? (-1) : Mathf.Max(10, val.value));
		}
	}

	internal static void ApplyMenuFrameRateLimit()
	{
		SettingBool val = Settings.Get<SettingBool>("vsync");
		if (val != null && val.value)
		{
			Application.targetFrameRate = 60;
			return;
		}
		SettingInt val2 = Settings.Get<SettingInt>("framerate");
		if (val2 == null)
		{
			Application.targetFrameRate = 120;
		}
		else if (val2.value == 0)
		{
			Application.targetFrameRate = 120;
		}
		else
		{
			Application.targetFrameRate = Mathf.Clamp(val2.value, 10, 120);
		}
	}
}
