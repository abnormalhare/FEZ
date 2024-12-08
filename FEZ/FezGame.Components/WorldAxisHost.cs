using FezEngine;
using FezEngine.Effects;
using FezEngine.Structure;
using Microsoft.Xna.Framework;

namespace FezGame.Components;

internal class WorldAxisHost : DrawableGameComponent
{
	private Mesh axisMesh;

	public WorldAxisHost(Game game)
		: base(game)
	{
	}

	public override void Initialize()
	{
		axisMesh = new Mesh
		{
			AlwaysOnTop = true
		};
		axisMesh.AddWireframeArrow(1f, 0.1f, Vector3.Zero, FaceOrientation.Right, Color.Red);
		axisMesh.AddWireframeArrow(1f, 0.1f, Vector3.Zero, FaceOrientation.Top, Color.Green);
		axisMesh.AddWireframeArrow(1f, 0.1f, Vector3.Zero, FaceOrientation.Front, Color.Blue);
		base.DrawOrder = 1000;
		base.Initialize();
	}

	protected override void LoadContent()
	{
		axisMesh.Effect = new DefaultEffect.VertexColored();
	}

	public override void Draw(GameTime gameTime)
	{
		axisMesh.Draw();
	}
}
