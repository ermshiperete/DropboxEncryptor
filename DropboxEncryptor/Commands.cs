using System;
using Ardalis.SmartEnum;

namespace DropboxEncryptor
{
	public class Commands: SmartEnum<Commands, int>
	{
		public const string Stop = "stop";
		public const string Status = "status";
		public const string EncryptedFileChanged = "encChanged";
		public const string DecryptedFileChanged = "decChanged";

		public static readonly Commands StopCmd = new Commands(Stop, 1);
		public static readonly Commands StatusCmd = new Commands(Status, 2);
		public static readonly Commands EncryptedFileChangedCmd = new Commands(EncryptedFileChanged, 3);
		public static readonly Commands DecryptedFileChangedCmd = new Commands(DecryptedFileChanged, 4);

		private Commands(string name, int value): base(name, value)
		{
		}
	}
}