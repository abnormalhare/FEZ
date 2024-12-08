using FezEngine.Structure;
using Microsoft.Xna.Framework;

namespace FezGame.Components;

internal class BridgeState
{
	public readonly TrileInstance Instance;

	public Vector3 OriginalPosition;

	public float Downforce;

	public bool Dirty;

	public BridgeState(TrileInstance instance)
	{
		Instance = instance;
		OriginalPosition = instance.Position;
		if (instance.PhysicsState == null)
		{
			instance.PhysicsState = new InstancePhysicsState(instance)
			{
				Sticky = true
			};
		}
	}
}
