using System;
using System.Linq;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Tools;
using FezGame.Services;
using Microsoft.Xna.Framework;

namespace FezGame.Components;

internal class OrreryHost : GameComponent
{
	private ArtObjectInstance Moon;

	private ArtObjectInstance Earth;

	private ArtObjectInstance Sun;

	private ArtObjectInstance PlanetW;

	private ArtObjectInstance MoonBranch;

	private ArtObjectInstance EarthBranch;

	private ArtObjectInstance SubBranch;

	private ArtObjectInstance SunBranch;

	private ArtObjectInstance PlanetWBranch;

	private ArtObjectInstance SmallGear1;

	private ArtObjectInstance SmallGear2;

	private ArtObjectInstance MediumGear;

	private ArtObjectInstance LargeGear1;

	private ArtObjectInstance LargeGear2;

	private float MoonBranchHeight;

	private float EarthBranchHeight;

	private float PlanetWBranchHeight;

	private float MoonDistance;

	private float EarthDistance;

	private float PlanetWDistance;

	[ServiceDependency]
	public ILevelManager LevelManager { private get; set; }

	[ServiceDependency]
	public IGameStateManager GameState { private get; set; }

	public OrreryHost(Game game)
		: base(game)
	{
	}

	public override void Initialize()
	{
		LevelManager.LevelChanged += TryInitialize;
		TryInitialize();
	}

	private void TryInitialize()
	{
		base.Enabled = LevelManager.Name != null && LevelManager.Name == "ORRERY";
		Moon = (Earth = (Sun = (PlanetW = null)));
		MoonBranch = (EarthBranch = (SubBranch = (SunBranch = (PlanetWBranch = null))));
		SmallGear1 = (SmallGear2 = (MediumGear = (LargeGear1 = (LargeGear2 = null))));
		if (base.Enabled)
		{
			Moon = LevelManager.ArtObjects.Values.Single((ArtObjectInstance x) => x.ArtObjectName == "ORR_MOONAO");
			Earth = LevelManager.ArtObjects.Values.Single((ArtObjectInstance x) => x.ArtObjectName == "ORR_EARTHAO");
			Sun = LevelManager.ArtObjects.Values.Single((ArtObjectInstance x) => x.ArtObjectName == "ORR_SUNAO");
			PlanetW = LevelManager.ArtObjects.Values.Single((ArtObjectInstance x) => x.ArtObjectName == "ORR_PLANET_WAO");
			MoonBranch = LevelManager.ArtObjects.Values.Single((ArtObjectInstance x) => x.ArtObjectName == "ORR_MOON_BRANCHAO");
			EarthBranch = LevelManager.ArtObjects.Values.Single((ArtObjectInstance x) => x.ArtObjectName == "ORR_EARTH_BRANCHAO");
			SubBranch = LevelManager.ArtObjects.Values.First((ArtObjectInstance x) => x.ArtObjectName == "ORR_SUN_BRANCHAO");
			SunBranch = LevelManager.ArtObjects.Values.Where((ArtObjectInstance x) => x.ArtObjectName == "ORR_SUN_BRANCHAO").Skip(1).First();
			PlanetWBranch = LevelManager.ArtObjects.Values.Single((ArtObjectInstance x) => x.ArtObjectName == "ORR_W_BRANCHAO");
			SmallGear1 = LevelManager.ArtObjects.Values.First((ArtObjectInstance x) => x.ArtObjectName == "ORR_COG_SAO");
			SmallGear2 = LevelManager.ArtObjects.Values.Where((ArtObjectInstance x) => x.ArtObjectName == "ORR_COG_SAO").Skip(1).First();
			MediumGear = LevelManager.ArtObjects.Values.First((ArtObjectInstance x) => x.ArtObjectName == "ORR_COG_MAO");
			LargeGear1 = LevelManager.ArtObjects.Values.First((ArtObjectInstance x) => x.ArtObjectName == "ORR_COG_LAO");
			LargeGear2 = LevelManager.ArtObjects.Values.Where((ArtObjectInstance x) => x.ArtObjectName == "ORR_COG_LAO").Skip(1).First();
			MoonDistance = Vector3.Distance(Earth.Position, Moon.Position);
			EarthDistance = Vector3.Distance(Sun.Position, Earth.Position);
			PlanetWDistance = Vector3.Distance(Sun.Position, PlanetW.Position);
			MoonBranchHeight = MoonBranch.Position.Y;
			EarthBranchHeight = EarthBranch.Position.Y;
			PlanetWBranchHeight = PlanetWBranch.Position.Y;
		}
	}

	public override void Update(GameTime gameTime)
	{
		if (!GameState.Loading && !GameState.Paused && !GameState.InMap && !GameState.InMenuCube && !GameState.InFpsMode)
		{
			float num = (float)gameTime.ElapsedGameTime.TotalSeconds * 0.5f;
			Sun.Rotation *= Quaternion.CreateFromAxisAngle(Vector3.UnitY, num * 1f);
			Earth.Rotation *= Quaternion.CreateFromAxisAngle(Vector3.UnitY, num * 0.75f);
			Moon.Rotation *= Quaternion.CreateFromAxisAngle(Vector3.UnitY, num * 0.5f);
			PlanetW.Rotation *= Quaternion.CreateFromAxisAngle(Vector3.UnitY, num * 0.25f);
			Earth.Position = Sun.Position + Vector3.Transform(Vector3.UnitZ, Earth.Rotation) * EarthDistance;
			Moon.Position = Earth.Position + Vector3.Transform(Vector3.Right, Moon.Rotation) * MoonDistance;
			PlanetW.Position = Sun.Position + Vector3.Transform(Vector3.Left, PlanetW.Rotation) * PlanetWDistance;
			SmallGear1.Rotation *= Quaternion.CreateFromAxisAngle(Vector3.UnitY, num * 3f);
			SmallGear2.Rotation *= Quaternion.CreateFromAxisAngle(Vector3.UnitY, num * -3f);
			MediumGear.Rotation *= Quaternion.CreateFromAxisAngle(Vector3.UnitY, num * -1f);
			LargeGear1.Rotation *= Quaternion.CreateFromAxisAngle(Vector3.UnitY, num * 0.5f);
			LargeGear2.Rotation *= Quaternion.CreateFromAxisAngle(Vector3.UnitY, num * -0.5f);
			SunBranch.Rotation = Sun.Rotation;
			SubBranch.Rotation = Sun.Rotation;
			(Matrix.CreateTranslation(1.875f, 0f, -1.875f) * Matrix.CreateFromQuaternion(Earth.Rotation * Quaternion.CreateFromAxisAngle(Vector3.UnitY, -(float)Math.PI / 2f)) * Matrix.CreateTranslation(Sun.Position * FezMath.XZMask + Vector3.UnitY * EarthBranchHeight)).Decompose(out var scale, out var rotation, out var translation);
			EarthBranch.Position = translation;
			EarthBranch.Rotation = rotation;
			(Matrix.CreateTranslation(1.875f, 0f, 1.875f) * Matrix.CreateFromQuaternion(Moon.Rotation) * Matrix.CreateTranslation(Earth.Position * FezMath.XZMask + Vector3.UnitY * MoonBranchHeight)).Decompose(out scale, out rotation, out translation);
			MoonBranch.Position = translation;
			MoonBranch.Rotation = rotation;
			(Matrix.CreateTranslation(3.875f, 0f, -3.875f) * Matrix.CreateFromQuaternion(PlanetW.Rotation * Quaternion.CreateFromAxisAngle(Vector3.UnitY, (float)Math.PI)) * Matrix.CreateTranslation(Sun.Position * FezMath.XZMask + Vector3.UnitY * PlanetWBranchHeight)).Decompose(out scale, out rotation, out translation);
			PlanetWBranch.Position = translation;
			PlanetWBranch.Rotation = rotation;
		}
	}
}
