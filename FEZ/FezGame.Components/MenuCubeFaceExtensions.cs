using System;
using FezGame.Tools;
using Microsoft.Xna.Framework;

namespace FezGame.Components;

internal static class MenuCubeFaceExtensions
{
	public static Vector3 GetForward(this MenuCubeFace face)
	{
		return face switch
		{
			MenuCubeFace.CubeShards => Vector3.UnitZ, 
			MenuCubeFace.Maps => Vector3.UnitX, 
			MenuCubeFace.Artifacts => -Vector3.UnitZ, 
			MenuCubeFace.AntiCubes => -Vector3.UnitX, 
			_ => throw new InvalidOperationException(), 
		};
	}

	public static Vector3 GetRight(this MenuCubeFace face)
	{
		return face switch
		{
			MenuCubeFace.CubeShards => Vector3.UnitX, 
			MenuCubeFace.Maps => -Vector3.UnitZ, 
			MenuCubeFace.Artifacts => -Vector3.UnitX, 
			MenuCubeFace.AntiCubes => Vector3.UnitZ, 
			_ => throw new InvalidOperationException(), 
		};
	}

	public static int GetCount(this MenuCubeFace face)
	{
		return face switch
		{
			MenuCubeFace.CubeShards => 36, 
			MenuCubeFace.Maps => 9, 
			MenuCubeFace.Artifacts => 4, 
			MenuCubeFace.AntiCubes => 36, 
			_ => throw new InvalidOperationException(), 
		};
	}

	public static int GetOffset(this MenuCubeFace face)
	{
		return face switch
		{
			MenuCubeFace.CubeShards => 34, 
			MenuCubeFace.Maps => 36, 
			MenuCubeFace.Artifacts => 44, 
			MenuCubeFace.AntiCubes => 34, 
			_ => throw new InvalidOperationException(), 
		};
	}

	public static int GetSpacing(this MenuCubeFace face)
	{
		return face switch
		{
			MenuCubeFace.CubeShards => 12, 
			MenuCubeFace.Maps => 28, 
			MenuCubeFace.Artifacts => 40, 
			MenuCubeFace.AntiCubes => 12, 
			_ => throw new InvalidOperationException(), 
		};
	}

	public static int GetSize(this MenuCubeFace face)
	{
		return face switch
		{
			MenuCubeFace.CubeShards => 8, 
			MenuCubeFace.Maps => 22, 
			MenuCubeFace.Artifacts => 30, 
			MenuCubeFace.AntiCubes => 8, 
			_ => throw new InvalidOperationException(), 
		};
	}

	public static int GetDepth(this MenuCubeFace face)
	{
		return face switch
		{
			MenuCubeFace.CubeShards => 4, 
			MenuCubeFace.Maps => 16, 
			MenuCubeFace.Artifacts => 16, 
			MenuCubeFace.AntiCubes => 4, 
			_ => throw new InvalidOperationException(), 
		};
	}

	public static string GetTitle(this MenuCubeFace face)
	{
		return face switch
		{
			MenuCubeFace.Artifacts => StaticText.GetString("MenuCube_Artifacts"), 
			MenuCubeFace.CubeShards => StaticText.GetString("MenuCube_CubeShards"), 
			MenuCubeFace.AntiCubes => StaticText.GetString("MenuCube_AntiCubes"), 
			MenuCubeFace.Maps => StaticText.GetString("MenuCube_Maps"), 
			_ => throw new InvalidOperationException(), 
		};
	}
}
