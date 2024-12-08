namespace FezEngine.Structure;

public class TrileCustomData
{
	public bool Unstable;

	public bool Shiny;

	public bool TiltTwoAxis;

	public bool IsCustom;

	public void DetermineCustom()
	{
		IsCustom = Unstable || Shiny || TiltTwoAxis;
	}
}
