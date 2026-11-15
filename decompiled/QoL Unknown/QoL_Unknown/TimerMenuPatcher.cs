using System;
using System.Diagnostics;
using BepInEx.Logging;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace QoL_Unknown;

internal class TimerMenuPatcher
{
	private static ManualLogSource Logger;

	private static GameObject pausePanelRef;

	private static GameObject timerPanelObj;

	private static TextMeshProUGUI igtText;

	private static TextMeshProUGUI rtaText;

	private static PauseHandler pauseHandlerRef;

	private const float TimerPauseMenuGap = 24f;

	private const float TimerPanelWidth = 440f;

	private const float TimerPanelHeight = 44f;

	private static float inGameTime = 0f;

	private static Stopwatch rtaStopwatch = new Stopwatch();

	private static bool timersStarted = false;

	private static TimeSpan _rtaStartOffset = TimeSpan.Zero;

	public static void Initialize(ManualLogSource logger)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		Logger = logger;
		new Harmony("org.bepinex.plugins.qol.timerpanel").PatchAll(typeof(TimerMenuPatcher));
		SceneManager.sceneUnloaded += delegate
		{
			pausePanelRef = null;
			timerPanelObj = null;
			igtText = null;
			rtaText = null;
			pauseHandlerRef = null;
		};
	}

	[HarmonyPatch(typeof(WorldGeneration), "Start")]
	[HarmonyPostfix]
	private static void WorldGeneration_Start_Postfix()
	{
		if (SaveSystem.loadedRun)
		{
			timersStarted = true;
			return;
		}
		inGameTime = 0f;
		_rtaStartOffset = TimeSpan.Zero;
		rtaStopwatch.Reset();
		rtaStopwatch.Start();
		timersStarted = true;
	}

	[HarmonyPatch(typeof(PlayerCamera), "Update")]
	[HarmonyPostfix]
	private static void PlayerCamera_Update_Postfix(PlayerCamera __instance)
	{
		if (timersStarted && Time.timeScale > 0f)
		{
			inGameTime += Time.deltaTime;
		}
		if ((Object)(object)timerPanelObj != (Object)null && timerPanelObj.activeSelf)
		{
			UpdateTimerDisplay();
		}
	}

	[HarmonyPatch(typeof(PlayerCamera), "Start")]
	[HarmonyPostfix]
	private static void PlayerCamera_Start_Postfix(PlayerCamera __instance)
	{
		Transform val = FindPauseMenu(__instance);
		if ((Object)(object)val == (Object)null)
		{
			ManualLogSource logger = Logger;
			if (logger != null)
			{
				logger.LogWarning((object)"[TimerMenuPatcher] PauseMenu not found; skipping timer menu creation.");
			}
		}
		else
		{
			pausePanelRef = ((Component)val).gameObject;
			pauseHandlerRef = pausePanelRef.GetComponent<PauseHandler>() ?? PauseHandler.main;
			BuildTimerUI();
		}
	}

	[HarmonyPatch(typeof(PlayerCamera), "LateUpdate")]
	[HarmonyPostfix]
	private static void PlayerCamera_LateUpdate_Postfix()
	{
		if ((Object)(object)pausePanelRef == (Object)null || (Object)(object)timerPanelObj == (Object)null)
		{
			return;
		}
		PauseHandler val = (((Object)(object)pauseHandlerRef != (Object)null) ? pauseHandlerRef : PauseHandler.main);
		bool flag = (((Object)(object)val != (Object)null) ? (val.pauseProgress > 0f) : pausePanelRef.activeInHierarchy);
		if (timerPanelObj.activeSelf != flag)
		{
			timerPanelObj.SetActive(flag);
			if (flag)
			{
				UpdateTimerDisplay();
			}
		}
		if (flag)
		{
			UpdateTimerPanelTransform(val);
		}
	}

	private static void BuildTimerUI()
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Expected O, but got Unknown
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0114: Unknown result type (might be due to invalid IL or missing references)
		//IL_0136: Unknown result type (might be due to invalid IL or missing references)
		//IL_0140: Expected O, but got Unknown
		if (!((Object)(object)timerPanelObj != (Object)null) && !((Object)(object)pausePanelRef == (Object)null))
		{
			timerPanelObj = new GameObject("TimerPanel");
			timerPanelObj.transform.SetParent(pausePanelRef.transform.parent, false);
			RectTransform obj = timerPanelObj.AddComponent<RectTransform>();
			obj.anchorMin = new Vector2(0.5f, 0.5f);
			obj.anchorMax = new Vector2(0.5f, 0.5f);
			obj.pivot = new Vector2(0.5f, 0f);
			obj.sizeDelta = new Vector2(440f, 44f);
			UpdateTimerPanelTransform(((Object)(object)pauseHandlerRef != (Object)null) ? pauseHandlerRef : PauseHandler.main);
			Image val = timerPanelObj.AddComponent<Image>();
			Sprite val2 = ResourceLookupCache.FindSprite("uiBlock") ?? ResourceLookupCache.FindSpriteContains("uiBlock");
			if ((Object)(object)val2 != (Object)null)
			{
				val.sprite = val2;
				val.type = (Type)1;
			}
			((Graphic)val).color = new Color(0f, 0f, 0f, 0.7f);
			((Graphic)val).raycastTarget = false;
			HorizontalLayoutGroup obj2 = timerPanelObj.AddComponent<HorizontalLayoutGroup>();
			((LayoutGroup)obj2).padding = new RectOffset(12, 12, 8, 8);
			((HorizontalOrVerticalLayoutGroup)obj2).spacing = 20f;
			((HorizontalOrVerticalLayoutGroup)obj2).childControlWidth = true;
			((HorizontalOrVerticalLayoutGroup)obj2).childControlHeight = true;
			((HorizontalOrVerticalLayoutGroup)obj2).childForceExpandWidth = true;
			((HorizontalOrVerticalLayoutGroup)obj2).childForceExpandHeight = true;
			((LayoutGroup)obj2).childAlignment = (TextAnchor)4;
			TMP_FontAsset font = ResourceLookupCache.FindFontContains("sevenish");
			CreateTimerText("IGT", font, out igtText);
			CreateTimerText("RTA", font, out rtaText);
			timerPanelObj.SetActive(false);
		}
	}

	private static void UpdateTimerPanelTransform(PauseHandler pauseHandler)
	{
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		//IL_0128: Unknown result type (might be due to invalid IL or missing references)
		//IL_0137: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_015f: Unknown result type (might be due to invalid IL or missing references)
		//IL_016b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		RectTransform val = (((Object)(object)timerPanelObj != (Object)null) ? timerPanelObj.GetComponent<RectTransform>() : null);
		object obj;
		if (!((Object)(object)timerPanelObj != (Object)null))
		{
			obj = null;
		}
		else
		{
			Transform parent = timerPanelObj.transform.parent;
			obj = ((parent is RectTransform) ? parent : null);
		}
		RectTransform val2 = (RectTransform)obj;
		if ((Object)(object)val == (Object)null || (Object)(object)val2 == (Object)null)
		{
			return;
		}
		RectTransform val3 = (((Object)(object)pauseHandler != (Object)null) ? pauseHandler.center : null);
		Vector2 val4;
		Rect rect;
		float num;
		if ((Object)(object)val3 != (Object)null)
		{
			val4 = Vector2.op_Implicit(((Transform)val2).InverseTransformPoint(((Transform)val3).position));
			rect = val3.rect;
			float width = ((Rect)(ref rect)).width;
			rect = val3.rect;
			num = Mathf.Max(width, ((Rect)(ref rect)).height) * 0.5f;
		}
		else
		{
			RectTransform val5 = (((Object)(object)pausePanelRef != (Object)null) ? pausePanelRef.GetComponent<RectTransform>() : null);
			val4 = (((Object)(object)val5 != (Object)null) ? val5.anchoredPosition : Vector2.zero);
			float num2;
			if (!((Object)(object)val5 != (Object)null))
			{
				num2 = 300f;
			}
			else
			{
				rect = val5.rect;
				float width2 = ((Rect)(ref rect)).width;
				rect = val5.rect;
				num2 = Mathf.Max(width2, ((Rect)(ref rect)).height) * 0.5f;
			}
			num = num2;
		}
		val.anchoredPosition = new Vector2(val4.x, val4.y + num + 24f);
		float num3 = (((Object)(object)pauseHandler != (Object)null) ? Mathf.Clamp01(pauseHandler.pauseProgress) : 1f);
		((Transform)val).localScale = Vector3.one * Utils.OutCubic(num3);
	}

	private static Transform FindPauseMenu(PlayerCamera playerCamera)
	{
		Transform val = ((Component)playerCamera).transform.Find("Canvas/PauseMenu");
		if ((Object)(object)val == (Object)null)
		{
			GameObject obj = GameObject.Find("Main Camera/Canvas/PauseMenu");
			val = ((obj != null) ? obj.transform : null);
		}
		if ((Object)(object)val == (Object)null)
		{
			Transform val2 = ((Component)playerCamera).transform.Find("Canvas");
			if ((Object)(object)val2 == (Object)null)
			{
				GameObject obj2 = GameObject.Find("Canvas");
				val2 = ((obj2 != null) ? obj2.transform : null);
			}
			val = ((val2 != null) ? val2.Find("PauseMenu") : null);
		}
		return val;
	}

	private static void CreateTimerText(string label, TMP_FontAsset font, out TextMeshProUGUI timeText)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Expected O, but got Unknown
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0138: Unknown result type (might be due to invalid IL or missing references)
		//IL_013e: Expected O, but got Unknown
		//IL_0182: Unknown result type (might be due to invalid IL or missing references)
		GameObject val = new GameObject(label + "Container", new Type[1] { typeof(RectTransform) });
		val.transform.SetParent(timerPanelObj.transform, false);
		HorizontalLayoutGroup obj = val.AddComponent<HorizontalLayoutGroup>();
		((HorizontalOrVerticalLayoutGroup)obj).spacing = 6f;
		((LayoutGroup)obj).childAlignment = (TextAnchor)3;
		((HorizontalOrVerticalLayoutGroup)obj).childControlWidth = true;
		((HorizontalOrVerticalLayoutGroup)obj).childControlHeight = true;
		((HorizontalOrVerticalLayoutGroup)obj).childForceExpandWidth = false;
		((HorizontalOrVerticalLayoutGroup)obj).childForceExpandHeight = true;
		val.AddComponent<LayoutElement>().flexibleWidth = 1f;
		GameObject val2 = new GameObject("Label", new Type[2]
		{
			typeof(RectTransform),
			typeof(TextMeshProUGUI)
		});
		val2.transform.SetParent(val.transform, false);
		TextMeshProUGUI component = val2.GetComponent<TextMeshProUGUI>();
		((TMP_Text)component).text = "<b>" + label + ":</b>";
		if (Object.op_Implicit((Object)(object)font))
		{
			((TMP_Text)component).font = font;
		}
		((TMP_Text)component).fontSize = 20f;
		((Graphic)component).color = Color.yellow;
		((TMP_Text)component).alignment = (TextAlignmentOptions)4097;
		((TMP_Text)component).enableWordWrapping = false;
		((Graphic)component).raycastTarget = false;
		GameObject val3 = new GameObject("Time", new Type[2]
		{
			typeof(RectTransform),
			typeof(TextMeshProUGUI)
		});
		val3.transform.SetParent(val.transform, false);
		timeText = val3.GetComponent<TextMeshProUGUI>();
		((TMP_Text)timeText).text = "00:00:00.000";
		if (Object.op_Implicit((Object)(object)font))
		{
			((TMP_Text)timeText).font = font;
		}
		((TMP_Text)timeText).fontSize = 20f;
		((Graphic)timeText).color = Color.white;
		((TMP_Text)timeText).alignment = (TextAlignmentOptions)4100;
		((TMP_Text)timeText).enableWordWrapping = false;
		((Graphic)timeText).raycastTarget = false;
		val3.AddComponent<LayoutElement>().flexibleWidth = 1f;
	}

	private static void UpdateTimerDisplay()
	{
		if ((Object)(object)igtText != (Object)null)
		{
			TimeSpan ts = TimeSpan.FromSeconds(inGameTime);
			((TMP_Text)igtText).text = FormatTime(ts);
		}
		if ((Object)(object)rtaText != (Object)null)
		{
			((TMP_Text)rtaText).text = FormatTime(GetCurrentRTA());
		}
	}

	private static string FormatTime(TimeSpan ts)
	{
		if (ts.TotalHours >= 1.0)
		{
			return $"{(int)ts.TotalHours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}.{ts.Milliseconds:D3}";
		}
		return $"{ts.Minutes:D2}:{ts.Seconds:D2}.{ts.Milliseconds:D3}";
	}

	public static float GetIGT()
	{
		return inGameTime;
	}

	public static TimeSpan GetRTA()
	{
		return GetCurrentRTA();
	}

	public static void ResetTimers()
	{
		inGameTime = 0f;
		_rtaStartOffset = TimeSpan.Zero;
		rtaStopwatch.Reset();
		rtaStopwatch.Start();
		timersStarted = true;
	}

	public static void SetTimers(float igtSeconds, float rtaSeconds)
	{
		inGameTime = Mathf.Max(0f, igtSeconds);
		_rtaStartOffset = TimeSpan.FromSeconds(Mathf.Max(0f, rtaSeconds));
		rtaStopwatch.Reset();
		rtaStopwatch.Start();
		timersStarted = true;
	}

	private static TimeSpan GetCurrentRTA()
	{
		return _rtaStartOffset + rtaStopwatch.Elapsed;
	}
}
