using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx.Logging;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace QoL_Unknown;

internal class FontPatcher
{
	private static ManualLogSource Logger;

	private static TMP_FontAsset retroGamingExtendedFontAsset;

	private static readonly string[] RetroNormalNameHints = new string[2] { "Retro GamingPix", "Retro Gaming" };

	public static void Initialize(ManualLogSource logger)
	{
		Logger = logger;
		LoadCustomFonts();
		SceneManager.sceneLoaded += OnSceneLoaded;
		ApplyFallbacks("initial");
	}

	private static void LoadCustomFonts()
	{
		retroGamingExtendedFontAsset = LoadFontFromBundle("retrogamingextended.bundle", "Retro Gaming Extended");
	}

	private static TMP_FontAsset LoadFontFromBundle(string bundleName, string fontDisplayName)
	{
		try
		{
			Assembly executingAssembly = Assembly.GetExecutingAssembly();
			string name = executingAssembly.GetManifestResourceNames().FirstOrDefault((string n) => n.EndsWith(bundleName, StringComparison.OrdinalIgnoreCase));
			using Stream stream = executingAssembly.GetManifestResourceStream(name);
			byte[] array = new byte[stream.Length];
			stream.Read(array, 0, array.Length);
			AssetBundle val = AssetBundle.LoadFromMemory(array);
			TMP_FontAsset val2 = val.LoadAllAssets<TMP_FontAsset>().FirstOrDefault();
			Object.DontDestroyOnLoad((Object)(object)val2);
			val.Unload(false);
			return val2;
		}
		catch (Exception)
		{
			return null;
		}
	}

	private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		ApplyFallbacks(((Scene)(ref scene)).name);
	}

	private static void ApplyFallbacks(string contextName)
	{
		List<TMP_FontAsset> globalFallbackChain = GetGlobalFallbackChain();
		foreach (TMP_FontAsset item in globalFallbackChain)
		{
			if (!TMP_Settings.fallbackFontAssets.Contains(item))
			{
				TMP_Settings.fallbackFontAssets.Add(item);
			}
		}
		TMP_FontAsset retroNormal = FindRetroNormalFont();
		TMP_FontAsset[] array = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
		foreach (TMP_FontAsset val in array)
		{
			if ((Object)(object)val == (Object)null || ((Object)val).name == "LiberationSans SDF")
			{
				continue;
			}
			if (val.fallbackFontAssetTable == null)
			{
				val.fallbackFontAssetTable = new List<TMP_FontAsset>();
			}
			List<TMP_FontAsset> list = BuildPerFontChain(val, retroNormal, globalFallbackChain);
			foreach (TMP_FontAsset item2 in list)
			{
				if (!val.fallbackFontAssetTable.Contains(item2))
				{
					val.fallbackFontAssetTable.Add(item2);
				}
			}
		}
	}

	private static TMP_FontAsset FindRetroNormalFont()
	{
		return Resources.FindObjectsOfTypeAll<TMP_FontAsset>().FirstOrDefault((TMP_FontAsset f) => (Object)(object)f != (Object)null && MatchesAny(((Object)f).name, RetroNormalNameHints));
	}

	internal static TMP_FontAsset GetRetroGamingFont()
	{
		return FindRetroNormalFont() ?? ResourceLookupCache.FindFontContains("Retro GamingPix") ?? ResourceLookupCache.FindFontContains("Retro Gaming");
	}

	internal static TMP_FontAsset GetRetroPixelExtendedFont()
	{
		if ((Object)(object)retroGamingExtendedFontAsset != (Object)null)
		{
			return retroGamingExtendedFontAsset;
		}
		return Resources.FindObjectsOfTypeAll<TMP_FontAsset>().FirstOrDefault((TMP_FontAsset f) => (Object)(object)f != (Object)null && MatchesAny(((Object)f).name, new string[4] { "Retro Pixel Extended", "RetroPixelExtended", "Retro Gaming Extended", "retrogamingextended" }));
	}

	private static List<TMP_FontAsset> GetGlobalFallbackChain()
	{
		List<TMP_FontAsset> list = new List<TMP_FontAsset>();
		AddIfPresent(list, retroGamingExtendedFontAsset);
		return list;
	}

	private static List<TMP_FontAsset> BuildPerFontChain(TMP_FontAsset targetFont, TMP_FontAsset retroNormal, List<TMP_FontAsset> globalChain)
	{
		List<TMP_FontAsset> list = new List<TMP_FontAsset>();
		if ((Object)(object)retroNormal != (Object)null && (Object)(object)targetFont == (Object)(object)retroNormal)
		{
			AddIfPresent(list, retroGamingExtendedFontAsset);
			return list;
		}
		foreach (TMP_FontAsset item in globalChain)
		{
			AddIfPresent(list, item);
		}
		return list;
	}

	private static bool MatchesAny(string value, IEnumerable<string> hints)
	{
		if (string.IsNullOrEmpty(value))
		{
			return false;
		}
		foreach (string hint in hints)
		{
			if (value.IndexOf(hint, StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return true;
			}
		}
		return false;
	}

	private static void AddIfPresent(List<TMP_FontAsset> list, TMP_FontAsset font)
	{
		if ((Object)(object)font != (Object)null && !list.Contains(font))
		{
			list.Add(font);
		}
	}
}
