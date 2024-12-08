using System;
using Common;
using FezEngine.Structure.Scripting;

namespace FezEngine.Services.Scripting;

[Entity(Static = true)]
public interface IOwlService : IScriptingBase
{
	[Description("Number of owls collected up to now")]
	int OwlsCollected { get; }

	event Action OwlCollected;

	event Action OwlLanded;

	void OnOwlCollected();

	void OnOwlLanded();
}
