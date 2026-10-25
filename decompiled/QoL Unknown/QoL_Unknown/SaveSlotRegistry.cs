using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using BepInEx.Logging;
using Newtonsoft.Json;

namespace QoL_Unknown;

internal static class SaveSlotRegistry
{
	private class Layout
	{
		public List<int> Slots = new List<int>();

		public Dictionary<int, string> Names = new Dictionary<int, string>();
	}

	internal static ManualLogSource Logger;

	private const int MinimumSlots = 3;

	private const string LayoutFileName = "qol_slots.json";

	private static readonly Regex SlotFilePattern = new Regex("^slot_(\\d+)\\.sv$", RegexOptions.IgnoreCase);

	private static Layout layout;

	private static string LayoutPath => Path.Combine(SaveManager.SaveFolder, "qol_slots.json");

	public static string FileName(int slot)
	{
		return $"slot_{slot}.sv";
	}

	public static string DefaultName(int slot)
	{
		return (slot == 0) ? "Autosave" : $"Slot {slot}";
	}

	public static bool IsFilled(int slot)
	{
		return File.Exists(Path.Combine(SaveManager.SaveFolder, FileName(slot)));
	}

	public static List<int> GetSlots()
	{
		Layout layout = GetLayout();
		bool flag = false;
		string[] files = Directory.GetFiles(SaveManager.SaveFolder, "slot_*.sv");
		foreach (string path in files)
		{
			Match match = SlotFilePattern.Match(Path.GetFileName(path));
			if (match.Success && int.TryParse(match.Groups[1].Value, out var result) && result > 0 && !layout.Slots.Contains(result))
			{
				layout.Slots.Add(result);
				flag = true;
			}
		}
		while (layout.Slots.Count < 3)
		{
			layout.Slots.Add(LowestFreeSlot(layout));
			flag = true;
		}
		if (layout.Slots.All(IsFilled))
		{
			layout.Slots.Add(LowestFreeSlot(layout));
			flag = true;
		}
		layout.Slots.Sort();
		if (flag)
		{
			Save();
		}
		return new List<int>(layout.Slots);
	}

	public static bool TryRemoveEmptySlot(int slot)
	{
		List<int> slots = GetSlots();
		if (!slots.Contains(slot) || IsFilled(slot) || slots.Count <= 3 || slots.Count((int s) => !IsFilled(s)) <= 1)
		{
			return false;
		}
		layout.Slots.Remove(slot);
		layout.Names.Remove(slot);
		Save();
		return true;
	}

	public static string GetName(int slot)
	{
		string value;
		return (GetLayout().Names.TryGetValue(slot, out value) && !string.IsNullOrWhiteSpace(value)) ? value : DefaultName(slot);
	}

	public static void SetName(int slot, string name)
	{
		Layout layout = GetLayout();
		string text = name?.Trim();
		if (string.IsNullOrEmpty(text) || text == DefaultName(slot))
		{
			if (!layout.Names.Remove(slot))
			{
				return;
			}
		}
		else
		{
			if (layout.Names.TryGetValue(slot, out var value) && value == text)
			{
				return;
			}
			layout.Names[slot] = text;
		}
		Save();
	}

	private static int LowestFreeSlot(Layout current)
	{
		int i;
		for (i = 1; current.Slots.Contains(i); i++)
		{
		}
		return i;
	}

	private static Layout GetLayout()
	{
		if (layout != null)
		{
			return layout;
		}
		try
		{
			if (File.Exists(LayoutPath))
			{
				layout = JsonConvert.DeserializeObject<Layout>(File.ReadAllText(LayoutPath));
			}
		}
		catch (Exception ex)
		{
			ManualLogSource logger = Logger;
			if (logger != null)
			{
				logger.LogWarning((object)("Could not read qol_slots.json, rebuilding slot layout: " + ex.Message));
			}
		}
		layout = layout ?? new Layout();
		layout.Slots = layout.Slots?.Where((int s) => s > 0).Distinct().ToList() ?? new List<int>();
		layout.Names = layout.Names ?? new Dictionary<int, string>();
		return layout;
	}

	private static void Save()
	{
		SaveFileStore.TryWriteAllTextAtomic(LayoutPath, JsonConvert.SerializeObject((object)layout, (Formatting)1), Logger, "write save slot layout");
	}
}
