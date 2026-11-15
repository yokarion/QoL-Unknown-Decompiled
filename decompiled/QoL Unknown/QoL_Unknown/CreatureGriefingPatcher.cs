using System;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace QoL_Unknown;

internal class CreatureGriefingPatcher
{
	internal const string PrefKey = "qol.game.preventcreaturegriefing";

	private static readonly string[] ProtectedCreatures = new string[3] { "snowstrider", "thornbackyoung", "cryodrone" };

	private static readonly FieldInfo biteCooldownField = AccessTools.Field(typeof(SpiderHandler), "biteCooldown");

	public static void Initialize(ManualLogSource logger)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		new Harmony("org.bepinex.plugins.qol.creaturegriefing").PatchAll(typeof(CreatureGriefingPatcher));
	}

	internal static int GetMode()
	{
		return PlayerPrefs.GetInt("qol.game.preventcreaturegriefing", 0);
	}

	private static bool IsProtectedCreature(string name)
	{
		string a = name.Replace("(Clone)", "").Trim();
		for (int i = 0; i < ProtectedCreatures.Length; i++)
		{
			if (string.Equals(a, ProtectedCreatures[i], StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}
		return false;
	}

	[HarmonyPatch(typeof(SpiderHandler), "CheckForBlockDamage")]
	[HarmonyPrefix]
	private static bool CheckForBlockDamage_Prefix(SpiderHandler __instance, Collision2D collision)
	{
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		int mode = GetMode();
		if (mode == 0)
		{
			return true;
		}
		if (!__instance.burrowWalls)
		{
			return true;
		}
		if (!IsProtectedCreature(((Object)__instance).name))
		{
			return true;
		}
		bool flag = mode == 1 || mode == 3;
		bool flag2 = mode == 2 || mode == 3;
		if (biteCooldownField != null)
		{
			biteCooldownField.SetValue(__instance, __instance.biteCoolToSet * 0.3f);
		}
		if (flag && flag2)
		{
			return false;
		}
		if (!flag2)
		{
			WorldGeneration world = WorldGeneration.world;
			WorldGeneration world2 = WorldGeneration.world;
			ContactPoint2D contact = collision.GetContact(0);
			Vector2 point = ((ContactPoint2D)(ref contact)).point;
			contact = collision.GetContact(0);
			world.DamageBlock(world2.WorldToBlockPos(point - ((ContactPoint2D)(ref contact)).normal * 0.5f), __instance.burrowWallDamage, true, false, true);
			BuildingEntity val = default(BuildingEntity);
			if (collision.gameObject.TryGetComponent<BuildingEntity>(ref val) && !val.animal)
			{
				BuildingEntity obj = val;
				obj.health -= __instance.burrowWallDamage * 0.5f;
			}
		}
		Item val2 = default(Item);
		if (!flag && collision.gameObject.TryGetComponent<Item>(ref val2))
		{
			Object.Destroy((Object)(object)((Component)val2).gameObject);
		}
		return false;
	}
}
