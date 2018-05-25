using System;
using System.IO;
using System.Security.Cryptography;
using DropboxEncryptor.Utils;

namespace DropboxEncryptor
{
	public class FileDecryptor : FileBaseCryptor
	{
		static FileDecryptor()
		{
			Instance = new FileDecryptor();
		}

		public static FileDecryptor Instance { get; }

		public static string GetDecryptedFilePath(string encryptedFilePath)
		{
			return Path.Combine(Configuration.Instance.DecryptedDir,
				Path.GetFileNameWithoutExtension(encryptedFilePath));
		}

		public void DecryptFile(string encryptedFilePath)
		{
			DecryptFile(encryptedFilePath, GetDecryptedFilePath(encryptedFilePath));
		}

		public void DecryptFile(string encryptedFilePath, string decryptedFilePath)
		{
			using (var aesAlg = new AesCryptoServiceProvider())
			{
				aesAlg.Key = EncryptedPassword;
				aesAlg.IV = KeyProvider.IV;
				//aesAlg.Padding = PaddingMode.ISO10126;

				// Create a decrytor to perform the stream transform.
				var decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

				// Create the streams used for decryption.
				using (var msDecrypt = new MemoryStream(File.ReadAllBytes(encryptedFilePath)))
				{
					using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
					{
						using (var srDecrypt = new StreamReader(csDecrypt))
						{
							// Read the decrypted bytes from the decrypting stream
							// and place them in a string.
							File.WriteAllText(decryptedFilePath, srDecrypt.ReadToEnd());
						}
					}
				}
			}

			RetryIfLocked.Do(() => File.SetLastWriteTimeUtc(decryptedFilePath,
				new FileInfo(encryptedFilePath).LastWriteTimeUtc), TimeSpan.FromSeconds(1), 10);
		}
	}
}