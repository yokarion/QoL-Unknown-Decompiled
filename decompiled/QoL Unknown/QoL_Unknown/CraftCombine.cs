namespace QoL_Unknown;

internal sealed class CraftCombine
{
	public Item Target { get; }

	public Item Source { get; }

	public CraftCombine(Item target, Item source)
	{
		Target = target;
		Source = source;
	}
}
