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
}
