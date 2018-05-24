namespace DropboxEncryptor
{
	public interface IKeyProvider
	{
		byte[] Key { get; }
		byte[] IV { get; }
	}
}