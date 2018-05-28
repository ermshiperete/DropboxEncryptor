using System;
using System.IO;

namespace DropboxEncryptor
{
	[Serializable]
	public class FileChangedDataObject
	{
		// ReSharper disable once MemberCanBePrivate.Global
		public FileChangedDataObject()
		{
		}

		public FileChangedDataObject(WatcherChangeTypes changeType,
			string directory, string file, string oldDirectory = null, string oldFile = null)
		{
			ChangeType = changeType;
			FullPath = Path.Combine(directory, file);
			if (!string.IsNullOrEmpty(oldDirectory) && !string.IsNullOrEmpty(oldFile))
				OldFullPath = Path.Combine(oldDirectory, oldFile);
		}

		public FileChangedDataObject(Commands command, FileSystemEventArgs e)
		{
			Command = command.Name;
			ChangeType = e.ChangeType;
			FullPath = e.FullPath;

			var renameEventArgs = e as RenamedEventArgs;
			OldFullPath = renameEventArgs?.OldFullPath;
		}

		public string Command { get; }
		public WatcherChangeTypes ChangeType { get; }
		public string FullPath { get; }
		public string OldFullPath { get; }

		public string Name => Path.GetFileName(FullPath);
		public string OldName => Path.GetFileName(OldFullPath);

		public override bool Equals(object obj)
		{
			if (!(obj is FileChangedDataObject other))
				return false;

			return Command == other.Command &&
				ChangeType == other.ChangeType &&
				FullPath == other.FullPath &&
				OldFullPath == other.OldFullPath;
		}

		public override int GetHashCode()
		{
			return Command.GetHashCode() ^ (int)ChangeType ^
				FullPath.GetHashCode() ^ OldFullPath.GetHashCode();
		}

		public override string ToString()
		{
			return string.IsNullOrEmpty(OldFullPath)
				? $"[{Command}] {ChangeType} {FullPath}"
				: $"[{Command}] {ChangeType} {FullPath} from {OldFullPath}";
		}
	}
}