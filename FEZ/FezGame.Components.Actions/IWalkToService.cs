using System;
using FezGame.Structure;
using Microsoft.Xna.Framework;

namespace FezGame.Components.Actions;

public interface IWalkToService
{
	Func<Vector3> Destination { get; set; }

	ActionType NextAction { get; set; }
}
