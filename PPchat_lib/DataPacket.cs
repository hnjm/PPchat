using System.Text;
using System.IO;

namespace PPchat_lib
{
	public class DataPacket : Packet
	{
		readonly byte[] data;

		public DataPacket(PacketDataType data_type, byte[] data)
			: base(data_type)
		{
			this.data = data;
		}
		public byte[] GetBytes()
		{
			return data;
		}
		public string GetString()
		{
			return Encoding.ASCII.GetString(data);
		}
		public Stream GetStream()
		{
			return new MemoryStream(data);
		}
		public override long Size()
		{
			return data.Length;
		}
	}
}
