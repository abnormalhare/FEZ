using System;
using FezEngine.Components;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Tools;
using FezGame.Services;
using Microsoft.Xna.Framework;

namespace FezGame.Tools;

public class MovementHelper
{
	private const float RunInputThreshold = 0.5f;

	public float WalkAcceleration { get; set; }

	public float RunAcceleration { get; set; }

	public float RunTimeThreshold { get; set; }

	public float RunTime { get; private set; }

	public IPhysicsEntity Entity { private get; set; }

	public bool Running => RunTime > RunTimeThreshold;

	[ServiceDependency]
	public ICollisionManager CollisionManager { protected get; set; }

	[ServiceDependency]
	public IGameCameraManager CameraManager { protected get; set; }

	[ServiceDependency]
	public IDebuggingBag DebuggingBag { protected get; set; }

	[ServiceDependency]
	public IInputManager InputProvider { protected get; set; }

	public MovementHelper(float walkAcceleration, float runAcceleration, float runTimeThreshold)
	{
		WalkAcceleration = walkAcceleration;
		RunAcceleration = runAcceleration;
		RunTimeThreshold = runTimeThreshold;
		ServiceHelper.InjectServices(this);
	}

	public void Update(float elapsedSeconds)
	{
		Update(elapsedSeconds, InputProvider.Movement.X);
	}

	public void Update(float elapsedSeconds, float input)
	{
		if (Math.Abs(input) > 0.5f)
		{
			RunTime += elapsedSeconds;
		}
		else
		{
			RunTime = 0f;
		}
		Vector3 vector = Vector3.Transform(new Vector3(input, 0f, 0f), CameraManager.Rotation);
		float num = (Running ? RunAcceleration : WalkAcceleration);
		Entity.Velocity += vector * 0.15f * num * elapsedSeconds * (0.5f + Math.Abs(CollisionManager.GravityFactor) * 1.5f) / 2f;
	}

	public void Reset()
	{
		RunTime = 0f;
	}
}
