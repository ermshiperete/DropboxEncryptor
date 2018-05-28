using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Threading;

namespace DropboxEncryptor
{
	public sealed class FileWatcher: IDisposable
	{
		private readonly FileState _fileState;
		private readonly FileSystemWatcher _encryptedFileSystemWatcher;
		private readonly FileSystemWatcher _decryptedFileSystemWatcher;

		public FileWatcher(FileState fileState)
		{
			_fileState = fileState;
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
			Console.WriteLine($"*** [{Thread.CurrentThread.ManagedThreadId}]: OnEncryptedChanged ({e.ChangeType}) for {e.FullPath}");
			Debug.WriteLine($"*** [{Thread.CurrentThread.ManagedThreadId}]: OnEncryptedChanged ({e.ChangeType}) for {e.FullPath}");
			if (!FileState.IsSpecialFile(e.FullPath) && NeedProcessing(e, true))
				Enqueue(new FileChangedDataObject(Commands.EncryptedFileChangedCmd, e));
			else
			{
				Console.WriteLine($"*** [{Thread.CurrentThread.ManagedThreadId}]: skipping {e.FullPath}");
				Debug.WriteLine($"*** [{Thread.CurrentThread.ManagedThreadId}]: skipping {e.FullPath}");
			}
		}

		private void OnDecryptedChanged(object sender, FileSystemEventArgs e)
		{
			Console.WriteLine($"*** [{Thread.CurrentThread.ManagedThreadId}]: OnDecryptedChanged ({e.ChangeType}) for {e.FullPath}");
			Debug.WriteLine($"*** [{Thread.CurrentThread.ManagedThreadId}]: OnDecryptedChanged ({e.ChangeType}) for {e.FullPath}");
			if (!FileState.IsSpecialFile(e.FullPath) && NeedProcessing(e, false))
				Enqueue(new FileChangedDataObject(Commands.DecryptedFileChangedCmd, e));
			else
			{
				Console.WriteLine($"*** [{Thread.CurrentThread.ManagedThreadId}]: skipping {e.FullPath}");
				Debug.WriteLine($"*** [{Thread.CurrentThread.ManagedThreadId}]: skipping {e.FullPath}");
			}
		}

		private bool NeedProcessing(FileSystemEventArgs e, bool fileIsEncrypted)
		{
			if (e.ChangeType == WatcherChangeTypes.Deleted || e.ChangeType == WatcherChangeTypes.Renamed)
				return true;

			return _fileState.NeedProcessing(e.FullPath, fileIsEncrypted);
		}

		private int count;
		private void Enqueue(FileChangedDataObject dataObject)
		{
			lock (Server.Queue)
			{
				var lastDataObject = Server.Queue.LastOrDefault();
				if (dataObject.Equals(lastDataObject))
					return;

				Server.Queue.Enqueue(dataObject);
				Server.FileChangeQueuedFileChangedQueuedEvent.Set();
				var no = count++;
				var qcnt = 0;
				File.AppendAllText("/tmp/queue.txt", $"Content of queue after Enqueue {no} ({dataObject})\n");
				foreach (var o in Server.Queue.ToArray())
				{
					File.AppendAllText("/tmp/queue.txt", $"\t{no}-{qcnt++}: {o}\n");
				}
				File.AppendAllText("/tmp/queue.txt", $"End of queue {no}\n");
			}
		}

	}
}