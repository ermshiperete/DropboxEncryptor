using System;
using System.Diagnostics;
using System.IO;

namespace DropboxEncryptor
{
	public static class Backup
	{
		private static string RunCommand(string cmd, string args = null)
		{
			var proc = new Process
			{
				EnableRaisingEvents = false,
				StartInfo = {
					FileName = cmd,
					Arguments = args,
					UseShellExecute = false,
					RedirectStandardOutput = true,
					WorkingDirectory = Configuration.Instance.BackupDir
				}
			};
			Debug.WriteLine($"Run command {cmd} {args}");
			proc.Start();
			proc.WaitForExit();
			var output = proc.StandardOutput.ReadToEnd();
			return output.Trim();
		}

		private static void Initialize()
		{
			if (!Directory.Exists(Configuration.Instance.BackupDir))
				Directory.CreateDirectory(Configuration.Instance.BackupDir);

			if (!Directory.Exists(Path.Combine(Configuration.Instance.BackupDir, ".git")))
			{
				RunCommand("git", "init .");
			}
		}

		public static void CreateBackup(FileChangedDataObject dataObject)
		{
			if (!Configuration.Instance.CreateBackups)
				return;

			Console.WriteLine($"*** [{System.Threading.Thread.CurrentThread.ManagedThreadId}] Start of CreateBackup");
			Initialize();

			Console.WriteLine($"Creating backup of {dataObject.FullPath}");
			try
			{
				switch (dataObject.ChangeType)
				{
					case WatcherChangeTypes.Created:
					case WatcherChangeTypes.Changed:
						File.Copy(dataObject.FullPath, Path.Combine(Configuration.Instance.BackupDir, dataObject.Name), true);
						RunCommand("git", $"add {dataObject.Name}");
						break;
					case WatcherChangeTypes.Deleted:
						RunCommand("git", $"rm {dataObject.Name}");
						break;
					case WatcherChangeTypes.Renamed:
						RunCommand("git", $"mv {dataObject.OldName} {dataObject.Name}");
						break;
				}
				RunCommand("git", $"commit -m \"{dataObject.ChangeType} {dataObject.FullPath}\"");
			}
			catch (Exception e)
			{
				Console.WriteLine($"Got exception {e.GetType()}: {e.Message}");
			}
			Console.WriteLine($"*** [{System.Threading.Thread.CurrentThread.ManagedThreadId}] End of CreateBackup");
		}
	}
}