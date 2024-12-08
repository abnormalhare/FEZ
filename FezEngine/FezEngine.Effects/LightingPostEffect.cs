using System.Collections.Generic;
using Common;
using FezEngine.Effects.Structures;
using Microsoft.Xna.Framework.Graphics;

namespace FezEngine.Effects;

public class LightingPostEffect : BaseEffect
{
	public enum Passes
	{
		Dawn,
		Dusk_Multiply,
		Dusk_Screen,
		Night
	}

	private class PassesComparer : IEqualityComparer<Passes>
	{
		public static readonly PassesComparer Default = new PassesComparer();

		public bool Equals(Passes x, Passes y)
		{
			return x == y;
		}

		public int GetHashCode(Passes obj)
		{
			return (int)obj;
		}
	}

	private readonly SemanticMappedSingle dawnContribution;

	private readonly SemanticMappedSingle duskContribution;

	private readonly SemanticMappedSingle nightContribution;

	private readonly Dictionary<Passes, EffectPass> passes;

	public float DawnContribution
	{
		get
		{
			return dawnContribution.Get();
		}
		set
		{
			dawnContribution.Set(value);
		}
	}

	public float DuskContribution
	{
		get
		{
			return duskContribution.Get();
		}
		set
		{
			duskContribution.Set(value);
		}
	}

	public float NightContribution
	{
		get
		{
			return nightContribution.Get();
		}
		set
		{
			nightContribution.Set(value);
		}
	}

	public Passes Pass
	{
		set
		{
			currentPass = passes[value];
		}
	}

	public LightingPostEffect()
		: base("LightingPostEffect")
	{
		dawnContribution = new SemanticMappedSingle(effect.Parameters, "DawnContribution");
		duskContribution = new SemanticMappedSingle(effect.Parameters, "DuskContribution");
		nightContribution = new SemanticMappedSingle(effect.Parameters, "NightContribution");
		passes = new Dictionary<Passes, EffectPass>(PassesComparer.Default);
		foreach (Passes value in Util.GetValues<Passes>())
		{
			passes.Add(value, currentTechnique.Passes[value.ToString()]);
		}
	}
}
