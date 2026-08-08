using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using DiscordRPC;
using DiscordRPC.Events;
using DiscordRPC.IO;
using DiscordRPC.Logging;
using DiscordRPC.Message;
using UnityEngine;

namespace QoL_Unknown;

public class DiscordManager : MonoBehaviour
{
	[Serializable]
	[CompilerGenerated]
	private sealed class _003C_003Ec
	{
		public static readonly _003C_003Ec _003C_003E9 = new _003C_003Ec();

		public static OnErrorEvent _003C_003E9__17_0;

		internal void _003CInitialize_003Eb__17_0(object sender, ErrorMessage e)
		{
			Debug.LogError((object)("Discord RPC Error: " + e.Message));
		}
	}

	private DiscordRpcClient client;

	private float updateTimer = 0f;

	private float invokeTimer = 0f;

	private const float UpdateInterval = 5f;

	private const float InvokeInterval = 0.25f;

	private bool suppressPresenceUpdates;

	private DateTime sessionStartTime;

	private static readonly Dictionary<Type, Dictionary<string, FieldInfo>> BodyFieldCache = new Dictionary<Type, Dictionary<string, FieldInfo>>();

	private static readonly Dictionary<Type, Dictionary<string, PropertyInfo>> BodyPropertyCache = new Dictionary<Type, Dictionary<string, PropertyInfo>>();

	private static readonly Dictionary<string, string> limbNameMap = new Dictionary<string, string>
	{
		{ "Head", "head" },
		{ "UpTorso", "thorax" },
		{ "DownTorso", "abdomen" },
		{ "UpArmF", "upper right arm" },
		{ "DownArmF", "right forearm" },
		{ "HandF", "right hand" },
		{ "UpArmB", "upper left arm" },
		{ "DownArmB", "left forearm" },
		{ "HandB", "left hand" },
		{ "ThighF", "right thigh" },
		{ "CrusF", "right shins" },
		{ "FootF", "right foot" },
		{ "ThighB", "left thigh" },
		{ "CrusB", "left shins" },
		{ "FootB", "left foot" }
	};

	public static DiscordManager Instance { get; private set; }

	private void Awake()
	{
		if ((Object)(object)Instance != (Object)null && (Object)(object)Instance != (Object)(object)this)
		{
			Object.Destroy((Object)(object)this);
		}
		else
		{
			Instance = this;
		}
	}

	private void OnEnable()
	{
	}

	private void OnDisable()
	{
	}

	private void Start()
	{
		Initialize();
	}

	public void Initialize()
	{
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Expected O, but got Unknown
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Expected O, but got Unknown
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Expected O, but got Unknown
		try
		{
			if ((Object)(object)Instance == (Object)null)
			{
				Instance = this;
			}
			sessionStartTime = DateTime.UtcNow;
			client = new DiscordRpcClient("1453021186689859757", -1, (ILogger)null, false, (INamedPipeClient)null);
			client.Logger = (ILogger)new ConsoleLogger
			{
				Level = (LogLevel)3
			};
			DiscordRpcClient obj = client;
			object obj2 = _003C_003Ec._003C_003E9__17_0;
			if (obj2 == null)
			{
				OnErrorEvent val = delegate(object sender, ErrorMessage e)
				{
					Debug.LogError((object)("Discord RPC Error: " + e.Message));
				};
				_003C_003Ec._003C_003E9__17_0 = val;
				obj2 = (object)val;
			}
			obj.OnError += (OnErrorEvent)obj2;
			if (!client.Initialize())
			{
				suppressPresenceUpdates = true;
				return;
			}
			for (int num = 0; num < 10; num++)
			{
				client.Invoke();
			}
			UpdatePresence();
		}
		catch (Exception ex)
		{
			Debug.LogError((object)("Exception during dsc initialization: " + ex.Message + "\n" + ex.StackTrace));
			suppressPresenceUpdates = true;
		}
	}

	private void Update()
	{
		if (client != null && client.IsInitialized)
		{
			invokeTimer += Time.unscaledDeltaTime;
			if (invokeTimer >= 0.25f)
			{
				invokeTimer = 0f;
				try
				{
					client.Invoke();
				}
				catch (Exception ex)
				{
					Debug.LogError((object)("Error invoking client: " + ex.Message));
				}
			}
		}
		updateTimer += Time.unscaledDeltaTime;
		if (updateTimer >= 5f)
		{
			updateTimer = 0f;
			UpdatePresence();
		}
	}

	public static string LimbToName(string id)
	{
		string value;
		return limbNameMap.TryGetValue(id, out value) ? value : id;
	}

	internal static float GetBodyFloatCompat(Body body, float fallbackValue, params string[] fieldNames)
	{
		if ((Object)(object)body == (Object)null || fieldNames == null || fieldNames.Length == 0)
		{
			return fallbackValue;
		}
		Type type = ((object)body).GetType();
		foreach (string text in fieldNames)
		{
			if (string.IsNullOrEmpty(text))
			{
				continue;
			}
			FieldInfo cachedBodyField = GetCachedBodyField(type, text);
			if (!(cachedBodyField == null))
			{
				object value = cachedBodyField.GetValue(body);
				if (value is float result)
				{
					return result;
				}
				if (value is double num)
				{
					return (float)num;
				}
				if (value is int num2)
				{
					return num2;
				}
			}
		}
		return fallbackValue;
	}

	internal static bool GetBodyBoolCompat(Body body, bool fallbackValue, params string[] fieldNames)
	{
		if ((Object)(object)body == (Object)null || fieldNames == null || fieldNames.Length == 0)
		{
			return fallbackValue;
		}
		Type type = ((object)body).GetType();
		foreach (string text in fieldNames)
		{
			if (string.IsNullOrEmpty(text))
			{
				continue;
			}
			FieldInfo cachedBodyField = GetCachedBodyField(type, text);
			if (cachedBodyField != null)
			{
				object value = cachedBodyField.GetValue(body);
				if (value is bool result)
				{
					return result;
				}
				if (value is int num)
				{
					return num != 0;
				}
			}
			PropertyInfo cachedBodyProperty = GetCachedBodyProperty(type, text);
			if (!(cachedBodyProperty == null) && cachedBodyProperty.CanRead)
			{
				object value2 = cachedBodyProperty.GetValue(body, null);
				if (value2 is bool result2)
				{
					return result2;
				}
				if (value2 is int num2)
				{
					return num2 != 0;
				}
			}
		}
		return fallbackValue;
	}

	private static FieldInfo GetCachedBodyField(Type bodyType, string fieldName)
	{
		if (!BodyFieldCache.TryGetValue(bodyType, out var value))
		{
			value = new Dictionary<string, FieldInfo>(StringComparer.Ordinal);
			BodyFieldCache[bodyType] = value;
		}
		if (!value.TryGetValue(fieldName, out var value2))
		{
			value2 = (value[fieldName] = bodyType.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
		}
		return value2;
	}

	private static PropertyInfo GetCachedBodyProperty(Type bodyType, string propertyName)
	{
		if (!BodyPropertyCache.TryGetValue(bodyType, out var value))
		{
			value = new Dictionary<string, PropertyInfo>(StringComparer.Ordinal);
			BodyPropertyCache[bodyType] = value;
		}
		if (!value.TryGetValue(propertyName, out var value2))
		{
			value2 = (value[propertyName] = bodyType.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
		}
		return value2;
	}

	public static string LargestDanger(Body body)
	{
		float bodyFloatCompat = GetBodyFloatCompat(body, 100f, "oxygenAmount", "bloodOxygen");
		float bodyFloatCompat2 = GetBodyFloatCompat(body, 100f, "bloodAmount", "bloodVolume");
		float bodyFloatCompat3 = GetBodyFloatCompat(body, 120f, "bloodPressure");
		float bodyFloatCompat4 = GetBodyFloatCompat(body, 0f, "fibrillationProgress");
		bool bodyBoolCompat = GetBodyBoolCompat(body, false, "fibrillationRising");
		float bodyFloatCompat5 = GetBodyFloatCompat(body, 0f, "strokeAmount");
		float bodyFloatCompat6 = GetBodyFloatCompat(body, 70f, "heartRate");
		bool flag = GetBodyBoolCompat(body, false, "inCardiacArrest") || bodyFloatCompat6 < 20f;
		if (body.brainHealth < 20f)
		{
			return $"Comatose! {body.brainHealth:0}% brain health.";
		}
		if (flag)
		{
			return "Cardiac arrest. No pulse detected.";
		}
		if (bodyFloatCompat4 > 80f)
		{
			return $"Fibrillation crisis! {bodyFloatCompat4:0}% severity.";
		}
		if (bodyBoolCompat && bodyFloatCompat4 > 45f)
		{
			return $"Fibrillation worsening... ({bodyFloatCompat4:0}% severity).";
		}
		if (!WorldGeneration.unchipped && bodyFloatCompat5 > 35f)
		{
			return $"Experiencing a stroke. ({bodyFloatCompat5:0}% severity).";
		}
		if (bodyFloatCompat <= 5f)
		{
			return $"Cardiac Arrest!! {body.brainHealth:0}% brain health.";
		}
		if (bodyFloatCompat3 < 60f)
		{
			return $"Profound hypotension! {bodyFloatCompat3:0} mmHg.";
		}
		if (bodyFloatCompat3 < 75f)
		{
			return $"Hypotensive! {bodyFloatCompat3:0} mmHg.";
		}
		if (bodyFloatCompat3 > 180f)
		{
			return $"Hypertensive crisis! {bodyFloatCompat3:0} mmHg.";
		}
		if (bodyFloatCompat3 > 160f)
		{
			return $"Severe hypertension! {bodyFloatCompat3:0} mmHg.";
		}
		float num = body.bloodToLiters(body.totalBleedSpeed) * 60f;
		if (num > 0.3f)
		{
			return $"Bleeding Out!! {num:0.00}L/m bleed rate.";
		}
		if (body.inWater && bodyFloatCompat < 30f)
		{
			return $"Drowning!! {bodyFloatCompat:0}% oxygen remaining.";
		}
		if (body.limbs.Length != 0 && body.limbs[0].muscleHealth < 4f)
		{
			return $"Lung failure! {body.limbs[0].muscleHealth:0} thorax health.";
		}
		if (body.horrifiedLevel > 0f)
		{
			return "RUNNING FROM AN ELDER THORNBACK!!";
		}
		if (WorldGeneration.unchipped)
		{
			return "Chipless. Status unknown.";
		}
		if (bodyFloatCompat2 < 30f)
		{
			return $"Hypovolemic! {bodyFloatCompat2:F2}L blood amount.";
		}
		if (body.septicShock > 52.5f)
		{
			return $"In septic shock! {body.septicShock:0}% severity.";
		}
		if (body.radiationSickness > 70f)
		{
			return $"Irradiated! {body.radiationSickness:0}% severity.";
		}
		if (body.temperature < 28f && body.consciousness < 30f)
		{
			return $"Hypothermic! {body.temperature:F1}C body temp.";
		}
		if (body.temperature > 41.5f)
		{
			return $"Heatstroke! {body.temperature:F1}C body temp.";
		}
		if (body.hunger < 0f && body.limbs.Length != 0 && body.limbs[0].muscleHealth < 25f)
		{
			return $"Starving! {body.hunger:0}% fullness.";
		}
		if (body.thirst < 0f && bodyFloatCompat < 60f)
		{
			return $"Dessicated! {body.thirst:0}% thirst.";
		}
		int num2 = Mathf.RoundToInt(body.averagePain);
		if (num2 > 96)
		{
			return $"In shock from {num2}% pain!";
		}
		if (Object.op_Implicit((Object)(object)body.mindWipe) && body.mindWipe.active)
		{
			return " .. Hollow .. ";
		}
		string text = null;
		string text2 = null;
		string text3 = null;
		int num3 = 0;
		int num4 = 0;
		for (int i = 0; i < body.limbs.Length; i++)
		{
			Limb val = body.limbs[i];
			if (text == null && val.broken)
			{
				text = ((Object)val).name;
			}
			if (text2 == null && val.hasShrapnel)
			{
				text2 = ((Object)val).name;
			}
			if (text3 == null && val.dislocated)
			{
				text3 = ((Object)val).name;
			}
			if (val.infectionAmount > 10f)
			{
				num3++;
				int num5 = Mathf.RoundToInt(val.infectionAmount);
				if (num5 > num4)
				{
					num4 = num5;
				}
			}
		}
		if (text != null)
		{
			return "Healing a " + LimbToName(text) + " fracture...";
		}
		if (num > 0f)
		{
			return $"Treating {num:0.00}L/m of bleeding...";
		}
		if (text2 != null)
		{
			return "Removing shrapnel from the " + LimbToName(text2) + "...";
		}
		if (text3 != null)
		{
			return "Tending a " + LimbToName(text3) + " dislocation...";
		}
		if (body.brainHealth < 95f)
		{
			string arg = ((body.brainHealth < 60f) ? "Severe" : ((body.brainHealth < 90f) ? "Moderate" : "Minor"));
			return $"{arg} brain damage! ({body.brainHealth:0}% brain health)";
		}
		if (body.energy < 10f)
		{
			return $"Taking a nap... {body.energy:0}% energy..";
		}
		if (num2 > 50)
		{
			return $"In {num2}% pain. Ouch!";
		}
		if (body.sicknessAmount > 35f)
		{
			string arg2 = ((body.sicknessAmount > 70f) ? "extremely" : ((body.sicknessAmount > 50f) ? "very" : "a bit"));
			return $"Feeling {arg2} sick. ({Mathf.RoundToInt(body.sicknessAmount)}% sickness)";
		}
		if (num4 > 30)
		{
			string arg3 = ((num4 > 70) ? "severely" : ((num4 > 40) ? "badly" : "mildly"));
			return string.Format("{0} {1} infected! ({2}% infection)", (num3 > 1) ? (num3 + " limbs are") : "A limb is", arg3, num4);
		}
		if (body.stamina < 40f)
		{
			return $"{body.stamina:0}% exhausted... phew..";
		}
		if (body.totalHappiness < -5f)
		{
			string arg4 = ((body.totalHappiness < -80f) ? "suicidal" : ((body.totalHappiness < -50f) ? "depressed" : "down"));
			return $"Feeling {arg4}... ({body.totalHappiness:F1} happiness)";
		}
		if (body.hunger < 66f)
		{
			string arg5 = ((body.hunger < 20f) ? "Near-starving" : ((body.hunger < 50f) ? "Feeling hungry" : "Feeling peckish"));
			return $"{arg5}.. ({body.hunger:0} hunger)";
		}
		if (body.thirst < 66f)
		{
			string arg6 = ((body.thirst < 15f) ? "Parched of water!" : ((body.thirst < 40f) ? "Feeling very thirsty" : "Feeling a bit thirsty"));
			return $"{arg6}.. ({body.thirst:0} thirst)";
		}
		string arg7 = ((body.happiness > 80f) ? "ecstatic" : ((body.happiness > 50f) ? "happy" : "content"));
		string arg8 = ((body.hunger > 80f) ? " and well-fed." : ((body.hunger > 50f) ? " and satisfied." : "."));
		return $"Feeling {arg7}{arg8} ({body.happiness:F1} happiness)";
	}

	private static bool IsTutorialRun(WorldGeneration world)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Invalid comparison between Unknown and I4
		return (Object)(object)world != (Object)null && world.worldExists && (int)world.biomeOverride == 1;
	}

	private static string BuildTutorialState()
	{
		TutorialHandler main = TutorialHandler.main;
		TutorialCourse val = (((Object)(object)main != (Object)null) ? main.activeCourse : null);
		string text = (((Object)(object)val != (Object)null) ? val.LocaleName() : null);
		return (text ?? string.Empty).ToLowerInvariant() switch
		{
			"sandbox" => "In the sandbox", 
			"craftingcourse" => "Currently attempting the crafting course", 
			"firstaidcourse" => "Currently attempting the first aid course", 
			"basicscourse" => "Currently attempting the basics course", 
			"escapecourse" => "Being tranquilized.!", 
			_ => "In the tutorial", 
		};
	}

	private static void AddRuleValue(DiscordPresenceContext context, string name, double value)
	{
		if (context.RuleValues == null)
		{
			context.RuleValues = new Dictionary<string, DiscordPresenceRuleValue>(StringComparer.OrdinalIgnoreCase);
		}
		context.RuleValues[name] = DiscordPresenceRuleValue.FromNumber(value);
	}

	private static void AddRuleValue(DiscordPresenceContext context, string name, bool value)
	{
		if (context.RuleValues == null)
		{
			context.RuleValues = new Dictionary<string, DiscordPresenceRuleValue>(StringComparer.OrdinalIgnoreCase);
		}
		context.RuleValues[name] = DiscordPresenceRuleValue.FromBoolean(value);
	}

	private static void AddRuleValue(DiscordPresenceContext context, string name, string value)
	{
		if (context.RuleValues == null)
		{
			context.RuleValues = new Dictionary<string, DiscordPresenceRuleValue>(StringComparer.OrdinalIgnoreCase);
		}
		context.RuleValues[name] = DiscordPresenceRuleValue.FromString(value);
	}

	private static void PopulateBaseRuleValues(DiscordPresenceContext context)
	{
		AddRuleValue(context, "InWorld", context.InWorld);
		AddRuleValue(context, "InTutorial", context.IsTutorial);
		AddRuleValue(context, "Alive", context.Alive);
		AddRuleValue(context, "Depth", context.Depth);
		AddRuleValue(context, "BiomeDepth", context.BiomeDepth);
		AddRuleValue(context, "Unchipped", (Object)(object)WorldGeneration.world != (Object)null && WorldGeneration.world.unchippedMode);
	}

	private static void PopulateBodyRuleValues(DiscordPresenceContext context, Body body)
	{
		if ((Object)(object)body == (Object)null)
		{
			return;
		}
		float bodyFloatCompat = GetBodyFloatCompat(body, 100f, "oxygenAmount", "bloodOxygen");
		float bodyFloatCompat2 = GetBodyFloatCompat(body, 100f, "bloodAmount", "bloodVolume");
		float bodyFloatCompat3 = GetBodyFloatCompat(body, 120f, "bloodPressure");
		float bodyFloatCompat4 = GetBodyFloatCompat(body, 0f, "fibrillationProgress");
		bool bodyBoolCompat = GetBodyBoolCompat(body, false, "fibrillationRising");
		float bodyFloatCompat5 = GetBodyFloatCompat(body, 0f, "strokeAmount");
		float bodyFloatCompat6 = GetBodyFloatCompat(body, 70f, "heartRate");
		bool value = GetBodyBoolCompat(body, false, "inCardiacArrest") || bodyFloatCompat6 < 20f;
		bool value2 = bodyBoolCompat && bodyFloatCompat4 > 45f;
		float num = body.bloodToLiters(body.totalBleedSpeed) * 60f;
		float num2 = ((body.limbs != null && body.limbs.Length != 0 && (Object)(object)body.limbs[0] != (Object)null) ? body.limbs[0].muscleHealth : 100f);
		bool value3 = Object.op_Implicit((Object)(object)body.mindWipe) && body.mindWipe.active;
		bool value4 = (!((Object)(object)WorldGeneration.world != (Object)null) || !WorldGeneration.world.unchippedMode) && bodyFloatCompat5 > 35f;
		bool value5 = num > 0.3f;
		bool value6 = body.inWater && bodyFloatCompat < 30f;
		bool value7 = body.temperature < 28f && body.consciousness < 30f;
		bool value8 = body.hunger < 0f && num2 < 25f;
		bool value9 = body.thirst < 0f && bodyFloatCompat < 60f;
		bool flag = false;
		bool flag2 = false;
		bool flag3 = false;
		string value10 = string.Empty;
		string value11 = string.Empty;
		string value12 = string.Empty;
		int num3 = 0;
		int num4 = 0;
		if (body.limbs != null)
		{
			for (int i = 0; i < body.limbs.Length; i++)
			{
				Limb val = body.limbs[i];
				if ((Object)(object)val == (Object)null)
				{
					continue;
				}
				if (!flag && val.broken)
				{
					flag = true;
					value10 = LimbToName(((Object)val).name);
				}
				if (!flag2 && val.hasShrapnel)
				{
					flag2 = true;
					value11 = LimbToName(((Object)val).name);
				}
				if (!flag3 && val.dislocated)
				{
					flag3 = true;
					value12 = LimbToName(((Object)val).name);
				}
				if (val.infectionAmount > 10f)
				{
					num3++;
					int num5 = Mathf.RoundToInt(val.infectionAmount);
					if (num5 > num4)
					{
						num4 = num5;
					}
				}
			}
		}
		AddRuleValue(context, "BrainHealth", body.brainHealth);
		AddRuleValue(context, "Health", body.brainHealth);
		AddRuleValue(context, "Consciousness", body.consciousness);
		AddRuleValue(context, "Oxygen", bodyFloatCompat);
		AddRuleValue(context, "BloodOxygen", bodyFloatCompat);
		AddRuleValue(context, "BloodAmount", bodyFloatCompat2);
		AddRuleValue(context, "BloodVolume", bodyFloatCompat2);
		AddRuleValue(context, "BloodPressure", bodyFloatCompat3);
		AddRuleValue(context, "HeartRate", bodyFloatCompat6);
		AddRuleValue(context, "InCardiacArrest", value);
		AddRuleValue(context, "FibrillationProgress", bodyFloatCompat4);
		AddRuleValue(context, "FibrillationRising", bodyBoolCompat);
		AddRuleValue(context, "FibrillationWorsening", value2);
		AddRuleValue(context, "StrokeAmount", bodyFloatCompat5);
		AddRuleValue(context, "StrokeRisk", value4);
		AddRuleValue(context, "BleedRatePerMinute", num);
		AddRuleValue(context, "TotalBleedSpeed", body.totalBleedSpeed);
		AddRuleValue(context, "BleedingOut", value5);
		AddRuleValue(context, "Hunger", body.hunger);
		AddRuleValue(context, "Thirst", body.thirst);
		AddRuleValue(context, "Energy", body.energy);
		AddRuleValue(context, "Stamina", body.stamina);
		AddRuleValue(context, "Happiness", body.happiness);
		AddRuleValue(context, "TotalHappiness", body.totalHappiness);
		AddRuleValue(context, "AveragePain", body.averagePain);
		AddRuleValue(context, "SepticShock", body.septicShock);
		AddRuleValue(context, "RadiationSickness", body.radiationSickness);
		AddRuleValue(context, "Temperature", body.temperature);
		AddRuleValue(context, "SicknessAmount", body.sicknessAmount);
		AddRuleValue(context, "InWater", body.inWater);
		AddRuleValue(context, "Drowning", value6);
		AddRuleValue(context, "HypothermicUnconscious", value7);
		AddRuleValue(context, "StarvingWithLungDamage", value8);
		AddRuleValue(context, "Dessicated", value9);
		AddRuleValue(context, "HorrifiedLevel", body.horrifiedLevel);
		AddRuleValue(context, "MindWipeActive", value3);
		AddRuleValue(context, "ThoraxMuscleHealth", num2);
		AddRuleValue(context, "HasBrokenLimb", flag);
		AddRuleValue(context, "HasShrapnel", flag2);
		AddRuleValue(context, "HasDislocation", flag3);
		AddRuleValue(context, "InfectedLimbCount", num3);
		AddRuleValue(context, "MaxLimbInfection", num4);
		AddRuleValue(context, "BrokenLimbName", value10);
		AddRuleValue(context, "ShrapnelLimbName", value11);
		AddRuleValue(context, "DislocatedLimbName", value12);
		AddRuleValue(context, "BrainDamageDescriptor", (body.brainHealth < 60f) ? "Severe" : ((body.brainHealth < 90f) ? "Moderate" : "Minor"));
		AddRuleValue(context, "SicknessDescriptor", (body.sicknessAmount > 70f) ? "extremely" : ((body.sicknessAmount > 50f) ? "very" : "a bit"));
		AddRuleValue(context, "InfectionDescriptor", (num4 > 70) ? "severely" : ((num4 > 40) ? "badly" : "mildly"));
		AddRuleValue(context, "InfectionLimbSubject", (num3 > 1) ? (num3 + " limbs") : "A limb");
		AddRuleValue(context, "InfectionVerb", (num3 > 1) ? "are" : "is");
		AddRuleValue(context, "SadnessDescriptor", (body.totalHappiness < -80f) ? "suicidal" : ((body.totalHappiness < -50f) ? "depressed" : "down"));
		AddRuleValue(context, "HungerDescriptor", (body.hunger < 20f) ? "Near-starving" : ((body.hunger < 50f) ? "Feeling hungry" : "Feeling peckish"));
		AddRuleValue(context, "ThirstDescriptor", (body.thirst < 15f) ? "Parched of water!" : ((body.thirst < 40f) ? "Feeling very thirsty" : "Feeling a bit thirsty"));
		AddRuleValue(context, "HappinessDescriptor", (body.happiness > 80f) ? "ecstatic" : ((body.happiness > 50f) ? "happy" : "content"));
		AddRuleValue(context, "FullnessDescriptor", (body.hunger > 80f) ? " and well-fed." : ((body.hunger > 50f) ? " and satisfied." : "."));
	}

	internal static DiscordPresenceContext BuildPresenceContext()
	{
		WorldGeneration world = WorldGeneration.world;
		Body val = PlayerCamera.main?.body;
		DiscordPresenceContext discordPresenceContext = new DiscordPresenceContext
		{
			InWorld = ((Object)(object)world != (Object)null && (Object)(object)val != (Object)null && world.worldExists),
			LayerName = "Depth: Unknown..",
			LastSpeech = (string.IsNullOrWhiteSpace(DiscordRPCHelper.LastSpokenSentence) ? "..." : DiscordRPCHelper.LastSpokenSentence),
			Mode = "In the Main Menu",
			Status = "Preparing to descend...",
			DefaultDetails = "In the Main Menu",
			DefaultState = "Preparing to descend...",
			DefaultLargeImageText = string.Empty,
			DefaultSmallImageText = string.Empty,
			RuleValues = new Dictionary<string, DiscordPresenceRuleValue>(StringComparer.OrdinalIgnoreCase)
		};
		PopulateBaseRuleValues(discordPresenceContext);
		if (!discordPresenceContext.InWorld)
		{
			return discordPresenceContext;
		}
		discordPresenceContext.IsTutorial = IsTutorialRun(world);
		discordPresenceContext.Alive = val.alive;
		discordPresenceContext.Health = val.brainHealth;
		discordPresenceContext.Hunger = val.hunger;
		discordPresenceContext.Thirst = val.thirst;
		discordPresenceContext.Happiness = val.happiness;
		discordPresenceContext.Death = string.Empty;
		PopulateBaseRuleValues(discordPresenceContext);
		PopulateBodyRuleValues(discordPresenceContext, val);
		if (discordPresenceContext.IsTutorial)
		{
			discordPresenceContext.TutorialCourse = BuildTutorialState();
			discordPresenceContext.Mode = (val.alive ? discordPresenceContext.TutorialCourse : "Deceased (Tutorial)");
			discordPresenceContext.Status = (val.alive ? LargestDanger(val) : "Died in tutorial");
			discordPresenceContext.Death = (val.alive ? string.Empty : "Died in tutorial");
			discordPresenceContext.LayerName = "Tutorial";
			discordPresenceContext.DefaultDetails = discordPresenceContext.Mode;
			discordPresenceContext.DefaultState = discordPresenceContext.Status;
			discordPresenceContext.DefaultSmallImageText = "Tutorial";
			return discordPresenceContext;
		}
		int biomeDepth = world.biomeDepth;
		string text = "Depth: Unknown..";
		if (world.biomeTitles != null && biomeDepth >= 0 && biomeDepth < world.biomeTitles.Length)
		{
			string text2 = world.biomeTitles[biomeDepth];
			if (!string.IsNullOrEmpty(text2))
			{
				text = text2;
			}
		}
		int num = Mathf.RoundToInt(world.PlayerTotalDepthMeters());
		string text3 = $"Died at {num}m";
		discordPresenceContext.Depth = num;
		discordPresenceContext.BiomeDepth = biomeDepth;
		discordPresenceContext.LayerName = text;
		discordPresenceContext.Mode = (val.alive ? $"Descending the {text} ({num}m)" : ("Deceased (" + text + ")"));
		discordPresenceContext.Status = (val.alive ? LargestDanger(val) : text3);
		discordPresenceContext.Death = (val.alive ? string.Empty : text3);
		discordPresenceContext.DefaultDetails = discordPresenceContext.Mode;
		discordPresenceContext.DefaultState = discordPresenceContext.Status;
		discordPresenceContext.DefaultLargeImageText = "\"" + discordPresenceContext.LastSpeech + "\"";
		discordPresenceContext.DefaultSmallImageText = text;
		PopulateBaseRuleValues(discordPresenceContext);
		return discordPresenceContext;
	}

	public void UpdatePresence()
	{
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Expected O, but got Unknown
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Expected O, but got Unknown
		//IL_0083: Expected O, but got Unknown
		if (suppressPresenceUpdates || client == null || !client.IsInitialized)
		{
			return;
		}
		try
		{
			DiscordPresenceContext discordPresenceContext = BuildPresenceContext();
			DiscordPresencePreview discordPresencePreview = DiscordPresenceSettings.Render(discordPresenceContext);
			RichPresence val = new RichPresence
			{
				Timestamps = new Timestamps
				{
					Start = sessionStartTime
				},
				Assets = new Assets
				{
					LargeImageKey = "cover",
					SmallImageKey = "1"
				}
			};
			((BaseRichPresence)val).Details = discordPresencePreview.Details;
			((BaseRichPresence)val).State = discordPresencePreview.State;
			if (!string.IsNullOrWhiteSpace(discordPresencePreview.LargeImageText))
			{
				((BaseRichPresence)val).Assets.LargeImageText = discordPresencePreview.LargeImageText;
			}
			if (discordPresenceContext.InWorld && !discordPresenceContext.IsTutorial)
			{
				int num = ((discordPresenceContext.BiomeDepth >= 0) ? discordPresenceContext.BiomeDepth : 0);
				((BaseRichPresence)val).Assets.SmallImageKey = ((num < 5) ? $"{num + 1}" : $"{num % 5 + 2}");
			}
			if (!string.IsNullOrWhiteSpace(discordPresencePreview.SmallImageText))
			{
				((BaseRichPresence)val).Assets.SmallImageText = discordPresencePreview.SmallImageText;
			}
			client.SetPresence(val);
		}
		catch (Exception arg)
		{
			Debug.LogError((object)$"[DiscordManager] UpdatePresence failed: {arg}");
		}
	}

	private void OnDestroy()
	{
		try
		{
			if (client != null && client.IsInitialized)
			{
				client.Dispose();
			}
		}
		catch (Exception)
		{
		}
	}
}
