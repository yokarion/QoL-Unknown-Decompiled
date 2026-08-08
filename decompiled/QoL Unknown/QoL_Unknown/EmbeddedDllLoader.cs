using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace QoL_Unknown;

public static class EmbeddedDllLoader
{
	private const string DLL_RESOURCE_NAME = "DiscordRPC.dll";

	public static void Init()
	{
		AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
	}

	private static Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
	{
		string name = new AssemblyName(args.Name).Name;
		if (name.Equals(Path.GetFileNameWithoutExtension("DiscordRPC.dll"), StringComparison.InvariantCultureIgnoreCase))
		{
			Assembly executingAssembly = Assembly.GetExecutingAssembly();
			string text = executingAssembly.GetManifestResourceNames().FirstOrDefault((string n) => n.EndsWith("DiscordRPC.dll"));
			if (string.IsNullOrEmpty(text))
			{
				return null;
			}
			using Stream stream = executingAssembly.GetManifestResourceStream(text);
			if (stream == null)
			{
				return null;
			}
			byte[] array = new byte[stream.Length];
			stream.Read(array, 0, array.Length);
			return Assembly.Load(array);
		}
		return null;
	}
}
