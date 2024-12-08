using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Common;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Tools;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;

namespace FezEngine.Components;

public abstract class Sequencer : GameComponent
{
	protected class CrystalState
	{
		public SoundEffect Sample { get; set; }

		public SoundEffect AlternateSample { get; set; }

		public bool[] Alternate { get; private set; }

		public CrystalState()
		{
			Alternate = new bool[16];
		}
	}

	private const float WarningTime = 0.45f;

	private const int WarningBlinks = 6;

	protected readonly Dictionary<TrileInstance, CrystalState> crystals = new Dictionary<TrileInstance, CrystalState>();

	private TimeSpan measureLength;

	private int step;

	[ServiceDependency]
	public IEngineStateManager EngineState { protected get; set; }

	[ServiceDependency]
	public IDefaultCameraManager CameraManager { protected get; set; }

	[ServiceDependency]
	public ILevelManager LevelManager { protected get; set; }

	[ServiceDependency]
	public ILevelMaterializer LevelMaterializer { protected get; set; }

	[ServiceDependency]
	public IDebuggingBag DebuggingBag { protected get; set; }

	[ServiceDependency]
	public ISoundManager SoundManager { protected get; set; }

	[ServiceDependency]
	public IContentManagerProvider CMProvider { private get; set; }

	protected Sequencer(Game game)
		: base(game)
	{
	}

	public override void Initialize()
	{
		base.Enabled = false;
		LevelManager.LevelChanged += TryStartSequence;
		TryStartSequence();
	}

	private void TryStartSequence()
	{
		crystals.Clear();
		base.Enabled = LevelManager.SequenceSamplesPath != null && LevelManager.Song != null && LevelManager.BlinkingAlpha;
		if (!base.Enabled)
		{
			return;
		}
		foreach (TrileInstance item in LevelManager.Triles.Values.Where((TrileInstance x) => x.Trile.ActorSettings.Type == ActorType.Crystal))
		{
			crystals.Add(item, new CrystalState());
		}
		StartSequence();
	}

	protected void StartSequence()
	{
		LoadCrystalSamples();
		UpdateTempo();
	}

	protected void UpdateTempo()
	{
		measureLength = TimeSpan.FromMinutes(1.0 / ((double)LevelManager.Song.Tempo / 4.0) * 4.0);
	}

	protected void LoadCrystalSamples()
	{
		ContentManager forLevel = CMProvider.GetForLevel(LevelManager.Name);
		string path = Path.Combine("Sounds", LevelManager.SequenceSamplesPath ?? "");
		foreach (TrileInstance item in crystals.Keys.Where((TrileInstance x) => x.ActorSettings.SequenceSampleName != null))
		{
			CrystalState crystalState = crystals[item];
			InstanceActorSettings actorSettings = item.ActorSettings;
			try
			{
				crystalState.Sample = forLevel.Load<SoundEffect>(Path.Combine(path, actorSettings.SequenceSampleName));
				if (actorSettings.SequenceAlternateSampleName != null)
				{
					crystalState.AlternateSample = forLevel.Load<SoundEffect>(Path.Combine(path, actorSettings.SequenceAlternateSampleName));
				}
			}
			catch (Exception)
			{
				Logger.Log("Sequencer", LogSeverity.Warning, string.Concat("Could not find crystal sample : ", crystalState.Sample, " or ", crystalState.AlternateSample));
			}
			bool flag = actorSettings.Sequence[15];
			bool flag2 = false;
			for (int i = 0; i < 16; i++)
			{
				if (!flag && actorSettings.Sequence[i])
				{
					crystalState.Alternate[i] = flag2 && crystalState.AlternateSample != null;
					flag2 = !flag2;
				}
				flag = actorSettings.Sequence[i];
			}
		}
	}

	public override void Update(GameTime gameTime)
	{
		if (!base.Enabled || EngineState.Loading || EngineState.Paused || EngineState.InMap)
		{
			return;
		}
		double num = FezMath.Frac((double)SoundManager.PlayPosition.Ticks / (double)measureLength.Ticks);
		step = (int)Math.Floor(num * 16.0);
		double num2 = FezMath.Frac(num * 16.0);
		int num3 = (step + 1) % 16;
		bool flag = false;
		foreach (TrileInstance key in crystals.Keys)
		{
			if (key.ActorSettings.Sequence == null)
			{
				continue;
			}
			CrystalState crystalState = crystals[key];
			bool enabled = key.Enabled;
			key.Enabled = key.ActorSettings.Sequence.Length > step && key.ActorSettings.Sequence[step];
			if (!enabled && key.Enabled)
			{
				LevelManager.RestoreTrile(key);
				key.Hidden = false;
				key.Enabled = true;
				if (key.InstanceId == -1)
				{
					LevelMaterializer.CullInstanceIn(key);
				}
				if (crystalState.Sample != null)
				{
					Vector3 position = key.Position + new Vector3(0.5f);
					(crystalState.Alternate[step] ? crystalState.AlternateSample : crystalState.Sample).EmitAt(position);
				}
				flag = true;
			}
			else if (enabled && !key.Enabled)
			{
				LevelMaterializer.UnregisterViewedInstance(key);
				LevelMaterializer.CullInstanceOut(key, skipUnregister: true);
				LevelManager.ClearTrile(key, skipRecull: true);
				OnDisappear(key);
				flag = true;
			}
			else
			{
				if (!(num2 > 0.44999998807907104) || !key.Enabled || key.ActorSettings.Sequence.Length <= num3 || key.ActorSettings.Sequence[num3])
				{
					continue;
				}
				if ((int)Math.Round((num2 - 0.44999998807907104) / 0.550000011920929 * 6.0) % 3 == 0)
				{
					if (!key.Hidden)
					{
						key.Hidden = true;
						LevelMaterializer.UnregisterViewedInstance(key);
						LevelMaterializer.CullInstanceOut(key, skipUnregister: true);
						flag = true;
					}
				}
				else if (key.Hidden)
				{
					key.Hidden = false;
					if (key.InstanceId == -1)
					{
						LevelMaterializer.CullInstanceIn(key);
						flag = true;
					}
				}
			}
		}
		if (flag)
		{
			LevelMaterializer.CommitBatchesIfNeeded();
		}
	}

	protected virtual void OnDisappear(TrileInstance crystal)
	{
	}
}
