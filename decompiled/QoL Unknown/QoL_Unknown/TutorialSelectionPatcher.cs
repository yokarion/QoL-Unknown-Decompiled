using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace QoL_Unknown;

internal class TutorialSelectionPatcher
{
	public static void Initialize(ManualLogSource logger)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		new Harmony("org.bepinex.plugins.qol.tutorialselection").PatchAll(typeof(TutorialSelectionPatcher));
	}

	[HarmonyPatch(typeof(TutorialHandler), "Update")]
	[HarmonyPrefix]
	private static bool Update_Prefix(TutorialHandler __instance)
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
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
