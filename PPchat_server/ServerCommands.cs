using PPchat_lib;

namespace PPchat_server
{
	class ServerCommands : Commands
	{
		public ServerCommands(Server server)
			: base(
				"",
				new Command(async (_) => await server.Print($"bad option count")),
				new Command(),
				new Command(async (arguments) => await server.Print($"non existing command: {arguments[0]}")))
		{
			Add(new Command(
				async (x) => await server.Start(),
				"start",
				"starts the server"));

			Add(new Command(
				async (x) => await server.Stop(),
				"stop",
				"stops the server"));

			Add(new Command(
				async (x) => await server.ListConnectedUsers(),
				"list_users",
				"lists all users"));

			Add(new Command(
				async (x) => await server.ListConnectedUsers(),
				"say",
				"sends a message to all users"));

			Add(new Command(
				async (arguments) =>
				{
					if (arguments.Length == 2)
						await server.ListUploads(arguments[1]);
					else
						await server.ListUploads();
				},
				"list_uploads",
				"[<username>]",
				"lists all uploads or specific to <username>",
				1));

			CreateAlternativeNames();
		}
	}
}
