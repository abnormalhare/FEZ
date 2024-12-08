using System;
using Common;
using FezEngine.Tools;
using Microsoft.Xna.Framework;

namespace FezEngine.Services;

public class FogManager : GameComponent, IFogManager
{
	private FogType type;

	private Color color;

	private float density;

	private float start;

	private float end;

	public FogType Type
	{
		get
		{
			return type;
		}
		set
		{
			type = value;
			this.FogSettingsChanged();
		}
	}

	public Color Color
	{
		get
		{
			return color;
		}
		set
		{
			if (color != value)
			{
				color = value;
				this.FogSettingsChanged();
			}
		}
	}

	public float Density
	{
		get
		{
			return density;
		}
		set
		{
			density = value;
			this.FogSettingsChanged();
		}
	}

	public float Start
	{
		get
		{
			return start;
		}
		set
		{
			start = value;
			this.FogSettingsChanged();
		}
	}

	public float End
	{
		get
		{
			return end;
		}
		set
		{
			end = value;
			this.FogSettingsChanged();
		}
	}

	[ServiceDependency]
	public ILevelManager LevelManager { private get; set; }

	public event Action FogSettingsChanged = Util.NullAction;

	public FogManager(Game game)
		: base(game)
	{
	}

	public override void Initialize()
	{
		LevelManager.LevelChanged += delegate
		{
			type = FogType.ExponentialSquared;
			if (LevelManager.Sky != null)
			{
				density = LevelManager.Sky.FogDensity;
				this.FogSettingsChanged();
			}
		};
	}
}
