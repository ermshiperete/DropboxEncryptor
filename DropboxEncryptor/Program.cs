using System;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.IO.Pipes;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using CommandLine;

namespace DropboxEncryptor
{
	public static class Program
	{
		private static ParserResult<Options> _parserResult;

		public static int Main(string[] args)
		{
			_parserResult = Options.ParseCommandLineArgs(args);
			return _parserResult.MapResult(
				Run,
				err => 1);
		}

		private static int Run(Options options)
		{
			if (options.Stop || options.Status)
				return ClientMode(options);

			if (options.IsDaemon)
				return ServerMode();

			Console.WriteLine("Missing arguments.");
			return 1;
		}

		private static int ServerMode()
		{
			if (!Configuration.Load())
			{
				Console.WriteLine("Can't find configuration.");
				Configure();
				Console.WriteLine("New configuration saved.");
				return 0;
			}

			using (var server = new Server())
			{
				return server.Run();
			}
		}

		private static int ClientMode(Options options)
		{
			using (var namedPipe = new NamedPipeClientStream(Server.NamedPipeName))
			{
				if (options.Stop)
				{
					SendCommandToDaemon(namedPipe, Commands.Stop);
					{
						return 0;
					}
				}

				if (!options.Status)
					return -1;

				Console.Write("Status: ");
				if (!SendCommandToDaemon(namedPipe, Commands.Status))
				{
					Console.WriteLine("not running");
					return 0;
				}

				var streamString = new StreamHelper(namedPipe);
				Console.WriteLine(streamString.ReadString());
			}

			return 0;
		}

		private static bool SendCommandToDaemon(NamedPipeClientStream namedPipe, string command)
		{
			try
			{
				namedPipe.Connect(100);
				var streamString = new StreamHelper(namedPipe);
				streamString.WriteString(command);

				return true;
			}
			catch (TimeoutException)
			{
				Debug.WriteLine("Got timeout trying to connect to named pipe");
				return false;
			}
		}

		private static void Configure()
		{
			Console.Write($"Directory for encrypted files ({Configuration.Instance.EncryptedDir}): ");
			var encryptedDir = Console.ReadLine();
			if (!string.IsNullOrEmpty(encryptedDir))
				Configuration.Instance.EncryptedDir = encryptedDir;

			Console.Write($"Directory for decrypted files ({Configuration.Instance.DecryptedDir}): ");
			var decryptedDir = Console.ReadLine();
			if (!string.IsNullOrEmpty(decryptedDir))
				Configuration.Instance.DecryptedDir = decryptedDir;

			var defaultCreateBackup = Configuration.Instance.CreateBackups ? "Y,n" : "y,N";
			Console.Write($"Create backup? ({defaultCreateBackup}): ");
			var createBackups = Console.ReadLine();
			if (!string.IsNullOrEmpty(createBackups))
				Configuration.Instance.CreateBackups = createBackups.ToLowerInvariant().StartsWith("y");

			if (Configuration.Instance.CreateBackups)
			{
				Console.Write($"Directory for backups ({Configuration.Instance.BackupDir}): ");
				var backupDir = Console.ReadLine();
				if (!string.IsNullOrEmpty(backupDir))
					Configuration.Instance.BackupDir = backupDir;
			}

			Configuration.Instance.Save();
		}
	}
}