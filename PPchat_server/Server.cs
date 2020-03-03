using PPchat_lib;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace PPchat_server
{
	public class Server : IPrinter
	{
		readonly TextReader input;
		readonly TextWriter output;

		bool running;

		readonly Commands commands;

		readonly TcpListener listener;

		public Users Users { get; private set; }
		public Storage Storage { get; private set; }

		readonly IDictionary<User, ServerConnection> connected_users;
		readonly HashSet<ServerConnection> connections;
		readonly HashSet<Task> connection_tasks;
		Task connections_task;

		readonly bool logging;

		Server(TextReader input, TextWriter output, TcpListener listener, Users users, Storage storage, bool logging)
		{
			this.input = input;
			this.output = output;

			running = false;

			commands = new ServerCommands(this);

			this.listener = listener;
			Users = users;
			Storage = storage;

			connected_users = new Dictionary<User, ServerConnection>();
			connections = new HashSet<ServerConnection>();
			connection_tasks = new HashSet<Task>();
			connections_task = Task.CompletedTask;

			this.logging = logging;
		}

		public static async Task<Server> CreateAsync(TextReader input, TextWriter output, IPAddress ip, int port, string users_filename, string storage_dirname, bool logging)
		{
			var storage = new Storage(storage_dirname);
			var users = await Users.FromFile(users_filename, storage);
			return new Server(
				input,
				output,
				new TcpListener(ip, port),
				users,
				storage,
				logging);
		}

		public async Task Launch(bool start = false)
		{
			await commands.PrintInfo(this);
			if (start)
				await Start();
			await HandleInput();
		}

		public async Task Print(string message)
		{
			await output.WriteLineAsync($"- {message}");
		}
		public async Task Log(string message)
		{
			if (logging)
				await Print(message);
		}

		public IEnumerable<ServerConnection> ConnectionsExcept(ServerConnection connection)
		{
			foreach (var c in connections)
				if (!ReferenceEquals(c, connection))
					yield return c;
		}

		async Task HandleConnections(TcpListener server)
		{
			TcpClient tcp_client;
			while (true)
			{
				try
				{
					tcp_client = await server.AcceptTcpClientAsync();
				}
				catch
				{
					break;
				}
				connection_tasks.RemoveWhere(t => t.IsCompleted);
				var connection = new ServerConnection(tcp_client, this);
				connections.Add(connection);
				connection_tasks.Add(connection.Handle());
			}
			await Task.WhenAll(connection_tasks);
		}

		public void RemoveConnection(ServerConnection connection)
		{
			connections.Remove(connection);
			connected_users.Remove(connection.User);
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

		public async Task Start()
		{
			if (!running)
			{
				listener.Start();
				connections_task = HandleConnections(listener);
				running = true;
				await Print("server started");
			}
			else
				await Print("already running");
		}
		public async Task Stop()
		{
			if (running)
			{
				Parallel.ForEach(connections,
					async (c) => await Packet.SendStringAsync(c.Stream, PacketDataType.End, "server stopped manually"));
				listener.Stop();
				await connections_task;
				running = false;
				await Print("server stopped");
			}
			else
				await Print("already stopped");
		}
		public async Task ListConnectedUsers()
		{
			if (connections.Count != 0)
			{
				await Print("connected users:");
				ISet<User> logged_in_users = new HashSet<User>();
				foreach (var connection in connections)
				{
					var user = connection.User;
					if (!logged_in_users.Contains(user))
					{
						logged_in_users.Add(user);
						await Print(user.Name);
					}
				}
			}
			else
				await Print("there are no users connected");
		}
		public async Task ListRegisteredUsers()
		{
			if (!Users.Empty())
				foreach (var user in Users)
					await Print(user.Name);
			else
				await Print("server has no registered users");
		}
		async Task ListUploads(User user)
		{
			await Print($"{user.Name}'s uploaded files:");
			foreach (var filename in user.Folder.Files)
				await Print(filename);
		}
		public async Task ListUploads(string username)
		{
			var user = Users.GetUser(username);
			if (user != null)
				await ListUploads(user);
			else
				await Print($"no user {username}");
		}
		public async Task ListUploads()
		{
			foreach (var user in Users)
				await ListUploads(user);
		}
		public async Task SendMessage(string what)
		{
			foreach (var connection in connections)
				await connection.SendMessage(what);
		}
		public bool UserConnected(User user)
		{
			return connected_users.ContainsKey(user);
		}
		public void ConnectUser(User user, ServerConnection connection)
		{
			connected_users.Add(user, connection);
		}
		public async Task SendMessage(string username, string what)
		{
			var user = Users.GetUser(username);
			if (user == null || !connected_users.TryGetValue(user, out var connection))
				await Print($"user {username} is not connected");
			else
			{
				await connection.SendMessage($"server says: {what}");
				await Log($"sent a message to {user.Name}");
			}
		}
	}
}
