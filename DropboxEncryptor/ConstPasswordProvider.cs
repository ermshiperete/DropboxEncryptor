using System.Security;
using System.Text;
using DropboxEncryptor.Interfaces;

namespace DropboxEncryptor
{
	public class ConstPasswordProvider: IPasswordProvider
	{
		public SecureString Password
		{
			get
			{
				var str = new SecureString();
				foreach (var c in Encoding.UTF8.GetChars(Encoding.UTF8.GetBytes("Hello World!")))
					str.AppendChar(c);
				return str;
			}
		}
	}
}