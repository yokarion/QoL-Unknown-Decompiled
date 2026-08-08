using System.Collections.Generic;
using UnityEngine;

namespace QoL_Unknown;

public static class StructureSpawnManager
{
	public static Dictionary<string, float> Multipliers = new Dictionary<string, float>();

	public static readonly List<string> KnownStructures = new List<string>
	{
		"LifepodStart", "Lifepod", "LifepodCollapsed", "dropcapsule", "BioContainer", "Special/TutorialStructure", "Structures/SteelBridge", "Structures/CratePod", "Structures/MiniPod", "Structures/SteelThing",
		"Structures/WoodCross", "Structures/WoodHorizontal", "Structures/MedicalBuilding", "Structures/BrickLoot"
	};

	public static float GetMultiplier(string spawnedObjectName)
	{
		string text = spawnedObjectName.Replace("(Clone)", "").Trim();
		foreach (string key in Multipliers.Keys)
		{
			if (key == text)
			{
				return Multipliers[key];
			}
			if (key.EndsWith("/" + text))
			{
				return Multipliers[key];
			}
		}
		return 1f;
	}

	public static void SetMultiplier(string name, float val)
	{
		Multipliers[name] = Mathf.Clamp01(val);
		PlayerPrefs.SetFloat("StructMult_" + name, val);
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
		foreach (string knownStructure in KnownStructures)
		{
			if (PlayerPrefs.HasKey("StructMult_" + knownStructure))
			{
				Multipliers[knownStructure] = PlayerPrefs.GetFloat("StructMult_" + knownStructure);
			}
		}
	}
}
