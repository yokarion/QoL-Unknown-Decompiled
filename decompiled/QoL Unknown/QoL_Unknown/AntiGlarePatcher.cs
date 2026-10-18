using System.Collections.Generic;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace QoL_Unknown;

internal class AntiGlarePatcher
{
	private static ManualLogSource Logger;

	public static float MasterLightScale = 100f;

	public static float MaxLightIntensity = 1.1f;

	private const string EffectsPrefKey = "qol_video_effects";

	private const string AntiEpilepsyPrefKey = "qol_video_antiepilepsy";

	private static float EffectsIntensity = 1f;

	private static float ScreenEffectMultiplier = 1f;

	private static bool AntiEpilepsyEnabled;

	private const float JumpPadFlashIntensity = 0.1f;

	private const float BrainFlashMaxAlpha = 0.25f;

	private const float SoundCannonShaderMultiplier = 0.25f;

	private const float FullOpacityEpsilon = 0.0005f;

	private static readonly FieldInfo JumpPadLightField = AccessTools.Field(typeof(JumpPadScript), "light");

	private static readonly FieldInfo JumpPadCooldownField = AccessTools.Field(typeof(JumpPadScript), "cooldown");

	private static readonly FieldInfo SoundCannonSpentField = AccessTools.Field(typeof(SoundCannon), "spent");

	private static readonly int PainProperty = Shader.PropertyToID("_Pain");

	private static readonly int BloodAmountProperty = Shader.PropertyToID("_BloodAmount");

	private static readonly int LowOxyAmtProperty = Shader.PropertyToID("_LowOxyAmt");

	private static readonly int FrostAmountProperty = Shader.PropertyToID("_FrostAmount");

	private static readonly int OverheatAmountProperty = Shader.PropertyToID("_OverheatAmount");

	private static readonly int BadFocusAmountProperty = Shader.PropertyToID("_BadFocusAmount");

	private static readonly int InsanityFxProperty = Shader.PropertyToID("_InsanityFX");

	private static readonly int DirtynessProperty = Shader.PropertyToID("_Dirtyness");

	private static readonly int BlurIntensityProperty = Shader.PropertyToID("_BlurIntensity");

	private static readonly int HungerIntensityProperty = Shader.PropertyToID("_HungerIntensity");

	private static readonly int WaterAmountProperty = Shader.PropertyToID("_WaterAmount");

	private static readonly int DropletAmountProperty = Shader.PropertyToID("_DropletAmount");

	private static readonly int SicknessAmountProperty = Shader.PropertyToID("_SicknessAmount");

	private static readonly int InsanityProperty = Shader.PropertyToID("_Insanity");

	private static readonly int EyeDamageProperty = Shader.PropertyToID("_EyeDamage");

	private static readonly int BrainDamageProperty = Shader.PropertyToID("_BrainDamage");

	private static readonly int BloodlossProperty = Shader.PropertyToID("_Bloodloss");

	private static readonly int RadAmountProperty = Shader.PropertyToID("_RadAmount");

	private static readonly Dictionary<int, float> BaseIntensities = new Dictionary<int, float>();

	private static readonly HashSet<int> JumpPadLightIds = new HashSet<int>();

	private static readonly HashSet<int> RawNextLightSetIds = new HashSet<int>();

	private static readonly HashSet<int> ProcessedSoundCannons = new HashSet<int>();

	private static bool applyingStoredIntensity;

	private static float appliedLightScale = 100f;

	public static void Initialize(ManualLogSource logger)
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Expected O, but got Unknown
		Logger = logger;
		LoadSavedSettings();
		RecalculateFromSettings();
		Harmony val = new Harmony("org.bepinex.plugins.qol.antiglare");
		val.PatchAll(typeof(AntiGlarePatcher));
		SceneManager.sceneLoaded += delegate
		{
			JumpPadLightIds.Clear();
			RawNextLightSetIds.Clear();
			ProcessedSoundCannons.Clear();
			RefreshSettingsFromPrefs();
			SweepAllLights();
		};
	}

	private static void LoadSavedSettings()
	{
		EffectsIntensity = NormalizeSliderValue(PlayerPrefs.GetFloat("qol_video_effects", 1f));
		AntiEpilepsyEnabled = PlayerPrefs.GetInt("qol_video_antiepilepsy", 0) == 1;
	}

	private static void SaveSettings()
	{
		PlayerPrefs.SetFloat("qol_video_effects", EffectsIntensity);
		PlayerPrefs.SetInt("qol_video_antiepilepsy", AntiEpilepsyEnabled ? 1 : 0);
		PlayerPrefs.Save();
	}

	private static float NormalizeSliderValue(float raw)
	{
		if (raw > 1f)
		{
			raw *= 0.01f;
		}
		return Mathf.Clamp01(raw);
	}

	private static void RefreshSettingsFromPrefs()
	{
		if (PlayerPrefs.HasKey("qol_video_effects") || PlayerPrefs.HasKey("qol_video_antiepilepsy"))
		{
			float num = NormalizeSliderValue(PlayerPrefs.GetFloat("qol_video_effects", EffectsIntensity));
			bool flag = PlayerPrefs.GetInt("qol_video_antiepilepsy", AntiEpilepsyEnabled ? 1 : 0) == 1;
			if (!Mathf.Approximately(num, EffectsIntensity) || flag != AntiEpilepsyEnabled)
			{
				EffectsIntensity = num;
				AntiEpilepsyEnabled = flag;
				RecalculateFromSettings();
			}
		}
	}

	private static void RecalculateFromSettings()
	{
		ScreenEffectMultiplier = Mathf.Clamp01(EffectsIntensity);
	}

	public static float GetScreenEffectsIntensity()
	{
		return EffectsIntensity;
	}

	public static void SetScreenEffectsIntensity(float normalizedValue, bool persist = true)
	{
		EffectsIntensity = NormalizeSliderValue(normalizedValue);
		RecalculateFromSettings();
		if (persist)
		{
			PlayerPrefs.SetFloat("qol_video_effects", EffectsIntensity);
			PlayerPrefs.Save();
		}
	}

	public static bool GetAntiEpilepsyEnabled()
	{
		return AntiEpilepsyEnabled;
	}

	public static void SetAntiEpilepsyEnabled(bool enabled, bool persist = true)
	{
		AntiEpilepsyEnabled = enabled;
		RecalculateFromSettings();
		RegisterJumpPadLights();
		if (persist)
		{
			PlayerPrefs.SetInt("qol_video_antiepilepsy", AntiEpilepsyEnabled ? 1 : 0);
			PlayerPrefs.Save();
		}
	}

	private static float GetScreenFxOpacity()
	{
		return Mathf.Clamp01(ScreenEffectMultiplier);
	}

	private static void ApplyPostProcessingScale()
	{
	}

	[HarmonyPatch(typeof(PlayerCamera), "LateUpdate")]
	[HarmonyPostfix]
	private static void PlayerCamera_LateUpdate_Postfix()
	{
		ApplyPostProcessingScale();
		ApplyBrainFlashOpacity(PlayerCamera.main);
	}

	[HarmonyPatch(typeof(PlayerCamera), "HandleScreenShaders")]
	[HarmonyPostfix]
	private static void PlayerCamera_HandleScreenShaders_Postfix(PlayerCamera __instance)
	{
		ApplyPlayerCameraShaderScale(__instance);
	}

	private static void ApplyPlayerCameraShaderScale(PlayerCamera cam)
	{
		if (!((Object)(object)cam == (Object)null))
		{
			float screenFxOpacity = GetScreenFxOpacity();
			if (!(screenFxOpacity >= 0.9995f))
			{
				BlendFloatToNeutralIfExists(cam.mainScreenPass, PainProperty, 0f, screenFxOpacity);
				BlendFloatToNeutralIfExists(cam.mainScreenPass, BloodAmountProperty, 1f, screenFxOpacity);
				BlendFloatToNeutralIfExists(cam.mainScreenPass, LowOxyAmtProperty, 0f, screenFxOpacity);
				BlendFloatToNeutralIfExists(cam.mainScreenPass, FrostAmountProperty, 0f, screenFxOpacity);
				BlendFloatToNeutralIfExists(cam.mainScreenPass, OverheatAmountProperty, 0f, screenFxOpacity);
				BlendFloatToNeutralIfExists(cam.mainScreenPass, BadFocusAmountProperty, 0f, screenFxOpacity);
				BlendFloatToNeutralIfExists(cam.mainScreenPass, InsanityFxProperty, 0f, screenFxOpacity);
				BlendFloatToNeutralIfExists(cam.mainScreenPass, DirtynessProperty, 0f, screenFxOpacity);
				BlendFloatToNeutralIfExists(cam.blurPass, BlurIntensityProperty, 0f, screenFxOpacity);
				BlendFloatToNeutralIfExists(cam.blurPass, HungerIntensityProperty, 0f, screenFxOpacity);
				BlendFloatToNeutralIfExists(cam.blurPass, WaterAmountProperty, 0f, screenFxOpacity);
				BlendFloatToNeutralIfExists(cam.blurPass, DropletAmountProperty, 0f, screenFxOpacity);
				BlendFloatToNeutralIfExists(cam.blurPass, SicknessAmountProperty, 0f, screenFxOpacity);
				BlendFloatToNeutralIfExists(cam.blurPass, InsanityProperty, 0f, screenFxOpacity);
				BlendFloatToNeutralIfExists(cam.damagePass, EyeDamageProperty, 0f, screenFxOpacity);
				BlendFloatToNeutralIfExists(cam.damagePass, BrainDamageProperty, 0f, screenFxOpacity);
				BlendFloatToNeutralIfExists(cam.damagePass, BloodlossProperty, 0f, screenFxOpacity);
				BlendFloatToNeutralIfExists(cam.damagePass, RadAmountProperty, 0f, screenFxOpacity);
			}
		}
	}

	private static void BlendFloatToNeutralIfExists(Material mat, int property, float neutral, float opacity)
	{
		if (!((Object)(object)mat == (Object)null) && mat.HasProperty(property))
		{
			float num = mat.GetFloat(property);
			mat.SetFloat(property, neutral + (num - neutral) * opacity);
		}
	}

	private static void ApplyBrainFlashOpacity(PlayerCamera cam)
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		if (AntiEpilepsyEnabled && !((Object)(object)cam == (Object)null) && !((Object)(object)cam.brainFlash == (Object)null))
		{
			Color color = ((Graphic)cam.brainFlash).color;
			if (color.a > 0.25f)
			{
				color.a = 0.25f;
				((Graphic)cam.brainFlash).color = color;
			}
		}
	}

	[HarmonyPatch(/*Could not decode attribute arguments.*/)]
	[HarmonyPrefix]
	private static void Intensity_Setter_Prefix(Light2D __instance, ref float value)
	{
		int instanceID = ((Object)__instance).GetInstanceID();
		bool flag = JumpPadLightIds.Contains(instanceID);
		if (!applyingStoredIntensity)
		{
			if (flag)
			{
				RawNextLightSetIds.Remove(instanceID);
			}
			else
			{
				float num = (RawNextLightSetIds.Remove(instanceID) ? value : GetUnscaledLightValue(instanceID, value));
				BaseIntensities[instanceID] = num;
				value = num;
			}
		}
		if (AntiEpilepsyEnabled && flag)
		{
			value = Mathf.Min(value, 0.1f);
		}
		if (!flag)
		{
			value *= MasterLightScale * 0.01f;
			if (!(value > MaxLightIntensity))
			{
			}
		}
	}

	private static float GetUnscaledLightValue(int id, float value)
	{
		if (Mathf.Approximately(MasterLightScale, 100f) || !BaseIntensities.ContainsKey(id))
		{
			return value;
		}
		float num = MasterLightScale * 0.01f;
		if (Mathf.Approximately(num, 0f))
		{
			return 0f;
		}
		float num2 = value / num;
		return (value >= 0f && num2 <= MaxLightIntensity + 0.0001f) ? num2 : value;
	}

	public static void SweepAllLights()
	{
		Light2D[] array = Object.FindObjectsOfType<Light2D>();
		float num = appliedLightScale * 0.01f;
		applyingStoredIntensity = true;
		try
		{
			Light2D[] array2 = array;
			foreach (Light2D val in array2)
			{
				int instanceID = ((Object)val).GetInstanceID();
				if (!JumpPadLightIds.Contains(instanceID))
				{
					float num2;
					if (BaseIntensities.ContainsKey(instanceID))
					{
						num2 = BaseIntensities[instanceID];
					}
					else
					{
						num2 = (Mathf.Approximately(num, 0f) ? val.intensity : (val.intensity / num));
						BaseIntensities[instanceID] = num2;
					}
					val.intensity = num2;
				}
			}
			appliedLightScale = MasterLightScale;
		}
		finally
		{
			applyingStoredIntensity = false;
		}
	}

	[HarmonyPatch(typeof(JumpPadScript), "OnCollisionEnter2D")]
	[HarmonyPrefix]
	private static void JumpPadScript_OnCollisionEnter2D_Prefix(JumpPadScript __instance, Collision2D collision)
	{
		if (WillJumpPadSetFlashLight(__instance, collision))
		{
			RegisterRawNextJumpPadLightSet(__instance);
		}
		if (AntiEpilepsyEnabled && !((Object)(object)__instance == (Object)null))
		{
			ClampJumpPadLight(__instance);
		}
	}

	[HarmonyPatch(typeof(JumpPadScript), "Start")]
	[HarmonyPostfix]
	private static void JumpPadScript_Start_Postfix(JumpPadScript __instance)
	{
		RegisterJumpPadLight(__instance);
	}

	[HarmonyPatch(typeof(JumpPadScript), "OnCollisionEnter2D")]
	[HarmonyPostfix]
	private static void JumpPadScript_OnCollisionEnter2D_Postfix(JumpPadScript __instance)
	{
		if (AntiEpilepsyEnabled && !((Object)(object)__instance == (Object)null))
		{
			ClampJumpPadLight(__instance);
		}
	}

	[HarmonyPatch(typeof(WorldGeneration), "FinishWorldGeneration")]
	[HarmonyPostfix]
	private static void WorldGeneration_FinishWorldGeneration_Postfix()
	{
		RegisterJumpPadLights();
	}

	[HarmonyPatch(typeof(SoundCannon), "Update")]
	[HarmonyPostfix]
	private static void SoundCannon_Update_Postfix(SoundCannon __instance)
	{
		if (!AntiEpilepsyEnabled || (Object)(object)__instance == (Object)null)
		{
			return;
		}
		int instanceID = ((Object)__instance).GetInstanceID();
		if (ProcessedSoundCannons.Contains(instanceID))
		{
			return;
		}
		bool flag = default(bool);
		int num;
		if (SoundCannonSpentField != null)
		{
			object value = SoundCannonSpentField.GetValue(__instance);
			if (value is bool)
			{
				flag = (bool)value;
				num = 1;
			}
			else
			{
				num = 0;
			}
		}
		else
		{
			num = 0;
		}
		if (((uint)num & (flag ? 1u : 0u)) != 0)
		{
			ProcessedSoundCannons.Add(instanceID);
			PlayerCamera main = PlayerCamera.main;
			if (!((Object)(object)main == (Object)null))
			{
				main.bonusAbber *= 0.25f;
			}
		}
	}

	private static void RegisterJumpPadLights()
	{
		JumpPadScript[] array = Object.FindObjectsOfType<JumpPadScript>();
		foreach (JumpPadScript val in array)
		{
			if (!((Object)(object)val == (Object)null))
			{
				RegisterJumpPadLight(val);
			}
		}
	}

	private static void RegisterJumpPadLight(JumpPadScript jumpPad)
	{
		if ((Object)(object)jumpPad == (Object)null)
		{
			return;
		}
		object obj;
		if (!(JumpPadLightField != null))
		{
			obj = null;
		}
		else
		{
			object? value = JumpPadLightField.GetValue(jumpPad);
			obj = ((value is Light2D) ? value : null);
		}
		Light2D val = (Light2D)obj;
		if ((Object)(object)val == (Object)null)
		{
			val = ((Component)jumpPad).GetComponent<Light2D>();
		}
		if (!((Object)(object)val == (Object)null))
		{
			JumpPadLightIds.Add(((Object)val).GetInstanceID());
			if (AntiEpilepsyEnabled)
			{
				val.intensity = Mathf.Min(val.intensity, 0.1f);
			}
		}
	}

	private static void ClampJumpPadLight(JumpPadScript jumpPad)
	{
		if (!((Object)(object)jumpPad == (Object)null))
		{
			object obj;
			if (!(JumpPadLightField != null))
			{
				obj = null;
			}
			else
			{
				object? value = JumpPadLightField.GetValue(jumpPad);
				obj = ((value is Light2D) ? value : null);
			}
			Light2D val = (Light2D)obj;
			if ((Object)(object)val == (Object)null)
			{
				val = ((Component)jumpPad).GetComponent<Light2D>();
			}
			if (!((Object)(object)val == (Object)null))
			{
				JumpPadLightIds.Add(((Object)val).GetInstanceID());
				val.intensity = Mathf.Min(val.intensity, 0.1f);
			}
		}
	}

	private static void RegisterRawNextJumpPadLightSet(JumpPadScript jumpPad)
	{
		if (!((Object)(object)jumpPad == (Object)null))
		{
			object obj;
			if (!(JumpPadLightField != null))
			{
				obj = null;
			}
			else
			{
				object? value = JumpPadLightField.GetValue(jumpPad);
				obj = ((value is Light2D) ? value : null);
			}
			Light2D val = (Light2D)obj;
			if ((Object)(object)val == (Object)null)
			{
				val = ((Component)jumpPad).GetComponent<Light2D>();
			}
			if (!((Object)(object)val == (Object)null))
			{
				int instanceID = ((Object)val).GetInstanceID();
				JumpPadLightIds.Add(instanceID);
				RawNextLightSetIds.Add(instanceID);
			}
		}
	}

	private static bool WillJumpPadSetFlashLight(JumpPadScript jumpPad, Collision2D collision)
	{
		if ((Object)(object)jumpPad == (Object)null || collision == null || jumpPad.disabled || (Object)(object)collision.rigidbody == (Object)null || collision.rigidbody.isKinematic)
		{
			return false;
		}
		if (JumpPadCooldownField != null && JumpPadCooldownField.GetValue(jumpPad) is float num && num > 0f)
		{
			return false;
		}
		return (Object)(object)collision.gameObject.GetComponent<Body>() == (Object)null;
	}

	[HarmonyPatch(typeof(ConsoleScript), "RegisterAllCommands")]
	[HarmonyPostfix]
	private static void RegisterCommands_Postfix()
	{
	}
}
