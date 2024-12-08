using System;
using System.Linq;
using FezEngine;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Tools;
using FezGame.Services;
using Microsoft.Xna.Framework;

namespace FezGame.Components;

internal class HeavyGroupState
{
	private readonly TrileGroup group;

	private readonly TrileInstance[] bottomTriles;

	private bool moving;

	private bool velocityNeedsReset;

	[ServiceDependency]
	public ILevelManager LevelManager { private get; set; }

	[ServiceDependency]
	public ICollisionManager CollisionManager { private get; set; }

	public HeavyGroupState(TrileGroup group)
	{
		ServiceHelper.InjectServices(this);
		this.group = group;
		int minY = group.Triles.Min((TrileInstance x) => x.Emplacement.Y);
		group.Triles.Sort((TrileInstance a, TrileInstance b) => a.Emplacement.Y - b.Emplacement.Y);
		bottomTriles = group.Triles.Where((TrileInstance x) => x.Emplacement.Y == minY).ToArray();
		foreach (TrileInstance trile in group.Triles)
		{
			trile.PhysicsState = new InstancePhysicsState(trile);
		}
		MarkGrounds();
	}

	private void MarkGrounds()
	{
		foreach (TrileInstance trile in group.Triles)
		{
			TrileInstance nearLow = LevelManager.ActualInstanceAt(trile.Center - trile.Trile.Size.Y * Vector3.UnitY);
			trile.PhysicsState.Ground = new MultipleHits<TrileInstance>
			{
				NearLow = nearLow
			};
			trile.IsMovingGroup = false;
		}
	}

	public void Update(TimeSpan elapsed)
	{
		TrileInstance[] array;
		if (!moving)
		{
			if (velocityNeedsReset)
			{
				foreach (TrileInstance trile in group.Triles)
				{
					trile.PhysicsState.Velocity = Vector3.Zero;
				}
				velocityNeedsReset = false;
			}
			bool flag = false;
			array = bottomTriles;
			for (int i = 0; i < array.Length; i++)
			{
				TrileInstance first = array[i].PhysicsState.Ground.First;
				flag |= first != null && first.Enabled && (first.PhysicsState == null || first.PhysicsState.Grounded);
			}
			if (!flag)
			{
				moving = true;
				foreach (TrileInstance trile2 in group.Triles)
				{
					trile2.PhysicsState.Ground = default(MultipleHits<TrileInstance>);
					trile2.IsMovingGroup = true;
				}
			}
		}
		if (!moving)
		{
			return;
		}
		Vector3 vector = 0.47250003f * (float)elapsed.TotalSeconds * -Vector3.UnitY;
		foreach (TrileInstance trile3 in group.Triles)
		{
			trile3.PhysicsState.UpdatingPhysics = true;
		}
		bool flag2 = false;
		Vector3 vector2 = Vector3.Zero;
		array = bottomTriles;
		foreach (TrileInstance trileInstance in array)
		{
			MultipleHits<CollisionResult> multipleHits = CollisionManager.CollideEdge(trileInstance.Center, trileInstance.PhysicsState.Velocity + vector, trileInstance.TransformedSize / 2f, Direction2D.Vertical);
			if (multipleHits.First.Collided)
			{
				flag2 = true;
				vector2 = Vector3.Max(vector2, multipleHits.First.Response);
			}
		}
		vector += vector2;
		foreach (TrileInstance trile4 in group.Triles)
		{
			trile4.Position += (trile4.PhysicsState.Velocity += vector);
			LevelManager.UpdateInstance(trile4);
			trile4.PhysicsState.UpdatingPhysics = false;
		}
		if (flag2)
		{
			MarkGrounds();
			moving = false;
			velocityNeedsReset = true;
		}
	}
}
