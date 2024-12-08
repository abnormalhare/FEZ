using System;
using Microsoft.Xna.Framework;

namespace FezEngine.Services;

public interface ICameraProvider
{
	Matrix Projection { get; }

	Matrix View { get; }

	event Action ViewChanged;

	event Action ProjectionChanged;
}
