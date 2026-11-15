using System;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace QoL_Unknown;

internal class StructureSpawnPatcher
{
	private static ManualLogSource Logger;

	private static readonly FieldInfo worldBlocksField = AccessTools.Field(typeof(WorldGeneration), "worldBlocks");

	public static bool SkippingCurrent = false;

	public static bool IsGeneratingComplexStructure = false;

	public static void Initialize(ManualLogSource logger)
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		Logger = logger;
		StructureSpawnManager.LoadMultipliers();
		new Harmony("org.bepinex.plugins.qol.structurespawn").PatchAll(typeof(StructureSpawnPatcher));
	}

	[HarmonyPatch(typeof(WorldGeneration), "GenerateObjectAtPos")]
	[HarmonyPrefix]
	private static bool GenerateObjectAtPos_Prefix(WorldGeneration __instance, Vector2Int pos, Tilemap tilemap, float blockChance, bool genMode)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		if (ShouldBypassStructureSpawnPatches())
		{
			SkippingCurrent = false;
			IsGeneratingComplexStructure = false;
			return true;
		}
		if (IsGeneratingComplexStructure)
		{
			return ExecuteVanillaGeneration(__instance, pos, tilemap, blockChance, genMode);
		}
		string spawnedObjectName = "Unknown";
		if ((Object)(object)tilemap != (Object)null)
		{
			spawnedObjectName = ((!((Object)(object)((Component)tilemap).transform.parent != (Object)null)) ? ((Object)tilemap).name : ((Object)((Component)tilemap).transform.parent).name);
		}
		float multiplier = StructureSpawnManager.GetMultiplier(spawnedObjectName);
		if (Random.value > multiplier)
		{
			SkippingCurrent = true;
			return false;
		}
		SkippingCurrent = false;
		return ExecuteVanillaGeneration(__instance, pos, tilemap, blockChance, genMode);
	}

	private static bool ExecuteVanillaGeneration(WorldGeneration instance, Vector2Int pos, Tilemap tilemap, float blockChance, bool genMode)
	{
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_014a: Unknown result type (might be due to invalid IL or missing references)
		//IL_014f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0132: Unknown result type (might be due to invalid IL or missing references)
		//IL_0137: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_011b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		ushort[,] worldBlocks = null;
		if (genMode && worldBlocksField != null)
		{
			worldBlocks = worldBlocksField.GetValue(instance) as ushort[,];
		}
		BoundsInt cellBounds = tilemap.cellBounds;
		int num = ((BoundsInt)(ref cellBounds)).xMin;
		Vector3Int val = default(Vector3Int);
		while (true)
		{
			int num2 = num;
			cellBounds = tilemap.cellBounds;
			if (num2 >= ((BoundsInt)(ref cellBounds)).xMax)
			{
				break;
			}
			cellBounds = tilemap.cellBounds;
			int num3 = ((BoundsInt)(ref cellBounds)).yMin;
			while (true)
			{
				int num4 = num3;
				cellBounds = tilemap.cellBounds;
				if (num4 >= ((BoundsInt)(ref cellBounds)).yMax)
				{
					break;
				}
				if (tilemap.HasTile(new Vector3Int(num, num3)) && Random.Range(0f, 1f) < blockChance)
				{
					try
					{
						((Vector3Int)(ref val))._002Ector(num, num3, 0);
						TileBase tile = tilemap.GetTile(val);
						if ((Object)(object)tile == (Object)(object)instance.specialNullTile)
						{
							if (genMode)
							{
								SetWorldBlock(instance, worldBlocks, ((Vector2Int)(ref pos)).x + num, ((Vector2Int)(ref pos)).y + num3, 0);
							}
							else
							{
								instance.SetBlock(new Vector2Int(((Vector2Int)(ref pos)).x + num, ((Vector2Int)(ref pos)).y + num3), (ushort)0);
							}
						}
						else
						{
							ushort num5 = (ushort)Array.IndexOf(instance.tiles, tile);
							if (genMode)
							{
								SetWorldBlock(instance, worldBlocks, ((Vector2Int)(ref pos)).x + num, ((Vector2Int)(ref pos)).y + num3, num5);
							}
							else
							{
								instance.SetBlock(new Vector2Int(((Vector2Int)(ref pos)).x + num, ((Vector2Int)(ref pos)).y + num3), num5);
							}
						}
					}
					catch (Exception)
					{
					}
				}
				num3++;
			}
			num++;
		}
		return false;
	}

	private static void SetWorldBlock(WorldGeneration world, ushort[,] worldBlocks, int x, int y, ushort val)
	{
		if (worldBlocks != null && x >= 0 && y >= 0 && x < world.width && y < world.height)
		{
			worldBlocks[x, y] = val;
		}
	}

	[HarmonyPatch(typeof(WorldGeneration), "GenerateEntityAtPos")]
	[HarmonyPrefix]
	private static bool GenerateEntityAtPos_Prefix()
	{
		if (ShouldBypassStructureSpawnPatches())
		{
			SkippingCurrent = false;
			return true;
		}
		if (SkippingCurrent)
		{
			SkippingCurrent = false;
			return false;
		}
		return true;
	}

	private static bool ShouldBypassStructureSpawnPatches()
	{
		return MultiplayerCompat.IsActive;
	}

	[HarmonyPatch(typeof(WorldGeneration), "GenerateLifePods")]
	[HarmonyPrefix]
	private static void GenerateLifePods_Prefix(ref float amt, ref bool __state)
	{
		if (ShouldBypassStructureSpawnPatches())
		{
			SkippingCurrent = false;
			IsGeneratingComplexStructure = false;
			__state = false;
		}
		else
		{
			float multiplier = StructureSpawnManager.GetMultiplier("Lifepod");
			amt *= multiplier;
			SkippingCurrent = false;
			IsGeneratingComplexStructure = true;
			__state = true;
		}
	}

	[HarmonyPatch(typeof(WorldGeneration), "GenerateLifePods")]
	[HarmonyPostfix]
	private static void GenerateLifePods_Postfix(bool __state)
	{
		if (__state)
		{
			IsGeneratingComplexStructure = false;
			SkippingCurrent = false;
		}
	}

	[HarmonyPatch(typeof(WorldGeneration), "GenerateCollapsedPods")]
	[HarmonyPrefix]
	private static void GenerateCollapsedPods_Prefix(ref float amt, ref bool __state)
	{
		if (ShouldBypassStructureSpawnPatches())
		{
			SkippingCurrent = false;
			IsGeneratingComplexStructure = false;
			__state = false;
		}
		else
		{
			float multiplier = StructureSpawnManager.GetMultiplier("LifepodCollapsed");
			amt *= multiplier;
			SkippingCurrent = false;
			IsGeneratingComplexStructure = true;
			__state = true;
		}
	}

	[HarmonyPatch(typeof(WorldGeneration), "GenerateCollapsedPods")]
	[HarmonyPostfix]
	private static void GenerateCollapsedPods_Postfix(bool __state)
	{
		if (__state)
		{
			IsGeneratingComplexStructure = false;
			SkippingCurrent = false;
		}
	}

	[HarmonyPatch(typeof(WorldGeneration), "GenerateDropCapsules")]
	[HarmonyPrefix]
	private static void GenerateDropCapsules_Prefix(ref float amt, ref bool __state)
	{
		if (ShouldBypassStructureSpawnPatches())
		{
			SkippingCurrent = false;
			IsGeneratingComplexStructure = false;
			__state = false;
		}
		else
		{
			float multiplier = StructureSpawnManager.GetMultiplier("dropcapsule");
			amt *= multiplier;
			SkippingCurrent = false;
			IsGeneratingComplexStructure = true;
			__state = true;
		}
	}

	[HarmonyPatch(typeof(WorldGeneration), "GenerateDropCapsules")]
	[HarmonyPostfix]
	private static void GenerateDropCapsules_Postfix(bool __state)
	{
		if (__state)
		{
			IsGeneratingComplexStructure = false;
			SkippingCurrent = false;
		}
	}
}
