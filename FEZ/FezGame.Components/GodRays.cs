using System;
using System.Collections.Generic;
using FezEngine;
using FezEngine.Effects;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Tools;
using FezGame.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FezGame.Components;

public class GodRays : DrawableGameComponent
{
	private class RayCustomData
	{
		public TimeSpan AccumulatedTime;

		public float RandomSpeed;

		public float RandomOpacity;

		public int Layer;
	}

	private readonly Dictionary<Viewpoint, Mesh> Meshes = new Dictionary<Viewpoint, Mesh>(ViewpointComparer.Default);

	private bool viewLocked;

	private Viewpoint lockedTo;

	[ServiceDependency]
	public ILevelManager LevelManager { get; set; }

	[ServiceDependency]
	public IDotManager DotManager { get; set; }

	[ServiceDependency]
	public IGameStateManager GameState { get; set; }

	[ServiceDependency]
	public IDefaultCameraManager CameraManager { get; set; }

	[ServiceDependency]
	public ITimeManager TimeManager { get; set; }

	public GodRays(Game game)
		: base(game)
	{
		base.DrawOrder = 1;
		base.UpdateOrder = 11;
		base.Enabled = (base.Visible = false);
	}

	public override void Initialize()
	{
		base.Initialize();
		Meshes.Add(Viewpoint.Front, new Mesh
		{
			AlwaysOnTop = true,
			DepthWrites = false,
			Blending = BlendingMode.Additive,
			Culling = CullMode.CullClockwiseFace,
			CustomRenderingHandler = DrawLayered,
			Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, 0f)
		});
		Meshes.Add(Viewpoint.Right, new Mesh
		{
			AlwaysOnTop = true,
			DepthWrites = false,
			Blending = BlendingMode.Additive,
			Culling = CullMode.CullClockwiseFace,
			CustomRenderingHandler = DrawLayered,
			Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, (float)Math.PI / 2f)
		});
		Meshes.Add(Viewpoint.Back, new Mesh
		{
			AlwaysOnTop = true,
			DepthWrites = false,
			Blending = BlendingMode.Additive,
			Culling = CullMode.CullClockwiseFace,
			CustomRenderingHandler = DrawLayered,
			Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, -(float)Math.PI)
		});
		Meshes.Add(Viewpoint.Left, new Mesh
		{
			AlwaysOnTop = true,
			DepthWrites = false,
			Blending = BlendingMode.Additive,
			Culling = CullMode.CullClockwiseFace,
			CustomRenderingHandler = DrawLayered,
			Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, -(float)Math.PI / 2f)
		});
		DrawActionScheduler.Schedule(delegate
		{
			foreach (Mesh value in Meshes.Values)
			{
				value.Effect = new DefaultEffect.VertexColored();
			}
		});
		LevelManager.LevelChanged += TryInitialize;
	}

	private void TryInitialize()
	{
		bool enabled = base.Enabled;
		bool enabled2 = (base.Visible = LevelManager.Sky != null && LevelManager.Sky.Name == "WATERFRONT");
		base.Enabled = enabled2;
		if (base.Enabled)
		{
			bool wide = LevelManager.Name == "NATURE_HUB";
			if (!enabled)
			{
				for (int i = 0; i < 60; i++)
				{
					TryAddRay(wide, midLife: true);
				}
			}
			return;
		}
		foreach (Mesh value in Meshes.Values)
		{
			lock (value)
			{
				value.ClearGroups();
			}
		}
	}

	public override void Update(GameTime gameTime)
	{
		if ((GameState.Loading && !GameState.FarawaySettings.InTransition) || GameState.Paused || GameState.InMenuCube || GameState.InMap || !CameraManager.Viewpoint.IsOrthographic())
		{
			return;
		}
		if (DotManager.Behaviour == DotHost.BehaviourType.SpiralAroundWithCamera || Fez.LongScreenshot)
		{
			TryAddRay(wide: true, midLife: false);
			ScrollRays(Viewpoint.Back, gameTime.ElapsedGameTime);
			ScrollRays(Viewpoint.Front, gameTime.ElapsedGameTime);
			ScrollRays(Viewpoint.Left, gameTime.ElapsedGameTime);
			ScrollRays(Viewpoint.Right, gameTime.ElapsedGameTime);
		}
		else if (!viewLocked && !CameraManager.ProjectionTransition)
		{
			if (RandomHelper.Probability(0.25))
			{
				TryAddRay(wide: false, midLife: false);
			}
			ScrollRays(CameraManager.Viewpoint, gameTime.ElapsedGameTime, 8f);
		}
		else
		{
			ScrollRays(Viewpoint.Back, gameTime.ElapsedGameTime, 6f);
			ScrollRays(Viewpoint.Front, gameTime.ElapsedGameTime, 6f);
			ScrollRays(Viewpoint.Left, gameTime.ElapsedGameTime, 6f);
			ScrollRays(Viewpoint.Right, gameTime.ElapsedGameTime, 6f);
		}
	}

	private void TryAddRay(bool wide, bool midLife)
	{
		Viewpoint key = FezMath.OrientationFromPhi((float)RandomHelper.Random.Next(0, 4) * ((float)Math.PI / 2f)).AsViewpoint();
		Mesh mesh = Meshes[key];
		if (mesh.Groups.Count < 15)
		{
			float num = (wide ? (CameraManager.Radius * 2f) : CameraManager.Radius);
			float num2 = (0f - num) / 2f;
			float num3 = num / 2f;
			Vector3 position = RandomHelper.Between(num2, num3) * Vector3.UnitX;
			float num4 = RandomHelper.Between(0.75, 4.0);
			float num5 = 8f + RandomHelper.Centered(4.0);
			lock (mesh)
			{
				Group group = mesh.AddColoredQuad(new Vector3(0f, 0f, 0f), new Vector3(0f - num5 - num4, 0f - num5, 0f), new Vector3(0f - num5, 0f - num5, 0f), new Vector3(0f - num4, 0f, 0f), new Color(255, 255, 255), Color.Black, Color.Black, new Color(255, 255, 255));
				group.CustomData = new RayCustomData
				{
					RandomOpacity = RandomHelper.Between(0.125, 0.5),
					Layer = RandomHelper.Random.Next(0, 4),
					RandomSpeed = RandomHelper.Between(0.25, 2.0),
					AccumulatedTime = (midLife ? TimeSpan.FromSeconds(RandomHelper.Between(0.0, 8.0)) : TimeSpan.Zero)
				};
				group.Material = new Material
				{
					Diffuse = Vector3.Zero
				};
				group.Position = position;
			}
		}
	}

	private void AlignMesh(Viewpoint viewpoint)
	{
		float num = CameraManager.Radius / base.GraphicsDevice.GetViewScale();
		Vector3 interpolatedCenter = CameraManager.InterpolatedCenter;
		Vector3 vector = viewpoint.ForwardVector();
		Vector3 vector2 = viewpoint.RightVector();
		Meshes[viewpoint].Position = new Vector3(0f, interpolatedCenter.Y + num / 2f / CameraManager.AspectRatio, 0f) + interpolatedCenter * vector2.Abs() + LevelManager.Size / 2f * vector.Abs() + num * vector;
	}

	private void ScrollRays(Viewpoint viewpoint, TimeSpan elapsed)
	{
		ScrollRays(viewpoint, elapsed, 8f);
	}

	private void ScrollRays(Viewpoint viewpoint, TimeSpan elapsed, float lifetime)
	{
		float val = ((TimeManager.DayFraction > 0.8f) ? 1f : (TimeManager.DuskContribution / 0.85f));
		try
		{
			for (int i = 0; i < Meshes[viewpoint].Groups.Count; i++)
			{
				Group group = Meshes[viewpoint].Groups[i];
				RayCustomData rayCustomData = (RayCustomData)group.CustomData;
				rayCustomData.AccumulatedTime += elapsed;
				group.Material.Diffuse = new Vector3((float)Math.Sin(rayCustomData.AccumulatedTime.TotalSeconds / (double)lifetime * 3.1415927410125732) * rayCustomData.RandomOpacity) * (1f - FezMath.Saturate(Math.Max(TimeManager.NightContribution * 2f, val)));
				if (TimeManager.DuskContribution != 0f)
				{
					group.Material.Diffuse *= new Vector3(1f, 1f - TimeManager.DuskContribution, 0f);
				}
				else if (TimeManager.DawnContribution != 0f)
				{
					group.Material.Diffuse *= new Vector3(1f, 1f - TimeManager.DawnContribution * 0.5f, 0f);
				}
				else
				{
					group.Material.Diffuse *= new Vector3(1f, 1f, 0f);
				}
				group.Position += (float)elapsed.TotalSeconds * Vector3.UnitX * 0.25f * (0.25f + (float)rayCustomData.Layer / 3f * 1.25f) * rayCustomData.RandomSpeed;
				if (rayCustomData.AccumulatedTime.TotalSeconds > (double)lifetime)
				{
					lock (Meshes[viewpoint])
					{
						Meshes[viewpoint].RemoveGroupAt(i);
					}
					i--;
				}
			}
		}
		catch (NullReferenceException)
		{
		}
	}

	public override void Draw(GameTime gameTime)
	{
		if ((GameState.Loading && !GameState.FarawaySettings.InTransition) || TimeManager.NightContribution > 0.5f || GameState.InMap || !CameraManager.Viewpoint.IsOrthographic())
		{
			return;
		}
		if (GameState.FarawaySettings.InTransition && !viewLocked)
		{
			foreach (Mesh value in Meshes.Values)
			{
				(value.Effect as DefaultEffect).ForcedViewMatrix = CameraManager.View;
				(value.Effect as DefaultEffect).ForcedProjectionMatrix = CameraManager.Projection;
				foreach (Group group in value.Groups)
				{
					(group.CustomData as RayCustomData).AccumulatedTime = TimeSpan.FromSeconds((group.CustomData as RayCustomData).AccumulatedTime.TotalSeconds / 8.0 * 6.0);
				}
			}
			viewLocked = true;
			lockedTo = CameraManager.Viewpoint;
		}
		if (!GameState.FarawaySettings.InTransition && viewLocked)
		{
			foreach (Mesh value2 in Meshes.Values)
			{
				(value2.Effect as DefaultEffect).ForcedViewMatrix = null;
				(value2.Effect as DefaultEffect).ForcedProjectionMatrix = null;
			}
			viewLocked = false;
			lockedTo = Viewpoint.None;
		}
		if (DotManager.Behaviour == DotHost.BehaviourType.SpiralAroundWithCamera || Fez.LongScreenshot)
		{
			TryAddRay(wide: true, midLife: false);
			AlignMesh(Viewpoint.Back);
			AlignMesh(Viewpoint.Front);
			AlignMesh(Viewpoint.Left);
			AlignMesh(Viewpoint.Right);
		}
		else if (!viewLocked && !CameraManager.ProjectionTransition)
		{
			AlignMesh(CameraManager.Viewpoint);
		}
		if (CameraManager.ProjectionTransition)
		{
			foreach (Mesh value3 in Meshes.Values)
			{
				foreach (Group group2 in value3.Groups)
				{
					group2.Material.Diffuse *= new Vector3(CameraManager.ViewTransitionStep / 2f);
				}
			}
		}
		if (DotManager.Behaviour == DotHost.BehaviourType.SpiralAroundWithCamera || GameState.FarawaySettings.InTransition)
		{
			foreach (Mesh value4 in Meshes.Values)
			{
				value4.Draw();
			}
			return;
		}
		Meshes[CameraManager.Viewpoint].Draw();
		if (!CameraManager.ViewTransitionReached)
		{
			Meshes[CameraManager.LastViewpoint].Draw();
		}
	}

	private void DrawLayered(Mesh m, BaseEffect e)
	{
		lock (m)
		{
			foreach (Group group in m.Groups)
			{
				switch (((RayCustomData)group.CustomData).Layer)
				{
				case 0:
					base.GraphicsDevice.PrepareStencilRead(CompareFunction.Greater, StencilMask.SkyLayer3);
					break;
				case 1:
					base.GraphicsDevice.PrepareStencilRead(CompareFunction.Greater, StencilMask.SkyLayer2);
					break;
				case 2:
					base.GraphicsDevice.PrepareStencilRead(CompareFunction.Greater, StencilMask.SkyLayer1);
					break;
				case 3:
					base.GraphicsDevice.PrepareStencilRead(CompareFunction.Always, StencilMask.None);
					break;
				}
				group.Draw(e);
			}
		}
		base.GraphicsDevice.PrepareStencilRead(CompareFunction.Always, StencilMask.None);
	}
}
