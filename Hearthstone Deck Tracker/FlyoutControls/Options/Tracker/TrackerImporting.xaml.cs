﻿#region

using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Hearthstone_Deck_Tracker.Enums;
using Hearthstone_Deck_Tracker.Hearthstone;
using MahApps.Metro.Controls.Dialogs;

#endregion

namespace Hearthstone_Deck_Tracker.FlyoutControls.Options.Tracker
{
	/// <summary>
	/// Interaction logic for OtherImporting.xaml
	/// </summary>
	public partial class TrackerImporting
	{
		private GameV2 _game;
		private bool _initialized;

		public TrackerImporting()
		{
			InitializeComponent();
		}

		private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
		{
			Process.Start(e.Uri.AbsoluteUri);
		}

		public void Load(GameV2 game)
		{
			_game = game;
			ComboboxArenaImportingBehaviour.IsEnabled = !Config.Instance.UseOldArenaImporting;
			ComboboxArenaImportingBehaviour.ItemsSource = Enum.GetValues(typeof(ArenaImportingBehaviour));
			if(Config.Instance.SelectedArenaImportingBehaviour.HasValue)
				ComboboxArenaImportingBehaviour.SelectedItem = Config.Instance.SelectedArenaImportingBehaviour.Value;
			CheckboxUseOldArenaImporting.IsChecked = Config.Instance.UseOldArenaImporting;
			BtnArenaHowTo.IsEnabled = Config.Instance.UseOldArenaImporting;
			CheckboxTagOnImport.IsChecked = Config.Instance.TagDecksOnImport;
			CheckboxImportNetDeck.IsChecked = Config.Instance.NetDeckClipboardCheck ?? false;
			CheckboxAutoSaveOnImport.IsChecked = Config.Instance.AutoSaveOnImport;
			TextBoxArenaTemplate.Text = Config.Instance.ArenaDeckNameTemplate;
			_initialized = true;
		}

		private void CheckboxTagOnImport_Checked(object sender, RoutedEventArgs e)
		{
			if(!_initialized)
				return;
			Config.Instance.TagDecksOnImport = true;
			Config.Save();
		}

		private void CheckboxTagOnImport_Unchecked(object sender, RoutedEventArgs e)
		{
			if(!_initialized)
				return;
			Config.Instance.TagDecksOnImport = false;
			Config.Save();
		}

		private void CheckboxImportNetDeck_OnChecked(object sender, RoutedEventArgs e)
		{
			if(!_initialized)
				return;
			Config.Instance.NetDeckClipboardCheck = true;
			Config.Save();
		}

		private void CheckboxImportNetDeck_OnUnchecked(object sender, RoutedEventArgs e)
		{
			if(!_initialized)
				return;
			Config.Instance.NetDeckClipboardCheck = false;
			Config.Save();
		}

		private void CheckboxAutoSaveOnImport_OnChecked(object sender, RoutedEventArgs e)
		{
			if(!_initialized)
				return;
			Config.Instance.AutoSaveOnImport = true;
			Config.Save();
		}

		private void CheckboxAutoSaveOnImport_OnUnchecked(object sender, RoutedEventArgs e)
		{
			if(!_initialized)
				return;
			Config.Instance.AutoSaveOnImport = false;
			Config.Save();
		}

		private async void ButtonArenaHowTo_OnClick(object sender, RoutedEventArgs e)
		{
			await
				Core.MainWindow.ShowMessageAsync((string)App.Current.FindResource("How this works:"),
				                                 (string)App.Current.FindResource("import tips arena"));
		}

		private async void ButtonConstructedHowTo_OnClick(object sender, RoutedEventArgs e)
		{
			await
				Core.MainWindow.ShowMessageAsync((string)App.Current.FindResource("How this works:"),
				                                 (string)App.Current.FindResource("import tips constructed"));
		}

		private void ButtonSetUpConstructed_OnClick(object sender, RoutedEventArgs e)
		{
			Helper.SetupConstructedImporting(_game);
		}

		private void BtnEditTemplate_Click(object sender, RoutedEventArgs e)
		{
			if(TextBoxArenaTemplate.IsEnabled)
			{
				BtnEditTemplate.Content = (string)App.Current.FindResource("EDIT");
				Config.Instance.ArenaDeckNameTemplate = TextBoxArenaTemplate.Text;
				Config.Save();
				TextBoxArenaTemplate.IsEnabled = false;
			}
			else
			{
				BtnEditTemplate.Content = (string)App.Current.FindResource("SAVE");
				TextBoxArenaTemplate.IsEnabled = true;
			}
		}

		private void TextBoxArenaTemplate_OnTextChanged(object sender, TextChangedEventArgs e)
		{
			TextBlockNamePreview.Text = Helper.ParseDeckNameTemplate(TextBoxArenaTemplate.Text);
		}

		private void ButtonActivateHdtProtocol_OnClick(object sender, RoutedEventArgs e)
		{
			Core.MainWindow.SetupProtocol();
		}

		private void CheckboxUseOldArenaImporting_OnChecked(object sender, RoutedEventArgs e)
		{
			if(!_initialized)
				return;
			Config.Instance.UseOldArenaImporting = true;
			ComboboxArenaImportingBehaviour.IsEnabled = false;
			ComboboxArenaImportingBehaviour.SelectedIndex = -1;
			BtnArenaHowTo.IsEnabled = true;
			Config.Save();
		}

		private void CheckboxUseOldArenaImporting_OnUnchecked(object sender, RoutedEventArgs e)
		{
			if(!_initialized)
				return;
			Config.Instance.UseOldArenaImporting = false;
			ComboboxArenaImportingBehaviour.IsEnabled = true;
			ComboboxArenaImportingBehaviour.SelectedItem = ArenaImportingBehaviour.AutoAsk;
			BtnArenaHowTo.IsEnabled = false;
			Config.Save();
		}

		private void ComboboxArenaImportingBehaviour_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if(!_initialized)
				return;
			var selected = ComboboxArenaImportingBehaviour.SelectedItem as ArenaImportingBehaviour?;
			if(selected != null)
			{
				Config.Instance.SelectedArenaImportingBehaviour = selected;
				Config.Save();
			}
		}
	}
}