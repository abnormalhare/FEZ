using Microsoft.Xna.Framework;

namespace FezEngine.Structure;

public interface ITrixelObject
{
	Vector3 Size { get; }

	TrixelCluster MissingTrixels { get; }

	bool TrixelExists(TrixelEmplacement trixelIdentifier);

	bool CanContain(TrixelEmplacement trixel);

	bool IsBorderTrixelFace(TrixelEmplacement id, FaceOrientation face);

	bool IsBorderTrixelFace(TrixelEmplacement traversed);
}
