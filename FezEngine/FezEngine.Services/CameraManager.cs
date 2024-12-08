using System;
using Common;
using Microsoft.Xna.Framework;

namespace FezEngine.Services;

public abstract class CameraManager : GameComponent, ICameraProvider
{
	protected Matrix view = Matrix.Identity;

	protected Matrix projection = Matrix.Identity;

	public Matrix View => view;

	public Matrix Projection => projection;

	public event Action ViewChanged = Util.NullAction;

	public event Action ProjectionChanged = Util.NullAction;

	protected CameraManager(Game game)
		: base(game)
	{
		base.UpdateOrder = 10;
	}

	protected virtual void OnViewChanged()
	{
		this.ViewChanged();
	}

	protected virtual void OnProjectionChanged()
	{
		this.ProjectionChanged();
	}
}
