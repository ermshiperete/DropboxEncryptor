using System.IO;
using System.Security;
using System.Security.Cryptography;
using DropboxEncryptor.Interfaces;

namespace DropboxEncryptor
{
	public class FileBaseCryptor
	{
		public const string EncodingExtension = ".enc";

		protected IKeyProvider KeyProvider { get; set; }
		protected IPasswordProvider PasswordProvider { get; set; }

		public void Setup(IKeyProvider keyProvider, IPasswordProvider passwordProvider)
		{
			KeyProvider = keyProvider;
			PasswordProvider = passwordProvider;
		}

		protected byte[] EncryptedPassword
		{
			get
			{
				using (var aesAlg = new AesCryptoServiceProvider())
				{
					aesAlg.Key = KeyProvider.Key;
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
								swEncrypt.Write(PasswordProvider.Password);
							}
							return msEncrypt.ToArray();
						}
					}
				}
			}
		}
	}
}