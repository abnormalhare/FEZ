using System;
using System.Globalization;
using Common;
using FezEngine.Components;
using FezEngine.Services;
using FezEngine.Structure.Input;
using FezEngine.Tools;
using FezGame.Components;
using FezGame.Services;
using FezGame.Tools;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Steamworks;

namespace FezGame.Structure;

internal class LeaderboardsMenuLevel : MenuLevel
{
	private const int EntriesPerPage = 10;

	private LeaderboardView view;

	private CachedLeaderboard leaderboard;

	private bool moveToLast;

	private bool moveToTop;

	private Rectangle? leftArrowRect;

	private Rectangle? rightArrowRect;

	private readonly MenuBase menuBase;

	private bool viewChanged;

	public IInputManager InputManager { private get; set; }

	public IMouseStateManager MouseState { private get; set; }

	public IGameStateManager GameState { private get; set; }

	public SpriteFont Font { private get; set; }

	public LeaderboardsMenuLevel(MenuBase menuBase)
	{
		this.menuBase = menuBase;
	}

	public override void Initialize()
	{
		base.Initialize();
		IsDynamic = true;
		OnClose = delegate
		{
			if (leaderboard != null)
			{
				GameState.LiveConnectionChanged -= ChangeView;
				leaderboard = null;
			}
		};
		XButtonAction = delegate
		{
			view++;
			if (view > LeaderboardView.Overall)
			{
				view = LeaderboardView.Friends;
			}
			ChangeView();
		};
	}

	public override void Reset()
	{
		base.Reset();
		base.XButtonString = "{X} " + string.Format(StaticText.GetString("CurrentLeaderboardView"), StaticText.GetString(string.Concat(view, "LeaderboardView")));
	}

	public override void Update(TimeSpan elapsed)
	{
		base.Update(elapsed);
		if (leaderboard == null)
		{
			InitLeaderboards();
		}
		if (!leaderboard.InError)
		{
			if (InputManager.RotateRight == FezButtonState.Pressed)
			{
				TryPageUp();
			}
			if (InputManager.RotateLeft == FezButtonState.Pressed)
			{
				TryPageDown();
			}
		}
		else if (GameState.HasActivePlayer && GameState.ActiveGamer != null && !leaderboard.Reading && !leaderboard.ChangingPage)
		{
			leaderboard.ActiveGamer = GameState.ActiveGamer;
			ChangeView();
		}
		Point position = MouseState.Position;
		if (rightArrowRect.HasValue && rightArrowRect.Value.Contains(position))
		{
			menuBase.CursorSelectable = true;
			if (MouseState.LeftButton.State == MouseButtonStates.Pressed)
			{
				TryPageUp();
			}
		}
		if (leftArrowRect.HasValue && leftArrowRect.Value.Contains(position))
		{
			menuBase.CursorSelectable = true;
			if (MouseState.LeftButton.State == MouseButtonStates.Pressed)
			{
				TryPageDown();
			}
		}
		if (leaderboard.InError || leaderboard.Reading)
		{
			base.XButtonString = null;
		}
	}

	private void TryPageUp()
	{
		if (!leaderboard.Reading && leaderboard.CanPageDown)
		{
			Items.Clear();
			AddItem("LoadingLeaderboard", Util.NullAction);
			leaderboard.PageDown(Refresh);
		}
	}

	private void TryPageDown()
	{
		if (!leaderboard.Reading && leaderboard.CanPageUp)
		{
			Items.Clear();
			AddItem("LoadingLeaderboard", Util.NullAction);
			leaderboard.PageUp(Refresh);
		}
	}

	private void CheckLive()
	{
		if (leaderboard != null)
		{
			leaderboard.ActiveGamer = GameState.ActiveGamer;
		}
		ChangeView();
	}

	private void InitLeaderboards()
	{
		leaderboard = new CachedLeaderboard(GameState.ActiveGamer, 10);
		OnScrollDown = delegate
		{
			if (!leaderboard.InError && leaderboard.CanPageDown && !leaderboard.Reading)
			{
				moveToTop = true;
				TryPageUp();
			}
		};
		OnScrollUp = delegate
		{
			if (!leaderboard.InError && leaderboard.CanPageUp && !leaderboard.Reading)
			{
				moveToLast = true;
				TryPageDown();
			}
		};
		ChangeView();
		GameState.LiveConnectionChanged += CheckLive;
	}

	public override void Dispose()
	{
		if (leaderboard != null)
		{
			GameState.LiveConnectionChanged -= CheckLive;
		}
		OnScrollDown = null;
		OnScrollUp = null;
		OnClose = null;
	}

	private void ChangeView()
	{
		lock (this)
		{
			Items.Clear();
			AddItem("LoadingLeaderboard", Util.NullAction);
		}
		viewChanged = true;
		leaderboard.ChangeView(view, Refresh);
	}

	public override void PostDraw(SpriteBatch batch, SpriteFont font, GlyphTextRenderer tr, float alpha)
	{
		if (leaderboard == null)
		{
			InitLeaderboards();
		}
		float viewScale = batch.GraphicsDevice.GetViewScale();
		if (!leaderboard.InError)
		{
			tr.DrawString(batch, font, string.Format(StaticText.GetString("LeaderboardEntriesCount").ToUpper(CultureInfo.InvariantCulture), leaderboard.TotalEntries), new Vector2(125f, batch.GraphicsDevice.Viewport.Height / 2 + 260) * viewScale, new Color(1f, 1f, 1f, alpha), (Culture.IsCJK ? 0.2f : 1.5f) * viewScale);
		}
		float num = ((leaderboard.InError || leaderboard.Reading) ? 0f : (leaderboard.CanPageUp ? 1f : 0.1f));
		float num2 = ((leaderboard.InError || leaderboard.Reading) ? 0f : (leaderboard.CanPageDown ? 1f : 0.1f));
		float num3 = (Culture.IsCJK ? (-15f) : 0f);
		if (Items.Count > 1)
		{
			int num4 = ServiceHelper.Game.GraphicsDevice.Viewport.Width / 2 - (int)viewScale * 20;
			int y = ServiceHelper.Game.GraphicsDevice.Viewport.Height / 2;
			leftArrowRect = new Rectangle((int)((float)num4 - (float)num4 * 5f / 7f + num3 - viewScale * 10f), y, (int)(40f * viewScale), (int)(25f * viewScale));
			rightArrowRect = new Rectangle((int)((float)num4 + (float)num4 * 5f / 7f + num3), y, (int)(40f * viewScale), (int)(25f * viewScale));
			tr.DrawString(batch, font, "{LA}", new Vector2((float)leftArrowRect.Value.Left + 15f * viewScale, leftArrowRect.Value.Top), new Color(1f, 1f, 1f, num * alpha), (Culture.IsCJK ? 0.2f : 1f) * viewScale);
			tr.DrawString(batch, font, "{RA}", new Vector2((float)rightArrowRect.Value.Left + 15f * viewScale, rightArrowRect.Value.Top), new Color(1f, 1f, 1f, num2 * alpha), (Culture.IsCJK ? 0.2f : 1f) * viewScale);
		}
		else
		{
			leftArrowRect = (rightArrowRect = null);
		}
		if (!leaderboard.CanPageUp)
		{
			leftArrowRect = null;
		}
		if (!leaderboard.CanPageDown)
		{
			rightArrowRect = null;
		}
	}

	private void Refresh()
	{
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0104: Unknown result type (might be due to invalid IL or missing references)
		lock (this)
		{
			int selectedIndex = base.SelectedIndex;
			Items.Clear();
			bool flag = false;
			if (leaderboard.InError)
			{
				AddItem("LeaderboardsNeedLIVE", Util.NullAction);
			}
			else
			{
				base.SelectedIndex = 0;
				bool flag2 = false;
				int num = 0;
				foreach (LeaderboardEntry_t entry in leaderboard.Entries)
				{
					LeaderboardEntry_t e = entry;
					MenuItem menuItem = AddItem(null, delegate
					{
						//IL_000b: Unknown result type (might be due to invalid IL or missing references)
						SteamFriends.ActivateGameOverlayToUser("stats", e.m_steamIDUser);
					}, !flag2);
					string personaName = SteamFriends.GetFriendPersonaName(e.m_steamIDUser);
					menuItem.SuffixText = () => e.m_nGlobalRank + ". " + personaName + " : " + Math.Round((float)e.m_nScore / 32f * 100f, 1) + " %";
					menuItem.Hovered = !flag2;
					flag2 = true;
					if ((leaderboard.View == LeaderboardView.MyScore || leaderboard.View == LeaderboardView.Friends) && e.m_steamIDUser == Steamworks.SteamUser.GetSteamID())
					{
						foreach (MenuItem item in Items)
						{
							item.Hovered = false;
						}
						menuItem.Hovered = true;
						base.SelectedIndex = num;
						flag = true;
					}
					num++;
				}
			}
			if (leaderboard.View == LeaderboardView.Friends && viewChanged && !flag)
			{
				if (leaderboard.CanPageDown)
				{
					Items.Clear();
					leaderboard.PageDown(Refresh);
					return;
				}
				base.SelectedIndex = 0;
			}
			if (!flag)
			{
				base.SelectedIndex = Math.Min(selectedIndex, Items.Count - 1);
			}
			if (moveToLast)
			{
				base.SelectedIndex = Items.Count - 1;
				moveToLast = false;
			}
			if (moveToTop)
			{
				base.SelectedIndex = 0;
				moveToTop = false;
			}
			if (Items.Count > 0 && base.SelectedIndex == -1)
			{
				base.SelectedIndex = 0;
			}
			if (!leaderboard.InError)
			{
				if (Items.Count == 0)
				{
					string text = ((view == LeaderboardView.MyScore) ? "NotRankedInLeaderboard" : "NoEntriesLeaderboard");
					AddItem(text, Util.NullAction);
					base.SelectedIndex = -1;
				}
				else
				{
					while (Items.Count < 10)
					{
						AddItem(null, Util.NullAction);
					}
				}
			}
			base.XButtonString = "{X} " + string.Format(StaticText.GetString("CurrentLeaderboardView"), StaticText.GetString(string.Concat(view, "LeaderboardView")));
			viewChanged = false;
		}
	}
}
