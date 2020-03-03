using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;

namespace PPchat_lib
{
	public class Folder
	{
		readonly Storage storage;
		readonly User User;
		public ISet<string> Files { get; private set; }
		public Folder(User user, Storage storage)
		{
			this.storage = storage;
			User = user;
			Files = new HashSet<string>();

			var path = GetPath();
			if (Directory.Exists(path))
				foreach (var file in Directory.EnumerateFiles(path))
					Add(Path.GetFileName(file));
		}
		void Add(string filename)
		{
			storage.Add(filename, User);
			Files.Add(filename);
		}
		void Remove(string filename)
		{
			storage.Remove(filename, User);
			Files.Remove(filename);
		}
		string GetPath()
		{
			return Path.Join(storage.Name, User.Name);
		}
		string GetPath(string filename)
		{
			return Path.Join(storage.Name, User.Name, filename);
		}
		public bool Exists(string filename)
		{
			return Files.Contains(filename);
		}
		public Stream? GetFile(string filename)
		{
			if (Exists(filename))
				return File.OpenRead(GetPath(filename));
			else
				return null;
		}
		public async Task<bool> Save(Stream stream, string filename)
		{
			Directory.CreateDirectory(GetPath());

			if (!Exists(filename))
			{
				using var file = File.OpenWrite(GetPath(filename));
				await stream.CopyToAsync(file);
				Add(filename);
				return true;
			}

			return false;
		}
		public bool Delete(string filename)
		{
			if (Exists(filename))
			{
				Remove(filename);
				File.Delete(GetPath(filename));
				return true;
			}
			else
				return false;
		}
		public bool Rename(string old_filename, string new_filename)
		{
			bool valid = Exists(old_filename) && !Exists(new_filename);
			if (valid)
			{
				Remove(old_filename);
				Add(new_filename);
				File.Move(GetPath(old_filename), GetPath(new_filename));
			}
			return valid;
		}
		public bool Empty => Files.Count == 0;
	}
}
