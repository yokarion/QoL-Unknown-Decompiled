using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

namespace QoL_Unknown;

internal static class KeybindCombos
{
	[Flags]
	internal enum Modifiers
	{
		None = 0,
		Ctrl = 1,
		Shift = 2,
		Alt = 4
	}

	private sealed class PressState
	{
		public int Frame = -2;

		public bool WasDown;

		public bool ModsDuringPress;

		public bool Held;

		public bool Pressed;

		public bool Released;
	}

	private const int ComboMarker = 1073741824;

	private const int ModifierShift = 16;

	private const int BaseKeyMask = 65535;

	private const int ModifierMask = 7;

	private const int InitialFrame = -2;

	private static readonly Dictionary<KeyCode, PressState> states = new Dictionary<KeyCode, PressState>();

	public static bool IsCombo(KeyCode key)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Invalid comparison between Unknown and I4
		return (key & 0x40000000) > 0;
	}

	public static KeyCode Combine(KeyCode baseKey, Modifiers modifiers)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		return (KeyCode)(0x40000000 | ((int)modifiers << 16) | baseKey);
	}

	public static void Split(KeyCode key, out KeyCode baseKey, out Modifiers modifiers)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Expected I4, but got Unknown
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Expected I4, but got Unknown
		baseKey = (KeyCode)(key & 0xFFFF);
		modifiers = (Modifiers)((key >> 16) & 7);
	}

	public static string Format(KeyCode key)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		Split(key, out var baseKey, out var modifiers);
		StringBuilder stringBuilder = new StringBuilder();
		AppendModifier(stringBuilder, modifiers, Modifiers.Ctrl, "Ctrl");
		AppendModifier(stringBuilder, modifiers, Modifiers.Shift, "Shift");
		AppendModifier(stringBuilder, modifiers, Modifiers.Alt, "Alt");
		stringBuilder.Append(InputDisplayHelper.GetFriendlyName(baseKey));
		return stringBuilder.ToString();
	}

	public static bool GetKey(KeyCode key)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		return Refresh(key).Held;
	}

	public static bool GetKeyDown(KeyCode key)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		return Refresh(key).Pressed;
	}

	public static bool GetKeyUp(KeyCode key)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		return Refresh(key).Released;
	}

	public static IEnumerator Capture(SettingKeybind keybind, TextMeshProUGUI text)
	{
		if ((Object)(object)text != (Object)null)
		{
			((TMP_Text)text).text = "...";
		}
		KeyCode pendingModifier = (KeyCode)0;
		while (true)
		{
			bool pressedIsModifier;
			KeyCode pressed = FindPressedKey(out pressedIsModifier);
			if ((int)pressed > 0 && pressedIsModifier)
			{
				if ((int)pendingModifier == 0)
				{
					pendingModifier = pressed;
				}
			}
			else if ((int)pressed > 0)
			{
				Modifiers modifiers = HeldModifiers();
				Commit(keybind, text, (modifiers == Modifiers.None) ? pressed : Combine(pressed, modifiers));
				yield break;
			}
			if ((int)pendingModifier != 0 && HeldModifiers() == Modifiers.None)
			{
				break;
			}
			yield return null;
		}
		Commit(keybind, text, pendingModifier);
	}

	private static void AppendModifier(StringBuilder builder, Modifiers modifiers, Modifiers flag, string name)
	{
		if ((modifiers & flag) != Modifiers.None)
		{
			if (builder.Length > 0)
			{
				builder.Append(" + ");
			}
			builder.Append(name);
		}
	}

	private static PressState Refresh(KeyCode key)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		Split(key, out var baseKey, out var modifiers);
		if (!states.TryGetValue(key, out var value))
		{
			value = new PressState();
			states[key] = value;
		}
		if (value.Frame == Time.frameCount)
		{
			return value;
		}
		bool flag = RawHeld(baseKey);
		bool flag2 = ModifiersHeld(modifiers);
		bool flag3 = value.Frame == Time.frameCount - 1 || value.Frame == -2;
		bool flag4 = value.ModsDuringPress;
		bool flag5 = flag3 && flag && !value.WasDown;
		bool flag6 = flag3 && !flag && value.WasDown;
		if (flag5 || (!flag3 && flag))
		{
			flag4 = flag2;
			value.ModsDuringPress = flag2;
		}
		else if (flag6)
		{
			value.ModsDuringPress = false;
		}
		value.WasDown = flag;
		value.Frame = Time.frameCount;
		bool flag7 = flag2 || flag4;
		value.Held = flag && flag7;
		value.Pressed = flag5 && flag2;
		value.Released = flag6 && flag7;
		return value;
	}

	private static bool RawHeld(KeyCode baseKey)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		int num = MouseButtonIndex(baseKey);
		return (num >= 0) ? Input.GetMouseButton(num) : Input.GetKey(baseKey);
	}

	private static int MouseButtonIndex(KeyCode key)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Expected I4, but got Unknown
		int num = key - 323;
		return (num >= 0 && num <= 6) ? num : (-1);
	}

	private static bool ModifiersHeld(Modifiers modifiers)
	{
		if ((modifiers & Modifiers.Ctrl) != Modifiers.None && !Input.GetKey((KeyCode)306) && !Input.GetKey((KeyCode)305))
		{
			return false;
		}
		if ((modifiers & Modifiers.Shift) != Modifiers.None && !Input.GetKey((KeyCode)304) && !Input.GetKey((KeyCode)303))
		{
			return false;
		}
		if ((modifiers & Modifiers.Alt) != Modifiers.None && !Input.GetKey((KeyCode)308) && !Input.GetKey((KeyCode)307))
		{
			return false;
		}
		return true;
	}

	private static Modifiers HeldModifiers()
	{
		Modifiers modifiers = Modifiers.None;
		if (Input.GetKey((KeyCode)306) || Input.GetKey((KeyCode)305))
		{
			modifiers |= Modifiers.Ctrl;
		}
		if (Input.GetKey((KeyCode)304) || Input.GetKey((KeyCode)303))
		{
			modifiers |= Modifiers.Shift;
		}
		if (Input.GetKey((KeyCode)308) || Input.GetKey((KeyCode)307))
		{
			modifiers |= Modifiers.Alt;
		}
		return modifiers;
	}

	private static bool IsModifier(KeyCode key)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Invalid comparison between Unknown and I4
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Invalid comparison between Unknown and I4
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Invalid comparison between Unknown and I4
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Invalid comparison between Unknown and I4
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Invalid comparison between Unknown and I4
		return (int)key == 306 || (int)key == 305 || (int)key == 304 || (int)key == 303 || (int)key == 308 || (int)key == 307;
	}

	private static KeyCode FindPressedKey(out bool isModifier)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Invalid comparison between Unknown and I4
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		KeyCode val = (KeyCode)0;
		foreach (KeyCode value in Enum.GetValues(typeof(KeyCode)))
		{
			if (Input.GetKeyDown(value))
			{
				if (!IsModifier(value))
				{
					isModifier = false;
					return value;
				}
				if ((int)val == 0)
				{
					val = value;
				}
			}
		}
		isModifier = true;
		return val;
	}

	private static void Commit(SettingKeybind keybind, TextMeshProUGUI text, KeyCode value)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		((Setting)keybind).SetValue((object)value);
		((Setting)keybind).Apply();
		if ((Object)(object)text != (Object)null)
		{
			((TMP_Text)text).text = keybind.GetDisplayString();
		}
	}
}
