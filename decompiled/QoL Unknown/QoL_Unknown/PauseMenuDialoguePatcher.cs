using System.Collections.Generic;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace QoL_Unknown;

internal static class PauseMenuDialoguePatcher
{
	private const string HarmonyId = "org.bepinex.plugins.qol_unknown.pause_menu_dialogue";

	private static readonly List<string> customQuotes = new List<string>
	{
		"Sleep, those little slices of death. How I loathe them.", "Also try Custom Structures!", "Also try CUCoreLib!", "Those burial-clouds that lower victorious soon to devour all", "This shall be a story like none other before.", "I had planned my life's endeavor", "but my life had plans for me.", "I am lost but not forever,", "I am where I am meant to be.", "Imagine now unbearably, how unutterly cold the universe would be if one is all alone.",
		"There's really no hard limit to how long these pause menu names can be and to be quite honest I'm rather curious to see how far we can go. Adolphus W. Green (1844–1917) started as the Principal of the Groton School in 1864. By 1865, he became second assistant librarian - oh wait nevermind there' a limit.", "Astralsounds", "Everything will be okay.", "Remember to do an act of good today.", "Pause and hydrate. It'll help.", "Don't forget to eat, now...", "Little one.", "You're almost there. Just a couple more steps.", "To the north, a new future.", "To reach the antechamber.",
		"To remember, to forget. To forget, to remember.", "Take care of yourself. You deserve it.", "From now on you’re going to have to think. There’s a reason why we’re born with brains in our heads, not rocks.", "Remember the past, live in the present, and fear not the future, for it doesn't exist and never shall. There is only now", "Change itself is nether good nor bad, but knowledge is always useful.", "Then I shall keep myself, to myself.", "Do questions not taste best in the absence of answers?", "In the silent, hidden spaces where we watch and wait, we see the artistry of all that is.", "When a being dies, a world dies.", "Never is a very, very long time.",
		"We're all just stories in the end, so let's make ours a good one.", "You cannot retreat from this fight. Stand with pride and fight with honor.", "...to have turned the wheel and approached genius...", "We are creatures of habit.", "Fear is a good servant, but a poor leader.", "Wisdom is neither a knowing nor a finding but rather a seeking.", "A box without hinges, key, or lid, yet golden treasure inside is hid.", "One learns more from defeat than victory. Therefore, fear the general that has never won a battle.", "The price of entry to being good at something is to suck at that something.", "Death is only the end if you think the story is about you.",
		"If you bring only these quotes, then it will be bereft of the meaning and lesson. You must seek these lessons and live them.", "The gods envy us, because of our mortality. Death is what makes everything worthwhile and beautiful.", "When they speak, they shake the very earth.", "Don't be afraid. Jump.", "Un du evarínya ono varda."
	};

	private static ManualLogSource Logger;

	private static bool initialized;

	public static void Initialize(ManualLogSource logger)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Expected O, but got Unknown
		if (!initialized)
		{
			initialized = true;
			Logger = logger;
			Harmony val = new Harmony("org.bepinex.plugins.qol_unknown.pause_menu_dialogue");
			val.PatchAll(typeof(PauseMenuDialoguePatcher));
			Logger.LogInfo((object)"PauseMenuDialoguePatcher initialized");
		}
	}

	[HarmonyPatch(typeof(Locale), "GetPauseQuote")]
	[HarmonyPostfix]
	private static void GetPauseQuote_Postfix(ref string __result)
	{
		if (customQuotes.Count == 0)
		{
			return;
		}
		int num = ((Locale.currentLang != null && Locale.currentLang.pauseQuotes != null) ? Locale.currentLang.pauseQuotes.Count : 0);
		int num2 = num + customQuotes.Count;
		if (num2 > 0)
		{
			int num3 = Random.Range(0, num2);
			if (num3 >= num)
			{
				__result = customQuotes[num3 - num];
			}
		}
	}
}
