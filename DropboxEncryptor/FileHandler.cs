using System;
using System.Collections.Generic;
using System.IO;
using System.Timers;

namespace DropboxEncryptor
{
	public class FileHandler: IDisposable
	{
		public static int TimerInterval { get; set; } = 1000;

		private Configuration _config;
		private FileSystemWatcher _encryptedFileSystemWatcher;
		private FileSystemWatcher _decryptedFileSystemWatcher;
		private Queue<FileSystemEventArgs> _encryptedFileQueue;
		private Queue<FileSystemEventArgs> _decryptedFileQueue;
		private Timer _timer;
		private Dictionary<string, FileInfo> _fileTree;

		public FileHandler(Configuration config)
		{
			_config = config;
			_fileTree = new Dictionary<string, FileInfo>();

			_encryptedFileQueue = new Queue<FileSystemEventArgs>();
			_decryptedFileQueue = new Queue<FileSystemEventArgs>();

			_timer = new Timer(TimerInterval) { AutoReset = true }; // check queues every second
			_timer.Elapsed += OnCheckQueues;
			_timer.Enabled = true;

			_encryptedFileSystemWatcher = new FileSystemWatcher(config.EncryptedDir) {
				IncludeSubdirectories = false,
				NotifyFilter = NotifyFilters.LastWrite
					| NotifyFilters.FileName | NotifyFilters.DirectoryName
			};
			_encryptedFileSystemWatcher.Changed += OnEncryptedChanged;
			//_encryptedFileSystemWatcher.Created += OnEncryptedChanged;
			_encryptedFileSystemWatcher.Deleted += OnEncryptedChanged;
			_encryptedFileSystemWatcher.Renamed += OnEncryptedRenamed;
			_encryptedFileSystemWatcher.EnableRaisingEvents = true;

			foreach (var file in Directory.EnumerateFiles(config.EncryptedDir))
			{
				AddFileToQueue(file, _encryptedFileQueue);
			}

			_decryptedFileSystemWatcher = new FileSystemWatcher(config.DecryptedDir) {
				IncludeSubdirectories = true,
				NotifyFilter = NotifyFilters.LastWrite
					| NotifyFilters.FileName | NotifyFilters.DirectoryName
			};
			_decryptedFileSystemWatcher.Changed += OnDecryptedChanged;
			//_decryptedFileSystemWatcher.Created += OnDecryptedChanged;
			_decryptedFileSystemWatcher.Deleted += OnDecryptedChanged;
			_decryptedFileSystemWatcher.Renamed += OnDecryptedRenamed;
			_decryptedFileSystemWatcher.EnableRaisingEvents = true;

			foreach (var file in Directory.EnumerateFiles(config.DecryptedDir))
			{
				AddFileToQueue(file, _decryptedFileQueue);
			}
			
			OnCheckQueues(this, null);
		}

		#region Disposable

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!disposing)
				return;

			_encryptedFileSystemWatcher.Dispose();
			_encryptedFileSystemWatcher = null;

			_decryptedFileSystemWatcher.Dispose();
			_decryptedFileSystemWatcher = null;
		}
		#endregion

		private void OnEncryptedChanged(object sender, FileSystemEventArgs e)
		{
			AddFileToQueue(e, _encryptedFileQueue);
		}

		private void AddFileToQueue(FileSystemEventArgs e, Queue<FileSystemEventArgs> queue)
		{
			if (queue.Contains(e.FullPath))
				return;

			var fileName = queue == _encryptedFileQueue 
				? Path.GetFileNameWithoutExtension(e.FullPath) 
				: Path.GetFileName(e.FullPath);
			var newFileInfo = new FileInfo(e.FullPath);
			if (!_fileTree.TryGetValue(fileName, out var tuple) || 
				newFileInfo.LastWriteTimeUtc > tuple.Item3.LastWriteTimeUtc)
			{
				Console.WriteLine($"{DateTime.Now}: Adding file {e.FullPath} to queue");
				queue.Enqueue(filePath);
			}

			_fileTree[fileName] = (newFileInfo;
		}

		private void OnEncryptedRenamed(object sender, RenamedEventArgs e)
		{
			throw new System.NotImplementedException();
		}

		private void OnDecryptedChanged(object sender, FileSystemEventArgs e)
		{
			AddFileToQueue(e.FullPath, _decryptedFileQueue);
		}

		private void OnDecryptedRenamed(object sender, RenamedEventArgs e)
		{
			throw new System.NotImplementedException();
		}

		private void OnCheckQueues(object sender, ElapsedEventArgs e)
		{
			Console.WriteLine($"{DateTime.Now}: Checking queue");
			for (var i = 0; i < _encryptedFileQueue.Count; i++)
			{
				var fileName = _encryptedFileQueue.Dequeue();
				DecryptFile(fileName);
			}

			for (var fileName = _decryptedFileQueue.Dequeue();
				fileName != null;
				fileName = _decryptedFileQueue.Dequeue())
			{
				EncryptFile(fileName);
			}
		}

		private void EncryptFile(string filePath)
		{
			var dest = Path.Combine(_config.EncryptedDir, Path.GetFileName(filePath) + ".enc");
			Console.WriteLine($"{DateTime.Now}: Encrypting {filePath} into {dest}");
			File.Copy(filePath, dest, true);
			Console.WriteLine($"After copying: exists: {File.Exists(dest)}");
		}

		private void DecryptFile(string filePath)
		{
			var dest = Path.Combine(_config.DecryptedDir, Path.GetFileNameWithoutExtension(filePath));
			Console.WriteLine($"{DateTime.Now}: Decrypting {filePath} into {dest}");
			File.Copy(filePath, dest, true);
			Console.WriteLine($"After copying: exists: {File.Exists(dest)}");
		}
	}
}