namespace FezEngine.Structure.Scripting;

public class ScriptTrigger : ScriptPart
{
	public string Event { get; set; }

	public override string ToString()
	{
		return ((base.Object == null) ? "(none)" : base.Object.ToString()) + "." + (Event ?? "(none)");
	}

	public ScriptTrigger Clone()
	{
		return new ScriptTrigger
		{
			Event = Event,
			Object = base.Object.Clone()
		};
	}
}
