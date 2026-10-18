using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace QoL_Unknown;

internal class PreGenPatcher
{
	private const int MenuFrameRateCap = 120;

	private const int MenuVSyncFrameRateCap = 60;

	private static ManualLogSource Logger;

	public static void Initialize(ManualLogSource logger)
	{
		Logger = logger;
		if (!RuntimePatchRegistry.PatchAllOnce("org.bepinex.plugins.qol.pregen_ui", typeof(PreGenPatcher), logger))
		{
		}
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
