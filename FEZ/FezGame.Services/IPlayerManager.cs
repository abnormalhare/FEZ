using System;
using System.Collections.Generic;
using FezEngine;
using FezEngine.Structure;
using FezGame.Components;
using FezGame.Structure;
using Microsoft.Xna.Framework;

namespace FezGame.Services;

public interface IPlayerManager : IComplexPhysicsEntity, IPhysicsEntity
{
	List<Volume> CurrentVolumes { get; }

	bool CanRotate { get; set; }

	ActionType Action { get; set; }

	ActionType LastAction { get; set; }

	ActionType NextAction { get; set; }

	bool CanDoubleJump { get; set; }

	TrileInstance HeldInstance { get; set; }

	TrileInstance CarriedInstance { get; set; }

	TrileInstance PushedInstance { get; set; }

	Vector3 Position { get; set; }

	Vector3 RespawnPosition { get; }

	Vector3 LeaveGroundPosition { get; set; }

	float OffsetAtLeaveGround { get; }

	TrileInstance CheckpointGround { get; set; }

	HorizontalDirection LookingDirection { get; set; }

	GomezHost MeshHost { get; set; }

	bool CanControl { get; set; }

	bool DoorEndsTrial { get; set; }

	TimeSpan AirTime { get; set; }

	string NextLevel { get; set; }

	int? DoorVolume { get; set; }

	int? PipeVolume { get; set; }

	int? TunnelVolume { get; set; }

	AnimatedTexture Animation { get; set; }

	bool IgnoreFreefall { get; set; }

	bool SpinThroughDoor { get; set; }

	bool Hidden { get; set; }

	bool IsOnRotato { get; set; }

	TrileInstance ForcedTreasure { get; set; }

	bool HideFez { get; set; }

	bool InDoorTransition { get; set; }

	WarpPanel WarpPanel { get; set; }

	Viewpoint OriginWarpViewpoint { get; set; }

	float GomezOpacity { get; set; }

	bool FullBright { get; set; }

	float BlinkSpeed { get; set; }

	Vector3 SplitUpCubeCollectorOffset { get; set; }

	bool FreshlyRespawned { get; set; }

	void Reset();

	void SyncCollisionSize();

	void RecordRespawnInformation();

	void RecordRespawnInformation(bool markCheckpoint);

	void Respawn();

	void RespawnAtCheckpoint();

	void ForceOverlapsDetermination();

	AnimatedTexture GetAnimation(ActionType type);

	void FillAnimations();

	void CopyTo(IPlayerManager other);
}
