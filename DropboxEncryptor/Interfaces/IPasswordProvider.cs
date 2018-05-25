using System.Security;

namespace DropboxEncryptor.Interfaces
{
	public interface IPasswordProvider
	{
		SecureString Password { get; }
	}
}