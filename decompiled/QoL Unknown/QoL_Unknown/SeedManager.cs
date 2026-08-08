using System.IO;
using UnityEngine;

namespace QoL_Unknown;

public static class SeedManager
{
	public static string InputString = "";

	public static int CurrentSeed = 0;

	public static bool IsSeeded = false;

	public static bool EnsureNonZeroSeedForLoadedRun()
	{
		if (!SaveSystem.loadedRun)
		{
			return false;
		}
		if (IsSeeded && CurrentSeed != 0)
		{
			return false;
		}
		int num = GetDeterministicSeedFromSaveFile();
		if (num == 0)
		{
			num = 1;
		}
		CurrentSeed = num;
		IsSeeded = true;
		if (string.IsNullOrWhiteSpace(InputString))
		{
			InputString = num.ToString();
		}
		return true;
	}

	private static int GetDeterministicSeedFromSaveFile()
	{
		string path = Path.Combine(Application.persistentDataPath, "save.sv");
		if (!File.Exists(path))
		{
			return 0;
		}
		byte[] array = File.ReadAllBytes(path);
		if (array == null || array.Length == 0)
		{
			return 0;
		}
		uint num = 2166136261u;
		for (int i = 0; i < array.Length; i++)
		{
			num ^= array[i];
			num *= 16777619;
		}
		int num2 = (int)num;
		return (num2 == 0) ? 1 : num2;
	}

	public static int GetStableHash(string str)
	{
		if (string.IsNullOrEmpty(str))
		{
			return 0;
		}
		int num = 23;
		foreach (char c in str)
		{
			num = num * 31 + c;
		}
		return num;
	}

	public static void SetSeed(string input)
	{
		InputString = input;
		if (string.IsNullOrWhiteSpace(input))
		{
			IsSeeded = false;
			CurrentSeed = 0;
			return;
		}
		IsSeeded = true;
		if (int.TryParse(input, out var result))
		{
			CurrentSeed = result;
		}
		else
		{
			CurrentSeed = GetStableHash(input);
		}
	}

	public static void SetNetworkSeed(int seed, string input)
	{
		CurrentSeed = seed;
		InputString = input ?? "";
		IsSeeded = true;
	}
}
