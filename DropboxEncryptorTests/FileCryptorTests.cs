using System.IO;
using DropboxEncryptor;
using NUnit.Framework;

namespace DropboxEncryptorTests
{
	[TestFixture]
	public class FileCryptorTests
	{
		private string _decryptedSource;
		private string _encrypted;
		private string _decrypted;

		[OneTimeSetUp]
		public void SetupFixture()
		{
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

			Directory.CreateDirectory(Configuration.Instance.EncryptedDir);
			Directory.CreateDirectory(Configuration.Instance.DecryptedDir);

			var keyProvider = new KeyProvider();
			var passwordProvider = new ConstPasswordProvider();

			FileEncryptor.Instance.Setup(keyProvider, passwordProvider);
			FileDecryptor.Instance.Setup(keyProvider, passwordProvider);
		}

		[TearDown]
		public void TearDown()
		{
			File.Delete(_decrypted);
			File.Delete(_encrypted);
			File.Delete(_decryptedSource);
		}

		[Test]
		public void EncryptDecrypt()
		{
			const string text = "This is a test";
			_decryptedSource = Path.GetTempFileName();
			_encrypted = Path.GetTempFileName();
			_decrypted = Path.GetTempFileName();
			File.WriteAllText(_decryptedSource, text);
			var sourceInfo = new FileInfo(_decryptedSource);

			FileEncryptor.Instance.EncryptFile(_decryptedSource, _encrypted);
			Assert.That(new FileInfo(_encrypted).LastWriteTimeUtc, Is.EqualTo(sourceInfo.LastWriteTimeUtc));

			FileDecryptor.Instance.DecryptFile(_encrypted, _decrypted);
			Assert.That(new FileInfo(_decrypted).LastWriteTimeUtc, Is.EqualTo(sourceInfo.LastWriteTimeUtc));

			Assert.That(File.ReadAllText(_decrypted), Is.EqualTo(text));
		}
	}
}