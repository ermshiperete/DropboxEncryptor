using System.IO;
using DropboxEncryptor;
using NUnit.Framework;

namespace DropboxEncryptorTests
{
	[TestFixture]
	public class FileChangedDataObjectTests
	{
		[TestCase("/tmp")]
		[TestCase("/tmp/")]
		public void Equals_Same(string directory)
		{
			var other = new FileChangedDataObject(WatcherChangeTypes.Created, directory, "A.txt");

			var sut = new FileChangedDataObject(WatcherChangeTypes.Created, "/tmp", "A.txt");

			Assert.That(sut.Equals(other), Is.True);
		}

		[Test]
		public void Equals_Identical()
		{
			var sut = new FileChangedDataObject(WatcherChangeTypes.Created, "/tmp", "A.txt");

			Assert.That(sut.Equals(sut), Is.True);
		}

		[Test]
		public void Equals_Null()
		{
			var sut = new FileChangedDataObject(WatcherChangeTypes.Created, "/tmp", "A.txt");

			Assert.That(sut.Equals(null), Is.False);
		}

		[Test]
		public void Equals_DifferentFile()
		{
			var other = new FileChangedDataObject(WatcherChangeTypes.Created, "/tmp", "A.txt");

			var sut = new FileChangedDataObject(WatcherChangeTypes.Created, "/tmp", "B.txt");

			Assert.That(sut.Equals(other), Is.False);
		}

		[Test]
		public void Equals_DifferentChangeType()
		{
			var other = new FileChangedDataObject(WatcherChangeTypes.Changed, "/tmp", "A.txt");

			var sut = new FileChangedDataObject(WatcherChangeTypes.Created, "/tmp", "A.txt");

			Assert.That(sut.Equals(other), Is.False);
		}

		[Test]
		public void Equals_DifferentDirectory()
		{
			var other = new FileChangedDataObject(WatcherChangeTypes.Changed, "/tmp2", "A.txt");

			var sut = new FileChangedDataObject(WatcherChangeTypes.Changed, "/tmp", "A.txt");

			Assert.That(sut.Equals(other), Is.False);
		}

		[Test]
		public void Equals_DifferentOldDirFile()
		{
			var other = new FileChangedDataObject(WatcherChangeTypes.Changed, "/tmp", "A.txt",
				"/tmp", "oldfile");

			var sut = new FileChangedDataObject(WatcherChangeTypes.Created, "/tmp", "A.txt");

			Assert.That(sut.Equals(other), Is.False);
		}

		[Test]
		public void Equals_WithCommandSame()
		{
			var other = new FileChangedDataObject(Commands.EncryptedFileChangedCmd,
				new FileSystemEventArgs(WatcherChangeTypes.Created, "/tmp", "A.txt"));

			var sut = new FileChangedDataObject(Commands.EncryptedFileChangedCmd,
				new FileSystemEventArgs(WatcherChangeTypes.Created, "/tmp", "A.txt"));

			Assert.That(sut.Equals(other), Is.True);
		}

		[Test]
		public void Equals_WithCommandDifferentCommand()
		{
			var other = new FileChangedDataObject(Commands.EncryptedFileChangedCmd,
				new FileSystemEventArgs(WatcherChangeTypes.Created, "/tmp", "A.txt"));

			var sut = new FileChangedDataObject(Commands.DecryptedFileChangedCmd,
				new FileSystemEventArgs(WatcherChangeTypes.Created, "/tmp", "A.txt"));

			Assert.That(sut.Equals(other), Is.False);
		}

		[Test]
		public void Equals_WithCommandDifferentChangeType()
		{
			var other = new FileChangedDataObject(Commands.EncryptedFileChangedCmd,
				new FileSystemEventArgs(WatcherChangeTypes.Changed, "/tmp", "A.txt"));

			var sut = new FileChangedDataObject(Commands.DecryptedFileChangedCmd,
				new FileSystemEventArgs(WatcherChangeTypes.Created, "/tmp", "A.txt"));

			Assert.That(sut.Equals(other), Is.False);
		}

		[Test]
		public void Equals_WithCommandDifferentDir()
		{
			var other = new FileChangedDataObject(Commands.EncryptedFileChangedCmd,
				new FileSystemEventArgs(WatcherChangeTypes.Created, "/tmp2", "A.txt"));

			var sut = new FileChangedDataObject(Commands.DecryptedFileChangedCmd,
				new FileSystemEventArgs(WatcherChangeTypes.Created, "/tmp", "A.txt"));

			Assert.That(sut.Equals(other), Is.False);
		}

		[Test]
		public void Equals_WithCommandDifferentFile()
		{
			var other = new FileChangedDataObject(Commands.EncryptedFileChangedCmd,
				new FileSystemEventArgs(WatcherChangeTypes.Created, "/tmp", "B.txt"));

			var sut = new FileChangedDataObject(Commands.DecryptedFileChangedCmd,
				new FileSystemEventArgs(WatcherChangeTypes.Created, "/tmp", "A.txt"));

			Assert.That(sut.Equals(other), Is.False);
		}

		[Test]
		public void Equals_WithCommandDifferentOldDir()
		{
			var other = new FileChangedDataObject(Commands.EncryptedFileChangedCmd,
				new RenamedEventArgs(WatcherChangeTypes.Created, "/tmp", "A.txt", "B.txt"));

			var sut = new FileChangedDataObject(Commands.DecryptedFileChangedCmd,
				new FileSystemEventArgs(WatcherChangeTypes.Created, "/tmp", "A.txt"));

			Assert.That(sut.Equals(other), Is.False);
		}

		[Test]
		public void Equals_MixCommandAndNonCommand()
		{
			var other = new FileChangedDataObject(Commands.EncryptedFileChangedCmd,
				new FileSystemEventArgs(WatcherChangeTypes.Created, "/tmp", "A.txt"));

			var sut = new FileChangedDataObject(WatcherChangeTypes.Created, "/tmp", "A.txt");

			Assert.That(sut.Equals(other), Is.False);
		}

		[Test]
		public void ToString_FileSystemEventArgs()
		{
			var sut = new FileChangedDataObject(Commands.DecryptedFileChangedCmd,
				new FileSystemEventArgs(WatcherChangeTypes.Created, "/tmp", "A.txt"));

			Assert.That(sut.ToString(), Is.EqualTo("[decChanged] Created /tmp/A.txt"));
		}

		[Test]
		public void ToString_RenamedEventArgs()
		{
			var sut = new FileChangedDataObject(Commands.DecryptedFileChangedCmd,
				new RenamedEventArgs(WatcherChangeTypes.Renamed, "/tmp", "A.txt", "B.txt"));

			Assert.That(sut.ToString(), Is.EqualTo("[decChanged] Renamed /tmp/A.txt from /tmp/B.txt"));
		}
	}
}