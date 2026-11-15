using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Bootstrap;
using HarmonyLib;
using UnityEngine;

namespace QoL_Unknown;

public static class SoundLibrary
{
	public static List<string> AllClipNames = new List<string>();

	private static readonly Dictionary<string, AudioClip> CuCoreLibClips = new Dictionary<string, AudioClip>(StringComparer.OrdinalIgnoreCase);

	private static bool vanillaLoaded;

	private static bool cuCoreLibCacheResolved;

	private static FieldInfo cuCoreLibAudioCacheField;

	public static IEnumerable<string> CuCoreLibClipNames => CuCoreLibClips.Keys;

	public static void Load()
	{
		if (!vanillaLoaded)
		{
			vanillaLoaded = true;
			try
			{
				AudioClip[] array = Resources.LoadAll<AudioClip>("Sounds");
				foreach (AudioClip val in array)
				{
					if ((Object)(object)val != (Object)null && !IsIntegerClipName(((Object)val).name))
					{
						AllClipNames.Add(((Object)val).name);
					}
				}
				array = Resources.LoadAll<AudioClip>("Sounds/music");
				foreach (AudioClip val2 in array)
				{
					if ((Object)(object)val2 != (Object)null && !IsIntegerClipName(((Object)val2).name) && !AllClipNames.Contains(((Object)val2).name))
					{
						AllClipNames.Add(((Object)val2).name);
					}
				}
			}
			catch
			{
			}
		}
		foreach (AudioClip cuCoreLibCachedClip in GetCuCoreLibCachedClips())
		{
			if (!((Object)(object)cuCoreLibCachedClip == (Object)null) && !string.IsNullOrWhiteSpace(((Object)cuCoreLibCachedClip).name))
			{
				CuCoreLibClips[((Object)cuCoreLibCachedClip).name] = cuCoreLibCachedClip;
				if (!AllClipNames.Contains(((Object)cuCoreLibCachedClip).name))
				{
					AllClipNames.Add(((Object)cuCoreLibCachedClip).name);
				}
			}
		}
		AllClipNames.Sort();
	}

	public static AudioClip GetPreviewClip(string clipName)
	{
		AudioClip value = Resources.Load<AudioClip>("Sounds/" + clipName) ?? Resources.Load<AudioClip>("Sounds/" + clipName.ToLowerInvariant());
		if ((Object)(object)value == (Object)null)
		{
			CuCoreLibClips.TryGetValue(clipName, out value);
		}
		return value;
	}

	private static List<AudioClip> GetCuCoreLibCachedClips()
	{
		if (!cuCoreLibCacheResolved)
		{
			cuCoreLibCacheResolved = true;
			if (Chainloader.PluginInfos.ContainsKey("net.cucorelib"))
			{
				cuCoreLibAudioCacheField = AccessTools.Field(AccessTools.TypeByName("CUCoreLib.Helpers.AssetLoader"), "AudioClipCache");
			}
		}
		if (!(cuCoreLibAudioCacheField?.GetValue(null) is Dictionary<string, AudioClip> dictionary))
		{
			return new List<AudioClip>();
		}
		return dictionary.Values.ToList();
	}

	private static bool IsIntegerClipName(string clipName)
	{
		int result;
		return int.TryParse(clipName, out result);
	}
}
