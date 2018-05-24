using System.Diagnostics;
using System.IO;
using DropboxEncryptor;
using NUnit.Framework;
using SIL.IO;

namespace DropboxEncryptorTests
{
	[TestFixture(TestOf = typeof(Backup))]
	public class BackupTests
	{
		private string _tmpDir;

		private static string RunCommand(string cmd, string args = null)
		{
			var proc = new Process {
				EnableRaisingEvents = false,
				StartInfo = {
					FileName = cmd,
					Arguments = args,
					UseShellExecute = false,
					RedirectStandardOutput = true,
					WorkingDirectory = Configuration.Instance.BackupDir
				}
			};
			proc.Start();
			proc.WaitForExit();
			var output = proc.StandardOutput.ReadToEnd();
			return output.Trim();
		}

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
			Configuration.Instance.BackupDir = Path.Combine(baseDir, "backup");
			_tmpDir = Path.Combine(baseDir, "tmp");
			Directory.CreateDirectory(_tmpDir);

			File.WriteAllText(Path.Combine(_tmpDir, "A.txt"), "Hello world!");
		}

		[TearDown]
		public void TearDown()
		{
			RobustIO.DeleteDirectory(Path.GetDirectoryName(Configuration.Instance.BackupDir), true);
		}

		[Test]
		public void CreateBackupsFalse_DoesntInitializeBackupDir()
		{
			Configuration.Instance.CreateBackups = false;

			Backup.CreateBackup(new FileChangedDataObject(WatcherChangeTypes.Created, _tmpDir, "A.txt"));

			Assert.That(Configuration.Instance.BackupDir, Does.Not.Exist);
		}

		[Test]
		public void CreateBackupsTrue_DoesInitializeBackupDir()
		{
			Configuration.Instance.CreateBackups = true;

			Backup.CreateBackup(new FileChangedDataObject(WatcherChangeTypes.Created, _tmpDir, "A.txt"));

			Assert.That(Configuration.Instance.BackupDir, Does.Exist);
			Assert.That(Path.Combine(Configuration.Instance.BackupDir, ".git"), Does.Exist);
		}

		[TestCase(WatcherChangeTypes.Created)]
		[TestCase(WatcherChangeTypes.Changed)]
		public void Backup_CreatedOrChangedFile(WatcherChangeTypes changeType)
		{
			Backup.CreateBackup(new FileChangedDataObject(changeType, _tmpDir, "A.txt"));

			Assert.That(Path.Combine(Configuration.Instance.BackupDir, "A.txt"), Does.Exist);
		}

		[Test]
		public void Backup_DeleteFile()
		{
			// Setup
			Directory.CreateDirectory(Configuration.Instance.BackupDir);
			var testFile = Path.Combine(Configuration.Instance.BackupDir, "A.txt");
			File.WriteAllText(testFile, "Delete file test");
			RunCommand("git", "init .");
			RunCommand("git", "add A.txt");
			RunCommand("git", "commit -m \"Add A.txt\"");

			// SUT
			Backup.CreateBackup(new FileChangedDataObject(WatcherChangeTypes.Deleted, _tmpDir, "A.txt"));

			// Verify
			Assert.That(testFile, Does.Not.Exist);
		}

		[Test]
		public void Backup_RenameFile()
		{
			// Setup
			Directory.CreateDirectory(Configuration.Instance.BackupDir);
			var testFile = Path.Combine(Configuration.Instance.BackupDir, "A.txt");
			File.WriteAllText(testFile, "Rename file test");
			RunCommand("git", "init .");
			RunCommand("git", "add A.txt");
			RunCommand("git", "commit -m \"Add A.txt\"");

			// SUT
			Backup.CreateBackup(new FileChangedDataObject(WatcherChangeTypes.Renamed, _tmpDir,
				"B.txt", _tmpDir, "A.txt"));

			// Verify
			Assert.That(testFile, Does.Not.Exist);
			Assert.That(Path.Combine(Configuration.Instance.BackupDir, "B.txt"), Does.Exist);
		}
	}
}