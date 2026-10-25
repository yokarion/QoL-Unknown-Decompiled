using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace QoL_Unknown;

internal static class CuCoreLibSpawnCompat
{
	private static ManualLogSource Logger;

	private static MethodInfo getBuildingIdsMethod;

	private static MethodInfo tryGetBuildingDefinitionMethod;

	private static FieldInfo spawnMinPerChunkField;

	private static FieldInfo spawnMaxPerChunkField;

	private static FieldInfo generationStyleField;

	private static MethodInfo getStructureIdsMethod;

	private static MethodInfo tryGetSpawnCountsMethod;

	private static MethodInfo trySetSpawnCountsMethod;

	private static readonly Dictionary<string, int[]> originalStructureCounts = new Dictionary<string, int[]>();

	public static void Initialize(Harmony harmony, ManualLogSource logger)
	{
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f4: Expected O, but got Unknown
		//IL_00f4: Expected O, but got Unknown
		//IL_014a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0157: Expected O, but got Unknown
		Logger = logger;
		if (!Chainloader.PluginInfos.ContainsKey("net.cucorelib"))
		{
			return;
		}
		try
		{
			Type type = AccessTools.TypeByName("CUCoreLib.Registries.BuildingEntityRegistry");
			Type type2 = AccessTools.TypeByName("CUCoreLib.Data.CustomBuildingEntityDefinition");
			Type type3 = AccessTools.TypeByName("CUCoreLib.Registries.StructureRegistry");
			getBuildingIdsMethod = AccessTools.Method(type, "GetRegisteredIds", (Type[])null, (Type[])null);
			tryGetBuildingDefinitionMethod = AccessTools.Method(type, "TryGetDefinition", new Type[2]
			{
				typeof(string),
				type2.MakeByRefType()
			}, (Type[])null);
			spawnMinPerChunkField = AccessTools.Field(type2, "SpawnMinPerChunk");
			spawnMaxPerChunkField = AccessTools.Field(type2, "SpawnMaxPerChunk");
			generationStyleField = AccessTools.Field(type2, "GenerationStyle");
			harmony.Patch((MethodBase)AccessTools.Method(type, "DistributeInWorld", (Type[])null, (Type[])null), new HarmonyMethod(typeof(CuCoreLibSpawnCompat), "DistributeInWorld_Prefix", (Type[])null), (HarmonyMethod)null, (HarmonyMethod)null, new HarmonyMethod(typeof(CuCoreLibSpawnCompat), "DistributeInWorld_Finalizer", (Type[])null), (HarmonyMethod)null);
			getStructureIdsMethod = AccessTools.Method(type3, "GetRegisteredIds", (Type[])null, (Type[])null);
			tryGetSpawnCountsMethod = AccessTools.Method(type3, "TryGetSpawnCounts", (Type[])null, (Type[])null);
			trySetSpawnCountsMethod = AccessTools.Method(type3, "TrySetSpawnCounts", (Type[])null, (Type[])null);
			harmony.Patch((MethodBase)AccessTools.Method(type3, "GenerateRegisteredStructures", (Type[])null, (Type[])null), (HarmonyMethod)null, new HarmonyMethod(typeof(CuCoreLibSpawnCompat), "GenerateRegisteredStructures_Postfix", (Type[])null), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
		}
		catch (Exception ex)
		{
			Logger.LogWarning((object)("[SpawnDensity] Failed to hook CUCoreLib spawns: " + ex.Message));
		}
	}

	public static List<string> GetBuildingEntityIds()
	{
		if (getBuildingIdsMethod == null)
		{
			return new List<string>();
		}
		return ((IEnumerable<string>)getBuildingIdsMethod.Invoke(null, null)).Where(delegate(string id)
		{
			object buildingDefinition = GetBuildingDefinition(id);
			return buildingDefinition != null && Convert.ToInt32(generationStyleField.GetValue(buildingDefinition)) != 0;
		}).ToList();
	}

	public static List<string> GetStructureIds()
	{
		if (getStructureIdsMethod == null)
		{
			return new List<string>();
		}
		return ((IEnumerable<string>)getStructureIdsMethod.Invoke(null, null)).ToList();
	}

	private static object GetBuildingDefinition(string id)
	{
		object[] array = new object[2] { id, null };
		return ((bool)tryGetBuildingDefinitionMethod.Invoke(null, array)) ? array[1] : null;
	}

	private static void DistributeInWorld_Prefix(string id, out float[] __state)
	{
		__state = null;
		float multiplier = EntitySpawnManager.GetMultiplier(id);
		if (multiplier != 1f)
		{
			object buildingDefinition = GetBuildingDefinition(id);
			if (buildingDefinition != null)
			{
				float num = (float)spawnMinPerChunkField.GetValue(buildingDefinition);
				float num2 = (float)spawnMaxPerChunkField.GetValue(buildingDefinition);
				__state = new float[2] { num, num2 };
				spawnMinPerChunkField.SetValue(buildingDefinition, num * multiplier);
				spawnMaxPerChunkField.SetValue(buildingDefinition, num2 * multiplier);
			}
		}
	}

	private static void DistributeInWorld_Finalizer(string id, float[] __state)
	{
		if (__state != null)
		{
			object buildingDefinition = GetBuildingDefinition(id);
			if (buildingDefinition != null)
			{
				spawnMinPerChunkField.SetValue(buildingDefinition, __state[0]);
				spawnMaxPerChunkField.SetValue(buildingDefinition, __state[1]);
			}
		}
	}

	private static void GenerateRegisteredStructures_Postfix(ref IEnumerator __result)
	{
		if (!MultiplayerCompat.IsActive)
		{
			__result = ScaleStructureCounts(__result);
		}
	}

	private static IEnumerator ScaleStructureCounts(IEnumerator inner)
	{
		ApplyStructureMultipliers();
		try
		{
			while (inner.MoveNext())
			{
				yield return inner.Current;
			}
		}
		finally
		{
			RestoreStructureCounts();
		}
	}

	private static void ApplyStructureMultipliers()
	{
		RestoreStructureCounts();
		foreach (string structureId in GetStructureIds())
		{
			float mult = StructureSpawnManager.GetMultiplier(structureId);
			if (mult == 1f)
			{
				continue;
			}
			object[] array = new object[2] { structureId, null };
			if ((bool)tryGetSpawnCountsMethod.Invoke(null, array) && array[1] is int[] array2)
			{
				originalStructureCounts[structureId] = array2;
				int[] array3 = array2.Select((int count) => Mathf.RoundToInt((float)count * mult)).ToArray();
				trySetSpawnCountsMethod.Invoke(null, new object[2] { structureId, array3 });
			}
		}
	}

	private static void RestoreStructureCounts()
	{
		foreach (KeyValuePair<string, int[]> originalStructureCount in originalStructureCounts)
		{
			trySetSpawnCountsMethod.Invoke(null, new object[2] { originalStructureCount.Key, originalStructureCount.Value });
		}
		originalStructureCounts.Clear();
	}
}
