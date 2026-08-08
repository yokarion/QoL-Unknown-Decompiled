namespace QoL_Unknown;

public sealed class ModDropdownChoice
{
	public string Key { get; }

	public string Label { get; }

	public ModDropdownChoice(string key, string label)
	{
		Key = key;
		Label = label;
	}
}
