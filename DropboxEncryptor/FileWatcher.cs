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
		private readonly AutoResetEvent _eventQueued;
		private readonly AutoResetEvent _threadExited;
		private Queue<FileChangedDataObject> _queue;

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
			if (_queue != null)
			{
				Enqueue(null);
				_threadExited.WaitOne();
			}

			Console.WriteLine($"*** [{Thread.CurrentThread.ManagedThreadId}]: continuing Dispose");
			Debug.WriteLine($"*** [{Thread.CurrentThread.ManagedThreadId}]: continuing Dispose");
			_encryptedFileSystemWatcher.Dispose();
			_decryptedFileSystemWatcher.Dispose();

			_eventQueued.Dispose();
			_threadExited.Dispose();
			Console.WriteLine($"*** [{Thread.CurrentThread.ManagedThreadId}]: End of FileWatcher.Dispose(true)");
			Debug.WriteLine($"*** [{Thread.CurrentThread.ManagedThreadId}]: End of FileWatcher.Dispose(true)");
		}

		#endregion

		private void ProcessQueue(object obj)
		{
			// runs on background thread
			while (true)
			{
				Console.WriteLine(
					$"*** [{Thread.CurrentThread.ManagedThreadId}]: ProcessQueue waiting for queue");
				Debug.WriteLine(
					$"*** [{Thread.CurrentThread.ManagedThreadId}]: ProcessQueue waiting for queue");
				_eventQueued.WaitOne();

				FileChangedDataObject dataObject;
				lock (_queue)
				{
					dataObject = _queue.Dequeue();
				}

				Console.WriteLine(
					$"*** [{Thread.CurrentThread.ManagedThreadId}]: ProcessQueue: dequeued {dataObject}");
				Debug.WriteLine(
					$"*** [{Thread.CurrentThread.ManagedThreadId}]: ProcessQueue: dequeued {dataObject}");

				if (dataObject != null && WriteChangeToPipe(dataObject))
					continue;

				Console.WriteLine($"*** [{Thread.CurrentThread.ManagedThreadId}]: ProcessQueue exiting");
				Debug.WriteLine($"*** [{Thread.CurrentThread.ManagedThreadId}]: ProcessQueue exiting");
				try
				{
					_threadExited.Set();
				}
				catch (ObjectDisposedException)
				{
					EnableEvents(false);
					_queue = null;
				}

				return;
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

		private static bool WriteChangeToPipe(FileChangedDataObject dataObject)
		{
			Console.WriteLine($"*** [{Thread.CurrentThread.ManagedThreadId}]: Start of FileWatch.erWriteChangeToPipe");
			Debug.WriteLine($"*** [{Thread.CurrentThread.ManagedThreadId}]: Start of FileWatch.erWriteChangeToPipe");
			Console.WriteLine(
				$"*** [{Thread.CurrentThread.ManagedThreadId}]: Got change {dataObject.ChangeType} for {dataObject.FullPath}, direction {dataObject.Command}");
			Debug.WriteLine(
				$"*** [{Thread.CurrentThread.ManagedThreadId}]: Got change {dataObject.ChangeType} for {dataObject.FullPath}, direction {dataObject.Command}");
			using (var namedPipe = new NamedPipeClientStream(Server.NamedPipeName))
			{
				try
				{
					namedPipe.Connect(100);
				}
				catch (TimeoutException)
				{
					// We got a timeout which means that the server probably already shut down
					Console.WriteLine($"*** [{Thread.CurrentThread.ManagedThreadId}]: Timeout - End of FileWatch.erWriteChangeToPipe");
					Debug.WriteLine($"*** [{Thread.CurrentThread.ManagedThreadId}]: Timeout - End of FileWatch.erWriteChangeToPipe");
					return false;
				}

				var streamHelper = new StreamHelper(namedPipe);
				streamHelper.WriteString(dataObject.Command);
				streamHelper.WriteBinary(dataObject);
			}
			Console.WriteLine($"*** [{Thread.CurrentThread.ManagedThreadId}]: End of FileWatch.erWriteChangeToPipe");
			Debug.WriteLine($"*** [{Thread.CurrentThread.ManagedThreadId}]: End of FileWatch.erWriteChangeToPipe");
			return true;
		}

	}
}