using System.Collections;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace QoL_Unknown;

internal static class EtomophobiaPatcher
{
	private const string PrefKey = "Etomophobia";

	private static ManualLogSource Logger;

	private static bool initialized;

	private static GameObject blankSplatPrefab;

	internal static bool Enabled => PlayerPrefs.GetInt("Etomophobia", 0) == 1;

	private static GameObject BlankSplatPrefab
	{
		get
		{
			//IL_0016: Unknown result type (might be due to invalid IL or missing references)
			//IL_0020: Expected O, but got Unknown
			if ((Object)(object)blankSplatPrefab == (Object)null)
			{
				blankSplatPrefab = new GameObject("QoLBlankSplat");
				blankSplatPrefab.AddComponent<SpriteRenderer>();
				Object.DontDestroyOnLoad((Object)(object)blankSplatPrefab);
				blankSplatPrefab.SetActive(false);
			}
			return blankSplatPrefab;
		}
	}

	public static void Initialize(ManualLogSource logger)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Expected O, but got Unknown
		Logger = logger;
		if (!initialized)
		{
			initialized = true;
			Harmony val = new Harmony("org.bepinex.plugins.qol.etomophobia");
			val.PatchAll(typeof(EtomophobiaPatcher));
		}
	}

	internal static void SetEnabled(bool enabled)
	{
		PlayerPrefs.SetInt("Etomophobia", enabled ? 1 : 0);
		PlayerPrefs.Save();
	}

	[HarmonyPatch(typeof(Vomiter), "Vomit")]
	[HarmonyPrefix]
	private static bool Vomit_Prefix(Vomiter __instance)
	{
		if (!Enabled)
		{
			return true;
		}
		Traverse val = Traverse.Create((object)__instance);
		if (val.Field("vomiting").GetValue<bool>())
		{
			float value = val.Field("vomitPower").GetValue<float>();
			val.Field("vomitPower").SetValue((object)(value + 1f / (value + 1f)));
			return false;
		}
		val.Field("vomiting").SetValue((object)true);
		((MonoBehaviour)__instance).StartCoroutine(SimpleDoVomit(__instance));
		return false;
	}

	[HarmonyPatch(typeof(Vomiter), "VomitBlood")]
	[HarmonyPrefix]
	private static bool VomitBlood_Prefix(Vomiter __instance)
	{
		if (!Enabled)
		{
			return true;
		}
		((MonoBehaviour)__instance).StartCoroutine(SimpleDoBloodVomit(__instance));
		return false;
	}

	private static IEnumerator SimpleDoVomit(Vomiter vomiter)
	{
		Body body = ((Component)vomiter).GetComponent<Body>();
		Traverse t = Traverse.Create((object)vomiter);
		float timer = 0f;
		while (timer < 1f)
		{
			if (body.conscious)
			{
				PlayerCamera.main.SetTimeScale((SpeedType)0, false, false);
			}
			body.temporarySlowdown = timer * 0.15f;
			timer += Time.deltaTime;
			vomiter.badLookAmount = timer * 0.4f;
			body.consciousness = Mathf.Min(body.consciousness, 100f - timer * 13.5f);
			yield return null;
		}
		if (body.alive)
		{
			body.DropItem(2);
			float vomitPower = t.Field("vomitPower").GetValue<float>();
			body.happiness -= 2.5f;
			body.hunger -= 17f + vomitPower * 5f;
			body.thirst -= 10f + vomitPower * 5f;
			body.sicknessAmount -= 8f + vomitPower * 5f;
			body.bloodVolume -= 1f;
			if (vomitPower > 0f)
			{
				body.internalBleeding += vomitPower * 10f;
			}
			body.temporarySlowdown = 0.95f;
			if (body.sleeping && !Object.op_Implicit((Object)(object)((Component)body).GetComponent<SleepingPills>()))
			{
				body.WakeUp();
			}
		}
		t.Field("vomiting").SetValue((object)false);
		t.Field("vomitPower").SetValue((object)0f);
	}

	private static IEnumerator SimpleDoBloodVomit(Vomiter vomiter)
	{
		Body body = ((Component)vomiter).GetComponent<Body>();
		float timer = 0f;
		while (timer < 1f)
		{
			if (body.conscious)
			{
				PlayerCamera.main.SetTimeScale((SpeedType)0, false, false);
			}
			body.temporarySlowdown = timer * 0.15f;
			timer += Time.deltaTime;
			vomiter.badLookAmount = timer * 0.4f;
			body.consciousness = Mathf.Min(body.consciousness, 100f - timer * 10f);
			yield return null;
		}
		if (body.alive)
		{
			body.DropItem(2);
			body.happiness -= 0.8f;
			body.temporarySlowdown = 0.6f;
			body.thirst -= 1f;
			body.bloodVolume -= 1f;
			if (body.sleeping && !Object.op_Implicit((Object)(object)((Component)body).GetComponent<SleepingPills>()))
			{
				body.WakeUp();
			}
		}
	}

	[HarmonyPatch(typeof(Talker), "Vomit")]
	[HarmonyPrefix]
	private static bool Talker_Vomit_Prefix()
	{
		return !Enabled;
	}

	[HarmonyPatch(typeof(Talker), "VomitBlood")]
	[HarmonyPrefix]
	private static bool Talker_VomitBlood_Prefix()
	{
		return !Enabled;
	}

	[HarmonyPatch(typeof(BleedParticle), "Start")]
	[HarmonyPrefix]
	private static bool BleedParticle_Start_Prefix(BleedParticle __instance)
	{
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		if (!__instance.vomit || !Enabled)
		{
			return true;
		}
		Traverse val = Traverse.Create((object)__instance);
		val.Field("every").SetValue((object)1);
		ParticleSystem component = ((Component)__instance).GetComponent<ParticleSystem>();
		val.Field("part").SetValue((object)component);
		val.Field("wallBleed").SetValue((object)BlankSplatPrefab);
		val.Field("groundBleed").SetValue((object)BlankSplatPrefab);
		Traverse obj = val.Field("particles");
		MainModule main = component.main;
		obj.SetValue((object)new Particle[((MainModule)(ref main)).maxParticles]);
		if ((Object)(object)component != (Object)null)
		{
			EmissionModule emission = component.emission;
			((EmissionModule)(ref emission)).enabled = false;
		}
		return false;
	}
}
