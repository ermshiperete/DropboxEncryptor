using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Threading;

namespace DropboxEncryptor
{
	public sealed class FileWatcher: IDisposable
	{
		private readonly FileSystemWatcher _encryptedFileSystemWatcher;
		private readonly FileSystemWatcher _decryptedFileSystemWatcher;

		public FileWatcher()
		{
			_encryptedFileSystemWatcher =
				new FileSystemWatcher(Configuration.Instance.EncryptedDir) {
					IncludeSubdirectories = false,
					NotifyFilter = NotifyFilters.LastWrite
									| NotifyFilters.FileName | NotifyFilters.DirectoryName,
					InternalBufferSize = 16384
				};
			_encryptedFileSystemWatcher.Changed += OnEncryptedChanged;
			_encryptedFileSystemWatcher.Created += OnEncryptedChanged;
			_encryptedFileSystemWatcher.Deleted += OnEncryptedChanged;
			_encryptedFileSystemWatcher.Renamed += OnEncryptedChanged;

			_decryptedFileSystemWatcher =
				new FileSystemWatcher(Configuration.Instance.DecryptedDir) {
					IncludeSubdirectories = true,
					NotifyFilter = NotifyFilters.LastWrite
									| NotifyFilters.FileName | NotifyFilters.DirectoryName,
					InternalBufferSize = 16384
				};
			_decryptedFileSystemWatcher.Changed += OnDecryptedChanged;
			_decryptedFileSystemWatcher.Created += OnDecryptedChanged;
			_decryptedFileSystemWatcher.Deleted += OnDecryptedChanged;
			_decryptedFileSystemWatcher.Renamed += OnDecryptedChanged;
		}

		public void EnableEvents(bool enable = true)
		{
			_encryptedFileSystemWatcher.EnableRaisingEvents = enable;
			_decryptedFileSystemWatcher.EnableRaisingEvents = enable;
			Console.WriteLine($"*** [{Thread.CurrentThread.ManagedThreadId}]: EnableEvents({enable})");
			Debug.WriteLine($"*** [{Thread.CurrentThread.ManagedThreadId}]: EnableEvents({enable})");
		}

		#region Disposable

		~FileWatcher()
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

			EnableEvents(false);

			Console.WriteLine($"*** [{Thread.CurrentThread.ManagedThreadId}]: Start of FileWatcher.Dispose(true)");
			Debug.WriteLine($"*** [{Thread.CurrentThread.ManagedThreadId}]: Start of FileWatcher.Dispose(true)");

			_encryptedFileSystemWatcher.Dispose();
			_decryptedFileSystemWatcher.Dispose();

			Console.WriteLine($"*** [{Thread.CurrentThread.ManagedThreadId}]: End of FileWatcher.Dispose(true)");
			Debug.WriteLine($"*** [{Thread.CurrentThread.ManagedThreadId}]: End of FileWatcher.Dispose(true)");
		}

		#endregion

		private void OnEncryptedChanged(object sender, FileSystemEventArgs e)
		{
			Enqueue(new FileChangedDataObject(Commands.EncryptedFileChangedCmd, e));
		}

		private void OnDecryptedChanged(object sender, FileSystemEventArgs e)
		{
			Enqueue(new FileChangedDataObject(Commands.DecryptedFileChangedCmd, e));
		}

		private void Enqueue(FileChangedDataObject dataObject)
		{
			lock (Server.Queue)
			{
				Server.Queue.Enqueue(dataObject);
				Server.FileChangeQueuedFileChangedQueuedEvent.Set();
			}
		}

	}
}