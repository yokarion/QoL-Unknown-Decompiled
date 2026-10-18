using System;
using System.Collections.Generic;
using BepInEx.Logging;
using UnityEngine;

namespace QoL_Unknown;

public static class QoLModuleManager
{
	public static List<QoLModuleDef> Modules = new List<QoLModuleDef>();

	private static ManualLogSource Logger;

	public static void SetupRegistry(ManualLogSource logger)
	{
		Logger = logger;
		Modules.Clear();
		Register("qol_ui_discord", "Discord RPC", "Rich Presence integration.", DiscordRPCHelper.Initialize, defaultOn: true);
		Register("qol_vis_los", "Line of Sight Fix", "Adjusts the LOS shader to not outline blocks in the dark", defaultOn: true);
		Register("qol_craft_bulk", "Bulk Crafting", "Adds craft amounts and auto-crafts.", CraftingModule.Initialize, defaultOn: true);
		Register("qol_ui_preview", "Crafting Preview", "Enhances the preview of items in the crafting menu.", CraftingPreviewPatcher.Initialize, defaultOn: true);
		Register("qol_ui_scroll", "Container Scroll", "Adds scroll wheel support to containers.", ContainerScrollPatcher.Initialize, defaultOn: true);
		Register("qol_inv_stacking", "Container Stacking", "Groups duplicate items in containers. Hover to see each item and its condition.", ContainerStackingPatcher.Initialize, defaultOn: true);
		Register("qol_inv_sortbtn", "Sort Button", "Adds a UI button for sorting.", InventorySortButtonPatcher.Initialize, defaultOn: true);
		Register("qol_inv_quickstash", "Quick Stash", "Use the Quick Stash keybind to insta-pickup items.", QuickStashPatcher.Initialize, defaultOn: true);
		Register("qol_inv_liquid", "Inventory Keybinds", "Adds Shift interactions and a configurable inventory interaction modifier.", CtrlDragPatcher.Initialize, defaultOn: true);
		Register("qol_game_pusher", "Water Physics", "1.5-tile water no longer pushes you.", WaterPusherPatcher.Initialize, defaultOn: true);
		Register("qol_game_animal_attack_assist", "Attack Assist", "Box-casts melee attacks and retargets swings onto nearby animals.", AnimalAttackAssistPatcher.Initialize, defaultOn: true);
		Register("qol_game_soundcannon_indicator", "Sound Cannon Indicator", "Warns about charging sound cannons with speech and arrows.", SoundCannonIndicator.Initialize, defaultOn: true);
		Register("qol_game_modifiers", "Layer Modifiers", "Enables/disables layer modifier adjustments.", defaultOn: true);
		Register("qol_audio_safety", "Audio Safety", "Reduces loud and high-pitched sounds.", defaultOn: true);
		Register("qol_save_menu", "Save Menu", "Adds a save file selection menu on pause", SaveMenuPatcher.Initialize, defaultOn: true);
		Register("qol_timer", "Timer", "Adds a timer to the pause menu.", TimerMenuPatcher.Initialize, defaultOn: true);
		Register("qol_autosave", "Autosave", "Automatically saves the game at regular intervals.", defaultOn: true);
	}

	private static void Register(string id, string name, string desc, Action<ManualLogSource> init, bool defaultOn, bool isCore = false)
	{
		Modules.Add(new QoLModuleDef
		{
			ID = id,
			Name = name,
			Description = desc,
			InitAction = init,
			EnabledByDefault = defaultOn,
			IsCore = isCore
		});
	}

	private static void Register(string id, string name, string desc, bool defaultOn, bool isCore = false)
	{
		Register(id, name, desc, null, defaultOn, isCore);
	}

	public static bool IsEnabled(string id)
	{
		QoLModuleDef qoLModuleDef = Modules.Find((QoLModuleDef m) => m.ID == id);
		int num = ((qoLModuleDef != null && qoLModuleDef.EnabledByDefault) ? 1 : 0);
		return PlayerPrefs.GetInt(id, num) == 1;
	}

	public static void SetEnabled(string id, bool enabled)
	{
		PlayerPrefs.SetInt(id, enabled ? 1 : 0);
		PlayerPrefs.Save();
	}
}
