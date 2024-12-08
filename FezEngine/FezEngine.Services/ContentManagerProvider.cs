using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using FezEngine.Tools;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;

namespace FezEngine.Services;

public class ContentManagerProvider : GameComponent, IContentManagerProvider
{
	private readonly ContentManager global;

	private readonly Dictionary<string, SharedContentManager> levelScope;

	private readonly Dictionary<CM, SharedContentManager> temporary;

	public ContentManager Global => global;

	public ContentManager CurrentLevel => GetForLevel(LevelManager.Name ?? "");

	[ServiceDependency]
	public ILevelManager LevelManager { private get; set; }

	public ContentManagerProvider(Game game)
		: base(game)
	{
		global = new SharedContentManager("Global");
		levelScope = new Dictionary<string, SharedContentManager>();
		temporary = new Dictionary<CM, SharedContentManager>();
	}

	public override void Initialize()
	{
		LevelManager.LevelChanged += CleanAndPrecache;
	}

	private void CleanAndPrecache()
	{
		string[] array = levelScope.Keys.ToArray();
		foreach (string text in array)
		{
			if (text != LevelManager.Name)
			{
				levelScope[text].Dispose();
				levelScope.Remove(text);
			}
		}
	}

	public ContentManager GetForLevel(string levelName)
	{
		if (!levelScope.TryGetValue(levelName, out var value))
		{
			levelScope.Add(levelName, value = new SharedContentManager(levelName));
			value.RootDirectory = global.RootDirectory;
		}
		return value;
	}

	public ContentManager Get(CM name)
	{
		if (!temporary.TryGetValue(name, out var value))
		{
			temporary.Add(name, value = new SharedContentManager(name.ToString()));
			value.RootDirectory = global.RootDirectory;
		}
		return value;
	}

	public void Dispose(CM name)
	{
		if (temporary.TryGetValue(name, out var value))
		{
			value.Dispose();
			temporary.Remove(name);
		}
	}

	public IEnumerable<string> GetAllIn(string directory)
	{
		directory = directory.Replace('/', '\\').ToLower(CultureInfo.InvariantCulture);
		return MemoryContentManager.AssetNames.Where((string x) => x.StartsWith(directory));
	}
}
