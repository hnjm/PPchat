using System.IO;
using System.Collections.Generic;

namespace PPchat_lib
{
	public class Storage
	{
		public string Name { get; private set; }
		public IDictionary<string, ISet<User>> Files { get; private set; }
		public Storage(string name)
		{
			Name = name;
			Files = new Dictionary<string, ISet<User>>();
			Directory.CreateDirectory(Name);
		}
		public void Add(string filename, User user)
		{
			if (!Files.TryGetValue(filename, out var users))
			{
				users = new HashSet<User>();
				Files.Add(filename, users);
			}
			users.Add(user);
		}
		public void Remove(string filename, User user)
		{
			var users = Files[filename];
			if (users.Count == 1)
				Files.Remove(filename);
			else
				users.Remove(user);
		}
	}
}
