using System;
using System.Collections.Generic;
using FezEngine.Structure;
using Microsoft.Xna.Framework;

namespace FezEngine.Services;

public interface ILightsManager
{
	int DirectionalLightsCount { get; }

	int PointLightsCount { get; }

	IList<DirectionalLight> DirectionalLights { get; }

	IList<PointLight> PointLights { get; }

	Vector3 GlobalAmbient { get; set; }

	event Action<LightEventArgs> DirectionalLightAdded;

	event Action<LightEventArgs> DirectionalLightChanged;

	event Action<LightEventArgs> DirectionalLightRemoved;

	event Action<LightEventArgs> PointLightAdded;

	event Action<LightEventArgs> PointLightChanged;

	event Action<LightEventArgs> PointLightRemoved;

	event Action GlobalAmbientChanged;

	DirectionalLight GetDirectionalLight(int lightNumber);

	PointLight GetPointLight(int lightNumber);

	void OnDirectionalLightAdded(int newIndex);

	void OnDirectionalLightChanged(int index);

	void OnDirectionalLightRemoved(int oldIndex);

	void OnPointLightAdded(int newIndex);

	void OnPointLightChanged(int index);

	void OnPointLightRemoved(int oldIndex);
}
