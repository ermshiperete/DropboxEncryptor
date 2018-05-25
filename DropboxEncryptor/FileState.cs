// // Copyright (c) 2018 SIL International
// // This software is licensed under the MIT License (http://opensource.org/licenses/MIT)

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using Newtonsoft.Json;

namespace DropboxEncryptor
{
	public class FileState
	{
		private readonly Dictionary<string, CryptFileInfo> _fileTree;

		public FileState()
		{
			_fileTree = LoadFileTree();
		}

		public ImmutableDictionary<string, CryptFileInfo> FileTree
		{
			get
			{
				lock (_fileTree)
				{
					return _fileTree.ToImmutableDictionary();
				}
			}
		}
		public bool NeedProcessing(string fullFilePath, bool fileIsEncrypted)
		{
			lock (_fileTree)
			{
				var fileName = fileIsEncrypted ? Path.GetFileNameWithoutExtension(fullFilePath) : Path.GetFileName(fullFilePath);
				var newFileInfo = new FileInfo(fullFilePath);
				return !_fileTree.TryGetValue(fileName, out var oldFileInfo) ||
						newFileInfo.LastWriteTimeUtc > oldFileInfo.LastWriteTimeUtc;
			}
		}

		public void SaveFileTree()
		{
			lock (_fileTree)
			{
				var output = JsonConvert.SerializeObject(_fileTree);
				File.WriteAllText(StateFileName, output);
			}
		}

		private static Dictionary<string, CryptFileInfo> LoadFileTree()
		{
			if (!File.Exists(StateFileName))
				return new Dictionary<string, CryptFileInfo>();

			var input = File.ReadAllText(StateFileName);
			return JsonConvert.DeserializeObject<Dictionary<string, CryptFileInfo>>(input);
		}

		public static string StateFileName => Path.Combine(Configuration.Instance.DecryptedDir, ".state.config");

		public void AddFileToTree(string filePath)
		{
			lock (_fileTree)
			{
				_fileTree[FileHandler.GetFileName(filePath)] = new CryptFileInfo(new FileInfo(filePath));
				SaveFileTree();
			}
		}

		public void RemoveFileFromTree(string filePath)
		{
			lock (_fileTree)
			{
				_fileTree.Remove(FileHandler.GetFileName(filePath));
				SaveFileTree();
			}
		}

		public static bool IsSpecialFile(string filePath)
		{
			return Path.GetFileName(filePath) == KeyProvider.KeyFileName || filePath == FileState.StateFileName;
		}
	}
}