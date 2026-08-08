using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx.Logging;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace QoL_Unknown;

internal static class QoLModSettings
{
	private const string MinigameQoLPrefKey = "Setting_Minigame_QoL";

	private const string SkillPointAllocationPrefKey = "Setting_SkillPointAllocation";

	private const string UiScalePrefKey = "UIScale";

	private const string ShowFpsPrefKey = "ShowFPSCounter";

	private const string LightIntensityPrefKey = "LightIntensity";

	private const string StartStrPrefKey = "StartSTR";

	private const string StartResPrefKey = "StartRES";

	private const string StartIntPrefKey = "StartINT";

	private const string SelectedEntityDensityPrefKey = "SelectedEntityDensityTarget";

	private const string SelectedStructureDensityPrefKey = "SelectedStructureDensityTarget";

	private const string SelectedLayerModifierFrequencyPrefKey = "SelectedLayerModifierFrequencyTarget";

	internal const string ExperimentalFeaturesPrefKey = "ExperimentalFeatures";

	private const float UiScaleMin = 0.75f;

	private const float UiScaleMax = 1.5f;

	private const float SoundOverrideMax = 1.2f;

	private static bool initialized;

	private static ManualLogSource Logger;

	public static void Initialize(ManualLogSource logger)
	{
		Logger = logger;
		if (!initialized)
		{
			initialized = true;
			ModOptionsMenu.Initialize(logger);
			ModManagementSettings.Initialize(logger);
			RegisterStandardSettings();
			RegisterCustomSettings();
			ModManagementSettings.RegisterSettings();
		}
	}

	private static void RegisterStandardSettings()
	{
		ModOptionsMenu.Register(ModOptionDefinition.Float("qol.video.intensity", "Intensity", "Controls the maximum brightness of in-world lights. Similar to the V6 brightness slider (with a higher range).", (SettingCategory)0, 100f, 0f, 200f, delegate(float value)
		{
			AntiGlarePatcher.MasterLightScale = Mathf.Clamp(value, 0f, 200f);
			PlayerPrefs.SetFloat("LightIntensity", AntiGlarePatcher.MasterLightScale);
			PlayerPrefs.Save();
			AntiGlarePatcher.SweepAllLights();
		}, (float value) => $"{Mathf.RoundToInt(Mathf.Clamp(value, 0f, 200f))}%"));
		ModOptionsMenu.Register(ModOptionDefinition.Float("qol.video.effects", "Effect %", "Scales screen effect intensity.", (SettingCategory)0, 1f, 0f, 1f, delegate(float value)
		{
			AntiGlarePatcher.SetScreenEffectsIntensity(value);
		}, FormatPercent));
		ModOptionsMenu.Register(ModOptionDefinition.Bool("qol.video.antiepilepsy", "Anti-Epilepsy", "Reduces flashing and strobing effects.", (SettingCategory)0, defaultValue: false, delegate(bool value)
		{
			AntiGlarePatcher.SetAntiEpilepsyEnabled(value);
		}));
		ModOptionsMenu.Register(ModOptionDefinition.Float("qol.video.uiscale", "UI Scale", "Adjusts UI scale across menus and overlays.", (SettingCategory)0, 1f, 0.75f, 1.5f, delegate(float value)
		{
			float num = Mathf.Clamp(value, 0.75f, 1.5f);
			PlayerPrefs.SetFloat("UIScale", num);
			PlayerPrefs.Save();
			EnhancedMenuController.ApplySavedUiScale(num);
		}, (float value) => $"{Mathf.RoundToInt(Mathf.Clamp(value, 0.75f, 1.5f) * 100f)}%"));
		ModOptionsMenu.Register(ModOptionDefinition.Bool("qol.video.fpscounter", "FPS", "Shows the QoL FPS counter overlay.", (SettingCategory)0, defaultValue: false, delegate(bool value)
		{
			PlayerPrefs.SetInt("ShowFPSCounter", value ? 1 : 0);
			PlayerPrefs.Save();
			EnhancedMenuController.SetFpsCounterEnabledStatic(value);
		}));
		ModOptionsMenu.Register(ModOptionDefinition.Bool("qol.game.pauseonunfocus", "Pause on application unfocus", "Pauses the game when the application loses focus.", (SettingCategory)2, defaultValue: false, delegate(bool value)
		{
			EnhancedMenuController.SetPauseOnUnfocusEnabled(value);
		}));
		ModOptionsMenu.Register(ModOptionDefinition.Bool("qol.game.v6ilmenite", "V6 Titanium Generation", "Makes Ilmenite generate in earlier layers, similar to V6.", (SettingCategory)2, defaultValue: false, delegate(bool value)
		{
			IlmeniteWorldGenPatcher.SetEnabled(value);
		}));
		ModOptionsMenu.Register(ModOptionDefinition.Bool("qol.game.skipexitanimation", "Skip exit animation", "Instantly returns to the main menu on exit without playing the exit animation.", (SettingCategory)2, defaultValue: false, delegate(bool value)
		{
			ExitConfirmationPatcher.SetSkipExitAnimationEnabled(value);
		}));
		ModOptionsMenu.Register(ModOptionDefinition.Bool("qol.game.autosaveonexit", "Autosave on exit", "Updates the autosave slot when using save-and-exit.", (SettingCategory)2, defaultValue: false, delegate(bool value)
		{
			SaveManager.SetAutosaveOnExitEnabled(value);
		}));
		ModOptionsMenu.Register(ModOptionDefinition.Bool("qol.game.deleteautosaveondeath", "Delete autosave on death", "Deletes the autosave slot once the death screen is reached.", (SettingCategory)2, defaultValue: false, delegate(bool value)
		{
			SaveManager.SetDeleteAutosaveOnDeathEnabled(value);
		}));
		ModOptionsMenu.Register(ModOptionDefinition.Bool("qol.game.disableradline", "Disable radline", "Prevents the radline from appearing, even if already active.", (SettingCategory)2, defaultValue: false, delegate(bool value)
		{
			RadlinePatcher.SetDisabled(value);
		}));
		ModOptionsMenu.Register(ModOptionDefinition.Bool("qol.game.disablelineofsight", "Disable unchipped line of sight", "Disables line of sight in unchipped. Woah.", (SettingCategory)2, defaultValue: false, delegate(bool value)
		{
			LineOfSightPatcher.SetLineOfSightDisabled(value);
		}));
		ModOptionsMenu.Register(ModOptionDefinition.Bool("qol.game.minigameqol", "Faster minigames", "Makes lockpicking faster and shortens keypad codes to 5-9 digits.", (SettingCategory)2, defaultValue: false, delegate(bool value)
		{
			PlayerPrefs.SetInt("Setting_Minigame_QoL", value ? 1 : 0);
			PlayerPrefs.Save();
		}));
		ModOptionsMenu.Register(ModOptionDefinition.Bool("qol.game.skillallocation", "Skill point allocation", "Enables the QoL skill allocation adjustments for starting stats.", (SettingCategory)2, defaultValue: true, delegate(bool value)
		{
			PlayerPrefs.SetInt("Setting_SkillPointAllocation", value ? 1 : 0);
			PlayerPrefs.Save();
		}));
		RegisterStartingStat("qol.game.skillallocation.str", "Starting STR", "StartSTR", 9);
		RegisterStartingStat("qol.game.skillallocation.res", "Starting RES", "StartRES", 8);
		RegisterStartingStat("qol.game.skillallocation.int", "Starting INT", "StartINT", 7);
	}

	private static void RegisterStartingStat(string id, string label, string prefKey, int defaultValue)
	{
		ModOptionsMenu.Register(ModOptionDefinition.Int(id, label, "Sets the starting stat value used by the QoL skill allocation patch.", (SettingCategory)2, defaultValue, 0, 20, delegate(int value)
		{
			PlayerPrefs.SetInt(prefKey, Mathf.Clamp(value, 0, 20));
			PlayerPrefs.Save();
		}));
	}

	private static void RegisterCustomSettings()
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_0120: Unknown result type (might be due to invalid IL or missing references)
		//IL_0172: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c4: Unknown result type (might be due to invalid IL or missing references)
		ModCustomSettingDefinition modCustomSettingDefinition = new ModCustomSettingDefinition();
		modCustomSettingDefinition.Id = "qol.game.downloadlocale";
		modCustomSettingDefinition.Label = "Download Locale";
		modCustomSettingDefinition.Description = "Downloads and installs additional locale files from the locale repository. Warning: WILL RESET THE LAYER IF USED IN-GAME";
		modCustomSettingDefinition.Category = (SettingCategory)4;
		modCustomSettingDefinition.TemplateResourcePath = "Special/GameSettingFloat";
		modCustomSettingDefinition.Binder = BindLocaleRow;
		ModOptionsMenu.RegisterCustom(modCustomSettingDefinition);
		modCustomSettingDefinition = new ModCustomSettingDefinition();
		modCustomSettingDefinition.Id = "qol.game.entitydensity";
		modCustomSettingDefinition.Label = "Individual Entity Density";
		modCustomSettingDefinition.Description = "Adjust any entity's spawn multiplier.";
		modCustomSettingDefinition.Category = (SettingCategory)2;
		modCustomSettingDefinition.TemplateResourcePath = "Special/GameSettingFloat";
		modCustomSettingDefinition.Binder = BindEntityDensityRow;
		ModOptionsMenu.RegisterCustom(modCustomSettingDefinition);
		modCustomSettingDefinition = new ModCustomSettingDefinition();
		modCustomSettingDefinition.Id = "qol.game.structuredensity";
		modCustomSettingDefinition.Label = "Individual Structure Density";
		modCustomSettingDefinition.Description = "Adjust any structure's spawn multiplier.";
		modCustomSettingDefinition.Category = (SettingCategory)2;
		modCustomSettingDefinition.TemplateResourcePath = "Special/GameSettingFloat";
		modCustomSettingDefinition.Binder = BindStructureDensityRow;
		ModOptionsMenu.RegisterCustom(modCustomSettingDefinition);
		modCustomSettingDefinition = new ModCustomSettingDefinition();
		modCustomSettingDefinition.Id = "qol.game.layermodifierdensity";
		modCustomSettingDefinition.Label = "Layer Modifier Frequency";
		modCustomSettingDefinition.Description = "Adjust the frequency of layer modifiers.";
		modCustomSettingDefinition.Category = (SettingCategory)2;
		modCustomSettingDefinition.TemplateResourcePath = "Special/GameSettingFloat";
		modCustomSettingDefinition.Binder = BindLayerModifierFrequencyRow;
		ModOptionsMenu.RegisterCustom(modCustomSettingDefinition);
		modCustomSettingDefinition = new ModCustomSettingDefinition();
		modCustomSettingDefinition.Id = "qol.audio.soundoverride";
		modCustomSettingDefinition.Label = "Individual Sound Overrides";
		modCustomSettingDefinition.Description = "Adjust the volume of any specific sound";
		modCustomSettingDefinition.Category = (SettingCategory)1;
		modCustomSettingDefinition.TemplateResourcePath = "Special/GameSettingFloat";
		modCustomSettingDefinition.Binder = BindSoundOverrideRow;
		ModOptionsMenu.RegisterCustom(modCustomSettingDefinition);
		modCustomSettingDefinition = new ModCustomSettingDefinition();
		modCustomSettingDefinition.Id = "qol.game.discordpresence";
		modCustomSettingDefinition.Label = "Discord Presence";
		modCustomSettingDefinition.Description = "Edit Discord Rich Presence templates for details, state, and image tooltips.";
		modCustomSettingDefinition.Category = (SettingCategory)2;
		modCustomSettingDefinition.TemplateResourcePath = "Special/GameSettingInput";
		modCustomSettingDefinition.Binder = BindDiscordPresenceRow;
		modCustomSettingDefinition.IsVisible = AreExperimentalFeaturesEnabled;
		ModOptionsMenu.RegisterCustom(modCustomSettingDefinition);
	}

	internal static bool AreExperimentalFeaturesEnabled()
	{
		return PlayerPrefs.GetInt("ExperimentalFeatures", 0) == 1;
	}

	internal static bool ToggleExperimentalFeatures()
	{
		bool flag = !AreExperimentalFeaturesEnabled();
		PlayerPrefs.SetInt("ExperimentalFeatures", flag ? 1 : 0);
		PlayerPrefs.Save();
		return flag;
	}

	internal static void ResetCustomSettingsToDefaults()
	{
		PlayerPrefs.DeleteKey("SelectedEntityDensityTarget");
		PlayerPrefs.DeleteKey("SelectedStructureDensityTarget");
		PlayerPrefs.DeleteKey("SelectedLayerModifierFrequencyTarget");
		EntitySpawnManager.ResetMultipliers();
		StructureSpawnManager.ResetMultipliers();
		LayerModifierPatcher.ResetMultipliers();
		AudioSafetyPatcher.ClearAllOverrides();
		PlayerPrefs.Save();
	}

	private static void BindDiscordPresenceRow(GameObject row)
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0123: Unknown result type (might be due to invalid IL or missing references)
		//IL_0139: Unknown result type (might be due to invalid IL or missing references)
		//IL_014b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0155: Unknown result type (might be due to invalid IL or missing references)
		//IL_016b: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bf: Expected O, but got Unknown
		RectTransform component = row.GetComponent<RectTransform>();
		if ((Object)(object)component != (Object)null)
		{
			component.sizeDelta = new Vector2(component.sizeDelta.x, 40f);
		}
		RectTransform component2 = ((Component)row.transform.GetChild(0)).GetComponent<RectTransform>();
		if ((Object)(object)component2 != (Object)null)
		{
			component2.anchorMin = new Vector2(0f, 0.5f);
			component2.anchorMax = new Vector2(0f, 0.5f);
			component2.pivot = new Vector2(0f, 0.5f);
			component2.sizeDelta = new Vector2(420f, component2.sizeDelta.y);
			component2.anchoredPosition = new Vector2(14f, 0f);
		}
		Button component3 = ((Component)row.transform.GetChild(1)).GetComponent<Button>();
		if (!((Object)(object)component3 == (Object)null))
		{
			RectTransform component4 = ((Component)component3).GetComponent<RectTransform>();
			if ((Object)(object)component4 != (Object)null)
			{
				component4.anchorMin = new Vector2(1f, 0.5f);
				component4.anchorMax = new Vector2(1f, 0.5f);
				component4.pivot = new Vector2(1f, 0.5f);
				component4.sizeDelta = new Vector2(120f, component4.sizeDelta.y);
				component4.anchoredPosition = new Vector2(-20f, 0f);
			}
			TextMeshProUGUI componentInChildren = ((Component)component3).GetComponentInChildren<TextMeshProUGUI>();
			if ((Object)(object)componentInChildren != (Object)null)
			{
				((TMP_Text)componentInChildren).text = "Edit";
			}
			((UnityEventBase)component3.onClick).RemoveAllListeners();
			((UnityEvent)component3.onClick).AddListener(new UnityAction(DiscordPresenceEditor.Open));
		}
	}

	private static void BindLocaleRow(GameObject row)
	{
		Slider component = ((Component)row.transform.GetChild(1)).GetComponent<Slider>();
		if ((Object)(object)component == (Object)null)
		{
			return;
		}
		PrepareInlineSelectorRow(row);
		((Component)component).gameObject.SetActive(false);
		TextMeshProUGUI component2 = ((Component)row.transform.GetChild(2)).GetComponent<TextMeshProUGUI>();
		if ((Object)(object)component2 != (Object)null)
		{
			((TMP_Text)component2).alignment = (TextAlignmentOptions)4100;
			((TMP_Text)component2).text = "Waiting...";
		}
		TMP_Dropdown dropdown = CreateInlineDropdown(row.transform);
		dropdown.ClearOptions();
		dropdown.AddOptions(new List<string> { "Fetching List..." });
		((Selectable)dropdown).interactable = false;
		TextMeshProUGUI status = component2;
		SharedCoroutineStarter.StartCoroutine(LocaleDownloader.FetchLocaleList(delegate(bool success)
		{
			if (!((Object)(object)dropdown == (Object)null))
			{
				dropdown.ClearOptions();
				if (!success)
				{
					dropdown.AddOptions(new List<string> { "Connection Error" });
					if ((Object)(object)status != (Object)null)
					{
						((TMP_Text)status).text = "Failed to reach GitHub";
					}
				}
				else
				{
					List<string> list = new List<string> { "Select Language..." };
					foreach (LocaleDownloader.RemoteLocaleInfo availableLocale in LocaleDownloader.AvailableLocales)
					{
						string text = availableLocale.Name;
						if (File.Exists(Path.Combine(Application.dataPath, "Lang", availableLocale.FileName)))
						{
							text += " (Installed)";
						}
						list.Add(text);
					}
					dropdown.AddOptions(list);
					((Selectable)dropdown).interactable = true;
					if ((Object)(object)status != (Object)null)
					{
						((TMP_Text)status).text = " ";
					}
					((UnityEventBase)dropdown.onValueChanged).RemoveAllListeners();
					((UnityEvent<int>)(object)dropdown.onValueChanged).AddListener((UnityAction<int>)delegate(int index)
					{
						if (index > 0 && index - 1 < LocaleDownloader.AvailableLocales.Count)
						{
							LocaleDownloader.RemoteLocaleInfo info = LocaleDownloader.AvailableLocales[index - 1];
							((Selectable)dropdown).interactable = false;
							if ((Object)(object)status != (Object)null)
							{
								((TMP_Text)status).text = "Downloading " + info.Name + "...";
							}
							SharedCoroutineStarter.StartCoroutine(LocaleDownloader.DownloadAndInstall(info, delegate(string langCode)
							{
								if ((Object)(object)status != (Object)null)
								{
									((TMP_Text)status).text = "Installed " + langCode;
								}
								((Selectable)dropdown).interactable = true;
								dropdown.SetValueWithoutNotify(0);
								Locale.ChangeLanguage(langCode);
							}));
						}
					});
				}
			}
		}));
	}

	private static void BindEntityDensityRow(GameObject row)
	{
		BindDensityRow(row, EntitySpawnManager.KnownEntities, "SelectedEntityDensityTarget", (string entity) => entity, (string entity) => EntitySpawnManager.GetMultiplier(entity), delegate(string entity, float value)
		{
			EntitySpawnManager.SetMultiplier(entity, value);
		}, 0f, 5f, FormatPercentExtended);
	}

	private static void BindStructureDensityRow(GameObject row)
	{
		BindDensityRow(row, StructureSpawnManager.KnownStructures, "SelectedStructureDensityTarget", (string structure) => Path.GetFileName(structure), (string structure) => StructureSpawnManager.GetMultiplier(structure), delegate(string structure, float value)
		{
			StructureSpawnManager.SetMultiplier(structure, value);
		}, 0f, 3f, (float value) => FormatPercentExtended(value, 3f));
	}

	private static void BindLayerModifierFrequencyRow(GameObject row)
	{
		BindDensityRow(row, LayerModifierPatcher.ModifierNames, "SelectedLayerModifierFrequencyTarget", (string modifier) => modifier, (string modifier) => LayerModifierPatcher.GetMultiplier(modifier), delegate(string modifier, float value)
		{
			LayerModifierPatcher.SetMultiplier(modifier, value);
		}, 0f, 5f, FormatPercentExtended);
	}

	private static void BindDensityRow(GameObject row, IList<string> items, string selectedItemPrefKey, Func<string, string> labelSelector, Func<string, float> getValue, Action<string, float> setValue, float minValue, float maxValue, Func<float, string> formatter)
	{
		Slider slider = ((Component)row.transform.GetChild(1)).GetComponent<Slider>();
		if ((Object)(object)slider == (Object)null)
		{
			return;
		}
		PrepareInlineSelectorRow(row);
		List<string> orderedItems = items.Where((string item) => !string.IsNullOrWhiteSpace(item)).Distinct<string>(StringComparer.Ordinal).OrderBy<string, string>(labelSelector, StringComparer.OrdinalIgnoreCase)
			.ToList();
		slider.minValue = minValue;
		slider.maxValue = maxValue;
		((Selectable)slider).interactable = false;
		TextMeshProUGUI sliderValueText = ((Component)row.transform.GetChild(2)).GetComponent<TextMeshProUGUI>();
		if ((Object)(object)sliderValueText != (Object)null)
		{
			((TMP_Text)sliderValueText).text = "-";
		}
		TMP_Dropdown val = CreateInlineDropdown(row.transform);
		val.ClearOptions();
		val.AddOptions(new List<string> { "Select..." });
		val.AddOptions(orderedItems.Select(labelSelector).ToList());
		string selectedItem = null;
		Action clearSelection = delegate
		{
			selectedItem = null;
			((UnityEventBase)slider.onValueChanged).RemoveAllListeners();
			((Selectable)slider).interactable = false;
			slider.SetValueWithoutNotify(minValue);
			if ((Object)(object)sliderValueText != (Object)null)
			{
				((TMP_Text)sliderValueText).text = "-";
			}
		};
		((UnityEventBase)val.onValueChanged).RemoveAllListeners();
		((UnityEvent<int>)(object)val.onValueChanged).AddListener((UnityAction<int>)delegate(int index)
		{
			((UnityEventBase)slider.onValueChanged).RemoveAllListeners();
			if (index <= 0 || index - 1 >= orderedItems.Count)
			{
				PlayerPrefs.DeleteKey(selectedItemPrefKey);
				PlayerPrefs.Save();
				clearSelection();
			}
			else
			{
				selectedItem = orderedItems[index - 1];
				PlayerPrefs.SetString(selectedItemPrefKey, selectedItem);
				PlayerPrefs.Save();
				float num2 = Mathf.Clamp(getValue(selectedItem), minValue, maxValue);
				((Selectable)slider).interactable = true;
				slider.SetValueWithoutNotify(num2);
				if ((Object)(object)sliderValueText != (Object)null)
				{
					((TMP_Text)sliderValueText).text = formatter(num2);
				}
				((UnityEvent<float>)(object)slider.onValueChanged).AddListener((UnityAction<float>)delegate(float newValue)
				{
					float num3 = Mathf.Clamp(newValue, minValue, maxValue);
					setValue(selectedItem, num3);
					PlayerPrefs.Save();
					if ((Object)(object)sliderValueText != (Object)null)
					{
						((TMP_Text)sliderValueText).text = formatter(num3);
					}
				});
			}
		});
		string storedSelection = PlayerPrefs.GetString(selectedItemPrefKey, string.Empty);
		int num = orderedItems.FindIndex((string item) => string.Equals(item, storedSelection, StringComparison.Ordinal));
		if (num >= 0)
		{
			val.SetValueWithoutNotify(num + 1);
			((UnityEvent<int>)(object)val.onValueChanged).Invoke(num + 1);
		}
		else
		{
			clearSelection();
		}
	}

	private static void BindSoundOverrideRow(GameObject row)
	{
		//IL_011c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0132: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c5: Expected O, but got Unknown
		//IL_015d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0167: Unknown result type (might be due to invalid IL or missing references)
		//IL_017e: Unknown result type (might be due to invalid IL or missing references)
		Slider slider = ((Component)row.transform.GetChild(1)).GetComponent<Slider>();
		if ((Object)(object)slider == (Object)null)
		{
			return;
		}
		PrepareInlineSelectorRow(row);
		SoundLibrary.Load();
		TMP_Dropdown val = CreateInlineDropdown(row.transform);
		val.ClearOptions();
		List<string> list = new List<string> { "Select..." };
		list.AddRange(SoundLibrary.AllClipNames);
		val.AddOptions(list);
		slider.minValue = 0f;
		slider.maxValue = 1.2f;
		((Selectable)slider).interactable = false;
		TextMeshProUGUI valueText = ((Component)row.transform.GetChild(2)).GetComponent<TextMeshProUGUI>();
		if ((Object)(object)valueText != (Object)null)
		{
			((TMP_Text)valueText).text = "-";
		}
		Button playButton = CreatePlayButton(row.transform);
		((Selectable)playButton).interactable = false;
		RectTransform component = ((Component)playButton).GetComponent<RectTransform>();
		component.sizeDelta = new Vector2(76f, 26f);
		component.anchoredPosition = new Vector2(-20f, 0f);
		RectTransform component2 = ((Component)val).GetComponent<RectTransform>();
		if ((Object)(object)component2 != (Object)null)
		{
			component2.sizeDelta = new Vector2(180f, component2.sizeDelta.y);
			component2.anchoredPosition = new Vector2(-106f, 0f);
		}
		string currentSound = null;
		string currentSoundOriginal = null;
		((UnityEventBase)playButton.onClick).RemoveAllListeners();
		((UnityEvent)playButton.onClick).AddListener((UnityAction)delegate
		{
			//IL_0054: Unknown result type (might be due to invalid IL or missing references)
			if (!string.IsNullOrEmpty(currentSoundOriginal))
			{
				AudioClip val2 = Resources.Load<AudioClip>("Sounds/" + currentSoundOriginal) ?? Resources.Load<AudioClip>("Sounds/" + currentSoundOriginal.ToLowerInvariant());
				if ((Object)(object)val2 != (Object)null)
				{
					Sound.Play(val2, Vector2.zero, true, false, (Transform)null, 1f, 1f, true, false);
				}
			}
		});
		((UnityEventBase)val.onValueChanged).RemoveAllListeners();
		((UnityEvent<int>)(object)val.onValueChanged).AddListener((UnityAction<int>)delegate(int index)
		{
			((UnityEventBase)slider.onValueChanged).RemoveAllListeners();
			if (index <= 0 || index - 1 >= SoundLibrary.AllClipNames.Count)
			{
				currentSound = null;
				currentSoundOriginal = null;
				slider.SetValueWithoutNotify(0f);
				((Selectable)slider).interactable = false;
				((Selectable)playButton).interactable = false;
				if ((Object)(object)valueText != (Object)null)
				{
					((TMP_Text)valueText).text = "-";
				}
			}
			else
			{
				currentSoundOriginal = SoundLibrary.AllClipNames[index - 1];
				currentSound = currentSoundOriginal.ToLowerInvariant();
				float num = AudioSafetyPatcher.GetOverride(currentSound);
				((Selectable)slider).interactable = true;
				slider.SetValueWithoutNotify(num);
				((Selectable)playButton).interactable = true;
				if ((Object)(object)valueText != (Object)null)
				{
					((TMP_Text)valueText).text = FormatPercent(num, 1.2f);
				}
				((UnityEvent<float>)(object)slider.onValueChanged).AddListener((UnityAction<float>)delegate(float newValue)
				{
					AudioSafetyPatcher.SetOverride(currentSound, newValue);
					if ((Object)(object)valueText != (Object)null)
					{
						((TMP_Text)valueText).text = FormatPercent(newValue, 1.2f);
					}
				});
			}
		});
	}

	private static void PrepareInlineSelectorRow(GameObject row)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_010a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0120: Unknown result type (might be due to invalid IL or missing references)
		//IL_0136: Unknown result type (might be due to invalid IL or missing references)
		//IL_016d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0183: Unknown result type (might be due to invalid IL or missing references)
		//IL_0199: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cb: Unknown result type (might be due to invalid IL or missing references)
		RectTransform component = row.GetComponent<RectTransform>();
		component.sizeDelta = new Vector2(component.sizeDelta.x, 48f);
		RectTransform component2 = ((Component)row.transform.GetChild(0)).GetComponent<RectTransform>();
		if ((Object)(object)component2 != (Object)null)
		{
			component2.anchorMin = new Vector2(0f, 0.5f);
			component2.anchorMax = new Vector2(0f, 0.5f);
			component2.pivot = new Vector2(0f, 0.5f);
			component2.sizeDelta = new Vector2(320f, 44f);
			component2.anchoredPosition = new Vector2(14f, 0f);
		}
		RectTransform component3 = ((Component)row.transform.GetChild(1)).GetComponent<RectTransform>();
		if ((Object)(object)component3 != (Object)null)
		{
			component3.anchorMin = new Vector2(0f, 0.5f);
			component3.anchorMax = new Vector2(1f, 0.5f);
			component3.pivot = new Vector2(0.5f, 0.5f);
			component3.offsetMin = new Vector2(330f, -10f);
			component3.offsetMax = new Vector2(-410f, 10f);
		}
		RectTransform component4 = ((Component)row.transform.GetChild(2)).GetComponent<RectTransform>();
		if ((Object)(object)component4 != (Object)null)
		{
			component4.anchorMin = new Vector2(1f, 0.5f);
			component4.anchorMax = new Vector2(1f, 0.5f);
			component4.pivot = new Vector2(1f, 0.5f);
			component4.sizeDelta = new Vector2(76f, component4.sizeDelta.y);
			component4.anchoredPosition = new Vector2(-318f, 0f);
		}
	}

	private static TMP_Dropdown CreateInlineDropdown(Transform parent)
	{
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
		GameObject val = Utils.Create("Special/GameSettingDropdown", parent);
		if ((Object)(object)val == (Object)null)
		{
			return null;
		}
		Transform child = val.transform.GetChild(0);
		TextMeshProUGUI component = ((Component)child).GetComponent<TextMeshProUGUI>();
		if ((Object)(object)component != (Object)null)
		{
			((TMP_Text)component).text = string.Empty;
		}
		((Component)child).gameObject.SetActive(false);
		Transform child2 = val.transform.GetChild(1);
		child2.SetParent(parent, false);
		Object.Destroy((Object)(object)val);
		RectTransform component2 = ((Component)child2).GetComponent<RectTransform>();
		component2.anchorMin = new Vector2(1f, 0.5f);
		component2.anchorMax = new Vector2(1f, 0.5f);
		component2.pivot = new Vector2(1f, 0.5f);
		component2.sizeDelta = new Vector2(220f, component2.sizeDelta.y);
		component2.anchoredPosition = new Vector2(-20f, 0f);
		TMP_Dropdown component3 = ((Component)child2).GetComponent<TMP_Dropdown>();
		ScrollRect val2 = (((Object)(object)component3.template != (Object)null) ? ((Component)component3.template).GetComponent<ScrollRect>() : null);
		if ((Object)(object)val2 != (Object)null)
		{
			val2.vertical = true;
			val2.scrollSensitivity = 20f;
			component3.template.SetSizeWithCurrentAnchors((Axis)1, 220f);
			EnsureDropdownScrollbar(component3, val2);
		}
		return component3;
	}

	private static TextMeshProUGUI CreateAttachedText(Transform parent, string name, string initialText, Vector2? anchoredPosition = null, TextAlignmentOptions alignment = (TextAlignmentOptions)4097)
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Expected O, but got Unknown
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		GameObject val = new GameObject(name, new Type[2]
		{
			typeof(RectTransform),
			typeof(TextMeshProUGUI)
		});
		val.transform.SetParent(parent, false);
		RectTransform component = val.GetComponent<RectTransform>();
		component.anchorMin = new Vector2(1f, 0.5f);
		component.anchorMax = new Vector2(1f, 0.5f);
		component.pivot = new Vector2(1f, 0.5f);
		component.sizeDelta = new Vector2(260f, 26f);
		component.anchoredPosition = (Vector2)(((_003F?)anchoredPosition) ?? new Vector2(-20f, 0f));
		TextMeshProUGUI component2 = val.GetComponent<TextMeshProUGUI>();
		((TMP_Text)component2).text = initialText;
		((TMP_Text)component2).fontSize = 16f;
		((TMP_Text)component2).alignment = alignment;
		((Graphic)component2).raycastTarget = false;
		TMP_FontAsset val2 = ResourceLookupCache.FindFontContains("Retro GamingPix");
		if ((Object)(object)val2 != (Object)null)
		{
			((TMP_Text)component2).font = val2;
		}
		return component2;
	}

	private static Button CreatePlayButton(Transform parent)
	{
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Expected O, but got Unknown
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0113: Expected O, but got Unknown
		//IL_0130: Unknown result type (might be due to invalid IL or missing references)
		//IL_013d: Unknown result type (might be due to invalid IL or missing references)
		//IL_014a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0157: Unknown result type (might be due to invalid IL or missing references)
		GameObject val = new GameObject("QoLSoundOverridePlay", new Type[3]
		{
			typeof(RectTransform),
			typeof(Image),
			typeof(Button)
		});
		val.transform.SetParent(parent, false);
		RectTransform component = val.GetComponent<RectTransform>();
		component.anchorMin = new Vector2(1f, 0.5f);
		component.anchorMax = new Vector2(1f, 0.5f);
		component.pivot = new Vector2(1f, 0.5f);
		component.sizeDelta = new Vector2(90f, 26f);
		component.anchoredPosition = new Vector2(-20f, 0f);
		Image component2 = val.GetComponent<Image>();
		component2.sprite = ResourceLookupCache.FindSpriteContains("uiBlockNano");
		component2.type = (Type)1;
		((Graphic)component2).color = Color.white;
		GameObject val2 = new GameObject("Label", new Type[2]
		{
			typeof(RectTransform),
			typeof(TextMeshProUGUI)
		});
		val2.transform.SetParent(val.transform, false);
		RectTransform component3 = val2.GetComponent<RectTransform>();
		component3.anchorMin = Vector2.zero;
		component3.anchorMax = Vector2.one;
		component3.offsetMin = Vector2.zero;
		component3.offsetMax = Vector2.zero;
		TextMeshProUGUI component4 = val2.GetComponent<TextMeshProUGUI>();
		((TMP_Text)component4).text = "Play";
		((TMP_Text)component4).fontSize = 16f;
		((TMP_Text)component4).alignment = (TextAlignmentOptions)514;
		((Graphic)component4).raycastTarget = false;
		TMP_FontAsset val3 = ResourceLookupCache.FindFontContains("Retro GamingPix");
		if ((Object)(object)val3 != (Object)null)
		{
			((TMP_Text)component4).font = val3;
		}
		return val.GetComponent<Button>();
	}

	private static string FormatPercent(float value)
	{
		return $"{Mathf.RoundToInt(Mathf.Clamp01(value) * 100f)}%";
	}

	private static string FormatPercent(float value, float maxValue)
	{
		return $"{Mathf.RoundToInt(Mathf.Clamp(value, 0f, maxValue) * 100f)}%";
	}

	private static void EnsureDropdownScrollbar(TMP_Dropdown dropdown, ScrollRect scrollRect)
	{
		//IL_011e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0125: Expected O, but got Unknown
		//IL_014e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0165: Unknown result type (might be due to invalid IL or missing references)
		//IL_017c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0193: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_020b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0212: Expected O, but got Unknown
		//IL_0232: Unknown result type (might be due to invalid IL or missing references)
		//IL_023f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0256: Unknown result type (might be due to invalid IL or missing references)
		//IL_026d: Unknown result type (might be due to invalid IL or missing references)
		//IL_029d: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a4: Expected O, but got Unknown
		//IL_02ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_02fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0313: Unknown result type (might be due to invalid IL or missing references)
		//IL_035d: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)dropdown == (Object)null || (Object)(object)scrollRect == (Object)null || (Object)(object)dropdown.template == (Object)null)
		{
			return;
		}
		if ((Object)(object)scrollRect.verticalScrollbar != (Object)null)
		{
			((Component)scrollRect.verticalScrollbar).gameObject.SetActive(true);
			return;
		}
		Transform val = ((Transform)dropdown.template).Find("Viewport");
		if (!((Object)(object)val == (Object)null))
		{
			Image component = ((Component)dropdown.template).GetComponent<Image>();
			Sprite val2 = (((Object)(object)component != (Object)null) ? component.sprite : null);
			Image component2 = ((Component)dropdown).GetComponent<Image>();
			Sprite val3 = (((Object)(object)component2 != (Object)null) ? component2.sprite : val2);
			RectTransform val4 = (RectTransform)(object)((val is RectTransform) ? val : null);
			if ((Object)(object)val4 != (Object)null)
			{
				val4.offsetMax = new Vector2(-18f, val4.offsetMax.y);
			}
			GameObject val5 = new GameObject("Scrollbar", new Type[3]
			{
				typeof(RectTransform),
				typeof(Image),
				typeof(Scrollbar)
			});
			val5.transform.SetParent((Transform)(object)dropdown.template, false);
			RectTransform component3 = val5.GetComponent<RectTransform>();
			component3.anchorMin = new Vector2(1f, 0f);
			component3.anchorMax = new Vector2(1f, 1f);
			component3.pivot = new Vector2(1f, 1f);
			component3.sizeDelta = new Vector2(18f, 0f);
			component3.anchoredPosition = Vector2.zero;
			Image component4 = val5.GetComponent<Image>();
			component4.sprite = val2;
			component4.type = (Type)(((Object)(object)val2 != (Object)null) ? 1 : 0);
			((Graphic)component4).color = new Color(1f, 1f, 1f, 0.9f);
			GameObject val6 = new GameObject("Sliding Area", new Type[1] { typeof(RectTransform) });
			val6.transform.SetParent(val5.transform, false);
			RectTransform component5 = val6.GetComponent<RectTransform>();
			component5.anchorMin = Vector2.zero;
			component5.anchorMax = Vector2.one;
			component5.offsetMin = new Vector2(2f, 2f);
			component5.offsetMax = new Vector2(-2f, -2f);
			GameObject val7 = new GameObject("Handle", new Type[2]
			{
				typeof(RectTransform),
				typeof(Image)
			});
			val7.transform.SetParent(val6.transform, false);
			RectTransform component6 = val7.GetComponent<RectTransform>();
			component6.anchorMin = new Vector2(0f, 1f);
			component6.anchorMax = new Vector2(1f, 1f);
			component6.pivot = new Vector2(0.5f, 1f);
			component6.sizeDelta = new Vector2(0f, 32f);
			Image component7 = val7.GetComponent<Image>();
			component7.sprite = val3;
			component7.type = (Type)(((Object)(object)val3 != (Object)null) ? 1 : 0);
			((Graphic)component7).color = new Color(0.85f, 0.85f, 0.85f, 1f);
			Scrollbar component8 = val5.GetComponent<Scrollbar>();
			component8.direction = (Direction)2;
			component8.handleRect = component6;
			((Selectable)component8).targetGraphic = (Graphic)(object)component7;
			component8.size = 0.2f;
			component8.numberOfSteps = 0;
			scrollRect.verticalScrollbar = component8;
			scrollRect.verticalScrollbarVisibility = (ScrollbarVisibility)2;
			scrollRect.movementType = (MovementType)2;
		}
	}

	private static string FormatPercentExtended(float value)
	{
		return FormatPercentExtended(value, 5f);
	}

	private static string FormatPercentExtended(float value, float maxValue)
	{
		return $"{Mathf.RoundToInt(Mathf.Clamp(value, 0f, maxValue) * 100f)}%";
	}
}
