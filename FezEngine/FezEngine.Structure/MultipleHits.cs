namespace FezEngine.Structure;

public struct MultipleHits<T>
{
	public T NearLow;

	public T FarHigh;

	public T First
	{
		get
		{
			if (!object.Equals(NearLow, default(T)))
			{
				return NearLow;
			}
			return FarHigh;
		}
	}

	public override string ToString()
	{
		return $"{{Near/Low: {NearLow} Far/High: {FarHigh}}}";
	}
}
