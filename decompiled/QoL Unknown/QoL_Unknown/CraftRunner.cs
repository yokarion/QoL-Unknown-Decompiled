using System.Collections;
using System.Collections.Generic;
using BepInEx.Logging;
using UnityEngine;

namespace QoL_Unknown;

internal static class CraftRunner
{
	internal static ManualLogSource Logger;

	private static PlayerCamera mpCamera;

	private static Body mpBody;

	private static CraftPlan mpPlan;

	private static bool mpCancelRequested;

	internal static bool IsMultiplayerCraftRunning => mpPlan != null && (Object)(object)mpCamera != (Object)null && ((Behaviour)mpCamera).isActiveAndEnabled && (Object)(object)mpBody != (Object)null && (Object)(object)mpCamera.body == (Object)(object)mpBody && mpBody.conscious && MultiplayerCompat.IsActive;

	internal static void CancelMultiplayerCraft()
	{
		mpCancelRequested = true;
	}

	public static bool Run(PlayerCamera camera, CraftPlan plan)
	{
		if ((Object)(object)camera == (Object)null || (Object)(object)camera.body == (Object)null || plan == null || !plan.CanCraft || IsMultiplayerCraftRunning)
		{
			return false;
		}
		if (MultiplayerCompat.IsActive)
		{
			if (!((Behaviour)camera).isActiveAndEnabled || !camera.body.conscious || !MultiplayerCompat.NativeCraftAvailable)
			{
				return false;
			}
			mpCamera = camera;
			mpBody = camera.body;
			mpPlan = plan;
			mpCancelRequested = false;
			((MonoBehaviour)camera).StartCoroutine(ExecuteMultiplayer(camera, camera.body, plan));
			return true;
		}
		bool result = Execute(camera.body, plan);
		CraftingModule.InvalidateInventoryCache();
		CraftingModule.ScheduleRefresh(camera);
		return result;
	}

	internal static bool Execute(Body body, CraftPlan plan)
	{
		if ((Object)(object)body == (Object)null || plan == null || !plan.CanCraft)
		{
			return false;
		}
		if (!ApplyCombines(body, plan.Combines))
		{
			return false;
		}
		for (int i = 0; i < plan.Intermediates.Count; i++)
		{
			CraftStep craftStep = plan.Intermediates[i];
			for (int j = 0; j < craftStep.Count; j++)
			{
				ReachableItems.InvalidateGround();
				craftStep.Recipe.TryMake();
			}
		}
		for (int k = 0; k < plan.RequestedAmount; k++)
		{
			ReachableItems.InvalidateGround();
			plan.FinalRecipe.TryMake();
		}
		return true;
	}

	private static IEnumerator ExecuteMultiplayer(PlayerCamera camera, Body body, CraftPlan plan)
	{
		try
		{
			float deadline = Time.realtimeSinceStartup + MultiplayerCompat.CraftWaitSeconds;
			while (MultiplayerCompat.NativeCraftOutstanding)
			{
				if (!StillRunning(camera, plan))
				{
					yield break;
				}
				if (Time.realtimeSinceStartup >= deadline)
				{
					camera.DoAlert("Autocraft stopped waiting for the previous craft's answer.", false);
					yield break;
				}
				MultiplayerCompat.RequestNativeCraft(camera, plan.FinalRecipe);
				yield return null;
			}
			if (!ApplyCombines(body, plan.Combines))
			{
				camera.PlayUISound((UISoundType)6, 1f);
				yield break;
			}
			List<CraftStep> steps = new List<CraftStep>(plan.Intermediates)
			{
				new CraftStep
				{
					Recipe = plan.FinalRecipe,
					Count = plan.RequestedAmount
				}
			};
			for (int stepIdx = 0; stepIdx < steps.Count; stepIdx++)
			{
				CraftStep step = steps[stepIdx];
				for (int n = 0; n < step.Count; n++)
				{
					deadline = Time.realtimeSinceStartup + MultiplayerCompat.CraftWaitSeconds;
					while (true)
					{
						if (!StillRunning(camera, plan))
						{
							yield break;
						}
						if (step.Recipe.GetItemsForRecipe() != null)
						{
							break;
						}
						if (Time.realtimeSinceStartup >= deadline)
						{
							camera.DoAlert("Autocraft stopped: ingredients never became ready.", false);
							yield break;
						}
						yield return null;
					}
					MultiplayerCompat.NativeCraftState state = MultiplayerCompat.RequestNativeCraft(camera, step.Recipe);
					while (true)
					{
						switch (state)
						{
						case MultiplayerCompat.NativeCraftState.Pending:
							yield return null;
							if (StillRunning(camera, plan))
							{
								if (!(Time.realtimeSinceStartup >= deadline))
								{
									state = MultiplayerCompat.RequestNativeCraft(camera, step.Recipe);
									continue;
								}
								camera.DoAlert("Autocraft stopped waiting for the host's answer.", false);
							}
							yield break;
						default:
							yield break;
						case MultiplayerCompat.NativeCraftState.Accepted:
							break;
						}
						break;
					}
					ReachableItems.InvalidateGround();
					CraftingModule.InvalidateInventoryCache();
				}
			}
		}
		finally
		{
			if (mpPlan == plan)
			{
				mpPlan = null;
				mpCamera = null;
				mpBody = null;
				ReachableItems.InvalidateGround();
				CraftingModule.InvalidateInventoryCache();
				CraftingModule.ForceRefresh(camera);
			}
		}
	}

	private static bool StillRunning(PlayerCamera camera, CraftPlan plan)
	{
		return mpPlan == plan && IsMultiplayerCraftRunning && !mpCancelRequested && (Object)(object)camera == (Object)(object)PlayerCamera.main;
	}

	private static bool ApplyCombines(Body body, List<CraftCombine> combines)
	{
		if ((Object)(object)body == (Object)null)
		{
			return false;
		}
		for (int i = 0; i < combines.Count; i++)
		{
			CraftCombine craftCombine = combines[i];
			if ((Object)(object)craftCombine.Target == (Object)null || (Object)(object)craftCombine.Source == (Object)null || !body.CanCombine(craftCombine.Target, craftCombine.Source))
			{
				return false;
			}
			body.CombineItems(craftCombine.Target, craftCombine.Source);
		}
		return true;
	}
}
