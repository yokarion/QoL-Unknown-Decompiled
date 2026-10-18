using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace QoL_Unknown;

internal static class ResourceLookupCache
{
	private static readonly Dictionary<string, Sprite> SpritesByName = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);

	private static readonly Dictionary<string, Sprite> SpriteContainsCache = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);

	private static readonly Dictionary<string, TMP_FontAsset> FontsByName = new Dictionary<string, TMP_FontAsset>(StringComparer.OrdinalIgnoreCase);

	private static readonly Dictionary<string, TMP_FontAsset> FontContainsCache = new Dictionary<string, TMP_FontAsset>(StringComparer.OrdinalIgnoreCase);

	private static bool spritesLoaded;

	private static bool fontsLoaded;

	private static float nextSpriteRescanTime;

	private static float nextFontRescanTime;

	private const float MissingResourceRescanInterval = 2f;

	public static Sprite FindSprite(string name)
	{
		if (string.IsNullOrEmpty(name))
		{
			return null;
		}
		EnsureSpritesLoaded();
		if (!SpritesByName.TryGetValue(name, out var value) || (Object)(object)value == (Object)null)
		{
			TryRescanSprites();
			SpritesByName.TryGetValue(name, out value);
		}
		return value;
	}

	public static Sprite FindSpriteContains(string fragment)
	{
		if (string.IsNullOrEmpty(fragment))
		{
			return null;
		}
		EnsureSpritesLoaded();
		if (SpriteContainsCache.TryGetValue(fragment, out var value) && (Object)(object)value != (Object)null)
		{
			return value;
		}
		foreach (KeyValuePair<string, Sprite> item in SpritesByName)
		{
			if ((Object)(object)item.Value != (Object)null && item.Key.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0)
			{
				SpriteContainsCache[fragment] = item.Value;
				return item.Value;
			}
		}
		if (!TryRescanSprites())
		{
			return null;
		}
		foreach (KeyValuePair<string, Sprite> item2 in SpritesByName)
		{
			if ((Object)(object)item2.Value != (Object)null && item2.Key.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0)
			{
				SpriteContainsCache[fragment] = item2.Value;
				return item2.Value;
			}
		}
		return null;
	}

	public static TMP_FontAsset FindFontContains(string fragment)
	{
		if (string.IsNullOrEmpty(fragment))
		{
			return null;
		}
		EnsureFontsLoaded();
		if (FontContainsCache.TryGetValue(fragment, out var value) && (Object)(object)value != (Object)null)
		{
			return value;
		}
		foreach (KeyValuePair<string, TMP_FontAsset> item in FontsByName)
		{
			if ((Object)(object)item.Value != (Object)null && item.Key.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0)
			{
				FontContainsCache[fragment] = item.Value;
				return item.Value;
			}
		}
		if (!TryRescanFonts())
		{
			return null;
		}
		foreach (KeyValuePair<string, TMP_FontAsset> item2 in FontsByName)
		{
			if ((Object)(object)item2.Value != (Object)null && item2.Key.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0)
			{
				FontContainsCache[fragment] = item2.Value;
				return item2.Value;
			}
		}
		return null;
	}

	private static void EnsureSpritesLoaded()
	{
		if (!spritesLoaded)
		{
			spritesLoaded = true;
			ScanSprites();
			nextSpriteRescanTime = Time.realtimeSinceStartup + 2f;
		}
	}

	private static void ScanSprites()
	{
		SpriteContainsCache.Clear();
		Sprite[] array = Resources.FindObjectsOfTypeAll<Sprite>();
		foreach (Sprite val in array)
		{
			if (!((Object)(object)val == (Object)null) && !string.IsNullOrEmpty(((Object)val).name))
			{
				SpritesByName[((Object)val).name] = val;
			}
		}
	}

	private static void EnsureFontsLoaded()
	{
		if (!fontsLoaded)
		{
			fontsLoaded = true;
			ScanFonts();
			nextFontRescanTime = Time.realtimeSinceStartup + 2f;
		}
	}

	private static bool TryRescanSprites()
	{
		float realtimeSinceStartup = Time.realtimeSinceStartup;
		if (realtimeSinceStartup < nextSpriteRescanTime)
		{
			return false;
		}
		nextSpriteRescanTime = realtimeSinceStartup + 2f;
		ScanSprites();
		return true;
	}

	private static void ScanFonts()
	{
		FontContainsCache.Clear();
		TMP_FontAsset[] array = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
		foreach (TMP_FontAsset val in array)
		{
			if (!((Object)(object)val == (Object)null) && !string.IsNullOrEmpty(((Object)val).name))
			{
				FontsByName[((Object)val).name] = val;
			}
		}
	}

	private static bool TryRescanFonts()
	{
		float realtimeSinceStartup = Time.realtimeSinceStartup;
		if (realtimeSinceStartup < nextFontRescanTime)
		{
			return false;
		}
		nextFontRescanTime = realtimeSinceStartup + 2f;
		ScanFonts();
		return true;
	}
}
