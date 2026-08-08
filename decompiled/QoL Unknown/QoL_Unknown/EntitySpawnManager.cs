using System.Collections.Generic;
using UnityEngine;

namespace QoL_Unknown;

public static class EntitySpawnManager
{
	public static Dictionary<string, float> Multipliers = new Dictionary<string, float>();

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
		if (Multipliers.TryGetValue(entityName, out var value))
		{
			return value;
		}
		return 1f;
	}

	public static void SetMultiplier(string entityName, float val)
	{
		Multipliers[entityName] = val;
		PlayerPrefs.SetFloat("SpawnMult_" + entityName, val);
	}

	public static void ResetMultipliers()
	{
		Multipliers.Clear();
		foreach (string knownEntity in KnownEntities)
		{
			PlayerPrefs.DeleteKey("SpawnMult_" + knownEntity);
		}
	}

	public static void LoadMultipliers()
	{
		foreach (string knownEntity in KnownEntities)
		{
			if (PlayerPrefs.HasKey("SpawnMult_" + knownEntity))
			{
				Multipliers[knownEntity] = PlayerPrefs.GetFloat("SpawnMult_" + knownEntity);
			}
		}
	}
}
