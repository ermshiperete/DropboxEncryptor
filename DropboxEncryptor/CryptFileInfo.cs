using System;
using System.IO;

namespace DropboxEncryptor
{
	public class CryptFileInfo
	{
		// ReSharper disable once UnusedMember.Global
		public CryptFileInfo()
		{
		}

		public CryptFileInfo(FileSystemInfo fileInfo)
		{
			Name = fileInfo.Name;
			LastWriteTimeUtc = fileInfo.LastWriteTimeUtc;
		}

		private string Name { get; }
		public DateTime LastWriteTimeUtc { get; }
	}
}
