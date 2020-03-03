namespace PPchat_lib
{
	public class Arguments
	{
		readonly string? s;
		readonly string[]? args;
		public int Count => args?.Length ?? 0;
		Arguments()
		{ }
		public Arguments(string s)
		{
			this.s = s;
			args = null;
		}
		public Arguments(string[] args)
		{
			this.args = args;
			s = null;
		}
		public static Arguments Empty { get; } = new Arguments();
		public string this[int i]
		{
			get => args![i];
			set => args![i] = value;
		}
		public string Unparsed()
		{
			return s!;
		}
	}
}
