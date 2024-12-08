using System;
using System.Collections.Generic;
using Common;
using FezEngine.Effects;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Structure.Geometry;
using FezEngine.Tools;
using FezGame.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FezGame.Components.EndCutscene32;

internal class Fractal : DrawableGameComponent
{
	private enum State
	{
		RotateReveal,
		RainbowZoom,
		AxisReveal,
		AxisZoom
	}

	private const float RotateRevealDuration = 5f;

	private const float ZoomDuration = 18f;

	private const float AxisZoomDuration = 6f;

	private const int FractalDepth = 15;

	private const float FractalCycleMaxScreenSize = 20f;

	private readonly EndCutscene32Host Host;

	private readonly List<Mesh> FractalMeshes = new List<Mesh>();

	private float Time;

	private State ActiveState;

	private Mesh OuterShellMesh;

	private Mesh AxisMesh;

	[ServiceDependency]
	public IGameCameraManager CameraManager { private get; set; }

	[ServiceDependency(Optional = true)]
	public IKeyboardStateManager KeyboardState { private get; set; }

	[ServiceDependency]
	public ILevelManager LevelManager { private get; set; }

	[ServiceDependency]
	public IGameStateManager GameState { private get; set; }

	public Fractal(Game game, EndCutscene32Host host)
		: base(game)
	{
		Host = host;
		base.DrawOrder = 1000;
	}

	public override void Initialize()
	{
		base.Initialize();
		Reset();
	}

	private void Reset()
	{
		FractalMeshes.Clear();
		OuterShellMesh = new Mesh();
		AddShell(OuterShellMesh, positive: true, full: true);
		AddShell(OuterShellMesh, positive: false, full: true);
		Mesh parent = OuterShellMesh;
		bool flag = true;
		for (int i = 0; i < 15; i++)
		{
			Mesh mesh = new Mesh
			{
				Material = 
				{
					Diffuse = Color.Cyan.ToVector3()
				}
			};
			AddShell(mesh, flag, full: false);
			mesh.Culling = ((!flag) ? CullMode.CullClockwiseFace : CullMode.CullCounterClockwiseFace);
			mesh.CustomData = flag;
			mesh.Parent = parent;
			FractalMeshes.Add(mesh);
			flag = !flag;
			parent = mesh;
		}
		AxisMesh = new Mesh();
		AxisMesh.AddColoredBox(new Vector3(1f, 1f, 10000f) / 200f, Vector3.Zero, Color.Blue, centeredOnOrigin: false);
		AxisMesh.AddColoredBox(new Vector3(1f, 10000f, 1f) / 200f, Vector3.Zero, Color.Green, centeredOnOrigin: false);
		AxisMesh.AddColoredBox(new Vector3(10000f, 1f, 1f) / 200f, Vector3.Zero, Color.Red, centeredOnOrigin: false);
		DrawActionScheduler.Schedule(delegate
		{
			OuterShellMesh.Effect = new DefaultEffect.LitVertexColored();
			for (int j = 0; j < 15; j++)
			{
				FractalMeshes[j].Effect = new DefaultEffect.LitVertexColored();
			}
			AxisMesh.Effect = new DefaultEffect.VertexColored
			{
				Fullbright = true
			};
		});
		OuterShellMesh.Scale = Vector3.One * 2f;
	}

	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);
		foreach (Mesh fractalMesh in FractalMeshes)
		{
			fractalMesh.Dispose();
		}
		FractalMeshes.Clear();
		if (OuterShellMesh != null)
		{
			OuterShellMesh.Dispose();
		}
		OuterShellMesh = null;
		if (AxisMesh != null)
		{
			AxisMesh.Dispose();
		}
		AxisMesh = null;
	}

	private static void AddShell(Mesh mesh, bool positive, bool full)
	{
		Vector3 vector = new Vector3(0.5f);
		Group group = mesh.AddGroup();
		if (positive)
		{
			if (full)
			{
				group.Geometry = new IndexedUserPrimitives<VertexPositionNormalColor>(new VertexPositionNormalColor[12]
				{
					new VertexPositionNormalColor(new Vector3(1f, -1f, -1f) * vector, Vector3.UnitX, Color.White),
					new VertexPositionNormalColor(new Vector3(1f, 1f, -1f) * vector, Vector3.UnitX, Color.White),
					new VertexPositionNormalColor(new Vector3(1f, 1f, 1f) * vector, Vector3.UnitX, Color.White),
					new VertexPositionNormalColor(new Vector3(1f, -1f, 1f) * vector, Vector3.UnitX, Color.White),
					new VertexPositionNormalColor(new Vector3(1f, -1f, 1f) * vector, Vector3.UnitZ, Color.White),
					new VertexPositionNormalColor(new Vector3(1f, 1f, 1f) * vector, Vector3.UnitZ, Color.White),
					new VertexPositionNormalColor(new Vector3(-1f, 1f, 1f) * vector, Vector3.UnitZ, Color.White),
					new VertexPositionNormalColor(new Vector3(-1f, -1f, 1f) * vector, Vector3.UnitZ, Color.White),
					new VertexPositionNormalColor(new Vector3(-1f, 1f, -1f) * vector, Vector3.UnitY, Color.White),
					new VertexPositionNormalColor(new Vector3(-1f, 1f, 1f) * vector, Vector3.UnitY, Color.White),
					new VertexPositionNormalColor(new Vector3(1f, 1f, 1f) * vector, Vector3.UnitY, Color.White),
					new VertexPositionNormalColor(new Vector3(1f, 1f, -1f) * vector, Vector3.UnitY, Color.White)
				}, new int[18]
				{
					0, 2, 1, 0, 3, 2, 4, 6, 5, 4,
					7, 6, 8, 10, 9, 8, 11, 10
				}, PrimitiveType.TriangleList);
			}
			else
			{
				group.Geometry = new IndexedUserPrimitives<VertexPositionNormalColor>(new VertexPositionNormalColor[24]
				{
					new VertexPositionNormalColor(new Vector3(1f, -1f, 0f) * vector, Vector3.UnitX, Color.White),
					new VertexPositionNormalColor(new Vector3(1f, 0f, 0f) * vector, Vector3.UnitX, Color.White),
					new VertexPositionNormalColor(new Vector3(1f, 0f, 1f) * vector, Vector3.UnitX, Color.White),
					new VertexPositionNormalColor(new Vector3(1f, -1f, 1f) * vector, Vector3.UnitX, Color.White),
					new VertexPositionNormalColor(new Vector3(1f, -1f, -1f) * vector, Vector3.UnitX, Color.White),
					new VertexPositionNormalColor(new Vector3(1f, 1f, -1f) * vector, Vector3.UnitX, Color.White),
					new VertexPositionNormalColor(new Vector3(1f, 1f, 0f) * vector, Vector3.UnitX, Color.White),
					new VertexPositionNormalColor(new Vector3(1f, -1f, 0f) * vector, Vector3.UnitX, Color.White),
					new VertexPositionNormalColor(new Vector3(1f, -1f, 1f) * vector, Vector3.UnitZ, Color.White),
					new VertexPositionNormalColor(new Vector3(1f, 0f, 1f) * vector, Vector3.UnitZ, Color.White),
					new VertexPositionNormalColor(new Vector3(0f, 0f, 1f) * vector, Vector3.UnitZ, Color.White),
					new VertexPositionNormalColor(new Vector3(0f, -1f, 1f) * vector, Vector3.UnitZ, Color.White),
					new VertexPositionNormalColor(new Vector3(0f, -1f, 1f) * vector, Vector3.UnitZ, Color.White),
					new VertexPositionNormalColor(new Vector3(0f, 1f, 1f) * vector, Vector3.UnitZ, Color.White),
					new VertexPositionNormalColor(new Vector3(-1f, 1f, 1f) * vector, Vector3.UnitZ, Color.White),
					new VertexPositionNormalColor(new Vector3(-1f, -1f, 1f) * vector, Vector3.UnitZ, Color.White),
					new VertexPositionNormalColor(new Vector3(-1f, 1f, 0f) * vector, Vector3.UnitY, Color.White),
					new VertexPositionNormalColor(new Vector3(-1f, 1f, 1f) * vector, Vector3.UnitY, Color.White),
					new VertexPositionNormalColor(new Vector3(0f, 1f, 1f) * vector, Vector3.UnitY, Color.White),
					new VertexPositionNormalColor(new Vector3(0f, 1f, 0f) * vector, Vector3.UnitY, Color.White),
					new VertexPositionNormalColor(new Vector3(-1f, 1f, -1f) * vector, Vector3.UnitY, Color.White),
					new VertexPositionNormalColor(new Vector3(-1f, 1f, 0f) * vector, Vector3.UnitY, Color.White),
					new VertexPositionNormalColor(new Vector3(1f, 1f, 0f) * vector, Vector3.UnitY, Color.White),
					new VertexPositionNormalColor(new Vector3(1f, 1f, -1f) * vector, Vector3.UnitY, Color.White)
				}, new int[36]
				{
					0, 2, 1, 0, 3, 2, 4, 6, 5, 4,
					7, 6, 8, 10, 9, 8, 11, 10, 12, 14,
					13, 12, 15, 14, 16, 18, 17, 16, 19, 18,
					20, 22, 21, 20, 23, 22
				}, PrimitiveType.TriangleList);
			}
		}
		else
		{
			group.Geometry = new IndexedUserPrimitives<VertexPositionNormalColor>(new VertexPositionNormalColor[24]
			{
				new VertexPositionNormalColor(new Vector3(-1f, 0f, -1f) * vector, -Vector3.UnitZ, Color.White),
				new VertexPositionNormalColor(new Vector3(-1f, 1f, -1f) * vector, -Vector3.UnitZ, Color.White),
				new VertexPositionNormalColor(new Vector3(0f, 1f, -1f) * vector, -Vector3.UnitZ, Color.White),
				new VertexPositionNormalColor(new Vector3(0f, 0f, -1f) * vector, -Vector3.UnitZ, Color.White),
				new VertexPositionNormalColor(new Vector3(0f, -1f, -1f) * vector, -Vector3.UnitZ, Color.White),
				new VertexPositionNormalColor(new Vector3(0f, 1f, -1f) * vector, -Vector3.UnitZ, Color.White),
				new VertexPositionNormalColor(new Vector3(1f, 1f, -1f) * vector, -Vector3.UnitZ, Color.White),
				new VertexPositionNormalColor(new Vector3(1f, -1f, -1f) * vector, -Vector3.UnitZ, Color.White),
				new VertexPositionNormalColor(new Vector3(-1f, 0f, 0f) * vector, -Vector3.UnitX, Color.White),
				new VertexPositionNormalColor(new Vector3(-1f, 1f, 0f) * vector, -Vector3.UnitX, Color.White),
				new VertexPositionNormalColor(new Vector3(-1f, 1f, -1f) * vector, -Vector3.UnitX, Color.White),
				new VertexPositionNormalColor(new Vector3(-1f, 0f, -1f) * vector, -Vector3.UnitX, Color.White),
				new VertexPositionNormalColor(new Vector3(-1f, -1f, 1f) * vector, -Vector3.UnitX, Color.White),
				new VertexPositionNormalColor(new Vector3(-1f, 1f, 1f) * vector, -Vector3.UnitX, Color.White),
				new VertexPositionNormalColor(new Vector3(-1f, 1f, 0f) * vector, -Vector3.UnitX, Color.White),
				new VertexPositionNormalColor(new Vector3(-1f, -1f, 0f) * vector, -Vector3.UnitX, Color.White),
				new VertexPositionNormalColor(new Vector3(-1f, -1f, 0f) * vector, -Vector3.UnitY, Color.White),
				new VertexPositionNormalColor(new Vector3(-1f, -1f, 1f) * vector, -Vector3.UnitY, Color.White),
				new VertexPositionNormalColor(new Vector3(0f, -1f, 1f) * vector, -Vector3.UnitY, Color.White),
				new VertexPositionNormalColor(new Vector3(0f, -1f, 0f) * vector, -Vector3.UnitY, Color.White),
				new VertexPositionNormalColor(new Vector3(0f, -1f, -1f) * vector, -Vector3.UnitY, Color.White),
				new VertexPositionNormalColor(new Vector3(0f, -1f, 1f) * vector, -Vector3.UnitY, Color.White),
				new VertexPositionNormalColor(new Vector3(1f, -1f, 1f) * vector, -Vector3.UnitY, Color.White),
				new VertexPositionNormalColor(new Vector3(1f, -1f, -1f) * vector, -Vector3.UnitY, Color.White)
			}, new int[36]
			{
				0, 2, 1, 0, 3, 2, 4, 6, 5, 4,
				7, 6, 8, 10, 9, 8, 11, 10, 12, 14,
				13, 12, 15, 14, 16, 17, 18, 16, 18, 19,
				20, 21, 22, 20, 22, 23
			}, PrimitiveType.TriangleList);
		}
	}

	public override void Update(GameTime gameTime)
	{
		if (GameState.Paused || GameState.Loading)
		{
			return;
		}
		float num = (float)gameTime.ElapsedGameTime.TotalSeconds;
		Time += num;
		float num2 = (float)base.GraphicsDevice.Viewport.Width / (1280f * base.GraphicsDevice.GetViewScale());
		switch (ActiveState)
		{
		case State.RotateReveal:
		{
			float num7 = FezMath.Saturate(Time / 5f);
			float amount = Easing.EaseInOut(num7, EasingType.Quadratic);
			if (num7 == 0f)
			{
				CameraManager.Center = Vector3.Zero;
				CameraManager.Direction = Vector3.UnitZ;
				CameraManager.Radius = 10f * base.GraphicsDevice.GetViewScale() * num2;
				CameraManager.SnapInterpolation();
				LevelManager.ActualAmbient = new Color(0.25f, 0.25f, 0.25f);
				LevelManager.ActualDiffuse = Color.White;
				OuterShellMesh.Enabled = true;
				OuterShellMesh.Scale = Vector3.One * 2f;
				Mesh parent3 = OuterShellMesh;
				foreach (Mesh fractalMesh in FractalMeshes)
				{
					fractalMesh.Parent = parent3;
					fractalMesh.Enabled = true;
					parent3 = fractalMesh;
				}
			}
			OuterShellMesh.Scale *= 1.0025f;
			OuterShellMesh.Rotation = Quaternion.Slerp(Quaternion.Identity, Quaternion.CreateFromAxisAngle(Vector3.UnitY, (float)Math.PI), amount) * Quaternion.CreateFromRotationMatrix(Matrix.CreateLookAt(Vector3.One, Vector3.Zero, Vector3.Up));
			Util.ColorToHSV(new Color(FractalMeshes[0].Material.Diffuse), out var hue2, out var saturation2, out var value2);
			bool flag4 = true;
			for (int k = 0; k < FractalMeshes.Count; k++)
			{
				Mesh mesh3 = FractalMeshes[k];
				mesh3.Scale = mesh3.Parent.Scale / 2f;
				mesh3.Culling = (flag4 ? CullMode.CullClockwiseFace : CullMode.CullCounterClockwiseFace);
				mesh3.Rotation = mesh3.Parent.Rotation;
				mesh3.Position = mesh3.Parent.Position - Vector3.Transform(mesh3.Parent.Scale / 2f, mesh3.Parent.Rotation) * (flag4 ? 1 : (-1)) + Vector3.Transform(mesh3.Scale / 2f, mesh3.Rotation) * (flag4 ? 1 : (-1));
				mesh3.Material.Diffuse = Util.ColorFromHSV(hue2, saturation2, value2).ToVector3();
				hue2 += 15.0;
				flag4 = !flag4;
			}
			if (Time > 5f)
			{
				ChangeState();
			}
			break;
		}
		case State.RainbowZoom:
		{
			float num4 = FezMath.Saturate(Time / 18f);
			if (num4 == 0f)
			{
				OuterShellMesh.Enabled = true;
				OuterShellMesh.Scale = Vector3.One * 4.188076f;
				OuterShellMesh.Rotation = Quaternion.CreateFromRotationMatrix(Matrix.CreateLookAt(-Vector3.One, Vector3.Zero, Vector3.Up));
				Mesh parent = OuterShellMesh;
				foreach (Mesh fractalMesh2 in FractalMeshes)
				{
					fractalMesh2.Parent = parent;
					fractalMesh2.Rotation = OuterShellMesh.Rotation;
					fractalMesh2.Enabled = true;
					parent = fractalMesh2;
				}
			}
			float num5 = MathHelper.Lerp(1.0025f, 1.075f, num4);
			if (OuterShellMesh.Scale.X > 20f)
			{
				FractalMeshes[0].Scale *= num5;
				OuterShellMesh.Enabled = false;
			}
			else
			{
				OuterShellMesh.Scale *= num5;
			}
			Util.ColorToHSV(new Color(FractalMeshes[0].Material.Diffuse), out var hue, out var saturation, out var value);
			float num6 = Easing.EaseIn(FezMath.Saturate(FezMath.Saturate(num4 - 0.1f) / 0.7f), EasingType.Quadratic);
			Mesh outerShellMesh = OuterShellMesh;
			Quaternion rotation = (OuterShellMesh.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, num6 * 0.02f) * OuterShellMesh.Rotation);
			outerShellMesh.Rotation = rotation;
			AxisMesh.Rotation = OuterShellMesh.Rotation;
			for (int i = 0; i < FractalMeshes.Count; i++)
			{
				Mesh mesh = FractalMeshes[i];
				if (mesh.Parent.Enabled)
				{
					mesh.Scale = mesh.Parent.Scale / 2f;
				}
				bool flag = (bool)mesh.CustomData;
				mesh.Position = mesh.Parent.Position - Vector3.Transform(mesh.Parent.Scale / 2f, mesh.Parent.Rotation) * (flag ? 1 : (-1)) + Vector3.Transform(mesh.Scale / 2f, mesh.Rotation) * (flag ? 1 : (-1));
				mesh.Material.Diffuse = Util.ColorFromHSV(hue, saturation, value).ToVector3();
				mesh.Rotation = OuterShellMesh.Rotation;
				if (mesh.Scale.X > 20f)
				{
					FractalMeshes.RemoveAt(0);
					FractalMeshes[0].Parent = OuterShellMesh;
					mesh.Parent = FractalMeshes[FractalMeshes.Count - 1];
					FractalMeshes.Add(mesh);
					i--;
				}
				hue += 15.0;
			}
			if (Time > 18f)
			{
				ChangeState();
			}
			break;
		}
		case State.AxisReveal:
		{
			if (Time == 0f)
			{
				Mesh parent2 = OuterShellMesh;
				foreach (Mesh fractalMesh3 in FractalMeshes)
				{
					fractalMesh3.Parent = parent2;
					fractalMesh3.Rotation = OuterShellMesh.Rotation;
					parent2 = fractalMesh3;
				}
			}
			foreach (Mesh fractalMesh4 in FractalMeshes)
			{
				if (fractalMesh4.Enabled)
				{
					fractalMesh4.Scale *= 1.075f;
					break;
				}
			}
			OuterShellMesh.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, 0.02f) * OuterShellMesh.Rotation;
			AxisMesh.Rotation = OuterShellMesh.Rotation;
			bool flag2 = false;
			for (int j = 0; j < FractalMeshes.Count; j++)
			{
				Mesh mesh2 = FractalMeshes[j];
				if (!mesh2.Enabled)
				{
					continue;
				}
				if (mesh2.Parent.Enabled)
				{
					mesh2.Scale = mesh2.Parent.Scale / 2f;
				}
				bool flag3 = (bool)mesh2.CustomData;
				mesh2.Position = mesh2.Parent.Position - Vector3.Transform(mesh2.Parent.Scale / 2f, mesh2.Parent.Rotation) * (flag3 ? 1 : (-1)) + Vector3.Transform(mesh2.Scale / 2f, mesh2.Rotation) * (flag3 ? 1 : (-1));
				mesh2.Rotation = OuterShellMesh.Rotation;
				if (mesh2.Scale.X > 20f)
				{
					FractalMeshes[j].Enabled = false;
					if (j < FractalMeshes.Count - 1)
					{
						FractalMeshes[j + 1].Parent = OuterShellMesh;
					}
				}
				flag2 |= FractalMeshes[j].Enabled;
			}
			if (!flag2)
			{
				ChangeState();
			}
			break;
		}
		case State.AxisZoom:
		{
			float num3 = FezMath.Saturate(Time / 6f);
			AxisMesh.Scale = Vector3.Lerp(Vector3.One, new Vector3(3f), Easing.EaseIn(num3, EasingType.Cubic));
			AxisMesh.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, 0.025f * (1f - num3)) * AxisMesh.Rotation;
			AxisMesh.Position = Vector3.Lerp(Vector3.Zero, Vector3.Transform(Vector3.Forward * 10f, AxisMesh.Rotation), Easing.EaseIn(num3, EasingType.Cubic));
			if (Time > 6f)
			{
				ChangeState();
			}
			break;
		}
		}
	}

	private void ChangeState()
	{
		if (ActiveState == State.AxisZoom)
		{
			Host.Cycle();
			return;
		}
		Time = 0f;
		ActiveState++;
		Update(new GameTime());
	}

	public override void Draw(GameTime gameTime)
	{
		if (GameState.Loading)
		{
			return;
		}
		base.GraphicsDevice.Clear(EndCutscene32Host.PurpleBlack);
		switch (ActiveState)
		{
		case State.RotateReveal:
		case State.RainbowZoom:
			OuterShellMesh.Draw();
			{
				foreach (Mesh fractalMesh in FractalMeshes)
				{
					fractalMesh.Draw();
				}
				break;
			}
		case State.AxisReveal:
			base.GraphicsDevice.PrepareStencilWrite(StencilMask.CutsceneWipe);
			foreach (Mesh fractalMesh2 in FractalMeshes)
			{
				fractalMesh2.Draw();
			}
			base.GraphicsDevice.PrepareStencilRead(CompareFunction.NotEqual, StencilMask.CutsceneWipe);
			AxisMesh.Draw();
			base.GraphicsDevice.PrepareStencilRead(CompareFunction.Always, StencilMask.None);
			break;
		case State.AxisZoom:
			base.GraphicsDevice.PrepareStencilReadWrite(CompareFunction.NotEqual, StencilMask.CutsceneWipe);
			AxisMesh.Draw();
			base.GraphicsDevice.PrepareStencilRead(CompareFunction.Always, StencilMask.None);
			break;
		}
	}
}
