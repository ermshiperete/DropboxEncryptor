
using System;
using System.IO;
using System.Threading;
using DropboxEncryptor;
using NUnit.Framework;

namespace DropboxEncryptorTests
{
	[TestFixture(TestOf = typeof(FileHandler))]
	public class FileHandlerTests
	{
		private Configuration _config;
		private const int TestInterval = 100;

		public enum EncryptionDirection
		{
			Encrypted,
			Decrypted
		};

		private const EncryptionDirection Encrypted = EncryptionDirection.Encrypted;
		private const EncryptionDirection Decrypted = EncryptionDirection.Decrypted;
		
		private static EncryptionDirection Not(EncryptionDirection dir)
		{
			return dir == Encrypted ? Decrypted : Encrypted;
		}

		private string FileName(EncryptionDirection encrypted, string filename = "A.txt")
		{
			return encrypted == Encrypted
				? Path.Combine(_config.EncryptedDir, filename + ".enc")
				: Path.Combine(_config.DecryptedDir, filename);
		}

		[SetUp]
		public void Setup()
		{
			FileHandler.TimerInterval = TestInterval;

			var baseDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
			_config = new Configuration {
				EncryptedDir = Path.Combine(baseDir, "Encrypted"),
				DecryptedDir = Path.Combine(baseDir, "Decrypted")
			};

			Directory.CreateDirectory(_config.EncryptedDir);
			Directory.CreateDirectory(_config.DecryptedDir);
		}

		[TearDown]
		public void Teardown()
		{
			Directory.Delete(Path.GetDirectoryName(_config.EncryptedDir), true);
		}

		[Test]
		[Combinatorial]
		public void NewFileGetsAdded(
			[Values(Decrypted, Encrypted)] EncryptionDirection encrypted,
			[Values("A.txt", "B.bla.txt", "C", "D.enc")] string fileName)
		{
			using (var sut = new FileHandler(_config))
			{
				File.WriteAllText(FileName(encrypted, fileName), "This is a test");

				Thread.Sleep(2 * TestInterval);
				Assert.That(File.Exists(FileName(Not(encrypted), fileName)), Is.True);
			}
		}

		[TestCase(Encrypted)]
		[TestCase(Decrypted)]
		public void ExistingFileGetsAdded(EncryptionDirection encrypted)
		{
			File.WriteAllText(FileName(encrypted), "This is a test");
			using (var sut = new FileHandler(_config))
			{
				Thread.Sleep(2 * TestInterval);
				Assert.That(File.Exists(FileName(Not(encrypted))), Is.True);
			}
		}

		[TestCase(Encrypted)]
		[TestCase(Decrypted)]
		public void DeletedFileGetsDeleted(EncryptionDirection encrypted)
		{
			File.WriteAllText(FileName(encrypted), "This is a test");
			using (var sut = new FileHandler(_config))
			{
				Assert.That(File.Exists(FileName(encrypted)), Is.True);
				File.Delete(FileName(encrypted));
				Thread.Sleep(2 * TestInterval);
				Assert.That(File.Exists(FileName(Not(encrypted))), Is.False);
			}
		}
	}
}