using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace QoL_Unknown;

internal class EntitySpawnPatcher
{
	public static void Initialize(ManualLogSource logger)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Expected O, but got Unknown
		EntitySpawnManager.LoadMultipliers();
		Harmony val = new Harmony("org.bepinex.plugins.qol.entityspawn");
		val.PatchAll(typeof(EntitySpawnPatcher));
	}

	[HarmonyPatch(typeof(WorldGeneration), "DistributeEntities")]
	[HarmonyPrefix]
	private static void DistributeEntities_Prefix(GameObject basObj, ref float minPerChunk, ref float maxPerChunk)
	{
		if (!((Object)(object)basObj == (Object)null))
		{
			float multiplier = EntitySpawnManager.GetMultiplier(((Object)basObj).name);
			if (multiplier != 1f)
			{
				minPerChunk *= multiplier;
				maxPerChunk *= multiplier;
			}
		}
	}

	[HarmonyPatch(typeof(WorldGeneration), "WorldPlaceEntities")]
	[HarmonyTranspiler]
	private static IEnumerable<CodeInstruction> WorldPlaceEntities_Transpiler(IEnumerable<CodeInstruction> instructions)
	{
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Expected O, but got Unknown
		List<CodeInstruction> list = new List<CodeInstruction>(instructions);
		for (int i = 0; i < list.Count; i++)
		{
			if (!(list[i].opcode == OpCodes.Ldstr) || !(list[i].operand as string == "thornbackelder"))
			{
				continue;
			}
			int num = i - 1;
			while (num >= 0 && num > i - 30)
			{
				if (list[num].opcode == OpCodes.Ldc_I4_3)
				{
					list[num] = new CodeInstruction(OpCodes.Call, (object)typeof(EntitySpawnPatcher).GetMethod("GetElderThornbackCount", BindingFlags.Static | BindingFlags.NonPublic));
					break;
				}
				num--;
			}
			break;
		}
		return list;
	}

	private static int GetElderThornbackCount()
	{
		float multiplier = EntitySpawnManager.GetMultiplier("thornbackelder");
		return Mathf.Max(0, Mathf.RoundToInt(3f * multiplier));
	}
}
