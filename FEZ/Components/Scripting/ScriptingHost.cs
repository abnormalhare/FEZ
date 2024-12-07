// Type: FezGame.Components.Scripting.ScriptingHost
// Assembly: FEZ, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 9D78BCDD-808F-47ED-B61F-DABBAB0FB594
// Assembly location: F:\Program Files (x86)\FEZ\FEZ.exe

using Common;
using FezEngine.Services;
using FezEngine.Services.Scripting;
using FezEngine.Structure.Scripting;
using FezEngine.Tools;
using FezGame.Services;
using FezGame.Structure;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FezGame.Components.Scripting
{
  internal class ScriptingHost : GameComponent, IScriptingManager
  {
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

    public ScriptingHost(Game game)
      : base(game)
    {
      ScriptingHost.Instance = this;
    }

    public void OnCutsceneSkipped()
    {
      if (this.CutsceneSkipped == null)
        return;
      this.CutsceneSkipped();
    }

    public override void Initialize()
    {
      base.Initialize();
      foreach (KeyValuePair<string, EntityTypeDescriptor> keyValuePair in (IEnumerable<KeyValuePair<string, EntityTypeDescriptor>>) EntityTypes.Types)
        this.services.Add(keyValuePair.Key, ServiceHelper.Get(keyValuePair.Value.Interface) as IScriptingBase);
      this.LevelManager.LevelChanged += new Action(this.PrepareScripts);
      this.PrepareScripts();
    }

    private void PrepareScripts()
    {
      this.levelScripts = Enumerable.ToArray<Script>((IEnumerable<Script>) this.LevelManager.Scripts.Values);
      foreach (ActiveScript activeScript in this.activeScripts)
      {
        activeScript.Dispose();
        if (activeScript.Script.OneTime)
        {
          activeScript.Script.Disabled = true;
          LevelSaveData levelSaveData;
          if (!activeScript.Script.LevelWideOneTime && this.GameState.SaveData.World.TryGetValue(this.LastLevel, out levelSaveData))
            levelSaveData.InactiveEvents.Add(activeScript.Script.Id);
        }
      }
      this.activeScripts.Clear();
      foreach (IScriptingBase scriptingBase in this.services.Values)
        scriptingBase.ResetEvents();
      if (this.LevelManager.Name != null)
      {
        foreach (int key in this.GameState.SaveData.ThisLevel.InactiveEvents)
        {
          Script script;
          if (this.LevelManager.Scripts.TryGetValue(key, out script))
            script.Disabled = true;
        }
      }
      this.LastLevel = this.LevelManager.Name;
      foreach (Script script in this.levelScripts)
        this.HookScriptTriggers(script);
    }

    private void HookScriptTriggers(Script script)
    {
      foreach (ScriptTrigger scriptTrigger in script.Triggers)
      {
        ScriptTrigger triggerCopy = scriptTrigger;
        EntityTypeDescriptor entityTypeDescriptor = EntityTypes.Types[scriptTrigger.Object.Type];
        EventDescriptor eventDescriptor = entityTypeDescriptor.Events[scriptTrigger.Event];
        if (entityTypeDescriptor.Static)
        {
          Action action = (Action) (() => this.ProcessTrigger(triggerCopy, script));
          object obj = eventDescriptor.AddHandler((object) this.services[scriptTrigger.Object.Type], new object[1]
          {
            (object) action
          });
        }
        else
        {
          Action<int> action = (Action<int>) (id => this.ProcessTrigger(triggerCopy, script, new int?(id)));
          object obj = eventDescriptor.AddHandler((object) this.services[scriptTrigger.Object.Type], new object[1]
          {
            (object) action
          });
        }
      }
    }

    private void ProcessTrigger(ScriptTrigger trigger, Script script)
    {
      this.ProcessTrigger(trigger, script, new int?());
    }

    private void ProcessTrigger(ScriptTrigger trigger, Script script, int? id)
    {
      if ((GameState.Loading && trigger.Object.Type != "Level" && trigger.Event != "Start") || script.Disabled)
        return;

      int? num = id;
      int? identifier = trigger.Object.Identifier;

      if (num.GetValueOrDefault() != identifier.GetValueOrDefault() || num.HasValue != identifier.HasValue
          || (script.Conditions != null && script.Conditions.Any((ScriptCondition c) => !c.Check(this.services[c.Object.Type])))
          || (script.OneTime && activeScripts.Any((ActiveScript x) => x.Script == script)))
        return;

      ActiveScript activeScript = new ActiveScript(script, trigger);
      this.activeScripts.Add(activeScript);

      if (script.IsWinCondition && !GameState.SaveData.ThisLevel.FilledConditions.ScriptIds.Contains(script.Id))
      {
        GameState.SaveData.ThisLevel.FilledConditions.ScriptIds.Add(script.Id);
        GameState.SaveData.ThisLevel.FilledConditions.ScriptIds.Sort();
      }

      foreach (ScriptAction action3 in script.Actions)
      {
        RunnableAction runnableAction = new RunnableAction { Action = action3 };
        runnableAction.Invocation = () => runnableAction.Action.Invoke.Invoke(this.services[runnableAction.Action.Object.Type], runnableAction.Action.ProcessedArguments);
        activeScript.EnqueueAction(runnableAction);
      }

      if (!script.IgnoreEndTriggers)
      {
        foreach (ScriptTrigger trigger2 in script.Triggers)
        {
          EntityTypeDescriptor val = EntityTypes.Types[((ScriptPart)trigger2).Object.Type];
          DynamicMethodDelegate addEndTriggerHandler = val.Events[trigger2.Event].AddEndTriggerHandler;
          if (addEndTriggerHandler == null)
          {
            continue;
          }
          if (val.Static)
          {
            Action action = delegate
            {
              ProcessEndTrigger(trigger, activeScript);
            };
            addEndTriggerHandler.Invoke((object)services[((ScriptPart)trigger).Object.Type], new object[1] { action });
          }
          else
          {
            Action<int> action2 = delegate(int i)
            {
              ProcessEndTrigger(trigger, activeScript, i);
            };
            addEndTriggerHandler.Invoke((object)services[((ScriptPart)trigger).Object.Type], new object[1] { action2 });
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
      ScriptingHost.ProcessEndTrigger(trigger, script, new int?());
    }

    private static void ProcessEndTrigger(ScriptTrigger trigger, ActiveScript script, int? id)
    {
      int? nullable = id;
      int? identifier = trigger.Object.Identifier;
      if ((nullable.GetValueOrDefault() != identifier.GetValueOrDefault() ? 0 : (nullable.HasValue == identifier.HasValue ? 1 : 0)) == 0)
        return;
      script.Dispose();
    }

    public override void Update(GameTime gameTime)
    {
      if (this.GameState.Paused || this.GameState.Loading || (this.GameState.InCutscene || this.GameState.InFpsMode))
        return;
      this.ForceUpdate(gameTime);
    }

    public void ForceUpdate(GameTime gameTime)
    {
      foreach (Script script in this.levelScripts)
      {
        if (script.ScheduleEvalulation)
        {
          this.ProcessTrigger((ScriptTrigger) ScriptingHost.NullTrigger.Instance, script);
          script.ScheduleEvalulation = false;
        }
      }
      for (int index = this.activeScripts.Count - 1; index != -1; --index)
      {
        ActiveScript activeScript = this.activeScripts[index];
        this.EvaluatedScript = activeScript;
        activeScript.Update(gameTime.ElapsedGameTime);
        if (activeScript.IsDisposed && this.activeScripts.Count > 0 && this.activeScripts[index] == activeScript)
        {
          this.activeScripts.RemoveAt(index);
          if (activeScript.Script.OneTime)
          {
            activeScript.Script.Disabled = true;
            if (!activeScript.Script.LevelWideOneTime)
              this.GameState.SaveData.ThisLevel.InactiveEvents.Add(activeScript.Script.Id);
            this.GameState.Save();
          }
        }
      }
      this.EvaluatedScript = (ActiveScript) null;
    }

    private class NullTrigger : ScriptTrigger
    {
      public static readonly ScriptingHost.NullTrigger Instance = new ScriptingHost.NullTrigger();
      private const string NullEvent = "Null Event";

      static NullTrigger()
      {
      }

      private NullTrigger()
      {
        this.Event = "Null Event";
        this.Object = new Entity()
        {
          Type = (string) null,
          Identifier = new int?()
        };
      }
    }
  }
}
