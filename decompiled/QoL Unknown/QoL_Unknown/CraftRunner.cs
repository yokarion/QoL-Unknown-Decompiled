using System.Collections;
using System.Collections.Generic;
using BepInEx.Logging;
using UnityEngine;

namespace QoL_Unknown;

internal static class CraftRunner
{
	internal static ManualLogSource Logger;

	private static PlayerCamera clientCamera;

	private static Body clientBody;

	private static CraftPlan clientPlan;

	private static bool clientCancelRequested;

	internal static bool IsClientCraftRunning => clientPlan != null && (Object)(object)clientCamera != (Object)null && ((Behaviour)clientCamera).isActiveAndEnabled && (Object)(object)clientBody != (Object)null && (Object)(object)clientCamera.body == (Object)(object)clientBody && clientBody.conscious && MultiplayerCompat.IsActive && MultiplayerCompat.IsClient;

	internal static void CancelClientCraft()
	{
		clientCancelRequested = true;
	}

	public static bool Run(PlayerCamera camera, CraftPlan plan)
	{
		if ((Object)(object)camera == (Object)null || (Object)(object)camera.body == (Object)null || plan == null || !plan.CanCraft || IsClientCraftRunning)
		{
			return false;
		}
		if (MultiplayerCompat.IsActive && MultiplayerCompat.IsClient)
		{
			if (!((Behaviour)camera).isActiveAndEnabled || !camera.body.conscious)
			{
				return false;
			}
			clientCamera = camera;
			clientBody = camera.body;
			clientPlan = plan;
			clientCancelRequested = false;
			((MonoBehaviour)camera).StartCoroutine(ExecuteClient(camera, camera.body, plan));
			return true;
		}
		CraftBackend backend = CraftBackend.Singleplayer;
		if (MultiplayerCompat.IsActive)
		{
			backend = ((!MultiplayerCompat.IsClient) ? CraftBackend.MultiplayerHost : CraftBackend.MultiplayerClient);
		}
		bool result = Execute(camera.body, plan, backend);
		CraftingModule.InvalidateInventoryCache();
		CraftingModule.ScheduleRefresh(camera);
		return result;
	}

	internal static bool Execute(Body body, CraftPlan plan, CraftBackend backend)
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
				if (!CraftOne(body, craftStep.Recipe, backend))
				{
					return false;
				}
			}
		}
		for (int k = 0; k < plan.RequestedAmount; k++)
		{
			ReachableItems.InvalidateGround();
			if (!CraftOne(body, plan.FinalRecipe, backend))
			{
				return false;
			}
		}
		return true;
	}

	private static IEnumerator ExecuteClient(PlayerCamera camera, Body body, CraftPlan plan)
	{
		try
		{
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
					if (clientPlan != plan || !IsClientCraftRunning || clientCancelRequested || (Object)(object)camera != (Object)(object)PlayerCamera.main || !MultiplayerCompat.TryRequestClientCraft(camera, step.Recipe, out var request) || request == null)
					{
						yield break;
					}
					step.Recipe.hasMadeBefore = true;
					float resultBefore = request.ResultCountBefore;
					float waitStart = Time.realtimeSinceStartup;
					float timeout = waitStart + MultiplayerCompat.ClientCraftWaitSeconds;
					bool resultReceived = false;
					while (Time.realtimeSinceStartup < timeout)
					{
						if (request.Denied)
						{
							MultiplayerCompat.ClearClientCraft(request);
							yield break;
						}
						if (clientPlan != plan || !IsClientCraftRunning || clientCancelRequested || (Object)(object)camera != (Object)(object)PlayerCamera.main)
						{
							MultiplayerCompat.ClearClientCraft(request);
							yield break;
						}
						ReachableItems.InvalidateGround();
						float resultNow = CountRecipeResult(body, step.Recipe);
						if (resultNow > resultBefore + 0.01f)
						{
							resultReceived = true;
							break;
						}
						yield return null;
					}
					MultiplayerCompat.ClearClientCraft(request);
					if (!resultReceived)
					{
						camera.DoAlert("Autocraft stopped waiting for host to send crafted item.", false);
						yield break;
					}
					ReachableItems.InvalidateGround();
					CraftingModule.InvalidateInventoryCache();
					CraftingModule.ScheduleRefresh(camera);
					yield return null;
					request = null;
				}
			}
		}
		finally
		{
			if (clientPlan == plan)
			{
				clientPlan = null;
				clientCamera = null;
				clientBody = null;
				ReachableItems.InvalidateGround();
				CraftingModule.InvalidateInventoryCache();
				CraftingModule.ForceRefresh(camera);
			}
		}
	}

	internal static float CountRecipeResult(Body body, Recipe recipe)
	{
		RecipeResult val = recipe?.result;
		if ((Object)(object)body == (Object)null || val == null || string.IsNullOrEmpty(val.id))
		{
			return 0f;
		}
		List<Item> list = ReachableItems.Gather(body);
		float num = 0f;
		for (int i = 0; i < list.Count; i++)
		{
			Item val2 = list[i];
			if ((Object)(object)val2 == (Object)null)
			{
				continue;
			}
			if (val.isLiquid)
			{
				WaterContainerItem component = ((Component)val2).GetComponent<WaterContainerItem>();
				if ((Object)(object)component == (Object)null || component.stack == null)
				{
					continue;
				}
				foreach (LiquidStack item in component.stack)
				{
					if (item != null && item.liquidId == val.id)
					{
						num += item.amount;
					}
				}
			}
			else if (val2.id == val.id)
			{
				num += 1f;
			}
		}
		return num;
	}

	private static bool CraftOne(Body body, Recipe recipe, CraftBackend backend)
	{
		switch (backend)
		{
		case CraftBackend.MultiplayerHost:
			return MultiplayerCompat.CraftStepAsHost(body, recipe);
		case CraftBackend.MultiplayerClient:
			return MultiplayerCompat.CraftStepAsClient(PlayerCamera.main, recipe);
		default:
			recipe.TryMake();
			return true;
		}
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
