using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace QoL_Unknown;

internal class SkillsPatcher
{
	private const string SkillPointAllocationSettingId = "qol.game.skillallocation";

	private const string SkillPointAllocationPrefKey = "Setting_SkillPointAllocation";

	private static ManualLogSource Logger;

	public static void Initialize(ManualLogSource logger)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Expected O, but got Unknown
		Logger = logger;
		Harmony val = new Harmony("org.bepinex.plugins.qol.skills");
		val.PatchAll(typeof(SkillsPatcher));
		Logger.LogInfo((object)"SkillsPatcher initialized");
	}

	[HarmonyPatch(typeof(Body), "Start")]
	[HarmonyPostfix]
	private static void Body_Start_Postfix(Body __instance)
	{
		if (SaveSystem.loadedRun || !SkillPointAllocationEnabled())
		{
			return;
		}
		if (__instance.skills == null)
		{
			Logger.LogError((object)"Body.skills is null!");
			return;
		}
		bool flag = false;
		if (PlayerPrefs.HasKey("StartSTR"))
		{
			int sTR = PlayerPrefs.GetInt("StartSTR");
			__instance.skills.STR = sTR;
			flag = true;
		}
		if (PlayerPrefs.HasKey("StartRES"))
		{
			int rES = PlayerPrefs.GetInt("StartRES");
			__instance.skills.RES = rES;
			flag = true;
		}
		if (PlayerPrefs.HasKey("StartINT"))
		{
			int iNT = PlayerPrefs.GetInt("StartINT");
			__instance.skills.INT = iNT;
			flag = true;
		}
		if (flag)
		{
			__instance.skills.UpdateExpBoundaries();
			__instance.skills.expSTR = __instance.skills.minSTR;
			__instance.skills.expRES = __instance.skills.minRES;
			__instance.skills.expINT = __instance.skills.minINT;
		}
	}

	private static bool SkillPointAllocationEnabled()
	{
		return Settings.Get<SettingBool>("qol.game.skillallocation")?.value ?? (PlayerPrefs.GetInt("Setting_SkillPointAllocation", 1) == 1);
	}
}
