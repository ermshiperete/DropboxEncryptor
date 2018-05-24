using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace DropboxEncryptor
{
	public class StreamHelper
	{
		private readonly Stream          _ioStream;
		private readonly UnicodeEncoding _streamEncoding;

		public StreamHelper(Stream ioStream)
		{
			_ioStream = ioStream;
			_streamEncoding = new UnicodeEncoding();
		}

		private byte[] ReadBytes(out int len)
		{
			len = 1;
			try
			{
				len = _ioStream.ReadByte() * 256;
				len += _ioStream.ReadByte();
				if (len < 0)
				{
					Console.WriteLine("End of pipe stream!");
					Debug.WriteLine("End of pipe stream!");
					throw new EndOfStreamException();
				}

				Console.WriteLine($"Trying to read 2 + {len} bytes from named pipe");
				Debug.WriteLine($"Trying to read 2 + {len} bytes from named pipe");
				var inBuffer = new byte[len];
				len = _ioStream.Read(inBuffer, 0, len);
				//Console.WriteLine($"Actually read {len} bytes from named pipe");
				return inBuffer;
			}
			catch (OverflowException e)
			{
				Console.WriteLine($"Got OverflowException; Trying to create buffer of {len} bytes");
				Debug.WriteLine($"Got OverflowException; Trying to create buffer of {len} bytes");
				return new byte[1];
			}
		}

		public string ReadString()
		{
			var inBuffer = ReadBytes(out _);
			var str = _streamEncoding.GetString(inBuffer);
			Console.WriteLine($"*** [{System.Threading.Thread.CurrentThread.ManagedThreadId}]: Read {str} from pipe");
			Debug.WriteLine($"*** [{System.Threading.Thread.CurrentThread.ManagedThreadId}]: Read {str} from pipe");
			return str;
		}

		public T ReadBinary<T>()
		{
			var buffer = ReadBytes(out var len);
			if (len < 0)
				return default(T);
			using (var memoryStream = new MemoryStream())
			{
				memoryStream.Write(buffer, 0, len);
				memoryStream.Position = 0;
				var formatter = new BinaryFormatter();
				return (T)formatter.Deserialize(memoryStream);
			}
		}

		private int WriteBytes(byte[] buffer)
		{
			var len = buffer.Length;
			if (len > ushort.MaxValue)
			{
				len = ushort.MaxValue;
			}

			_ioStream.WriteByte((byte) (len / 256));
			_ioStream.WriteByte((byte) (len & 255));
			_ioStream.Write(buffer, 0, len);
			_ioStream.Flush();
			//Console.WriteLine($"Wrote 2 + {len} bytes to named pipe");

			return buffer.Length + 2;
		}

		public int WriteString(string outString)
		{
			Console.WriteLine($"*** [{System.Threading.Thread.CurrentThread.ManagedThreadId}]: Wrote {outString} to pipe");
			Debug.WriteLine($"*** [{System.Threading.Thread.CurrentThread.ManagedThreadId}]: Wrote {outString} to pipe");
			var outBuffer = _streamEncoding.GetBytes(outString);
			return WriteBytes(outBuffer);
		}

		public int WriteBinary<T>(T obj)
		{
			using (var memoryStream = new MemoryStream())
			{
				var formatter = new BinaryFormatter();
				formatter.Serialize(memoryStream, obj);
				return WriteBytes(memoryStream.GetBuffer());
			}
		}
	}
}