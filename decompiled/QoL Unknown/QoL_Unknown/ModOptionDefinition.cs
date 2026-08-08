using System;
using System.Linq;
using UnityEngine;

namespace QoL_Unknown;

public sealed class ModOptionDefinition
{
	internal ModOptionKind Kind;

	internal string Id;

	internal string Label;

	internal string Description;

	internal SettingCategory Category;

	internal string CustomCategory;

	internal float FloatDefault;

	internal int IntDefault;

	internal bool BoolDefault;

	internal KeyCode KeyDefault;

	internal float Min;

	internal float Max;

	internal ModDropdownChoice[] Choices;

	internal Func<float, string> FloatFormatter;

	internal Action<float> FloatApply;

	internal Action<int> IntApply;

	internal Action<int> DropdownApply;

	internal Action<bool> BoolApply;

	internal Action<KeyCode> KeybindApply;

	internal bool RestoreSavedValue = true;

	internal bool UsesCustomCategory => !string.IsNullOrWhiteSpace(CustomCategory);

	internal void SetResolvedCategory(SettingCategory category)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		Category = category;
	}

	public ModOptionDefinition WithoutSavedValueRestore()
	{
		RestoreSavedValue = false;
		return this;
	}

	public static ModOptionDefinition Float(string id, string label, string description, SettingCategory category, float defaultValue, float min, float max, Action<float> apply = null, Func<float, string> formatValue = null)
	{
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		return new ModOptionDefinition
		{
			Kind = ModOptionKind.Float,
			Id = id,
			Label = label,
			Description = description,
			Category = category,
			FloatDefault = defaultValue,
			Min = min,
			Max = max,
			FloatApply = apply,
			FloatFormatter = formatValue
		};
	}

	public static ModOptionDefinition Float(string id, string label, string description, string customCategory, float defaultValue, float min, float max, Action<float> apply = null, Func<float, string> formatValue = null)
	{
		return new ModOptionDefinition
		{
			Kind = ModOptionKind.Float,
			Id = id,
			Label = label,
			Description = description,
			CustomCategory = customCategory,
			FloatDefault = defaultValue,
			Min = min,
			Max = max,
			FloatApply = apply,
			FloatFormatter = formatValue
		};
	}

	public static ModOptionDefinition Int(string id, string label, string description, SettingCategory category, int defaultValue, int min, int max, Action<int> apply = null)
	{
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		return new ModOptionDefinition
		{
			Kind = ModOptionKind.Int,
			Id = id,
			Label = label,
			Description = description,
			Category = category,
			IntDefault = defaultValue,
			Min = min,
			Max = max,
			IntApply = apply
		};
	}

	public static ModOptionDefinition Int(string id, string label, string description, string customCategory, int defaultValue, int min, int max, Action<int> apply = null)
	{
		return new ModOptionDefinition
		{
			Kind = ModOptionKind.Int,
			Id = id,
			Label = label,
			Description = description,
			CustomCategory = customCategory,
			IntDefault = defaultValue,
			Min = min,
			Max = max,
			IntApply = apply
		};
	}

	public static ModOptionDefinition Bool(string id, string label, string description, SettingCategory category, bool defaultValue, Action<bool> apply = null)
	{
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		return new ModOptionDefinition
		{
			Kind = ModOptionKind.Bool,
			Id = id,
			Label = label,
			Description = description,
			Category = category,
			BoolDefault = defaultValue,
			BoolApply = apply
		};
	}

	public static ModOptionDefinition Bool(string id, string label, string description, string customCategory, bool defaultValue, Action<bool> apply = null)
	{
		return new ModOptionDefinition
		{
			Kind = ModOptionKind.Bool,
			Id = id,
			Label = label,
			Description = description,
			CustomCategory = customCategory,
			BoolDefault = defaultValue,
			BoolApply = apply
		};
	}

	public static ModOptionDefinition Dropdown(string id, string label, string description, SettingCategory category, int defaultValue, ModDropdownChoice[] choices, Action<int> apply = null)
	{
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		return new ModOptionDefinition
		{
			Kind = ModOptionKind.Dropdown,
			Id = id,
			Label = label,
			Description = description,
			Category = category,
			IntDefault = defaultValue,
			Choices = choices,
			DropdownApply = apply
		};
	}

	public static ModOptionDefinition Dropdown(string id, string label, string description, string customCategory, int defaultValue, ModDropdownChoice[] choices, Action<int> apply = null)
	{
		return new ModOptionDefinition
		{
			Kind = ModOptionKind.Dropdown,
			Id = id,
			Label = label,
			Description = description,
			CustomCategory = customCategory,
			IntDefault = defaultValue,
			Choices = choices,
			DropdownApply = apply
		};
	}

	public static ModOptionDefinition Keybind(string id, string label, string description, SettingCategory category, KeyCode defaultValue, Action<KeyCode> apply = null)
	{
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		return new ModOptionDefinition
		{
			Kind = ModOptionKind.Keybind,
			Id = id,
			Label = label,
			Description = description,
			Category = category,
			KeyDefault = defaultValue,
			KeybindApply = apply
		};
	}

	public static ModOptionDefinition Keybind(string id, string label, string description, string customCategory, KeyCode defaultValue, Action<KeyCode> apply = null)
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		return new ModOptionDefinition
		{
			Kind = ModOptionKind.Keybind,
			Id = id,
			Label = label,
			Description = description,
			CustomCategory = customCategory,
			KeyDefault = defaultValue,
			KeybindApply = apply
		};
	}

	internal Setting CreateSetting()
	{
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Expected O, but got Unknown
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
		//IL_0118: Expected O, but got Unknown
		//IL_011d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		//IL_012e: Unknown result type (might be due to invalid IL or missing references)
		//IL_013a: Unknown result type (might be due to invalid IL or missing references)
		//IL_013c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0141: Unknown result type (might be due to invalid IL or missing references)
		//IL_0146: Unknown result type (might be due to invalid IL or missing references)
		//IL_0159: Expected O, but got Unknown
		//IL_015e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0163: Unknown result type (might be due to invalid IL or missing references)
		//IL_016f: Unknown result type (might be due to invalid IL or missing references)
		//IL_017b: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_020d: Expected O, but got Unknown
		//IL_01b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cf: Expected O, but got Unknown
		return (Setting)(Kind switch
		{
			ModOptionKind.Float => (object)new SettingFloat
			{
				name = Id,
				value = Mathf.Clamp(FloatDefault, Min, Max),
				min = Min,
				max = Max,
				category = Category,
				formatValue = FloatFormatter,
				apply = delegate
				{
					FloatApply?.Invoke(Settings.Get<SettingFloat>(Id).value);
				}
			}, 
			ModOptionKind.Int => (object)new SettingInt
			{
				name = Id,
				value = Mathf.Clamp(IntDefault, Mathf.RoundToInt(Min), Mathf.RoundToInt(Max)),
				min = Mathf.RoundToInt(Min),
				max = Mathf.RoundToInt(Max),
				category = Category,
				apply = delegate
				{
					IntApply?.Invoke(Settings.Get<SettingInt>(Id).value);
				}
			}, 
			ModOptionKind.Bool => (object)new SettingBool
			{
				name = Id,
				value = BoolDefault,
				category = Category,
				apply = delegate
				{
					BoolApply?.Invoke(Settings.Get<SettingBool>(Id).value);
				}
			}, 
			ModOptionKind.Dropdown => (object)new SettingDropdown
			{
				name = Id,
				value = IntDefault,
				choices = Choices.Select((ModDropdownChoice choice) => choice.Key).ToArray(),
				category = Category,
				apply = delegate
				{
					DropdownApply?.Invoke(Settings.Get<SettingDropdown>(Id).value);
				}
			}, 
			ModOptionKind.Keybind => (object)new SettingKeybind
			{
				name = Id,
				value = KeyDefault,
				category = Category,
				apply = delegate
				{
					//IL_0017: Unknown result type (might be due to invalid IL or missing references)
					KeybindApply?.Invoke(Settings.Get<SettingKeybind>(Id).value);
				}
			}, 
			_ => throw new InvalidOperationException($"Unsupported mod option kind '{Kind}'."), 
		});
	}
}
