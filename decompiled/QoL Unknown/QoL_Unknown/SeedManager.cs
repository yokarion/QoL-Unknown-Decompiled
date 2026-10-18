using System.Globalization;
using System.IO;
using UnityEngine;

namespace QoL_Unknown;

public static class SeedManager
{
	public const int LegacySeedFormatVersion = 1;

	public const int CurrentSeedFormatVersion = 2;

	public static string InputString = "";

	public static int CurrentSeed = 0;

	public static bool IsSeeded = false;

	public static int SeedFormatVersion = 2;

	public static bool EnsureNonZeroSeedForLoadedRun()
	{
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Invalid comparison between Unknown and I4
		if (!SaveSystem.loadedRun)
		{
			return false;
		}
		if (IsSeeded)
		{
			return false;
		}
		if ((Object)(object)WorldGeneration.world != (Object)null && (int)WorldGeneration.world.biomeOverride == 2)
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
		SeedFormatVersion = 1;
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
		string text = (InputString = input?.Trim() ?? "");
		if (text.Length == 0)
		{
			IsSeeded = false;
			CurrentSeed = 0;
			SeedFormatVersion = 2;
			return;
		}
		IsSeeded = true;
		SeedFormatVersion = 2;
		if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
		{
			CurrentSeed = result;
		}
		else
		{
			CurrentSeed = GetStableHash(text);
		}
	}

	public static void SetNetworkSeed(int seed, string input)
	{
		CurrentSeed = seed;
		InputString = input?.Trim() ?? "";
		IsSeeded = true;
		SeedFormatVersion = 2;
	}

	public static void RestorePersistedSeed(int seed, string input, int formatVersion)
	{
		CurrentSeed = seed;
		InputString = input?.Trim() ?? "";
		IsSeeded = true;
		SeedFormatVersion = ((formatVersion <= 0) ? 1 : formatVersion);
	}
}
