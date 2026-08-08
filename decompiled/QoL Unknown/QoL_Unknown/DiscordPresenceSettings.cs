using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace QoL_Unknown;

internal static class DiscordPresenceSettings
{
	private static class DiscordPresenceRuleParser
	{
		public static bool TryEvaluate(string script, DiscordPresenceContext context, out string selected, out string error)
		{
			selected = null;
			error = null;
			if (context == null)
			{
				error = "Presence context is unavailable.";
				return false;
			}
			if (context.RuleValues == null)
			{
				context.RuleValues = new Dictionary<string, DiscordPresenceRuleValue>(StringComparer.OrdinalIgnoreCase);
			}
			string value = null;
			string[] array = script.Replace("\r\n", "\n").Replace('\r', '\n').Split(new char[1] { '\n' });
			for (int i = 0; i < array.Length; i++)
			{
				string text = array[i].Trim();
				if (text.Length == 0)
				{
					continue;
				}
				int nextIndex;
				if (text.StartsWith("if", StringComparison.OrdinalIgnoreCase))
				{
					if (!TryParseRuleLine(text, out var condition, out var message, out error))
					{
						error = $"Line {i + 1}: {error}";
						return false;
					}
					if (!TryEvaluateCondition(condition, context.RuleValues, out var matched, out error))
					{
						error = $"Line {i + 1}: {error}";
						return false;
					}
					if (matched)
					{
						selected = message;
						return true;
					}
				}
				else if (!TryParseQuotedString(text, 0, out value, out nextIndex, out error) || text.Substring(nextIndex).Trim().Length != 0)
				{
					error = $"Line {i + 1}: expected if(condition) {{ \"message\" }} or \"fallback\".";
					return false;
				}
			}
			selected = value ?? context.DefaultState ?? context.Status ?? string.Empty;
			return true;
		}

		private static bool TryParseRuleLine(string line, out string condition, out string message, out string error)
		{
			condition = null;
			message = null;
			error = null;
			int index = 0;
			SkipWhitespace(line, ref index);
			if (!ConsumeWord(line, ref index, "if"))
			{
				error = "expected if.";
				return false;
			}
			SkipWhitespace(line, ref index);
			if (index >= line.Length || line[index] != '(')
			{
				error = "expected condition opening parenthesis.";
				return false;
			}
			int num = ++index;
			int num2 = 1;
			while (index < line.Length && num2 > 0)
			{
				if (line[index] == '(')
				{
					num2++;
				}
				else if (line[index] == ')')
				{
					num2--;
				}
				if (num2 > 0)
				{
					index++;
				}
			}
			if (num2 != 0)
			{
				error = "missing condition closing parenthesis.";
				return false;
			}
			condition = line.Substring(num, index - num).Trim();
			index++;
			SkipWhitespace(line, ref index);
			if (index >= line.Length || line[index] != '{')
			{
				error = "expected message opening brace.";
				return false;
			}
			index++;
			SkipWhitespace(line, ref index);
			if (!TryParseQuotedString(line, index, out message, out index, out error))
			{
				return false;
			}
			SkipWhitespace(line, ref index);
			if (index >= line.Length || line[index] != '}')
			{
				error = "expected message closing brace.";
				return false;
			}
			index++;
			if (line.Substring(index).Trim().Length != 0)
			{
				error = "unexpected text after rule.";
				return false;
			}
			return true;
		}

		private static bool TryEvaluateCondition(string condition, Dictionary<string, DiscordPresenceRuleValue> values, out bool matched, out string error)
		{
			matched = false;
			error = null;
			if (string.IsNullOrWhiteSpace(condition))
			{
				error = "condition is empty.";
				return false;
			}
			string text = condition.Trim();
			bool flag = false;
			if (text.StartsWith("!", StringComparison.Ordinal))
			{
				flag = true;
				text = text.Substring(1).Trim();
			}
			string[] array = new string[6] { "<=", ">=", "==", "!=", "<", ">" };
			foreach (string text2 in array)
			{
				int num = text.IndexOf(text2, StringComparison.Ordinal);
				if (num < 0)
				{
					continue;
				}
				string variableName = text.Substring(0, num).Trim();
				string text3 = text.Substring(num + text2.Length).Trim();
				if (!TryGetValue(values, variableName, out var value, out error))
				{
					return false;
				}
				if (!TryParseLiteral(text3, out var value2))
				{
					error = "could not parse literal '" + text3 + "'.";
					return false;
				}
				if (value.IsBoolean || value2.IsBoolean)
				{
					if (text2 != "==" && text2 != "!=")
					{
						error = "boolean values only support == and !=.";
						return false;
					}
					matched = value.Boolean == value2.Boolean;
					if (text2 == "!=")
					{
						matched = !matched;
					}
				}
				else
				{
					matched = Compare(value.Number, value2.Number, text2);
				}
				return true;
			}
			if (!TryGetValue(values, text, out var value3, out error))
			{
				return false;
			}
			matched = value3.Boolean;
			if (flag)
			{
				matched = !matched;
			}
			return true;
		}

		private static bool Compare(double left, double right, string op)
		{
			return op switch
			{
				"<" => left < right, 
				"<=" => left <= right, 
				">" => left > right, 
				">=" => left >= right, 
				"==" => Math.Abs(left - right) < 0.0001, 
				"!=" => Math.Abs(left - right) >= 0.0001, 
				_ => false, 
			};
		}

		private static bool TryGetValue(Dictionary<string, DiscordPresenceRuleValue> values, string variableName, out DiscordPresenceRuleValue value, out string error)
		{
			error = null;
			value = default(DiscordPresenceRuleValue);
			if (string.IsNullOrWhiteSpace(variableName))
			{
				error = "variable name is empty.";
				return false;
			}
			if (values.TryGetValue(variableName, out value))
			{
				return true;
			}
			error = "unknown variable '" + variableName + "'.";
			return false;
		}

		private static bool TryParseLiteral(string literalText, out DiscordPresenceRuleValue value)
		{
			value = default(DiscordPresenceRuleValue);
			if (string.Equals(literalText, "true", StringComparison.OrdinalIgnoreCase))
			{
				value = DiscordPresenceRuleValue.FromBoolean(value: true);
				return true;
			}
			if (string.Equals(literalText, "false", StringComparison.OrdinalIgnoreCase))
			{
				value = DiscordPresenceRuleValue.FromBoolean(value: false);
				return true;
			}
			if (double.TryParse(literalText, NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
			{
				value = DiscordPresenceRuleValue.FromNumber(result);
				return true;
			}
			return false;
		}

		private static bool TryParseQuotedString(string line, int startIndex, out string value, out int nextIndex, out string error)
		{
			value = null;
			nextIndex = startIndex;
			error = null;
			if (startIndex >= line.Length || line[startIndex] != '"')
			{
				error = "expected quoted message.";
				return false;
			}
			StringBuilder stringBuilder = new StringBuilder();
			int num = startIndex + 1;
			while (num < line.Length)
			{
				char c = line[num];
				switch (c)
				{
				case '"':
					value = stringBuilder.ToString();
					nextIndex = num + 1;
					return true;
				case '\\':
					if (num + 1 < line.Length)
					{
						char c2 = line[num + 1];
						switch (c2)
						{
						case '"':
						case '\\':
							stringBuilder.Append(c2);
							break;
						case 'n':
							stringBuilder.Append('\n');
							break;
						default:
							stringBuilder.Append(c2);
							break;
						}
						num += 2;
						continue;
					}
					break;
				}
				stringBuilder.Append(c);
				num++;
			}
			error = "missing closing quote.";
			return false;
		}

		private static bool ConsumeWord(string line, ref int index, string word)
		{
			if (index + word.Length > line.Length || !string.Equals(line.Substring(index, word.Length), word, StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}
			index += word.Length;
			return true;
		}

		private static void SkipWhitespace(string line, ref int index)
		{
			while (index < line.Length && char.IsWhiteSpace(line[index]))
			{
				index++;
			}
		}
	}

	public const int MaxFieldLength = 120;

	public const string DetailsPrefKey = "DiscordPresenceTemplate_Details";

	public const string StatePrefKey = "DiscordPresenceTemplate_State";

	public const string LargeImageTextPrefKey = "DiscordPresenceTemplate_LargeImageText";

	public const string SmallImageTextPrefKey = "DiscordPresenceTemplate_SmallImageText";

	private const string DefaultStateRules = "if(!Alive) { \"{death}\" }\nif(BrainHealth < 20) { \"Comatose! {BrainHealth:0}% brain health.\" }\nif(InCardiacArrest) { \"Cardiac arrest. No pulse detected.\" }\nif(FibrillationProgress > 80) { \"Fibrillation crisis! {FibrillationProgress:0}% severity.\" }\nif(FibrillationWorsening == true) { \"Fibrillation worsening... ({FibrillationProgress:0}% severity).\" }\nif(StrokeRisk == true) { \"Experiencing a stroke. ({StrokeAmount:0}% severity).\" }\nif(Oxygen <= 5) { \"Cardiac Arrest!! {BrainHealth:0}% brain health.\" }\nif(BloodPressure < 60) { \"Profound hypotension! {BloodPressure:0} mmHg.\" }\nif(BloodPressure < 75) { \"Hypotensive! {BloodPressure:0} mmHg.\" }\nif(BloodPressure > 180) { \"Hypertensive crisis! {BloodPressure:0} mmHg.\" }\nif(BloodPressure > 160) { \"Severe hypertension! {BloodPressure:0} mmHg.\" }\nif(BleedingOut == true) { \"Bleeding Out!! {BleedRatePerMinute:0.00}L/m bleed rate.\" }\nif(Drowning == true) { \"Drowning!! {Oxygen:0}% oxygen remaining.\" }\nif(ThoraxMuscleHealth < 4) { \"Lung failure! {ThoraxMuscleHealth:0} thorax health.\" }\nif(HorrifiedLevel > 0) { \"RUNNING FROM AN ELDER THORNBACK!!\" }\nif(Unchipped) { \"Chipless. Status unknown.\" }\nif(BloodVolume < 30) { \"Hypovolemic! {BloodVolume:0.00}L blood amount.\" }\nif(SepticShock > 52.5) { \"In septic shock! {SepticShock:0}% severity.\" }\nif(RadiationSickness > 70) { \"Irradiated! {RadiationSickness:0}% severity.\" }\nif(HypothermicUnconscious == true) { \"Hypothermic! {Temperature:0.0}C body temp.\" }\nif(Temperature > 41.5) { \"Heatstroke! {Temperature:0.0}C body temp.\" }\nif(StarvingWithLungDamage == true) { \"Starving! {Hunger:0}% fullness.\" }\nif(Dessicated == true) { \"Dessicated! {Thirst:0}% thirst.\" }\nif(AveragePain > 96) { \"In shock from {AveragePain:0}% pain!\" }\nif(MindWipeActive) { \" .. Hollow .. \" }\nif(HasBrokenLimb) { \"Healing a {BrokenLimbName} fracture...\" }\nif(TotalBleedSpeed > 0) { \"Treating {BleedRatePerMinute:0.00}L/m of bleeding...\" }\nif(HasShrapnel) { \"Removing shrapnel from the {ShrapnelLimbName}...\" }\nif(HasDislocation) { \"Tending a {DislocatedLimbName} dislocation...\" }\nif(BrainHealth < 95) { \"{BrainDamageDescriptor} brain damage! ({BrainHealth:0}% brain health)\" }\nif(Energy < 10) { \"Taking a nap... {Energy:0}% energy..\" }\nif(AveragePain > 50) { \"In {AveragePain:0}% pain. Ouch!\" }\nif(SicknessAmount > 35) { \"Feeling {SicknessDescriptor} sick. ({SicknessAmount:0}% sickness)\" }\nif(MaxLimbInfection > 30) { \"{InfectionLimbSubject} {InfectionVerb} {InfectionDescriptor} infected! ({MaxLimbInfection:0}% infection)\" }\nif(Stamina < 40) { \"{Stamina:0}% exhausted... phew..\" }\nif(TotalHappiness < -5) { \"Feeling {SadnessDescriptor}... ({TotalHappiness:0.0} happiness)\" }\nif(Hunger < 66) { \"{HungerDescriptor}.. ({Hunger:0} hunger)\" }\nif(Thirst < 66) { \"{ThirstDescriptor}.. ({Thirst:0} thirst)\" }\n\"Feeling {HappinessDescriptor}{FullnessDescriptor} ({Happiness:0.0} happiness)\"";

	private static readonly string[] TokenNames = new string[12]
	{
		"{depth}", "{layer}", "{status}", "{lastSpeech}", "{health}", "{hunger}", "{thirst}", "{happiness}", "{mode}", "{death}",
		"{alive}", "{tutorialCourse}"
	};

	public static string TokensDisplay => string.Join("  ", TokenNames);

	public static string GetTemplate(DiscordPresenceField field)
	{
		string prefKey = GetPrefKey(field);
		return PlayerPrefs.GetString(prefKey, GetDefaultTemplate(field));
	}

	public static void SetTemplate(DiscordPresenceField field, string template)
	{
		if (string.Equals(template ?? string.Empty, GetDefaultTemplate(field), StringComparison.Ordinal))
		{
			ResetTemplate(field);
		}
		else
		{
			PlayerPrefs.SetString(GetPrefKey(field), template ?? string.Empty);
		}
	}

	public static void Save()
	{
		PlayerPrefs.Save();
	}

	public static void ResetTemplate(DiscordPresenceField field)
	{
		PlayerPrefs.DeleteKey(GetPrefKey(field));
	}

	public static void ResetAll()
	{
		ResetTemplate(DiscordPresenceField.Details);
		ResetTemplate(DiscordPresenceField.State);
		ResetTemplate(DiscordPresenceField.LargeImageText);
		ResetTemplate(DiscordPresenceField.SmallImageText);
		PlayerPrefs.Save();
	}

	public static bool HasCustomTemplate(DiscordPresenceField field)
	{
		return PlayerPrefs.HasKey(GetPrefKey(field));
	}

	public static DiscordPresencePreview Render(DiscordPresenceContext context)
	{
		if (context == null)
		{
			context = new DiscordPresenceContext
			{
				DefaultDetails = "In the Main Menu",
				DefaultState = "Preparing to descend...",
				DefaultLargeImageText = string.Empty,
				DefaultSmallImageText = string.Empty,
				LastSpeech = "...",
				LayerName = "Depth: Unknown..",
				Mode = "Main Menu",
				Death = string.Empty,
				TutorialCourse = string.Empty,
				Status = "Preparing to descend..."
			};
		}
		return new DiscordPresencePreview
		{
			Details = RenderField(DiscordPresenceField.Details, context, context.DefaultDetails),
			State = RenderField(DiscordPresenceField.State, context, context.DefaultState),
			LargeImageText = RenderField(DiscordPresenceField.LargeImageText, context, context.DefaultLargeImageText),
			SmallImageText = RenderField(DiscordPresenceField.SmallImageText, context, context.DefaultSmallImageText)
		};
	}

	public static string RenderTemplate(string template, DiscordPresenceContext context, string fallback)
	{
		string value = ExpandTokens(template ?? string.Empty, context);
		value = ClampField(value);
		return string.IsNullOrWhiteSpace(value) ? ClampField(fallback) : value;
	}

	public static string RenderStateTemplate(string template, DiscordPresenceContext context, string fallback, out string ruleError)
	{
		ruleError = null;
		string text = template ?? string.Empty;
		if (IsStateRuleScript(text))
		{
			if (DiscordPresenceRuleParser.TryEvaluate(text, context, out var selected, out ruleError))
			{
				return RenderTemplate(ExpandRuleValues(selected, context), context, fallback);
			}
			return ClampField(fallback);
		}
		return RenderTemplate(text, context, fallback);
	}

	private static string RenderField(DiscordPresenceField field, DiscordPresenceContext context, string fallback)
	{
		if (!HasCustomTemplate(field))
		{
			return ClampField(fallback);
		}
		string ruleError;
		if (field == DiscordPresenceField.State)
		{
			return RenderStateTemplate(GetTemplate(field), context, fallback, out ruleError);
		}
		return RenderTemplate(GetTemplate(field), context, fallback);
	}

	private static bool IsStateRuleScript(string template)
	{
		if (string.IsNullOrWhiteSpace(template))
		{
			return false;
		}
		string[] array = template.Replace("\r\n", "\n").Replace('\r', '\n').Split(new char[1] { '\n' });
		bool flag = false;
		for (int i = 0; i < array.Length; i++)
		{
			string text = array[i].Trim();
			if (text.Length == 0)
			{
				continue;
			}
			if (text.StartsWith("if", StringComparison.OrdinalIgnoreCase))
			{
				flag = true;
				continue;
			}
			if (flag && text.StartsWith("\"", StringComparison.Ordinal))
			{
				return true;
			}
			if (flag)
			{
				return true;
			}
		}
		return flag;
	}

	private static string ExpandTokens(string template, DiscordPresenceContext context)
	{
		if (string.IsNullOrEmpty(template))
		{
			return string.Empty;
		}
		Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			["{depth}"] = context.Depth.ToString(),
			["{layer}"] = context.LayerName ?? "Depth: Unknown..",
			["{status}"] = context.Status ?? string.Empty,
			["{lastSpeech}"] = (string.IsNullOrWhiteSpace(context.LastSpeech) ? "..." : context.LastSpeech),
			["{health}"] = FormatNumber(context.Health),
			["{hunger}"] = FormatNumber(context.Hunger),
			["{thirst}"] = FormatNumber(context.Thirst),
			["{happiness}"] = FormatNumber(context.Happiness),
			["{mode}"] = context.Mode ?? string.Empty,
			["{death}"] = context.Death ?? string.Empty,
			["{alive}"] = (context.Alive ? "Alive" : "Dead"),
			["{tutorialCourse}"] = context.TutorialCourse ?? string.Empty
		};
		string text = template;
		foreach (KeyValuePair<string, string> item in dictionary)
		{
			text = text.Replace(item.Key, item.Value);
		}
		return text;
	}

	private static string ClampField(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return string.Empty;
		}
		string text = value.Trim();
		if (text.Length <= 120)
		{
			return text;
		}
		return text.Substring(0, 117) + "...";
	}

	private static string FormatNumber(float value)
	{
		return Mathf.RoundToInt(value).ToString();
	}

	private static string GetPrefKey(DiscordPresenceField field)
	{
		return field switch
		{
			DiscordPresenceField.Details => "DiscordPresenceTemplate_Details", 
			DiscordPresenceField.State => "DiscordPresenceTemplate_State", 
			DiscordPresenceField.LargeImageText => "DiscordPresenceTemplate_LargeImageText", 
			DiscordPresenceField.SmallImageText => "DiscordPresenceTemplate_SmallImageText", 
			_ => throw new ArgumentOutOfRangeException("field", field, null), 
		};
	}

	public static string GetDefaultTemplate(DiscordPresenceField field)
	{
		return field switch
		{
			DiscordPresenceField.Details => "{mode}", 
			DiscordPresenceField.State => "if(!Alive) { \"{death}\" }\nif(BrainHealth < 20) { \"Comatose! {BrainHealth:0}% brain health.\" }\nif(InCardiacArrest) { \"Cardiac arrest. No pulse detected.\" }\nif(FibrillationProgress > 80) { \"Fibrillation crisis! {FibrillationProgress:0}% severity.\" }\nif(FibrillationWorsening == true) { \"Fibrillation worsening... ({FibrillationProgress:0}% severity).\" }\nif(StrokeRisk == true) { \"Experiencing a stroke. ({StrokeAmount:0}% severity).\" }\nif(Oxygen <= 5) { \"Cardiac Arrest!! {BrainHealth:0}% brain health.\" }\nif(BloodPressure < 60) { \"Profound hypotension! {BloodPressure:0} mmHg.\" }\nif(BloodPressure < 75) { \"Hypotensive! {BloodPressure:0} mmHg.\" }\nif(BloodPressure > 180) { \"Hypertensive crisis! {BloodPressure:0} mmHg.\" }\nif(BloodPressure > 160) { \"Severe hypertension! {BloodPressure:0} mmHg.\" }\nif(BleedingOut == true) { \"Bleeding Out!! {BleedRatePerMinute:0.00}L/m bleed rate.\" }\nif(Drowning == true) { \"Drowning!! {Oxygen:0}% oxygen remaining.\" }\nif(ThoraxMuscleHealth < 4) { \"Lung failure! {ThoraxMuscleHealth:0} thorax health.\" }\nif(HorrifiedLevel > 0) { \"RUNNING FROM AN ELDER THORNBACK!!\" }\nif(Unchipped) { \"Chipless. Status unknown.\" }\nif(BloodVolume < 30) { \"Hypovolemic! {BloodVolume:0.00}L blood amount.\" }\nif(SepticShock > 52.5) { \"In septic shock! {SepticShock:0}% severity.\" }\nif(RadiationSickness > 70) { \"Irradiated! {RadiationSickness:0}% severity.\" }\nif(HypothermicUnconscious == true) { \"Hypothermic! {Temperature:0.0}C body temp.\" }\nif(Temperature > 41.5) { \"Heatstroke! {Temperature:0.0}C body temp.\" }\nif(StarvingWithLungDamage == true) { \"Starving! {Hunger:0}% fullness.\" }\nif(Dessicated == true) { \"Dessicated! {Thirst:0}% thirst.\" }\nif(AveragePain > 96) { \"In shock from {AveragePain:0}% pain!\" }\nif(MindWipeActive) { \" .. Hollow .. \" }\nif(HasBrokenLimb) { \"Healing a {BrokenLimbName} fracture...\" }\nif(TotalBleedSpeed > 0) { \"Treating {BleedRatePerMinute:0.00}L/m of bleeding...\" }\nif(HasShrapnel) { \"Removing shrapnel from the {ShrapnelLimbName}...\" }\nif(HasDislocation) { \"Tending a {DislocatedLimbName} dislocation...\" }\nif(BrainHealth < 95) { \"{BrainDamageDescriptor} brain damage! ({BrainHealth:0}% brain health)\" }\nif(Energy < 10) { \"Taking a nap... {Energy:0}% energy..\" }\nif(AveragePain > 50) { \"In {AveragePain:0}% pain. Ouch!\" }\nif(SicknessAmount > 35) { \"Feeling {SicknessDescriptor} sick. ({SicknessAmount:0}% sickness)\" }\nif(MaxLimbInfection > 30) { \"{InfectionLimbSubject} {InfectionVerb} {InfectionDescriptor} infected! ({MaxLimbInfection:0}% infection)\" }\nif(Stamina < 40) { \"{Stamina:0}% exhausted... phew..\" }\nif(TotalHappiness < -5) { \"Feeling {SadnessDescriptor}... ({TotalHappiness:0.0} happiness)\" }\nif(Hunger < 66) { \"{HungerDescriptor}.. ({Hunger:0} hunger)\" }\nif(Thirst < 66) { \"{ThirstDescriptor}.. ({Thirst:0} thirst)\" }\n\"Feeling {HappinessDescriptor}{FullnessDescriptor} ({Happiness:0.0} happiness)\"", 
			DiscordPresenceField.LargeImageText => "\"{lastSpeech}\"", 
			DiscordPresenceField.SmallImageText => "{layer}", 
			_ => throw new ArgumentOutOfRangeException("field", field, null), 
		};
	}

	private static string ExpandRuleValues(string template, DiscordPresenceContext context)
	{
		if (string.IsNullOrEmpty(template) || context == null || context.RuleValues == null)
		{
			return template ?? string.Empty;
		}
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 0; i < template.Length; i++)
		{
			char c = template[i];
			if (c != '{')
			{
				stringBuilder.Append(c);
				continue;
			}
			int num = template.IndexOf('}', i + 1);
			if (num < 0)
			{
				stringBuilder.Append(c);
				continue;
			}
			string text = template.Substring(i + 1, num - i - 1);
			string key = text;
			string format = null;
			int num2 = text.IndexOf(':');
			if (num2 >= 0)
			{
				key = text.Substring(0, num2);
				format = text.Substring(num2 + 1);
			}
			if (context.RuleValues.TryGetValue(key, out var value))
			{
				stringBuilder.Append(FormatRuleValue(value, format));
				i = num;
			}
			else
			{
				stringBuilder.Append(template, i, num - i + 1);
				i = num;
			}
		}
		return stringBuilder.ToString();
	}

	private static string FormatRuleValue(DiscordPresenceRuleValue value, string format)
	{
		if (value.IsBoolean)
		{
			return value.Boolean ? "true" : "false";
		}
		if (value.IsString)
		{
			return value.String ?? string.Empty;
		}
		if (!string.IsNullOrEmpty(format))
		{
			return value.Number.ToString(format, CultureInfo.InvariantCulture);
		}
		return value.Number.ToString("0.##", CultureInfo.InvariantCulture);
	}
}
