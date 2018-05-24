using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Threading;

namespace DropboxEncryptor
{
	public class Server: IDisposable
	{
		public static string NamedPipeName { get; set; } = "DropboxEncryptorPipe";

		protected NamedPipeServerStream _namedPipe;
		protected FileWatcher _fileWatcher;
		protected FileHandler _fileHandler;
		private AutoResetEvent _commandEvent = new AutoResetEvent(false);

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
			}

			_namedPipe = null;
			_fileWatcher = null;
			_fileHandler = null;
			_commandEvent = null;
		}
		#endregion

		protected void Setup()
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

			while (true)
			{
				_namedPipe.BeginWaitForConnection(ar =>
				{
					_namedPipe.EndWaitForConnection(ar);
					_commandEvent.Set();
				}, null);

				WaitHandle.WaitAny(new WaitHandle[] {_commandEvent});

				try
				{
					var streamHelper = new StreamHelper(_namedPipe);
					var command = streamHelper.ReadString();
					Debug.WriteLine($"Read command {command}");
					Console.WriteLine($"*** [{Thread.CurrentThread.ManagedThreadId}]: Read command {command}");
					Debug.WriteLine($"*** [{Thread.CurrentThread.ManagedThreadId}]: Read command {command}");
					switch (command)
					{
						case Commands.Stop:
							_namedPipe.Disconnect();
							Debug.WriteLine("Exiting");
							OnAfterCommand(command);
							return 0;
						case Commands.Status:
							streamHelper.WriteString("ok");
							_namedPipe.Disconnect();
							OnAfterCommand(command);
							break;
						case Commands.DecryptedFileChanged:
						case Commands.EncryptedFileChanged:
							var dataObject = streamHelper.ReadBinary<FileChangedDataObject>();
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
			}
		}
	}
}