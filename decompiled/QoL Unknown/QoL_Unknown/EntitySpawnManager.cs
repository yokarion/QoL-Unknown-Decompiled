using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace QoL_Unknown;

public static class EntitySpawnManager
{
	public static readonly Dictionary<string, float> Multipliers = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);

	public static readonly List<string> KnownEntities = new List<string>
	{
		"glowplant", "stoneplant", "ceilingrye", "medcrate", "containercrate", "foodbox", "spikestabber", "shadecrawler", "corpse", "animalcorpse",
		"drillpod", "barbedwirefence", "beartrap", "CaveTicks", "geyser", "jumppad", "bandage", "droppings", "fleshchunk", "geotree",
		"mushplant", "leadbush", "cactus", "sandrose", "drybush", "brownshroom", "stalagmite", "landmine", "spentfuel", "soundcannon",
		"plantiation", "coil", "wallbiter", "oilpipe", "turret", "stalactite", "Special/sandvinehook", "Special/sandvinerope", "rag", "thornbackyoung",
		"overgrowntick", "caveticks", "thornbackelder", "browncap", "bananaplant", "grabberplant", "skullcrusher", "wallflower", "LifepodStart", "Special/wallholes",
		"minibarrel", "experimentflesh", "internalorgans", "lifepodchest", "LoreNote", "dropcapsule", "drillpodbroken", "BloodCrystal", "SoothingCrystal", "ReliefCrystal",
		"TurbulentCrystal", "OxygenCrystal", "EmissiveCrystal", "DigestionCrystal"
	};

	public static float GetMultiplier(string entityName)
	{
		if (string.IsNullOrWhiteSpace(entityName))
		{
			return 1f;
		}
		if (Multipliers.TryGetValue(entityName, out var value))
		{
			return value;
		}
		string text = "SpawnMult_" + entityName;
		if (!PlayerPrefs.HasKey(text))
		{
			return 1f;
		}
		value = NormalizeMultiplier(PlayerPrefs.GetFloat(text));
		Multipliers[entityName] = value;
		return value;
	}

	public static List<string> GetAllEntities()
	{
		return KnownEntities.Concat(CuCoreLibSpawnCompat.GetBuildingEntityIds()).ToList();
	}

	public static void SetMultiplier(string entityName, float val)
	{
		if (!string.IsNullOrWhiteSpace(entityName))
		{
			float num = NormalizeMultiplier(val);
			Multipliers[entityName] = num;
			PlayerPrefs.SetFloat("SpawnMult_" + entityName, num);
		}
	}

	public static void ResetMultipliers()
	{
		Multipliers.Clear();
		foreach (string allEntity in GetAllEntities())
		{
			PlayerPrefs.DeleteKey("SpawnMult_" + allEntity);
		}
	}

	public static void LoadMultipliers()
	{
		Multipliers.Clear();
		foreach (string knownEntity in KnownEntities)
		{
			if (PlayerPrefs.HasKey("SpawnMult_" + knownEntity))
			{
				Multipliers[knownEntity] = NormalizeMultiplier(PlayerPrefs.GetFloat("SpawnMult_" + knownEntity));
			}
		}
	}

	private static float NormalizeMultiplier(float value)
	{
		return (float.IsNaN(value) || float.IsInfinity(value)) ? 1f : Mathf.Clamp(value, 0f, 10f);
	}
}
