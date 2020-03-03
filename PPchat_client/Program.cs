using System;
using System.Net;
using System.Threading.Tasks;

namespace PPchat_client
{
	class Program
	{
		static async Task Main(string[] args)
		{
			Client client = new Client(Console.In, Console.Out, IPAddress.Parse("127.0.0.1"), 2048);
			await client.Launch();
		}
	}
}
