using System.Collections.Generic;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Tools;
using FezGame.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace FezGame.Components;

public class FlickeringNeon : GameComponent
{
	private class NeonState
	{
		public BackgroundPlane Neon;

		public float Time;

		public bool Enabled;

		public int FlickersLeft;
	}

	private readonly List<NeonState> NeonPlanes = new List<NeonState>();

	private readonly List<SoundEffect> Glitches = new List<SoundEffect>();

	[ServiceDependency]
	public IDefaultCameraManager CameraManager { private get; set; }

	[ServiceDependency]
	public IGameLevelManager LevelManager { private get; set; }

	[ServiceDependency]
	public IGameStateManager GameState { private get; set; }

	[ServiceDependency]
	public IContentManagerProvider CMProvider { private get; set; }

	public FlickeringNeon(Game game)
		: base(game)
	{
	}

	public override void Initialize()
	{
		base.Initialize();
		LevelManager.LevelChanged += TrackNeons;
		base.Enabled = false;
	}

	private void TrackNeons()
	{
		NeonPlanes.Clear();
		Glitches.Clear();
		base.Enabled = false;
		foreach (BackgroundPlane value in LevelManager.BackgroundPlanes.Values)
		{
			if (value.TextureName != null && value.TextureName.EndsWith("_GLOW") && value.TextureName.Contains("NEON"))
			{
				NeonPlanes.Add(new NeonState
				{
					Neon = value,
					Time = RandomHelper.Between(2.0, 4.0)
				});
			}
		}
		base.Enabled = NeonPlanes.Count > 0;
		if (base.Enabled)
		{
			Glitches.Add(CMProvider.CurrentLevel.Load<SoundEffect>("Sounds/Intro/Elders/Glitches/Glitch1"));
			Glitches.Add(CMProvider.CurrentLevel.Load<SoundEffect>("Sounds/Intro/Elders/Glitches/Glitch2"));
			Glitches.Add(CMProvider.CurrentLevel.Load<SoundEffect>("Sounds/Intro/Elders/Glitches/Glitch3"));
		}
	}

	public override void Update(GameTime gameTime)
	{
		if (GameState.Loading || GameState.InMap || GameState.InMenuCube || GameState.Paused)
		{
			return;
		}
		bool flag = !CameraManager.Viewpoint.IsOrthographic();
		Vector3 forward = CameraManager.InverseView.Forward;
		BoundingFrustum frustum = CameraManager.Frustum;
		foreach (NeonState neonPlane in NeonPlanes)
		{
			neonPlane.Time -= (float)gameTime.ElapsedGameTime.TotalSeconds;
			if (neonPlane.Time <= 0f)
			{
				if (neonPlane.FlickersLeft == 0)
				{
					neonPlane.FlickersLeft = RandomHelper.Random.Next(4, 18);
				}
				BackgroundPlane neon = neonPlane.Neon;
				bool flag3 = (neon.Visible = !neon.Hidden && (flag || neon.Doublesided || neon.Crosshatch || neon.Billboard || forward.Dot(neon.Forward) > 0f) && frustum.Contains(neon.Bounds) != ContainmentType.Disjoint);
				neonPlane.Enabled = !neonPlane.Enabled;
				neonPlane.Neon.Hidden = neonPlane.Enabled;
				neonPlane.Neon.Visible = !neonPlane.Neon.Hidden;
				neonPlane.Neon.Update();
				if (flag3 && RandomHelper.Probability(0.5))
				{
					RandomHelper.InList(Glitches).EmitAt(neonPlane.Neon.Position, loop: false, RandomHelper.Centered(0.10000000149011612), RandomHelper.Between(0.0, 1.0), paused: false);
				}
				neonPlane.Time = Easing.EaseIn(RandomHelper.Between(0.0, 0.44999998807907104), EasingType.Quadratic);
				neonPlane.FlickersLeft--;
				if (neonPlane.FlickersLeft == 0)
				{
					neonPlane.Enabled = true;
					neonPlane.Neon.Hidden = false;
					neonPlane.Neon.Visible = true;
					neonPlane.Neon.Update();
					neonPlane.Time = RandomHelper.Between(3.0, 8.0);
				}
			}
		}
	}
}
