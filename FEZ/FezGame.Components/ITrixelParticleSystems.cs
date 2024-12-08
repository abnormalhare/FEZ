using Microsoft.Xna.Framework;

namespace FezGame.Components;

public interface ITrixelParticleSystems
{
	int Count { get; }

	void Add(TrixelParticleSystem system);

	void PropagateEnergy(Vector3 energySource, float energy);

	void UnGroundAll();

	void ForceDraw();
}
