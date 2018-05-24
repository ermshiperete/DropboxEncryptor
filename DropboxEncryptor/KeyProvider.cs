using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace DropboxEncryptor
{
	public class KeyProvider: IKeyProvider
	{
		public static string KeyFileName => ".secure.config" + FileBaseCryptor.EncodingExtension;

		public KeyProvider()
		{
			var keyFile = Path.Combine(Configuration.Instance.EncryptedDir, KeyFileName);
			if (File.Exists(keyFile))
			{
				var allBytes = File.ReadAllBytes(keyFile);
				if (allBytes.Length < 48)
					throw new BadImageFormatException();

				var key = new byte[32];
				var iv = new byte[16];
				Array.Copy(allBytes, key, 32);
				Array.Copy(allBytes, 32, iv, 0, 16);
				Key = key;
				IV = iv;
			}
			else
			{
				using (var aesProvider = new AesCryptoServiceProvider())
				{
					Key = aesProvider.Key;
					IV = aesProvider.IV;

					var allBytes = new List<byte>();
					allBytes.AddRange(Key);
					allBytes.AddRange(IV);

					File.WriteAllBytes(keyFile, allBytes.ToArray());
				}
			}
		}

		public byte[] Key { get; }
		public byte[] IV { get; }
	}
}