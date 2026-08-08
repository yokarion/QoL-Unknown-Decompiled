using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace QoL_Unknown;

internal class WaterPusherPatcher
{
	private static ManualLogSource Logger;

	public static void Initialize(ManualLogSource logger)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Expected O, but got Unknown
		Logger = logger;
		Harmony val = new Harmony("org.bepinex.plugins.qol.waterpusher");
		val.PatchAll(typeof(WaterPusherPatcher));
	}

	[HarmonyPatch(typeof(WaterPusher), "OnTriggerStay2D")]
	[HarmonyPrefix]
	private static bool OnTriggerStay2D_Prefix(WaterPusher __instance, Collider2D collision)
	{
		//IL_01b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_011e: Unknown result type (might be due to invalid IL or missing references)
		//IL_012e: Unknown result type (might be due to invalid IL or missing references)
		if (!Object.op_Implicit((Object)(object)collision.attachedRigidbody))
		{
			return false;
		}
		float num = 10f;
		try
		{
			FieldInfo fieldInfo = AccessTools.Field(typeof(WaterPusher), "desiredSpeed");
			if (fieldInfo != null)
			{
				num = (float)fieldInfo.GetValue(null);
			}
		}
		catch
		{
		}
		Body body = PlayerCamera.main.body;
		if ((Object)(object)body != (Object)null && (Object)(object)((Component)collision).gameObject == (Object)(object)((Component)body).gameObject)
		{
			float num2 = ((Component)body).transform.position.y - body.col.size.y * 0.5f;
			float num3 = Mathf.Clamp01((((Component)__instance).transform.position.y - num2) * 0.16f + 0.2f);
			if (num3 < 0.4f)
			{
				return false;
			}
			if (!body.hasScubaGear)
			{
				float num4 = 0.9f;
				float num5 = Time.fixedDeltaTime * 0.5f * ((((Component)__instance).transform.position.y > ((Component)body).transform.position.y) ? 1f : 0.5f);
				body.liquidSlipTime = Mathf.MoveTowards(body.liquidSlipTime, num4, num5);
				body.liquidRagdollBar -= Time.fixedDeltaTime * num3 * 0.25f;
				if (body.liquidRagdollBar <= 0f)
				{
					body.Ragdoll();
					body.shock = 30f;
				}
			}
		}
		collision.attachedRigidbody.velocity = Vector2.Lerp(collision.attachedRigidbody.velocity, __instance.direction * num, 0.05f);
		return false;
	}
}
