using System;
using System.Collections.Generic;
using FezEngine.Structure.Geometry;
using Microsoft.Xna.Framework;

namespace FezEngine.Tools;

public static class FezMath
{
	public const float Epsilon = 0.001f;

	public static readonly Vector3 HalfVector = new Vector3(0.5f);

	public static readonly Vector3 XZMask = new Vector3(1f, 0f, 1f);

	private static readonly double Log2 = Math.Log(2.0);

	private static readonly Vector3[] tempCorners = new Vector3[8];

	public static float DoubleIter(float currentValue, float iterationTime, float timeUntilDoubles)
	{
		float num = timeUntilDoubles / iterationTime;
		double num2 = ((timeUntilDoubles == 0f) ? 0.0 : Math.Pow(2.0, 1f / num));
		return currentValue * (float)num2;
	}

	public static float GetReachFactor(float oldFactor, float dt)
	{
		return Saturate(1f - (float)Math.Pow(Math.Pow(0f - oldFactor + 1f, 58.8235294117647), dt));
	}

	public static double Saturate(double value)
	{
		if (!(value < 0.0))
		{
			if (!(value > 1.0))
			{
				return value;
			}
			return 1.0;
		}
		return 0.0;
	}

	public static float Saturate(float value)
	{
		if (!(value < 0f))
		{
			if (!(value > 1f))
			{
				return value;
			}
			return 1f;
		}
		return 0f;
	}

	public static Vector3 Saturate(this Vector3 vector)
	{
		return new Vector3(Saturate(vector.X), Saturate(vector.Y), Saturate(vector.Z));
	}

	public static Vector3 MaxClamp(this Vector3 vector)
	{
		Vector3 vector2 = vector.Abs();
		if (vector2.X >= vector2.Y && vector2.X >= vector2.Z)
		{
			return new Vector3(Math.Sign(vector.X), 0f, 0f);
		}
		if (vector2.Y >= vector2.X && vector2.Y >= vector2.Z)
		{
			return new Vector3(0f, Math.Sign(vector.Y), 0f);
		}
		if (vector2.Z >= vector2.X && vector2.Z >= vector2.Y)
		{
			return new Vector3(0f, 0f, Math.Sign(vector.Z));
		}
		return Vector3.Zero;
	}

	public static Vector3 MaxClampXZ(this Vector3 vector)
	{
		Vector3 vector2 = vector.Abs();
		if (vector2.X >= vector2.Z)
		{
			return new Vector3(Math.Sign(vector.X), 0f, 0f);
		}
		return new Vector3(0f, 0f, Math.Sign(vector.Z));
	}

	public static Vector3 UpVector(this Viewpoint view)
	{
		return view switch
		{
			Viewpoint.Back => Vector3.Up, 
			Viewpoint.Front => Vector3.Up, 
			Viewpoint.Left => Vector3.Up, 
			Viewpoint.Right => Vector3.Up, 
			Viewpoint.Down => Vector3.Backward, 
			Viewpoint.Up => Vector3.Backward, 
			_ => Vector3.Zero, 
		};
	}

	public static Vector3 RightVector(this Viewpoint view)
	{
		return view switch
		{
			Viewpoint.Back => Vector3.Left, 
			Viewpoint.Front => Vector3.Right, 
			Viewpoint.Left => Vector3.Backward, 
			Viewpoint.Right => Vector3.Forward, 
			Viewpoint.Down => Vector3.Right, 
			Viewpoint.Up => Vector3.Right, 
			_ => Vector3.Zero, 
		};
	}

	public static Vector3 ScreenSpaceMask(this Viewpoint view)
	{
		return view switch
		{
			Viewpoint.Back => new Vector3(1f, 1f, 0f), 
			Viewpoint.Front => new Vector3(1f, 1f, 0f), 
			Viewpoint.Left => new Vector3(0f, 1f, 1f), 
			Viewpoint.Right => new Vector3(0f, 1f, 1f), 
			Viewpoint.Down => new Vector3(1f, 1f, 0f), 
			Viewpoint.Up => new Vector3(1f, 1f, 0f), 
			_ => Vector3.Zero, 
		};
	}

	public static Vector3 SideMask(this Viewpoint view)
	{
		return view switch
		{
			Viewpoint.Back => Vector3.Right, 
			Viewpoint.Front => Vector3.Right, 
			Viewpoint.Left => Vector3.Backward, 
			Viewpoint.Right => Vector3.Backward, 
			Viewpoint.Down => Vector3.Right, 
			Viewpoint.Up => Vector3.Right, 
			_ => Vector3.Zero, 
		};
	}

	public static Vector3 ForwardVector(this Viewpoint view)
	{
		return view switch
		{
			Viewpoint.Back => Vector3.Backward, 
			Viewpoint.Front => Vector3.Forward, 
			Viewpoint.Left => Vector3.Right, 
			Viewpoint.Right => Vector3.Left, 
			Viewpoint.Down => Vector3.Down, 
			Viewpoint.Up => Vector3.Up, 
			_ => Vector3.Zero, 
		};
	}

	public static Vector3 DepthMask(this Viewpoint view)
	{
		return view switch
		{
			Viewpoint.Back => Vector3.UnitZ, 
			Viewpoint.Front => Vector3.UnitZ, 
			Viewpoint.Left => Vector3.UnitX, 
			Viewpoint.Right => Vector3.UnitX, 
			Viewpoint.Down => Vector3.UnitY, 
			Viewpoint.Up => Vector3.UnitY, 
			_ => Vector3.Zero, 
		};
	}

	public static Vector2 ComputeTexCoord<T>(this T vertex) where T : ILitVertex, ITexturedVertex
	{
		return vertex.ComputeTexCoord(Vector3.One);
	}

	public static Vector2 ComputeTexCoord<T>(this T vertex, Vector3 trileSize) where T : ILitVertex, ITexturedVertex
	{
		FaceOrientation faceOrientation = OrientationFromDirection(vertex.Normal);
		Vector2 vector = faceOrientation switch
		{
			FaceOrientation.Front => Vector2.Zero, 
			FaceOrientation.Right => new Vector2(0.25f, 0f), 
			FaceOrientation.Back => new Vector2(0.375f, 0f), 
			FaceOrientation.Left => new Vector2(0.375f, 0f), 
			FaceOrientation.Top => new Vector2(0.5f, 0f), 
			_ => new Vector2(0.625f, 0f), 
		};
		Vector3 vector2 = (Vector3.One - vertex.Normal.Abs()) * (vertex.Position / trileSize) * 2f + vertex.Normal;
		vector2 = vector2 / 2f + new Vector3(0.5f);
		float num = faceOrientation.AsViewpoint().RightVector().Dot(vector2);
		float num2 = faceOrientation.AsViewpoint().UpVector().Dot(vector2);
		if (faceOrientation != FaceOrientation.Top)
		{
			num2 = 1f - num2;
		}
		return new Vector2(vector.X + num / 8f, vector.Y + num2);
	}

	public static int NextPowerOfTwo(double value)
	{
		return (int)Math.Pow(2.0, Math.Ceiling(Math.Log(value) / Log2));
	}

	public static float Log(double b, double r)
	{
		return (float)(Math.Log(b) / Math.Log(r));
	}

	public static bool In<T>(T value, T value1, T value2, IEqualityComparer<T> comparer)
	{
		if (!comparer.Equals(value, value1))
		{
			return comparer.Equals(value, value2);
		}
		return true;
	}

	public static bool In<T>(T value, T value1, T value2, T value3, IEqualityComparer<T> comparer)
	{
		if (!In(value, value1, value2, comparer))
		{
			return comparer.Equals(value, value3);
		}
		return true;
	}

	public static bool In<T>(T value, T value1, T value2, T value3, T value4, IEqualityComparer<T> comparer)
	{
		if (!In(value, value1, value2, value3, comparer))
		{
			return comparer.Equals(value, value4);
		}
		return true;
	}

	public static bool In<T>(T value, T value1, T value2, T value3, T value4, T value5, IEqualityComparer<T> comparer)
	{
		if (!In(value, value1, value2, value3, value4, comparer))
		{
			return comparer.Equals(value, value5);
		}
		return true;
	}

	public static bool In<T>(T value, T value1, T value2, T value3, T value4, T value5, T value6, IEqualityComparer<T> comparer)
	{
		if (!In(value, value1, value2, value3, value4, value5, comparer))
		{
			return comparer.Equals(value, value6);
		}
		return true;
	}

	public static bool In<T>(T value, T value1, T value2, T value3, T value4, T value5, T value6, T value7, IEqualityComparer<T> comparer)
	{
		if (!In(value, value1, value2, value3, value4, value5, value6, comparer))
		{
			return comparer.Equals(value, value7);
		}
		return true;
	}

	public static bool In<T>(T value, T value1, T value2, T value3, T value4, T value5, T value6, T value7, T value8, IEqualityComparer<T> comparer)
	{
		if (!In(value, value1, value2, value3, value4, value5, value6, value7, comparer))
		{
			return comparer.Equals(value, value8);
		}
		return true;
	}

	public static bool In<T>(T value, T[] values) where T : IEquatable<T>
	{
		foreach (T other in values)
		{
			if (value.Equals(other))
			{
				return true;
			}
		}
		return false;
	}

	public static bool In<T>(T value, T value1, T value2) where T : IEquatable<T>
	{
		if (!value.Equals(value1))
		{
			return value.Equals(value2);
		}
		return true;
	}

	public static bool In<T>(T value, T value1, T value2, T value3) where T : IEquatable<T>
	{
		if (!In(value, value1, value2))
		{
			return value.Equals(value3);
		}
		return true;
	}

	public static bool In<T>(T value, T value1, T value2, T value3, T value4) where T : IEquatable<T>
	{
		if (!In(value, value1, value2, value3))
		{
			return value.Equals(value4);
		}
		return true;
	}

	public static bool In<T>(T value, T value1, T value2, T value3, T value4, T value5) where T : IEquatable<T>
	{
		if (!In(value, value1, value2, value3, value4))
		{
			return value.Equals(value5);
		}
		return true;
	}

	public static bool In<T>(T value, T value1, T value2, T value3, T value4, T value5, T value6) where T : IEquatable<T>
	{
		if (!In(value, value1, value2, value3, value4, value5))
		{
			return value.Equals(value6);
		}
		return true;
	}

	public static bool In<T>(T value, T value1, T value2, T value3, T value4, T value5, T value6, T value7) where T : IEquatable<T>
	{
		if (!In(value, value1, value2, value3, value4, value5, value6))
		{
			return value.Equals(value7);
		}
		return true;
	}

	public static bool In<T>(T value, T value1, T value2, T value3, T value4, T value5, T value6, T value7, T value8) where T : IEquatable<T>
	{
		if (!In(value, value1, value2, value3, value4, value5, value6, value7))
		{
			return value.Equals(value8);
		}
		return true;
	}

	public static Vector3 Max(Vector3 first, Vector3 second)
	{
		Vector3 vector = first;
		return new Vector3(Math.Max(vector.X, second.X), Math.Max(vector.Y, second.Y), Math.Max(vector.Z, second.Z));
	}

	public static T Max<T>(T first, T second) where T : IComparable<T>
	{
		T val = first;
		return (val.CompareTo(second) > 0) ? val : second;
	}

	public static T Max<T>(T first, T second, T third) where T : IComparable<T>
	{
		T val = first;
		val = ((val.CompareTo(second) > 0) ? val : second);
		return (val.CompareTo(third) > 0) ? val : third;
	}

	public static Vector3 Min(Vector3 first, Vector3 second)
	{
		Vector3 vector = first;
		return new Vector3(Math.Min(vector.X, second.X), Math.Min(vector.Y, second.Y), Math.Min(vector.Z, second.Z));
	}

	public static T Min<T>(T first, T second) where T : IComparable<T>
	{
		T val = first;
		return (val.CompareTo(second) < 0) ? val : second;
	}

	public static T Min<T>(T first, T second, T third) where T : IComparable<T>
	{
		T val = first;
		val = ((val.CompareTo(second) < 0) ? val : second);
		return (val.CompareTo(third) < 0) ? val : third;
	}

	public static T Coalesce<T>(T first, T second, IEqualityComparer<T> comparer) where T : struct
	{
		T val = default(T);
		if (!comparer.Equals(val, first))
		{
			return first;
		}
		if (!comparer.Equals(val, second))
		{
			return second;
		}
		return val;
	}

	public static T Coalesce<T>(T first, T second, T third, IEqualityComparer<T> comparer) where T : struct
	{
		T val = default(T);
		if (!comparer.Equals(val, first))
		{
			return first;
		}
		if (!comparer.Equals(val, second))
		{
			return second;
		}
		if (!comparer.Equals(val, third))
		{
			return third;
		}
		return val;
	}

	public static T Coalesce<T>(T first, T second) where T : struct, IEquatable<T>
	{
		T val = default(T);
		if (!first.Equals(val))
		{
			return first;
		}
		if (!second.Equals(val))
		{
			return second;
		}
		return val;
	}

	public static T Coalesce<T>(T first, T second, T third) where T : struct, IEquatable<T>
	{
		T val = default(T);
		if (!first.Equals(val))
		{
			return first;
		}
		if (!second.Equals(val))
		{
			return second;
		}
		if (!third.Equals(val))
		{
			return third;
		}
		return val;
	}

	public static int Round(double value)
	{
		if (value < 0.0)
		{
			return (int)(value - 0.5);
		}
		return (int)(value + 0.5);
	}

	public static Vector2 Round(this Vector2 v)
	{
		return new Vector2(Round(v.X), Round(v.Y));
	}

	public static Vector2 Round(this Vector2 v, int d)
	{
		return new Vector2((float)Math.Round(v.X, d), (float)Math.Round(v.Y, d));
	}

	public static Vector3 Round(this Vector3 v)
	{
		return new Vector3(Round(v.X), Round(v.Y), Round(v.Z));
	}

	public static Vector3 Round(this Vector3 v, int d)
	{
		return new Vector3((float)Math.Round(v.X, d), (float)Math.Round(v.Y, d), (float)Math.Round(v.Z, d));
	}

	public static Vector3 Floor(this Vector3 v)
	{
		return new Vector3((float)Math.Floor(v.X), (float)Math.Floor(v.Y), (float)Math.Floor(v.Z));
	}

	public static Vector3 Ceiling(this Vector3 v)
	{
		return new Vector3((float)Math.Ceiling(v.X), (float)Math.Ceiling(v.Y), (float)Math.Ceiling(v.Z));
	}

	public static Vector3 AlmostClamp(Vector3 v, float epsilon)
	{
		v.X = AlmostClamp(v.X, epsilon);
		v.Y = AlmostClamp(v.Y, epsilon);
		v.Z = AlmostClamp(v.Z, epsilon);
		return v;
	}

	public static Vector3 AlmostClamp(Vector3 v)
	{
		return AlmostClamp(v, 0.001f);
	}

	public static float AlmostClamp(float x, float epsilon)
	{
		if (AlmostEqual(x, 0f, epsilon))
		{
			return 0f;
		}
		if (AlmostEqual(x, 1f, epsilon))
		{
			return 1f;
		}
		if (AlmostEqual(x, -1f, epsilon))
		{
			return -1f;
		}
		return x;
	}

	public static float AlmostClamp(float x)
	{
		return AlmostClamp(x, 0.001f);
	}

	public static bool AlmostEqual(double a, double b, double epsilon)
	{
		return Math.Abs(a - b) <= epsilon;
	}

	public static bool AlmostEqual(double a, double b)
	{
		return Math.Abs(a - b) <= 0.0010000000474974513;
	}

	public static bool AlmostEqual(float a, float b, float epsilon)
	{
		return Math.Abs(a - b) <= epsilon;
	}

	public static bool AlmostEqual(float a, float b)
	{
		return Math.Abs(a - b) <= 0.001f;
	}

	public static bool AlmostEqual(Vector3 a, Vector3 b)
	{
		return AlmostEqual(a, b, 0.001f);
	}

	public static bool AlmostEqual(Vector3 a, Vector3 b, float epsilon)
	{
		if (AlmostEqual(a.X, b.X, epsilon) && AlmostEqual(a.Y, b.Y, epsilon))
		{
			return AlmostEqual(a.Z, b.Z, epsilon);
		}
		return false;
	}

	public static bool AlmostEqual(Quaternion a, Quaternion b)
	{
		return AlmostEqual(a, b, 0.001f);
	}

	public static bool AlmostEqual(Quaternion a, Quaternion b, float epsilon)
	{
		if (AlmostEqual(a.X, b.X, epsilon) && AlmostEqual(a.Y, b.Y, epsilon) && AlmostEqual(a.Z, b.Z, epsilon))
		{
			return AlmostEqual(a.W, b.W, epsilon);
		}
		return false;
	}

	public static bool AlmostEqual(Vector2 a, Vector2 b)
	{
		return AlmostEqual(a, b, 0.001f);
	}

	public static bool AlmostEqual(Vector2 a, Vector2 b, float epsilon)
	{
		if (AlmostEqual(a.X, b.X, epsilon))
		{
			return AlmostEqual(a.Y, b.Y, epsilon);
		}
		return false;
	}

	public static bool AlmostWrapEqual(Vector2 a, Vector2 b)
	{
		return AlmostWrapEqual(a, b, 0.001f);
	}

	public static bool AlmostWrapEqual(Vector2 a, Vector2 b, float epsilon)
	{
		if (AlmostWrapEqual(a.X, b.X, epsilon))
		{
			return AlmostWrapEqual(a.Y, b.Y, epsilon);
		}
		return false;
	}

	public static bool AlmostWrapEqual(float a, float b)
	{
		return AlmostWrapEqual(a, b, 0.001f);
	}

	public static bool AlmostWrapEqual(float a, float b, float epsilon)
	{
		Vector2 value = new Vector2((float)Math.Cos(a), (float)Math.Sin(a));
		Vector2 value2 = new Vector2((float)Math.Cos(b), (float)Math.Sin(b));
		return AlmostEqual(Vector2.Dot(value, value2), 1f, epsilon);
	}

	public static float CurveAngle(float from, float to, float step)
	{
		if (AlmostEqual(step, 0f))
		{
			return from;
		}
		if (AlmostEqual(from, to) || AlmostEqual(step, 1f))
		{
			return to;
		}
		Vector2 from2 = new Vector2((float)Math.Cos(from), (float)Math.Sin(from));
		Vector2 to2 = new Vector2((float)Math.Cos(to), (float)Math.Sin(to));
		Vector2 vector = Slerp(from2, to2, step);
		return (float)Math.Atan2(vector.Y, vector.X);
	}

	public static bool IsSide(this FaceOrientation orientation)
	{
		if (orientation != FaceOrientation.Down)
		{
			return orientation != FaceOrientation.Top;
		}
		return false;
	}

	public static float ToPhi(this FaceOrientation orientation)
	{
		return orientation switch
		{
			FaceOrientation.Front => 0f, 
			FaceOrientation.Right => (float)Math.PI / 2f, 
			FaceOrientation.Back => -(float)Math.PI, 
			FaceOrientation.Left => -(float)Math.PI / 2f, 
			_ => throw new InvalidOperationException("Side orientations only"), 
		};
	}

	public static float ToPhi(this Viewpoint view)
	{
		return view switch
		{
			Viewpoint.Front => 0f, 
			Viewpoint.Right => (float)Math.PI / 2f, 
			Viewpoint.Back => -(float)Math.PI, 
			Viewpoint.Left => -(float)Math.PI / 2f, 
			_ => throw new InvalidOperationException("Orthographic views only"), 
		};
	}

	public static FaceOrientation OrientationFromPhi(float phi)
	{
		phi = WrapAngle(phi);
		if (AlmostEqual(phi, 0f))
		{
			return FaceOrientation.Front;
		}
		if (AlmostEqual(phi, (float)Math.PI / 2f))
		{
			return FaceOrientation.Right;
		}
		if (AlmostEqual(phi, -(float)Math.PI))
		{
			return FaceOrientation.Back;
		}
		if (AlmostEqual(phi, -(float)Math.PI / 2f))
		{
			return FaceOrientation.Left;
		}
		return OrientationFromPhi(SnapPhi(phi));
	}

	public static Vector2 Slerp(Vector2 from, Vector2 to, float step)
	{
		if (AlmostEqual(step, 0f))
		{
			return from;
		}
		if (AlmostEqual(step, 1f))
		{
			return to;
		}
		double num = Math.Acos(MathHelper.Clamp(Vector2.Dot(from, to), -1f, 1f));
		if (AlmostEqual(num, 0.0))
		{
			return to;
		}
		double num2 = Math.Sin(num);
		return (float)(Math.Sin((double)(1f - step) * num) / num2) * from + (float)(Math.Sin((double)step * num) / num2) * to;
	}

	public static Vector3 Slerp(Vector3 from, Vector3 to, float step)
	{
		if (AlmostEqual(step, 0f))
		{
			return from;
		}
		if (AlmostEqual(step, 1f))
		{
			return to;
		}
		float num = Vector3.Dot(from, to);
		if (num == -1f)
		{
			Vector3 vector = Vector3.Cross(Vector3.Normalize(to - from), Vector3.UnitY);
			if ((double)step < 0.5)
			{
				return Slerp(from, vector, step * 2f);
			}
			return Slerp(vector, to, (step - 0.5f) / 2f);
		}
		double num2 = Math.Acos(MathHelper.Clamp(num, -1f, 1f));
		if (AlmostEqual(num2, 0.0))
		{
			return to;
		}
		double num3 = Math.Sin(num2);
		return (float)(Math.Sin((double)(1f - step) * num2) / num3) * from + (float)(Math.Sin((double)step * num2) / num3) * to;
	}

	public static float AngleBetween(Vector3 a, Vector3 b)
	{
		return (float)Math.Acos(MathHelper.Clamp(Vector3.Dot(a, b), -1f, 1f));
	}

	public static FaceOrientation VisibleOrientation(this Viewpoint view)
	{
		return view switch
		{
			Viewpoint.Down => FaceOrientation.Top, 
			Viewpoint.Back => FaceOrientation.Back, 
			Viewpoint.Front => FaceOrientation.Front, 
			Viewpoint.Left => FaceOrientation.Left, 
			_ => FaceOrientation.Right, 
		};
	}

	public static Viewpoint AsViewpoint(this FaceOrientation orientation)
	{
		return orientation switch
		{
			FaceOrientation.Down => Viewpoint.Down, 
			FaceOrientation.Top => Viewpoint.Up, 
			FaceOrientation.Back => Viewpoint.Back, 
			FaceOrientation.Front => Viewpoint.Front, 
			FaceOrientation.Left => Viewpoint.Left, 
			_ => Viewpoint.Right, 
		};
	}

	public static Axis AsAxis(this FaceOrientation face)
	{
		switch (face)
		{
		case FaceOrientation.Back:
		case FaceOrientation.Front:
			return Axis.Z;
		case FaceOrientation.Left:
		case FaceOrientation.Right:
			return Axis.X;
		default:
			return Axis.Y;
		}
	}

	public static Vector3 GetMask(this Axis axis)
	{
		return axis switch
		{
			Axis.X => Vector3.UnitX, 
			Axis.Y => Vector3.UnitY, 
			_ => Vector3.UnitZ, 
		};
	}

	public static Vector3 GetMask(this Vector3 vector)
	{
		return vector.Sign().Abs();
	}

	public static Axis VisibleAxis(this Viewpoint view)
	{
		switch (view)
		{
		case Viewpoint.Down:
			return Axis.Y;
		case Viewpoint.Front:
		case Viewpoint.Back:
			return Axis.Z;
		default:
			return Axis.X;
		}
	}

	public static float WrapAngle(float theta)
	{
		theta = ((!(theta >= -(float)Math.PI)) ? ((theta - (float)Math.PI) % ((float)Math.PI * 2f) + (float)Math.PI) : ((theta + (float)Math.PI) % ((float)Math.PI * 2f) - (float)Math.PI));
		return theta;
	}

	public static FaceOrientation OrientationFromDirection(Vector3 direction)
	{
		if (direction == Vector3.Forward)
		{
			return FaceOrientation.Back;
		}
		if (direction == Vector3.Backward)
		{
			return FaceOrientation.Front;
		}
		if (direction == Vector3.Up)
		{
			return FaceOrientation.Top;
		}
		if (direction == Vector3.Down)
		{
			return FaceOrientation.Down;
		}
		if (direction == Vector3.Left)
		{
			return FaceOrientation.Left;
		}
		return FaceOrientation.Right;
	}

	public static Vector3 AsVector(this FaceOrientation o)
	{
		return o switch
		{
			FaceOrientation.Back => Vector3.Forward, 
			FaceOrientation.Front => Vector3.Backward, 
			FaceOrientation.Top => Vector3.Up, 
			FaceOrientation.Down => Vector3.Down, 
			FaceOrientation.Left => Vector3.Left, 
			_ => Vector3.Right, 
		};
	}

	public static FaceOrientation GetTangent(this FaceOrientation o)
	{
		return o switch
		{
			FaceOrientation.Back => FaceOrientation.Top, 
			FaceOrientation.Front => FaceOrientation.Top, 
			FaceOrientation.Top => FaceOrientation.Right, 
			FaceOrientation.Down => FaceOrientation.Right, 
			FaceOrientation.Left => FaceOrientation.Front, 
			_ => FaceOrientation.Front, 
		};
	}

	public static FaceOrientation GetBitangent(this FaceOrientation o)
	{
		return o switch
		{
			FaceOrientation.Back => FaceOrientation.Right, 
			FaceOrientation.Front => FaceOrientation.Right, 
			FaceOrientation.Top => FaceOrientation.Front, 
			FaceOrientation.Down => FaceOrientation.Front, 
			FaceOrientation.Left => FaceOrientation.Top, 
			_ => FaceOrientation.Top, 
		};
	}

	public static FaceOrientation GetOpposite(this FaceOrientation o)
	{
		return (FaceOrientation)((int)(o + 3) % 6);
	}

	public static bool IsPositive(this Viewpoint view)
	{
		return view <= Viewpoint.Right;
	}

	public static int AsNumeric(this bool b)
	{
		if (!b)
		{
			return 0;
		}
		return 1;
	}

	public static Vector3 Abs(this Vector3 vector)
	{
		return new Vector3(Math.Abs(vector.X), Math.Abs(vector.Y), Math.Abs(vector.Z));
	}

	public static Vector2 Abs(this Vector2 vector)
	{
		return new Vector2(Math.Abs(vector.X), Math.Abs(vector.Y));
	}

	public static Vector3 Clamp(Vector3 vector, Vector3 minimum, Vector3 maximum)
	{
		Vector3 result = default(Vector3);
		result.X = MathHelper.Clamp(vector.X, minimum.X, maximum.X);
		result.Y = MathHelper.Clamp(vector.Y, minimum.Y, maximum.Y);
		result.Z = MathHelper.Clamp(vector.Z, minimum.Z, maximum.Z);
		return result;
	}

	public static Axis AxisFromPhi(float combinedPhi)
	{
		if ((combinedPhi > (float)Math.PI / 4f && combinedPhi < (float)Math.PI * 3f / 4f) || (combinedPhi < -(float)Math.PI / 4f && combinedPhi > (float)Math.PI * -3f / 4f))
		{
			return Axis.X;
		}
		return Axis.Z;
	}

	public static Viewpoint GetOpposite(this Viewpoint view)
	{
		return view switch
		{
			Viewpoint.Back => Viewpoint.Front, 
			Viewpoint.Left => Viewpoint.Right, 
			Viewpoint.Right => Viewpoint.Left, 
			Viewpoint.Front => Viewpoint.Back, 
			_ => throw new InvalidOperationException("Orthographic views only"), 
		};
	}

	public static HorizontalDirection DirectionFromMovement(float xMovement)
	{
		if (xMovement > 0f)
		{
			return HorizontalDirection.Right;
		}
		if (xMovement < 0f)
		{
			return HorizontalDirection.Left;
		}
		return HorizontalDirection.None;
	}

	public static HorizontalDirection GetOpposite(this HorizontalDirection direction)
	{
		if (direction != HorizontalDirection.Left)
		{
			return HorizontalDirection.Left;
		}
		return HorizontalDirection.Right;
	}

	public static bool IsPositive(this FaceOrientation orientation)
	{
		return orientation > FaceOrientation.Back;
	}

	public static Quaternion QuaternionFromPhi(float phi)
	{
		double num = (double)phi / 2.0;
		return new Quaternion(0f, (float)Math.Sin(num), 0f, (float)Math.Cos(num));
	}

	public static Vector3 Sign(this Vector3 vector)
	{
		return new Vector3(Math.Sign(vector.X), Math.Sign(vector.Y), Math.Sign(vector.Z));
	}

	public static Vector3 XYZ(this Vector4 vector)
	{
		return new Vector3(vector.X, vector.Y, vector.Z);
	}

	public static Vector2 XY(this Vector3 vector)
	{
		return new Vector2(vector.X, vector.Y);
	}

	public static Vector2 YZ(this Vector3 vector)
	{
		return new Vector2(vector.Y, vector.Z);
	}

	public static Vector2 XZ(this Vector3 vector)
	{
		return new Vector2(vector.X, vector.Z);
	}

	public static Vector3 ZYX(this Vector3 vector)
	{
		return new Vector3(vector.Z, vector.Y, vector.X);
	}

	public static Vector2 ZY(this Vector3 vector)
	{
		return new Vector2(vector.Z, vector.Y);
	}

	public static Vector3 X0Y(this Vector2 vector)
	{
		return new Vector3(vector.X, 0f, vector.Y);
	}

	public static Vector3 XYX(this Vector2 vector)
	{
		return new Vector3(vector.X, vector.Y, vector.X);
	}

	public static int Sign(this HorizontalDirection direction)
	{
		if (direction != HorizontalDirection.Right)
		{
			return -1;
		}
		return 1;
	}

	public static float SnapPhi(float phi)
	{
		return (float)((double)Round(0.6366197466850281 * (double)phi) / 0.6366197466850281);
	}

	public static float Frac(float number)
	{
		return number - (float)(int)number;
	}

	public static double Frac(double number)
	{
		return number - (double)(int)number;
	}

	public static Vector3 Frac(this Vector3 vector)
	{
		return new Vector3(vector.X - (float)(int)vector.X, vector.Y - (float)(int)vector.Y, vector.Z - (float)(int)vector.Z);
	}

	public static int GetDistance(this Viewpoint fromView, Viewpoint toView)
	{
		int num = toView - fromView;
		if (Math.Abs(num) == 3)
		{
			num = Math.Sign(num) * -1;
		}
		return num;
	}

	public static Viewpoint GetRotatedView(this Viewpoint fromView, int distance)
	{
		int i;
		for (i = (int)(fromView + distance); i > 4; i -= 4)
		{
		}
		for (; i < 1; i += 4)
		{
		}
		return (Viewpoint)i;
	}

	public static bool IsOrthographic(this Viewpoint view)
	{
		switch (view)
		{
		case Viewpoint.Front:
		case Viewpoint.Right:
		case Viewpoint.Back:
		case Viewpoint.Left:
			return true;
		default:
			return false;
		}
	}

	public static BoundingBox Enclose(Vector3 a, Vector3 b)
	{
		BoundingBox result = default(BoundingBox);
		result.Min = new Vector3(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y), Math.Min(a.Z, b.Z));
		result.Max = new Vector3(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y), Math.Max(a.Z, b.Z));
		return result;
	}

	public static float Dot(this Vector3 a, Vector3 b)
	{
		return Vector3.Dot(a, b);
	}

	public static Matrix CreateLookAt(Vector3 position, Vector3 lookAt, Vector3 upVector)
	{
		Vector3 vector = Vector3.Normalize(lookAt - position);
		Vector3 vector2 = Vector3.Normalize(Vector3.Cross(upVector, vector));
		Vector3 vector3 = Vector3.Cross(vector, vector2);
		return new Matrix(vector2.X, vector3.X, vector.X, 0f, vector2.Y, vector3.Y, vector.Y, 0f, vector2.Z, vector3.Z, vector.Z, 0f, 0f, 0f, 0f, 1f);
	}

	public static float SineTransition(float step)
	{
		return (float)Math.Sin((double)Saturate(step) * Math.PI);
	}

	public static void RotateOnCenter(ref BoundingBox boundingBox, ref Quaternion quaternion)
	{
		Vector3 vector = (boundingBox.Max - boundingBox.Min) / 2f;
		Vector3 vector2 = boundingBox.Min + vector;
		boundingBox.Min = -vector;
		boundingBox.Max = vector;
		boundingBox.GetCorners(tempCorners);
		boundingBox.Min = new Vector3(float.MaxValue);
		boundingBox.Max = new Vector3(float.MinValue);
		for (int i = 0; i < 8; i++)
		{
			Vector3.Transform(ref tempCorners[i], ref quaternion, out var result);
			result += vector2;
			Vector3.Min(ref boundingBox.Min, ref result, out boundingBox.Min);
			Vector3.Max(ref boundingBox.Max, ref result, out boundingBox.Max);
		}
	}

	public static Vector3 GetCenter(this BoundingBox boundingBox)
	{
		return boundingBox.Min + (boundingBox.Max - boundingBox.Min) / 2f;
	}

	public static Quaternion CatmullRom(Quaternion q0, Quaternion q1, Quaternion q2, Quaternion q3, float t)
	{
		Vector4 value = new Vector4(q0.X, q0.Y, q0.Z, q0.W);
		Vector4 value2 = new Vector4(q1.X, q1.Y, q1.Z, q1.W);
		Vector4 value3 = new Vector4(q2.X, q2.Y, q2.Z, q2.W);
		Vector4 value4 = new Vector4(q3.X, q3.Y, q3.Z, q3.W);
		Vector4 vector = Vector4.CatmullRom(value, value2, value3, value4, t);
		return Quaternion.Normalize(new Quaternion(vector.X, vector.Y, vector.Z, vector.W));
	}

	public static Vector2 TransformTexCoord(Vector2 texCoord, Matrix transform)
	{
		return new Vector2(texCoord.X * transform.M11 + texCoord.Y * transform.M21 + transform.M31, texCoord.X * transform.M12 + texCoord.Y * transform.M22 + transform.M32);
	}
}
