using System;
using System.IO;
using System.Threading.Tasks;
using System.Buffers.Binary;
using System.Text;

namespace PPchat_lib
{
    public static class Serialization
    {
		public static async Task<bool> WriteStreamAsync(Stream stream, Stream s)
		{
			if (s.Length != 0)
			{
				try { await s.CopyToAsync(stream); }
				catch { return false; }
			}
			return true;
		}
		public static async Task<bool> WriteBytesAsync(Stream stream, byte[] buffer)
		{
			if (buffer.Length != 0)
			{
				try { await stream.WriteAsync(buffer, 0, buffer.Length); }
				catch { return false; }
			}
			return true;
		}
		public static async Task<bool> WriteInt64Async(Stream stream, long value)
		{
			var buffer = new byte[sizeof(long)];
			BinaryPrimitives.WriteInt64LittleEndian(new Span<byte>(buffer), value);
			return await WriteBytesAsync(stream, buffer);
		}
		public static async Task<bool> WriteSizeAndBytesAsync(Stream stream, byte[] buffer)
		{
			return	await WriteInt64Async(stream, buffer.Length) &&
					await WriteBytesAsync(stream, buffer);
		}
		public static async Task<bool> WriteSizeAndStreamAsync(Stream stream, Stream s)
		{
			return	await WriteInt64Async(stream, s.Length) &&
					await WriteStreamAsync(stream, s);
		}
		public static async Task<bool> ReadBytesAsync(Stream stream, byte[] buffer)
		{
			try
			{
				if (buffer.Length == 0 || await stream.ReadAsync(buffer, 0, buffer.Length) == buffer.Length)
					return true;
				else
					return false;
			}
			catch { return false; }
		}
		public static async Task<long?> ReadInt64Async(Stream stream)
		{
			var buffer = new byte[sizeof(long)];
			if (await ReadBytesAsync(stream, buffer))
				return BinaryPrimitives.ReadInt64LittleEndian(new ReadOnlySpan<byte>(buffer));
			else
				return null;
		}
		public static async Task<byte[]?> ReadSizeAndBytesAsync(Stream stream)
		{
			var size = await ReadInt64Async(stream);
			if (size != null)
			{
				var buffer = new byte[(long)size];
				if (await ReadBytesAsync(stream, buffer))
					return buffer;
			}

			return null;
		}
		public static async Task<bool> WriteStringAsync(Stream stream, string s)
		{
			return await WriteSizeAndBytesAsync(stream, Encoding.ASCII.GetBytes(s));
		}
		public static async Task<string?> ReadStringAsync(Stream stream)
		{
			var s = await ReadSizeAndBytesAsync(stream);
			if (s != null)
				return Encoding.ASCII.GetString(s);
			else
				return null;
		}
	}
}
