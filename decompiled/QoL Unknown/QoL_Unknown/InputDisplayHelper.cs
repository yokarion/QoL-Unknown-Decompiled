using System.Collections.Generic;
using UnityEngine;

namespace QoL_Unknown;

public static class InputDisplayHelper
{
	private static Dictionary<KeyCode, Sprite> spriteCache = new Dictionary<KeyCode, Sprite>();

	private static readonly Dictionary<KeyCode, string> friendlyNames = new Dictionary<KeyCode, string>
	{
		{
			(KeyCode)323,
			"Left Click"
		},
		{
			(KeyCode)324,
			"Right Click"
		},
		{
			(KeyCode)325,
			"Middle Click"
		},
		{
			(KeyCode)48,
			"0"
		},
		{
			(KeyCode)49,
			"1"
		},
		{
			(KeyCode)50,
			"2"
		},
		{
			(KeyCode)51,
			"3"
		},
		{
			(KeyCode)52,
			"4"
		},
		{
			(KeyCode)53,
			"5"
		},
		{
			(KeyCode)54,
			"6"
		},
		{
			(KeyCode)55,
			"7"
		},
		{
			(KeyCode)56,
			"8"
		},
		{
			(KeyCode)57,
			"9"
		},
		{
			(KeyCode)13,
			"Enter"
		},
		{
			(KeyCode)27,
			"Esc"
		},
		{
			(KeyCode)96,
			"~"
		}
	};

	public unsafe static Sprite GetKeySprite(KeyCode key)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		if (spriteCache.TryGetValue(key, out var value))
		{
			return value;
		}
		string name = "Key_" + ((object)(*(KeyCode*)(&key))/*cast due to constrained. prefix*/).ToString();
		Sprite val = ResourceLookupCache.FindSprite(name);
		if ((Object)(object)val != (Object)null)
		{
			spriteCache[key] = val;
			return val;
		}
		return null;
	}

	public unsafe static string GetFriendlyName(KeyCode key)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		if (KeybindCombos.IsCombo(key))
		{
			return KeybindCombos.Format(key);
		}
		if (friendlyNames.TryGetValue(key, out var value))
		{
			return value;
		}
		return ((object)(*(KeyCode*)(&key))/*cast due to constrained. prefix*/).ToString();
	}
}
