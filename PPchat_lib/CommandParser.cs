using System.IO;
using System;
using System.Text;

namespace PPchat_lib
{
	class CommandParser : IDisposable
	{
		string line;
		readonly TextReader reader;
		int read;
		readonly StringBuilder builder;
		int argument_count;

		public CommandParser(string line)
		{
			this.line = line;
			reader = new StringReader(line);
			builder = new StringBuilder();
			Read();
			argument_count = -1;
		}

		void Read()
		{
			read = reader.Read();
		}
		void Append()
		{
			builder.Append((char)read);
		}
		string PopString()
		{
			var s = builder.ToString();
			builder.Clear();
			return s;
		}

		int ChugCharacters()
		{
			int count = 0;
			while (read != -1 && read != ' ')
			{
				Append();
				Read();
				++count;
			}
			return count;
		}
		int ChugSpaces()
		{
			int count = 0;
			while (read != -1 && read == ' ')
			{
				Read();
				++count;
			}
			return count;
		}

		int ParseSimpleToken()
		{
			int count = ChugCharacters();
			return count + ChugSpaces();
		}

		void ParseToken()
		{
			bool in_quotes = false;

			while (read != -1 && (read != ' ' || in_quotes))
			{
				if (read != '"')
					Append();
				else
					in_quotes = !in_quotes;
				Read();
			}
			while (read != -1 && read == ' ' && !in_quotes)
				Read();
		}

		public int CountArguments()
		{
			if (argument_count == -1)
			{
				int count = 0;

				bool in_quotes = false;

				char c;
				int i = 0;

				while (i != line.Length)
				{
					c = line[i];

					if (c == '"')
						in_quotes = !in_quotes;
					else if (c == ' ' && !in_quotes)
					{
						++count;
					}

					++i;
				}

				argument_count = count + 1;
			}
			
			return argument_count;
		}

		public bool PrefixParse(string prefix)
		{
			var has_prefix = line.StartsWith(prefix);
			if (has_prefix)
			{
				for (int i = 0; i != prefix.Length; ++i)
					Read();
				line = line.Substring(prefix.Length);
			}

			return has_prefix;
		}
		public string CommandNameParse()
		{
			var length = ParseSimpleToken();
			var s = PopString();
			line = line.Substring(length);
			return s;
		}
		public Arguments Rest()
		{
			return new Arguments(line);
		}
		public Arguments Arguments()
		{
			var args = new string[CountArguments()];

			for (int i = 0; i != args.Length; ++i)
				args[i] = ArgumentParse();

			return new Arguments(args);
		}
		string ArgumentParse()
		{
			ParseToken();
			return PopString();
		}

		public void Dispose()
		{
			reader.Dispose();
		}
	}
}
