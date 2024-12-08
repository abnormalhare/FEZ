using System.Collections.Generic;
using FezEngine.Structure;

namespace FezGame.Structure;

public class LevelSaveData
{
	public static readonly LevelSaveData Default = new LevelSaveData();

	public List<TrileEmplacement> DestroyedTriles = new List<TrileEmplacement>();

	public List<TrileEmplacement> InactiveTriles = new List<TrileEmplacement>();

	public List<int> InactiveArtObjects = new List<int>();

	public List<int> InactiveEvents = new List<int>();

	public List<int> InactiveGroups = new List<int>();

	public List<int> InactiveVolumes = new List<int>();

	public List<int> InactiveNPCs = new List<int>();

	public Dictionary<int, int> PivotRotations = new Dictionary<int, int>();

	public float? LastStableLiquidHeight;

	public string ScriptingState;

	public WinConditions FilledConditions = new WinConditions();

	public bool FirstVisit;

	public void CloneInto(LevelSaveData d)
	{
		FilledConditions.CloneInto(d.FilledConditions);
		d.FirstVisit = FirstVisit;
		d.LastStableLiquidHeight = LastStableLiquidHeight;
		d.ScriptingState = ScriptingState;
		d.DestroyedTriles.Clear();
		d.DestroyedTriles.AddRange(DestroyedTriles);
		d.InactiveArtObjects.Clear();
		d.InactiveArtObjects.AddRange(InactiveArtObjects);
		d.InactiveEvents.Clear();
		d.InactiveEvents.AddRange(InactiveEvents);
		d.InactiveGroups.Clear();
		d.InactiveGroups.AddRange(InactiveGroups);
		d.InactiveNPCs.Clear();
		d.InactiveNPCs.AddRange(InactiveNPCs);
		d.InactiveTriles.Clear();
		d.InactiveTriles.AddRange(InactiveTriles);
		d.InactiveVolumes.Clear();
		d.InactiveVolumes.AddRange(InactiveVolumes);
		d.PivotRotations.Clear();
		foreach (int key in PivotRotations.Keys)
		{
			d.PivotRotations.Add(key, PivotRotations[key]);
		}
	}
}
