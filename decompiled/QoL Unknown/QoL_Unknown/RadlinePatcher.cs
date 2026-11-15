using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace QoL_Unknown;

internal static class RadlinePatcher
{
	internal const string DisableRadlinePrefKey = "DisableRadline";

	private const string VanillaDisableRadlinePrefKey = "radlinedisable";

	private const float DefaultMaxTimePerLayerSeconds = 3600f;

	private static ManualLogSource Logger;

	private static bool initialized;

	public static void Initialize(ManualLogSource logger)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		Logger = logger;
		if (!initialized)
		{
			initialized = true;
			new Harmony("org.bepinex.plugins.qol.radline").PatchAll(typeof(RadlinePatcher));
		}
	}

	internal static bool IsDisabled()
	{
		if (PlayerPrefs.GetInt("DisableRadline", 0) != 1)
		{
			return PlayerPrefs.GetInt("radlinedisable", 0) == 1;
		}
		return true;
	}

	internal static void SetDisabled(bool disabled)
	{
		int num = (disabled ? 1 : 0);
		PlayerPrefs.SetInt("DisableRadline", num);
		PlayerPrefs.SetInt("radlinedisable", num);
		PlayerPrefs.Save();
		ApplyCurrentState();
	}

	internal static void ApplyCurrentState()
	{
		bool num = IsDisabled();
		if ((Object)(object)WorldGeneration.world != (Object)null && WorldGeneration.world.maxTimePerLayer <= 0f)
		{
			float num2 = WorldGeneration.GetRunSettingFloat("timelimit") * 60f;
			WorldGeneration.world.maxTimePerLayer = ((num2 > 0f) ? num2 : 3600f);
		}
		if (num && (Object)(object)RadiationLine.line != (Object)null)
		{
			RadiationLine.line.Deactivate();
		}
	}

	[HarmonyPatch(typeof(RadiationLine), "Activate")]
	[HarmonyPrefix]
	private static bool RadiationLine_Activate_Prefix(RadiationLine __instance)
	{
		if (!IsDisabled())
		{
			return true;
		}
		if ((Object)(object)__instance != (Object)null && __instance.active)
		{
			__instance.Deactivate();
		}
		return false;
	}
}
