using FezEngine.Structure;
using Microsoft.Xna.Framework;

namespace FezGame.Components;

internal class PickupState
{
	public readonly TrileInstance Instance;

	public readonly Vector3 OriginalCenter;

	public TrileGroup Group;

	public Vector3 LastGroundedCenter;

	public Vector3 LastVelocity;

	public float FlightApex;

	public bool TouchesWater;

	public float FloatSeed;

	public float FloatMalus;

	public Vector3 LastMovement;

	public PickupState VisibleOverlapper;

	public ArtObjectInstance[] AttachedAOs;

	public PickupState(TrileInstance ti, TrileGroup group)
	{
		Instance = ti;
		OriginalCenter = ti.Center;
		Group = group;
	}
}
