using System;
using FezEngine;
using FezEngine.Structure;

namespace FezGame.Components;

public class WarpPanel
{
	public string Destination;

	public Mesh PanelMask;

	public Mesh Layers;

	public FaceOrientation Face;

	public TimeSpan Timer;

	public bool Enabled;
}
