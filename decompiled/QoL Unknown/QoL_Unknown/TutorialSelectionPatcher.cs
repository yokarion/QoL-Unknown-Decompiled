using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace QoL_Unknown;

internal class TutorialSelectionPatcher
{
	public static void Initialize(ManualLogSource logger)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Expected O, but got Unknown
		Harmony val = new Harmony("org.bepinex.plugins.qol.tutorialselection");
		val.PatchAll(typeof(TutorialSelectionPatcher));
	}

	[HarmonyPatch(typeof(TutorialHandler), "Update")]
	[HarmonyPrefix]
	private static bool Update_Prefix(TutorialHandler __instance)
	{
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)__instance.courseSelectScreen == (Object)null || !__instance.courseSelectScreen.activeSelf)
		{
			return true;
		}
		KeyCode bind = KeyBinds.GetBind("left");
		KeyCode bind2 = KeyBinds.GetBind("right");
		if ((int)bind != 0 && Input.GetKeyDown(bind))
		{
			__instance.ChangeSelectedCourse(-1);
		}
		if ((int)bind2 != 0 && Input.GetKeyDown(bind2))
		{
			__instance.ChangeSelectedCourse(1);
		}
		__instance.HandleCourseSelect();
		return false;
	}
}
