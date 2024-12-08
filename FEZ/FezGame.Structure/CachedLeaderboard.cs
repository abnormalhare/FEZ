using System;
using System.Collections.Generic;
using System.Linq;
using FezGame.Services;
using Steamworks;

namespace FezGame.Structure;

public class CachedLeaderboard
{
	private const int ReadPageSize = 100;

	private const string LeaderboardKey = "CompletionPercentage";

	private readonly int virtualPageSize;

	private readonly List<LeaderboardEntry_t> cachedEntries = new List<LeaderboardEntry_t>();

	private SteamLeaderboard_t leaderboard;

	private int startIndex;

	private Action callback;

	private Action onLeaderboardFound;

	public LeaderboardView View { get; private set; }

	public SteamUser ActiveGamer { get; set; }

	public bool InError
	{
		get
		{
			if (leaderboard.m_SteamLeaderboard == 0L)
			{
				return !Reading;
			}
			return false;
		}
	}

	public bool Reading { get; private set; }

	public bool ChangingPage { get; private set; }

	public bool CanPageUp
	{
		get
		{
			//IL_003b: Unknown result type (might be due to invalid IL or missing references)
			if (cachedEntries.Count == 0)
			{
				return false;
			}
			if (View == LeaderboardView.Friends)
			{
				return startIndex - virtualPageSize >= 0;
			}
			if (startIndex <= 0)
			{
				return cachedEntries[0].m_nGlobalRank > 1;
			}
			return true;
		}
	}

	public bool CanPageDown
	{
		get
		{
			//IL_0058: Unknown result type (might be due to invalid IL or missing references)
			if (cachedEntries.Count == 0)
			{
				return false;
			}
			if (View == LeaderboardView.Friends)
			{
				return startIndex + virtualPageSize < cachedEntries.Count;
			}
			if (startIndex >= cachedEntries.Count)
			{
				return cachedEntries[cachedEntries.Count - 1].m_nGlobalRank < TotalEntries;
			}
			return true;
		}
	}

	public IEnumerable<LeaderboardEntry_t> Entries => cachedEntries.Skip(startIndex).Take(virtualPageSize);

	public int TotalEntries
	{
		get
		{
			//IL_000e: Unknown result type (might be due to invalid IL or missing references)
			if (leaderboard.m_SteamLeaderboard != 0L)
			{
				return SteamUserStats.GetLeaderboardEntryCount(leaderboard);
			}
			return 0;
		}
	}

	public CachedLeaderboard(SteamUser activeGamer, int virtualPageSize)
	{
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		this.virtualPageSize = virtualPageSize;
		ActiveGamer = activeGamer;
		CallResult<LeaderboardFindResult_t> obj = new CallResult<LeaderboardFindResult_t>((APIDispatchDelegate<LeaderboardFindResult_t>)OnReceiveLeaderboard);
		SteamAPICall_t val = SteamUserStats.FindLeaderboard("CompletionPercentage");
		obj.Set(val, (APIDispatchDelegate<LeaderboardFindResult_t>)null);
	}

	private void OnReceiveLeaderboard(LeaderboardFindResult_t result, bool bIOFailure)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		leaderboard = result.m_hSteamLeaderboard;
		if (onLeaderboardFound != null)
		{
			onLeaderboardFound();
		}
		onLeaderboardFound = null;
	}

	private void CacheEntries(LeaderboardScoresDownloaded_t result, bool bIOFailure)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		CacheEntries(result, 0);
	}

	private void CacheEntriesUp(LeaderboardScoresDownloaded_t result, bool bIOFailure)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		CacheEntries(result, -1);
	}

	private void CacheEntriesDown(LeaderboardScoresDownloaded_t result, bool bIOFailure)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		CacheEntries(result, 1);
	}

	private void CacheEntries(LeaderboardScoresDownloaded_t result, int pageSign)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		if (result.m_hSteamLeaderboardEntries.m_SteamLeaderboardEntries != 0L)
		{
			if (pageSign == 0)
			{
				cachedEntries.Clear();
			}
			if (leaderboard.m_SteamLeaderboard != 0L)
			{
				if (pageSign == -1)
				{
					LeaderboardEntry_t item = default(LeaderboardEntry_t);
					for (int num = result.m_cEntryCount - 1; num >= 0; num--)
					{
						SteamUserStats.GetDownloadedLeaderboardEntry(result.m_hSteamLeaderboardEntries, num, ref item, (int[])null, 0);
						cachedEntries.Insert(0, item);
					}
					startIndex += result.m_cEntryCount;
					startIndex -= virtualPageSize;
				}
				else
				{
					LeaderboardEntry_t item2 = default(LeaderboardEntry_t);
					for (int i = 0; i < result.m_cEntryCount; i++)
					{
						SteamUserStats.GetDownloadedLeaderboardEntry(result.m_hSteamLeaderboardEntries, i, ref item2, (int[])null, 0);
						cachedEntries.Add(item2);
					}
				}
			}
		}
		Reading = false;
		callback();
	}

	public void ChangeView(LeaderboardView leaderboardView, Action onFinished)
	{
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		if (Reading)
		{
			onFinished();
			return;
		}
		if (leaderboard.m_SteamLeaderboard == 0L)
		{
			onLeaderboardFound = delegate
			{
				ChangeView(leaderboardView, onFinished);
			};
			return;
		}
		Reading = true;
		View = leaderboardView;
		callback = onFinished;
		CallResult<LeaderboardScoresDownloaded_t> val = new CallResult<LeaderboardScoresDownloaded_t>((APIDispatchDelegate<LeaderboardScoresDownloaded_t>)CacheEntries);
		switch (View)
		{
		case LeaderboardView.Friends:
		{
			SteamAPICall_t val2 = SteamUserStats.DownloadLeaderboardEntries(leaderboard, (ELeaderboardDataRequest)2, 0, 100);
			val.Set(val2, (APIDispatchDelegate<LeaderboardScoresDownloaded_t>)null);
			startIndex = 0;
			break;
		}
		case LeaderboardView.MyScore:
		{
			SteamAPICall_t val2 = SteamUserStats.DownloadLeaderboardEntries(leaderboard, (ELeaderboardDataRequest)1, -50, 50);
			val.Set(val2, (APIDispatchDelegate<LeaderboardScoresDownloaded_t>)null);
			startIndex = 50 - virtualPageSize / 2 + 1;
			break;
		}
		case LeaderboardView.Overall:
		{
			SteamAPICall_t val2 = SteamUserStats.DownloadLeaderboardEntries(leaderboard, (ELeaderboardDataRequest)0, 1, 100);
			val.Set(val2, (APIDispatchDelegate<LeaderboardScoresDownloaded_t>)null);
			startIndex = 0;
			break;
		}
		}
	}

	public void PageUp(Action onFinished)
	{
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		if (InError)
		{
			onFinished();
			return;
		}
		if (startIndex >= virtualPageSize)
		{
			startIndex -= virtualPageSize;
			onFinished();
			return;
		}
		if (!CanPageUp)
		{
			onFinished();
			return;
		}
		callback = onFinished;
		CallResult<LeaderboardScoresDownloaded_t> obj = new CallResult<LeaderboardScoresDownloaded_t>((APIDispatchDelegate<LeaderboardScoresDownloaded_t>)CacheEntriesUp);
		SteamAPICall_t val = SteamUserStats.DownloadLeaderboardEntries(leaderboard, (ELeaderboardDataRequest)0, cachedEntries[0].m_nGlobalRank - 100, cachedEntries[0].m_nGlobalRank - 1);
		obj.Set(val, (APIDispatchDelegate<LeaderboardScoresDownloaded_t>)null);
	}

	public void PageDown(Action onFinished)
	{
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		if (InError)
		{
			onFinished();
			return;
		}
		if (startIndex + virtualPageSize * 2 <= cachedEntries.Count)
		{
			startIndex += virtualPageSize;
			onFinished();
			return;
		}
		if (!CanPageDown)
		{
			onFinished();
			return;
		}
		if (View == LeaderboardView.Friends)
		{
			startIndex += virtualPageSize;
			onFinished();
			return;
		}
		startIndex += virtualPageSize;
		callback = onFinished;
		CallResult<LeaderboardScoresDownloaded_t> obj = new CallResult<LeaderboardScoresDownloaded_t>((APIDispatchDelegate<LeaderboardScoresDownloaded_t>)CacheEntriesDown);
		SteamAPICall_t val = SteamUserStats.DownloadLeaderboardEntries(leaderboard, (ELeaderboardDataRequest)0, cachedEntries[cachedEntries.Count - 1].m_nGlobalRank + 1, cachedEntries[cachedEntries.Count - 1].m_nGlobalRank + 100);
		obj.Set(val, (APIDispatchDelegate<LeaderboardScoresDownloaded_t>)null);
	}
}
