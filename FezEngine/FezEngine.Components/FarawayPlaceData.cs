using FezEngine.Structure;
using Microsoft.Xna.Framework;

namespace FezEngine.Components;

internal struct FarawayPlaceData
{
	public Mesh WaterBodyMesh;

	public Vector3 OriginalCenter;

	public Viewpoint Viewpoint;

	public Volume Volume;

	public Vector3 DestinationOffset;

	public float? WaterLevelOffset;

	public string DestinationLevelName;

	public float DestinationWaterLevel;

	public float DestinationLevelSize;
}
