namespace QoL_Unknown;

internal sealed class CraftingPlanOperation
{
	public Recipe Recipe;

	public int Count;

	public string ResultId => Recipe?.result?.id ?? string.Empty;
}
