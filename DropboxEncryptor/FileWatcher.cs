using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Threading;

namespace DropboxEncryptor
{
	public sealed class FileWatcher: IDisposable
	{
		private readonly FileSystemWatcher _encryptedFileSystemWatcher;
		private readonly FileSystemWatcher _decryptedFileSystemWatcher;
		private readonly Queue<FileChangedDataObject> _queue;
		private readonly AutoResetEvent _eventQueued;
		private readonly AutoResetEvent _threadExited;

		public FileWatcher()
		{
			_eventQueued = new AutoResetEvent(false);
			_threadExited = new AutoResetEvent(false);
			_queue = new Queue<FileChangedDataObject>();
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

			ThreadPool.QueueUserWorkItem(ProcessQueue);
		}

		public void EnableEvents()
		{
			_encryptedFileSystemWatcher.EnableRaisingEvents = true;
			_decryptedFileSystemWatcher.EnableRaisingEvents = true;
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

			Console.WriteLine($"*** [{Thread.CurrentThread.ManagedThreadId}]: Start of FileWatcher.Dispose(true)");
			Enqueue(null);
			_threadExited.WaitOne();

			Console.WriteLine($"*** [{Thread.CurrentThread.ManagedThreadId}]: continuing Dispose");
			_encryptedFileSystemWatcher.Dispose();
			_decryptedFileSystemWatcher.Dispose();

			_eventQueued.Dispose();
			_threadExited.Dispose();
			Console.WriteLine($"*** [{Thread.CurrentThread.ManagedThreadId}]: End of FileWatcher.Dispose(true)");
		}

		#endregion

		private void ProcessQueue(object obj)
		{
			// runs on background thread
			while (true)
			{
				Console.WriteLine($"*** [{Thread.CurrentThread.ManagedThreadId}]: ProcessQueue waiting for queue");
				_eventQueued.WaitOne();

				FileChangedDataObject dataObject;
				lock (_queue)
				{
					dataObject = _queue.Dequeue();
				}

				Console.WriteLine($"*** [{Thread.CurrentThread.ManagedThreadId}]: ProcessQueue: dequeued {dataObject}");

				if (dataObject == null)
				{
					Console.WriteLine($"*** [{Thread.CurrentThread.ManagedThreadId}]: ProcessQueue exiting");
					_threadExited.Set();
					return;
				}

				WriteChangeToPipe(dataObject);
			}
		}
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
			lock (_queue)
			{
				_queue.Enqueue(dataObject);
				_eventQueued.Set();
			}
		}

//		private static void WriteChangeToPipeOld(Commands direction, FileSystemEventArgs e)
//		{
//			Console.WriteLine($"*** [{System.Threading.Thread.CurrentThread.ManagedThreadId}]: Start of FileWatch.erWriteChangeToPipe");
//			Console.WriteLine(
//				$"*** [{System.Threading.Thread.CurrentThread.ManagedThreadId}]: Got change {e.ChangeType} for {e.FullPath}, direction {direction}");
//			using (var namedPipe = new NamedPipeClientStream(Server.NamedPipeName))
//			{
//				namedPipe.Connect();
//
//				var streamHelper = new StreamHelper(namedPipe);
//				streamHelper.WriteString(direction.Name);
//
//				var dataObject = new FileChangedDataObject(direction, e);
//				streamHelper.WriteBinary(dataObject);
//			}
//			Console.WriteLine($"*** [{System.Threading.Thread.CurrentThread.ManagedThreadId}]: End of FileWatch.erWriteChangeToPipe");
//		}

		private static void WriteChangeToPipe(FileChangedDataObject dataObject)
		{
			Console.WriteLine($"*** [{Thread.CurrentThread.ManagedThreadId}]: Start of FileWatch.erWriteChangeToPipe");
			Console.WriteLine(
				$"*** [{Thread.CurrentThread.ManagedThreadId}]: Got change {dataObject.ChangeType} for {dataObject.FullPath}, direction {dataObject.Command}");
			using (var namedPipe = new NamedPipeClientStream(Server.NamedPipeName))
			{
				namedPipe.Connect();

				var streamHelper = new StreamHelper(namedPipe);
				streamHelper.WriteString(dataObject.Command);
				streamHelper.WriteBinary(dataObject);
			}
			Console.WriteLine($"*** [{Thread.CurrentThread.ManagedThreadId}]: End of FileWatch.erWriteChangeToPipe");
		}

	}
}