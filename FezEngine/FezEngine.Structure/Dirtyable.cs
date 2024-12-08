namespace FezEngine.Structure;

public class Dirtyable<T>
{
	public T Value;

	public bool Dirty;

	public void Clean()
	{
		Dirty = false;
	}

	public void Set(T newValue)
	{
		Value = newValue;
		Dirty = true;
	}

	public static implicit operator T(Dirtyable<T> dirtyable)
	{
		return dirtyable.Value;
	}

	public static implicit operator Dirtyable<T>(T dirtyable)
	{
		return new Dirtyable<T>
		{
			Value = dirtyable
		};
	}
}
