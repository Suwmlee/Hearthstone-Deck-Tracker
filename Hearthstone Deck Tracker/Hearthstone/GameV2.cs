﻿#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Hearthstone_Deck_Tracker.Enums;
using Hearthstone_Deck_Tracker.Enums.Hearthstone;
using Hearthstone_Deck_Tracker.Hearthstone.Entities;
using Hearthstone_Deck_Tracker.Replay;
using Hearthstone_Deck_Tracker.Stats;
using Hearthstone_Deck_Tracker.Windows;
using MahApps.Metro.Controls.Dialogs;

#endregion

namespace Hearthstone_Deck_Tracker.Hearthstone
{
	public class GameV2 : IGame
	{
		private static List<string> _hsLogLines = new List<string>();
		public readonly List<Deck> DiscardedArenaDecks = new List<Deck>();
		private bool _awaitingMainWindowOpen;
		private GameMode _currentGameMode;
		private bool _gameModeDetectionComplete;

		private bool _gameModeDetectionRunning;
		private readonly GameTime _gameTime = new GameTime();
		public Deck TempArenaDeck = new Deck();

		public GameV2()
		{
			Player = new Player(true);
			Opponent = new Player(false);

			Entities = new Dictionary<int, Entity>();
			CurrentGameMode = GameMode.None;
			IsInMenu = true;
			PossibleArenaCards = new List<Card>();
			PossibleConstructedCards = new List<Card>();
			OpponentSecrets = new OpponentSecrets(this);
			Reset();
		}

		public static List<string> HSLogLines
		{
			get { return _hsLogLines; }
		}

		public Deck IgnoreIncorrectDeck { get; set; }

		public GameTime GameTime
		{
			get { return _gameTime; }
		}

		public bool IsMinionInPlay
		{
			get { return Entities.FirstOrDefault(x => (x.Value.IsInPlay && x.Value.IsMinion)).Value != null; }
		}

		public bool IsOpponentMinionInPlay
		{
			get
			{
				return Entities.FirstOrDefault(x => (x.Value.IsInPlay && x.Value.IsMinion && x.Value.IsControlledBy(Opponent.Id))).Value != null;
			}
		}

		public int OpponentMinionCount
		{
			get { return Entities.Count(x => (x.Value.IsInPlay && x.Value.IsMinion && x.Value.IsControlledBy(Opponent.Id))); }
		}

		public int PlayerMinionCount
		{
			get { return Entities.Count(x => (x.Value.IsInPlay && x.Value.IsMinion && x.Value.IsControlledBy(Player.Id))); }
		}

		public Player Player { get; set; }
		public Player Opponent { get; set; }
		public bool NoMatchingDeck { get; set; }
		public bool IsInMenu { get; set; }
		public bool IsUsingPremade { get; set; }
		public int OpponentSecretCount { get; set; }
		public bool IsRunning { get; set; }
		public Region CurrentRegion { get; set; }
		public GameStats CurrentGameStats { get; set; }
		public OpponentSecrets OpponentSecrets { get; set; }
		public List<Card> DrawnLastGame { get; set; }
		public List<Card> PossibleArenaCards { get; set; }
		public List<Card> PossibleConstructedCards { get; set; }
		public Dictionary<int, Entity> Entities { get; set; }
		public bool SavedReplay { get; set; }

		public Entity PlayerEntity
		{
			get { return Entities.FirstOrDefault(x => x.Value.IsPlayer).Value; }
		}

		public Entity OpponentEntity
		{
			get { return Entities.FirstOrDefault(x => x.Value.HasTag(GAME_TAG.PLAYER_ID) && !x.Value.IsPlayer).Value; }
		}

		public bool IsMulliganDone
		{
			get
			{
				var player = Entities.FirstOrDefault(x => x.Value.IsPlayer);
				var opponent = Entities.FirstOrDefault(x => x.Value.HasTag(GAME_TAG.PLAYER_ID) && !x.Value.IsPlayer);
				if(player.Value == null || opponent.Value == null)
					return false;
				return player.Value.GetTag(GAME_TAG.MULLIGAN_STATE) == (int)TAG_MULLIGAN.DONE
				       && opponent.Value.GetTag(GAME_TAG.MULLIGAN_STATE) == (int)TAG_MULLIGAN.DONE;
			}
		}

		public GameMode CurrentGameMode
		{
			get { return _currentGameMode; }
			set
			{
				if(_currentGameMode != value)
				{
					_currentGameMode = value;
					Logger.WriteLine("Set CurrentGameMode to " + value, "Game");
				}
			}
		}

		public void Reset(bool resetStats = true)
		{
			Logger.WriteLine("-------- Reset ---------", "Game");

			ReplayMaker.Reset();
			Player.Reset();
			Opponent.Reset();

			Entities.Clear();
			SavedReplay = false;
			OpponentSecretCount = 0;
			OpponentSecrets.ClearSecrets();
			NoMatchingDeck = false;

			if(!IsInMenu && resetStats)
			{
				if(CurrentGameMode == GameMode.Ranked)
				{
					Logger.WriteLine("Resetting gamemode to casual", "Game");
					CurrentGameMode = GameMode.Casual;
				}
				CurrentGameStats = new GameStats(GameResult.None, "", "") {PlayerName = "", OpponentName = "", Region = CurrentRegion};
				_gameModeDetectionComplete = false;
			}
			_hsLogLines = new List<string>();

			if(Core.Game != null && Core.Overlay != null)
			{
				Helper.UpdatePlayerCards();
				Helper.UpdateOpponentCards();
				Core.MainWindow.NeedToIncorrectDeckMessage = false;
			}
		}

		public void SetPremadeDeck(Deck deck)
		{
			foreach(var card in deck.GetSelectedDeckVersion().Cards)
			{
				for(var i = 0; i < card.Count; i++)
					Player.RevealDeckCard(card.Id, -1);
			}
			IsUsingPremade = true;
		}

		public void AddPlayToCurrentGame(PlayType play, int turn, string cardId)
		{
			if(CurrentGameStats == null)
				return;
			CurrentGameStats.AddPlay(play, turn, cardId);
		}

		public void ResetArenaCards()
		{
			PossibleArenaCards.Clear();
		}

		public void ResetConstructedCards()
		{
			PossibleConstructedCards.Clear();
		}

		public async Task GameModeDetection(int timeoutInSeconds = 300)
		{
			if(_gameModeDetectionRunning || _gameModeDetectionComplete)
			{
				while(!_gameModeDetectionComplete)
					await Task.Delay(100);
				return;
			}
			_gameModeDetectionRunning = true;
			var startTime = DateTime.Now;
			var timeout = TimeSpan.FromSeconds(timeoutInSeconds);
			while(CurrentGameMode == GameMode.None && (DateTime.Now - startTime) < timeout)
				await Task.Delay(100);
			if(CurrentGameStats != null && CurrentGameMode != GameMode.None)
			{
				CurrentGameStats.GameMode = CurrentGameMode;
				Logger.WriteLine("Detected gamemode, set CurrentGameStats.GameMode=" + CurrentGameMode, "GameModeDetection");
			}
			_gameModeDetectionComplete = true;
			_gameModeDetectionRunning = false;
		}

		public void NewArenaDeck(string heroId)
		{
			TempArenaDeck = new Deck
			{
				Name = Helper.ParseDeckNameTemplate(Config.Instance.ArenaDeckNameTemplate),
				IsArenaDeck = true,
				Class = Database.GetHeroNameFromId(heroId)
			};
			Logger.WriteLine("Created new arena deck: " + TempArenaDeck.Class);
		}

		public void NewArenaCard(string cardId)
		{
			if(TempArenaDeck == null || string.IsNullOrEmpty(cardId))
				return;
			var existingCard = TempArenaDeck.Cards.FirstOrDefault(c => c.Id == cardId);
			if(existingCard != null)
				existingCard.Count++;
			else
				TempArenaDeck.Cards.Add((Card)Database.GetCardFromId(cardId).Clone());
			var numCards = TempArenaDeck.Cards.Sum(c => c.Count);
			Logger.WriteLine(string.Format("Added new card to arena deck: {0} ({1}/30)", cardId, numCards));
			if(numCards == 30)
			{
				Logger.WriteLine("Found complete arena deck!");
				if(!Config.Instance.SelectedArenaImportingBehaviour.HasValue)
				{
					Logger.WriteLine("...but we are using the old importing method.");
					return;
				}
				var recentArenaDecks = DeckList.Instance.Decks.Where(d => d.IsArenaDeck).OrderByDescending(d => d.LastPlayedNewFirst).Take(15);
				if(recentArenaDecks.Any(d => d.Cards.All(c => TempArenaDeck.Cards.Any(c2 => c.Id == c2.Id && c.Count == c2.Count))))
				{
					Logger.WriteLine("...but we already have that one. Discarding.");
					TempArenaDeck.Cards.Clear();
					return;
				}
				if(DiscardedArenaDecks.Any(d => d.Cards.All(c => TempArenaDeck.Cards.Any(c2 => c.Id == c2.Id && c.Count == c2.Count))))
				{
					Logger.WriteLine("...but it was already discarded by the user. No automatic action taken.");
					return;
				}
				if(Config.Instance.SelectedArenaImportingBehaviour.Value == ArenaImportingBehaviour.AutoImportSave)
				{
					Logger.WriteLine("...auto saving new arena deck.");
					Core.MainWindow.SetNewDeck(TempArenaDeck);
					Core.MainWindow.SaveDeck(false, TempArenaDeck.Version);
					TempArenaDeck.Cards.Clear();
				}
				else if(Config.Instance.SelectedArenaImportingBehaviour.Value == ArenaImportingBehaviour.AutoAsk)
				{
					ShowNewArenaDeckMessageAsync((Deck)TempArenaDeck.Clone());
					TempArenaDeck.Cards.Clear();
				}
			}
		}

		private async void ShowNewArenaDeckMessageAsync(Deck deck)
		{
			if(_awaitingMainWindowOpen)
				return;
			_awaitingMainWindowOpen = true;

			if(Core.MainWindow.WindowState == WindowState.Minimized)
				Core.TrayIcon.ShowMessage((string)App.Current.FindResource("New arena deck detected!"));

			while(Core.MainWindow.Visibility != Visibility.Visible || Core.MainWindow.WindowState == WindowState.Minimized)
				await Task.Delay(100);

			var result =
				await
				Core.MainWindow.ShowMessageAsync((string)App.Current.FindResource("New arena deck detected!"),
				                                 (string)App.Current.FindResource("You can change this behaviour to auto save import or manual in [options > tracker > importing]"),
				                                 MessageDialogStyle.AffirmativeAndNegative,
				                                 new MessageDialogs.Settings {AffirmativeButtonText = (string)App.Current.FindResource("import"), NegativeButtonText = (string)App.Current.FindResource("cancel")});

			if(result == MessageDialogResult.Affirmative)
			{
				Logger.WriteLine("...saving new arena deck.");
				Core.MainWindow.SetNewDeck(deck);
				Core.MainWindow.ActivateWindow();
			}
			else
			{
				Logger.WriteLine("...discarded by user.");
				DiscardedArenaDecks.Add(deck);
			}
			_awaitingMainWindowOpen = false;
		}

		public static void AddHSLogLine(string logLine)
		{
			HSLogLines.Add(logLine);
		}

		#region Database - Obsolete

		[Obsolete("Use Hearthstone.Database.GetCardFromId")]
		public static Card GetCardFromId(string cardId)
		{
			return Database.GetCardFromId(cardId);
		}

		[Obsolete("Use Hearthstone.Database.GetCardFromName")]
		public static Card GetCardFromName(string name, bool localized = false)
		{
			return Database.GetCardFromName(name, localized);
		}

		[Obsolete("Use Hearthstone.Database.GetActualCards")]
		public static List<Card> GetActualCards()
		{
			return Database.GetActualCards();
		}

		[Obsolete("Use Hearthstone.Database.GetHeroNameFromId")]
		public static string GetHeroNameFromId(string id, bool returnIdIfNotFound = true)
		{
			return Database.GetHeroNameFromId(id, returnIdIfNotFound);
		}

		[Obsolete("Use Hearthstone.Database.IsActualCard")]
		public static bool IsActualCard(Card card)
		{
			return Database.IsActualCard(card);
		}

		#endregion
	}
}