using FezEngine;
using FezEngine.Services;
using Microsoft.Xna.Framework;

namespace FezGame.Services;

public interface IGameCameraManager : IDefaultCameraManager, ICameraProvider
{
	Viewpoint RequestedViewpoint { get; set; }

	Vector3 OriginalDirection { get; set; }

	void RecordNewCarriedInstancePhi();

	void CancelViewTransition();
}
