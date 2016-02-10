#region

using System;
using System.Drawing;
using System.Windows.Forms;

#endregion

namespace Hearthstone_Deck_Tracker
{
	public class TrayIcon
	{
		public const string ClassCardsFirstMenuItemName = "classCardsFirst";
		public const string StartHearthstoneMenuItemName = "startHearthstone";
		public const string AutoSelectDeckMenuItemName = "autoSelectDeck";
		public const string UseNoDeckMenuItemName = "useNoDeck";
		public const string CheckedProperty = "Checked";
		private NotifyIcon _notifyIcon;

		public NotifyIcon NotifyIcon
		{
			get
			{
				if(_notifyIcon == null)
					Initialize();
				return _notifyIcon;
			}
		}

		public void Initialize()
		{
			_notifyIcon = new NotifyIcon
			{
				Icon = new Icon(@"Images/HearthstoneDeckTracker16.ico"),
				Visible = true,
				ContextMenu = new ContextMenu(),
				Text = "Hearthstone Deck Tracker v" + (Helper.GetCurrentVersion() ?? new Version("0.0")).ToVersionString()
			};

			var startHearthstonMenuItem = new MenuItem((string)App.Current.FindResource("START LAUNCHER / HEARTHSTONE"), (sender, args) => Helper.StartHearthstoneAsync())
			{
				Name = StartHearthstoneMenuItemName
			};
			_notifyIcon.ContextMenu.MenuItems.Add(startHearthstonMenuItem);

			var useNoDeckMenuItem = new MenuItem((string)App.Current.FindResource("USE _NO DECK"), (sender, args) => UseNoDeckContextMenu()) {Name = UseNoDeckMenuItemName};
			_notifyIcon.ContextMenu.MenuItems.Add(useNoDeckMenuItem);

			var autoSelectDeckMenuItem = new MenuItem((string)App.Current.FindResource("AUTO DECK SELECTION"), (sender, args) => AutoDeckDetectionContextMenu())
			{
				Name = AutoSelectDeckMenuItemName
			};
			_notifyIcon.ContextMenu.MenuItems.Add(autoSelectDeckMenuItem);

			var classCardsFirstMenuItem = new MenuItem((string)App.Current.FindResource("CLASS CARDS FIRST"), (sender, args) => SortClassCardsFirstContextMenu())
			{
				Name = ClassCardsFirstMenuItemName
			};
			_notifyIcon.ContextMenu.MenuItems.Add(classCardsFirstMenuItem);

			_notifyIcon.ContextMenu.MenuItems.Add((string)App.Current.FindResource("show"), (sender, args) => Core.MainWindow.ActivateWindow());
			_notifyIcon.ContextMenu.MenuItems.Add((string)App.Current.FindResource("exit"), (sender, args) => Core.MainWindow.Close());
			_notifyIcon.MouseClick += (sender, args) =>
			{
				if(args.Button == MouseButtons.Left)
					Core.MainWindow.ActivateWindow();
			};

			_notifyIcon.BalloonTipClicked += (sender1, e) => { Core.MainWindow.ActivateWindow(); };
		}

		private void AutoDeckDetectionContextMenu()
			=> Core.MainWindow.AutoDeckDetection(!(bool)GetContextMenuProperty(AutoSelectDeckMenuItemName, CheckedProperty));

		private void UseNoDeckContextMenu()
		{
			if((bool)GetContextMenuProperty(UseNoDeckMenuItemName, CheckedProperty))
				Core.MainWindow.SelectLastUsedDeck();
			else
				Core.MainWindow.SelectDeck(null, true);
		}

		private int IndexOfKeyContextMenuItem(string key) => NotifyIcon.ContextMenu.MenuItems.IndexOfKey(key);

		public void SetContextMenuProperty(string key, string property, object value)
		{
			var target = NotifyIcon.ContextMenu.MenuItems[IndexOfKeyContextMenuItem(key)];
			target.GetType().GetProperty(property).SetValue(target, value);
		}

		private object GetContextMenuProperty(string key, string property)
		{
			var target = NotifyIcon.ContextMenu.MenuItems[IndexOfKeyContextMenuItem(key)];
			return target.GetType().GetProperty(property).GetValue(target, null);
		}

		private void SortClassCardsFirstContextMenu()
			=> Core.MainWindow.SortClassCardsFirst(!(bool)GetContextMenuProperty(ClassCardsFirstMenuItemName, CheckedProperty));

		public void ShowMessage(string text, string title = "Hearthstone Deck Tracker", int duration = 5, ToolTipIcon icon = ToolTipIcon.Info)
			=> _notifyIcon.ShowBalloonTip(duration, title, text, icon);
	}
}
