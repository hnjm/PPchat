using System.Net;
using PPchat_lib;

namespace PPchat_client
{
	class ClientCommands : Commands
	{
		public ClientCommands(Client client)
			: base(
				"/",
				new Command(async (_) => await client.Print($"bad option count")),
				new Command(async (arguments) => await client.SendMessage(arguments[0]), "sends a message"),
				new Command(async (arguments) => await client.Print($"non existing command: {arguments[0]}")))
		{
			Add(new Command(
				async (x) => await PrintInfo(client),
				"help",
				"prints all commands with explanations"));

			Add(new Command(
				async (arguments) =>
				{
					if (arguments.Length == 2)
					{
						if (IPAddress.TryParse(arguments[1], out IPAddress ip))
							await client.ChangeIP(ip);
						else
							await client.Print($"invalid ip {arguments[1]}");
					}
					else
						await client.PrintIP();
				},
				"ip",
				"[<new_ip>]",
				"prints or changes the server IP",
				1));

			Add(new Command(
				async (arguments) =>
				{
					if (arguments.Length == 2)
					{
						if (int.TryParse(arguments[1], out int port) && port > 0 && port < 65536)
							await client.ChangePort(port);
						else
							await client.Print($"- invalid port {arguments[1]}");
					}
					else
						await client.PrintPort();
				},
				"port",
				"[<new_port>]",
				"prints or changes the server port",
				1));

			Add(new Command(
				async (arguments) => await client.Connect(arguments[1], arguments[2]),
				"connect",
				"<username> <password>",
				"connects to the server",
				2,
				2));

			Add(new Command(
				async (arguments) => await client.Disconnect(),
				"disconnect",
				"disconnects from the server"));

			Add(new Command(
				async (arguments) => await client.Upload(arguments[1]),
				"upload",
				"<file>",
				"uploads file to the server",
				1,
				1));

			Add(new Command(
				async (arguments) =>
				{
					if (arguments.Length == 3)
						await client.Download(arguments[1], arguments[2]);
					else
						await client.Download(arguments[1]);
				},
				"download",
				"<filename> [<username>]",
				"downloads a file",
				2,
				1));

			Add(new Command(
				async (arguments) =>
				{
					if (arguments.Length == 2)
					{
						if (arguments[0] == "-a")
							await client.ListAllUploads();
						else
							await client.ListUploads(arguments[1]);
					}
					else
						await client.ListMyUploads();
				},
				"list_uploads",
				"[<username>|-a]",
				"requests a list of uploads on the server",
				1));

			Add(new Command(
				async (arguments) => await client.ListAllUploads(),
				"list_uploads_all",
				"requests a list of all uploads on the server"));

			Add(new Command(
				async (arguments) => await client.RemoveUpload(arguments[1]),
				"remove_upload",
				"<filename>",
				"removes an uploaded file",
				1,
				1));

			Add(new Command(
				async (arguments) => await client.ListDownloads(),
				"list_downloads",
				"lists user's downloaded files"));

			Add(new Command(
				async (arguments) => await client.RemoveDownload(arguments[1]),
				"remove_download",
				"<filename>",
				"removes a downloaded file",
				1,
				1));

			Add(new Command(
				async (arguments) => await client.RenameDownload(arguments[1], arguments[2]),
				"rename_download",
				"<old_filename> <new_filename>",
				"renames a downloaded file",
				2,
				2));

			Add(new Command(
				async (arguments) => await client.ChangePassword(arguments[1]),
				"change_password",
				"<new_password>",
				"changes user's password",
				1,
				1));

			CreateAlternativeNames();
		}
	}
}
