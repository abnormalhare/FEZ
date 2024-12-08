using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using FezEngine.Services;
using FezEngine.Services.Scripting;
using FezEngine.Structure;
using FezEngine.Structure.Geometry;
using FezEngine.Tools;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace FezEngine.Components;

public class MovingGroupsHost : DrawableGameComponent
{
	protected class MovingGroupState
	{
		public readonly TrileGroup Group;

		public readonly bool IsConnective;

		private readonly Vector3[] referenceOrigins;

		private readonly bool neededTrigger;

		private SoundEmitter eAssociatedSound;

		private SoundEmitter eConnectiveIdle;

		private readonly SoundEffect sConnectiveKlonk;

		private readonly SoundEffect sConnectiveStartUp;

		public int CurrentSegmentIndex;

		private Vector3 segmentOrigin;

		private Vector3 lastSegmentOrigin;

		private TimeSpan sinceSegmentStarted;

		private bool paused;

		private readonly ArtObjectInstance[] attachedAOs;

		private readonly BackgroundPlane[] attachedPlanes;

		private readonly Vector3[] aoOrigins;

		private readonly Vector3[] planeOrigins;

		private readonly Vector3 MarkedPosition;

		private readonly List<PathSegment> Segments;

		private bool Silent;

		private bool SkipStartUp;

		private float SinceKlonk = 1f;

		private Vector3 lastVelocity;

		public bool Enabled { get; set; }

		private MovementPath Path => Group.Path;

		public PathSegment CurrentSegment => Segments[CurrentSegmentIndex];

		[ServiceDependency]
		public ILevelManager LevelManager { private get; set; }

		[ServiceDependency]
		public IContentManagerProvider CMProvider { private get; set; }

		public MovingGroupState(TrileGroup group, bool connective)
		{
			LevelManager = ServiceHelper.Get<ILevelManager>();
			CMProvider = ServiceHelper.Get<IContentManagerProvider>();
			Group = group;
			IsConnective = connective;
			neededTrigger = Path.NeedsTrigger;
			paused = neededTrigger;
			referenceOrigins = new Vector3[group.Triles.Count];
			MarkedPosition = Vector3.Zero;
			int num = 0;
			foreach (TrileInstance trile in group.Triles)
			{
				MarkedPosition += trile.Center;
				referenceOrigins[num++] = trile.Position;
			}
			MarkedPosition /= (float)group.Triles.Count;
			if (!group.PhysicsInitialized)
			{
				foreach (TrileInstance trile2 in group.Triles)
				{
					trile2.PhysicsState = new InstancePhysicsState(trile2);
				}
				group.PhysicsInitialized = true;
			}
			Segments = new List<PathSegment>(Path.Segments);
			foreach (TrileInstance trile3 in group.Triles)
			{
				trile3.ForceSeeThrough = true;
			}
			if (Path.EndBehavior == PathEndBehavior.Bounce)
			{
				for (int num2 = Segments.Count - 1; num2 >= 0; num2--)
				{
					PathSegment pathSegment = Segments[num2];
					Vector3 destination = ((num2 == 0) ? Vector3.Zero : Segments[num2 - 1].Destination);
					Segments.Add(new PathSegment
					{
						Acceleration = pathSegment.Deceleration,
						Deceleration = pathSegment.Acceleration,
						Destination = destination,
						Duration = pathSegment.Duration,
						JitterFactor = pathSegment.JitterFactor,
						WaitTimeOnFinish = pathSegment.WaitTimeOnStart,
						WaitTimeOnStart = pathSegment.WaitTimeOnFinish,
						Bounced = true
					});
				}
			}
			if (Path.SoundName != null)
			{
				try
				{
					SoundEffect soundEffect = CMProvider.CurrentLevel.Load<SoundEffect>("Sounds/" + Path.SoundName);
					eAssociatedSound = soundEffect.EmitAt(Vector3.Zero, loop: true, paused: true);
				}
				catch (Exception)
				{
					Logger.Log("Moving groups", LogSeverity.Warning, "Could not find sound " + Path.SoundName);
					Path.SoundName = null;
				}
			}
			if (IsConnective)
			{
				SoundEffect soundEffect2 = CMProvider.CurrentLevel.Load<SoundEffect>("Sounds/Industrial/ConnectiveIdle");
				eConnectiveIdle = soundEffect2.EmitAt(Vector3.Zero, loop: true, paused: true);
				sConnectiveKlonk = CMProvider.CurrentLevel.Load<SoundEffect>("Sounds/Industrial/ConnectiveKlonk");
				sConnectiveStartUp = CMProvider.CurrentLevel.Load<SoundEffect>("Sounds/Industrial/ConnectiveStartUp");
			}
			if (!IsConnective)
			{
				attachedAOs = LevelManager.ArtObjects.Values.Where((ArtObjectInstance x) => x.ActorSettings.AttachedGroup == group.Id).ToArray();
				if (attachedAOs.Length != 0)
				{
					aoOrigins = attachedAOs.Select((ArtObjectInstance x) => x.Position).ToArray();
				}
				attachedPlanes = LevelManager.BackgroundPlanes.Values.Where((BackgroundPlane x) => x.AttachedGroup == group.Id).ToArray();
				if (attachedPlanes.Length != 0)
				{
					planeOrigins = attachedPlanes.Select((BackgroundPlane x) => x.Position).ToArray();
				}
			}
			else
			{
				foreach (TrileInstance trile4 in Group.Triles)
				{
					trile4.ForceClampToGround = true;
				}
			}
			Enabled = true;
			Silent = true;
			Reset();
			StartNewSegment();
			Silent = false;
			sinceSegmentStarted -= TimeSpan.FromSeconds(Path.OffsetSeconds);
		}

		public void MoveTo(MovingGroupState oldTrack)
		{
			Silent = true;
			bool enabled = Enabled;
			Enabled = true;
			for (int i = 0; i < Segments.Count; i++)
			{
				if (FezMath.AlmostEqual(MarkedPosition + segmentOrigin, oldTrack.MarkedPosition + oldTrack.segmentOrigin) && CurrentSegment.Bounced == oldTrack.CurrentSegment.Bounced)
				{
					break;
				}
				ChangeSegment();
			}
			if (CurrentSegment.Duration != TimeSpan.Zero)
			{
				sinceSegmentStarted = oldTrack.sinceSegmentStarted;
			}
			Update(TimeSpan.Zero);
			Enabled = enabled;
			Silent = false;
		}

		public void MoveToEnd()
		{
			Silent = true;
			bool enabled = Enabled;
			PathEndBehavior endBehavior = Path.EndBehavior;
			int count = Segments.Count;
			if (endBehavior != 0)
			{
				for (int num = count - 1; num >= 0; num--)
				{
					PathSegment pathSegment = Segments[num];
					Vector3 destination = ((num == 0) ? Vector3.Zero : Segments[num - 1].Destination);
					Segments.Add(new PathSegment
					{
						Acceleration = pathSegment.Deceleration,
						Deceleration = pathSegment.Acceleration,
						Destination = destination,
						Duration = pathSegment.Duration,
						JitterFactor = pathSegment.JitterFactor,
						WaitTimeOnFinish = pathSegment.WaitTimeOnStart,
						WaitTimeOnStart = pathSegment.WaitTimeOnFinish,
						Bounced = true
					});
				}
			}
			Enabled = true;
			Path.NeedsTrigger = false;
			Path.EndBehavior = PathEndBehavior.Bounce;
			while (!CurrentSegment.Bounced)
			{
				ChangeSegment();
			}
			sinceSegmentStarted = TimeSpan.Zero;
			Update(TimeSpan.Zero);
			Path.EndBehavior = endBehavior;
			Enabled = enabled;
			Path.NeedsTrigger = neededTrigger;
			Silent = false;
			if (endBehavior != 0)
			{
				Segments.RemoveRange(count, Segments.Count - count);
			}
		}

		public void Reset()
		{
			if (CurrentSegmentIndex >= 0 && CurrentSegmentIndex < Segments.Count)
			{
				lastSegmentOrigin = segmentOrigin;
				segmentOrigin = CurrentSegment.Destination;
			}
			if (lastSegmentOrigin != Vector3.Zero)
			{
				Group.Triles.Sort(new MovingTrileInstanceComparer(-lastSegmentOrigin));
				Array.Sort(referenceOrigins, new MovingPositionComparer(-lastSegmentOrigin));
				for (int i = 0; i < Group.Triles.Count; i++)
				{
					Group.Triles[i].PhysicsState.Velocity = Vector3.Zero;
					Group.Triles[i].Position = (referenceOrigins[i] -= lastSegmentOrigin);
					LevelManager.UpdateInstance(Group.Triles[i]);
				}
			}
			if (aoOrigins != null)
			{
				for (int j = 0; j < aoOrigins.Length; j++)
				{
					attachedAOs[j].Position = aoOrigins[j];
				}
			}
			if (planeOrigins != null)
			{
				for (int k = 0; k < planeOrigins.Length; k++)
				{
					attachedPlanes[k].Position = planeOrigins[k];
				}
			}
			CurrentSegmentIndex = 0;
			lastSegmentOrigin = (segmentOrigin = Vector3.Zero);
			sinceSegmentStarted = TimeSpan.Zero;
			if (Path.Backwards)
			{
				Path.Backwards = false;
				while (CurrentSegmentIndex != Segments.Count - 1)
				{
					ChangeSegment();
				}
				Path.Backwards = true;
				sinceSegmentStarted = CurrentSegment.Duration + CurrentSegment.WaitTimeOnFinish;
			}
		}

		public void Update(TimeSpan elapsed)
		{
			if (!Enabled)
			{
				return;
			}
			if (Path.NeedsTrigger)
			{
				if (lastVelocity != Vector3.Zero)
				{
					for (int i = 0; i < Group.Triles.Count; i++)
					{
						Group.Triles[i].PhysicsState.Velocity = Vector3.Zero;
					}
					lastVelocity = Vector3.Zero;
					PauseSound();
				}
				return;
			}
			if (!LevelManager.Groups.ContainsKey(Group.Id))
			{
				Enabled = false;
				return;
			}
			if (eAssociatedSound != null)
			{
				if (lastVelocity != Vector3.Zero && eAssociatedSound.Cue.State == SoundState.Paused)
				{
					eAssociatedSound.Cue.Resume();
				}
				eAssociatedSound.Position = Group.Triles[Group.Triles.Count / 2].Center;
			}
			if (IsConnective)
			{
				if (lastVelocity != Vector3.Zero && eConnectiveIdle.Cue.State == SoundState.Paused)
				{
					if (IsConnective && !Silent && !SkipStartUp)
					{
						sConnectiveStartUp.EmitAt(eConnectiveIdle.Position);
					}
					eConnectiveIdle.Cue.Resume();
					SkipStartUp = false;
				}
				eConnectiveIdle.Position = Group.Triles[Group.Triles.Count / 2].Center;
			}
			SinceKlonk += (float)elapsed.TotalSeconds;
			if (neededTrigger && (CurrentSegmentIndex == Segments.Count || CurrentSegmentIndex == -1))
			{
				Reset();
				if (!Path.Backwards)
				{
					StartNewSegment();
				}
			}
			if (neededTrigger && Path.RunSingleSegment && paused)
			{
				for (int j = 0; j < Group.Triles.Count; j++)
				{
					referenceOrigins[j] = Group.Triles[j].Position - segmentOrigin;
				}
				paused = false;
			}
			TimeSpan timeSpan = sinceSegmentStarted;
			if (Path.Backwards)
			{
				sinceSegmentStarted -= elapsed;
			}
			else
			{
				sinceSegmentStarted += elapsed;
			}
			if (timeSpan.TotalSeconds < 0.0 && sinceSegmentStarted.TotalSeconds >= 0.0 && Group.ActorType == ActorType.Piston)
			{
				if (CurrentSegment.Destination.Y >= 2f)
				{
					CMProvider.Global.Load<SoundEffect>("Sounds/MiscActors/Piston").EmitAt(Group.Triles[Group.Triles.Count / 2].Center, RandomHelper.Centered(0.10000000149011612));
				}
				else
				{
					CMProvider.Global.Load<SoundEffect>("Sounds/MiscActors/PistonSetBack").EmitAt(Group.Triles[Group.Triles.Count / 2].Center, RandomHelper.Centered(0.10000000149011612));
				}
			}
			float num = (float)FezMath.Saturate(sinceSegmentStarted.TotalSeconds / CurrentSegment.Duration.TotalSeconds);
			if (float.IsNaN(num))
			{
				num = 1f;
			}
			float num2 = ((CurrentSegment.Deceleration == 0f && CurrentSegment.Acceleration == 0f) ? num : ((CurrentSegment.Acceleration == 0f) ? Easing.Ease(num, 0f - CurrentSegment.Deceleration, EasingType.Quadratic) : ((CurrentSegment.Deceleration != 0f) ? Easing.EaseInOut(num, EasingType.Sine, CurrentSegment.Acceleration, EasingType.Sine, CurrentSegment.Deceleration) : Easing.Ease(num, CurrentSegment.Acceleration, EasingType.Quadratic))));
			if (Path.Segments.Count == 1 && Path.EndBehavior == PathEndBehavior.Stop && eAssociatedSound != null && !eAssociatedSound.Dead)
			{
				eAssociatedSound.VolumeFactor = 1f - Easing.EaseIn(num2, EasingType.Quartic) * 0.825f;
			}
			Vector3 vector = (CurrentSegment.Destination - segmentOrigin) * num2;
			float num3 = CurrentSegment.JitterFactor;
			if (elapsed.Ticks == 0L)
			{
				num3 = 0f;
			}
			if (((!Path.Backwards && sinceSegmentStarted < CurrentSegment.Duration) || (Path.Backwards && sinceSegmentStarted.Ticks > 0)) && num3 > 0f)
			{
				vector += new Vector3(RandomHelper.Centered(num3) * 0.5f, RandomHelper.Centered(num3) * 0.5f, RandomHelper.Centered(num3) * 0.5f);
			}
			Vector3 vector2 = (lastVelocity = referenceOrigins[0] + vector - Group.Triles[0].Position);
			if (vector2 == Vector3.Zero)
			{
				PauseSound();
			}
			foreach (TrileInstance trile in Group.Triles)
			{
				trile.PhysicsState.Sticky = num3 != 0f && ((double)num < 0.1 || (double)num > 0.9);
				trile.PhysicsState.Velocity = ((elapsed.TotalSeconds == 0.0) ? Vector3.Zero : vector2);
				trile.Position += vector2;
				LevelManager.UpdateInstance(trile);
			}
			if (elapsed.Ticks != 0L || Silent)
			{
				if (aoOrigins != null)
				{
					for (int k = 0; k < aoOrigins.Length; k++)
					{
						attachedAOs[k].Position += vector2;
					}
				}
				if (planeOrigins != null)
				{
					for (int l = 0; l < planeOrigins.Length; l++)
					{
						attachedPlanes[l].Position = planeOrigins[l];
					}
				}
			}
			if ((!Path.Backwards && sinceSegmentStarted >= CurrentSegment.Duration + CurrentSegment.WaitTimeOnFinish) || (Path.Backwards && sinceSegmentStarted <= -CurrentSegment.WaitTimeOnStart))
			{
				ChangeSegment();
			}
			if (!Enabled || Path.NeedsTrigger)
			{
				PauseSound();
			}
		}

		public void PauseSound()
		{
			if (eAssociatedSound != null && eAssociatedSound.Cue.State == SoundState.Playing)
			{
				eAssociatedSound.Cue.Pause();
			}
			if (IsConnective && eConnectiveIdle.Cue.State == SoundState.Playing)
			{
				eConnectiveIdle.Cue.Pause();
			}
		}

		public void StopSound()
		{
			if (eAssociatedSound != null && !eAssociatedSound.Dead)
			{
				eAssociatedSound.FadeOutAndDie(0.25f);
				eAssociatedSound = null;
			}
			if (eConnectiveIdle != null && !eConnectiveIdle.Dead)
			{
				eConnectiveIdle.FadeOutAndDie(0.1f);
				eConnectiveIdle = null;
			}
		}

		private void ChangeSegment()
		{
			if (Path.Backwards)
			{
				if (CurrentSegmentIndex == 0)
				{
					CurrentSegmentIndex--;
					EndPath();
					if (Enabled && !Path.NeedsTrigger)
					{
						StartNewSegment();
					}
				}
				else
				{
					Path.Backwards = false;
					int num = CurrentSegmentIndex - 1;
					Reset();
					while (CurrentSegmentIndex != num)
					{
						ChangeSegment();
					}
					Path.Backwards = true;
					sinceSegmentStarted = CurrentSegment.Duration + CurrentSegment.WaitTimeOnFinish;
					Update(TimeSpan.Zero);
				}
				if (Path.RunSingleSegment)
				{
					Path.NeedsTrigger = true;
					Path.RunSingleSegment = false;
				}
				return;
			}
			lastSegmentOrigin = segmentOrigin;
			segmentOrigin = CurrentSegment.Destination;
			sinceSegmentStarted -= CurrentSegment.Duration + CurrentSegment.WaitTimeOnFinish;
			CurrentSegmentIndex++;
			if (CurrentSegmentIndex == Segments.Count || CurrentSegmentIndex == -1)
			{
				EndPath();
			}
			if (Enabled && !Path.NeedsTrigger)
			{
				StartNewSegment();
			}
			if (Path.RunSingleSegment)
			{
				Path.NeedsTrigger = true;
				Path.RunSingleSegment = false;
			}
			if (Group.ActorType == ActorType.Piston)
			{
				if (CurrentSegment.Destination.Y >= 2f && LevelManager.Name != "LAVA_FORK")
				{
					CMProvider.Global.Load<SoundEffect>("Sounds/MiscActors/Piston").EmitAt(Group.Triles[Group.Triles.Count / 2].Center, RandomHelper.Centered(0.10000000149011612));
				}
				else
				{
					CMProvider.Global.Load<SoundEffect>("Sounds/MiscActors/PistonSetBack").EmitAt(Group.Triles[Group.Triles.Count / 2].Center, RandomHelper.Centered(0.10000000149011612));
				}
			}
		}

		private void StartNewSegment()
		{
			if (Path.Backwards)
			{
				Group.Triles.Sort(new MovingTrileInstanceComparer(CurrentSegment.Destination - segmentOrigin));
				Array.Sort(referenceOrigins, new MovingPositionComparer(CurrentSegment.Destination - segmentOrigin));
				Vector3 vector = lastSegmentOrigin - segmentOrigin;
				for (int i = 0; i < Group.Triles.Count; i++)
				{
					referenceOrigins[i] += vector;
				}
				sinceSegmentStarted += CurrentSegment.Duration + CurrentSegment.WaitTimeOnFinish;
			}
			else
			{
				Vector3 vector2 = segmentOrigin - lastSegmentOrigin;
				Vector3 ordering = ((CurrentSegmentIndex == Segments.Count) ? vector2 : (CurrentSegment.Destination - segmentOrigin));
				Group.Triles.Sort(new MovingTrileInstanceComparer(ordering));
				Array.Sort(referenceOrigins, new MovingPositionComparer(ordering));
				for (int j = 0; j < Group.Triles.Count; j++)
				{
					referenceOrigins[j] += vector2;
				}
				if (CurrentSegmentIndex < Segments.Count)
				{
					sinceSegmentStarted -= CurrentSegment.WaitTimeOnStart;
				}
			}
			if (!IsConnective || Silent)
			{
				return;
			}
			if (SinceKlonk < 0.1f)
			{
				Waiters.Wait(0.10000000149011612, delegate
				{
					if (eConnectiveIdle != null)
					{
						sConnectiveKlonk.EmitAt(eConnectiveIdle.Position, 0f, 0.1f);
					}
				});
			}
			else
			{
				sConnectiveKlonk.EmitAt(eConnectiveIdle.Position);
			}
			SinceKlonk = 0f;
		}

		private void EndPath()
		{
			foreach (TrileInstance trile in Group.Triles)
			{
				trile.PhysicsState.Velocity = Vector3.Zero;
			}
			if (Path.EndBehavior == PathEndBehavior.Stop)
			{
				if (neededTrigger && Path.RunOnce)
				{
					Path.NeedsTrigger = true;
					Path.RunOnce = false;
				}
				else
				{
					Enabled = false;
				}
			}
			else if (neededTrigger && Path.RunOnce)
			{
				Path.NeedsTrigger = true;
				Path.RunOnce = false;
			}
			else
			{
				Reset();
			}
		}

		public void PrepareOldTrack(bool wasRunning)
		{
			SkipStartUp = true;
			if (wasRunning)
			{
				eConnectiveIdle.Cue.Resume();
			}
		}
	}

	protected readonly List<MovingGroupState> trackedGroups = new List<MovingGroupState>();

	protected bool DontTrack;

	private readonly Dictionary<int, MovingGroupState> enablingVolumes = new Dictionary<int, MovingGroupState>();

	private int firstAvailableVolumeId;

	private readonly List<ArtObjectInstance> connectedAos = new List<ArtObjectInstance>();

	[ServiceDependency]
	public ILevelManager LevelManager { private get; set; }

	[ServiceDependency]
	public ILevelMaterializer LevelMaterializer { private get; set; }

	[ServiceDependency]
	public IEngineStateManager EngineState { private get; set; }

	[ServiceDependency]
	public IDefaultCameraManager CameraManager { private get; set; }

	[ServiceDependency(Optional = true)]
	public IVolumeService VolumeService { private get; set; }

	[ServiceDependency]
	public ILightingPostProcess LightingPostProcess { private get; set; }

	public MovingGroupsHost(Game game)
		: base(game)
	{
		base.UpdateOrder = -2;
	}

	public override void Initialize()
	{
		base.Initialize();
		base.Enabled = false;
		LevelManager.LevelChanging += TrackNewGroups;
		LevelManager.LevelChanged += PostTrackNewGroups;
		TimeInterpolation.RegisterCallback(InterpolationCallback, 0);
		Waiters.Wait(0.10000000149011612, DelayedHook);
	}

	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);
	}

	private void DelayedHook()
	{
		CameraManager.PreViewpointChanged += TrackConnectivePaths;
	}

	private void TrackConnectivePaths()
	{
		if (!CameraManager.ViewTransitionCancelled)
		{
			TrackConnectivePaths(update: true);
		}
	}

	private void TrackConnectivePaths(bool update)
	{
		if (DontTrack || (update && EngineState.Loading) || CameraManager.Viewpoint == Viewpoint.Perspective || (CameraManager.Viewpoint == CameraManager.LastViewpoint && update))
		{
			return;
		}
		List<MovingGroupState> list = new List<MovingGroupState>();
		for (int num = trackedGroups.Count - 1; num >= 0; num--)
		{
			if (trackedGroups[num].IsConnective)
			{
				if (!trackedGroups[num].Group.MoveToEnd)
				{
					list.Add(trackedGroups[num]);
				}
				trackedGroups[num].StopSound();
				trackedGroups.RemoveAt(num);
			}
		}
		foreach (ArtObjectInstance levelArtObject in LevelMaterializer.LevelArtObjects)
		{
			if (levelArtObject.ArtObject.ActorType != ActorType.ConnectiveRail || !levelArtObject.ActorSettings.AttachedGroup.HasValue)
			{
				continue;
			}
			bool flag = false;
			foreach (MovingGroupState trackedGroup in trackedGroups)
			{
				flag |= trackedGroup.Group.Id == levelArtObject.ActorSettings.AttachedGroup;
				if (flag)
				{
					break;
				}
			}
			if (!flag)
			{
				TrackGroupConnectivePath(levelArtObject.ActorSettings.AttachedGroup.Value);
			}
		}
		if (list.Count > 0)
		{
			foreach (MovingGroupState trackedGroup2 in trackedGroups)
			{
				if (!trackedGroup2.IsConnective)
				{
					continue;
				}
				MovingGroupState movingGroupState = null;
				foreach (MovingGroupState item in list)
				{
					if (item.Group == trackedGroup2.Group)
					{
						movingGroupState = item;
						break;
					}
				}
				if (movingGroupState != null)
				{
					trackedGroup2.Enabled = movingGroupState.Enabled;
					trackedGroup2.MoveTo(movingGroupState);
					trackedGroup2.PrepareOldTrack(movingGroupState.Enabled && !movingGroupState.Group.Path.NeedsTrigger);
				}
			}
		}
		foreach (int key in enablingVolumes.Keys)
		{
			LevelManager.Volumes.Remove(key);
		}
		enablingVolumes.Clear();
		int num2 = firstAvailableVolumeId;
		foreach (MovingGroupState trackedGroup3 in trackedGroups)
		{
			if (!trackedGroup3.IsConnective || trackedGroup3.Enabled)
			{
				continue;
			}
			Vector3 value = new Vector3(float.MaxValue);
			Vector3 value2 = new Vector3(float.MinValue);
			foreach (TrileInstance trile in trackedGroup3.Group.Triles)
			{
				value = Vector3.Min(value, trile.Position);
				value2 = Vector3.Max(value2, trile.Position);
			}
			Volume volume = new Volume
			{
				Id = num2++,
				From = new Vector3(value.X, value.Y + 1f, value.Z),
				To = new Vector3(value2.X + 1f, value2.Y + 2f, value2.Z + 1f),
				Orientations = 
				{
					FaceOrientation.Left,
					FaceOrientation.Right,
					FaceOrientation.Back,
					FaceOrientation.Front
				},
				Enabled = true
			};
			LevelManager.Volumes.Add(volume.Id, volume);
			enablingVolumes.Add(volume.Id, trackedGroup3);
		}
		if (VolumeService != null)
		{
			VolumeService.RegisterNeeded = true;
		}
	}

	private void TryEnableConnectivePath(int volumeId)
	{
		if (enablingVolumes.TryGetValue(volumeId, out var value))
		{
			value.Enabled = true;
		}
	}

	private void TrackGroupConnectivePath(int groupId)
	{
		Vector3 vector = CameraManager.Viewpoint.ScreenSpaceMask();
		Vector3 vector2 = CameraManager.Viewpoint.ForwardVector().Abs();
		connectedAos.Clear();
		foreach (ArtObjectInstance levelArtObject in LevelMaterializer.LevelArtObjects)
		{
			if (levelArtObject.ActorSettings.AttachedGroup == groupId)
			{
				connectedAos.Add(levelArtObject);
			}
		}
		foreach (ArtObjectInstance connectedAo in connectedAos)
		{
			if (connectedAo.ActorSettings.NextNode.HasValue)
			{
				connectedAo.ActorSettings.NextNodeAo = LevelManager.ArtObjects[connectedAo.ActorSettings.NextNode.Value];
			}
			foreach (ArtObjectInstance connectedAo2 in connectedAos)
			{
				if (connectedAo2.ActorSettings.NextNode.HasValue && connectedAo2.ActorSettings.NextNode.Value == connectedAo.Id)
				{
					connectedAo.ActorSettings.PrecedingNodeAo = connectedAo2;
				}
			}
		}
		if (!LevelManager.Groups.TryGetValue(groupId, out var value))
		{
			Logger.Log("MovingGroupsHost::TrackGroupConnectivePath", LogSeverity.Warning, "Node is connected to a group that doesn't exist!");
			return;
		}
		if (value.MoveToEnd)
		{
			ArtObjectInstance artObjectInstance = null;
			foreach (ArtObjectInstance connectedAo3 in connectedAos)
			{
				if (!connectedAo3.ActorSettings.NextNode.HasValue && connectedAo3.ArtObject.ActorType == ActorType.ConnectiveRail)
				{
					artObjectInstance = connectedAo3;
					break;
				}
			}
			if (artObjectInstance == null)
			{
				throw new InvalidOperationException("No end-node! Can't move to end.");
			}
			Vector3 zero = Vector3.Zero;
			foreach (TrileInstance trile in value.Triles)
			{
				zero += trile.Center;
			}
			zero /= (float)value.Triles.Count;
			Vector3 vector3 = artObjectInstance.Position - zero;
			value.Triles.Sort(new MovingTrileInstanceComparer(vector3));
			foreach (TrileInstance trile2 in value.Triles)
			{
				trile2.Position += vector3;
				LevelManager.UpdateInstance(trile2);
			}
		}
		BoundingBox boundingBox = new BoundingBox(new Vector3(float.MaxValue), new Vector3(float.MinValue));
		Vector3 zero2 = Vector3.Zero;
		foreach (TrileInstance trile3 in value.Triles)
		{
			Vector3 vector4 = trile3.TransformedSize / 2f;
			boundingBox.Min = Vector3.Min(boundingBox.Min, (trile3.Center - vector4) * vector);
			boundingBox.Max = Vector3.Max(boundingBox.Max, (trile3.Center + vector4) * vector + vector2);
			zero2 += trile3.Center;
		}
		zero2 /= (float)value.Triles.Count;
		ArtObjectInstance artObjectInstance2 = null;
		BoundingBox boundingBox2 = default(BoundingBox);
		foreach (ArtObjectInstance connectedAo4 in connectedAos)
		{
			if (connectedAo4.ArtObject.ActorType == ActorType.None)
			{
				Vector3 vector4 = connectedAo4.Scale * connectedAo4.ArtObject.Size / 2f;
				boundingBox2 = new BoundingBox(connectedAo4.Position - vector4, connectedAo4.Position + vector4);
				Quaternion quaternion = connectedAo4.Rotation;
				FezMath.RotateOnCenter(ref boundingBox2, ref quaternion);
				boundingBox2.Min *= vector;
				boundingBox2.Max = boundingBox2.Max * vector + vector2;
				if (boundingBox.Intersects(boundingBox2))
				{
					break;
				}
			}
		}
		ArtObjectInstance artObjectInstance3 = null;
		foreach (ArtObjectInstance connectedAo5 in connectedAos)
		{
			if (connectedAo5.ArtObject.ActorType == ActorType.ConnectiveRail)
			{
				Vector3 vector4 = connectedAo5.Scale * connectedAo5.ArtObject.Size / 2f;
				BoundingBox box = new BoundingBox((connectedAo5.Position - vector4) * vector, (connectedAo5.Position + vector4) * vector + vector2);
				if (boundingBox2.Intersects(box))
				{
					artObjectInstance3 = connectedAo5;
					break;
				}
			}
		}
		if (artObjectInstance3 == null)
		{
			InvalidOperationException ex = new InvalidOperationException("Nodeless branch!");
			Logger.Log("Connective Groups", LogSeverity.Warning, ex.Message);
			throw ex;
		}
		while (true)
		{
			if (artObjectInstance3.ActorSettings.PrecedingNodeAo == null)
			{
				artObjectInstance2 = artObjectInstance3;
				break;
			}
			Vector3 vector5 = artObjectInstance3.ActorSettings.PrecedingNodeAo.Position - artObjectInstance3.Position;
			if (!(Math.Abs(vector5.Dot(vector2)) > Math.Abs(vector5.Dot(FezMath.XZMask - vector2))))
			{
				bool flag = false;
				Vector3 point = vector5 / 2f + artObjectInstance3.Position;
				foreach (ArtObjectInstance connectedAo6 in connectedAos)
				{
					if (connectedAo6.ArtObject.ActorType == ActorType.None)
					{
						Vector3 vector4 = connectedAo6.Scale * connectedAo6.ArtObject.Size / 2f;
						boundingBox2 = new BoundingBox(connectedAo6.Position - vector4, connectedAo6.Position + vector4);
						Quaternion quaternion2 = connectedAo6.Rotation;
						FezMath.RotateOnCenter(ref boundingBox2, ref quaternion2);
						if (boundingBox2.Contains(point) != 0)
						{
							flag = true;
							break;
						}
					}
				}
				if (!flag)
				{
					artObjectInstance2 = artObjectInstance3;
					break;
				}
			}
			artObjectInstance3 = artObjectInstance3.ActorSettings.PrecedingNodeAo;
		}
		Vector3 vector6 = artObjectInstance2.Position - zero2;
		value.Triles.Sort(new MovingTrileInstanceComparer(vector6));
		foreach (TrileInstance trile4 in value.Triles)
		{
			trile4.Position += vector6;
			LevelManager.UpdateInstance(trile4);
		}
		MovementPath movementPath = new MovementPath
		{
			EndBehavior = PathEndBehavior.Bounce
		};
		ArtObjectInstance artObjectInstance4 = artObjectInstance2;
		PathSegment pathSegment = null;
		while (true)
		{
			ArtObjectInstance nextNodeAo = artObjectInstance4.ActorSettings.NextNodeAo;
			if (nextNodeAo == null)
			{
				break;
			}
			Vector3 vector7 = nextNodeAo.Position - artObjectInstance4.Position;
			bool flag2 = Math.Abs(vector7.Dot(vector2)) > Math.Abs(vector7.Dot(FezMath.XZMask - vector2));
			bool flag3 = false;
			Vector3 point2 = vector7 / 2f + artObjectInstance4.Position;
			foreach (ArtObjectInstance connectedAo7 in connectedAos)
			{
				if (connectedAo7.ArtObject.ActorType == ActorType.None)
				{
					Vector3 vector4 = connectedAo7.Scale * connectedAo7.ArtObject.Size / 2f;
					BoundingBox boundingBox3 = new BoundingBox(connectedAo7.Position - vector4, connectedAo7.Position + vector4);
					Quaternion quaternion3 = connectedAo7.Rotation;
					FezMath.RotateOnCenter(ref boundingBox3, ref quaternion3);
					if (boundingBox3.Contains(point2) != 0)
					{
						flag3 = true;
						break;
					}
				}
			}
			if (!flag3 && !flag2)
			{
				break;
			}
			PathSegment pathSegment2 = nextNodeAo.ActorSettings.Segment.Clone();
			pathSegment2.Destination = (pathSegment?.Destination ?? Vector3.Zero) + (nextNodeAo.Position - artObjectInstance4.Position);
			if (!flag3)
			{
				pathSegment2.Duration = TimeSpan.Zero;
			}
			movementPath.Segments.Add(pathSegment2);
			pathSegment = pathSegment2;
			artObjectInstance4 = nextNodeAo;
		}
		value.Path = movementPath;
		MovingGroupState movingGroupState = new MovingGroupState(value, connective: true)
		{
			Enabled = false
		};
		trackedGroups.Add(movingGroupState);
		if (value.MoveToEnd)
		{
			value.MoveToEnd = false;
			movingGroupState.MoveToEnd();
		}
		foreach (ArtObjectInstance connectedAo8 in connectedAos)
		{
			connectedAo8.Enabled = true;
		}
	}

	protected virtual void TrackNewGroups()
	{
		firstAvailableVolumeId = IdentifierPool.FirstAvailable(LevelManager.Volumes);
		enablingVolumes.Clear();
		trackedGroups.Clear();
		foreach (TrileGroup value in LevelManager.Groups.Values)
		{
			if (value.Path != null)
			{
				MovingGroupState movingGroupState = new MovingGroupState(value, connective: false);
				trackedGroups.Add(movingGroupState);
				if (LevelManager.IsPathRecorded(value.Id) || LevelManager.WasPathSupposedToBeRecorded(value.Id))
				{
					movingGroupState.MoveToEnd();
				}
			}
		}
	}

	private void PostTrackNewGroups()
	{
		TrackConnectivePaths(update: false);
		base.Enabled = trackedGroups.Count > 0;
		if (base.Enabled && VolumeService != null)
		{
			VolumeService.Enter += TryEnableConnectivePath;
		}
	}

	public override void Update(GameTime gameTime)
	{
		if (!EngineState.Paused && !EngineState.InMenuCube && !EngineState.InMap && CameraManager.Viewpoint.IsOrthographic() && CameraManager.ActionRunning && !EngineState.Loading && (!EngineState.FarawaySettings.InTransition || EngineState.FarawaySettings.DestinationCrossfadeStep != 0f))
		{
			DoUpdate(gameTime);
		}
	}

	protected void DoUpdate(GameTime gameTime)
	{
		bool flag = false;
		foreach (MovingGroupState trackedGroup in trackedGroups)
		{
			if (trackedGroup.Group.MoveToEnd)
			{
				flag |= trackedGroup.IsConnective;
				if (!trackedGroup.IsConnective)
				{
					trackedGroup.MoveToEnd();
					trackedGroup.Group.MoveToEnd = false;
				}
			}
		}
		if (flag)
		{
			TrackConnectivePaths(update: false);
		}
		foreach (MovingGroupState trackedGroup2 in trackedGroups)
		{
			trackedGroup2.Update(gameTime.ElapsedGameTime);
		}
	}

	private void InterpolationCallback(GameTime gameTime)
	{
		if (EngineState.Paused || EngineState.InMenuCube || EngineState.InMap || !CameraManager.Viewpoint.IsOrthographic() || !CameraManager.ActionRunning || EngineState.Loading || (EngineState.FarawaySettings.InTransition && EngineState.FarawaySettings.DestinationCrossfadeStep == 0f))
		{
			return;
		}
		double num = (gameTime.TotalGameTime - TimeInterpolation.LastUpdate).TotalSeconds / TimeInterpolation.UpdateTimestep.TotalSeconds;
		foreach (MovingGroupState trackedGroup in trackedGroups)
		{
			if (!trackedGroup.Enabled)
			{
				continue;
			}
			TrileMaterializer trileMaterializer = null;
			Trile trile = null;
			foreach (TrileInstance trile2 in trackedGroup.Group.Triles)
			{
				if (trile2.PhysicsState.Velocity != Vector3.Zero)
				{
					Vector3 position = trile2.Position;
					if (trile != trile2.VisualTrile)
					{
						trileMaterializer = LevelMaterializer.GetTrileMaterializer(trile2.VisualTrile);
						trile = trile2.VisualTrile;
					}
					int instanceId = trile2.InstanceId;
					ShaderInstancedIndexedPrimitives<VertexPositionNormalTextureInstance, Vector4> geometry = trileMaterializer.Geometry;
					if (instanceId != -1 && geometry.Instances != null && instanceId < geometry.Instances.Length)
					{
						Vector4 vector = new Vector4(Vector3.Lerp(position, position + trile2.PhysicsState.Velocity, (float)num), trile2.Phi);
						geometry.Instances[instanceId] = vector;
						geometry.InstancesDirty = true;
					}
				}
			}
		}
	}
}
