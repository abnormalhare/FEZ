using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Microsoft.Xna.Framework.Content;

namespace FezEngine.Tools;

public class MemoryContentManager : ContentManager
{
	private static Dictionary<string, byte[]> cachedAssets;

	private static readonly object ReadLock = new object();

	private string TitleUpdateRoot => base.RootDirectory;

	public static IEnumerable<string> AssetNames => cachedAssets.Keys;

	public MemoryContentManager(IServiceProvider serviceProvider, string rootDirectory)
		: base(serviceProvider, rootDirectory)
	{
	}

	public void LoadEssentials()
	{
		cachedAssets = new Dictionary<string, byte[]>(3011);
		using FileStream input = File.OpenRead(Path.Combine(base.RootDirectory, "Essentials.pak"));
		using BinaryReader binaryReader = new BinaryReader(input);
		int num = binaryReader.ReadInt32();
		for (int i = 0; i < num; i++)
		{
			string key = binaryReader.ReadString();
			int num2 = binaryReader.ReadInt32();
			if (!cachedAssets.ContainsKey(key))
			{
				cachedAssets.Add(key, binaryReader.ReadBytes(num2));
			}
			else
			{
				binaryReader.BaseStream.Seek(num2, SeekOrigin.Current);
			}
		}
	}

	public void Preload()
	{
		Action<string> obj = delegate(string name)
		{
			using FileStream input = File.OpenRead(Path.Combine(base.RootDirectory, name));
			using BinaryReader binaryReader = new BinaryReader(input);
			int num = binaryReader.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				string key = binaryReader.ReadString();
				int num2 = binaryReader.ReadInt32();
				bool flag;
				lock (ReadLock)
				{
					flag = cachedAssets.ContainsKey(key);
				}
				if (!flag)
				{
					byte[] value = binaryReader.ReadBytes(num2);
					lock (ReadLock)
					{
						cachedAssets.Add(key, value);
					}
				}
				else
				{
					binaryReader.BaseStream.Seek(num2, SeekOrigin.Current);
				}
			}
		};
		obj("Updates.pak");
		obj("Other.pak");
	}

	protected override Stream OpenStream(string assetName)
	{
		lock (ReadLock)
		{
			if (!cachedAssets.TryGetValue(assetName.ToLower(CultureInfo.InvariantCulture).Replace('/', '\\'), out var value))
			{
				throw new ContentLoadException("Can't find asset named : " + assetName);
			}
			return new MemoryStream(value, 0, value.Length, writable: true, publiclyVisible: true);
		}
	}

	public static bool AssetExists(string name)
	{
		return cachedAssets.ContainsKey(name.Replace('/', '\\').ToLower(CultureInfo.InvariantCulture));
	}
}
