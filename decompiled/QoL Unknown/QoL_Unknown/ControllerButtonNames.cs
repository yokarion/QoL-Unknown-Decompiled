namespace QoL_Unknown;

internal static class ControllerButtonNames
{
	public static string ToBindingKey(ControllerButton button)
	{
		return button switch
		{
			ControllerButton.A => "A", 
			ControllerButton.B => "B", 
			ControllerButton.X => "X", 
			ControllerButton.Y => "Y", 
			ControllerButton.DpadUp => "DpadUp", 
			ControllerButton.DpadDown => "DpadDown", 
			ControllerButton.DpadLeft => "DpadLeft", 
			ControllerButton.DpadRight => "DpadRight", 
			ControllerButton.Start => "Start", 
			ControllerButton.Back => "Back", 
			ControllerButton.RT => "RT", 
			ControllerButton.LT => "LT", 
			ControllerButton.RB => "RB", 
			ControllerButton.LB => "LB", 
			ControllerButton.L3 => "L3", 
			_ => "none", 
		};
	}

	public static bool TryParse(string key, out ControllerButton button)
	{
		button = ControllerButton.A;
		if (string.IsNullOrEmpty(key))
		{
			return false;
		}
		switch (key)
		{
		case "A":
			button = ControllerButton.A;
			return true;
		case "B":
			button = ControllerButton.B;
			return true;
		case "X":
			button = ControllerButton.X;
			return true;
		case "Y":
			button = ControllerButton.Y;
			return true;
		case "DpadUp":
			button = ControllerButton.DpadUp;
			return true;
		case "DpadDown":
			button = ControllerButton.DpadDown;
			return true;
		case "DpadLeft":
			button = ControllerButton.DpadLeft;
			return true;
		case "DpadRight":
			button = ControllerButton.DpadRight;
			return true;
		case "Start":
			button = ControllerButton.Start;
			return true;
		case "Back":
			button = ControllerButton.Back;
			return true;
		case "RT":
			button = ControllerButton.RT;
			return true;
		case "LT":
			button = ControllerButton.LT;
			return true;
		case "RB":
			button = ControllerButton.RB;
			return true;
		case "LB":
			button = ControllerButton.LB;
			return true;
		case "L3":
			button = ControllerButton.L3;
			return true;
		default:
			return false;
		}
	}
}
