using System;
using System.IO;
using System.Security.Cryptography;
using DropboxEncryptor.Utils;

namespace DropboxEncryptor
{
	public class FileEncryptor: FileBaseCryptor
	{
		static FileEncryptor()
		{
			Instance = new FileEncryptor();
		}

		public static FileEncryptor Instance { get; }

		public static string GetEncryptedFilePath(string decryptedFilePath)
		{
			return Path.Combine(Configuration.Instance.EncryptedDir, Path.GetFileName(decryptedFilePath) + EncodingExtension);
		}

		public void EncryptFile(string decryptedFilePath)
		{
			EncryptFile(decryptedFilePath, GetEncryptedFilePath(decryptedFilePath));
		}

		public void EncryptFile(string decryptedFilePath, string encryptedFilePath)
		{
			using (var aesAlg = new AesCryptoServiceProvider())
			{
				aesAlg.Key = EncryptedPassword;
				aesAlg.IV = KeyProvider.IV;

				// Create a decrytor to perform the stream transform.
				var encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

				// Create the streams used for encryption.
				using (var msEncrypt = new MemoryStream())
				{
					using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
					{
						using (var swEncrypt = new StreamWriter(csEncrypt))
						{
							//Write all data to the stream.
							swEncrypt.Write(File.ReadAllText(decryptedFilePath));
						}
						File.WriteAllBytes(encryptedFilePath, msEncrypt.ToArray());
					}
				}
			}

			RetryIfLocked.Do(() => File.SetLastWriteTimeUtc(encryptedFilePath,
				new FileInfo(decryptedFilePath).LastWriteTimeUtc), TimeSpan.FromSeconds(1), 10);
		}
	}
}