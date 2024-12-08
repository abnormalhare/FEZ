using FezEngine.Components;
using FezEngine.Structure;
using Microsoft.Xna.Framework;

namespace FezGame.Components;

public interface IPlaneParticleSystems
{
	PlaneParticleSystem RainSplash(Vector3 center);

	void Splash(IPhysicsEntity entity, bool outwards);

	void Splash(IPhysicsEntity entity, bool outwards, float velocityBonus);

	void Add(PlaneParticleSystem system);

	void Remove(PlaneParticleSystem system, bool returnToPool);

	void ForceDraw();
}
