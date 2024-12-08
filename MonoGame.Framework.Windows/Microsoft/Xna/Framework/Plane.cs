﻿// Type: Microsoft.Xna.Framework.Plane
// Assembly: MonoGame.Framework.Windows, Version=2.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: D2107839-320D-467B-B82A-28CB452CC584
// Assembly location: F:\Program Files (x86)\FEZ\MonoGame.Framework.Windows.dll

using System;

namespace Microsoft.Xna.Framework
{
  public struct Plane : IEquatable<Plane>
  {
    public float D;
    public Vector3 Normal;

    public Plane(Vector4 value)
    {
      this = new Plane(new Vector3(value.X, value.Y, value.Z), value.W);
    }

    public Plane(Vector3 normal, float d)
    {
      this.Normal = normal;
      this.D = d;
    }

    public Plane(Vector3 a, Vector3 b, Vector3 c)
    {
      Vector3 vector3 = Vector3.Cross(b - a, c - a);
      this.Normal = Vector3.Normalize(vector3);
      this.D = -Vector3.Dot(vector3, a);
    }

    public Plane(float a, float b, float c, float d)
    {
      this = new Plane(new Vector3(a, b, c), d);
    }

    public static bool operator !=(Plane plane1, Plane plane2)
    {
      return !plane1.Equals(plane2);
    }

    public static bool operator ==(Plane plane1, Plane plane2)
    {
      return plane1.Equals(plane2);
    }

    public float Dot(Vector4 value)
    {
      return (float) ((double) this.Normal.X * (double) value.X + (double) this.Normal.Y * (double) value.Y + (double) this.Normal.Z * (double) value.Z + (double) this.D * (double) value.W);
    }

    public void Dot(ref Vector4 value, out float result)
    {
      result = (float) ((double) this.Normal.X * (double) value.X + (double) this.Normal.Y * (double) value.Y + (double) this.Normal.Z * (double) value.Z + (double) this.D * (double) value.W);
    }

    public float DotCoordinate(Vector3 value)
    {
      return (float) ((double) this.Normal.X * (double) value.X + (double) this.Normal.Y * (double) value.Y + (double) this.Normal.Z * (double) value.Z) + this.D;
    }

    public void DotCoordinate(ref Vector3 value, out float result)
    {
      result = (float) ((double) this.Normal.X * (double) value.X + (double) this.Normal.Y * (double) value.Y + (double) this.Normal.Z * (double) value.Z) + this.D;
    }

    public float DotNormal(Vector3 value)
    {
      return (float) ((double) this.Normal.X * (double) value.X + (double) this.Normal.Y * (double) value.Y + (double) this.Normal.Z * (double) value.Z);
    }

    public void DotNormal(ref Vector3 value, out float result)
    {
      result = (float) ((double) this.Normal.X * (double) value.X + (double) this.Normal.Y * (double) value.Y + (double) this.Normal.Z * (double) value.Z);
    }

    public static void Transform(ref Plane plane, ref Quaternion rotation, out Plane result)
    {
      throw new NotImplementedException();
    }

    public static void Transform(ref Plane plane, ref Matrix matrix, out Plane result)
    {
      throw new NotImplementedException();
    }

    public static Plane Transform(Plane plane, Quaternion rotation)
    {
      throw new NotImplementedException();
    }

    public static Plane Transform(Plane plane, Matrix matrix)
    {
      throw new NotImplementedException();
    }

    public void Normalize()
    {
      Vector3 vector3 = this.Normal;
      this.Normal = Vector3.Normalize(this.Normal);
      this.D = this.D * ((float) Math.Sqrt((double) this.Normal.X * (double) this.Normal.X + (double) this.Normal.Y * (double) this.Normal.Y + (double) this.Normal.Z * (double) this.Normal.Z) / (float) Math.Sqrt((double) vector3.X * (double) vector3.X + (double) vector3.Y * (double) vector3.Y + (double) vector3.Z * (double) vector3.Z));
    }

    public static Plane Normalize(Plane value)
    {
      Plane result;
      Plane.Normalize(ref value, out result);
      return result;
    }

    public static void Normalize(ref Plane value, out Plane result)
    {
      result.Normal = Vector3.Normalize(value.Normal);
      float num = (float) Math.Sqrt((double) result.Normal.X * (double) result.Normal.X + (double) result.Normal.Y * (double) result.Normal.Y + (double) result.Normal.Z * (double) result.Normal.Z) / (float) Math.Sqrt((double) value.Normal.X * (double) value.Normal.X + (double) value.Normal.Y * (double) value.Normal.Y + (double) value.Normal.Z * (double) value.Normal.Z);
      result.D = value.D * num;
    }

    public override bool Equals(object other)
    {
      return other is Plane && this.Equals((Plane) other);
    }

    public bool Equals(Plane other)
    {
      return this.Normal == other.Normal && (double) this.D == (double) other.D;
    }

    public override int GetHashCode()
    {
      return this.Normal.GetHashCode() ^ this.D.GetHashCode();
    }

    public PlaneIntersectionType Intersects(BoundingBox box)
    {
      return box.Intersects(this);
    }

    public void Intersects(ref BoundingBox box, out PlaneIntersectionType result)
    {
      result = this.Intersects(box);
    }

    public PlaneIntersectionType Intersects(BoundingFrustum frustum)
    {
      return frustum.Intersects(this);
    }

    public PlaneIntersectionType Intersects(BoundingSphere sphere)
    {
      return sphere.Intersects(this);
    }

    public void Intersects(ref BoundingSphere sphere, out PlaneIntersectionType result)
    {
      result = this.Intersects(sphere);
    }

    public override string ToString()
    {
      return string.Format("{{Normal:{0} D:{1}}}", (object) this.Normal, (object) this.D);
    }
  }
}
