using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace QoL_Unknown;

internal static class CharacterSwapManager
{
	private const string CommandName = "switchcharacter";

	private static readonly string[] RaceKeys = new string[3] { "experiment", "milky", "dune" };

	private static readonly string[] PrefabNames = new string[3] { "Experiment", "Milky", "Dune" };

	internal static List<string> RaceAutofill()
	{
		return new List<string>(RaceKeys);
	}

	internal static void SwapCommand(string[] args)
	{
		if (args == null || args.Length < 2)
		{
			ConsoleHelper.LogToConsole(ConsoleScript.instance, "<color=orange>Usage: switchcharacter &lt;experiment|milky|dune&gt; [changeSkillRatio=true]</color>");
			return;
		}
		if (!TryParseRace(args[1], out var type))
		{
			ConsoleHelper.LogToConsole(ConsoleScript.instance, "<color=orange>Unknown character \"" + args[1] + "\".</color>");
			return;
		}
		bool result = true;
		string message;
		if (args.Length > 2 && !bool.TryParse(args[2], out result))
		{
			ConsoleHelper.LogToConsole(ConsoleScript.instance, "<color=orange>changeSkillRatio must be true or false.</color>");
		}
		else if (TrySwap(type, result, out message))
		{
			ConsoleHelper.LogToConsole(ConsoleScript.instance, "<color=green>" + message + "</color>");
		}
		else
		{
			ConsoleHelper.LogToConsole(ConsoleScript.instance, "<color=orange>" + message + "</color>");
		}
	}

	private static bool TryParseRace(string value, out int type)
	{
		type = -1;
		if (string.IsNullOrWhiteSpace(value))
		{
			return false;
		}
		for (int i = 0; i < RaceKeys.Length; i++)
		{
			if (RaceKeys[i].Equals(value.Trim(), StringComparison.OrdinalIgnoreCase))
			{
				type = i;
				return true;
			}
		}
		return false;
	}

	private static bool TrySwap(int newType, bool changeSkillRatio, out string message)
	{
		Body val = PlayerCamera.main?.body;
		if ((Object)(object)val == (Object)null || (Object)(object)WorldGeneration.world == (Object)null)
		{
			message = "No active character to swap.";
			return false;
		}
		int charType = val.charType;
		if (charType == newType)
		{
			message = "You're already playing as a " + RaceKeys[newType] + ", silly.";
			return false;
		}
		Skills skills = val.skills;
		int sTR = skills.STR;
		int rES = skills.RES;
		int iNT = skills.INT;
		if (changeSkillRatio)
		{
			ConvertSkills(skills, charType, newType);
		}
		else
		{
			float[] array = Skills.BaseMultipliers(newType);
			skills.STRGainMult = array[0];
			skills.RESGainMult = array[1];
			skills.INTGainMult = array[2];
			skills.UpdateExpBoundaries();
		}
		TransplantVisuals(val, newType);
		val.charType = newType;
		PreRunScript.characterSelected = newType;
		RespriteWearables(val);
		RefreshWoundView(newType);
		message = $"Switched to {RaceKeys[newType]}! STR {sTR} -> {skills.STR}, RES {rES} -> {skills.RES}, INT {iNT} -> {skills.INT}.";
		return true;
	}

	private static void ConvertSkills(Skills skills, int oldType, int newType)
	{
		int[] array = Skills.BaseSkills(oldType);
		int[] array2 = Skills.BaseSkills(newType);
		float[] array3 = Skills.BaseMultipliers(oldType);
		float[] array4 = Skills.BaseMultipliers(newType);
		_ = new int[3] { skills.STR, skills.RES, skills.INT };
		float[] array5 = new float[3] { skills.expSTR, skills.expRES, skills.expINT };
		for (int i = 0; i < 3; i++)
		{
			float num = Mathf.Max(0f, array5[i] - (float)Skills.GetExperienceForLevel(array[i]));
			float num2 = ((array3[i] > 0f) ? (num / array3[i]) : num);
			float num3 = (float)Skills.GetExperienceForLevel(array2[i]) + num2 * array4[i];
			int num4 = LevelForExp(num3);
			switch (i)
			{
			case 0:
				skills.STR = num4;
				skills.expSTR = num3;
				break;
			case 1:
				skills.RES = num4;
				skills.expRES = num3;
				break;
			default:
				skills.INT = num4;
				skills.expINT = num3;
				break;
			}
		}
		skills.STRGainMult = array4[0];
		skills.RESGainMult = array4[1];
		skills.INTGainMult = array4[2];
		skills.UpdateExpBoundaries();
	}

	private static int LevelForExp(float exp)
	{
		int i;
		for (i = 0; i < 100 && (float)Skills.GetExperienceForLevel(i + 1) <= exp; i++)
		{
		}
		return i;
	}

	private static void TransplantVisuals(Body body, int newType)
	{
		GameObject val = Resources.Load<GameObject>("Special/Characters/" + PrefabNames[newType]);
		if ((Object)(object)val == (Object)null)
		{
			return;
		}
		Body componentInChildren = val.GetComponentInChildren<Body>();
		if ((Object)(object)componentInChildren == (Object)null)
		{
			return;
		}
		if ((Object)(object)body.bodyAnimator != (Object)null && (Object)(object)componentInChildren.bodyAnimator != (Object)null && (Object)(object)componentInChildren.bodyAnimator.runtimeAnimatorController != (Object)null)
		{
			body.bodyAnimator.runtimeAnimatorController = componentInChildren.bodyAnimator.runtimeAnimatorController;
		}
		if ((Object)(object)body.armsAnimator != (Object)null && (Object)(object)componentInChildren.armsAnimator != (Object)null && (Object)(object)componentInChildren.armsAnimator.runtimeAnimatorController != (Object)null)
		{
			body.armsAnimator.runtimeAnimatorController = componentInChildren.armsAnimator.runtimeAnimatorController;
		}
		Transform root = ((Component)body).transform.root;
		Dictionary<string, SpriteRenderer> dictionary = new Dictionary<string, SpriteRenderer>();
		CollectRenderers(val.transform, val.transform, dictionary);
		SpriteRenderer[] componentsInChildren = ((Component)root).GetComponentsInChildren<SpriteRenderer>(true);
		foreach (SpriteRenderer val2 in componentsInChildren)
		{
			if (dictionary.TryGetValue(RelativePath(((Component)val2).transform, root), out var value))
			{
				if ((Object)(object)value.sprite != (Object)null)
				{
					val2.sprite = value.sprite;
				}
				if ((Object)(object)((Renderer)value).sharedMaterial != (Object)null)
				{
					((Renderer)val2).sharedMaterial = ((Renderer)value).sharedMaterial;
				}
			}
		}
		TransplantFacialExpression(body, val);
		body.heightMultiplier = componentInChildren.heightMultiplier;
	}

	private static void TransplantFacialExpression(Body body, GameObject prefab)
	{
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		FacialExpression componentInChildren = ((Component)body).GetComponentInChildren<FacialExpression>();
		FacialExpression componentInChildren2 = prefab.GetComponentInChildren<FacialExpression>();
		if ((Object)(object)componentInChildren == (Object)null || (Object)(object)componentInChildren2 == (Object)null)
		{
			return;
		}
		componentInChildren.defaultHead = componentInChildren2.defaultHead;
		componentInChildren.defaultHeadMouth = componentInChildren2.defaultHeadMouth;
		componentInChildren.defaultHeadMouthHalf = componentInChildren2.defaultHeadMouthHalf;
		componentInChildren.eyesGone = componentInChildren2.eyesGone;
		componentInChildren.eyesGoneHealed = componentInChildren2.eyesGoneHealed;
		componentInChildren.disfiguredHead = componentInChildren2.disfiguredHead;
		componentInChildren.disfiguredHeadHeal = componentInChildren2.disfiguredHeadHeal;
		if (componentInChildren.disfiguredHead != null && componentInChildren.disfiguredIndex >= componentInChildren.disfiguredHead.Length)
		{
			componentInChildren.disfiguredIndex = 0;
		}
		if (componentInChildren.eyeList != null && componentInChildren2.eyeList != null)
		{
			int num = Mathf.Min(componentInChildren.eyeList.Count, componentInChildren2.eyeList.Count);
			for (int i = 0; i < num; i++)
			{
				componentInChildren.eyeList[i] = componentInChildren2.eyeList[i];
			}
		}
	}

	private static void CollectRenderers(Transform root, Transform current, Dictionary<string, SpriteRenderer> map)
	{
		SpriteRenderer component = ((Component)current).GetComponent<SpriteRenderer>();
		if ((Object)(object)component != (Object)null)
		{
			map[RelativePath(current, root)] = component;
		}
		for (int i = 0; i < current.childCount; i++)
		{
			CollectRenderers(root, current.GetChild(i), map);
		}
	}

	private static string RelativePath(Transform t, Transform root)
	{
		if ((Object)(object)t == (Object)(object)root)
		{
			return "";
		}
		string text = ((Object)t).name;
		Transform parent = t.parent;
		while ((Object)(object)parent != (Object)null && (Object)(object)parent != (Object)(object)root)
		{
			text = ((Object)parent).name + "/" + text;
			parent = parent.parent;
		}
		return text;
	}

	private static void RespriteWearables(Body body)
	{
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		foreach (Item allWearable in body.GetAllWearables())
		{
			if ((Object)(object)allWearable == (Object)null)
			{
				continue;
			}
			WearableSpriteSwap component = ((Component)allWearable).GetComponent<WearableSpriteSwap>();
			if ((Object)(object)component != (Object)null)
			{
				SpriteRenderer component2 = ((Component)allWearable).GetComponent<SpriteRenderer>();
				if ((Object)(object)component2 != (Object)null && component.sprites.Count > body.charType && (Object)(object)component.sprites[body.charType] != (Object)null)
				{
					component2.sprite = component.sprites[body.charType];
				}
				if (component.customScale.Count > body.charType && component.customScale[body.charType] != 0f)
				{
					((Component)allWearable).transform.localScale = Vector3.one * component.customScale[body.charType];
				}
			}
			Wearable component3 = ((Component)allWearable).GetComponent<Wearable>();
			if ((Object)(object)component3 != (Object)null)
			{
				component3.CreateSprites(body, component);
			}
		}
	}

	private static void RefreshWoundView(int newType)
	{
		WoundView view = WoundView.view;
		if ((Object)(object)view == (Object)null)
		{
			return;
		}
		if ((Object)(object)view.speciesText != (Object)null)
		{
			((TMP_Text)view.speciesText).text = Locale.GetOther(RaceKeys[newType]).ToUpper();
		}
		int num = newType * 2;
		if (view.headCharSprites == null || view.headCharSprites.Length <= num + 1 || view.limbImages.Length == 0)
		{
			return;
		}
		Image val = view.limbImages[0];
		val.sprite = view.headCharSprites[num];
		((Graphic)val).SetNativeSize();
		if (((Component)val).transform.childCount > 0)
		{
			Image component = ((Component)((Component)val).transform.GetChild(0)).GetComponent<Image>();
			if ((Object)(object)component != (Object)null)
			{
				component.sprite = view.headCharSprites[num + 1];
				((Graphic)component).SetNativeSize();
			}
		}
	}
}
