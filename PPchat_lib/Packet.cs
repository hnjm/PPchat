using System.IO;
using System.Text;
using System.Threading.Tasks;
using static PPchat_lib.Serialization;

namespace PPchat_lib
{
	public enum PacketDataType : long
	{
		AckEnd,
		ListMyUploads,
		ListAllUploads,
		
		File,
		Message,
		ListUploads,
		FileName,
		Username,
		UserFiles,
		Download,
		DownloadNoUsername,
		RemoveUpload,
		LogIn,
		Password,
		ChangePassword,
		End,
	}
	public enum PacketType : long
	{
		Empty,
		Data,
	}

	public class Packet
	{
		PacketDataType data_type;

		public Packet(PacketDataType data_type)
		{
			this.data_type = data_type;
		}

		public PacketDataType DataType()
		{
			return data_type;
		}
		public virtual long Size()
		{
			return 0;
		}
		static bool EmptyDataType(PacketDataType data_type)
		{
			return data_type < PacketDataType.File;
		}
		public static async Task<Packet?> ReadAsync(Stream stream)
		{
			var x = await ReadInt64Async(stream);
			if (x != null)
			{
				var data_type = (PacketDataType)x;
				var empty = EmptyDataType(data_type);
				if (empty)
					return new Packet(data_type);
				else
				{
					var data = await ReadSizeAndBytesAsync(stream);
					if (data != null)
						return new DataPacket(data_type, data);
				}
			}
			return null;
		}

		public static async Task<bool> SendEmptyAsync(Stream stream, PacketDataType data_type)
		{
			return await WriteInt64Async(stream, (long)data_type);
		}
		public static async Task<bool> SendBytesAsync(Stream stream, PacketDataType data_type, byte[] bytes)
		{
			return await WriteInt64Async(stream, (long)data_type) &&
					await WriteSizeAndBytesAsync(stream, bytes);
		}
		public static async Task<bool> SendStringAsync(Stream stream, PacketDataType data_type, string s)
		{
			return	await WriteInt64Async(stream, (long)data_type) &&
					await WriteSizeAndBytesAsync(stream, Encoding.ASCII.GetBytes(s));
		}
		public static async Task<bool> SendStreamAsync(Stream stream, PacketDataType data_type, Stream s)
		{
			return	await WriteInt64Async(stream, (long)data_type) &&
					await WriteSizeAndStreamAsync(stream, s);
		}
	}
}
