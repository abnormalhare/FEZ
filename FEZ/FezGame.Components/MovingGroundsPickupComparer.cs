using System;
using System.Collections.Generic;
using FezEngine.Structure;
using FezEngine.Tools;
using Microsoft.Xna.Framework;

namespace FezGame.Components;

internal class MovingGroundsPickupComparer : Comparer<PickupState>
{
	public new static readonly MovingGroundsPickupComparer Default = new MovingGroundsPickupComparer();

	public override int Compare(PickupState x, PickupState y)
	{
		int num = 0;
		int num2 = 0;
		TrileInstance trileInstance = null;
		TrileInstance trileInstance2 = null;
		InstancePhysicsState physicsState = x.Instance.PhysicsState;
		while (physicsState.Grounded && physicsState.Ground.First.PhysicsState != null && physicsState.Ground.First != x.Instance && physicsState.Ground.First != trileInstance2)
		{
			trileInstance2 = trileInstance;
			num++;
			trileInstance = physicsState.Ground.First;
			physicsState = trileInstance.PhysicsState;
		}
		physicsState = y.Instance.PhysicsState;
		trileInstance = (trileInstance2 = null);
		while (physicsState.Grounded && physicsState.Ground.First.PhysicsState != null && physicsState.Ground.First != y.Instance && physicsState.Ground.First != trileInstance2)
		{
			trileInstance2 = trileInstance;
			num2++;
			trileInstance = physicsState.Ground.First;
			physicsState = trileInstance.PhysicsState;
		}
		if (num - num2 == 0)
		{
			Vector3 vector = x.Instance.PhysicsState.Velocity.Sign() * FezMath.XZMask;
			if (vector == y.Instance.PhysicsState.Velocity.Sign() * FezMath.XZMask)
			{
				return Math.Sign((x.Instance.Position - y.Instance.Position).Dot(vector));
			}
			return Math.Sign(x.Instance.Position.Y - y.Instance.Position.Y);
		}
		return num - num2;
	}
}
