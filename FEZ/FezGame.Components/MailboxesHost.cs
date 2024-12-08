using System.Collections.Generic;
using FezEngine.Components;
using FezEngine.Services.Scripting;
using FezEngine.Structure;
using FezEngine.Structure.Input;
using FezEngine.Tools;
using FezGame.Services;
using Microsoft.Xna.Framework;

namespace FezGame.Components;

internal class MailboxesHost : GameComponent
{
	private class MailboxState
	{
		public readonly ArtObjectInstance MailboxAo;

		public bool Empty { get; private set; }

		[ServiceDependency]
		public IGomezService GomezService { private get; set; }

		[ServiceDependency]
		public IGameStateManager GameState { private get; set; }

		[ServiceDependency]
		public IInputManager InputManager { private get; set; }

		[ServiceDependency]
		public IPlayerManager PlayerManager { private get; set; }

		[ServiceDependency]
		public IGameCameraManager CameraManager { private get; set; }

		public MailboxState(ArtObjectInstance aoInstance)
		{
			ServiceHelper.InjectServices(this);
			MailboxAo = aoInstance;
		}

		public void Update()
		{
			Vector3 position = MailboxAo.Position;
			Vector3 center = PlayerManager.Center;
			Vector3 b = CameraManager.Viewpoint.SideMask();
			Vector3 b2 = CameraManager.Viewpoint.ForwardVector();
			Vector3 vector = new Vector3(position.Dot(b), position.Y, 0f);
			BoundingBox boundingBox = new BoundingBox(vector - new Vector3(0.5f, 0.75f, 0.5f), vector + new Vector3(0.5f, 0.75f, 0.5f));
			Vector3 point = new Vector3(center.Dot(b), center.Y, 0f);
			if (center.Dot(b2) < MailboxAo.Position.Dot(b2) && boundingBox.Contains(point) != 0 && InputManager.GrabThrow == FezButtonState.Pressed)
			{
				GameState.SaveData.ThisLevel.InactiveArtObjects.Add(MailboxAo.Id);
				ServiceHelper.AddComponent(new LetterViewer(ServiceHelper.Game, MailboxAo.ActorSettings.TreasureMapName));
				GomezService.OnReadMail();
				Empty = true;
			}
		}
	}

	private readonly List<MailboxState> Mailboxes = new List<MailboxState>();

	[ServiceDependency]
	public IGameLevelManager LevelManager { private get; set; }

	[ServiceDependency]
	public IGameStateManager GameState { private get; set; }

	public MailboxesHost(Game game)
		: base(game)
	{
	}

	public override void Initialize()
	{
		base.Initialize();
		LevelManager.LevelChanged += TryInitialize;
		TryInitialize();
	}

	private void TryInitialize()
	{
		Mailboxes.Clear();
		foreach (ArtObjectInstance value in LevelManager.ArtObjects.Values)
		{
			if (value.ArtObject.ActorType == ActorType.Mailbox && !GameState.SaveData.ThisLevel.InactiveArtObjects.Contains(value.Id) && value.ActorSettings.TreasureMapName != null)
			{
				Mailboxes.Add(new MailboxState(value));
			}
		}
	}

	public override void Update(GameTime gameTime)
	{
		if (GameState.Loading || GameState.Paused || GameState.InCutscene || GameState.InMap || GameState.InMenuCube || GameState.InFpsMode)
		{
			return;
		}
		for (int num = Mailboxes.Count - 1; num >= 0; num--)
		{
			Mailboxes[num].Update();
			if (Mailboxes[num].Empty)
			{
				Mailboxes.RemoveAt(num);
			}
		}
	}
}
