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

	public static Sprite FindSprite(string name)
	{
		if (string.IsNullOrEmpty(name))
		{
			return null;
		}
		EnsureSpritesLoaded();
		SpritesByName.TryGetValue(name, out var value);
		return value;
	}

	public static Sprite FindSpriteContains(string fragment)
	{
		if (string.IsNullOrEmpty(fragment))
		{
			return null;
		}
		EnsureSpritesLoaded();
		if (SpriteContainsCache.TryGetValue(fragment, out var value))
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
		SpriteContainsCache[fragment] = null;
		return null;
	}

	public static TMP_FontAsset FindFontContains(string fragment)
	{
		if (string.IsNullOrEmpty(fragment))
		{
			return null;
		}
		EnsureFontsLoaded();
		if (FontContainsCache.TryGetValue(fragment, out var value))
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
		FontContainsCache[fragment] = null;
		return null;
	}

	private static void EnsureSpritesLoaded()
	{
		if (spritesLoaded)
		{
			return;
		}
		spritesLoaded = true;
		Sprite[] array = Resources.FindObjectsOfTypeAll<Sprite>();
		foreach (Sprite val in array)
		{
			if (!((Object)(object)val == (Object)null) && !string.IsNullOrEmpty(((Object)val).name) && !SpritesByName.ContainsKey(((Object)val).name))
			{
				SpritesByName.Add(((Object)val).name, val);
			}
		}
	}

	private static void EnsureFontsLoaded()
	{
		if (fontsLoaded)
		{
			return;
		}
		fontsLoaded = true;
		TMP_FontAsset[] array = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
		foreach (TMP_FontAsset val in array)
		{
			if (!((Object)(object)val == (Object)null) && !string.IsNullOrEmpty(((Object)val).name) && !FontsByName.ContainsKey(((Object)val).name))
			{
				FontsByName.Add(((Object)val).name, val);
			}
		}
	}
}
