using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace QoL_Unknown;

internal class ControllerPatcher
{
	private static ManualLogSource Logger;

	public static void Initialize(ManualLogSource logger)
	{
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Expected O, but got Unknown
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Expected O, but got Unknown
		Logger = logger;
		ControllerBindings.Initialize(logger);
		if ((Object)(object)Object.FindObjectOfType<ControllerInputManager>() == (Object)null)
		{
			GameObject val = new GameObject("ControllerManager");
			Object.DontDestroyOnLoad((Object)(object)val);
			val.AddComponent<ControllerInputManager>();
		}
		Harmony val2 = new Harmony("org.bepinex.plugins.qol.controller");
		val2.PatchAll(typeof(ControllerPatcher));
		Logger.LogInfo((object)"ControllerPatcher initialized");
	}

	[HarmonyPatch(typeof(PlayerCamera), "HandleInput")]
	[HarmonyPostfix]
	private static void HandleInput_Postfix(PlayerCamera __instance)
	{
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		ControllerInputManager instance = ControllerInputManager.Instance;
		if ((Object)(object)instance == (Object)null || !instance.IsControllerActive || !Object.op_Implicit((Object)(object)__instance.body))
		{
			return;
		}
		Vector2 leftStick = instance.LeftStick;
		if (((Vector2)(ref leftStick)).sqrMagnitude > 0.05f)
		{
			__instance.body.moveDir = leftStick;
			if (leftStick.y < -0.6f)
			{
				__instance.body.crouching = true;
			}
		}
	}
}
