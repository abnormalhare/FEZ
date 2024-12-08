﻿// Type: Microsoft.Xna.Framework.Graphics.VertexPosition2ColorTexture
// Assembly: MonoGame.Framework, Version=3.0.1.0, Culture=neutral, PublicKeyToken=null
// MVID: 69677294-4E99-4B9C-B72B-CC2D8AA03B63
// Assembly location: F:\Program Files (x86)\FEZ\MonoGame.Framework.dll

using Microsoft.Xna.Framework;
using System.Runtime.InteropServices;

namespace Microsoft.Xna.Framework.Graphics
{
  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  internal struct VertexPosition2ColorTexture : IVertexType
  {
    public static readonly VertexDeclaration VertexDeclaration = new VertexDeclaration(new VertexElement[3]
    {
      new VertexElement(0, VertexElementFormat.Vector2, VertexElementUsage.Position, 0),
      new VertexElement(8, VertexElementFormat.Color, VertexElementUsage.Color, 0),
      new VertexElement(12, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0)
    });
    public Vector2 Position;
    public Color Color;
    public Vector2 TextureCoordinate;

    VertexDeclaration IVertexType.VertexDeclaration
    {
      get
      {
        return VertexPosition2ColorTexture.VertexDeclaration;
      }
    }

    static VertexPosition2ColorTexture()
    {
    }

    public VertexPosition2ColorTexture(Vector2 position, Color color, Vector2 texCoord)
    {
      this.Position = position;
      this.Color = color;
      this.TextureCoordinate = texCoord;
    }

    public static bool operator ==(VertexPosition2ColorTexture left, VertexPosition2ColorTexture right)
    {
      if (left.Position == right.Position && left.Color == right.Color)
        return left.TextureCoordinate == right.TextureCoordinate;
      else
        return false;
    }

    public static bool operator !=(VertexPosition2ColorTexture left, VertexPosition2ColorTexture right)
    {
      return !(left == right);
    }

    public static int GetSize()
    {
      return 20;
    }

    public override int GetHashCode()
    {
      return 0;
    }

    public override string ToString()
    {
      return string.Format("{{Position:{0} Color:{1} TextureCoordinate:{2}}}", new object[3]
      {
        (object) this.Position,
        (object) this.Color,
        (object) this.TextureCoordinate
      });
    }

    public override bool Equals(object obj)
    {
      if (obj == null || obj.GetType() != this.GetType())
        return false;
      else
        return this == (VertexPosition2ColorTexture) obj;
    }
  }
}
