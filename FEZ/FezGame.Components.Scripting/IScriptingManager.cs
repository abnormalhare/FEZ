using System;

namespace FezGame.Components.Scripting;

internal interface IScriptingManager
{
	ActiveScript EvaluatedScript { get; }

	event Action CutsceneSkipped;

	void OnCutsceneSkipped();
}
