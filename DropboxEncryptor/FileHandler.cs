using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace DropboxEncryptor
{
	public sealed class FileHandler: IDisposable
	{
		private FileState _fileState;
		private SHA1 _hashProvider;
		private string _tmpDecryptedFile;

		public FileHandler(FileState fileState)
		{
			_fileState = fileState;
			var oldFileTree = _fileState.FileTree;

			foreach (var file in Directory.EnumerateFiles(Configuration.Instance.EncryptedDir))
			{
				if (FileState.IsSpecialFile(file))
					continue;

				if (FileDeletedWhileOffline(oldFileTree, file))
				{
					oldFileTree.Remove(GetFileName(file));
					File.Delete(file);
					continue;
				}

				var decryptedFilePath = FileDecryptor.GetDecryptedFilePath(file);
				if (DoesFileConflict(file, decryptedFilePath))
				{
					var conflictedFilePath = GetConflictedFilePath(decryptedFilePath);
					DecryptFile(file, conflictedFilePath);
					Backup.CreateBackup(new FileChangedDataObject(WatcherChangeTypes.Created,
						Configuration.Instance.DecryptedDir,
						Path.GetFileName(conflictedFilePath)));
				}
				else
				{
					DecryptFile(file);
					Backup.CreateBackup(new FileChangedDataObject(WatcherChangeTypes.Created,
						Configuration.Instance.DecryptedDir,
						Path.GetFileName(decryptedFilePath)));
				}
			}

			foreach (var file in Directory.EnumerateFiles(Configuration.Instance.DecryptedDir))
			{
				if (FileState.IsSpecialFile(file))
					continue;

				var encrypedFilePath = FileEncryptor.GetEncryptedFilePath(file);
				if (FileDeletedWhileOffline(oldFileTree, file))
				{
					oldFileTree.Remove(GetFileName(file));
					Backup.CreateBackup(new FileChangedDataObject(WatcherChangeTypes.Deleted,
						Configuration.Instance.DecryptedDir, Path.GetFileName(file)));
					File.Delete(file);
					continue;
				}

				if (DoesFileConflict(file, encrypedFilePath))
					EncryptFile(file, GetConflictedFilePath(encrypedFilePath));
				else
					EncryptFile(file);
			}

		}

		#region Disposable

		~FileHandler()
		{
			Dispose(false);
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		private void Dispose(bool disposing)
		{
			if (!disposing)
				return;

			if (!string.IsNullOrEmpty(_tmpDecryptedFile))
				File.Delete(_tmpDecryptedFile);
			_tmpDecryptedFile = null;
		}
		#endregion

		private bool FileDeletedWhileOffline(IReadOnlyDictionary<string, CryptFileInfo> fileTree, string filePath)
		{
			var fileName = GetFileName(filePath);
			return fileTree.TryGetValue(fileName, out _);
		}

		private SHA1 HashProvider => _hashProvider ?? (_hashProvider = SHA1.Create());

		private FileStream GetFileStream(string filePath)
		{
			if (!filePath.EndsWith(FileBaseCryptor.EncodingExtension))
				return File.OpenRead(filePath);

			if (!string.IsNullOrEmpty(_tmpDecryptedFile))
				File.Delete(_tmpDecryptedFile);
			_tmpDecryptedFile = Path.GetTempFileName();

			FileDecryptor.Instance.DecryptFile(filePath, _tmpDecryptedFile);
			return File.OpenRead(_tmpDecryptedFile);
		}

		private string CalculateHash(string filePath)
		{
			byte[] hash;
			using (var fileStream = GetFileStream(filePath))
			{
				hash = HashProvider.ComputeHash(fileStream);
			}

			if (_tmpDecryptedFile != null)
			{
				File.Delete(_tmpDecryptedFile);
				_tmpDecryptedFile = null;
			}

			var bldr = new StringBuilder();

			foreach (var b in hash)
			{
				bldr.Append(b.ToString("x2"));
			}

			return bldr.ToString();
		}

		private bool DoesFileConflict(string filePath, string otherFilePath)
		{
			if (!File.Exists(otherFilePath))
				return false;

			var comparer = StringComparer.OrdinalIgnoreCase;
			return comparer.Compare(CalculateHash(filePath), CalculateHash(otherFilePath)) != 0;
		}

		public static string GetFileName(string filePath)
		{
			var fileName = filePath.EndsWith(FileBaseCryptor.EncodingExtension)
				? Path.GetFileNameWithoutExtension(filePath)
				: Path.GetFileName(filePath);
			return fileName;
		}

		private static string GetConflictedFilePath(string filePath)
		{
			return Path.Combine(Path.GetDirectoryName(filePath),
				Path.GetFileNameWithoutExtension(GetFileName(filePath))
				+ $" (someone's conflicted copy {DateTime.Now:yyyy-MM-dd})"
				+ Path.GetExtension(filePath));
		}

		public void HandleFileChange(Commands direction, FileChangedDataObject dataObject)
		{
			if (direction == Commands.DecryptedFileChangedCmd)
				HandleDecryptedFileChange(dataObject);
			else
				HandleEncryptedFileChange(dataObject);
		}

		private void HandleEncryptedFileChange(FileChangedDataObject dataObject)
		{
			if (FileState.IsSpecialFile(dataObject.FullPath))
			{
				Debug.WriteLine(
					$"*** [{System.Threading.Thread.CurrentThread.ManagedThreadId}]: Got EncryptedChanged ({dataObject.ChangeType}) for special file {dataObject.FullPath} - ignoring");
				return;
			}

			Console.WriteLine($"*** [{System.Threading.Thread.CurrentThread.ManagedThreadId}]: Start of HandleEncryptedFileChange");
			Debug.WriteLine($"*** [{System.Threading.Thread.CurrentThread.ManagedThreadId}]: Start of HandleEncryptedFileChange");
			Debug.WriteLine($"Got EncryptedChanged ({dataObject.ChangeType}) for {dataObject.FullPath}");
			Console.WriteLine($"Got EncryptedChanged ({dataObject.ChangeType}) for {dataObject.FullPath}");
			// ReSharper disable once SwitchStatementMissingSomeCases
			switch (dataObject.ChangeType)
			{
				case WatcherChangeTypes.Created:
				case WatcherChangeTypes.Changed:
					DecryptFile(dataObject.FullPath);
					break;
				case WatcherChangeTypes.Deleted:
					DeleteDecryptedFile(dataObject.FullPath);
					break;
				case WatcherChangeTypes.Renamed:
					DeleteDecryptedFile(dataObject.OldFullPath);
					DecryptFile(dataObject.FullPath);
					break;
			}
			Console.WriteLine($"*** [{System.Threading.Thread.CurrentThread.ManagedThreadId}]: End of HandleEncryptedFileChange");
			Debug.WriteLine($"*** [{System.Threading.Thread.CurrentThread.ManagedThreadId}]: End of HandleEncryptedFileChange");
		}

		private void HandleDecryptedFileChange(FileChangedDataObject dataObject)
		{
			if (FileState.IsSpecialFile(dataObject.FullPath))
			{
				Debug.WriteLine(
					$"Got DecryptedChanged ({dataObject.ChangeType}) for special file {dataObject.FullPath} - ignoring");
				return;
			}

			Console.WriteLine($"*** [{System.Threading.Thread.CurrentThread.ManagedThreadId}]: Start of HandleDecryptedFileChange");
			Debug.WriteLine($"*** [{System.Threading.Thread.CurrentThread.ManagedThreadId}]: Start of HandleDecryptedFileChange");
			//Debug.WriteLine($"Got DecryptedChanged ({dataObject.ChangeType}) for {dataObject.FullPath}");
			Console.WriteLine($"*** [{System.Threading.Thread.CurrentThread.ManagedThreadId}]: Got DecryptedChanged ({dataObject.ChangeType}) for {dataObject.FullPath}");
			Debug.WriteLine($"*** [{System.Threading.Thread.CurrentThread.ManagedThreadId}]: Got DecryptedChanged ({dataObject.ChangeType}) for {dataObject.FullPath}");
			Backup.CreateBackup(dataObject);

			// ReSharper disable once SwitchStatementMissingSomeCases
			switch (dataObject.ChangeType)
			{
				case WatcherChangeTypes.Created:
				case WatcherChangeTypes.Changed:
					EncryptFile(dataObject.FullPath);
					break;
				case WatcherChangeTypes.Deleted:
					DeleteEncryptedFile(dataObject.FullPath);
					break;
				case WatcherChangeTypes.Renamed:
					DeleteEncryptedFile(dataObject.OldFullPath);
					EncryptFile(dataObject.FullPath);
					break;
			}
			Console.WriteLine($"*** [{System.Threading.Thread.CurrentThread.ManagedThreadId}]: End of HandleDecryptedFileChange");
			Debug.WriteLine($"*** [{System.Threading.Thread.CurrentThread.ManagedThreadId}]: End of HandleDecryptedFileChange");
		}

		private static bool IsFileLocked(string filePath)
		{
			FileStream stream = null;

			try
			{
				stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.None);
				return false;
			}
			catch (IOException)
			{
				//the file is unavailable because it is:
				//still being written to
				//or being processed by another thread
				//or does not exist (has already been processed)
				return true;
			}
			finally
			{
				stream?.Close();
			}
		}

		private void EncryptFile(string decryptedFilePath, string encryptedFilePath = null)
		{
			if (!_fileState.NeedProcessing(decryptedFilePath, false) || FileState.IsSpecialFile(decryptedFilePath))
				return;

			if (IsFileLocked(decryptedFilePath))
			{
				// TODO: what now?
				return;
			}

			if (encryptedFilePath != null)
			{
				var newDecryptedFilePath = FileDecryptor.GetDecryptedFilePath(encryptedFilePath);
				File.Move(decryptedFilePath, newDecryptedFilePath);
				FileEncryptor.Instance.EncryptFile(decryptedFilePath, encryptedFilePath);
				_fileState.AddFileToTree(newDecryptedFilePath);
			}
			else
			{
				FileEncryptor.Instance.EncryptFile(decryptedFilePath);
				_fileState.AddFileToTree(decryptedFilePath);
			}
		}

		private void DeleteEncryptedFile(string decryptedFile)
		{
			var dest = Path.Combine(Configuration.Instance.EncryptedDir,
				Path.GetFileName(decryptedFile) + FileBaseCryptor.EncodingExtension);
			File.Delete(dest);
			_fileState.RemoveFileFromTree(decryptedFile);
		}

		private void DecryptFile(string encryptedFilePath, string decryptedFilePath = null)
		{
			if (!_fileState.NeedProcessing(encryptedFilePath, true) || FileState.IsSpecialFile(encryptedFilePath))
				return;

			if (IsFileLocked(encryptedFilePath))
			{
				// TODO: what now?
				return;
			}

			if (decryptedFilePath != null)
			{
				var newEncryptedFilePath = FileEncryptor.GetEncryptedFilePath(decryptedFilePath);
				File.Move(encryptedFilePath, newEncryptedFilePath);
				FileDecryptor.Instance.DecryptFile(newEncryptedFilePath, decryptedFilePath);
				_fileState.AddFileToTree(decryptedFilePath);
			}
			else
			{
				FileDecryptor.Instance.DecryptFile(encryptedFilePath);
				_fileState.AddFileToTree(encryptedFilePath);
			}
		}

		private void DeleteDecryptedFile(string encryptedFile)
		{
			var dest = Path.Combine(Configuration.Instance.DecryptedDir,
				Path.GetFileNameWithoutExtension(encryptedFile));
			File.Delete(dest);
			_fileState.RemoveFileFromTree(GetFileName(encryptedFile));
		}
	}
}