using System;
using System.Collections.Generic;
using System.IO;
using Common;
using FezEngine.Effects.Structures;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace FezEngine.Tools;

public class SharedContentManager : ContentManager
{
	private class CommonContentManager : MemoryContentManager
	{
		private class ReferencedAsset
		{
			public object Asset;

			public int References;
		}

		private readonly Dictionary<string, ReferencedAsset> references = new Dictionary<string, ReferencedAsset>();

		public CommonContentManager(IServiceProvider serviceProvider, string rootDirectory)
			: base(serviceProvider, rootDirectory)
		{
		}

		public T Load<T>(string name, string assetName)
		{
			lock (this)
			{
				assetName = GetCleanPath(assetName);
				if (!references.TryGetValue(assetName, out var value))
				{
					if (TraceFlags.TraceContentLoad)
					{
						Logger.Log("Content", "[" + name + "] Loading " + typeof(T).Name + " " + assetName);
					}
					value = new ReferencedAsset
					{
						Asset = ReadAsset<T>(assetName)
					};
					references.Add(assetName, value);
				}
				value.References++;
				if (value.Asset is SoundEffect)
				{
					(value.Asset as SoundEffect).Name = assetName.Substring("Sounds/".Length);
				}
				return (T)value.Asset;
			}
		}

		private T ReadAsset<T>(string assetName)
		{
			return ReadAsset<T>(assetName, Util.NullAction);
		}

		public void Unload(SharedContentManager container)
		{
			lock (this)
			{
				foreach (string loadedAsset in container.loadedAssets)
				{
					if (loadedAsset == null)
					{
						Logger.Log("Content", LogSeverity.Warning, "Null-named asset in content manager : " + container.Name);
						continue;
					}
					if (!references.TryGetValue(loadedAsset, out var value))
					{
						Logger.Log("Content", LogSeverity.Warning, "Couldn't find asset in references : " + loadedAsset);
						continue;
					}
					value.References--;
					if (value.References == 0)
					{
						if (value.Asset is Texture)
						{
							(value.Asset as Texture).Unhook();
						}
						if (value.Asset is IDisposable)
						{
							(value.Asset as IDisposable).Dispose();
						}
						references.Remove(loadedAsset);
						value.Asset = null;
					}
				}
			}
		}
	}

	private static CommonContentManager Common;

	private readonly string Name;

	private List<string> loadedAssets;

	public SharedContentManager(string name)
		: base(ServiceHelper.Game.Services, ServiceHelper.Game.Content.RootDirectory)
	{
		if (Common == null)
		{
			Common = new CommonContentManager(ServiceHelper.Game.Services, ServiceHelper.Game.Content.RootDirectory);
			Common.LoadEssentials();
		}
		Name = name;
		loadedAssets = new List<string>();
	}

	public static string GetCleanPath(string path)
	{
		path = path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
		int num = 1;
		while (num < path.Length)
		{
			num = path.IndexOf("\\..\\", num);
			if (num < 0)
			{
				return path;
			}
			int num2 = path.LastIndexOf(Path.DirectorySeparatorChar, num - 1) + 1;
			path = path.Remove(num2, num - num2 + "\\..\\".Length);
			num = Math.Max(num2 - 1, 1);
		}
		return path;
	}

	public override T Load<T>(string assetName)
	{
		assetName = GetCleanPath(assetName);
		loadedAssets.Add(assetName);
		return Common.Load<T>(Name, assetName);
	}

	public override void Unload()
	{
		if (loadedAssets == null)
		{
			throw new ObjectDisposedException(typeof(SharedContentManager).Name);
		}
		Common.Unload(this);
		loadedAssets = null;
		base.Unload();
	}

	public static void Preload()
	{
		Common.Preload();
	}
}
