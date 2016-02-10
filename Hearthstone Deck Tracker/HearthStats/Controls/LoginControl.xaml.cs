#region

using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Hearthstone_Deck_Tracker.HearthStats.API;
using Hearthstone_Deck_Tracker.Windows;
using MahApps.Metro.Controls.Dialogs;

#endregion

namespace Hearthstone_Deck_Tracker.HearthStats.Controls
{
	/// <summary>
	/// Interaction logic for LoginControl.xaml
	/// </summary>
	public partial class LoginControl : UserControl
	{
		private readonly bool _inizialized;

		public LoginControl()
		{
			InitializeComponent();
			CheckBoxRememberLogin.IsChecked = Config.Instance.RememberHearthStatsLogin;
			_inizialized = true;
		}

		private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
		{
			Process.Start(e.Uri.AbsoluteUri);
		}

		private async void BtnLogin_Click(object sender, RoutedEventArgs e)
		{
			var email = TextBoxEmail.Text;
			if(string.IsNullOrEmpty(email) || !Regex.IsMatch(email, @".*@.*\..*"))
			{
				DisplayLoginError((string)App.Current.FindResource("Please enter an valid email address"));
				return;
			}
			if(string.IsNullOrEmpty(TextBoxPassword.Password))
			{
				DisplayLoginError((string)App.Current.FindResource((string)App.Current.FindResource("Please enter a password")));
				return;
			}
			IsEnabled = false;
			var result = await HearthStatsAPI.LoginAsync(TextBoxEmail.Text, TextBoxPassword.Password);
			TextBoxPassword.Clear();
			if(result.Success)
			{
				Core.MainWindow.EnableHearthStatsMenu(true);
				Core.MainWindow.FlyoutHearthStatsLogin.IsOpen = false;
				Core.MainWindow.MenuItemLogin.Visibility = Visibility.Collapsed;
				Core.MainWindow.MenuItemLogout.Visibility = Visibility.Visible;
				Core.MainWindow.SeparatorLogout.Visibility = Visibility.Visible;
				Core.MainWindow.MenuItemLogout.Header = $"LOGOUT ({HearthStatsAPI.LoggedInAs})";

				var dialogResult =
					await
					Core.MainWindow.ShowMessageAsync((string)App.Current.FindResource("Sync now?"), (string)App.Current.FindResource("Do you want to sync with HearthStats now?"),
					                                 MessageDialogStyle.AffirmativeAndNegative,
					                                 new MessageDialogs.Settings {AffirmativeButtonText = (string)App.Current.FindResource("sync now"), NegativeButtonText = (string)App.Current.FindResource("later")});
				if(dialogResult == MessageDialogResult.Affirmative)
					HearthStatsManager.SyncAsync();
			}
			else
				DisplayLoginError(result.Message);
		}

		private void DisplayLoginError(string error)
		{
			TextBlockErrorMessage.Text = "Error:\n" + error;
			TextBlockErrorMessage.Visibility = Visibility.Visible;
			IsEnabled = true;
		}

		private void CheckBoxRememberLogin_Checked(object sender, RoutedEventArgs e)
		{
			if(!_inizialized)
				return;
			Config.Instance.RememberHearthStatsLogin = true;
			Config.Save();
		}

		private void CheckBoxRememberLogin_OnUnchecked(object sender, RoutedEventArgs e)
		{
			if(!_inizialized)
				return;
			Config.Instance.RememberHearthStatsLogin = false;
			Config.Save();
			try
			{
				if(File.Exists(Config.Instance.HearthStatsFilePath))
					File.Delete(Config.Instance.HearthStatsFilePath);
			}
			catch(Exception ex)
			{
				Logger.WriteLine("Error deleting hearthstats credentials file\n" + ex, "HearthStatsAPI");
			}
		}
	}
}
