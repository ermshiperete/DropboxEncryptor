using System;
using System.IO;

namespace DropboxEncryptor
{
	[Serializable]
	public class FileChangedDataObject
	{
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

		public string Command { get; set; }
		public WatcherChangeTypes ChangeType { get; set; }
		public string FullPath { get; set; }
		public string OldFullPath { get; set; }

		public string Name => Path.GetFileName(FullPath);
		public string OldName => Path.GetFileName(OldFullPath);
	}
}