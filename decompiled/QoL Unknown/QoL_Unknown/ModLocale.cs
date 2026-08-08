using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using Newtonsoft.Json;
using UnityEngine;

namespace QoL_Unknown;

internal static class ModLocale
{
	private const string LocaleResourceSuffix = "qol_locale_en.json";

	private const string DefaultLocaleName = "EN";

	private static readonly Dictionary<string, string> englishFallbacks = new Dictionary<string, string>();

	private static readonly Dictionary<string, string> activeOverrides = new Dictionary<string, string>();

	private static ManualLogSource Logger;

	private static string loadedLocaleName;

	public static void Initialize(ManualLogSource logger)
	{
		Logger = logger;
		LoadEnglishFallbacks();
		Reload();
	}

	public static string Get(string key, string fallback = null)
	{
		if (string.IsNullOrEmpty(key))
		{
			return fallback ?? string.Empty;
		}
		EnsureCurrentLocaleLoaded();
		if (activeOverrides.TryGetValue(key, out var value) && !string.IsNullOrEmpty(value))
		{
			return value;
		}
		if (englishFallbacks.TryGetValue(key, out var value2) && !string.IsNullOrEmpty(value2))
		{
			return value2;
		}
		return fallback ?? key;
	}

	public static void Reload()
	{
		activeOverrides.Clear();
		loadedLocaleName = GetCurrentLocaleName();
		LoadExternalOverrides(loadedLocaleName);
	}

	private static void EnsureCurrentLocaleLoaded()
	{
		string currentLocaleName = GetCurrentLocaleName();
		if (!string.Equals(currentLocaleName, loadedLocaleName, StringComparison.OrdinalIgnoreCase))
		{
			Reload();
		}
	}

	private static string GetCurrentLocaleName()
	{
		if (!string.IsNullOrEmpty(Locale.currentLangName))
		{
			return Locale.currentLangName;
		}
		string text = PlayerPrefs.GetString("locale", string.Empty);
		return string.IsNullOrEmpty(text) ? "EN" : text;
	}

	private static void LoadEnglishFallbacks()
	{
		englishFallbacks.Clear();
		try
		{
			Assembly executingAssembly = Assembly.GetExecutingAssembly();
			string text = executingAssembly.GetManifestResourceNames().FirstOrDefault((string name) => name.EndsWith("qol_locale_en.json", StringComparison.OrdinalIgnoreCase));
			if (string.IsNullOrEmpty(text))
			{
				ManualLogSource logger = Logger;
				if (logger != null)
				{
					logger.LogWarning((object)"[ModLocale] Embedded locale resource not found: qol_locale_en.json");
				}
				return;
			}
			using Stream stream = executingAssembly.GetManifestResourceStream(text);
			using StreamReader streamReader = new StreamReader(stream);
			MergeInto(englishFallbacks, streamReader.ReadToEnd(), "embedded English locale");
		}
		catch (Exception ex)
		{
			ManualLogSource logger2 = Logger;
			if (logger2 != null)
			{
				logger2.LogWarning((object)("[ModLocale] Failed to load embedded English locale: " + ex.Message));
			}
		}
	}

	private static void LoadExternalOverrides(string localeName)
	{
		string text = Path.Combine(Paths.ConfigPath, "QoL Unknown", "Locales", localeName + ".json");
		if (!File.Exists(text))
		{
			return;
		}
		try
		{
			MergeInto(activeOverrides, File.ReadAllText(text), text);
		}
		catch (Exception ex)
		{
			ManualLogSource logger = Logger;
			if (logger != null)
			{
				logger.LogWarning((object)("[ModLocale] Failed to load locale override '" + text + "': " + ex.Message));
			}
		}
	}

	private static void MergeInto(Dictionary<string, string> target, string json, string source)
	{
		Dictionary<string, string> dictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
		if (dictionary == null)
		{
			ManualLogSource logger = Logger;
			if (logger != null)
			{
				logger.LogWarning((object)("[ModLocale] Locale source '" + source + "' did not contain a string dictionary."));
			}
			return;
		}
		foreach (KeyValuePair<string, string> item in dictionary)
		{
			target[item.Key] = item.Value;
		}
	}
}
