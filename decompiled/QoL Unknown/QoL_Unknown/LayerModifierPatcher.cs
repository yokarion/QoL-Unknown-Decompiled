using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace QoL_Unknown;

internal class LayerModifierPatcher
{
	[Serializable]
	[CompilerGenerated]
	private sealed class _003C_003Ec
	{
		public static readonly _003C_003Ec _003C_003E9 = new _003C_003Ec();

		public static Action _003C_003E9__15_0;

		internal void _003CRegisterCommands_Postfix_003Eb__15_0(string[] args)
		{
			if (args.Length < 2)
			{
				throw new Exception("Usage: disablelayermodifiers [true/false]");
			}
			bool flag = bool.Parse(args[1]);
			for (int i = 0; i < modifierMultipliers.Length; i++)
			{
				SetMultiplier(i, flag ? 0f : 1f);
			}
		}
	}

	private const float DefaultChance = 3.6363637f;

	private const float MaxMultiplier = 5f;

	private const string ModifierMultiplierPrefPrefix = "LayerModifierMult_";

	private static readonly float[] modifierMultipliers = new float[11];

	public static readonly List<string> ModifierNames = new List<string>
	{
		"Foggy", "Warm", "Hot", "Cold", "Freezing", "Flooded", "Dry", "Rainy", "Infested", "Ionized",
		"Unchipped"
	};

	public static void Initialize(ManualLogSource logger)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Expected O, but got Unknown
		LoadMultipliers();
		Harmony val = new Harmony("org.bepinex.plugins.qol.layermod");
		val.PatchAll(typeof(LayerModifierPatcher));
	}

	public static float GetMultiplier(string modifierName)
	{
		int num = FindModifierIndex(modifierName);
		return (num >= 0) ? GetMultiplier(num) : 1f;
	}

	public static float GetMultiplier(int index)
	{
		if (index < 0 || index >= modifierMultipliers.Length)
		{
			return 1f;
		}
		return modifierMultipliers[index];
	}

	public static void SetMultiplier(string modifierName, float value)
	{
		int num = FindModifierIndex(modifierName);
		if (num >= 0)
		{
			SetMultiplier(num, value);
		}
	}

	public static void SetMultiplier(int index, float value)
	{
		if (index >= 0 && index < modifierMultipliers.Length)
		{
			float num = Mathf.Clamp(value, 0f, 5f);
			modifierMultipliers[index] = num;
			PlayerPrefs.SetFloat(GetModifierMultiplierPrefKey(index), num);
			PlayerPrefs.Save();
		}
	}

	public static void LoadMultipliers()
	{
		for (int i = 0; i < modifierMultipliers.Length; i++)
		{
			modifierMultipliers[i] = PlayerPrefs.GetFloat(GetModifierMultiplierPrefKey(i), 1f);
		}
	}

	public static void ResetMultipliers()
	{
		for (int i = 0; i < modifierMultipliers.Length; i++)
		{
			modifierMultipliers[i] = 1f;
			PlayerPrefs.DeleteKey(GetModifierMultiplierPrefKey(i));
		}
	}

	private static int FindModifierIndex(string modifierName)
	{
		return ModifierNames.FindIndex((string name) => string.Equals(name, modifierName, StringComparison.Ordinal));
	}

	private static string GetModifierMultiplierPrefKey(int index)
	{
		return "LayerModifierMult_" + ModifierNames[index];
	}

	[HarmonyPatch(typeof(WorldGeneration), "ApplyLayerModifiers")]
	[HarmonyPrefix]
	private static bool ApplyLayerModifiers_Prefix(WorldGeneration __instance)
	{
		if (KrokoshaMpCompat.IsActive && KrokoshaMpCompat.IsClient)
		{
			return true;
		}
		float num = Random.Range(0f, 100f);
		float num2 = 0f;
		for (int i = 0; i < modifierMultipliers.Length; i++)
		{
			num2 += 3.6363637f * modifierMultipliers[i];
			if (num < num2)
			{
				LayerModifier val = LayerModifier.availableModifiers[i];
				val.Initialize(__instance);
				val.active = true;
				Traverse.Create((object)__instance).Field("layerPrefix").SetValue((object)Locale.GetOther("layermodifier" + val.modifierIndex));
				Traverse.Create((object)__instance).Field("layerDescription").SetValue((object)Locale.GetOther("layermodifier" + val.modifierIndex + "dsc"));
				break;
			}
		}
		return false;
	}

	[HarmonyPatch(typeof(ConsoleScript), "RegisterAllCommands")]
	[HarmonyPostfix]
	private static void RegisterCommands_Postfix()
	{
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Expected O, but got Unknown
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Expected O, but got Unknown
		List<Command> commands = ConsoleScript.Commands;
		object obj = _003C_003Ec._003C_003E9__15_0;
		if (obj == null)
		{
			Action val = delegate(string[] args)
			{
				if (args.Length < 2)
				{
					throw new Exception("Usage: disablelayermodifiers [true/false]");
				}
				bool flag = bool.Parse(args[1]);
				for (int i = 0; i < modifierMultipliers.Length; i++)
				{
					SetMultiplier(i, flag ? 0f : 1f);
				}
			};
			_003C_003Ec._003C_003E9__15_0 = val;
			obj = (object)val;
		}
		commands.Add(new Command("disablelayermodifiers", "Disables/enables all layer modifiers.", (Action)obj, (Dictionary<int, List<string>>)null, new(string, string)[1] { ("bool disable", "True to set all to 0%, False to set to default") }));
	}
}
