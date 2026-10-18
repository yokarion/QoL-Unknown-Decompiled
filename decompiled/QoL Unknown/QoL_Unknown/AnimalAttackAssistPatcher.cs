using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace QoL_Unknown;

internal static class AnimalAttackAssistPatcher
{
	private const string ModuleId = "qol_game_animal_attack_assist";

	private const float DetectionWidth = 2.15f;

	private static ManualLogSource logger;

	private static bool initialized;

	public static void Initialize(ManualLogSource pluginLogger)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Expected O, but got Unknown
		logger = pluginLogger;
		if (!initialized)
		{
			initialized = true;
			Harmony val = new Harmony("org.bepinex.plugins.qol.animalattackassist");
			val.PatchAll(typeof(AnimalAttackAssistPatcher));
		}
	}

	[HarmonyPatch(typeof(Body), "Attack")]
	[HarmonyPrefix]
	private static void Attack_Prefix(Body __instance, AttackInfo atk)
	{
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		if (QoLModuleManager.IsEnabled("qol_game_animal_attack_assist") && ShouldAssistAttack(__instance, atk) && TryFindAnimalTarget(__instance, atk, out var targetPosition))
		{
			__instance.targetLookPos = Vector2.op_Implicit(targetPosition);
		}
	}

	private static bool ShouldAssistAttack(Body body, AttackInfo atk)
	{
		if ((Object)(object)body == (Object)null || atk == null || !body.conscious || body.attackCooldown > 0f || atk.distance <= 0f || atk.damage <= 0f)
		{
			return false;
		}
		return atk.unarmed || atk.physicalSwing;
	}

	private static bool TryFindAnimalTarget(Body body, AttackInfo atk, out Vector2 targetPosition)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_0113: Unknown result type (might be due to invalid IL or missing references)
		//IL_0118: Unknown result type (might be due to invalid IL or missing references)
		//IL_011c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0121: Unknown result type (might be due to invalid IL or missing references)
		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
		//IL_0126: Unknown result type (might be due to invalid IL or missing references)
		//IL_0128: Unknown result type (might be due to invalid IL or missing references)
		//IL_0129: Unknown result type (might be due to invalid IL or missing references)
		//IL_0149: Unknown result type (might be due to invalid IL or missing references)
		//IL_014b: Unknown result type (might be due to invalid IL or missing references)
		targetPosition = default(Vector2);
		Vector2 val = Vector2.op_Implicit(((Component)body.limbs[1]).transform.position);
		Vector2 val2 = Vector2.op_Implicit(body.targetLookPos) - val;
		Vector2 val3 = ((Vector2)(ref val2)).normalized;
		if (((Vector2)(ref val3)).sqrMagnitude < 0.0001f)
		{
			val3 = (body.isRight ? Vector2.right : Vector2.left);
		}
		Vector2 val4 = default(Vector2);
		((Vector2)(ref val4))._002Ector(2.15f, 1.35f);
		float num = Mathf.Atan2(val3.y, val3.x) * 57.29578f;
		RaycastHit2D[] array = Physics2D.BoxCastAll(val, val4, num, val3, atk.distance);
		float num2 = float.MaxValue;
		RaycastHit2D[] array2 = array;
		BuildingEntity val6 = default(BuildingEntity);
		for (int i = 0; i < array2.Length; i++)
		{
			RaycastHit2D val5 = array2[i];
			Transform transform = ((RaycastHit2D)(ref val5)).transform;
			if (((Component)transform).TryGetComponent<BuildingEntity>(ref val6) && val6.animal && !val6.cantHit)
			{
				Vector2 val7;
				if (!Object.op_Implicit((Object)(object)((RaycastHit2D)(ref val5)).collider))
				{
					val7 = Vector2.op_Implicit(transform.position);
				}
				else
				{
					Bounds bounds = ((RaycastHit2D)(ref val5)).collider.bounds;
					val7 = Vector2.op_Implicit(((Bounds)(ref bounds)).center);
				}
				Vector2 val8 = val7;
				float num3 = Vector2.Distance(val, val8);
				if (!(num3 >= num2))
				{
					num2 = num3;
					targetPosition = val8;
				}
			}
		}
		return num2 < float.MaxValue;
	}
}
