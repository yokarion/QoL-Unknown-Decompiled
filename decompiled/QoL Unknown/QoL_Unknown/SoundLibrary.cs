using System.Collections.Generic;
using UnityEngine;

namespace QoL_Unknown;

public static class SoundLibrary
{
	public static List<string> AllClipNames = new List<string>();

	public static void Load()
	{
		if (AllClipNames.Count > 0)
		{
			return;
		}
		try
		{
			AudioClip[] array = Resources.LoadAll<AudioClip>("Sounds");
			AudioClip[] array2 = array;
			foreach (AudioClip val in array2)
			{
				if ((Object)(object)val != (Object)null && !IsIntegerClipName(((Object)val).name))
				{
					AllClipNames.Add(((Object)val).name);
				}
			}
			AudioClip[] array3 = Resources.LoadAll<AudioClip>("Sounds/music");
			AudioClip[] array4 = array3;
			foreach (AudioClip val2 in array4)
			{
				if ((Object)(object)val2 != (Object)null && !IsIntegerClipName(((Object)val2).name) && !AllClipNames.Contains(((Object)val2).name))
				{
					AllClipNames.Add(((Object)val2).name);
				}
			}
			AllClipNames.Sort();
		}
		catch
		{
		}
	}

	private static bool IsIntegerClipName(string clipName)
	{
		int result;
		return int.TryParse(clipName, out result);
	}
}
