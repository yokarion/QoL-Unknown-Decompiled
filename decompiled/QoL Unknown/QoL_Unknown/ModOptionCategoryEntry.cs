namespace QoL_Unknown;

public sealed class ModOptionCategoryEntry
{
	public string DisplayName { get; }

	public int CategoryIndex { get; }

	public ModOptionCategoryEntry(string displayName, int categoryIndex)
	{
		DisplayName = displayName;
		CategoryIndex = categoryIndex;
	}
}
