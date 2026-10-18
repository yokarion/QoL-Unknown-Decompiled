using System;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace QoL_Unknown;

internal class WaterPusherPatcher
{
	private static ManualLogSource Logger;

	private static readonly FieldInfo DesiredSpeedField = AccessTools.Field(typeof(WaterPusher), "desiredSpeed");

	private const float FallbackDesiredSpeed = 15f;

	private static bool warnedAboutDesiredSpeed;

	public static void Initialize(ManualLogSource logger)
	{
		Logger = logger;
		RuntimePatchRegistry.PatchAllOnce("org.bepinex.plugins.qol.waterpusher", typeof(WaterPusherPatcher), logger);
	}

	[HarmonyPatch(typeof(WaterPusher), "OnTriggerStay2D")]
	[HarmonyPrefix]
	private static bool OnTriggerStay2D_Prefix(WaterPusher __instance, Collider2D collision)
	{
		//IL_0199: Unknown result type (might be due to invalid IL or missing references)
		//IL_019f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01af: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)collision == (Object)null || (Object)(object)collision.attachedRigidbody == (Object)null || (Object)(object)__instance == (Object)null)
		{
			return false;
		}
		float desiredSpeed = GetDesiredSpeed();
		Body val = (((Object)(object)PlayerCamera.main != (Object)null) ? PlayerCamera.main.body : null);
		if ((Object)(object)val != (Object)null && (Object)(object)((Component)collision).gameObject == (Object)(object)((Component)val).gameObject)
		{
			float num = ((Component)val).transform.position.y - val.col.size.y * 0.5f;
			float num2 = Mathf.Clamp01((((Component)__instance).transform.position.y - num) * 0.16f + 0.2f);
			if (num2 < 0.4f)
			{
				return false;
			}
			if (!val.hasScubaGear)
			{
				float num3 = 0.9f;
				float num4 = Time.fixedDeltaTime * 0.5f * ((((Component)__instance).transform.position.y > ((Component)val).transform.position.y) ? 1f : 0.5f);
				val.liquidSlipTime = Mathf.MoveTowards(val.liquidSlipTime, num3, num4);
				val.liquidRagdollBar -= Time.fixedDeltaTime * num2 * 0.25f;
				if (val.liquidRagdollBar <= 0f)
				{
					val.Ragdoll();
					val.shock = 30f;
				}
			}
		}
		collision.attachedRigidbody.velocity = Vector2.Lerp(collision.attachedRigidbody.velocity, __instance.direction * desiredSpeed, 0.05f);
		return false;
	}

	private static float GetDesiredSpeed()
	{
		if (DesiredSpeedField == null)
		{
			if (!warnedAboutDesiredSpeed)
			{
				warnedAboutDesiredSpeed = true;
				ManualLogSource logger = Logger;
				if (logger != null)
				{
					logger.LogWarning((object)"WaterPusher.desiredSpeed was not found; using the vanilla fallback speed.");
				}
			}
			return 15f;
		}
		try
		{
			return (DesiredSpeedField.GetValue(null) is float num && !float.IsNaN(num) && !float.IsInfinity(num)) ? num : 15f;
		}
		catch (Exception ex)
		{
			if (!warnedAboutDesiredSpeed)
			{
				warnedAboutDesiredSpeed = true;
				ManualLogSource logger2 = Logger;
				if (logger2 != null)
				{
					logger2.LogWarning((object)("Could not read WaterPusher.desiredSpeed; using the fallback speed. " + ex.Message));
				}
			}
			return 15f;
		}
	}
}
