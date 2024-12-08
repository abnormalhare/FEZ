using Microsoft.Xna.Framework.Graphics;

namespace FezEngine.Effects.Structures;

public abstract class SemanticMappedParameter<T>
{
	protected readonly EffectParameter parameter;

	private readonly bool missingParameter;

	protected T currentValue;

	protected bool firstSet = true;

	protected SemanticMappedParameter(EffectParameterCollection parent, string semanticName)
	{
		parameter = parent[semanticName] ?? parent[semanticName.Replace("Sampler", "Texture")];
		missingParameter = parameter == null;
	}

	public void Set(T value)
	{
		if (!missingParameter)
		{
			DoSet(value);
		}
	}

	public void Set(T value, int length)
	{
		if (!missingParameter)
		{
			DoSet(value, length);
		}
	}

	protected abstract void DoSet(T value);

	protected virtual void DoSet(T value, int length)
	{
	}

	public T Get()
	{
		return currentValue;
	}
}
