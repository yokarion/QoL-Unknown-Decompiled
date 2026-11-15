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

	internal Action<GameObject, SettingInt> IntRowBinder;

	internal Action<int> DropdownApply;

	internal Action<bool> BoolApply;

	internal Action<KeyCode> KeybindApply;

	internal bool RestoreSavedValue = true;

	internal bool UsesCustomCategory => !string.IsNullOrWhiteSpace(CustomCategory);

	internal void SetResolvedCategory(SettingCategory category)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		Category = category;
	}

	public ModOptionDefinition WithoutSavedValueRestore()
	{
		RestoreSavedValue = false;
		return this;
	}

	public static ModOptionDefinition Float(string id, string label, string description, SettingCategory category, float defaultValue, float min, float max, Action<float> apply = null, Func<float, string> formatValue = null)
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
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
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
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
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
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
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
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
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
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
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
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
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Expected O, but got Unknown
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0110: Expected O, but got Unknown
		//IL_0110: Unknown result type (might be due to invalid IL or missing references)
		//IL_0115: Unknown result type (might be due to invalid IL or missing references)
		//IL_0121: Unknown result type (might be due to invalid IL or missing references)
		//IL_012d: Unknown result type (might be due to invalid IL or missing references)
		//IL_012f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0134: Unknown result type (might be due to invalid IL or missing references)
		//IL_0139: Unknown result type (might be due to invalid IL or missing references)
		//IL_014c: Expected O, but got Unknown
		//IL_014c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0151: Unknown result type (might be due to invalid IL or missing references)
		//IL_015d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0169: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01da: Unknown result type (might be due to invalid IL or missing references)
		//IL_01dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f9: Expected O, but got Unknown
		//IL_019e: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bd: Expected O, but got Unknown
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
					//IL_0016: Unknown result type (might be due to invalid IL or missing references)
					KeybindApply?.Invoke(Settings.Get<SettingKeybind>(Id).value);
				}
			}, 
			_ => throw new InvalidOperationException($"Unsupported mod option kind '{Kind}'."), 
		});
	}
}
