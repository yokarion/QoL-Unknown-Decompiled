using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace QoL_Unknown;

internal class CasualtyReportPatcher
{
	[Serializable]
	private class CasualtyReportEntry
	{
		public string Id;

		public string SavedAtUtc;

		public string[] Lines;
	}

	private const int ReportLineCount = 14;

	private const string ReportsFolderName = "casualty_reports";

	private static readonly List<CasualtyReportEntry> LoadedReports = new List<CasualtyReportEntry>();

	private static ManualLogSource Logger;

	private static bool capturedCurrentDeathReport;

	private static int currentReportIndex = -1;

	private static RectTransform currentDeathStats;

	private static RectTransform navigationRoot;

	private static Button leftArrowButton;

	private static Button rightArrowButton;

	private static TextMeshProUGUI pageIndicator;

	private static string ReportsFolderPath => Path.Combine(SaveManager.SaveFolder, "casualty_reports");

	public static void Initialize(ManualLogSource logger)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Expected O, but got Unknown
		Logger = logger;
		Harmony val = new Harmony("org.bepinex.plugins.qol.casualtyreports");
		val.PatchAll(typeof(CasualtyReportPatcher));
		Logger.LogInfo((object)"CasualtyReportPatcher initialized");
	}

	[HarmonyPatch(typeof(PlayerCamera), "Update")]
	[HarmonyPostfix]
	private static void PlayerCamera_Update_Postfix(PlayerCamera __instance)
	{
		if ((Object)(object)__instance == (Object)null || (Object)(object)__instance.deathStats == (Object)null)
		{
			return;
		}
		if ((Object)(object)__instance.body == (Object)null || __instance.body.alive || !__instance.didDeathScreen)
		{
			ResetSessionState(clearUiBindings: false);
			return;
		}
		RectTransform deathStats = __instance.deathStats;
		EnsureNavigationUI(deathStats);
		UpdateNavigationLayout(deathStats);
		if (capturedCurrentDeathReport)
		{
			return;
		}
		string[] lines = ReadDeathStatLines(deathStats);
		if (HasCompleteReport(lines))
		{
			CasualtyReportEntry report = CreateReportEntry(lines);
			SaveReport(report);
			LoadReports();
			capturedCurrentDeathReport = true;
			currentReportIndex = Mathf.Max(0, LoadedReports.FindIndex((CasualtyReportEntry entry) => entry.Id == report.Id));
			ApplyReportToDeathStats(deathStats, GetCurrentReport());
			UpdateNavigationState();
		}
	}

	[HarmonyPatch(typeof(PlayerCamera), "HandleDeathScreen")]
	[HarmonyPostfix]
	private static void HandleDeathScreen_Postfix(PlayerCamera __instance)
	{
		if ((Object)(object)__instance == (Object)null || (Object)(object)__instance.body == (Object)null || __instance.body.alive)
		{
			ResetSessionState(clearUiBindings: false);
		}
	}

	private static void EnsureNavigationUI(RectTransform deathStats)
	{
		if (!((Object)(object)deathStats == (Object)null))
		{
			if ((Object)(object)currentDeathStats != (Object)(object)deathStats)
			{
				currentDeathStats = deathStats;
				Transform parent = ((Transform)deathStats).parent;
				navigationRoot = (RectTransform)(object)((parent is RectTransform) ? parent : null);
				leftArrowButton = null;
				rightArrowButton = null;
				pageIndicator = null;
			}
			if ((Object)(object)leftArrowButton != (Object)null && (Object)(object)rightArrowButton != (Object)null && (Object)(object)pageIndicator != (Object)null)
			{
				UpdateNavigationState();
				return;
			}
			TMP_FontAsset deathStatsFont = GetDeathStatsFont(deathStats);
			RectTransform parent2 = (((Object)(object)navigationRoot != (Object)null) ? navigationRoot : deathStats);
			leftArrowButton = CreateArrowButton(parent2, "PrevReportButton", "<", deathStatsFont, ShowPreviousReport);
			rightArrowButton = CreateArrowButton(parent2, "NextReportButton", ">", deathStatsFont, ShowNextReport);
			pageIndicator = CreatePageIndicator(parent2, deathStatsFont);
			LoadReports();
			UpdateNavigationLayout(deathStats);
			UpdateNavigationState();
		}
	}

	private static Button CreateArrowButton(RectTransform parent, string objectName, string label, TMP_FontAsset font, Action onClick)
	{
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Expected O, but got Unknown
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0146: Unknown result type (might be due to invalid IL or missing references)
		//IL_0150: Expected O, but got Unknown
		//IL_01be: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_018e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0195: Expected O, but got Unknown
		//IL_022d: Unknown result type (might be due to invalid IL or missing references)
		Transform val = ((Transform)parent).Find(objectName);
		GameObject val2;
		if ((Object)(object)val != (Object)null)
		{
			val2 = ((Component)val).gameObject;
		}
		else
		{
			val2 = new GameObject(objectName, new Type[3]
			{
				typeof(RectTransform),
				typeof(Image),
				typeof(Button)
			});
			val2.transform.SetParent((Transform)(object)parent, false);
		}
		RectTransform component = val2.GetComponent<RectTransform>();
		component.anchorMin = new Vector2(0.5f, 0.5f);
		component.anchorMax = new Vector2(0.5f, 0.5f);
		component.pivot = new Vector2(0.5f, 0.5f);
		component.anchoredPosition = Vector2.zero;
		component.sizeDelta = new Vector2(52f, 36f);
		Image component2 = val2.GetComponent<Image>();
		((Graphic)component2).color = new Color(0f, 0f, 0f, 0.45f);
		((Graphic)component2).raycastTarget = true;
		Button component3 = val2.GetComponent<Button>();
		((Selectable)component3).transition = (Transition)1;
		((Selectable)component3).targetGraphic = (Graphic)(object)component2;
		((UnityEventBase)component3.onClick).RemoveAllListeners();
		((UnityEvent)component3.onClick).AddListener((UnityAction)delegate
		{
			onClick?.Invoke();
		});
		TextMeshProUGUI val3 = val2.GetComponentInChildren<TextMeshProUGUI>(true);
		if ((Object)(object)val3 == (Object)null)
		{
			GameObject val4 = new GameObject("Label", new Type[2]
			{
				typeof(RectTransform),
				typeof(TextMeshProUGUI)
			});
			val4.transform.SetParent(val2.transform, false);
			val3 = val4.GetComponent<TextMeshProUGUI>();
		}
		RectTransform rectTransform = ((TMP_Text)val3).rectTransform;
		rectTransform.anchorMin = Vector2.zero;
		rectTransform.anchorMax = Vector2.one;
		rectTransform.offsetMin = Vector2.zero;
		rectTransform.offsetMax = Vector2.zero;
		if ((Object)(object)font != (Object)null)
		{
			((TMP_Text)val3).font = font;
		}
		((TMP_Text)val3).text = label;
		((TMP_Text)val3).fontSize = 24f;
		((TMP_Text)val3).alignment = (TextAlignmentOptions)514;
		((Graphic)val3).color = Color.white;
		((Graphic)val3).raycastTarget = false;
		return component3;
	}

	private static TextMeshProUGUI CreatePageIndicator(RectTransform parent, TMP_FontAsset font)
	{
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Expected O, but got Unknown
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
		Transform val = ((Transform)parent).Find("ReportPageIndicator");
		GameObject val2;
		if ((Object)(object)val != (Object)null)
		{
			val2 = ((Component)val).gameObject;
		}
		else
		{
			val2 = new GameObject("ReportPageIndicator", new Type[2]
			{
				typeof(RectTransform),
				typeof(TextMeshProUGUI)
			});
			val2.transform.SetParent((Transform)(object)parent, false);
		}
		RectTransform component = val2.GetComponent<RectTransform>();
		component.anchorMin = new Vector2(0.5f, 0.5f);
		component.anchorMax = new Vector2(0.5f, 0.5f);
		component.pivot = new Vector2(0.5f, 0.5f);
		component.anchoredPosition = Vector2.zero;
		component.sizeDelta = new Vector2(180f, 32f);
		TextMeshProUGUI component2 = val2.GetComponent<TextMeshProUGUI>();
		if ((Object)(object)font != (Object)null)
		{
			((TMP_Text)component2).font = font;
		}
		((TMP_Text)component2).fontSize = 16f;
		((TMP_Text)component2).alignment = (TextAlignmentOptions)514;
		((Graphic)component2).color = new Color(0.95f, 0.95f, 0.95f, 0.95f);
		((Graphic)component2).raycastTarget = false;
		return component2;
	}

	private static void UpdateNavigationLayout(RectTransform deathStats)
	{
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_011b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0128: Unknown result type (might be due to invalid IL or missing references)
		//IL_0135: Unknown result type (might be due to invalid IL or missing references)
		//IL_015d: Unknown result type (might be due to invalid IL or missing references)
		//IL_016a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0177: Unknown result type (might be due to invalid IL or missing references)
		//IL_019f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b3: Unknown result type (might be due to invalid IL or missing references)
		object obj;
		if (!((Object)(object)navigationRoot != (Object)null))
		{
			if (!((Object)(object)deathStats != (Object)null))
			{
				obj = null;
			}
			else
			{
				Transform parent = ((Transform)deathStats).parent;
				obj = ((parent is RectTransform) ? parent : null);
			}
		}
		else
		{
			obj = navigationRoot;
		}
		RectTransform val = (RectTransform)obj;
		if (!((Object)(object)deathStats == (Object)null) && !((Object)(object)val == (Object)null))
		{
			Vector3[] array = (Vector3[])(object)new Vector3[4];
			deathStats.GetWorldCorners(array);
			Vector2 val2 = Vector2.Lerp(Vector2.op_Implicit(array[0]), Vector2.op_Implicit(array[1]), 0.5f);
			Vector2 val3 = Vector2.Lerp(Vector2.op_Implicit(array[3]), Vector2.op_Implicit(array[2]), 0.5f);
			Vector2 val4 = Vector2.Lerp(Vector2.op_Implicit(array[0]), Vector2.op_Implicit(array[3]), 0.5f);
			Vector2 val5 = Vector2.op_Implicit(((Transform)val).InverseTransformPoint(Vector2.op_Implicit(val2)));
			Vector2 val6 = Vector2.op_Implicit(((Transform)val).InverseTransformPoint(Vector2.op_Implicit(val3)));
			Vector2 val7 = Vector2.op_Implicit(((Transform)val).InverseTransformPoint(Vector2.op_Implicit(val4)));
			if ((Object)(object)leftArrowButton != (Object)null)
			{
				((Component)leftArrowButton).GetComponent<RectTransform>().anchoredPosition = new Vector2(val5.x - 28f, val7.y + 10f);
			}
			if ((Object)(object)rightArrowButton != (Object)null)
			{
				((Component)rightArrowButton).GetComponent<RectTransform>().anchoredPosition = new Vector2(val6.x + 28f, val7.y + 10f);
			}
			if ((Object)(object)pageIndicator != (Object)null)
			{
				((TMP_Text)pageIndicator).rectTransform.anchoredPosition = new Vector2(val7.x, val7.y - 24f);
			}
		}
	}

	private static TMP_FontAsset GetDeathStatsFont(RectTransform deathStats)
	{
		for (int i = 0; i < ((Transform)deathStats).childCount; i++)
		{
			TextMeshProUGUI component = ((Component)((Transform)deathStats).GetChild(i)).GetComponent<TextMeshProUGUI>();
			if ((Object)(object)component != (Object)null && (Object)(object)((TMP_Text)component).font != (Object)null)
			{
				return ((TMP_Text)component).font;
			}
		}
		return FontPatcher.GetRetroGamingFont() ?? FontPatcher.GetRetroPixelExtendedFont();
	}

	private static void ShowPreviousReport()
	{
		LoadReports();
		if (LoadedReports.Count != 0 && !((Object)(object)currentDeathStats == (Object)null))
		{
			currentReportIndex = (currentReportIndex - 1 + LoadedReports.Count) % LoadedReports.Count;
			ApplyReportToDeathStats(currentDeathStats, GetCurrentReport());
			UpdateNavigationState();
		}
	}

	private static void ShowNextReport()
	{
		LoadReports();
		if (LoadedReports.Count != 0 && !((Object)(object)currentDeathStats == (Object)null))
		{
			currentReportIndex = (currentReportIndex + 1) % LoadedReports.Count;
			ApplyReportToDeathStats(currentDeathStats, GetCurrentReport());
			UpdateNavigationState();
		}
	}

	private static CasualtyReportEntry GetCurrentReport()
	{
		if (LoadedReports.Count == 0)
		{
			return null;
		}
		if (currentReportIndex < 0 || currentReportIndex >= LoadedReports.Count)
		{
			currentReportIndex = LoadedReports.Count - 1;
		}
		return LoadedReports[currentReportIndex];
	}

	private static void UpdateNavigationState()
	{
		bool flag = LoadedReports.Count > 0;
		if ((Object)(object)leftArrowButton != (Object)null)
		{
			((Component)leftArrowButton).gameObject.SetActive(flag);
			((Selectable)leftArrowButton).interactable = LoadedReports.Count > 1;
		}
		if ((Object)(object)rightArrowButton != (Object)null)
		{
			((Component)rightArrowButton).gameObject.SetActive(flag);
			((Selectable)rightArrowButton).interactable = LoadedReports.Count > 1;
		}
		if ((Object)(object)pageIndicator != (Object)null)
		{
			((Component)pageIndicator).gameObject.SetActive(flag);
			if (flag)
			{
				int num = Mathf.Clamp(currentReportIndex + 1, 1, LoadedReports.Count);
				((TMP_Text)pageIndicator).text = $"{num}/{LoadedReports.Count}";
			}
		}
	}

	private static void ApplyReportToDeathStats(RectTransform deathStats, CasualtyReportEntry report)
	{
		if ((Object)(object)deathStats == (Object)null || report == null || report.Lines == null)
		{
			return;
		}
		int num = Mathf.Min(new int[3]
		{
			14,
			report.Lines.Length,
			((Transform)deathStats).childCount
		});
		for (int i = 0; i < num; i++)
		{
			TextMeshProUGUI component = ((Component)((Transform)deathStats).GetChild(i)).GetComponent<TextMeshProUGUI>();
			if ((Object)(object)component != (Object)null)
			{
				((TMP_Text)component).text = report.Lines[i] ?? string.Empty;
			}
		}
	}

	private static string[] ReadDeathStatLines(RectTransform deathStats)
	{
		string[] array = new string[14];
		for (int i = 0; i < 14 && i < ((Transform)deathStats).childCount; i++)
		{
			TextMeshProUGUI component = ((Component)((Transform)deathStats).GetChild(i)).GetComponent<TextMeshProUGUI>();
			array[i] = (((Object)(object)component != (Object)null) ? ((TMP_Text)component).text : null);
		}
		return array;
	}

	private static bool HasCompleteReport(string[] lines)
	{
		if (lines == null || lines.Length < 14)
		{
			return false;
		}
		for (int i = 0; i < 14; i++)
		{
			if (string.IsNullOrWhiteSpace(lines[i]))
			{
				return false;
			}
		}
		return true;
	}

	private static CasualtyReportEntry CreateReportEntry(string[] lines)
	{
		return new CasualtyReportEntry
		{
			Id = DateTime.UtcNow.ToString("yyyyMMddHHmmssffff"),
			SavedAtUtc = DateTime.UtcNow.ToString("o"),
			Lines = lines.ToArray()
		};
	}

	private static void SaveReport(CasualtyReportEntry report)
	{
		try
		{
			Directory.CreateDirectory(ReportsFolderPath);
			string path = Path.Combine(ReportsFolderPath, report.Id + ".json");
			string contents = JsonConvert.SerializeObject((object)report, (Formatting)1);
			File.WriteAllText(path, contents);
		}
		catch (Exception arg)
		{
			ManualLogSource logger = Logger;
			if (logger != null)
			{
				logger.LogError((object)$"Failed to save casualty report: {arg}");
			}
		}
	}

	private static void LoadReports()
	{
		LoadedReports.Clear();
		try
		{
			if (!Directory.Exists(ReportsFolderPath))
			{
				return;
			}
			string[] files = Directory.GetFiles(ReportsFolderPath, "*.json");
			foreach (string path in files)
			{
				try
				{
					CasualtyReportEntry casualtyReportEntry = JsonConvert.DeserializeObject<CasualtyReportEntry>(File.ReadAllText(path));
					if (casualtyReportEntry?.Lines != null && casualtyReportEntry.Lines.Length >= 14)
					{
						LoadedReports.Add(casualtyReportEntry);
					}
				}
				catch (Exception ex)
				{
					ManualLogSource logger = Logger;
					if (logger != null)
					{
						logger.LogWarning((object)("Failed to read casualty report '" + Path.GetFileName(path) + "': " + ex.Message));
					}
				}
			}
			LoadedReports.Sort((CasualtyReportEntry a, CasualtyReportEntry b) => string.CompareOrdinal(a.Id, b.Id));
		}
		catch (Exception arg)
		{
			ManualLogSource logger2 = Logger;
			if (logger2 != null)
			{
				logger2.LogError((object)$"Failed to load casualty reports: {arg}");
			}
		}
	}

	private static void ResetSessionState(bool clearUiBindings)
	{
		capturedCurrentDeathReport = false;
		currentReportIndex = -1;
		if (clearUiBindings)
		{
			currentDeathStats = null;
			navigationRoot = null;
			leftArrowButton = null;
			rightArrowButton = null;
			pageIndicator = null;
		}
	}
}
