using System.Threading.Tasks;
using System.Text;

namespace PPchat_lib
{
	public class Command
	{
		public delegate Task CommandHandler(string[] arguments);

		public string Name { get; private set; }
		public int AlternativeNameLength { get; set; }
		public string Format { get; private set; }
		public string Info { get; private set; }
		public int MaxOptionCount { get; private set; }
		public int MinOptionCount { get; private set; }
		public CommandHandler Execute { get; private set; }

		public Command(
			CommandHandler execute_handler,
			string name = "",
			string format = "",
			string info = "",
			int max_option_count = 0,
			int min_option_count = 0)
		{
			Execute = execute_handler;
			Name = name;
			AlternativeNameLength = 0;
			Format = format;
			Info = info;
			MaxOptionCount = max_option_count;
			MinOptionCount = min_option_count;
		}
		public Command()
			: this((x) => Task.CompletedTask)
		{ }
		public Command(CommandHandler execute_handler, string name, string info)
		   : this(execute_handler, name, "", info, 0, 0)
		{ }
		public Command(CommandHandler execute_handler, string name, int max_option_count)
		   : this(execute_handler, name, "", "", max_option_count, 0)
		{ }
		string FormattedName()
		{
			var sb = new StringBuilder(Name);
			sb.Insert(AlternativeNameLength, '[');
			sb.Append(']');
			return sb.ToString();
		}
		public string GetFullInfo()
		{
			var sb = new StringBuilder();
			if (AlternativeNameLength == 0)
				sb.Append(Name);
			else
				sb.Append(FormattedName());
			if (MaxOptionCount != 0)
			{
				sb.Append(' ');
				sb.Append(Format);
			}
			sb.Append(": ");
			sb.Append(Info);
			return sb.ToString();
		}
	}
}
