namespace QoL_Unknown;

internal struct CraftLiquid
{
	public readonly string Id;

	public float Amount;

	public CraftLiquid(string id, float amount)
	{
		Id = id;
		Amount = amount;
	}
}
