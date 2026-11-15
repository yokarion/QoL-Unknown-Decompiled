using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace QoL_Unknown;

[HarmonyPatch(/*Could not decode attribute arguments.*/)]
internal static class ElderThornbackSpawnPatcher
{
	[HarmonyTranspiler]
	private static IEnumerable<CodeInstruction> WorldPlaceEntities_Transpiler(IEnumerable<CodeInstruction> instructions)
	{
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Expected O, but got Unknown
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
					list[num] = new CodeInstruction(OpCodes.Call, (object)AccessTools.Method(typeof(ElderThornbackSpawnPatcher), "GetElderThornbackCount", (Type[])null, (Type[])null));
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
