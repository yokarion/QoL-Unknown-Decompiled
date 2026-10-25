using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace QoL_Unknown;

internal class AudioSafetyPatcher
{
	[Serializable]
	[CompilerGenerated]
	private sealed class _003C_003Ec
	{
		public static readonly _003C_003Ec _003C_003E9 = new _003C_003Ec();

		public static Func<KeyValuePair<string, float>, string> _003C_003E9__12_0;

		public static Action _003C_003E9__19_1;

		internal string _003CSaveOverrides_003Eb__12_0(KeyValuePair<string, float> x)
		{
			return $"{x.Key}={x.Value}";
		}

		internal void _003CRegisterCommands_Postfix_003Eb__19_1(string[] args)
		{
			if (args.Length < 2)
			{
				throw new Exception("Usage: noise [change/list/remove/clear] ...");
			}
			string text = args[1].ToLower();
			if (text == "list")
			{
				if (UserVolumeOverrides.Count == 0)
				{
					ConsoleHelper.LogToConsole(ConsoleScript.instance, "No custom sound overrides set.");
					return;
				}
				string text2 = "Current Sound Overrides\n";
				foreach (KeyValuePair<string, float> userVolumeOverride in UserVolumeOverrides)
				{
					text2 += $" - \"{userVolumeOverride.Key}\": {userVolumeOverride.Value * 100f:0}%\n";
				}
				ConsoleHelper.LogToConsole(ConsoleScript.instance, text2.TrimEnd(Array.Empty<char>()));
				return;
			}
			if (text == "clear")
			{
				UserVolumeOverrides.Clear();
				SaveOverrides();
				ConsoleHelper.LogToConsole(ConsoleScript.instance, "Cleared all custom sound overrides.");
				return;
			}
			if (args.Length < 3)
			{
				throw new Exception("Missing sound name argument.");
			}
			string text3 = args[2].ToLower();
			if (text == "remove" || text == "delete")
			{
				if (UserVolumeOverrides.ContainsKey(text3))
				{
					UserVolumeOverrides.Remove(text3);
					SaveOverrides();
					ConsoleHelper.LogToConsole(ConsoleScript.instance, "Removed override for \"" + text3 + "\".");
				}
				else
				{
					ConsoleHelper.LogToConsole(ConsoleScript.instance, "No override found for \"" + text3 + "\".");
				}
				return;
			}
			if (text == "change" || text == "set" || text == "add")
			{
				if (args.Length < 4)
				{
					throw new Exception("Missing volume argument. Usage: noise change [name] [volume]");
				}
				if (float.TryParse(args[3], out var result))
				{
					float num = ((result > 5f) ? (result / 100f) : result);
					num = Mathf.Clamp(num, 0f, 1.2f);
					if (UserVolumeOverrides.ContainsKey(text3))
					{
						UserVolumeOverrides[text3] = num;
					}
					else
					{
						UserVolumeOverrides.Add(text3, num);
					}
					SaveOverrides();
					ConsoleHelper.LogToConsole(ConsoleScript.instance, $"Set volume for \"{text3}\" to {num} ({num * 100f:0}%).");
					return;
				}
				throw new Exception("Invalid volume number.");
			}
			throw new Exception("Unknown action '" + text + "'. Use change, list, remove, or clear.");
		}
	}

	private static ManualLogSource Logger;

	private const string PREFS_KEY = "QoL_AudioOverrides";

	private const float PitchCap = 1.25f;

	internal const float MaxUserVolumeOverride = 1.2f;

	private const float LoudNoiseMultiplier = 0.15f;

	private const float FirearmNoiseMultiplier = 0.35f;

	private const float SomewhatLoudNoiseMultiplier = 0.5f;

	private static Dictionary<string, float> UserVolumeOverrides = new Dictionary<string, float>();

	private static readonly HashSet<string> LOUDNoises = new HashSet<string>
	{
		"sonarmegaouchfixed", "sonarmegaouch", "sonarouch", "explosion", "warningloop", "lobotomy", "cuteye", "shotgunshot", "gunrifleshot", "pistolshot",
		"rifleshot", "pumpend"
	};

	private static readonly HashSet<string> SomewhatLoudNoises = new HashSet<string>
	{
		"mindwipe", "healthpanel-alert", "laststanddrone", "pumpstart", "cut5", "megabite", "drillloop", "cut1", "cut2", "cut3",
		"glass", "zap", "saw", "selfharm1", "bulbbreak", "beep", "turretcount", "turretsee"
	};

	private static readonly HashSet<string> FirearmNoises = new HashSet<string> { "shotgunshot", "gunrifleshot", "pistolshot", "rifleshot" };

	public static void Initialize(ManualLogSource logger)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Expected O, but got Unknown
		Logger = logger;
		LoadOverrides();
		Harmony val = new Harmony("org.bepinex.plugins.qol.audiosafety");
		val.PatchAll(typeof(AudioSafetyPatcher));
	}

	private static void SaveOverrides()
	{
		try
		{
			string text = string.Join(";", UserVolumeOverrides.Select((KeyValuePair<string, float> x) => $"{x.Key}={x.Value}"));
			PlayerPrefs.SetString("QoL_AudioOverrides", text);
			PlayerPrefs.Save();
		}
		catch (Exception ex)
		{
			Logger.LogError((object)("Read/write issue in mod :( " + ex));
		}
	}

	private static void LoadOverrides()
	{
		try
		{
			if (!PlayerPrefs.HasKey("QoL_AudioOverrides"))
			{
				return;
			}
			string text = PlayerPrefs.GetString("QoL_AudioOverrides");
			string[] array = text.Split(new char[1] { ';' });
			UserVolumeOverrides.Clear();
			string[] array2 = array;
			foreach (string text2 in array2)
			{
				if (!string.IsNullOrWhiteSpace(text2))
				{
					string[] array3 = text2.Split(new char[1] { '=' });
					if (array3.Length == 2 && float.TryParse(array3[1], out var result))
					{
						UserVolumeOverrides[array3[0]] = Mathf.Clamp(result, 0f, 1.2f);
					}
				}
			}
		}
		catch (Exception ex)
		{
			Logger.LogError((object)("Read/write issue in mod :( " + ex));
		}
	}

	public static float GetOverride(string soundName)
	{
		if (TryGetOverrideMultiplier(soundName, out var volumeMultiplier))
		{
			return volumeMultiplier;
		}
		return 1f;
	}

	public static void SetOverride(string soundName, float volume)
	{
		float value = Mathf.Clamp(volume, 0f, 1.2f);
		if (UserVolumeOverrides.ContainsKey(soundName))
		{
			UserVolumeOverrides[soundName] = value;
		}
		else
		{
			UserVolumeOverrides.Add(soundName, value);
		}
		SaveOverrides();
	}

	public static void ClearAllOverrides()
	{
		UserVolumeOverrides.Clear();
		SaveOverrides();
	}

	private static bool TryGetOverrideMultiplier(string soundName, out float volumeMultiplier)
	{
		volumeMultiplier = 1f;
		if (string.IsNullOrWhiteSpace(soundName) || UserVolumeOverrides.Count == 0)
		{
			return false;
		}
		string text = soundName.ToLowerInvariant();
		foreach (KeyValuePair<string, float> userVolumeOverride in UserVolumeOverrides)
		{
			if (string.IsNullOrWhiteSpace(userVolumeOverride.Key) || !text.Contains(userVolumeOverride.Key))
			{
				continue;
			}
			volumeMultiplier = userVolumeOverride.Value;
			return true;
		}
		return false;
	}

	internal static float GetVolumeMultiplierOrDefault(string soundName)
	{
		float volumeMultiplier;
		return TryGetOverrideMultiplier(soundName, out volumeMultiplier) ? Mathf.Max(0f, volumeMultiplier) : 1f;
	}

	[HarmonyPatch(typeof(ConsoleScript), "RegisterAllCommands")]
	[HarmonyPostfix]
	private static void RegisterCommands_Postfix()
	{
		//IL_0152: Unknown result type (might be due to invalid IL or missing references)
		//IL_015c: Expected O, but got Unknown
		//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		//IL_0109: Expected O, but got Unknown
		List<string> allSounds = new List<string>();
		try
		{
			AudioClip[] array = Resources.LoadAll<AudioClip>("Sounds");
			AudioClip[] array2 = array;
			foreach (AudioClip val in array2)
			{
				if ((Object)(object)val != (Object)null)
				{
					allSounds.Add(((Object)val).name);
				}
			}
		}
		catch
		{
		}
		SoundLibrary.Load();
		allSounds.AddRange(SoundLibrary.CuCoreLibClipNames.Where((string name) => !allSounds.Contains(name)));
		Dictionary<int, List<string>> dictionary = new Dictionary<int, List<string>>
		{
			{
				0,
				new List<string> { "change", "list", "remove", "clear" }
			},
			{ 1, allSounds }
		};
		List<Command> commands = ConsoleScript.Commands;
		object obj2 = _003C_003Ec._003C_003E9__19_1;
		if (obj2 == null)
		{
			Action val2 = delegate(string[] args)
			{
				if (args.Length < 2)
				{
					throw new Exception("Usage: noise [change/list/remove/clear] ...");
				}
				string text = args[1].ToLower();
				if (text == "list")
				{
					if (UserVolumeOverrides.Count == 0)
					{
						ConsoleHelper.LogToConsole(ConsoleScript.instance, "No custom sound overrides set.");
					}
					else
					{
						string text2 = "Current Sound Overrides\n";
						foreach (KeyValuePair<string, float> userVolumeOverride in UserVolumeOverrides)
						{
							text2 += $" - \"{userVolumeOverride.Key}\": {userVolumeOverride.Value * 100f:0}%\n";
						}
						ConsoleHelper.LogToConsole(ConsoleScript.instance, text2.TrimEnd(Array.Empty<char>()));
					}
				}
				else if (text == "clear")
				{
					UserVolumeOverrides.Clear();
					SaveOverrides();
					ConsoleHelper.LogToConsole(ConsoleScript.instance, "Cleared all custom sound overrides.");
				}
				else
				{
					if (args.Length < 3)
					{
						throw new Exception("Missing sound name argument.");
					}
					string text3 = args[2].ToLower();
					if (text == "remove" || text == "delete")
					{
						if (UserVolumeOverrides.ContainsKey(text3))
						{
							UserVolumeOverrides.Remove(text3);
							SaveOverrides();
							ConsoleHelper.LogToConsole(ConsoleScript.instance, "Removed override for \"" + text3 + "\".");
						}
						else
						{
							ConsoleHelper.LogToConsole(ConsoleScript.instance, "No override found for \"" + text3 + "\".");
						}
					}
					else
					{
						if (!(text == "change") && !(text == "set") && !(text == "add"))
						{
							throw new Exception("Unknown action '" + text + "'. Use change, list, remove, or clear.");
						}
						if (args.Length < 4)
						{
							throw new Exception("Missing volume argument. Usage: noise change [name] [volume]");
						}
						if (!float.TryParse(args[3], out var result))
						{
							throw new Exception("Invalid volume number.");
						}
						float num = ((result > 5f) ? (result / 100f) : result);
						num = Mathf.Clamp(num, 0f, 1.2f);
						if (UserVolumeOverrides.ContainsKey(text3))
						{
							UserVolumeOverrides[text3] = num;
						}
						else
						{
							UserVolumeOverrides.Add(text3, num);
						}
						SaveOverrides();
						ConsoleHelper.LogToConsole(ConsoleScript.instance, $"Set volume for \"{text3}\" to {num} ({num * 100f:0}%).");
					}
				}
			};
			_003C_003Ec._003C_003E9__19_1 = val2;
			obj2 = (object)val2;
		}
		commands.Add(new Command("noise", "Manage sound volume overrides.", (Action)obj2, dictionary, new(string, string)[3]
		{
			("string action", "change/list/remove/clear"),
			("string sound", "Name of the sound file"),
			("float volume", "New volume (0.0 - 1.2 or 0 - 120)")
		}));
	}

	[HarmonyPatch(typeof(Sound), "Play", new Type[]
	{
		typeof(AudioClip),
		typeof(Vector2),
		typeof(bool),
		typeof(bool),
		typeof(Transform),
		typeof(float),
		typeof(float),
		typeof(bool),
		typeof(bool)
	})]
	[HarmonyPrefix]
	private static void Sound_Play_Clip_Prefix(AudioClip clip, [HarmonyArgument(5)] ref float volume, [HarmonyArgument(6)] ref float pitch)
	{
		if (!((Object)(object)clip == (Object)null))
		{
			ApplyAudioSafety(((Object)clip).name, ref volume, ref pitch);
		}
	}

	private static void ApplyAudioSafety(string soundId, ref float volume, ref float pitch)
	{
		if (string.IsNullOrWhiteSpace(soundId))
		{
			if (pitch > 1.25f)
			{
				pitch = 1.25f;
			}
			return;
		}
		string text = soundId.ToLowerInvariant();
		if (TryGetOverrideMultiplier(text, out var volumeMultiplier))
		{
			volume *= Mathf.Max(0f, volumeMultiplier);
		}
		else
		{
			if (!QoLModuleManager.IsEnabled("qol_audio_safety"))
			{
				if (pitch > 1.25f)
				{
					pitch = 1.25f;
				}
				return;
			}
			if (text.Contains("tinnitus"))
			{
				volume *= 0.05f;
			}
			bool flag = false;
			foreach (string lOUDNoise in LOUDNoises)
			{
				if (text.Contains(lOUDNoise))
				{
					volume *= (FirearmNoises.Contains(lOUDNoise) ? 0.35f : 0.15f);
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				foreach (string somewhatLoudNoise in SomewhatLoudNoises)
				{
					if (text.Contains(somewhatLoudNoise))
					{
						volume *= 0.5f;
						break;
					}
				}
			}
		}
		if (pitch > 1.25f)
		{
			pitch = 1.25f;
		}
	}

	[HarmonyPatch(typeof(MusicManager), "Update")]
	[HarmonyPostfix]
	private static void MusicManager_Update_Postfix(MusicManager __instance, ref float ___maxDroneAmount, AudioSource ___source, List<AudioSource> ___droneSources)
	{
		if ((Object)(object)__instance == (Object)null || ___droneSources == null || __instance.droneTracks == null)
		{
			return;
		}
		int num = Mathf.Min(___droneSources.Count, __instance.droneTracks.Count);
		bool flag = false;
		for (int i = 0; i < num; i++)
		{
			DroneTrack val = __instance.droneTracks[i];
			AudioSource val2 = ___droneSources[i];
			if (!((Object)(object)val?.clip == (Object)null) && !((Object)(object)val2 == (Object)null))
			{
				float volumeMultiplierOrDefault = GetVolumeMultiplierOrDefault(((Object)val.clip).name);
				float num2 = Mathf.Clamp01(val2.volume * volumeMultiplierOrDefault);
				if (!Mathf.Approximately(num2, val2.volume))
				{
					val2.volume = num2;
					flag = true;
				}
			}
		}
		if ((Object)(object)PlayerCamera.main?.body == (Object)null)
		{
			return;
		}
		if (flag)
		{
			___maxDroneAmount = 0f;
			if (__instance.droneVolume > 0.01f)
			{
				foreach (AudioSource ___droneSource in ___droneSources)
				{
					if ((Object)(object)___droneSource != (Object)null)
					{
						___maxDroneAmount = Mathf.Max(___maxDroneAmount, ___droneSource.volume / __instance.droneVolume);
					}
				}
			}
			if ((Object)(object)WorldGeneration.world?.caveAudio != (Object)null)
			{
				float num3 = 0.07f;
				if (((Object)(object)___source == (Object)null || !___source.isPlaying) && ___maxDroneAmount < 0.5f)
				{
					num3 = 0.38f;
				}
				float num4 = (Object.op_Implicit((Object)(object)SettingsMenu.instance) ? Time.unscaledDeltaTime : Time.deltaTime);
				WorldGeneration.world.caveAudio.volume = Mathf.MoveTowards(WorldGeneration.world.caveAudio.volume, num3, num4 * 0.1f);
			}
		}
		if ((Object)(object)___source != (Object)null)
		{
			Body body = PlayerCamera.main.body;
			float num5 = 1f;
			if ((Object)(object)body.harmer != (Object)null && body.harmer.timeWasStill > 20f)
			{
				num5 = Mathf.Clamp01((30f - body.harmer.timeWasStill) * 0.1f);
			}
			float num6 = (((Object)(object)___source.clip != (Object)null) ? GetVolumeMultiplierOrDefault(((Object)___source.clip).name) : 1f);
			___source.volume = Mathf.Clamp01(__instance.origVolume * num5 * (1f - ___maxDroneAmount * 0.85f) * num6);
		}
	}
}
