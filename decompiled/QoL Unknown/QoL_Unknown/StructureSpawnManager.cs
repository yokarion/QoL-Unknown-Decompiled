using System;
using System.Collections.Generic;
using UnityEngine;

namespace QoL_Unknown;

public static class StructureSpawnManager
{
	public static readonly Dictionary<string, float> Multipliers = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);

	public static readonly List<string> KnownStructures = new List<string>
	{
		"LifepodStart", "Lifepod", "LifepodCollapsed", "dropcapsule", "BioContainer", "Special/TutorialStructure", "Structures/SteelBridge", "Structures/CratePod", "Structures/MiniPod", "Structures/SteelThing",
		"Structures/WoodCross", "Structures/WoodHorizontal", "Structures/MedicalBuilding", "Structures/BrickLoot"
	};

	public static float GetMultiplier(string spawnedObjectName)
	{
		if (string.IsNullOrWhiteSpace(spawnedObjectName))
		{
			return 1f;
		}
		string text = spawnedObjectName.Replace("(Clone)", "").Trim();
		if (Multipliers.TryGetValue(text, out var value))
		{
			return value;
		}
		foreach (KeyValuePair<string, float> multiplier in Multipliers)
		{
			if (multiplier.Key.EndsWith("/" + text, StringComparison.OrdinalIgnoreCase))
			{
				return multiplier.Value;
			}
		}
		return 1f;
	}

	public static void SetMultiplier(string name, float val)
	{
		if (!string.IsNullOrWhiteSpace(name))
		{
			float num = NormalizeMultiplier(val);
			Multipliers[name] = num;
			PlayerPrefs.SetFloat("StructMult_" + name, num);
		}
	}

	public static void ResetMultipliers()
	{
		Multipliers.Clear();
		foreach (string knownStructure in KnownStructures)
		{
			PlayerPrefs.DeleteKey("StructMult_" + knownStructure);
		}
	}

	public static void LoadMultipliers()
	{
		Multipliers.Clear();
		foreach (string knownStructure in KnownStructures)
		{
			if (PlayerPrefs.HasKey("StructMult_" + knownStructure))
			{
				Multipliers[knownStructure] = NormalizeMultiplier(PlayerPrefs.GetFloat("StructMult_" + knownStructure));
			}
		}
	}

	private static float NormalizeMultiplier(float value)
	{
		return (float.IsNaN(value) || float.IsInfinity(value)) ? 1f : Mathf.Clamp01(value);
	}
}
