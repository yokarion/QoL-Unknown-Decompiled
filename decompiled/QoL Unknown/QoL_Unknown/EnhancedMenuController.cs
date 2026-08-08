using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using BepInEx.Logging;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace QoL_Unknown;

internal class EnhancedMenuController : MonoBehaviour
{
	private sealed class FpsCounterTicker : MonoBehaviour
	{
		private const float UpdateInterval = 0.5f;

		private float timer;

		private int frameCount;

		private void Update()
		{
			//IL_0097: Unknown result type (might be due to invalid IL or missing references)
			//IL_0090: Unknown result type (might be due to invalid IL or missing references)
			//IL_0089: Unknown result type (might be due to invalid IL or missing references)
			if (!((Object)(object)fpsCounterText == (Object)null))
			{
				frameCount++;
				timer += Time.unscaledDeltaTime;
				if (!(timer < 0.5f))
				{
					float num = (float)frameCount / timer;
					((TMP_Text)fpsCounterText).text = $"FPS: {num:F0}";
					((Graphic)fpsCounterText).color = ((num >= 50f) ? Color.green : ((num >= 29f) ? Color.yellow : Color.red));
					frameCount = 0;
					timer = 0f;
				}
			}
		}
	}

	public static EnhancedMenuController Instance;

	private const string UiScalePrefKey = "UIScale";

	private const float UiScaleDefault = 1f;

	private const float UiScaleMin = 0.75f;

	private const float UiScaleMax = 1.5f;

	private const int UiScaleSnapMinPercent = 99;

	private const int UiScaleSnapMaxPercent = 101;

	private static readonly Dictionary<int, Vector2> baseReferenceResolutions = new Dictionary<int, Vector2>();

	private static readonly Dictionary<int, float> baseScaleFactors = new Dictionary<int, float>();

	private static readonly Dictionary<int, Vector3> baseCanvasScales = new Dictionary<int, Vector3>();

	private static readonly List<CanvasScaler> cachedCanvasScalers = new List<CanvasScaler>(32);

	private const float CanvasScalerCacheRefreshInterval = 2f;

	private static float nextCanvasScalerRefreshTime = -1f;

	private static bool hasAppliedCustomUiScale;

	private static GameObject fpsCounterObject;

	private static TextMeshProUGUI fpsCounterText;

	private static bool pauseOnUnfocusEnabled;

	private static bool wasApplicationFocused = true;

	private static bool pendingUnfocusPause;

	private static int currentProcessId = -1;

	[DllImport("user32.dll")]
	private static extern IntPtr GetForegroundWindow();

	[DllImport("user32.dll")]
	private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

	public static void Initialize(ManualLogSource logger)
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Expected O, but got Unknown
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Expected O, but got Unknown
		if (!((Object)(object)Instance != (Object)null))
		{
			GameObject val = new GameObject("EnhancedSettingsController");
			Object.DontDestroyOnLoad((Object)(object)val);
			Instance = val.AddComponent<EnhancedMenuController>();
			Application.focusChanged += OnApplicationFocusChanged;
			Harmony val2 = new Harmony("org.bepinex.plugins.qol.enhancedsettings");
			val2.PatchAll(typeof(EnhancedMenuController));
			if (logger != null)
			{
				logger.LogInfo((object)"EnhancedMenuController initialized");
			}
		}
	}

	internal static float NormalizeUiScale(float uiScale)
	{
		float num = Mathf.Clamp(uiScale, 0.75f, 1.5f);
		int num2 = Mathf.RoundToInt(num * 100f);
		if (num2 >= 99 && num2 <= 101)
		{
			return 1f;
		}
		return num;
	}

	internal static float GetSavedUiScale()
	{
		return NormalizeUiScale(PlayerPrefs.GetFloat("UIScale", 1f));
	}

	public static void ApplySavedUiScale(float uiScale)
	{
		ApplyUIScale(uiScale, forceRefresh: true);
	}

	internal static bool IsCustomUiScaleActive()
	{
		return GetSavedUiScale() != 1f;
	}

	internal static bool NeedsScreenSpaceUiCompatibility()
	{
		return IsCustomUiScaleActive();
	}

	public static void ApplyAllSettings()
	{
		SettingInt val = Settings.Get<SettingInt>("framerate");
		if (val != null)
		{
			((Setting)val).Apply();
			if ((Object)(object)WorldGeneration.world == (Object)null)
			{
				PreGenPatcher.ApplyMenuFrameRateLimit();
			}
		}
		float num = Settings.Get<SettingFloat>("qol.video.uiscale")?.value ?? GetSavedUiScale();
		PlayerPrefs.SetFloat("UIScale", num);
		ApplyUIScale(num, forceRefresh: true);
		SettingBool val2 = Settings.Get<SettingBool>("vsync");
		if (val2 != null)
		{
			((Setting)val2).Apply();
		}
		SettingBool val3 = Settings.Get<SettingBool>("pixelate");
		if (val3 != null)
		{
			((Setting)val3).Apply();
		}
		SettingFloat val4 = Settings.Get<SettingFloat>("brightness");
		if (val4 != null)
		{
			((Setting)val4).Apply();
		}
		SettingFloat val5 = Settings.Get<SettingFloat>("screenshake");
		if (val5 != null)
		{
			((Setting)val5).Apply();
		}
		SettingInt val6 = Settings.Get<SettingInt>("lineofsightcount");
		if (val6 != null)
		{
			((Setting)val6).Apply();
		}
		float num2 = Settings.Get<SettingFloat>("qol.video.intensity")?.value ?? PlayerPrefs.GetFloat("LightIntensity", 100f);
		AntiGlarePatcher.MasterLightScale = Mathf.Clamp(num2, 0f, 200f);
		PlayerPrefs.SetFloat("LightIntensity", AntiGlarePatcher.MasterLightScale);
		AntiGlarePatcher.SweepAllLights();
		SettingFloat val7 = Settings.Get<SettingFloat>("qol.video.effects");
		if (val7 != null)
		{
			AntiGlarePatcher.SetScreenEffectsIntensity(val7.value);
		}
		SettingBool val8 = Settings.Get<SettingBool>("qol.video.antiepilepsy");
		if (val8 != null)
		{
			AntiGlarePatcher.SetAntiEpilepsyEnabled(val8.value);
		}
		SetPauseOnUnfocusEnabled(Settings.Get<SettingBool>("qol.game.pauseonunfocus")?.value ?? false);
		MirrorBoolSettingToPlayerPrefs("qol.game.v6ilmenite", "UseV6IlmeniteWorldgen");
		MirrorBoolSettingToPlayerPrefs("qol.game.skipexitanimation", "Setting_SkipExitAnimation");
		MirrorBoolSettingToPlayerPrefs("qol.game.minigameqol", "Setting_Minigame_QoL");
		MirrorBoolSettingToPlayerPrefs("qol.game.skillallocation", "Setting_SkillPointAllocation");
		MirrorIntSettingToPlayerPrefs("qol.game.skillallocation.str", "StartSTR");
		MirrorIntSettingToPlayerPrefs("qol.game.skillallocation.res", "StartRES");
		MirrorIntSettingToPlayerPrefs("qol.game.skillallocation.int", "StartINT");
		if ((Object)(object)WorldGeneration.world != (Object)null)
		{
			SettingBool val9 = Settings.Get<SettingBool>("qol.game.disablelineofsight");
			if (val9 != null)
			{
				LineOfSightPatcher.SetLineOfSightDisabled(val9.value);
			}
			SettingBool val10 = Settings.Get<SettingBool>("qol.game.disableradline");
			if (val10 != null)
			{
				RadlinePatcher.SetDisabled(val10.value);
			}
			RadlinePatcher.ApplyCurrentState();
		}
		bool flag = Settings.Get<SettingBool>("qol.video.fpscounter")?.value ?? (PlayerPrefs.GetInt("ShowFPSCounter", 0) == 1);
		PlayerPrefs.SetInt("ShowFPSCounter", flag ? 1 : 0);
		SetFpsCounterEnabledStatic(flag);
	}

	private static void MirrorBoolSettingToPlayerPrefs(string settingId, string prefKey)
	{
		SettingBool val = Settings.Get<SettingBool>(settingId);
		if (val != null)
		{
			PlayerPrefs.SetInt(prefKey, val.value ? 1 : 0);
		}
	}

	private static void MirrorIntSettingToPlayerPrefs(string settingId, string prefKey)
	{
		SettingInt val = Settings.Get<SettingInt>(settingId);
		if (val != null)
		{
			PlayerPrefs.SetInt(prefKey, val.value);
		}
	}

	public static void SetPauseOnUnfocusEnabled(bool enabled)
	{
		pauseOnUnfocusEnabled = enabled;
		bool flag = IsApplicationFocused();
		pendingUnfocusPause = enabled && !flag;
		wasApplicationFocused = flag;
	}

	private void Update()
	{
		SyncPauseOnUnfocusSetting();
		HandleApplicationFocusPolling();
	}

	private void OnApplicationFocus(bool hasFocus)
	{
		wasApplicationFocused = hasFocus;
		if (!hasFocus)
		{
			RequestPauseForApplicationUnfocus();
		}
	}

	private void OnApplicationPause(bool pauseStatus)
	{
		if (pauseStatus)
		{
			RequestPauseForApplicationUnfocus();
		}
	}

	private static void OnApplicationFocusChanged(bool hasFocus)
	{
		wasApplicationFocused = hasFocus;
		if (!hasFocus)
		{
			RequestPauseForApplicationUnfocus();
		}
	}

	private static void SyncPauseOnUnfocusSetting()
	{
		SettingBool val = Settings.Get<SettingBool>("qol.game.pauseonunfocus");
		if (val != null && pauseOnUnfocusEnabled != val.value)
		{
			SetPauseOnUnfocusEnabled(val.value);
		}
	}

	private static void HandleApplicationFocusPolling()
	{
		bool flag = IsApplicationFocused();
		if (wasApplicationFocused && !flag)
		{
			RequestPauseForApplicationUnfocus();
		}
		wasApplicationFocused = flag;
		if (flag)
		{
			pendingUnfocusPause = false;
		}
		if (pendingUnfocusPause)
		{
			PauseForApplicationUnfocus();
		}
	}

	private static bool IsApplicationFocused()
	{
		try
		{
			if (currentProcessId < 0)
			{
				currentProcessId = Process.GetCurrentProcess().Id;
			}
			IntPtr foregroundWindow = GetForegroundWindow();
			if (foregroundWindow == IntPtr.Zero)
			{
				return Application.isFocused;
			}
			GetWindowThreadProcessId(foregroundWindow, out var processId);
			return processId == currentProcessId;
		}
		catch (Exception)
		{
			return Application.isFocused;
		}
	}

	private static void RequestPauseForApplicationUnfocus()
	{
		if (pauseOnUnfocusEnabled)
		{
			pendingUnfocusPause = true;
			PauseForApplicationUnfocus();
		}
	}

	private static void PauseForApplicationUnfocus()
	{
		if (pauseOnUnfocusEnabled && !((Object)(object)PauseHandler.main == (Object)null) && !PauseHandler.paused && !((Object)(object)PlayerCamera.main == (Object)null) && !((Object)(object)WorldGeneration.world == (Object)null))
		{
			PauseHandler.main.isPaused = true;
			if (PauseHandler.paused)
			{
				pendingUnfocusPause = false;
			}
		}
	}

	[HarmonyPatch(typeof(PlayerCamera), "Update")]
	[HarmonyPrefix]
	private static void PlayerCamera_Update_Prefix()
	{
		SettingBool val = Settings.Get<SettingBool>("qol.game.pauseonunfocus");
		if (val != null && val.value && !PauseHandler.paused && !IsApplicationFocused())
		{
			pauseOnUnfocusEnabled = true;
			RequestPauseForApplicationUnfocus();
		}
	}

	public static void SetFpsCounterEnabledStatic(bool enabled)
	{
		if (enabled)
		{
			CreateFpsCounter();
		}
		else
		{
			DestroyFpsCounter();
		}
	}

	public void OpenMenu()
	{
	}

	private static void RefreshCanvasScalerCache(bool forceRefresh = false)
	{
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		float unscaledTime = Time.unscaledTime;
		if (!forceRefresh && unscaledTime < nextCanvasScalerRefreshTime)
		{
			return;
		}
		nextCanvasScalerRefreshTime = unscaledTime + 2f;
		cachedCanvasScalers.Clear();
		CanvasScaler[] array = Resources.FindObjectsOfTypeAll<CanvasScaler>();
		foreach (CanvasScaler val in array)
		{
			if (!((Object)(object)val == (Object)null) && !((Object)(object)((Component)val).gameObject == (Object)null))
			{
				Scene scene = ((Component)val).gameObject.scene;
				if (((Scene)(ref scene)).IsValid())
				{
					cachedCanvasScalers.Add(val);
				}
			}
		}
	}

	private static void ApplyUIScale(float uiScale, bool forceRefresh = false)
	{
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Invalid comparison between Unknown and I4
		//IL_022c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0231: Unknown result type (might be due to invalid IL or missing references)
		//IL_023a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_024a: Unknown result type (might be due to invalid IL or missing references)
		//IL_024d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0246: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		//IL_0252: Unknown result type (might be due to invalid IL or missing references)
		//IL_025a: Unknown result type (might be due to invalid IL or missing references)
		//IL_025f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0261: Unknown result type (might be due to invalid IL or missing references)
		//IL_0266: Unknown result type (might be due to invalid IL or missing references)
		//IL_0283: Unknown result type (might be due to invalid IL or missing references)
		//IL_0125: Unknown result type (might be due to invalid IL or missing references)
		//IL_0128: Unknown result type (might be due to invalid IL or missing references)
		//IL_0121: Unknown result type (might be due to invalid IL or missing references)
		//IL_012d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0131: Unknown result type (might be due to invalid IL or missing references)
		//IL_0136: Unknown result type (might be due to invalid IL or missing references)
		//IL_0138: Unknown result type (might be due to invalid IL or missing references)
		//IL_013d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0156: Unknown result type (might be due to invalid IL or missing references)
		float num = NormalizeUiScale(uiScale);
		bool flag = Mathf.Abs(num - 1f) > 0.0005f;
		if (!flag && !hasAppliedCustomUiScale)
		{
			return;
		}
		RefreshCanvasScalerCache(forceRefresh);
		for (int num2 = cachedCanvasScalers.Count - 1; num2 >= 0; num2--)
		{
			CanvasScaler val = cachedCanvasScalers[num2];
			if (!((Object)(object)val == (Object)null) && !((Object)(object)((Component)val).gameObject == (Object)null))
			{
				Scene scene = ((Component)val).gameObject.scene;
				if (((Scene)(ref scene)).IsValid())
				{
					int instanceID = ((Object)val).GetInstanceID();
					if ((int)val.uiScaleMode == 1)
					{
						if (!baseReferenceResolutions.TryGetValue(instanceID, out var value))
						{
							value = val.referenceResolution;
							baseReferenceResolutions[instanceID] = value;
						}
						if (value.x > 0f && value.y > 0f)
						{
							Vector2 val2 = (flag ? (value / num) : value);
							Vector2 val3 = val.referenceResolution - val2;
							if (((Vector2)(ref val3)).sqrMagnitude > 0.0001f)
							{
								val.referenceResolution = val2;
							}
						}
					}
					else
					{
						if (!baseScaleFactors.TryGetValue(instanceID, out var value2))
						{
							value2 = val.scaleFactor;
							baseScaleFactors[instanceID] = value2;
						}
						float num3 = (flag ? (value2 * num) : value2);
						if (Mathf.Abs(val.scaleFactor - num3) > 0.0001f)
						{
							val.scaleFactor = num3;
						}
					}
					continue;
				}
			}
			cachedCanvasScalers.RemoveAt(num2);
		}
		Canvas val4 = PlayerCamera.main?.mainCanvas;
		if ((Object)(object)val4 != (Object)null)
		{
			int instanceID2 = ((Object)val4).GetInstanceID();
			if (!baseCanvasScales.TryGetValue(instanceID2, out var value3))
			{
				value3 = ((Component)val4).transform.localScale;
				baseCanvasScales[instanceID2] = value3;
			}
			Vector3 val5 = (flag ? (value3 * num) : value3);
			Vector3 val6 = ((Component)val4).transform.localScale - val5;
			if (((Vector3)(ref val6)).sqrMagnitude > 1E-06f)
			{
				((Component)val4).transform.localScale = val5;
			}
		}
		hasAppliedCustomUiScale = flag;
	}

	private static void CreateFpsCounter()
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Expected O, but got Unknown
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Expected O, but got Unknown
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		//IL_011c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0167: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a8: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)fpsCounterObject != (Object)null))
		{
			fpsCounterObject = new GameObject("QoL_FPSCounter");
			Object.DontDestroyOnLoad((Object)(object)fpsCounterObject);
			Canvas val = fpsCounterObject.AddComponent<Canvas>();
			val.renderMode = (RenderMode)0;
			val.sortingOrder = 10000;
			CanvasScaler val2 = fpsCounterObject.AddComponent<CanvasScaler>();
			val2.uiScaleMode = (ScaleMode)1;
			val2.referenceResolution = new Vector2(1920f, 1080f);
			fpsCounterObject.AddComponent<GraphicRaycaster>();
			fpsCounterObject.AddComponent<FpsCounterTicker>();
			GameObject val3 = new GameObject("FPSText");
			val3.transform.SetParent(fpsCounterObject.transform, false);
			RectTransform val4 = val3.AddComponent<RectTransform>();
			val4.anchorMin = new Vector2(1f, 1f);
			val4.anchorMax = new Vector2(1f, 1f);
			val4.pivot = new Vector2(1f, 1f);
			val4.anchoredPosition = new Vector2(-18f, -10f);
			val4.sizeDelta = new Vector2(150f, 30f);
			fpsCounterText = val3.AddComponent<TextMeshProUGUI>();
			((TMP_Text)fpsCounterText).text = "FPS: --";
			((TMP_Text)fpsCounterText).fontSize = 18f;
			((TMP_Text)fpsCounterText).alignment = (TextAlignmentOptions)260;
			((Graphic)fpsCounterText).color = Color.white;
			((Graphic)fpsCounterText).raycastTarget = false;
			((TMP_Text)fpsCounterText).font = FontPatcher.GetRetroGamingFont();
			((TMP_Text)fpsCounterText).outlineWidth = 0.2f;
			((TMP_Text)fpsCounterText).outlineColor = Color32.op_Implicit(Color.black);
			ApplyUIScale(Settings.Get<SettingFloat>("qol.video.uiscale")?.value ?? GetSavedUiScale(), forceRefresh: true);
		}
	}

	private static void DestroyFpsCounter()
	{
		if ((Object)(object)fpsCounterObject != (Object)null)
		{
			Object.Destroy((Object)(object)fpsCounterObject);
			fpsCounterObject = null;
			fpsCounterText = null;
		}
	}

	[HarmonyPatch(typeof(WorldGeneration), "FinishWorldGeneration")]
	[HarmonyPostfix]
	private static void FinishWorldGeneration_Postfix()
	{
		ApplyAllSettings();
	}

	[HarmonyPatch(typeof(WorldGeneration), "Start")]
	[HarmonyPostfix]
	private static void WorldGeneration_Start_Postfix()
	{
		ApplyAllSettings();
	}
}
