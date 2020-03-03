using System.Security.Cryptography;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System;
using static PPchat_lib.Serialization;

namespace PPchat_lib
{
	public class User
	{
		const int hash_size = 32;

		public string Name { get; private set; }
		public byte[] Hash { get; private set; }
		public Folder Folder { get; private set; }

		public static byte[] GetHash(string s)
		{
			using var hasher = SHA256.Create();
			return hasher.ComputeHash(Encoding.ASCII.GetBytes(s));
		}

		public User(string name, byte[] hash, Storage storage)
		{
			Name = name;
			Hash = hash;
			Folder = new Folder(this, storage);
		}

		public static User FromPassword(string username, string password, Storage storage)
		{
			return new User(username, GetHash(password), storage);
		}

		public void ChangeHash(byte[] hash)
		{
			Hash = hash;
		}

		public static async Task<User?> FromStream(Stream stream, Storage storage)
		{
			var name = await ReadStringAsync(stream);
			if (name == null)
				return null;

			var hash = new byte[hash_size];
			if (!await ReadBytesAsync(stream, hash))
				return null;

			return new User(name, hash, storage);
		}
		public async Task<bool> ToStream(Stream stream)
		{
			return	await WriteStringAsync(stream, Name) &&
					await WriteBytesAsync(stream, Hash);
		}

		public string CreateUploadsList()
		{
			var sb = new StringBuilder();

			if (!Folder.Empty)
			{
				sb.AppendLine($"{Name}'s files:");
				foreach (var filename in Folder.Files)
					sb.AppendLine(filename);
				sb.Remove(sb.Length - 1, 1);
			}
			else
				sb.Append($"user {Name} has no files");

			return sb.ToString();
		}
	}
}
