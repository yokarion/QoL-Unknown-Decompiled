using System;
using System.IO;
using System.Text;
using BepInEx.Logging;

namespace QoL_Unknown;

internal static class SaveFileStore
{
	internal const string DefaultSaveFileName = "save.sv";

	internal static bool TryGetSavePath(string saveFolder, string fileName, out string path)
	{
		path = null;
		if (string.IsNullOrWhiteSpace(saveFolder) || string.IsNullOrWhiteSpace(fileName))
		{
			return false;
		}
		string fileName2 = Path.GetFileName(fileName);
		if (!string.Equals(fileName2, fileName, StringComparison.Ordinal) || !fileName2.EndsWith(".sv", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		path = Path.Combine(saveFolder, fileName2);
		return true;
	}

	internal static bool TryCopyAtomic(string sourcePath, string destinationPath, ManualLogSource logger, string operation)
	{
		if (string.IsNullOrWhiteSpace(sourcePath) || string.IsNullOrWhiteSpace(destinationPath) || !File.Exists(sourcePath))
		{
			if (logger != null)
			{
				logger.LogWarning((object)("Cannot " + operation + ": source file is missing."));
			}
			return false;
		}
		string text = CreateTemporaryPath(destinationPath);
		try
		{
			EnsureParentDirectory(destinationPath);
			File.Copy(sourcePath, text, overwrite: true);
			CommitTemporaryFile(text, destinationPath);
			return true;
		}
		catch (Exception arg)
		{
			if (logger != null)
			{
				logger.LogError((object)$"Failed to {operation}: {arg}");
			}
			TryDelete(text, logger, "clean up temporary file for " + operation);
			return false;
		}
	}

	internal static bool TryWriteAllTextAtomic(string destinationPath, string contents, ManualLogSource logger, string operation)
	{
		if (string.IsNullOrWhiteSpace(destinationPath))
		{
			if (logger != null)
			{
				logger.LogWarning((object)("Cannot " + operation + ": destination path is empty."));
			}
			return false;
		}
		string text = CreateTemporaryPath(destinationPath);
		try
		{
			EnsureParentDirectory(destinationPath);
			File.WriteAllText(text, contents ?? string.Empty, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
			CommitTemporaryFile(text, destinationPath);
			return true;
		}
		catch (Exception arg)
		{
			if (logger != null)
			{
				logger.LogError((object)$"Failed to {operation}: {arg}");
			}
			TryDelete(text, logger, "clean up temporary file for " + operation);
			return false;
		}
	}

	internal static bool TryDelete(string path, ManualLogSource logger, string operation)
	{
		if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
		{
			return true;
		}
		try
		{
			File.Delete(path);
			return true;
		}
		catch (Exception ex)
		{
			if (logger != null)
			{
				logger.LogWarning((object)("Failed to " + operation + ": " + ex.Message));
			}
			return false;
		}
	}

	internal static bool TryReplaceDirectory(string sourceDirectory, string destinationDirectory, ManualLogSource logger, string operation)
	{
		if (string.IsNullOrWhiteSpace(sourceDirectory) || string.IsNullOrWhiteSpace(destinationDirectory) || !Directory.Exists(sourceDirectory))
		{
			if (logger != null)
			{
				logger.LogWarning((object)("Cannot " + operation + ": source directory is missing."));
			}
			return false;
		}
		string text = destinationDirectory + ".qol-tmp-" + Guid.NewGuid().ToString("N");
		string text2 = destinationDirectory + ".qol-backup-" + Guid.NewGuid().ToString("N");
		bool flag = false;
		try
		{
			CopyDirectory(sourceDirectory, text);
			if (Directory.Exists(destinationDirectory))
			{
				Directory.Move(destinationDirectory, text2);
				flag = true;
			}
			Directory.Move(text, destinationDirectory);
			if (flag)
			{
				TryDeleteDirectory(text2, logger, "remove old directory after " + operation);
			}
			return true;
		}
		catch (Exception arg)
		{
			if (logger != null)
			{
				logger.LogError((object)$"Failed to {operation}: {arg}");
			}
			TryDeleteDirectory(text, logger, "clean up temporary directory for " + operation);
			if (flag && !Directory.Exists(destinationDirectory) && Directory.Exists(text2))
			{
				try
				{
					Directory.Move(text2, destinationDirectory);
				}
				catch (Exception arg2)
				{
					if (logger != null)
					{
						logger.LogError((object)$"Failed to roll back {operation}. Recovery directory remains at '{text2}': {arg2}");
					}
				}
			}
			return false;
		}
	}

	internal static bool TryDeleteDirectory(string path, ManualLogSource logger, string operation)
	{
		if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
		{
			return true;
		}
		try
		{
			Directory.Delete(path, recursive: true);
			return true;
		}
		catch (Exception ex)
		{
			if (logger != null)
			{
				logger.LogWarning((object)("Failed to " + operation + ": " + ex.Message));
			}
			return false;
		}
	}

	private static string CreateTemporaryPath(string destinationPath)
	{
		return destinationPath + ".qol-tmp-" + Guid.NewGuid().ToString("N");
	}

	private static void EnsureParentDirectory(string path)
	{
		string directoryName = Path.GetDirectoryName(path);
		if (!string.IsNullOrEmpty(directoryName))
		{
			Directory.CreateDirectory(directoryName);
		}
	}

	private static void CommitTemporaryFile(string temporaryPath, string destinationPath)
	{
		if (File.Exists(destinationPath))
		{
			File.Replace(temporaryPath, destinationPath, null);
		}
		else
		{
			File.Move(temporaryPath, destinationPath);
		}
	}

	private static void CopyDirectory(string sourceDirectory, string destinationDirectory)
	{
		Directory.CreateDirectory(destinationDirectory);
		string[] directories = Directory.GetDirectories(sourceDirectory, "*", SearchOption.AllDirectories);
		foreach (string text in directories)
		{
			string path = text.Substring(sourceDirectory.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
			Directory.CreateDirectory(Path.Combine(destinationDirectory, path));
		}
		string[] files = Directory.GetFiles(sourceDirectory, "*", SearchOption.AllDirectories);
		foreach (string text2 in files)
		{
			string path2 = text2.Substring(sourceDirectory.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
			string text3 = Path.Combine(destinationDirectory, path2);
			EnsureParentDirectory(text3);
			File.Copy(text2, text3, overwrite: true);
		}
	}
}
