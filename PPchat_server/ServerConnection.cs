using System.Net.Sockets;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PPchat_lib;

namespace PPchat_server
{
	public class ServerConnection : Connection
	{
		readonly Server server;

		public ServerConnection(TcpClient tcp_client, Server server)
			: base(tcp_client)
		{
			this.server = server;
		}

		async Task Log(string message)
		{
			await server.Log(message);
		}

		public async Task Handle()
		{
			Packet? packet;

			while (true)
			{
				packet = await GetPacket();
				if (packet == null)
				{
					await HandleNullPacket();
					break;
				}
				var type = packet.DataType();
				if (type == PacketDataType.LogIn)
					await HandleLogIn((DataPacket)packet);
				else if (type == PacketDataType.Message)
					await HandleMessage((DataPacket)packet);
				else if (type == PacketDataType.FileName)
					await HandleUpload((DataPacket)packet);
				else if (type == PacketDataType.ListUploads)
					await HandleListUploads((DataPacket)packet);
				else if (type == PacketDataType.ListMyUploads)
					await HandleListMyUploads();
				else if (type == PacketDataType.ListAllUploads)
					await HandleListAllUploads();
				else if (type == PacketDataType.Download)
					await HandleDownload((DataPacket)packet);
				else if (type == PacketDataType.DownloadNoUsername)
					await HandleDownloadNoUsername((DataPacket)packet);
				else if (type == PacketDataType.RemoveUpload)
					await HandleRemoveUpload((DataPacket)packet);
				else if (type == PacketDataType.ChangePassword)
					await HandleChangePassword((DataPacket)packet);
				else if (type == PacketDataType.End)
				{
					await HandleEnd();
					break;
				}
				else if (type == PacketDataType.AckEnd)
					break;
			}

			server.RemoveConnection(this);
			Disconnect();
		}

		async Task HandleNullPacket()
		{
			await Log($"client {User?.Name ?? "**unknown**"} disconnected unexpectedly");
		}

		async Task HandleEnd()
		{
			await Packet.SendEmptyAsync(Stream, PacketDataType.AckEnd);
			await Log($"client {User.Name} disconnected");
		}

		async Task HandleMessage(DataPacket packet)
		{
			string message = packet.GetString();

			await Log($"{User.Name} said: {message}");

			foreach (var c in server.ConnectionsExcept(this))
				await c.SendMessage($"{User.Name}: {message}");
		}

		async Task HandleUpload(DataPacket filename_packet)
		{
			string filename = filename_packet.GetString();

			var file_packet = await GetDataPacket();
			using var file = file_packet!.GetStream();

			await User.Folder.Save(file, filename);

			await SendMessage($"successfully uploaded file \"{filename}\"");

			await Log($"{User.Name} successfully uploaded file \"{filename}\"");

			foreach (var c in server.ConnectionsExcept(this))
				if (!ReferenceEquals(c, this))
					await c.SendMessage($"{User.Name} uploaded file \"{filename}\"");
		}

		async Task HandleListAllUploads()
		{
			await Log($"{User.Name} requested list of all uploads");

			foreach (var user in server.Users)
				await SendMessage(user.CreateUploadsList());
		}
		async Task HandleListMyUploads()
		{
			await Log($"{User.Name} requested list of their uploads");

			await SendMessage(User.CreateUploadsList());
		}
		async Task HandleListUploads(DataPacket packet)
		{
			var username = packet.GetString();

			await Log($"{User.Name} requested list of {username}'s uploads");

			var user = server.Users.GetUser(username);
			if (user != null)
				await SendMessage(user.CreateUploadsList());
			else
				await SendMessage($"no user \"{username}\" registered on the server");
		}

		async Task HandleDownload(DataPacket filename_packet)
		{
			var filename = filename_packet.GetString();
			var username = (await GetDataPacket())!.GetString();

			var user = server.Users.GetUser(username);
			if (user != null)
			{
				using var file = user.Folder.GetFile(filename);
				if (file != null)
				{
					await Packet.SendStringAsync(Stream, PacketDataType.FileName, filename);
					await Packet.SendStreamAsync(Stream, PacketDataType.File, file);
					await SendMessage($"sent file \"{filename}\"");
				}
				else
					await SendMessage($"file \"{filename}\" from \"{user.Name}\" not found");
			}
			else
				await SendMessage($"no user \"{username}\"");
		}

		async Task HandleDownloadNoUsername(DataPacket filename_packet)
		{
			var filename = filename_packet.GetString();

			if (server.Storage.Files.TryGetValue(filename, out var users))
			{
				var sb = new StringBuilder();
				sb.Append($"file {filename} was uploaded by these users: ");
				foreach (var user in users)
					sb.AppendLine(user.Name);
				sb.Remove(sb.Length - 2, 2);
				await SendMessage(sb.ToString());
			}
			else
				await SendMessage($"file \"{filename}\" not found");
		}

		async Task HandleLogIn(DataPacket username_packet)
		{
			var username = username_packet.GetString();
			var hash_packet = await GetDataPacket();
			var hash = hash_packet!.GetBytes();

			var user = server.Users.GetUser(username);
			if (user != null)
			{
				LogIn(user);
				if (Enumerable.SequenceEqual(hash, User.Hash))
				{
					if (!server.UserConnected(User))
					{
						server.ConnectUser(User, this);
						await SendMessage($"welcome back, {User.Name}");
					}
					else
						await Packet.SendStringAsync(Stream, PacketDataType.End, $"you can only log in once, {User.Name}");
				}
				else
					await Packet.SendStringAsync(Stream, PacketDataType.End, $"wrong password, {User.Name}");
			}
			else
			{
				user = new User(username, hash, server.Storage);
				LogIn(user);
				await server.Users.AddUserAsync(User);
				await SendMessage($"you are now registered, {User.Name}");
			}
		}
		async Task HandleRemoveUpload(DataPacket filename_packet)
		{
			var filename = filename_packet.GetString();

			if (User.Folder.Delete(filename))
				await server.Print($"removed file \"{filename}\"");
			else
				await server.Print($"\"{filename}\" not found");
		}
		async Task HandleChangePassword(DataPacket hash_packet)
		{
			User.ChangeHash(hash_packet.GetBytes());
			await server.Users.Save();
			await SendMessage("password changed");
		}
	}
}
