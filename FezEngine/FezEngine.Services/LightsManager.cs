using System;
using System.Collections.Generic;
using Common;
using FezEngine.Structure;
using Microsoft.Xna.Framework;

namespace FezEngine.Services;

public class LightsManager : ILightsManager
{
	private readonly List<PointLight> pointLights;

	private readonly List<DirectionalLight> directionalLights;

	private Vector3 globalAmbient;

	public IList<DirectionalLight> DirectionalLights => directionalLights;

	public IList<PointLight> PointLights => pointLights;

	public int DirectionalLightsCount => directionalLights.Count;

	public int PointLightsCount => directionalLights.Count;

	public Vector3 GlobalAmbient
	{
		get
		{
			return globalAmbient;
		}
		set
		{
			globalAmbient = value;
			this.GlobalAmbientChanged();
		}
	}

	public event Action<LightEventArgs> DirectionalLightAdded = Util.NullAction;

	public event Action<LightEventArgs> DirectionalLightRemoved = Util.NullAction;

	public event Action<LightEventArgs> DirectionalLightChanged = Util.NullAction;

	public event Action<LightEventArgs> PointLightAdded = Util.NullAction;

	public event Action<LightEventArgs> PointLightChanged = Util.NullAction;

	public event Action<LightEventArgs> PointLightRemoved = Util.NullAction;

	public event Action GlobalAmbientChanged = Util.NullAction;

	public LightsManager()
	{
		directionalLights = new List<DirectionalLight>();
		pointLights = new List<PointLight>();
	}

	public DirectionalLight GetDirectionalLight(int lightNumber)
	{
		return directionalLights[lightNumber];
	}

	public PointLight GetPointLight(int lightNumber)
	{
		return pointLights[lightNumber];
	}

	public void OnDirectionalLightAdded(int newIndex)
	{
		this.DirectionalLightAdded(new LightEventArgs(newIndex));
	}

	public void OnDirectionalLightChanged(int index)
	{
		this.DirectionalLightChanged(new LightEventArgs(index));
	}

	public void OnDirectionalLightRemoved(int oldIndex)
	{
		this.DirectionalLightRemoved(new LightEventArgs(oldIndex));
	}

	public void OnPointLightAdded(int newIndex)
	{
		this.PointLightAdded(new LightEventArgs(newIndex));
	}

	public void OnPointLightChanged(int index)
	{
		this.PointLightChanged(new LightEventArgs(index));
	}

	public void OnPointLightRemoved(int oldIndex)
	{
		this.PointLightRemoved(new LightEventArgs(oldIndex));
	}
}
