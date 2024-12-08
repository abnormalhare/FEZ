using System;
using System.Collections.Generic;
using FezEngine;
using FezEngine.Effects;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Structure.Geometry;
using FezEngine.Tools;
using FezGame.Components.Scripting;
using FezGame.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FezGame.Components;

public class FakeDot : DrawableGameComponent
{
	private List<Vector4> Vertices = new List<Vector4>();

	private int[] FaceVertexIndices;

	private Mesh DotMesh;

	private Mesh RaysMesh;

	private Mesh FlareMesh;

	private IndexedUserPrimitives<FezVertexPositionColor> DotWireGeometry;

	private IndexedUserPrimitives<FezVertexPositionColor> DotFacesGeometry;

	private float Theta;

	private float EightShapeStep;

	public float Opacity { get; set; }

	[ServiceDependency]
	public ITargetRenderingManager TargetRenderer { get; set; }

	[ServiceDependency]
	public ISoundManager SoundManager { get; set; }

	[ServiceDependency]
	public IGameStateManager GameState { get; set; }

	[ServiceDependency]
	public IPlayerManager PlayerManager { get; set; }

	[ServiceDependency]
	public ILevelManager LevelManager { get; set; }

	[ServiceDependency]
	public ILevelMaterializer LevelMaterializer { get; set; }

	[ServiceDependency]
	public IContentManagerProvider CMProvider { get; set; }

	[ServiceDependency]
	internal IScriptingManager Scripting { get; set; }

	public FakeDot(Game game)
		: base(game)
	{
		base.DrawOrder = 100000000;
		base.Visible = (base.Enabled = false);
	}

	public override void Initialize()
	{
		base.Initialize();
		Vertices = new List<Vector4>
		{
			new Vector4(-1f, -1f, -1f, -1f),
			new Vector4(1f, -1f, -1f, -1f),
			new Vector4(-1f, 1f, -1f, -1f),
			new Vector4(1f, 1f, -1f, -1f),
			new Vector4(-1f, -1f, 1f, -1f),
			new Vector4(1f, -1f, 1f, -1f),
			new Vector4(-1f, 1f, 1f, -1f),
			new Vector4(1f, 1f, 1f, -1f),
			new Vector4(-1f, -1f, -1f, 1f),
			new Vector4(1f, -1f, -1f, 1f),
			new Vector4(-1f, 1f, -1f, 1f),
			new Vector4(1f, 1f, -1f, 1f),
			new Vector4(-1f, -1f, 1f, 1f),
			new Vector4(1f, -1f, 1f, 1f),
			new Vector4(-1f, 1f, 1f, 1f),
			new Vector4(1f, 1f, 1f, 1f)
		};
		DotMesh = new Mesh
		{
			Blending = BlendingMode.Additive,
			DepthWrites = false,
			Culling = CullMode.None,
			AlwaysOnTop = true,
			Material = 
			{
				Opacity = 1f / 3f
			}
		};
		RaysMesh = new Mesh
		{
			Texture = CMProvider.Global.Load<Texture2D>("Other Textures/smooth_ray"),
			Blending = BlendingMode.Additive,
			SamplerState = SamplerState.AnisotropicClamp,
			DepthWrites = false,
			AlwaysOnTop = true
		};
		FlareMesh = new Mesh
		{
			Texture = CMProvider.Global.Load<Texture2D>("Other Textures/rainbow_flare"),
			Blending = BlendingMode.Additive,
			SamplerState = SamplerState.AnisotropicClamp,
			DepthWrites = false,
			AlwaysOnTop = true
		};
		DrawActionScheduler.Schedule(delegate
		{
			DotMesh.Effect = new DotEffect
			{
				ForcedViewMatrix = Matrix.CreateLookAt(new Vector3(0f, 0f, 10f), Vector3.Zero, Vector3.Up)
			};
			RaysMesh.Effect = new DefaultEffect.Textured
			{
				ForcedViewMatrix = Matrix.CreateLookAt(new Vector3(0f, 0f, 10f), Vector3.Zero, Vector3.Up)
			};
			FlareMesh.Effect = new DefaultEffect.Textured
			{
				ForcedViewMatrix = Matrix.CreateLookAt(new Vector3(0f, 0f, 10f), Vector3.Zero, Vector3.Up)
			};
		});
		FlareMesh.AddFace(Vector3.One, Vector3.Zero, FaceOrientation.Front, centeredOnOrigin: true);
		DotMesh.AddGroup().Geometry = (DotWireGeometry = new IndexedUserPrimitives<FezVertexPositionColor>(PrimitiveType.LineList));
		DotMesh.AddGroup().Geometry = (DotFacesGeometry = new IndexedUserPrimitives<FezVertexPositionColor>(PrimitiveType.TriangleList));
		DotWireGeometry.Vertices = new FezVertexPositionColor[16];
		for (int i = 0; i < 16; i++)
		{
			DotWireGeometry.Vertices[i].Color = new Color(1f, 1f, 1f, 1f);
		}
		DotWireGeometry.Indices = new int[64]
		{
			0, 1, 0, 2, 2, 3, 3, 1, 4, 5,
			6, 7, 4, 6, 5, 7, 4, 0, 6, 2,
			3, 7, 1, 5, 10, 11, 8, 9, 8, 10,
			9, 11, 12, 14, 14, 15, 15, 13, 12, 13,
			12, 8, 14, 10, 15, 11, 13, 9, 2, 10,
			3, 11, 0, 8, 1, 9, 6, 14, 7, 15,
			4, 12, 5, 13
		};
		DotFacesGeometry.Vertices = new FezVertexPositionColor[96];
		for (int j = 0; j < 4; j++)
		{
			for (int k = 0; k < 6; k++)
			{
				Vector3 vector = Vector3.Zero;
				switch ((k + j * 6) % 6)
				{
				case 0:
					vector = new Vector3(0f, 1f, 0.75f);
					break;
				case 1:
					vector = new Vector3(1f / 6f, 1f, 0.75f);
					break;
				case 2:
					vector = new Vector3(1f / 3f, 1f, 0.75f);
					break;
				case 3:
					vector = new Vector3(0.5f, 1f, 0.75f);
					break;
				case 4:
					vector = new Vector3(2f / 3f, 1f, 0.75f);
					break;
				case 5:
					vector = new Vector3(5f / 6f, 1f, 0.75f);
					break;
				}
				for (int l = 0; l < 4; l++)
				{
					int num = l + k * 4 + j * 24;
					DotFacesGeometry.Vertices[num].Color = new Color(vector.X, vector.Y, vector.Z);
				}
			}
		}
		FaceVertexIndices = new int[96]
		{
			0, 2, 3, 1, 1, 3, 7, 5, 5, 7,
			6, 4, 4, 6, 2, 0, 0, 4, 5, 1,
			2, 6, 7, 3, 8, 10, 11, 9, 9, 11,
			15, 13, 13, 15, 14, 12, 12, 14, 10, 8,
			8, 12, 13, 9, 10, 14, 15, 11, 0, 1,
			9, 8, 0, 2, 10, 8, 2, 3, 11, 10,
			3, 1, 9, 11, 4, 5, 13, 12, 6, 7,
			15, 14, 4, 6, 14, 12, 5, 7, 15, 13,
			4, 0, 8, 12, 6, 2, 10, 14, 3, 7,
			15, 11, 1, 5, 13, 9
		};
		DotFacesGeometry.Indices = new int[144]
		{
			0, 2, 1, 0, 3, 2, 4, 6, 5, 4,
			7, 6, 8, 10, 9, 8, 11, 10, 12, 14,
			13, 12, 15, 14, 16, 17, 18, 16, 18, 19,
			20, 22, 21, 20, 23, 22, 24, 26, 25, 24,
			27, 26, 28, 30, 29, 28, 31, 30, 32, 34,
			33, 32, 35, 34, 36, 38, 37, 36, 39, 38,
			40, 41, 42, 40, 42, 43, 44, 46, 45, 44,
			47, 46, 48, 50, 49, 48, 51, 50, 52, 54,
			53, 52, 55, 54, 56, 58, 57, 56, 59, 58,
			60, 62, 61, 60, 63, 62, 64, 65, 66, 64,
			66, 67, 68, 70, 69, 68, 71, 70, 72, 74,
			73, 72, 75, 74, 76, 78, 77, 76, 79, 78,
			80, 82, 81, 80, 83, 82, 84, 86, 85, 84,
			87, 86, 88, 89, 90, 88, 90, 91, 92, 94,
			93, 92, 95, 94
		};
	}

	public override void Update(GameTime gameTime)
	{
		Theta += (float)gameTime.ElapsedGameTime.TotalSeconds * 1f;
		float num = (float)Math.Cos(Theta);
		float num2 = (float)Math.Sin(Theta);
		Matrix matrix = new Matrix(num, 0f, 0f, num2, 0f, 1f, 0f, 0f, 0f, 0f, 1f, 0f, 0f - num2, 0f, 0f, num);
		for (int i = 0; i < Vertices.Count; i++)
		{
			Vector4 vector = Vector4.Transform(Vertices[i], matrix);
			float num3 = ((vector.W + 1f) / 3f * 1f + 0.5f) * (1f / 3f);
			DotWireGeometry.Vertices[i].Position = new Vector3(vector.X, vector.Y, vector.Z) * num3;
		}
		for (int j = 0; j < FaceVertexIndices.Length; j++)
		{
			DotFacesGeometry.Vertices[j].Position = DotWireGeometry.Vertices[FaceVertexIndices[j]].Position;
		}
		float num4 = (float)Math.Sin(gameTime.TotalGameTime.TotalSeconds / 3.0) * 0.5f + 1f;
		EightShapeStep += (float)gameTime.ElapsedGameTime.TotalSeconds * num4;
		Vector3 scale = new Vector3(4f + (float)Math.Sin(EightShapeStep * 4f / 3f) * 1.25f);
		DotMesh.Rotation = Quaternion.CreateFromAxisAngle(Vector3.Right, (float)Math.Asin(Math.Sqrt(2.0) / Math.Sqrt(3.0))) * Quaternion.CreateFromAxisAngle(Vector3.Up, (float)Math.PI / 4f);
		DotMesh.Position = Vector3.Zero;
		DotMesh.Scale = scale;
		UpdateRays((float)gameTime.ElapsedGameTime.TotalSeconds);
	}

	private void UpdateRays(float elapsedSeconds)
	{
		if (RandomHelper.Probability(0.03))
		{
			float num = 6f + RandomHelper.Centered(4.0);
			float num2 = RandomHelper.Between(0.5, num / 2.5f);
			Group group = RaysMesh.AddGroup();
			group.Geometry = new IndexedUserPrimitives<FezVertexPositionTexture>(new FezVertexPositionTexture[6]
			{
				new FezVertexPositionTexture(new Vector3(0f, num2 / 2f * 0.1f, 0f), new Vector2(0f, 0f)),
				new FezVertexPositionTexture(new Vector3(num, num2 / 2f, 0f), new Vector2(1f, 0f)),
				new FezVertexPositionTexture(new Vector3(num, num2 / 2f * 0.1f, 0f), new Vector2(1f, 0.45f)),
				new FezVertexPositionTexture(new Vector3(num, (0f - num2) / 2f * 0.1f, 0f), new Vector2(1f, 0.55f)),
				new FezVertexPositionTexture(new Vector3(num, (0f - num2) / 2f, 0f), new Vector2(1f, 1f)),
				new FezVertexPositionTexture(new Vector3(0f, (0f - num2) / 2f * 0.1f, 0f), new Vector2(0f, 1f))
			}, new int[12]
			{
				0, 1, 2, 0, 2, 5, 5, 2, 3, 5,
				3, 4
			}, PrimitiveType.TriangleList);
			group.CustomData = new DotHost.RayState();
			group.Material = new Material
			{
				Diffuse = new Vector3(0f)
			};
			group.Rotation = Quaternion.CreateFromAxisAngle(Vector3.Forward, RandomHelper.Between(0.0, 6.2831854820251465));
		}
		for (int num3 = RaysMesh.Groups.Count - 1; num3 >= 0; num3--)
		{
			Group group2 = RaysMesh.Groups[num3];
			DotHost.RayState rayState = group2.CustomData as DotHost.RayState;
			rayState.Age += elapsedSeconds * 0.15f;
			float num4 = (float)Math.Sin(rayState.Age * ((float)Math.PI * 2f) - (float)Math.PI / 2f) * 0.5f + 0.5f;
			num4 = Easing.EaseOut(num4, EasingType.Quadratic);
			group2.Material.Diffuse = new Vector3(num4 * 0.0375f) + rayState.Tint.ToVector3() * 0.075f * num4;
			float speed = rayState.Speed;
			group2.Rotation *= Quaternion.CreateFromAxisAngle(Vector3.Forward, elapsedSeconds * speed * 0.3f);
			group2.Scale = new Vector3(num4 * 0.75f + 0.25f, num4 * 0.5f + 0.5f, 1f);
			if (rayState.Age > 1f)
			{
				RaysMesh.RemoveGroupAt(num3);
			}
		}
		Mesh flareMesh = FlareMesh;
		Vector3 position2 = (RaysMesh.Position = DotMesh.Position);
		flareMesh.Position = position2;
		Mesh flareMesh2 = FlareMesh;
		Quaternion rotation = (RaysMesh.Rotation = Quaternion.Identity);
		flareMesh2.Rotation = rotation;
		RaysMesh.Scale = DotMesh.Scale * 0.5f;
		FlareMesh.Scale = new Vector3(MathHelper.Lerp(DotMesh.Scale.X * 0.875f, (float)Math.Pow(DotMesh.Scale.X * 1.5f, 1.5), 1f));
		FlareMesh.Material.Diffuse = new Vector3(0.25f * FezMath.Saturate(Opacity * 2f));
	}

	public override void Draw(GameTime gameTime)
	{
		float aspectRatio = base.GraphicsDevice.Viewport.AspectRatio;
		BaseEffect effect = RaysMesh.Effect;
		BaseEffect effect2 = FlareMesh.Effect;
		Matrix? matrix2 = (DotMesh.Effect.ForcedProjectionMatrix = Matrix.CreateOrthographic(14f * aspectRatio, 14f, 0.1f, 100f));
		Matrix? forcedProjectionMatrix = (effect2.ForcedProjectionMatrix = matrix2);
		effect.ForcedProjectionMatrix = forcedProjectionMatrix;
		FlareMesh.Draw();
		RaysMesh.Draw();
		base.GraphicsDevice.PrepareStencilWrite(StencilMask.Dot);
		(DotMesh.Effect as DotEffect).UpdateHueOffset(gameTime.ElapsedGameTime);
		DotMesh.Blending = BlendingMode.Alphablending;
		DotMesh.Material.Diffuse = new Vector3(0f);
		DotMesh.Material.Opacity = (((double)Opacity > 0.5) ? (Opacity * 0.25f) : 0f);
		DotMesh.Draw();
		DotMesh.Groups[0].Enabled = true;
		DotMesh.Groups[1].Enabled = false;
		DotMesh.Blending = BlendingMode.Additive;
		float num = (float)Math.Pow(Math.Sin(gameTime.TotalGameTime.TotalSeconds * 2.0) * 0.5 + 0.5, 3.0);
		DotMesh.Material.Opacity = 1f;
		DotMesh.Material.Diffuse = new Vector3(num * 0.5f * Opacity);
		DotMesh.Draw();
		DotMesh.Groups[0].Enabled = false;
		DotMesh.Groups[1].Enabled = true;
		DotMesh.Material.Diffuse = new Vector3(Opacity);
		DotMesh.Draw();
		base.GraphicsDevice.PrepareStencilRead(CompareFunction.Always, StencilMask.None);
	}
}
