using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Threading;

namespace DropboxEncryptor
{
	public class Server: IDisposable
	{
		public static string NamedPipeName { get; set; } = "DropboxEncryptorPipe";

		protected static Server Instance { get; set; }

		private NamedPipeServerStream _namedPipe;
		private FileWatcher _fileWatcher;
		private FileHandler _fileHandler;
		protected AutoResetEvent _commandEvent;
		private AutoResetEvent _fileChangedQueuedEvent;
		private readonly Queue<FileChangedDataObject> _queue;
		protected readonly CancellationTokenSource _cancellationTokenSource;
		protected readonly CancellationToken _cancellationToken;

		protected Server()
		{
			_commandEvent = new AutoResetEvent(false);
			_fileChangedQueuedEvent = new AutoResetEvent(false);
			_queue = new Queue<FileChangedDataObject>();
			_cancellationTokenSource = new CancellationTokenSource();
			_cancellationToken = _cancellationTokenSource.Token;
			Instance = this;
		}

		#region Disposable
		~Server()
		{
			Dispose(false);
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				OnDisposing();

				_fileHandler?.Dispose();
				_fileWatcher?.Dispose();
				_namedPipe?.Dispose();
				_commandEvent?.Dispose();
				_fileChangedQueuedEvent?.Dispose();
				_cancellationTokenSource.Dispose();
			}

			_namedPipe = null;
			_fileWatcher = null;
			_fileHandler = null;
			_commandEvent = null;
			_fileChangedQueuedEvent = null;
		}
		#endregion

		private void Setup()
		{
			_namedPipe = new NamedPipeServerStream(NamedPipeName);
			SetupKeyProviders();

			_fileWatcher = new FileWatcher();
			_fileHandler = new FileHandler();
			_fileWatcher.EnableEvents();
			OnSetupComplete();
		}

		protected virtual void SetupKeyProviders()
		{
			var keyProvider = new KeyProvider();
			var passwordProvider = new ConstPasswordProvider();

			FileEncryptor.Instance.Setup(keyProvider, passwordProvider);
			FileDecryptor.Instance.Setup(keyProvider, passwordProvider);
		}

		protected virtual void OnSetupComplete()
		{
		}

		protected virtual void OnAfterCommand(string command)
		{
		}

		protected virtual void OnDisposing()
		{
		}

		public int Run()
		{
			Setup();

			var exiting = false;

			var waitHandles = new List<WaitHandle> {
				_fileChangedQueuedEvent,
				_commandEvent,
				_cancellationToken.WaitHandle
			};

			while (!exiting || _queue.Count > 0)
			{
				_namedPipe.BeginWaitForConnection(ar =>
				{
					_namedPipe.EndWaitForConnection(ar);
					_commandEvent.Set();
				}, null);

				switch (_queue.Count > 0 ? 0 : WaitHandle.WaitAny(waitHandles.ToArray()))
				{
					case 0: // _eventQueuedWaitHandle
					{
						ProcessFileChangedQueuedEvent();
						break;
					}
					case 1: // _commandEvent
					{
						var commandResult = ProcessCommand();
						if (commandResult == 0)
							return commandResult;
						break;
					}
					case 2: // _cancellationToken
					{
						// Thread cancelled - process remaining events in queue
						Console.WriteLine(
							$"*** [{Thread.CurrentThread.ManagedThreadId}]: Detected cancellation ***");
						Debug.WriteLine(
							$"*** [{Thread.CurrentThread.ManagedThreadId}]: Detected cancellation ***");
						exiting = true;
						waitHandles.Remove(_cancellationToken.WaitHandle);
						waitHandles.Remove(_commandEvent);
						break;
					}
				}
			}

			return 0;
		}

		private int ProcessCommand()
		{
			try
			{
				var streamHelper = new StreamHelper(_namedPipe);
				var command = streamHelper.ReadString();
				Debug.WriteLine($"Read command {command}");
				Console.WriteLine(
					$"*** [{Thread.CurrentThread.ManagedThreadId}]: Read command {command}");
				Debug.WriteLine(
					$"*** [{Thread.CurrentThread.ManagedThreadId}]: Read command {command}");
				switch (command)
				{
					case Commands.Stop:
						_namedPipe.Disconnect();
						Debug.WriteLine("Exiting");
						OnAfterCommand(command);
					{
						return 0;
					}
					case Commands.Status:
						streamHelper.WriteString("ok");
						_namedPipe.Disconnect();
						OnAfterCommand(command);
						break;
					case Commands.DecryptedFileChanged:
					case Commands.EncryptedFileChanged:
						var dataObject =
							streamHelper.ReadBinary<FileChangedDataObject>();
						_fileHandler.HandleFileChange(Commands.FromName(command),
							dataObject);
						_namedPipe.Disconnect();
						OnAfterCommand(command);
						break;
				}
			}
			catch (EndOfStreamException)
			{
				Console.WriteLine(
					$"*** [{Thread.CurrentThread.ManagedThreadId}]: Reached EndOfStream - exiting");
				Debug.WriteLine(
					$"*** [{Thread.CurrentThread.ManagedThreadId}]: Reached EndOfStream - exiting");
				OnAfterCommand(Commands.Stop);
				return 0;
			}
			catch (InvalidOperationException)
			{
				Console.WriteLine(
					$"*** [{Thread.CurrentThread.ManagedThreadId}]: Reached InvalidOperation - exiting");
				Debug.WriteLine(
					$"*** [{Thread.CurrentThread.ManagedThreadId}]: Reached InvalidOperation - exiting");
				OnAfterCommand(Commands.Stop);
				return 0;
			}

			return 1;
		}

		private void ProcessFileChangedQueuedEvent()
		{
			FileChangedDataObject dataObject;
			lock (_queue)
			{
				if (!_queue.TryDequeue(out dataObject))
					return;
			}
			Console.WriteLine(
				$"*** [{Thread.CurrentThread.ManagedThreadId}]: Dequeued {dataObject.ChangeType} for {dataObject.Name}");
			Debug.WriteLine(
				$"*** [{Thread.CurrentThread.ManagedThreadId}]: Dequeued {dataObject.ChangeType} for {dataObject.Name}");
			_fileHandler.HandleFileChange(Commands.FromName(dataObject.Command),
				dataObject);
			OnAfterCommand(dataObject.Command);
		}

		public static void StopServer()
		{
			// This method is mainly needed for unit tests
			Instance._cancellationTokenSource.Cancel();
		}

		public static Server Create()
		{
			var server = new Server();
			Instance = server;
			return server;
		}

		public static AutoResetEvent FileChangeQueuedFileChangedQueuedEvent => Instance._fileChangedQueuedEvent;

		public static Queue<FileChangedDataObject> Queue => Instance._queue;
	}
}