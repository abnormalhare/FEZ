using System.Collections.Generic;
using Microsoft.Xna.Framework.Content;

namespace FezEngine.Services;

public interface IContentManagerProvider
{
	ContentManager Global { get; }

	ContentManager CurrentLevel { get; }

	ContentManager GetForLevel(string levelName);

	ContentManager Get(CM name);

	IEnumerable<string> GetAllIn(string directory);

	void Dispose(CM name);
}
