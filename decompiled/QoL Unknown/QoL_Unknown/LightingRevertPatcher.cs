using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace QoL_Unknown;

internal static class LightingRevertPatcher
{
	private const string EnabledPrefKey = "UseV6LightingBehavior";

	private static ManualLogSource Logger;

	public static void Initialize(ManualLogSource logger)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Expected O, but got Unknown
		Logger = logger;
		Harmony val = new Harmony("org.bepinex.plugins.qol.lightingrevert");
		val.PatchAll(typeof(LightingRevertPatcher));
		ManualLogSource logger2 = Logger;
		if (logger2 != null)
		{
			logger2.LogInfo((object)"LightingRevertPatcher initialized");
		}
	}

	public static bool IsEnabled()
	{
		return PlayerPrefs.GetInt("UseV6LightingBehavior", 0) == 1;
	}

	public static void SetEnabled(bool enabled)
	{
		PlayerPrefs.SetInt("UseV6LightingBehavior", enabled ? 1 : 0);
		PlayerPrefs.Save();
		ApplyToCurrentWorld();
	}

	public static void ApplyToCurrentWorld()
	{
		WorldGeneration world = WorldGeneration.world;
		if (!((Object)(object)world == (Object)null) && !((Object)(object)world.ambientLight == (Object)null) && IsEnabled())
		{
			world.ambientLight.intensity = 0f;
		}
	}

	[HarmonyPatch(typeof(WorldGeneration), "UpdateAmbientLight")]
	[HarmonyPrefix]
	private static bool UpdateAmbientLight_Prefix(WorldGeneration __instance)
	{
		if (!IsEnabled() || (Object)(object)__instance == (Object)null || (Object)(object)__instance.ambientLight == (Object)null)
		{
			return true;
		}
		float intensity = 0f;
		if (((Object)(object)ConsoleScript.instance != (Object)null && ConsoleScript.instance.fullBright) || ((Object)(object)PlayerCamera.main != (Object)null && (Object)(object)PlayerCamera.main.body != (Object)null && PlayerCamera.main.body.bothEyesGone))
		{
			intensity = 0.7f;
		}
		__instance.ambientLight.intensity = intensity;
		return false;
	}

	[HarmonyPatch(typeof(WorldGeneration), "FinishWorldGeneration")]
	[HarmonyPostfix]
	private static void FinishWorldGeneration_Postfix()
	{
		ApplyToCurrentWorld();
	}
}
