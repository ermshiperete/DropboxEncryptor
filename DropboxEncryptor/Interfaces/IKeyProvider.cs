namespace DropboxEncryptor.Interfaces
{
	public interface IKeyProvider
	{
		byte[] Key { get; }
		byte[] IV { get; }
	}
}