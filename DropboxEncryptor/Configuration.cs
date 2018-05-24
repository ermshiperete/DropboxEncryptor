using System;
using System.IO;
using System.Text;
using IniParser;
using IniParser.Model;

namespace DropboxEncryptor
{
	public class Configuration
	{
		public static Configuration Instance { get; private set; }

		public static bool Load()
		{
			Instance = new Configuration();
			return File.Exists(ConfigFile);
		}

		private Configuration()
		{
			if (string.IsNullOrEmpty(ConfigFile))
			{
				ConfigFile = Path.Combine(
					Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
					"DropboxEncryptor", "config.ini");
			}

			if (!File.Exists(ConfigFile))
			{
				CreateBackups = true;
				BackupDir = DefaultBackupDir;
				EncryptedDir = DefaultEncryptedDir;
				DecryptedDir = DefaultDecryptedDir;
				Directory.CreateDirectory(BackupDir);
				Directory.CreateDirectory(EncryptedDir);
				Directory.CreateDirectory(DecryptedDir);
				return;
			}
			var parser = new FileIniDataParser();
			var data = parser.ReadFile(ConfigFile);

			var createBackups = data["Config"]["CreateBackups"];
			CreateBackups = string.IsNullOrEmpty(createBackups) || createBackups.ToLowerInvariant() == "true";

			BackupDir = data["Config"]["BackupDir"];
			if (string.IsNullOrEmpty(BackupDir))
				BackupDir = DefaultBackupDir;

			EncryptedDir = data["Config"]["EncryptedDir"];
			if (string.IsNullOrEmpty(EncryptedDir))
				EncryptedDir = DefaultEncryptedDir;

			DecryptedDir = data["Config"]["DecryptedDir"];
			if (string.IsNullOrEmpty(DecryptedDir))
				DecryptedDir = DefaultDecryptedDir;

			Directory.CreateDirectory(BackupDir);
			Directory.CreateDirectory(EncryptedDir);
			Directory.CreateDirectory(DecryptedDir);
		}

		public void Save()
		{
			var iniData = new IniData();
			var configData = iniData["Config"];
			configData["BackupDir"] = BackupDir;
			configData["CreateBackups"] = CreateBackups.ToString();
			configData["EncryptedDir"] = EncryptedDir;
			configData["DecryptedDir"] = DecryptedDir;
			var parser = new FileIniDataParser();
			Directory.CreateDirectory(Path.GetDirectoryName(ConfigFile));
			parser.WriteFile(ConfigFile, iniData, Encoding.UTF8);
		}

		private static string DefaultDecryptedDir => Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
			"Documents", "DecryptedData");

		private static string DefaultEncryptedDir => Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.Personal),
			"Dropbox", "EncryptedData");

		private static string DefaultBackupDir => Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
			"Documents", "DropboxEncryptorBackup");

		public static string ConfigFile { private get; set; }

		public string EncryptedDir { get; set; }
		public string DecryptedDir { get; set; }
		public string BackupDir { get; set; }

		public bool CreateBackups { get; set; }
	}
}