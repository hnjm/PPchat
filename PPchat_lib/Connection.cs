using System.IO;
using System.Threading.Tasks;
using System.Net.Sockets;

namespace PPchat_lib
{
	public abstract class Connection
	{
		TcpClient tcp_client;
		User? user;

		public User User { get => user!; protected set => user = value; }
		Stream? stream;
		public Stream Stream => stream!;
		public bool Connected => tcp_client.Connected;

		public Connection()
		{
			tcp_client = new TcpClient();
			user = null;
			stream = null;
		}
		public Connection(TcpClient tcp_client)
		{
			this.tcp_client = tcp_client;
			user = null;
			stream = tcp_client.GetStream();
		}

		protected void LogIn(User user)
		{
			User = user;
		}
		protected void Connect(TcpClient tcp_client)
		{
			this.tcp_client = tcp_client;
			stream = tcp_client.GetStream();
		}
		protected void Disconnect()
		{
			stream!.Close();
			tcp_client.Close();
		}

		public async Task<bool> SendMessage(string message)
		{
			return await Packet.SendStringAsync(Stream, PacketDataType.Message, message);
		}
		public async Task<Packet?> GetPacket()
		{
			return await Packet.ReadAsync(Stream);
		}
		public async Task<DataPacket?> GetDataPacket()
		{
			return (DataPacket?)await GetPacket();
		}
	}
}
