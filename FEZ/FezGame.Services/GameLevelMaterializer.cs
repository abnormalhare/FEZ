using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Tools;
using Microsoft.Xna.Framework;

namespace FezGame.Services;

public class GameLevelMaterializer : LevelMaterializer
{
	public GameLevelMaterializer(Game game)
		: base(game)
	{
	}

	public override void RebuildTrile(Trile trile)
	{
		TrileMaterializer trileMaterializer = new TrileMaterializer(trile, base.TrilesMesh, mutableSurfaces: false);
		trileMaterializers.Add(trile, trileMaterializer);
		trileMaterializer.Geometry = trile.Geometry;
		trileMaterializer.DetermineFlags();
	}
}
