using FezEngine.Structure;
using FezEngine.Tools;
using FezGame.Services;

namespace FezGame.Tools;

internal class BackgroundTrileMaterializer : TrileMaterializer
{
	[ServiceDependency]
	public IPlayerManager PlayerManager { private get; set; }

	public BackgroundTrileMaterializer(Trile trile, Mesh levelMesh)
		: base(trile, levelMesh)
	{
	}
}
