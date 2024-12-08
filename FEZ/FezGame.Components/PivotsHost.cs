using System;
using System.Collections.Generic;
using System.Linq;
using FezEngine;
using FezEngine.Components;
using FezEngine.Services;
using FezEngine.Services.Scripting;
using FezEngine.Structure;
using FezEngine.Structure.Input;
using FezEngine.Tools;
using FezGame.Services;
using FezGame.Structure;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace FezGame.Components;

public class PivotsHost : GameComponent
{
	private class PivotState
	{
		private const float SpinTime = 1.5f;

		private readonly PivotsHost Host;

		private readonly TrileGroup Group;

		public readonly ArtObjectInstance HandleAo;

		private readonly ArtObjectInstance[] AttachedArtObjects;

		private readonly Vector3[] AttachedAoOrigins;

		private readonly Quaternion[] AttachedAoRotations;

		private readonly List<TrileInstance> TopLayer = new List<TrileInstance>();

		private readonly List<TrileInstance> AttachedTriles = new List<TrileInstance>();

		private Vector4[] OriginalStates;

		private Quaternion OriginalAoRotation;

		private SpinAction State;

		private TimeSpan SinceChanged;

		private int SpinSign;

		private bool HasShaken;

		[ServiceDependency]
		public IPivotService PivotService { private get; set; }

		[ServiceDependency]
		public IGameLevelManager LevelManager { private get; set; }

		[ServiceDependency]
		public ILevelMaterializer LevelMaterializer { private get; set; }

		[ServiceDependency]
		public IInputManager InputManager { private get; set; }

		[ServiceDependency]
		public IDefaultCameraManager CameraManager { private get; set; }

		[ServiceDependency]
		public IPlayerManager PlayerManager { private get; set; }

		[ServiceDependency]
		public IGameStateManager GameState { private get; set; }

		public PivotState(PivotsHost host, ArtObjectInstance handleAo)
		{
			ServiceHelper.InjectServices(this);
			Host = host;
			HandleAo = handleAo;
			Group = LevelManager.Groups[handleAo.ActorSettings.AttachedGroup.Value];
			AttachedArtObjects = LevelManager.ArtObjects.Values.Where((ArtObjectInstance x) => x.ActorSettings.AttachedGroup == Group.Id && x != HandleAo).ToArray();
			AttachedAoOrigins = AttachedArtObjects.Select((ArtObjectInstance x) => x.Position).ToArray();
			AttachedAoRotations = AttachedArtObjects.Select((ArtObjectInstance x) => x.Rotation).ToArray();
			foreach (TrileInstance trile in Group.Triles)
			{
				trile.ForceSeeThrough = true;
			}
			float num = Group.Triles.Where((TrileInstance x) => !x.Trile.Immaterial).Max((TrileInstance x) => x.Position.Y);
			foreach (TrileInstance trile2 in Group.Triles)
			{
				if (trile2.Position.Y == num)
				{
					TopLayer.Add(trile2);
				}
			}
			int value;
			if (LevelManager.Name == "WATER_TOWER" && LevelManager.LastLevelName == "LIGHTHOUSE")
			{
				if (GameState.SaveData.ThisLevel.PivotRotations.ContainsKey(handleAo.Id))
				{
					GameState.SaveData.ThisLevel.PivotRotations[handleAo.Id] = 0;
				}
			}
			else if (GameState.SaveData.ThisLevel.PivotRotations.TryGetValue(handleAo.Id, out value) && value != 0)
			{
				ForceSpinTo(value);
			}
			if (GameState.SaveData.ThisLevel.InactiveArtObjects.Contains(HandleAo.Id))
			{
				HandleAo.Enabled = false;
			}
		}

		private void ForceSpinTo(int initialSpins)
		{
			int num = Math.Abs(initialSpins);
			for (int i = 0; i < num; i++)
			{
				OriginalAoRotation = HandleAo.Rotation;
				AttachedTriles.Clear();
				foreach (TrileInstance item in TopLayer)
				{
					AddSupportedTrilesOver(item);
				}
				OriginalStates = (from x in Group.Triles.Union(AttachedTriles)
					select new Vector4(x.Position, x.Phi)).ToArray();
				float num2 = (float)Math.PI / 2f * (float)Math.Sign(initialSpins);
				Quaternion quaternion = Quaternion.CreateFromAxisAngle(Vector3.UnitY, num2);
				Vector3 position = HandleAo.Position;
				for (int j = 0; j < AttachedArtObjects.Length; j++)
				{
					AttachedAoRotations[j] = AttachedArtObjects[j].Rotation;
					AttachedAoOrigins[j] = AttachedArtObjects[j].Position;
					AttachedArtObjects[j].Rotation = AttachedAoRotations[j] * quaternion;
					AttachedArtObjects[j].Position = Vector3.Transform(AttachedAoOrigins[j] - position, quaternion) + position;
				}
				for (int k = 0; k < OriginalStates.Length; k++)
				{
					TrileInstance trileInstance = ((k < Group.Triles.Count) ? Group.Triles[k] : AttachedTriles[k - Group.Triles.Count]);
					Vector4 vector = OriginalStates[k];
					trileInstance.Position = Vector3.Transform(vector.XYZ() + new Vector3(0.5f) - position, quaternion) + position - new Vector3(0.5f);
					trileInstance.Phi = FezMath.WrapAngle(vector.W + num2);
					LevelMaterializer.GetTrileMaterializer(trileInstance.Trile).UpdateInstance(trileInstance);
				}
				RotateTriles();
			}
			HandleAo.Rotation *= Quaternion.CreateFromAxisAngle(Vector3.UnitY, (float)Math.PI / 2f * (float)initialSpins);
		}

		public bool Update(TimeSpan elapsed)
		{
			SinceChanged += elapsed;
			switch (State)
			{
			case SpinAction.Idle:
			{
				bool flag;
				if (HandleAo.ArtObject.ActorType == ActorType.Bookcase)
				{
					Vector3 vector2 = PlayerManager.Position - HandleAo.Position;
					flag = vector2.Z < 2.75f && vector2.Z > -0.25f && vector2.Y < -3f && vector2.Y > -4f && CameraManager.Viewpoint == Viewpoint.Left;
				}
				else
				{
					Vector3 vector3 = (PlayerManager.Position - (HandleAo.Position - new Vector3(0f, 1.5f, 0f))) * CameraManager.Viewpoint.ScreenSpaceMask();
					vector3.X += vector3.Z;
					Vector3 vector4 = vector3.Abs();
					flag = vector4.X > 0.75f && vector4.X < 1.75f && vector4.Y < 1f;
				}
				if (HandleAo.Enabled && flag && PlayerManager.Grounded && PlayerManager.Action != ActionType.PushingPivot && InputManager.GrabThrow.IsDown() && PlayerManager.Action != ActionType.ReadingSign && PlayerManager.Action != ActionType.FreeFalling && PlayerManager.Action != ActionType.Dying)
				{
					SinceChanged = TimeSpan.Zero;
					return true;
				}
				break;
			}
			case SpinAction.Spinning:
			{
				double num = FezMath.Saturate(SinceChanged.TotalSeconds / 1.5);
				float num2 = Easing.EaseIn((float)((num < 0.800000011920929) ? (num / 0.800000011920929) : (1.0 + Math.Sin((num - 0.800000011920929) / 0.20000000298023224 * 6.2831854820251465 * 2.0) * 0.009999999776482582 * (1.0 - num) / 0.20000000298023224)), EasingType.Quartic) * ((float)Math.PI / 2f) * (float)SpinSign;
				Quaternion quaternion = Quaternion.CreateFromAxisAngle(Vector3.UnitY, num2);
				Vector3 position = HandleAo.Position;
				for (int i = 0; i < OriginalStates.Length; i++)
				{
					TrileInstance trileInstance = ((i < Group.Triles.Count) ? Group.Triles[i] : AttachedTriles[i - Group.Triles.Count]);
					Vector4 vector = OriginalStates[i];
					trileInstance.Position = Vector3.Transform(vector.XYZ() + new Vector3(0.5f) - position, quaternion) + position - new Vector3(0.5f);
					trileInstance.Phi = FezMath.WrapAngle(vector.W + num2);
					LevelMaterializer.GetTrileMaterializer(trileInstance.Trile).UpdateInstance(trileInstance);
				}
				if (!HasShaken && num > 0.800000011920929)
				{
					ServiceHelper.AddComponent(new CamShake(ServiceHelper.Game)
					{
						Distance = 0.25f,
						Duration = TimeSpan.FromSeconds(0.20000000298023224)
					});
					HasShaken = true;
				}
				HandleAo.Rotation = OriginalAoRotation * quaternion;
				for (int j = 0; j < AttachedArtObjects.Length; j++)
				{
					AttachedArtObjects[j].Rotation = AttachedAoRotations[j] * quaternion;
					AttachedArtObjects[j].Position = Vector3.Transform(AttachedAoOrigins[j] - position, quaternion) + position;
				}
				if (SinceChanged.TotalSeconds >= 1.5)
				{
					RotateTriles();
					SinceChanged -= TimeSpan.FromSeconds(1.5);
					State = SpinAction.Idle;
				}
				break;
			}
			}
			return false;
		}

		public void Spin()
		{
			PlayerManager.Action = ActionType.PushingPivot;
			Waiters.Wait(0.5, (float _) => PlayerManager.Action != ActionType.PushingPivot, delegate
			{
				if (PlayerManager.Action == ActionType.PushingPivot)
				{
					SinceChanged = TimeSpan.Zero;
					OriginalAoRotation = HandleAo.Rotation;
					foreach (TrileInstance trile in Group.Triles)
					{
						if (trile.InstanceId == -1)
						{
							LevelMaterializer.CullInstanceIn(trile);
						}
					}
					Vector3 vector = (PlayerManager.Position - (HandleAo.Position - new Vector3(0f, 1.5f, 0f))) * CameraManager.Viewpoint.ScreenSpaceMask();
					vector.X += vector.Z;
					if (HandleAo.ArtObject.ActorType == ActorType.Bookcase)
					{
						SpinSign = 1;
					}
					else
					{
						SpinSign = (int)(CameraManager.Viewpoint.RightVector().Sign().Dot(Vector3.One) * (float)Math.Sign(vector.X));
					}
					if (SpinSign == 1)
					{
						Host.RightSound.Emit();
					}
					else
					{
						Host.LeftSound.Emit();
					}
					AttachedTriles.Clear();
					foreach (TrileInstance item in TopLayer)
					{
						AddSupportedTrilesOver(item);
					}
					OriginalStates = (from x in Group.Triles.Union(AttachedTriles)
						select new Vector4(x.Position, x.Phi)).ToArray();
					for (int i = 0; i < AttachedArtObjects.Length; i++)
					{
						AttachedAoRotations[i] = AttachedArtObjects[i].Rotation;
						AttachedAoOrigins[i] = AttachedArtObjects[i].Position;
					}
					if (!GameState.SaveData.ThisLevel.PivotRotations.TryGetValue(HandleAo.Id, out var value))
					{
						GameState.SaveData.ThisLevel.PivotRotations.Add(HandleAo.Id, SpinSign);
					}
					else
					{
						GameState.SaveData.ThisLevel.PivotRotations[HandleAo.Id] = value + SpinSign;
					}
					if (SpinSign == 1)
					{
						PivotService.OnRotateRight(HandleAo.Id);
					}
					else
					{
						PivotService.OnRotateLeft(HandleAo.Id);
					}
					HasShaken = false;
					State = SpinAction.Spinning;
					if (HandleAo.ArtObject.ActorType == ActorType.Bookcase)
					{
						HandleAo.Enabled = false;
						GameState.SaveData.ThisLevel.InactiveArtObjects.Add(HandleAo.Id);
					}
				}
			});
		}

		private void AddSupportedTrilesOver(TrileInstance instance)
		{
			TrileEmplacement id = new TrileEmplacement(instance.Emplacement.X, instance.Emplacement.Y + 1, instance.Emplacement.Z);
			TrileInstance trileInstance = LevelManager.TrileInstanceAt(ref id);
			if (trileInstance == null)
			{
				return;
			}
			AddSupportedTrile(trileInstance);
			if (!trileInstance.Overlaps)
			{
				return;
			}
			foreach (TrileInstance overlappedTrile in trileInstance.OverlappedTriles)
			{
				AddSupportedTrile(overlappedTrile);
			}
		}

		private void AddSupportedTrile(TrileInstance instance)
		{
			if (AttachedTriles.Contains(instance) || Group.Triles.Contains(instance) || (instance.PhysicsState == null && !instance.Trile.ActorSettings.Type.IsPickable()))
			{
				return;
			}
			AttachedTriles.Add(instance);
			AddSupportedTrilesOver(instance);
			if (!LevelManager.PickupGroups.TryGetValue(instance, out var value))
			{
				return;
			}
			foreach (TrileInstance trile in value.Triles)
			{
				AddSupportedTrile(trile);
			}
		}

		private void RotateTriles()
		{
			TrileInstance[] array = Group.Triles.Union(AttachedTriles).ToArray();
			foreach (TrileInstance instance in array)
			{
				LevelManager.UpdateInstance(instance);
			}
			if (!LevelManager.Groups.ContainsKey(Group.Id))
			{
				throw new InvalidOperationException("Group was lost after pivot rotation!");
			}
		}
	}

	private readonly List<PivotState> TrackedPivots = new List<PivotState>();

	private SoundEffect LeftSound;

	private SoundEffect RightSound;

	[ServiceDependency]
	public IGameStateManager GameState { get; set; }

	[ServiceDependency]
	public ILevelManager LevelManager { get; set; }

	[ServiceDependency]
	public IDefaultCameraManager CameraManager { get; set; }

	[ServiceDependency]
	public IEngineStateManager EngineState { get; set; }

	[ServiceDependency]
	public ILevelMaterializer LevelMaterializer { get; set; }

	[ServiceDependency]
	public IContentManagerProvider CMProvider { get; set; }

	public PivotsHost(Game game)
		: base(game)
	{
	}

	public override void Initialize()
	{
		base.Initialize();
		LevelManager.LevelChanging += TryInitialize;
		TryInitialize();
		LeftSound = CMProvider.Global.Load<SoundEffect>("Sounds/Industrial/PivotLeft");
		RightSound = CMProvider.Global.Load<SoundEffect>("Sounds/Industrial/PivotRight");
	}

	private void TryInitialize()
	{
		TrackedPivots.Clear();
		foreach (ArtObjectInstance value in LevelManager.ArtObjects.Values)
		{
			if (value.ArtObject.ActorType == ActorType.PivotHandle || value.ArtObject.ActorType == ActorType.Bookcase)
			{
				TrackedPivots.Add(new PivotState(this, value));
			}
		}
		if (TrackedPivots.Count > 0)
		{
			LevelMaterializer.CullInstances();
		}
	}

	public override void Update(GameTime gameTime)
	{
		if (EngineState.Loading || EngineState.Paused || GameState.InMap || !CameraManager.Viewpoint.IsOrthographic() || !CameraManager.ActionRunning)
		{
			return;
		}
		float num = float.MaxValue;
		PivotState pivotState = null;
		foreach (PivotState trackedPivot in TrackedPivots)
		{
			if (trackedPivot.Update(gameTime.ElapsedGameTime))
			{
				float num2 = trackedPivot.HandleAo.Position.Dot(CameraManager.Viewpoint.ForwardVector());
				if (num2 < num)
				{
					pivotState = trackedPivot;
					num = num2;
				}
			}
		}
		pivotState?.Spin();
	}
}
