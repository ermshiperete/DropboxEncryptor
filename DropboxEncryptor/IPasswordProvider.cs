using System.Security;

namespace DropboxEncryptor
{
	public interface IPasswordProvider
	{
		SecureString Password { get; }
	}
}