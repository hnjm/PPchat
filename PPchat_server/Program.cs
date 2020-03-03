using System;
using System.Net;
using System.Threading.Tasks;

namespace PPchat_server
{
	class Program
	{
		static async Task Main(string[] args)
		{
			Server server;
			try
			{
				server = await Server.CreateAsync(
				Console.In,
				Console.Out,
				IPAddress.Any,
				2048,
				"users",
				"uploads",
				true);
			}
			catch (Exception e)
			{
				Console.WriteLine("couldn't create server. reason:");
				Console.WriteLine(e.Message);
				return;
			}
			
			await server.Launch(true);
		}
	}
}
