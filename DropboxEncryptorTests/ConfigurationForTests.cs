using System.IO;
using System.IO.Pipes;
using System.Text;
using DropboxEncryptor;
using IniParser;
using IniParser.Model;

namespace DropboxEncryptorTests
{
	public static class ConfigurationForTests
	{
		private static string _tempDir;

		public static void Create()
		{
			Server.NamedPipeName = Path.GetRandomFileName();
			_tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
			Directory.CreateDirectory(_tempDir);

			var configFile = Path.Combine(_tempDir, "DropboxEncryptor", "config.ini");
			var iniData = new IniData();
			var configData = iniData["Config"];
			configData["BackupDir"] = Path.Combine(_tempDir, "Documents", "DropboxEncryptorBackup");
			configData["CreateBackups"] = "true";
			configData["EncryptedDir"] = Path.Combine(_tempDir, "Dropbox", "EncryptedData");
			configData["DecryptedDir"] = Path.Combine(_tempDir, "Documents", "DecryptedData");
			var parser = new FileIniDataParser();
			Directory.CreateDirectory(Path.GetDirectoryName(configFile));
			Directory.CreateDirectory(configData["BackupDir"]);
			Directory.CreateDirectory(configData["EncryptedDir"]);
			Directory.CreateDirectory(configData["DecryptedDir"]);
			System.Console.WriteLine($"*** [{System.Threading.Thread.CurrentThread.ManagedThreadId}] Create directory {configData["BackupDir"]}");
			System.Console.WriteLine($"*** [{System.Threading.Thread.CurrentThread.ManagedThreadId}] Create directory {configData["EncryptedDir"]}");
			System.Console.WriteLine($"*** [{System.Threading.Thread.CurrentThread.ManagedThreadId}] Create directory {configData["DecryptedDir"]}");

			parser.WriteFile(configFile, iniData, Encoding.UTF8);
			Configuration.ConfigFile = configFile;
			Configuration.Load();
		}

		public static void Delete()
		{
			Directory.Delete(_tempDir, true);
		}
	}
}