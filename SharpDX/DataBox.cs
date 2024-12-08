﻿// Type: SharpDX.DataBox
// Assembly: SharpDX, Version=2.4.2.0, Culture=neutral, PublicKeyToken=627a3d6d1956f55a
// MVID: 578390A1-1524-4146-8C27-2E9750400D7A
// Assembly location: F:\Program Files (x86)\FEZ\SharpDX.dll

using System;

namespace SharpDX
{
  public struct DataBox
  {
    public IntPtr DataPointer;
    public int RowPitch;
    public int SlicePitch;

    public DataBox(IntPtr datapointer, int rowPitch, int slicePitch)
    {
      this.DataPointer = datapointer;
      this.RowPitch = rowPitch;
      this.SlicePitch = slicePitch;
    }

    public DataBox(IntPtr dataPointer)
    {
      this.DataPointer = dataPointer;
      this.RowPitch = 0;
      this.SlicePitch = 0;
    }
  }
}
