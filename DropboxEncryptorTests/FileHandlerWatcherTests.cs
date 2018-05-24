
using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using DropboxEncryptor;
using NUnit.Framework;

namespace DropboxEncryptorTests
{
	[TestFixture(TestOf = typeof(FileHandler))]
	public class FileHandlerWatcherTests
	{
		private const int TestInterval = 500;

		public enum EncryptionDirection
		{
			Encrypted,
			Decrypted
		}

		private const EncryptionDirection Encrypted = EncryptionDirection.Encrypted;
		private const EncryptionDirection Decrypted = EncryptionDirection.Decrypted;

		private static EncryptionDirection Not(EncryptionDirection dir)
		{
			return dir == Encrypted ? Decrypted : Encrypted;
		}

		private static string FileName(EncryptionDirection encrypted, string filename = "A.txt")
		{
			return encrypted == Encrypted
				? Path.Combine(Configuration.Instance.EncryptedDir, filename + ".enc")
				: Path.Combine(Configuration.Instance.DecryptedDir, filename);
		}

		private static void WriteFile(EncryptionDirection encrypted, string fileName = "A.txt", string text = "This is a test")
		{
			if (encrypted == Encrypted)
			{
				var decryptedFilePath = Path.GetTempFileName();
				File.WriteAllText(decryptedFilePath, text);
				FileEncryptor.Instance.EncryptFile(decryptedFilePath, FileName(encrypted, fileName));
				File.Delete(decryptedFilePath);
			}
			else
			{
				File.WriteAllText(FileName(encrypted, fileName), text);
			}
		}

		private class ServerForTesting : Server
		{
			private readonly AutoResetEvent _threadFinishedEvent;
			private readonly AutoResetEvent _serverReadyEvent;

			private ServerForTesting()
			{
				_threadFinishedEvent = new AutoResetEvent(false);
				_serverReadyEvent = new AutoResetEvent(false);
			}

			protected override void Dispose(bool disposing)
			{
				Console.WriteLine($"*** [{Thread.CurrentThread.ManagedThreadId}]: Start of ServerForTesting.Dispose({disposing})");
				base.Dispose(disposing);

				if (disposing)
				{
					_threadFinishedEvent.Dispose();
					_serverReadyEvent.Dispose();
				}

				Console.WriteLine($"*** [{Thread.CurrentThread.ManagedThreadId}]: End of ServerForTesting.Dispose({disposing})");
			}

			protected override void OnDisposing()
			{
				Console.WriteLine($"*** [{Thread.CurrentThread.ManagedThreadId}]: Start of OnDisposing");
				using (var namedPipe = new NamedPipeClientStream(NamedPipeName))
				{
					namedPipe.Connect(100);
					var streamString = new StreamHelper(namedPipe);
					streamString.WriteString(Commands.Stop);
					Console.WriteLine($"*** [{Thread.CurrentThread.ManagedThreadId}]: Sent STOP command");
				}

				Console.WriteLine($"*** [{Thread.CurrentThread.ManagedThreadId}]: Starting to wait");
				_threadFinishedEvent.WaitOne(10 * TestInterval);
				Console.WriteLine($"*** [{Thread.CurrentThread.ManagedThreadId}]: End of OnDisposing");
			}

			protected override void OnSetupComplete()
			{
				_serverReadyEvent.Set();
			}

			protected override void OnAfterCommand(string command)
			{
				if (command == Commands.Stop)
				{
					_threadFinishedEvent.Set();
				}
				else
				{
					_serverReadyEvent.Set();
				}
			}

			private void ServerLoop(object _)
			{
				Console.WriteLine($"*** [{Thread.CurrentThread.ManagedThreadId}] start of ServerLoop");
				Console.WriteLine($"Creating pipe {NamedPipeName}");
				Run();
				Console.WriteLine($"*** [{Thread.CurrentThread.ManagedThreadId}] End of ServerLoop");
			}

			private void RunInternal()
			{
				ThreadPool.QueueUserWorkItem(ServerLoop);
				_serverReadyEvent.WaitOne(TestInterval);
			}

			public void WaitUntilServerProcessedChange()
			{
				_serverReadyEvent.WaitOne(TestInterval);
			}

			public static ServerForTesting Create()
			{
				var server = new ServerForTesting();
				server.RunInternal();
				return server;
			}
		}

		[OneTimeSetUp]
		public void SetupFixture()
		{
			Console.WriteLine("*** SetupFixture");
			ConfigurationForTests.Create();
		}

		[OneTimeTearDown]
		public void TeardDownFixture()
		{
			ConfigurationForTests.Delete();
		}

		[SetUp]
		public void Setup()
		{
			var baseDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
			Configuration.Instance.EncryptedDir = Path.Combine(baseDir, "Encrypted");
			Configuration.Instance.DecryptedDir = Path.Combine(baseDir, "Decrypted");
			Configuration.Instance.BackupDir = Path.Combine(baseDir, "Backup");

			Directory.CreateDirectory(Configuration.Instance.EncryptedDir);
			Directory.CreateDirectory(Configuration.Instance.DecryptedDir);

			var keyProvider = new KeyProvider();
			var passwordProvider = new ConstPasswordProvider();

			FileEncryptor.Instance.Setup(keyProvider, passwordProvider);
			FileDecryptor.Instance.Setup(keyProvider, passwordProvider);

			Server.NamedPipeName = Path.GetRandomFileName();
		}

		[TearDown]
		public void Teardown()
		{
			Directory.Delete(Path.GetDirectoryName(Configuration.Instance.EncryptedDir), true);
		}

		[Test]
		[Combinatorial]
		public void NewFileGetsAdded(
			[Values(Decrypted, Encrypted)] EncryptionDirection encrypted,
			[Values("A.txt", "B.bla.txt", "C", "D.enc")] string fileName)
		{
			using (var server = ServerForTesting.Create())
			{
				WriteFile(encrypted, fileName);

				server.WaitUntilServerProcessedChange();
				Assert.That(File.Exists(FileName(Not(encrypted), fileName)), Is.True);
			}
		}

		[TestCase(Encrypted)]
		[TestCase(Decrypted)]
		public void ExistingFileGetsAdded(EncryptionDirection encrypted)
		{
			WriteFile(encrypted);
			using (var server = ServerForTesting.Create())
			{
				Assert.That(File.Exists(FileName(Not(encrypted))), Is.True);
			}
		}

		[TestCase(Encrypted)]
		[TestCase(Decrypted)]
		public void DeletedFileGetsDeleted(EncryptionDirection encrypted)
		{
			WriteFile(encrypted);
			using (var server = ServerForTesting.Create())
			{
				Assert.That(File.Exists(FileName(encrypted)), Is.True);
				Assert.That(File.Exists(FileName(Not(encrypted))), Is.True);
				File.Delete(FileName(encrypted));
				server.WaitUntilServerProcessedChange();
				Assert.That(File.Exists(FileName(encrypted)), Is.False);
				Assert.That(File.Exists(FileName(Not(encrypted))), Is.False);
			}
		}

		[TestCase(Encrypted)]
		[TestCase(Decrypted)]
		public void RenamedFileGetsRenamed(EncryptionDirection encrypted)
		{
			WriteFile(encrypted);
			using (var server = ServerForTesting.Create())
			{
				Assert.That(File.Exists(FileName(encrypted)), Is.True);
				File.Move(FileName(encrypted), FileName(encrypted, "B.txt"));
				server.WaitUntilServerProcessedChange();
				Assert.That(File.Exists(FileName(Not(encrypted))), Is.False);
				Assert.That(File.Exists(FileName(Not(encrypted), "B.txt")), Is.True);
			}
		}

		[Test]
		public void ConflictGetsMarked()
		{
			WriteFile(Encrypted, "A.txt", "Encrypted text");
			WriteFile(Decrypted, "A.txt", "Decrypted text");
			using (var server = ServerForTesting.Create())
			{
				Assert.That(File.Exists(FileName(Encrypted, "A.txt")), Is.True);
				Assert.That(File.Exists(FileName(Encrypted, $"A (someone's conflicted copy {DateTime.Now:yyyy-MM-dd}).txt")), Is.True);
				Assert.That(File.Exists(FileName(Decrypted, "A.txt")), Is.True);
				Assert.That(File.Exists(FileName(Decrypted, $"A (someone's conflicted copy {DateTime.Now:yyyy-MM-dd}).txt")), Is.True);
			}
		}

		[Test]
		public void NoConflictIfSameContent()
		{
			WriteFile(Encrypted);
			WriteFile(Decrypted);
			using (var server = ServerForTesting.Create())
			{
				Assert.That(File.Exists(FileName(Encrypted, "A.txt")), Is.True);
				Assert.That(File.Exists(FileName(Encrypted, $"A (someone's conflicted copy {DateTime.Now:yyyy-MM-dd}).txt")), Is.False);
				Assert.That(File.Exists(FileName(Decrypted, "A.txt")), Is.True);
				Assert.That(File.Exists(FileName(Decrypted, $"A (someone's conflicted copy {DateTime.Now:yyyy-MM-dd}).txt")), Is.False);
			}
		}

		[TestCase(Encrypted)]
		[TestCase(Decrypted)]
		public void DeletedFileGetsDeletedAfterRestart(EncryptionDirection encrypted)
		{
			// Setup
			WriteFile(encrypted);
			var processFiles = new FileHandler();

			File.Delete(FileName(encrypted));

			// Execute/Verify
			using (var server = ServerForTesting.Create())
			{
				Assert.That(FileName(encrypted), Does.Not.Exist);
				Assert.That(FileName(Not(encrypted)), Does.Not.Exist);
			}
		}
	}
}