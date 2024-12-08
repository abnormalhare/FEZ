using System;
using FezEngine.Structure.Geometry;

namespace FezEngine.Structure;

public struct FaceMaterialization<T> where T : struct, IEquatable<T>, IVertex
{
	private SharedVertex<T> i0;

	private SharedVertex<T> i1;

	private SharedVertex<T> i2;

	private SharedVertex<T> i3;

	private SharedVertex<T> i4;

	private SharedVertex<T> i5;

	public SharedVertex<T> V0 { get; set; }

	public SharedVertex<T> V1 { get; set; }

	public SharedVertex<T> V2 { get; set; }

	public SharedVertex<T> V3 { get; set; }

	public int GetIndex(ushort relativeIndex)
	{
		return relativeIndex switch
		{
			0 => i0.Index, 
			1 => i1.Index, 
			2 => i2.Index, 
			3 => i3.Index, 
			4 => i4.Index, 
			_ => i5.Index, 
		};
	}

	public void SetupIndices(FaceOrientation face)
	{
		if (face == FaceOrientation.Front || face == FaceOrientation.Top || face == FaceOrientation.Right)
		{
			i0 = V0;
			i1 = V1;
			i2 = V2;
			i3 = V0;
			i4 = V2;
			i5 = V3;
		}
		else
		{
			i0 = V0;
			i1 = V2;
			i2 = V1;
			i3 = V0;
			i4 = V3;
			i5 = V2;
		}
	}
}
