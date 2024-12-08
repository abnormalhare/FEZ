using FezEngine;
using FezEngine.Structure;
using FezGame.Structure;
using Microsoft.Xna.Framework;

namespace FezGame.Services;

public interface IPhysicsManager
{
	void DetermineOverlaps(IComplexPhysicsEntity entity);

	void DetermineOverlaps(ISimplePhysicsEntity entity);

	bool DetermineInBackground(IPhysicsEntity entity, bool allowEnterInBackground, bool viewpointChanged, bool keepInFront);

	bool Update(ISimplePhysicsEntity entity);

	bool Update(ISimplePhysicsEntity entity, bool simple, bool keepInFront);

	bool Update(IComplexPhysicsEntity entity);

	void ClampToGround(IPhysicsEntity entity, Vector3? distance, Viewpoint viewpoint);

	PhysicsManager.WallHuggingResult HugWalls(IPhysicsEntity entity, bool determineBackground, bool postRotation, bool keepInFront);
}
