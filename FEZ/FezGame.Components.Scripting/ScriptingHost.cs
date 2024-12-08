using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using FezEngine.Services;
using FezEngine.Services.Scripting;
using FezEngine.Structure.Scripting;
using FezEngine.Tools;
using FezGame.Services;
using Microsoft.Xna.Framework;

namespace FezGame.Components.Scripting;

internal class ScriptingHost : GameComponent, IScriptingManager
{
	private class NullTrigger : ScriptTrigger
	{
		public static readonly NullTrigger Instance = new NullTrigger();

		private const string NullEvent = "Null Event";

		private NullTrigger()
		{
			base.Event = "Null Event";
			base.Object = new Entity
			{
				Type = null,
				Identifier = null
			};
		}
	}

	private Script[] levelScripts = new Script[0];

	private readonly Dictionary<string, IScriptingBase> services = new Dictionary<string, IScriptingBase>();

	private readonly List<ActiveScript> activeScripts = new List<ActiveScript>();

	public static bool ScriptExecuted;

	private string LastLevel;

	public static ScriptingHost Instance;

	public ActiveScript EvaluatedScript { get; private set; }

	[ServiceDependency]
	public IDebuggingBag DebuggingBag { private get; set; }

	[ServiceDependency]
	public IGameStateManager GameState { private get; set; }

	[ServiceDependency]
	public ILevelManager LevelManager { private get; set; }

	[ServiceDependency]
	public IScriptService ScriptService { private get; set; }

	public event Action CutsceneSkipped;

	public void OnCutsceneSkipped()
	{
		if (this.CutsceneSkipped != null)
		{
			this.CutsceneSkipped();
		}
	}

	public ScriptingHost(Game game)
		: base(game)
	{
		Instance = this;
	}

	public override void Initialize()
	{
		base.Initialize();
		foreach (KeyValuePair<string, EntityTypeDescriptor> type in EntityTypes.Types)
		{
			services.Add(type.Key, ServiceHelper.Get(type.Value.Interface) as IScriptingBase);
		}
		LevelManager.LevelChanged += PrepareScripts;
		PrepareScripts();
	}

	private void PrepareScripts()
	{
		levelScripts = LevelManager.Scripts.Values.ToArray();
		foreach (ActiveScript activeScript in activeScripts)
		{
			activeScript.Dispose();
			if (activeScript.Script.OneTime)
			{
				activeScript.Script.Disabled = true;
				if (!activeScript.Script.LevelWideOneTime && GameState.SaveData.World.TryGetValue(LastLevel, out var value))
				{
					value.InactiveEvents.Add(activeScript.Script.Id);
				}
			}
		}
		activeScripts.Clear();
		foreach (IScriptingBase value3 in services.Values)
		{
			value3.ResetEvents();
		}
		if (LevelManager.Name != null)
		{
			foreach (int inactiveEvent in GameState.SaveData.ThisLevel.InactiveEvents)
			{
				if (LevelManager.Scripts.TryGetValue(inactiveEvent, out var value2) && (!(LevelManager.Name == "WATERFALL") || inactiveEvent != 9 || GameState.SaveData.ThisLevel.InactiveVolumes.Contains(20)))
				{
					value2.Disabled = true;
				}
			}
		}
		LastLevel = LevelManager.Name;
		Script[] array = levelScripts;
		foreach (Script script in array)
		{
			HookScriptTriggers(script);
		}
	}

	private void HookScriptTriggers(Script script)
	{
		foreach (ScriptTrigger trigger in script.Triggers)
		{
			ScriptTrigger triggerCopy = trigger;
			EntityTypeDescriptor entityTypeDescriptor = EntityTypes.Types[trigger.Object.Type];
			EventDescriptor eventDescriptor = entityTypeDescriptor.Events[trigger.Event];
			if (entityTypeDescriptor.Static)
			{
				Action action = delegate
				{
					ProcessTrigger(triggerCopy, script);
				};
				eventDescriptor.AddHandler(services[trigger.Object.Type], action);
			}
			else
			{
				Action<int> action2 = delegate(int id)
				{
					ProcessTrigger(triggerCopy, script, id);
				};
				eventDescriptor.AddHandler(services[trigger.Object.Type], action2);
			}
		}
	}

	private void ProcessTrigger(ScriptTrigger trigger, Script script)
	{
		ProcessTrigger(trigger, script, null);
	}

	private void ProcessTrigger(ScriptTrigger trigger, Script script, int? id)
	{
		if ((GameState.Loading && trigger.Object.Type != "Level" && trigger.Event != "Start") || script.Disabled)
		{
			return;
		}
		int? num = id;
		int? identifier = trigger.Object.Identifier;
		if (num.GetValueOrDefault() != identifier.GetValueOrDefault() || num.HasValue != identifier.HasValue || (script.Conditions != null && script.Conditions.Any((ScriptCondition c) => !c.Check(services[c.Object.Type]))) || (script.OneTime && activeScripts.Any((ActiveScript x) => x.Script == script)))
		{
			return;
		}
		ActiveScript activeScript = new ActiveScript(script, trigger);
		activeScripts.Add(activeScript);
		if (script.IsWinCondition && !GameState.SaveData.ThisLevel.FilledConditions.ScriptIds.Contains(script.Id))
		{
			GameState.SaveData.ThisLevel.FilledConditions.ScriptIds.Add(script.Id);
			GameState.SaveData.ThisLevel.FilledConditions.ScriptIds.Sort();
		}
		foreach (ScriptAction action3 in script.Actions)
		{
			RunnableAction runnableAction = new RunnableAction
			{
				Action = action3
			};
			runnableAction.Invocation = () => runnableAction.Action.Invoke(services[runnableAction.Action.Object.Type], runnableAction.Action.ProcessedArguments);
			activeScript.EnqueueAction(runnableAction);
		}
		if (!script.IgnoreEndTriggers)
		{
			foreach (ScriptTrigger trigger2 in script.Triggers)
			{
				EntityTypeDescriptor entityTypeDescriptor = EntityTypes.Types[trigger2.Object.Type];
				DynamicMethodDelegate addEndTriggerHandler = entityTypeDescriptor.Events[trigger2.Event].AddEndTriggerHandler;
				if (addEndTriggerHandler == null)
				{
					continue;
				}
				if (entityTypeDescriptor.Static)
				{
					Action action = delegate
					{
						ProcessEndTrigger(trigger, activeScript);
					};
					addEndTriggerHandler(services[trigger.Object.Type], action);
				}
				else
				{
					Action<int> action2 = delegate(int i)
					{
						ProcessEndTrigger(trigger, activeScript, i);
					};
					addEndTriggerHandler(services[trigger.Object.Type], action2);
				}
			}
		}
		activeScript.Disposed += delegate
		{
			ScriptService.OnComplete(activeScript.Script.Id);
		};
		ScriptExecuted = true;
	}

	private static void ProcessEndTrigger(ScriptTrigger trigger, ActiveScript script)
	{
		ProcessEndTrigger(trigger, script, null);
	}

	private static void ProcessEndTrigger(ScriptTrigger trigger, ActiveScript script, int? id)
	{
		if (id == trigger.Object.Identifier)
		{
			script.Dispose();
		}
	}

	public override void Update(GameTime gameTime)
	{
		if (!GameState.Paused && !GameState.Loading && !GameState.InCutscene && !GameState.InFpsMode)
		{
			ForceUpdate(gameTime);
		}
	}

	public void ForceUpdate(GameTime gameTime)
	{
		Script[] array = levelScripts;
		foreach (Script script in array)
		{
			if (script.ScheduleEvalulation)
			{
				ProcessTrigger(NullTrigger.Instance, script);
				script.ScheduleEvalulation = false;
			}
		}
		for (int num = activeScripts.Count - 1; num != -1; num--)
		{
			ActiveScript activeScript2 = (EvaluatedScript = activeScripts[num]);
			activeScript2.Update(gameTime.ElapsedGameTime);
			if (activeScript2.IsDisposed && activeScripts.Count > 0 && activeScripts[num] == activeScript2)
			{
				activeScripts.RemoveAt(num);
				if (activeScript2.Script.OneTime)
				{
					activeScript2.Script.Disabled = true;
					if (!activeScript2.Script.LevelWideOneTime)
					{
						GameState.SaveData.ThisLevel.InactiveEvents.Add(activeScript2.Script.Id);
					}
					GameState.Save();
				}
			}
		}
		EvaluatedScript = null;
	}
}
