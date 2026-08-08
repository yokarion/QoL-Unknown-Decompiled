using BepInEx;

namespace QoL_Unknown;

[BepInPlugin("org.bepinex.plugins.qol_unknown", "QoL Unknown", "1.0.4.8")]
[BepInDependency(/*Could not decode attribute arguments.*/)]
public class Plugin : BaseUnityPlugin
{
	private void Awake()
	{
		((BaseUnityPlugin)this).Logger.LogInfo((object)"Mod QoLified to run!");
		ModLocale.Initialize(((BaseUnityPlugin)this).Logger);
		QoLModuleManager.SetupRegistry(((BaseUnityPlugin)this).Logger);
		KrokoshaMpCompat.Initialize(((BaseUnityPlugin)this).Logger);
		KrokMpCommandUtil.Initialize(((BaseUnityPlugin)this).Logger);
		KrokMpCommandUtil.EnsureReceiversRegistered();
		EnhancedMenuController.Initialize(((BaseUnityPlugin)this).Logger);
		QoLModSettings.Initialize(((BaseUnityPlugin)this).Logger);
		AntiGlarePatcher.Initialize(((BaseUnityPlugin)this).Logger);
		PreRunScriptPatcher.Initialize(((BaseUnityPlugin)this).Logger);
		UpdateChecker.Initialize(((BaseUnityPlugin)this).Logger);
		ConsoleScriptPatcher.Initialize(((BaseUnityPlugin)this).Logger);
		LineOfSightPatcher.Initialize(((BaseUnityPlugin)this).Logger);
		GroundContainerPickupPatcher.Initialize(((BaseUnityPlugin)this).Logger);
		KeybindsPatcher.Initialize(((BaseUnityPlugin)this).Logger);
		InventorySortPatcher.Initialize(((BaseUnityPlugin)this).Logger);
		IlmeniteWorldGenPatcher.Initialize(((BaseUnityPlugin)this).Logger);
		RadlinePatcher.Initialize(((BaseUnityPlugin)this).Logger);
		SeededRunPatcher.Initialize(((BaseUnityPlugin)this).Logger);
		PreGenPatcher.Initialize(((BaseUnityPlugin)this).Logger);
		RecipeFixPatcher.Initialize(((BaseUnityPlugin)this).Logger);
		SaveSaveMenuPatcher.Initialize(((BaseUnityPlugin)this).Logger);
		SaveSystemPatcher.Initialize(((BaseUnityPlugin)this).Logger);
		SaveMenuPatcher.Initialize(((BaseUnityPlugin)this).Logger);
		PauseMenuDialoguePatcher.Initialize(((BaseUnityPlugin)this).Logger);
		SaveManager.Initialize(((BaseUnityPlugin)this).Logger);
		CasualtyReportPatcher.Initialize(((BaseUnityPlugin)this).Logger);
		LayerTransitionPatcher.Initialize(((BaseUnityPlugin)this).Logger);
		SavePanelPatcher.Initialize(((BaseUnityPlugin)this).Logger);
		ControllerPatcher.Initialize(((BaseUnityPlugin)this).Logger);
		ControllerItemHighlightPatcher.Initialize(((BaseUnityPlugin)this).Logger);
		AltItemHighlightPatcher.Initialize(((BaseUnityPlugin)this).Logger);
		WearableCarouselPatcher.Initialize(((BaseUnityPlugin)this).Logger);
		QoLSettingsMenu.Initialize(((BaseUnityPlugin)this).Logger);
		EmbeddedDllLoader.Init();
		FontPatcher.Initialize(((BaseUnityPlugin)this).Logger);
		EntitySpawnPatcher.Initialize(((BaseUnityPlugin)this).Logger);
		StructureSpawnPatcher.Initialize(((BaseUnityPlugin)this).Logger);
		SkillsPatcher.Initialize(((BaseUnityPlugin)this).Logger);
		ExitConfirmationPatcher.Initialize(((BaseUnityPlugin)this).Logger);
		SpawnAutofill.Initialize(((BaseUnityPlugin)this).Logger);
		AudioSafetyPatcher.Initialize(((BaseUnityPlugin)this).Logger);
		MiscPatches.Initialize(((BaseUnityPlugin)this).Logger);
		MultiplayerHoverArbitrationPatcher.Initialize(((BaseUnityPlugin)this).Logger);
		MultiplayerSaveLoadPatcher.Initialize();
		MinigameQoLPatcher.Initialize(((BaseUnityPlugin)this).Logger);
		foreach (QoLModuleDef module in QoLModuleManager.Modules)
		{
			if (module.IsCore || QoLModuleManager.IsEnabled(module.ID))
			{
				module.InitAction?.Invoke(((BaseUnityPlugin)this).Logger);
			}
		}
	}
}
