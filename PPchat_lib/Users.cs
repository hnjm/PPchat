using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using System.Collections;

namespace PPchat_lib
{
	public class Users : IEnumerable<User>
	{
		public string Filename { get; private set; }
		public Storage Storage { get; private set; }
		readonly Dictionary<string, User> dictionary;

		Users(string filename, Storage storage)
		{
			Filename = filename;
			Storage = storage;
			dictionary = new Dictionary<string, User>();
		}

		public static async Task<Users> FromFile(string filename, Storage storage)
		{
			var users = new Users(filename, storage);

			if (File.Exists(filename))
			{
				using var file = File.OpenRead(users.Filename);
				while (true)
				{
					var user = await User.FromStream(file, users.Storage);
					if (user != null)
						users.dictionary.Add(user.Name, user);
					else
						break;
				}
			}
			else
				File.Create(filename).Dispose();

			return users;
		}

		public async Task Save()
		{
			File.Delete(Filename);
			using var file = File.OpenWrite(Filename);
			foreach (var user in dictionary.Values)
				await user.ToStream(file);
		}

		public async Task<bool> AddUserAsync(User user)
		{
			bool result = dictionary.TryAdd(user.Name, user);
			if (result)
				await Save();
			return result;
		}

		public User? GetUser(string name)
		{
			return dictionary.GetValueOrDefault(name, null);
		}

		public bool Contains(string username)
		{
			return dictionary.ContainsKey(username);
		}
		public bool Empty()
		{
			return dictionary.Count == 0;
		}

		public IEnumerator<User> GetEnumerator()
		{
			return dictionary.Values.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}
