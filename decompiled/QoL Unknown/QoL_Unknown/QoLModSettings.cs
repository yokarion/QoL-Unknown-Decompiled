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

	internal const string BestToolByEfficiencyPrefKey = "Setting_BestToolByEfficiency";

	private const string UiScalePrefKey = "UIScale";

	private const string ShowFpsPrefKey = "ShowFPSCounter";

	private const string LightIntensityPrefKey = "LightIntensity";

	private const string StartStrPrefKey = "StartSTR";

	private const string StartResPrefKey = "StartRES";

	private const string StartIntPrefKey = "StartINT";

	private const int StartingStatUnset = -1;

	private const string StartingStatLegacyMigratedPrefKey = "qol.startingstat.legacy.migrated";

	private static readonly List<KeyValuePair<string, int>> LegacyStartingStats = new List<KeyValuePair<string, int>>();

	private const string SelectedEntityDensityPrefKey = "SelectedEntityDensityTarget";

	private const string SelectedStructureDensityPrefKey = "SelectedStructureDensityTarget";

	private const string SelectedLayerModifierFrequencyPrefKey = "SelectedLayerModifierFrequencyTarget";

	internal const string ExperimentalFeaturesPrefKey = "ExperimentalFeatures";

	internal const string StackLiquidContainersPrefKey = "qol.game.stackliquidcontainers";

	private const string StackExpandEnabledPrefKey = "qol.game.stackexpandenabled";

	private const string StackExpandKeyPrefKey = "qol.game.stackexpandkey";

	private const string StackExpandSettingId = "qol.game.stackexpand";

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
			RegisterStandardSettings();
			RegisterCustomSettings();
			ModManagementSettings.Initialize(logger);
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
		ModOptionsMenu.Register(ModOptionDefinition.Bool("qol.video.camerazoom", "Enable zoom in/out", "Hold + or - to smoothly zoom the camera in or out (numpad supported). Disables pixel snapping.", (SettingCategory)0, defaultValue: false, delegate(bool value)
		{
			CameraZoomPatcher.SetEnabled(value);
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
		ModOptionsMenu.Register(ModOptionDefinition.Bool("qol.game.emetophobia", "Emetophobia", "Prevents vomit and blood vomit visuals, particles, and text. Stat loss is preserved.", (SettingCategory)2, defaultValue: false, delegate(bool value)
		{
			EmetophobiaPatcher.SetEnabled(value);
		}));
		ModOptionsMenu.Register(ModOptionDefinition.Bool("qol.game.achievementswithcheats", "Keep achievements with console commands", "Allows achievements to keep unlocking after using console/cheat commands, instead of disabling them for the rest of the session.", (SettingCategory)2, defaultValue: false, delegate(bool value)
		{
			AchievementCheatsPatcher.SetEnabled(value);
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
		ModOptionsMenu.Register(ModOptionDefinition.Bool("qol.game.besttoolefficiency", "Use best tool by efficiency", "Crafting selects the most efficient tool rather than the first one found.", (SettingCategory)2, defaultValue: true, delegate(bool value)
		{
			PlayerPrefs.SetInt("Setting_BestToolByEfficiency", value ? 1 : 0);
			PlayerPrefs.Save();
		}));
		ModOptionsMenu.Register(ModOptionDefinition.Bool("qol.game.additionalrecipes", "Additional Recipe Options", "Shows the bookmark and uncrafted recipe buttons in the crafting menu.", (SettingCategory)2, defaultValue: true, delegate(bool value)
		{
			PlayerPrefs.SetInt("qol_craft_uncrafted", value ? 1 : 0);
			PlayerPrefs.Save();
		}));
		ModOptionsMenu.Register(ModOptionDefinition.Bool("qol.game.containerscroll", "Container Scroll", "Adds scroll wheel support to containers.", (SettingCategory)2, defaultValue: true, delegate(bool value)
		{
			PlayerPrefs.SetInt("qol_ui_scroll", value ? 1 : 0);
			PlayerPrefs.Save();
		}));
		ModOptionsMenu.Register(ModOptionDefinition.Bool("qol.game.categorytint", "Automatically color-code items by type", "Tints inventory slots a light color based on item category (tool, medicine, food, etc.). Manual color-codes and favourites keep their color.", (SettingCategory)2, defaultValue: false, delegate(bool value)
		{
			PlayerPrefs.SetInt("qol.game.categorytint", value ? 1 : 0);
			PlayerPrefs.Save();
		}));
		ModOptionsMenu.Register(ModOptionDefinition.Dropdown("qol.game.preventcreaturegriefing", "Prevent Creature Griefing", "Prevents certain creatures from causing world damage, excluding the elder thornback.", (SettingCategory)2, 0, new ModDropdownChoice[4]
		{
			new ModDropdownChoice("off", "Off"),
			new ModDropdownChoice("items", "Items"),
			new ModDropdownChoice("blocks", "Blocks"),
			new ModDropdownChoice("both", "Items and Blocks")
		}, delegate(int value)
		{
			PlayerPrefs.SetInt("qol.game.preventcreaturegriefing", value);
			PlayerPrefs.Save();
		}));
		RegisterStartingStat("qol.game.skillallocation.str", "Starting STR", "StartSTR", 9);
		RegisterStartingStat("qol.game.skillallocation.res", "Starting RES", "StartRES", 8);
		RegisterStartingStat("qol.game.skillallocation.int", "Starting INT", "StartINT", 7);
		ModOptionsMenu.SettingsLoaded += MigrateLegacyStartingStats;
		MigrateLegacyStartingStats();
	}

	private static void RegisterStartingStat(string id, string label, string prefKey, int legacyDefault)
	{
		ModOptionDefinition modOptionDefinition = ModOptionDefinition.Int(id, label, "Sets the starting stat value used by the QoL skill allocation patch. Leave blank to use the character's default stats.", (SettingCategory)2, -1, -1, 20, delegate(int value)
		{
			if (value < 0)
			{
				PlayerPrefs.DeleteKey(prefKey);
			}
			else
			{
				PlayerPrefs.SetInt(prefKey, Mathf.Clamp(value, 0, 20));
			}
			PlayerPrefs.Save();
		});
		modOptionDefinition.IntRowBinder = ModOptionsMenu.BindBlankAwareIntInput;
		LegacyStartingStats.Add(new KeyValuePair<string, int>(id, legacyDefault));
		ModOptionsMenu.Register(modOptionDefinition);
	}

	private static void MigrateLegacyStartingStats()
	{
		if (Settings.settings == null || PlayerPrefs.GetInt("qol.startingstat.legacy.migrated", 0) == 1)
		{
			return;
		}
		PlayerPrefs.SetInt("qol.startingstat.legacy.migrated", 1);
		PlayerPrefs.Save();
		bool flag = false;
		for (int i = 0; i < LegacyStartingStats.Count; i++)
		{
			SettingInt val = Settings.Get<SettingInt>(LegacyStartingStats[i].Key);
			if (val != null && val.value == LegacyStartingStats[i].Value)
			{
				((Setting)val).SetValue((object)(-1));
				((Setting)val).Apply();
				flag = true;
			}
		}
		if (flag)
		{
			Settings.SaveSettings();
		}
	}

	private static void RegisterCustomSettings()
	{
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0118: Unknown result type (might be due to invalid IL or missing references)
		//IL_0168: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_021a: Unknown result type (might be due to invalid IL or missing references)
		//IL_027c: Unknown result type (might be due to invalid IL or missing references)
		ModOptionsMenu.RegisterCustom(new ModCustomSettingDefinition
		{
			Id = "qol.game.downloadlocale",
			Label = "Download Locale",
			Description = "Downloads and installs additional locale files from the locale repository. Warning: WILL RESET THE LAYER IF USED IN-GAME",
			Category = (SettingCategory)4,
			TemplateResourcePath = "Special/GameSettingFloat",
			Binder = BindLocaleRow
		});
		ModOptionsMenu.RegisterCustom(new ModCustomSettingDefinition
		{
			Id = "qol.game.entitydensity",
			Label = "Individual Entity Density",
			Description = "Adjust any entity's spawn multiplier.",
			Category = (SettingCategory)2,
			TemplateResourcePath = "Special/GameSettingFloat",
			Binder = BindEntityDensityRow
		});
		ModOptionsMenu.RegisterCustom(new ModCustomSettingDefinition
		{
			Id = "qol.game.structuredensity",
			Label = "Individual Structure Density",
			Description = "Adjust any structure's spawn multiplier.",
			Category = (SettingCategory)2,
			TemplateResourcePath = "Special/GameSettingFloat",
			Binder = BindStructureDensityRow
		});
		ModOptionsMenu.RegisterCustom(new ModCustomSettingDefinition
		{
			Id = "qol.game.layermodifierdensity",
			Label = "Layer Modifier Frequency",
			Description = "Adjust the frequency of layer modifiers.",
			Category = (SettingCategory)2,
			TemplateResourcePath = "Special/GameSettingFloat",
			Binder = BindLayerModifierFrequencyRow
		});
		ModOptionsMenu.RegisterCustom(new ModCustomSettingDefinition
		{
			Id = "qol.audio.soundoverride",
			Label = "Individual Sound Overrides",
			Description = "Adjust the volume of any specific sound",
			Category = (SettingCategory)1,
			TemplateResourcePath = "Special/GameSettingFloat",
			Binder = BindSoundOverrideRow
		});
		ModOptionsMenu.RegisterCustom(new ModCustomSettingDefinition
		{
			Id = "qol.game.discordpresence",
			Label = "Discord Presence",
			Description = "Edit Discord Rich Presence templates for details, state, and image tooltips.",
			Category = (SettingCategory)2,
			TemplateResourcePath = "Special/GameSettingInput",
			Binder = BindDiscordPresenceRow,
			IsVisible = AreExperimentalFeaturesEnabled
		});
		ModOptionsMenu.RegisterCustom(new ModCustomSettingDefinition
		{
			Id = "qol.game.stackliquidcontainers",
			Label = "Stack Liquid Containers",
			Description = "Groups same-type bottles and canteens into a single container slot even when their fill levels differ. Hover the stack to reveal each container.",
			Category = (SettingCategory)2,
			TemplateResourcePath = "Special/GameSettingBool",
			Binder = BindStackLiquidContainersRow,
			IsVisible = IsContainerStackingEnabled
		});
		ModOptionsMenu.RegisterCustom(new ModCustomSettingDefinition
		{
			Id = "qol.game.stackexpand",
			Label = "Hold Key to Expand Stacks",
			Description = "Adds a checkbox and a keybind. With the checkbox on, a stack only opens while you hold that key instead of on plain hover.",
			Category = (SettingCategory)2,
			TemplateResourcePath = "Special/GameSettingInput",
			Binder = BindStackExpandRow,
			IsVisible = IsContainerStackingEnabled
		});
	}

	internal static bool StackExpandEnabled()
	{
		return PlayerPrefs.GetInt("qol.game.stackexpandenabled", 0) == 1;
	}

	internal static KeyCode StackExpandKey()
	{
		return (KeyCode)PlayerPrefs.GetInt("qol.game.stackexpandkey", 306);
	}

	internal static bool StackExpandAllowed()
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		if (StackExpandEnabled())
		{
			return ModKeybinds.Held(StackExpandKey());
		}
		return true;
	}

	private static void BindStackExpandRow(GameObject row)
	{
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Expected O, but got Unknown
		//IL_010e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0123: Unknown result type (might be due to invalid IL or missing references)
		//IL_0138: Unknown result type (might be due to invalid IL or missing references)
		//IL_0149: Unknown result type (might be due to invalid IL or missing references)
		//IL_0153: Unknown result type (might be due to invalid IL or missing references)
		//IL_0168: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c4: Expected O, but got Unknown
		//IL_01ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_0203: Unknown result type (might be due to invalid IL or missing references)
		//IL_0218: Unknown result type (might be due to invalid IL or missing references)
		//IL_022d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0241: Unknown result type (might be due to invalid IL or missing references)
		Button val = ((row.transform.childCount > 1) ? ((Component)row.transform.GetChild(1)).GetComponent<Button>() : null);
		if ((Object)(object)val == (Object)null)
		{
			return;
		}
		SettingKeybind keybind = new SettingKeybind
		{
			name = "qol.game.stackexpand",
			category = (SettingCategory)2,
			value = StackExpandKey()
		};
		((Setting)keybind).apply = delegate
		{
			//IL_000b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0015: Expected I4, but got Unknown
			PlayerPrefs.SetInt("qol.game.stackexpandkey", (int)keybind.value);
			PlayerPrefs.Save();
		};
		RectTransform component = ((Component)row.transform.GetChild(0)).GetComponent<RectTransform>();
		if ((Object)(object)component != (Object)null)
		{
			component.anchorMin = new Vector2(0f, 0.5f);
			component.anchorMax = new Vector2(0f, 0.5f);
			component.pivot = new Vector2(0f, 0.5f);
			component.sizeDelta = new Vector2(320f, component.sizeDelta.y);
			component.anchoredPosition = new Vector2(14f, 0f);
		}
		RectTransform component2 = ((Component)val).GetComponent<RectTransform>();
		component2.anchorMin = new Vector2(1f, 0.5f);
		component2.anchorMax = new Vector2(1f, 0.5f);
		component2.pivot = new Vector2(1f, 0.5f);
		component2.sizeDelta = new Vector2(120f, component2.sizeDelta.y);
		component2.anchoredPosition = new Vector2(-20f, 0f);
		TextMeshProUGUI keyText = ((Component)val).GetComponentInChildren<TextMeshProUGUI>();
		if ((Object)(object)keyText != (Object)null)
		{
			((TMP_Text)keyText).text = keybind.GetDisplayString();
		}
		((UnityEventBase)val.onClick).RemoveAllListeners();
		((UnityEvent)val.onClick).AddListener((UnityAction)delegate
		{
			SharedCoroutineStarter.StartCoroutine(KeybindCombos.Capture(keybind, keyText));
		});
		Toggle val2 = CreateInlineToggle(row.transform);
		if (!((Object)(object)val2 == (Object)null))
		{
			RectTransform component3 = ((Component)val2).GetComponent<RectTransform>();
			component3.anchorMin = new Vector2(1f, 0.5f);
			component3.anchorMax = new Vector2(1f, 0.5f);
			component3.pivot = new Vector2(1f, 0.5f);
			component3.sizeDelta = new Vector2(30f, 30f);
			component3.anchoredPosition = new Vector2(-152f, 0f);
			TextMeshProUGUI componentInChildren = ((Component)val2).GetComponentInChildren<TextMeshProUGUI>(true);
			if ((Object)(object)componentInChildren != (Object)null)
			{
				((TMP_Text)componentInChildren).text = string.Empty;
			}
			((UnityEventBase)val2.onValueChanged).RemoveAllListeners();
			val2.SetIsOnWithoutNotify(StackExpandEnabled());
			((UnityEvent<bool>)(object)val2.onValueChanged).AddListener((UnityAction<bool>)delegate(bool isOn)
			{
				PlayerPrefs.SetInt("qol.game.stackexpandenabled", isOn ? 1 : 0);
				PlayerPrefs.Save();
			});
		}
	}

	private static Toggle CreateInlineToggle(Transform parent)
	{
		GameObject val = Utils.Create("Special/GameSettingBool", parent);
		if ((Object)(object)val == (Object)null)
		{
			return null;
		}
		((Component)val.transform.GetChild(0)).gameObject.SetActive(false);
		Transform child = val.transform.GetChild(1);
		child.SetParent(parent, false);
		Object.Destroy((Object)(object)val);
		return ((Component)child).GetComponent<Toggle>();
	}

	internal static bool StackLiquidContainersEnabled()
	{
		return PlayerPrefs.GetInt("qol.game.stackliquidcontainers", 0) == 1;
	}

	private static bool IsContainerStackingEnabled()
	{
		return QoLModuleManager.IsEnabled("qol_inv_stacking");
	}

	private static void BindStackLiquidContainersRow(GameObject row)
	{
		Toggle component = ((Component)row.transform.GetChild(1)).GetComponent<Toggle>();
		if ((Object)(object)component == (Object)null)
		{
			return;
		}
		((UnityEventBase)component.onValueChanged).RemoveAllListeners();
		component.SetIsOnWithoutNotify(StackLiquidContainersEnabled());
		((UnityEvent<bool>)(object)component.onValueChanged).AddListener((UnityAction<bool>)delegate(bool isOn)
		{
			PlayerPrefs.SetInt("qol.game.stackliquidcontainers", isOn ? 1 : 0);
			PlayerPrefs.Save();
			PlayerCamera main = PlayerCamera.main;
			if ((Object)(object)main != (Object)null && (Object)(object)main.containerMenu != (Object)null && main.containerMenu.activeSelf && (Object)(object)main.currentContainer != (Object)null)
			{
				main.RepopulateContainer();
			}
		});
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
		PlayerPrefs.DeleteKey("qol.game.stackliquidcontainers");
		PlayerPrefs.DeleteKey("qol.game.stackexpandenabled");
		PlayerPrefs.DeleteKey("qol.game.stackexpandkey");
		PlayerPrefs.DeleteKey("qol.game.preventcreaturegriefing");
		EntitySpawnManager.ResetMultipliers();
		StructureSpawnManager.ResetMultipliers();
		LayerModifierPatcher.ResetMultipliers();
		AudioSafetyPatcher.ClearAllOverrides();
		PlayerPrefs.Save();
	}

	private static void BindDiscordPresenceRow(GameObject row)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_0101: Unknown result type (might be due to invalid IL or missing references)
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
		//IL_0127: Unknown result type (might be due to invalid IL or missing references)
		//IL_0131: Unknown result type (might be due to invalid IL or missing references)
		//IL_0146: Unknown result type (might be due to invalid IL or missing references)
		//IL_0186: Unknown result type (might be due to invalid IL or missing references)
		//IL_0190: Expected O, but got Unknown
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
		BindDensityRow(row, EntitySpawnManager.GetAllEntities(), "SelectedEntityDensityTarget", (string entity) => entity, (string entity) => EntitySpawnManager.GetMultiplier(entity), delegate(string entity, float value)
		{
			EntitySpawnManager.SetMultiplier(entity, value);
		}, 0f, 5f, FormatPercentExtended);
	}

	private static void BindStructureDensityRow(GameObject row)
	{
		BindDensityRow(row, StructureSpawnManager.GetAllStructures(), "SelectedStructureDensityTarget", (string structure) => structure.Substring(structure.LastIndexOf('/') + 1), (string structure) => StructureSpawnManager.GetMultiplier(structure), delegate(string structure, float value)
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
		//IL_0110: Unknown result type (might be due to invalid IL or missing references)
		//IL_0124: Unknown result type (might be due to invalid IL or missing references)
		//IL_019e: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a8: Expected O, but got Unknown
		//IL_0145: Unknown result type (might be due to invalid IL or missing references)
		//IL_014f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0164: Unknown result type (might be due to invalid IL or missing references)
		Slider slider = ((Component)row.transform.GetChild(1)).GetComponent<Slider>();
		if ((Object)(object)slider == (Object)null)
		{
			return;
		}
		PrepareInlineSelectorRow(row);
		SoundLibrary.Load();
		List<string> clipNames = SoundLibrary.AllClipNames.ToList();
		TMP_Dropdown obj = CreateInlineDropdown(row.transform);
		obj.ClearOptions();
		List<string> list = new List<string> { "Select..." };
		list.AddRange(clipNames);
		obj.AddOptions(list);
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
		RectTransform component2 = ((Component)obj).GetComponent<RectTransform>();
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
			//IL_0024: Unknown result type (might be due to invalid IL or missing references)
			if (!string.IsNullOrEmpty(currentSoundOriginal))
			{
				AudioClip previewClip = SoundLibrary.GetPreviewClip(currentSoundOriginal);
				if ((Object)(object)previewClip != (Object)null)
				{
					Sound.Play(previewClip, Vector2.zero, true, false, (Transform)null, 1f, 1f, true, false);
				}
			}
		});
		((UnityEventBase)obj.onValueChanged).RemoveAllListeners();
		((UnityEvent<int>)(object)obj.onValueChanged).AddListener((UnityAction<int>)delegate(int index)
		{
			((UnityEventBase)slider.onValueChanged).RemoveAllListeners();
			if (index <= 0 || index - 1 >= clipNames.Count)
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
				currentSoundOriginal = clipNames[index - 1];
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
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0109: Unknown result type (might be due to invalid IL or missing references)
		//IL_011e: Unknown result type (might be due to invalid IL or missing references)
		//IL_014e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0163: Unknown result type (might be due to invalid IL or missing references)
		//IL_0178: Unknown result type (might be due to invalid IL or missing references)
		//IL_0189: Unknown result type (might be due to invalid IL or missing references)
		//IL_0193: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a8: Unknown result type (might be due to invalid IL or missing references)
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
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
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
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
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
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Expected O, but got Unknown
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0104: Unknown result type (might be due to invalid IL or missing references)
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
		//IL_011d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0128: Unknown result type (might be due to invalid IL or missing references)
		//IL_0133: Unknown result type (might be due to invalid IL or missing references)
		//IL_013d: Unknown result type (might be due to invalid IL or missing references)
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
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fe: Expected O, but got Unknown
		//IL_0123: Unknown result type (might be due to invalid IL or missing references)
		//IL_0138: Unknown result type (might be due to invalid IL or missing references)
		//IL_014d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0162: Unknown result type (might be due to invalid IL or missing references)
		//IL_016c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d4: Expected O, but got Unknown
		//IL_01f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0210: Unknown result type (might be due to invalid IL or missing references)
		//IL_0224: Unknown result type (might be due to invalid IL or missing references)
		//IL_0253: Unknown result type (might be due to invalid IL or missing references)
		//IL_0258: Unknown result type (might be due to invalid IL or missing references)
		//IL_026b: Unknown result type (might be due to invalid IL or missing references)
		//IL_027f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0295: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0306: Unknown result type (might be due to invalid IL or missing references)
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
