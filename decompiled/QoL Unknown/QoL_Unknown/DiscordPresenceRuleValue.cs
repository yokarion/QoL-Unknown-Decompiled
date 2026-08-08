using System;

namespace QoL_Unknown;

internal struct DiscordPresenceRuleValue
{
	public bool IsBoolean;

	public bool IsString;

	public double Number;

	public bool Boolean;

	public string String;

	public static DiscordPresenceRuleValue FromNumber(double value)
	{
		return new DiscordPresenceRuleValue
		{
			IsBoolean = false,
			IsString = false,
			Number = value,
			Boolean = (Math.Abs(value) > double.Epsilon),
			String = null
		};
	}

	public static DiscordPresenceRuleValue FromBoolean(bool value)
	{
		return new DiscordPresenceRuleValue
		{
			IsBoolean = true,
			IsString = false,
			Number = (value ? 1.0 : 0.0),
			Boolean = value,
			String = null
		};
	}

	public static DiscordPresenceRuleValue FromString(string value)
	{
		return new DiscordPresenceRuleValue
		{
			IsBoolean = false,
			IsString = true,
			Number = 0.0,
			Boolean = !string.IsNullOrEmpty(value),
			String = (value ?? string.Empty)
		};
	}
}
