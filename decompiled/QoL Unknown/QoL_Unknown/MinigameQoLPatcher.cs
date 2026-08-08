using System.Text;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace QoL_Unknown;

internal class MinigameQoLPatcher
{
	private const string MinigameQoLPrefKey = "Setting_Minigame_QoL";

	private const float LockpickDegreeMultiplier = 2f;

	private const float LockpickSpeedMultiplier = 1.2f;

	private const int KeypadMinDigits = 5;

	private const int KeypadMaxDigitsInclusive = 9;

	public static void Initialize(ManualLogSource logger)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Expected O, but got Unknown
		Harmony val = new Harmony("org.bepinex.plugins.qol.minigame");
		val.PatchAll(typeof(MinigameQoLPatcher));
		if (logger != null)
		{
			logger.LogInfo((object)"MinigameQoLPatcher initialized");
		}
	}

	private static bool MinigameQoLEnabled()
	{
		return Settings.Get<SettingBool>("qol.game.minigameqol")?.value ?? (PlayerPrefs.GetInt("Setting_Minigame_QoL", 0) == 1);
	}

	[HarmonyPatch(/*Could not decode attribute arguments.*/)]
	[HarmonyPrefix]
	private static void LockpingMinigame_Ctor_Prefix(ref float anglePrecision)
	{
		if (MinigameQoLEnabled())
		{
			anglePrecision *= 2f;
		}
	}

	[HarmonyPatch(typeof(LockpingMinigame), "Update")]
	[HarmonyPriority(800)]
	[HarmonyBefore(new string[] { "KrokoshaCasualtiesMP" })]
	[HarmonyPrefix]
	private static void LockpingMinigame_Update_Prefix(LockpingMinigame __instance)
	{
		if (MinigameQoLEnabled() && __instance.clickingInside && !((Object)(object)__instance.toDestroy == (Object)null))
		{
			float num = __instance.MaxTurnProgress();
			if (!(__instance.lockProgress >= num))
			{
				float num2 = __instance.pickLevel + 1;
				float num3 = Time.deltaTime * (0.66f + num2 * 0.065f);
				float num4 = Mathf.Min(__instance.lockProgress + num3, num) - __instance.lockProgress;
				float num5 = num4 * 0.20000005f;
				__instance.lockProgress = Mathf.Clamp(__instance.lockProgress + num5, 0f, num);
			}
		}
	}

	[HarmonyPatch(typeof(KeypadMinigame), "GenerateCode")]
	[HarmonyPrefix]
	private static bool KeypadMinigame_GenerateCode_Prefix(ref string __result)
	{
		if (!MinigameQoLEnabled())
		{
			return true;
		}
		int num = Random.Range(5, 10);
		StringBuilder stringBuilder = new StringBuilder(num);
		for (int i = 0; i < num; i++)
		{
			stringBuilder.Append(Random.Range(0, 10));
		}
		__result = stringBuilder.ToString();
		return false;
	}
}
