using PPchat_lib;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace PPchat_client
{
	public class Client : Connection, IPrinter
	{
		readonly Commands commands;
		readonly TextReader input;
		readonly TextWriter output;
		public Storage Storage { get; private set; }

		Task connection_task;

		IPAddress ip;
		int port;

		public Client(TextReader input, TextWriter output, IPAddress ip, int port)
		{
			commands = new ClientCommands(this);

			this.input = input;
			this.output = output;

			Storage = new Storage("downloads");

			connection_task = Task.CompletedTask;

			this.ip = ip;
			this.port = port;
		}
		public async Task Print(string message)
		{
			await output.WriteLineAsync($"- {message}");
		}
		public async Task Launch()
		{
			await commands.PrintInfo(this);

			await HandleInput();
		}

		async Task HandleInput()
		{
			string? line;
			while (true)
			{
				line = await input.ReadLineAsync() ?? "";
				if (line == "")	continue;
				await commands.Parse(line);
			}
		}

		public async Task PrintIP()
		{
			await Print($"current IP is {ip}");
		}

		public async Task ChangeIP(IPAddress ip)
		{
			if (!Connected)
			{
				this.ip = ip;
				await PrintIP();
			}
			else
				await Print("disconnect first");
		}
		public async Task PrintPort()
		{
			await Print($"current port is {port}");
		}

		public async Task ChangePort(int port)
		{
			if (!Connected)
			{
				this.port = port;
				await PrintPort();
			}
			else
				await Print("- disconnect before changing the IP");
		}

		async Task<bool> CheckConnected()
		{
			bool connected = Connected;
			if (!connected)
				await Print("connect first");
			return connected;
		}

		async Task HandleNullPacket()
		{
			await Print("server ended connection unexpectedly");
			base.Disconnect();
		}
		async Task HandleIncomingMessage(DataPacket packet)
		{
			await Print(packet.GetString());
		}
		async Task HandleDownload(DataPacket filename_packet)
		{
			var filename = filename_packet.GetString();
			using var file = (await GetDataPacket())!.GetStream();
			if (User.Folder.Exists(filename))
			{
				string new_name;
				do
				{
					new_name = Path.GetRandomFileName();
				} while (User.Folder.Exists(new_name));

				await Print($"a file with name {filename} already exists, saving as {new_name}");
				filename = new_name;
			}
			else
				await Print($"saved file {filename}");
			await User.Folder.Save(file, filename);
		}
		async Task HandleIncomingEnd(DataPacket packet)
		{
			await Packet.SendEmptyAsync(Stream, PacketDataType.AckEnd);

			await Print("server ended connection");
			await Print($"reason: {packet.GetString()}");
		}
		public async Task Handle()
		{
			while (true)
			{
				var packet = await Packet.ReadAsync(Stream);
				if (packet == null)
				{
					await HandleNullPacket();
					break;
				}
				var type = packet.DataType();
				if (type == PacketDataType.Message)
					await HandleIncomingMessage((DataPacket)packet);
				else if (type == PacketDataType.FileName)
					await HandleDownload((DataPacket)packet);
				else if (type == PacketDataType.End)
				{
					await HandleIncomingEnd((DataPacket)packet);
					break;
				}
				else if (type == PacketDataType.AckEnd)
					break;
			}

			base.Disconnect();
		}

		public async Task Connect(string username, string password)
		{
			if (!Connected)
			{
				if (!username.Contains('.'))
				{
					var tcp_client = new TcpClient();
					try
					{
						await tcp_client.ConnectAsync(ip, port);
					}
					catch
					{
						await Print("can't connect to server");
						return;
					}

					Connect(tcp_client);

					LogIn(User.FromPassword(username, password, Storage));

					await Packet.SendStringAsync(Stream, PacketDataType.LogIn, User.Name);
					await Packet.SendBytesAsync(Stream, PacketDataType.Password, User.Hash);

					connection_task = Handle();
				}
				else
					await Print("username can't contain '.'");
			}
			else
				await Print("already connected");
		}
		public async Task ChangePassword(string password)
		{
			if (await CheckConnected())
				await Packet.SendBytesAsync(Stream, PacketDataType.ChangePassword, User.GetHash(password));
		}
		public async Task RemoveUpload(string filename)
		{
			if (await CheckConnected())
				await Packet.SendStringAsync(Stream, PacketDataType.RemoveUpload, filename);
		}
		public async Task RemoveDownload(string filename)
		{
			if (await CheckConnected())
			{
				if (User.Folder.Delete(filename))
					await Print($"removed file \"{filename}\"");
				else
					await Print($"\"{filename}\" not found");
			}
		}
		public async Task RenameDownload(string old_filename, string new_filename)
		{
			if (!await CheckConnected())
				return;

			if (User.Folder.Rename(old_filename, new_filename))
				await Print($"\"{old_filename}\" renamed to \"{new_filename}\"");
			else if (User.Folder.Exists(new_filename))
				await Print($"a file with name \"{new_filename}\" already exists");
			else
				await Print($"no file \"{old_filename}\" found");
		}
		public async Task ListDownloads()
		{
			if (await CheckConnected())
				await Print(User.CreateUploadsList());
		}
		public new async Task Disconnect()
		{
			if (Connected)
			{
				await Packet.SendStringAsync(Stream, PacketDataType.End, "manual disconnect");
				await connection_task;
				base.Disconnect();
				await Print("disconnected");
			}
			else
				await Print("already disconnected");
		}
		public async Task Upload(string path)
		{
			if (await CheckConnected())
			{
				if (File.Exists(path))
				{
					await Packet.SendStringAsync(Stream, PacketDataType.FileName, Path.GetFileName(path));
					using var file = File.OpenRead(path);
					await Packet.SendStreamAsync(Stream, PacketDataType.File, file);
				}
				else
					await Print($"file {path} does not exist");
			}
		}
		public async Task ListAllUploads()
		{
			if (await CheckConnected())
				await Packet.SendEmptyAsync(Stream, PacketDataType.ListAllUploads);
		}
		public async Task ListUploads(string username)
		{
			if (await CheckConnected())
				await Packet.SendStringAsync(Stream, PacketDataType.ListUploads, username);
		}
		public async Task ListMyUploads()
		{
			if (await CheckConnected())
				await Packet.SendEmptyAsync(Stream, PacketDataType.ListMyUploads);
		}
		public async Task Download(string filename)
		{
			if (await CheckConnected())
				await Packet.SendStringAsync(Stream, PacketDataType.DownloadNoUsername, filename);
		}
		public async Task Download(string filename, string username)
		{
			if (await CheckConnected())
			{
				await Packet.SendStringAsync(Stream, PacketDataType.Download, filename);
				await Packet.SendStringAsync(Stream, PacketDataType.Username, username);
			}
		}
		public new async Task SendMessage(string message)
		{
			if (await CheckConnected())
				await Packet.SendStringAsync(Stream, PacketDataType.Message, message);
		}
	}
}
