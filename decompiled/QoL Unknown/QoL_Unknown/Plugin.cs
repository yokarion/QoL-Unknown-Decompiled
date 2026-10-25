using System;
using BepInEx;
using UnityEngine;

namespace QoL_Unknown;

[BepInPlugin("org.bepinex.plugins.qol_unknown", "QoL Unknown", "1.0.5.2")]
[BepInDependency(/*Could not decode attribute arguments.*/)]
[BepInDependency(/*Could not decode attribute arguments.*/)]
public class Plugin : BaseUnityPlugin
{
	private static int patchersInitialized;

	private static int patchersTotal;

	private void Awake()
	{
		((BaseUnityPlugin)this).Logger.LogInfo((object)"Mod QoLified to run!");
		InitializeComponent("ModLocale", delegate
		{
			ModLocale.Initialize(((BaseUnityPlugin)this).Logger);
		});
		InitializeComponent("QoLModuleManager", delegate
		{
			QoLModuleManager.SetupRegistry(((BaseUnityPlugin)this).Logger);
		});
		MultiplayerSaveBundle.SetLogger(((BaseUnityPlugin)this).Logger);
		InitializeComponent("MultiplayerCompat", delegate
		{
			MultiplayerCompat.Initialize(((BaseUnityPlugin)this).Logger);
		});
		InitializeComponent("Together command receivers", MultiplayerCommandUtil.EnsureReceiversRegistered);
		InitializeComponent("EnhancedMenuController", delegate
		{
			EnhancedMenuController.Initialize(((BaseUnityPlugin)this).Logger);
		});
		InitializeComponent("AntiGlarePatcher", delegate
		{
			AntiGlarePatcher.Initialize(((BaseUnityPlugin)this).Logger);
		});
		InitializeComponent("PreRunScriptPatcher", delegate
		{
			PreRunScriptPatcher.Initialize(((BaseUnityPlugin)this).Logger);
		});
		InitializeComponent("UpdateChecker", delegate
		{
			UpdateChecker.Initialize(((BaseUnityPlugin)this).Logger);
		});
		InitializeComponent("ConsoleScriptPatcher", delegate
		{
			ConsoleScriptPatcher.Initialize(((BaseUnityPlugin)this).Logger);
		});
		InitializeComponent("LineOfSightPatcher", delegate
		{
			LineOfSightPatcher.Initialize(((BaseUnityPlugin)this).Logger);
		});
		InitializeComponent("GroundContainerPickupPatcher", delegate
		{
			GroundContainerPickupPatcher.Initialize(((BaseUnityPlugin)this).Logger);
		});
		InitializeComponent("KeybindsPatcher", delegate
		{
			KeybindsPatcher.Initialize(((BaseUnityPlugin)this).Logger);
		});
		InitializeComponent("InventorySortPatcher", delegate
		{
			InventorySortPatcher.Initialize(((BaseUnityPlugin)this).Logger);
		});
		InitializeComponent("IlmeniteWorldGenPatcher", delegate
		{
			IlmeniteWorldGenPatcher.Initialize(((BaseUnityPlugin)this).Logger);
		});
		InitializeComponent("RadlinePatcher", delegate
		{
			RadlinePatcher.Initialize(((BaseUnityPlugin)this).Logger);
		});
		InitializeComponent("SeededRunPatcher", delegate
		{
			SeededRunPatcher.Initialize(((BaseUnityPlugin)this).Logger);
		});
		InitializeComponent("PreGenPatcher", delegate
		{
			PreGenPatcher.Initialize(((BaseUnityPlugin)this).Logger);
		});
		InitializeComponent("RecipeFixPatcher", delegate
		{
			RecipeFixPatcher.Initialize(((BaseUnityPlugin)this).Logger);
		});
		InitializeComponent("SaveSaveMenuPatcher", delegate
		{
			SaveSaveMenuPatcher.Initialize(((BaseUnityPlugin)this).Logger);
		});
		InitializeComponent("SaveSystemPatcher", delegate
		{
			SaveSystemPatcher.Initialize(((BaseUnityPlugin)this).Logger);
		});
		InitializeComponent("SaveMenuPatcher", delegate
		{
			SaveMenuPatcher.Initialize(((BaseUnityPlugin)this).Logger);
		});
		InitializeComponent("PauseMenuDialoguePatcher", delegate
		{
			PauseMenuDialoguePatcher.Initialize(((BaseUnityPlugin)this).Logger);
		});
		InitializeComponent("SaveManager", delegate
		{
			SaveManager.Initialize(((BaseUnityPlugin)this).Logger);
		});
		InitializeComponent("CasualtyReportPatcher", delegate
		{
			CasualtyReportPatcher.Initialize(((BaseUnityPlugin)this).Logger);
		});
		InitializeComponent("LayerTransitionPatcher", delegate
		{
			LayerTransitionPatcher.Initialize(((BaseUnityPlugin)this).Logger);
		});
		InitializeComponent("SavePanelPatcher", delegate
		{
			SavePanelPatcher.Initialize(((BaseUnityPlugin)this).Logger);
		});
		InitializeComponent("ControllerPatcher", delegate
		{
			ControllerPatcher.Initialize(((BaseUnityPlugin)this).Logger);
		});
		InitializeComponent("ControllerItemHighlightPatcher", delegate
		{
			ControllerItemHighlightPatcher.Initialize(((BaseUnityPlugin)this).Logger);
		});
		InitializeComponent("AltItemHighlightPatcher", delegate
		{
			AltItemHighlightPatcher.Initialize(((BaseUnityPlugin)this).Logger);
		});
		InitializeComponent("WearableCarouselPatcher", delegate
		{
			WearableCarouselPatcher.Initialize(((BaseUnityPlugin)this).Logger);
		});
		InitializeComponent("QoLSettingsMenu", delegate
		{
			QoLSettingsMenu.Initialize(((BaseUnityPlugin)this).Logger);
		});
		InitializeComponent("EmbeddedDllLoader", EmbeddedDllLoader.Init);
		InitializeComponent("FontPatcher", delegate
		{
			FontPatcher.Initialize(((BaseUnityPlugin)this).Logger);
		});
		InitializeComponent("EntitySpawnPatcher", delegate
		{
			EntitySpawnPatcher.Initialize(((BaseUnityPlugin)this).Logger);
		});
		InitializeComponent("StructureSpawnPatcher", delegate
		{
			StructureSpawnPatcher.Initialize(((BaseUnityPlugin)this).Logger);
		});
		InitializeComponent("SkillsPatcher", delegate
		{
			SkillsPatcher.Initialize(((BaseUnityPlugin)this).Logger);
		});
		InitializeComponent("ExitConfirmationPatcher", delegate
		{
			ExitConfirmationPatcher.Initialize(((BaseUnityPlugin)this).Logger);
		});
		InitializeComponent("SpawnAutofill", delegate
		{
			SpawnAutofill.Initialize(((BaseUnityPlugin)this).Logger);
		});
		InitializeComponent("AudioSafetyPatcher", delegate
		{
			AudioSafetyPatcher.Initialize(((BaseUnityPlugin)this).Logger);
		});
		InitializeComponent("LayerModifierPatcher", delegate
		{
			LayerModifierPatcher.Initialize(((BaseUnityPlugin)this).Logger);
		});
		InitializeComponent("MiscPatches", delegate
		{
			MiscPatches.Initialize(((BaseUnityPlugin)this).Logger);
		});
		InitializeComponent("CraftFavourites", delegate
		{
			CraftFavourites.Initialize(((BaseUnityPlugin)this).Logger);
		});
		InitializeComponent("MultiplayerHoverArbitrationPatcher", delegate
		{
			MultiplayerHoverArbitrationPatcher.Initialize(((BaseUnityPlugin)this).Logger);
		});
		InitializeComponent("MultiplayerSaveLoadPatcher", MultiplayerSaveLoadPatcher.Initialize);
		InitializeComponent("MinigameQoLPatcher", delegate
		{
			MinigameQoLPatcher.Initialize(((BaseUnityPlugin)this).Logger);
		});
		InitializeComponent("EtomophobiaPatcher", delegate
		{
			EtomophobiaPatcher.Initialize(((BaseUnityPlugin)this).Logger);
		});
		InitializeComponent("TutorialSelectionPatcher", delegate
		{
			TutorialSelectionPatcher.Initialize(((BaseUnityPlugin)this).Logger);
		});
		foreach (QoLModuleDef module in QoLModuleManager.Modules)
		{
			if (module.IsCore || QoLModuleManager.IsEnabled(module.ID))
			{
				InitializeComponent("module '" + module.ID + "'", delegate
				{
					module.InitAction?.Invoke(((BaseUnityPlugin)this).Logger);
				});
			}
		}
		string arg = ((patchersInitialized == patchersTotal) ? "! We're all good to go." : ". We're missing a few..?");
		((BaseUnityPlugin)this).Logger.LogInfo((object)$"Initialized {patchersInitialized}/{patchersTotal} patchers{arg}");
	}

	private void Start()
	{
		InitializeComponent("QoLModSettings", delegate
		{
			QoLModSettings.Initialize(((BaseUnityPlugin)this).Logger);
		});
	}

	public static void RegisterLayerArt(int biomeIndex, Sprite banner, Sprite icon)
	{
		if ((Object)(object)banner != (Object)null)
		{
			SaveSaveMenuPatcher.layerBackgrounds[biomeIndex] = banner;
		}
		if ((Object)(object)icon != (Object)null)
		{
			SaveSaveMenuPatcher.layerIcons[biomeIndex] = icon;
		}
	}

	private void InitializeComponent(string componentName, Action initialize)
	{
		patchersTotal++;
		try
		{
			initialize?.Invoke();
			patchersInitialized++;
		}
		catch (Exception arg)
		{
			((BaseUnityPlugin)this).Logger.LogError((object)$"Failed to initialize {componentName}. Other QoL systems will continue loading.\n{arg}");
		}
	}
}
