using System.Collections.Generic;

namespace QoL_Unknown;

internal sealed class CraftPlan
{
	public Recipe FinalRecipe;

	public bool CanCraft;

	public int RequestedAmount;

	public int MaxCraftable;

	public readonly List<CraftCombine> Combines = new List<CraftCombine>();

	public readonly List<CraftStep> Intermediates = new List<CraftStep>();

	public bool HasPreparations => Combines.Count > 0 || Intermediates.Count > 0;

	public int TotalSteps
	{
		get
		{
			int num = RequestedAmount;
			for (int i = 0; i < Intermediates.Count; i++)
			{
				num += Intermediates[i].Count;
			}
			return num;
		}
	}
}
