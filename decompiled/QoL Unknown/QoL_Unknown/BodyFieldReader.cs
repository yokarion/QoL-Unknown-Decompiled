using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace QoL_Unknown;

internal static class BodyFieldReader
{
	private class MemberTable
	{
		public readonly Dictionary<string, MemberInfo> Exact = new Dictionary<string, MemberInfo>(StringComparer.Ordinal);

		public readonly Dictionary<string, MemberInfo> Loose = new Dictionary<string, MemberInfo>(StringComparer.OrdinalIgnoreCase);

		public readonly List<string> SuggestedNames = new List<string>();

		public void Add(MemberInfo member, bool suggest)
		{
			if (!Exact.ContainsKey(member.Name))
			{
				Exact.Add(member.Name, member);
			}
			if (!Loose.ContainsKey(member.Name))
			{
				Loose.Add(member.Name, member);
			}
			if (suggest && !SuggestedNames.Contains(member.Name))
			{
				SuggestedNames.Add(member.Name);
			}
		}

		public MemberInfo Find(string name)
		{
			if (string.IsNullOrEmpty(name))
			{
				return null;
			}
			if (!Exact.TryGetValue(name, out var value))
			{
				if (!Loose.TryGetValue(name, out var value2))
				{
					return null;
				}
				return value2;
			}
			return value;
		}
	}

	private const BindingFlags PublicInstance = BindingFlags.Instance | BindingFlags.Public;

	private const BindingFlags AllInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

	private const int MaxPreviewItems = 16;

	private static readonly Dictionary<Type, MemberTable> tables = new Dictionary<Type, MemberTable>();

	internal static void GetBodyField(string[] args)
	{
		Body body;
		string targetLabel;
		string error;
		if (args == null || args.Length <= 1)
		{
			Log("<color=orange>Usage: getbodyfield &lt;field&gt; [player]</color>");
		}
		else if (!TryResolveTargetBody(JoinTail(args, 2), out body, out targetLabel, out error))
		{
			Log(error);
		}
		else
		{
			PrintMember(body, typeof(Body), args[1], targetLabel + "body");
		}
	}

	internal static void GetLimbField(string[] args)
	{
		Body body;
		string targetLabel;
		string error;
		Limb limb;
		if (args == null || args.Length <= 2)
		{
			Log("<color=orange>Usage: getlimbfield &lt;limb&gt; &lt;field&gt; [player]</color>");
		}
		else if (!TryResolveTargetBody(JoinTail(args, 3), out body, out targetLabel, out error))
		{
			Log(error);
		}
		else if (!TryResolveLimb(body, args[1], out limb, out error))
		{
			Log(error);
		}
		else
		{
			PrintMember(limb, typeof(Limb), args[2], targetLabel + ((Object)limb).name);
		}
	}

	internal static Dictionary<int, List<string>> BodyAutofill()
	{
		return new Dictionary<int, List<string>>
		{
			{
				0,
				SuggestedNames(typeof(Body))
			},
			{
				1,
				new List<string>()
			}
		};
	}

	internal static Dictionary<int, List<string>> LimbAutofill()
	{
		return new Dictionary<int, List<string>>
		{
			{
				0,
				new List<string>()
			},
			{
				1,
				SuggestedNames(typeof(Limb))
			},
			{
				2,
				new List<string>()
			}
		};
	}

	internal static void RefreshAutofill(string[] args)
	{
		if (args == null || args.Length == 0)
		{
			return;
		}
		bool flag = args[0].Equals("getlimbfield", StringComparison.OrdinalIgnoreCase);
		if (!flag && !args[0].Equals("getbodyfield", StringComparison.OrdinalIgnoreCase))
		{
			return;
		}
		Command val = ConsoleScript.SearchExact(args[0]);
		if (val == null || val.argAutofill == null)
		{
			return;
		}
		val.argAutofill[(!flag) ? 1 : 2] = (MultiplayerCompat.IsNetworkRunning ? MultiplayerCompat.GetConnectedPlayerNames() : new List<string>());
		if (flag)
		{
			Body val2 = (((Object)(object)PlayerCamera.main != (Object)null) ? PlayerCamera.main.body : null);
			if ((Object)(object)val2 != (Object)null && val2.limbs != null)
			{
				val.argAutofill[0] = LimbNames(val2);
			}
		}
	}

	private static void PrintMember(object target, Type owner, string name, string label)
	{
		MemberInfo memberInfo = Table(owner).Find(name);
		if (memberInfo == null)
		{
			Log("<color=orange>Nothing on " + owner.Name + " named \"" + Safe(name) + "\".</color>" + SuggestHint(owner, name));
			return;
		}
		string text;
		try
		{
			text = Format((memberInfo is FieldInfo fieldInfo) ? fieldInfo.GetValue(target) : ((PropertyInfo)memberInfo).GetValue(target, null));
		}
		catch (Exception ex)
		{
			Log("<color=orange>Could not read " + Safe(label) + "." + Safe(name) + ": " + Safe((ex.InnerException != null) ? ex.InnerException.Message : ex.Message) + "</color>");
			return;
		}
		Type type = ((memberInfo is FieldInfo fieldInfo2) ? fieldInfo2.FieldType : ((PropertyInfo)memberInfo).PropertyType);
		Log("<color=green>" + Safe(label) + "." + Safe(memberInfo.Name) + "</color> = <color=yellow>" + text + "</color> <color=grey>(" + type.Name + ")</color>");
	}

	private static string SuggestHint(Type owner, string name)
	{
		List<string> list = (from candidate in SuggestedNames(owner)
			where candidate.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0
			select candidate).Take(6).ToList();
		if (list.Count != 0)
		{
			return " <color=grey>Try: " + string.Join(", ", list) + "</color>";
		}
		return "";
	}

	private static bool TryResolveTargetBody(string playerArg, out Body body, out string targetLabel, out string error)
	{
		body = null;
		targetLabel = string.Empty;
		error = null;
		if (string.IsNullOrEmpty(playerArg))
		{
			body = (((Object)(object)PlayerCamera.main != (Object)null) ? PlayerCamera.main.body : null);
			if ((Object)(object)body == (Object)null)
			{
				error = "<color=orange>No player in the world right now.</color>";
				return false;
			}
			return true;
		}
		if (!MultiplayerCompat.IsNetworkRunning)
		{
			error = "<color=orange>Only the local player can be read outside multiplayer. Drop the last argument.</color>";
			return false;
		}
		List<KeyValuePair<string, Body>> connectedPlayerBodies = MultiplayerCompat.GetConnectedPlayerBodies();
		if (connectedPlayerBodies.Count == 0)
		{
			error = "<color=orange>No players registered yet.</color>";
			return false;
		}
		List<KeyValuePair<string, Body>> list = connectedPlayerBodies.FindAll((KeyValuePair<string, Body> player) => player.Key.Equals(playerArg, StringComparison.OrdinalIgnoreCase));
		if (list.Count == 0)
		{
			list = connectedPlayerBodies.FindAll((KeyValuePair<string, Body> player) => player.Key.IndexOf(playerArg, StringComparison.OrdinalIgnoreCase) >= 0);
		}
		if (list.Count == 0)
		{
			error = "<color=orange>No player matches \"" + Safe(playerArg) + "\".</color> <color=grey>Players: " + string.Join(", ", connectedPlayerBodies.Select((KeyValuePair<string, Body> player) => Safe(player.Key))) + "</color>";
			return false;
		}
		if (list.Count > 1)
		{
			error = string.Format("<color=orange>\"{0}\" matches {1} players: {2}.</color> <color=grey>Be more specific.</color>", Safe(playerArg), list.Count, string.Join(", ", list.Select((KeyValuePair<string, Body> player) => Safe(player.Key))));
			return false;
		}
		body = list[0].Value;
		targetLabel = Safe(list[0].Key) + "/";
		return true;
	}

	private static bool TryResolveLimb(Body body, string limbArg, out Limb limb, out string error)
	{
		error = null;
		limb = body.LimbByName(limbArg);
		if ((Object)(object)limb != (Object)null)
		{
			return true;
		}
		Limb[] limbs = body.limbs;
		if (limbs == null)
		{
			error = "<color=orange>This body has no limbs.</color>";
			return false;
		}
		foreach (Limb val in limbs)
		{
			if ((Object)(object)val != (Object)null && (string.Equals(((Object)val).name, limbArg, StringComparison.OrdinalIgnoreCase) || string.Equals(val.shortName, limbArg, StringComparison.OrdinalIgnoreCase) || string.Equals(val.fullName, limbArg, StringComparison.OrdinalIgnoreCase)))
			{
				limb = val;
				return true;
			}
		}
		if (int.TryParse(limbArg, out var result) && result >= 0 && result < limbs.Length && (Object)(object)limbs[result] != (Object)null)
		{
			limb = limbs[result];
			return true;
		}
		error = "<color=orange>\"" + Safe(limbArg) + "\" is not a valid limb!</color> <color=grey>Limbs: " + string.Join(", ", LimbNames(body)) + "</color>";
		return false;
	}

	private static List<string> LimbNames(Body body)
	{
		List<string> list = new List<string>();
		Limb[] limbs = body.limbs;
		if (limbs == null)
		{
			return list;
		}
		for (int i = 0; i < limbs.Length; i++)
		{
			if ((Object)(object)limbs[i] != (Object)null && !string.IsNullOrEmpty(((Object)limbs[i]).name))
			{
				list.Add(((Object)limbs[i]).name);
			}
		}
		return list;
	}

	private static MemberTable Table(Type owner)
	{
		if (!tables.TryGetValue(owner, out var value))
		{
			value = BuildTable(owner);
			tables[owner] = value;
		}
		return value;
	}

	private static MemberTable BuildTable(Type owner)
	{
		MemberTable memberTable = new MemberTable();
		foreach (FieldInfo item in owner.GetFields(BindingFlags.Instance | BindingFlags.Public).OrderBy<FieldInfo, string>((FieldInfo f) => f.Name, StringComparer.Ordinal))
		{
			if (!item.IsLiteral)
			{
				memberTable.Add(item, suggest: true);
			}
		}
		foreach (PropertyInfo item2 in owner.GetProperties(BindingFlags.Instance | BindingFlags.Public).OrderBy<PropertyInfo, string>((PropertyInfo p) => p.Name, StringComparer.Ordinal))
		{
			if (item2.CanRead && item2.GetGetMethod(nonPublic: false) != null && item2.GetIndexParameters().Length == 0)
			{
				memberTable.Add(item2, suggest: true);
			}
		}
		foreach (FieldInfo item3 in owner.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).OrderBy<FieldInfo, string>((FieldInfo f) => f.Name, StringComparer.Ordinal))
		{
			if (!item3.IsLiteral && !item3.IsPublic)
			{
				memberTable.Add(item3, suggest: false);
			}
		}
		return memberTable;
	}

	private static List<string> SuggestedNames(Type owner)
	{
		return new List<string>(Table(owner).SuggestedNames);
	}

	private static string Format(object value)
	{
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
		if (value == null)
		{
			return "<color=grey>null</color>";
		}
		if (value is float num)
		{
			return num.ToString("0.####", CultureInfo.InvariantCulture);
		}
		if (value is double num2)
		{
			return num2.ToString("0.####", CultureInfo.InvariantCulture);
		}
		if (value is bool)
		{
			if (!(bool)value)
			{
				return "false";
			}
			return "true";
		}
		if (value is string text)
		{
			return "\"" + Safe(text) + "\"";
		}
		if (value is Vector2 val)
		{
			return $"({val.x:0.###}, {val.y:0.###})";
		}
		if (value is Vector3 val2)
		{
			return $"({val2.x:0.###}, {val2.y:0.###}, {val2.z:0.###})";
		}
		Object val3 = (Object)((value is Object) ? value : null);
		if (val3 != null)
		{
			if (!(val3 == (Object)null))
			{
				Limb val4 = (Limb)(object)((val3 is Limb) ? val3 : null);
				if (val4 == null)
				{
					return Safe(((object)val3).ToString());
				}
				return ((Object)val4).name;
			}
			return "<color=grey>null</color>";
		}
		Type type = value.GetType();
		if (type.IsEnum)
		{
			return $"{Safe(value.ToString())} ({Convert.ChangeType(value, Enum.GetUnderlyingType(type), CultureInfo.InvariantCulture)})";
		}
		if (value is Array array)
		{
			return FormatArray(array);
		}
		if (type.IsValueType)
		{
			return FormatStruct(value, type);
		}
		return Safe(value.ToString());
	}

	private static string FormatArray(Array array)
	{
		Type elementType = array.GetType().GetElementType();
		string text = ((elementType != null) ? elementType.Name : "Object");
		if (array.Length == 0)
		{
			return text + "[] { }";
		}
		int num = Mathf.Min(array.Length, 16);
		List<string> list = new List<string>(num + 1);
		for (int i = 0; i < num; i++)
		{
			object value = array.GetValue(i);
			Object val = (Object)((value is Object) ? value : null);
			if (val != null)
			{
				object item;
				if (!(val == (Object)null))
				{
					Limb val2 = (Limb)(object)((val is Limb) ? val : null);
					item = ((val2 != null) ? ((Object)val2).name : Safe(val.name));
				}
				else
				{
					item = "null";
				}
				list.Add((string)item);
			}
			else
			{
				list.Add((value == null) ? "null" : Safe(value.ToString()));
			}
		}
		if (array.Length > num)
		{
			list.Add($"... ({array.Length - num} more)");
		}
		return string.Format("{0}[{1}] {{ {2} }}", text, array.Length, string.Join(", ", list));
	}

	private static string FormatStruct(object value, Type type)
	{
		FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		if (fields.Length == 0)
		{
			return Safe(value.ToString());
		}
		int num = Mathf.Min(fields.Length, 16);
		List<string> list = new List<string>(num + 1);
		for (int i = 0; i < num; i++)
		{
			string text;
			try
			{
				text = Format(fields[i].GetValue(value));
			}
			catch
			{
				text = "?";
			}
			list.Add(fields[i].Name + "=" + text);
		}
		if (fields.Length > num)
		{
			list.Add($"... ({fields.Length - num} more)");
		}
		return type.Name + " { " + string.Join(", ", list) + " }";
	}

	private static string JoinTail(string[] args, int startIndex)
	{
		if (args.Length > startIndex)
		{
			return string.Join(" ", args.Skip(startIndex)).Trim();
		}
		return string.Empty;
	}

	private static string Safe(string text)
	{
		if (!string.IsNullOrEmpty(text))
		{
			return text.Replace("<", string.Empty).Replace(">", string.Empty);
		}
		return string.Empty;
	}

	private static void Log(string message)
	{
		ConsoleHelper.LogToConsole(ConsoleScript.instance, message);
	}
}
