using System.Collections.Generic;

namespace QoL_Unknown;

internal sealed class CraftingPlan
{
	public bool CanCraft;

	public int RequestedAmount;

	public int MaxCraftable;

	public readonly List<CraftingCombineOperation> CombineOperations = new List<CraftingCombineOperation>();

	public readonly List<CraftingPlanOperation> IntermediateOperations = new List<CraftingPlanOperation>();

	public bool HasIntermediates => IntermediateOperations.Count > 0;

	public bool HasPreparations => CombineOperations.Count > 0 || IntermediateOperations.Count > 0;
}
