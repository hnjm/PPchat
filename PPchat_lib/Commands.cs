using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using System.Linq;
using System.Text.RegularExpressions;

namespace PPchat_lib
{
	public class Commands
	{
		readonly string prefix;
		readonly IDictionary<string, Command> names;
		readonly IDictionary<string, Command> alternative_names;
		readonly Command bad_arguments;
		readonly Command no_prefix;
		readonly Command not_found;

		public Commands(string prefix, Command bad_arguments, Command no_prefix, Command not_found)
		{
			this.prefix = prefix;
			names = new Dictionary<string, Command>();
			alternative_names = new Dictionary<string, Command>();
			this.bad_arguments = bad_arguments;
			this.no_prefix = no_prefix;
			this.not_found = not_found;
		}

		public void Add(Command command)
		{
			names.Add(command.Name, command);
		}

		public void CreateAlternativeNames()
		{
			foreach (var command in names.Values.OrderBy((command) => command.Name.Length))
			{
				var name = command.Name;
				var length = name.Length;
				var sb = new StringBuilder(length);
				int i = 0;
				do
				{
					sb.Append(name[i]);
					++i;
				}
				while (i != length && (names.ContainsKey(sb.ToString()) || alternative_names.ContainsKey(sb.ToString())));

				if (i != length)
				{
					alternative_names.Add(sb.ToString(), command);
					command.AlternativeNameLength = i;
				}
			}
		}

		public static async Task PrintInfo(IPrinter printer, Command command)
		{
			await printer.Print(command.GetFullInfo());
		}

		public async Task Parse(string line)
		{
			if (line.StartsWith(prefix))
			{
				line = line.Substring(prefix.Length);
				var matches = Regex.Matches(line, "[^ ]+");
				var command_name = matches[0].Value;
				
				bool found = names.TryGetValue(command_name, out Command? command);
				if (!found)
					found = alternative_names.TryGetValue(command_name, out command);
				if (found)
				{
					if (matches.Count - 1 >= command!.MinOptionCount && matches.Count - 1 <= command.MaxOptionCount)
						await command.Execute(matches);
					else
						await bad_arguments.Execute(tokens);
				}
				else
					await not_found.Execute(tokens);
			}
			else
				await no_prefix.Execute(new string[] { line });
		}

		public async Task PrintInfo(IPrinter printer)
		{
			if (prefix == "")
				await printer.Print("there is no command prefix required");
			else
			{
				await printer.Print($"command prefix: {prefix}");
				await printer.Print($"no prefix command: {no_prefix.Info}");
			}

			foreach (var command in names.Values)
				await PrintInfo(printer, command);
		}
	}
}
