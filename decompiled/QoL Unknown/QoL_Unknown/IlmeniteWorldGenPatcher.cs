using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace QoL_Unknown;

internal static class IlmeniteWorldGenPatcher
{
	private const string EnabledPrefKey = "UseV6IlmeniteWorldgen";

	private static readonly FieldInfo WorldBlocksField = AccessTools.Field(typeof(WorldGeneration), "worldBlocks");

	private static ManualLogSource Logger;

	public static void Initialize(ManualLogSource logger)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Expected O, but got Unknown
		Logger = logger;
		Harmony val = new Harmony("org.bepinex.plugins.qol.ilmeniteworldgen");
		val.PatchAll(typeof(IlmeniteWorldGenPatcher));
		ManualLogSource logger2 = Logger;
		if (logger2 != null)
		{
			logger2.LogInfo((object)"IlmeniteWorldGenPatcher initialized");
		}
	}

	public static bool IsEnabled()
	{
		return PlayerPrefs.GetInt("UseV6IlmeniteWorldgen", 0) == 1;
	}

	public static void SetEnabled(bool enabled)
	{
		PlayerPrefs.SetInt("UseV6IlmeniteWorldgen", enabled ? 1 : 0);
		PlayerPrefs.Save();
	}

	[HarmonyPatch(typeof(WorldGeneration), "GenerateOres")]
	[HarmonyPrefix]
	private static bool GenerateOres_Prefix(WorldGeneration __instance)
	{
		if (!IsEnabled() || (Object)(object)__instance == (Object)null || __instance.biomeDepth >= 4)
		{
			return true;
		}
		GenerateIlmeniteUsingV6Threshold(__instance);
		return false;
	}

	private static void GenerateIlmeniteUsingV6Threshold(WorldGeneration world)
	{
		//IL_0118: Unknown result type (might be due to invalid IL or missing references)
		//IL_0158: Unknown result type (might be due to invalid IL or missing references)
		//IL_015d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0162: Unknown result type (might be due to invalid IL or missing references)
		float runSettingFloat = WorldGeneration.GetRunSettingFloat("oreamount");
		if (!(WorldBlocksField?.GetValue(world) is ushort[,] array))
		{
			ManualLogSource logger = Logger;
			if (logger != null)
			{
				logger.LogWarning((object)"Skipping v6 ilmenite worldgen because worldBlocks was unavailable.");
			}
			return;
		}
		Vector2Int val = default(Vector2Int);
		for (int num = Mathf.RoundToInt((float)((int)(world.chunkWidth * world.chunkHeight) / 2) * runSettingFloat); num > 0; num--)
		{
			((Vector2Int)(ref val))._002Ector((int)Random.Range(0f, (float)world.width), (int)Random.Range(0f, (float)world.height));
			for (int num2 = Random.Range(1, 26); num2 > 0; num2--)
			{
				if (((Vector2Int)(ref val)).x > 0 && ((Vector2Int)(ref val)).x < world.width - 1 && ((Vector2Int)(ref val)).y > 0 && ((Vector2Int)(ref val)).y < world.height - 1 && array[((Vector2Int)(ref val)).x, ((Vector2Int)(ref val)).y] > 0)
				{
					array[((Vector2Int)(ref val)).x, ((Vector2Int)(ref val)).y] = 34;
				}
				val += new Vector2Int((Random.value > 0.5f) ? ((Random.value > 0.5f) ? 1 : (-1)) : 0, (Random.value > 0.5f) ? ((Random.value > 0.5f) ? 1 : (-1)) : 0);
			}
		}
		int num3 = 0;
		int num4 = Mathf.RoundToInt(1024f / Mathf.Clamp(runSettingFloat, 0.01f, 999f));
		for (int i = 0; i < world.width; i++)
		{
			for (int j = 0; j < world.height; j++)
			{
				num3++;
				if (num3 > num4 && Random.value < 0.001f)
				{
					if (array[i, j] > 0)
					{
						array[i, j] = 35;
					}
					num3 = 0;
				}
			}
		}
	}
}
