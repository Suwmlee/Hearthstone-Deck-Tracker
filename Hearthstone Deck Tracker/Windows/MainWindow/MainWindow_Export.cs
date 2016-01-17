﻿#region

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using Hearthstone_Deck_Tracker.Exporting;
using Hearthstone_Deck_Tracker.Hearthstone;
using MahApps.Metro.Controls.Dialogs;

#endregion

namespace Hearthstone_Deck_Tracker.Windows
{
	public partial class MainWindow
	{
		private void BtnExport_Click(object sender, RoutedEventArgs e)
		{
			var deck = DeckPickerList.SelectedDecks.FirstOrDefault() ?? DeckList.Instance.ActiveDeck;
			if(deck == null)
				return;
			ExportDeck(deck.GetSelectedDeckVersion());
		}

		private async void ExportDeck(Deck deck)
		{
			var export = true;
			if(Config.Instance.ShowExportingDialog)
			{
                var message = (string)App.Current.FindResource("1) create a new") + " " + (string)App.Current.FindResource(deck.Class) + " " + (string)App.Current.FindResource("deck") +
                    (Config.Instance.AutoClearDeck ? (string)App.Current.FindResource("(or open an existing one to be cleared automatically)") : "").ToString() +
                    (string)App.Current.FindResource("export tips 2");

				if(deck.GetSelectedDeckVersion().Cards.Any(c => c.Name == "Stalagg" || c.Name == "Feugen"))
				{
                    message += (string)App.Current.FindResource("export tips note");
				}

				var settings = new MessageDialogs.Settings {AffirmativeButtonText = (string)App.Current.FindResource("Export"),NegativeButtonText = (string)App.Current.FindResource("cancel")};
				var result =
					await
					this.ShowMessageAsync((string)App.Current.FindResource("Export") + " " + deck.Name + " " + (string)App.Current.FindResource("to Hearthstone"), message, MessageDialogStyle.AffirmativeAndNegative, settings);
				export = result == MessageDialogResult.Affirmative;
			}
			if(export)
			{
				var controller = await this.ShowProgressAsync((string)App.Current.FindResource("Creating Deck"), (string)App.Current.FindResource("Please do not move your mouse or type."));
				Topmost = false;
				await Task.Delay(500);
				await DeckExporter.Export(deck);
				await controller.CloseAsync();

				if(deck.MissingCards.Any())
					this.ShowMissingCardsMessage(deck);
			}
		}

		private async void BtnScreenhot_Click(object sender, RoutedEventArgs e)
		{
			var selectedDeck = DeckPickerList.SelectedDecks.FirstOrDefault();
			if(selectedDeck == null)
				return;
			Logger.WriteLine("Creating screenshot of " + selectedDeck.GetSelectedDeckVersion().GetDeckInfo(), "Screenshot");
			var screenShotWindow = new PlayerWindow(Core.Game, selectedDeck.GetSelectedDeckVersion().Cards.ToSortedCardList());
			screenShotWindow.Show();
			screenShotWindow.Top = 0;
			screenShotWindow.Left = 0;
			await Task.Delay(100);
			var source = PresentationSource.FromVisual(screenShotWindow);
			if(source == null)
				return;

			//adjusting the DPI is apparently no longer/not necessary?
			var dpiX = 96.0; //* source.CompositionTarget.TransformToDevice.M11;
			var dpiY = 96.0; //* source.CompositionTarget.TransformToDevice.M22;

			var deck = selectedDeck.GetSelectedDeckVersion();
			var pngEncoder = Helper.ScreenshotDeck(screenShotWindow.ListViewPlayer, 96, 96, deck.Name);
			screenShotWindow.Shutdown();
			SaveOrUploadScreenshot(pngEncoder, deck.Name);
		}

		public async Task SaveOrUploadScreenshot(PngBitmapEncoder pngEncoder, string proposedFileName)
		{
			if(pngEncoder != null)
			{
				var saveOperation = await this.ShowScreenshotUploadSelectionDialog();
				var tmpFile = new FileInfo(Path.Combine(Config.Instance.DataDir, string.Format("tmp{0}.png", DateTime.Now.ToFileTime())));
				var fileName = saveOperation.SaveLocal
					               ? Helper.ShowSaveFileDialog(Helper.RemoveInvalidFileNameChars(proposedFileName), "png") : tmpFile.FullName;
				if(fileName != null)
				{
					string imgurUrl = null;
					using(var ms = new MemoryStream())
					using(var fs = new FileStream(fileName, FileMode.Create, FileAccess.Write))
					{
						pngEncoder.Save(ms);
						ms.WriteTo(fs);
						if(saveOperation.Upload)
						{
							var controller = await this.ShowProgressAsync("Uploading...", "");
							imgurUrl = await Imgur.Upload(Config.Instance.ImgurClientId, ms, proposedFileName);
							await controller.CloseAsync();
						}
					}

					if(imgurUrl != null)
					{
						await this.ShowSavedAndUploadedFileMessage(saveOperation.SaveLocal ? fileName : null, imgurUrl);
						Logger.WriteLine("Uploaded screenshot to " + imgurUrl, "Export");
					}
					else
						await this.ShowSavedFileMessage(fileName);
					Logger.WriteLine("Saved screenshot to: " + fileName, "Export");
				}
				if(tmpFile.Exists)
				{
					try
					{
						tmpFile.Delete();
					}
					catch(Exception ex)
					{
						Logger.WriteLine(ex.ToString(), "ExportScreenshot");
					}
				}
			}
		}

		private async void BtnSaveToFile_OnClick(object sender, RoutedEventArgs e)
		{
			var deck = DeckPickerList.SelectedDecks.FirstOrDefault();
			if(deck == null)
				return;

			var fileName = Helper.ShowSaveFileDialog(Helper.RemoveInvalidFileNameChars(deck.Name), "xml");

			if(fileName != null)
			{
				XmlManager<Deck>.Save(fileName, deck.GetSelectedDeckVersion());
				await this.ShowSavedFileMessage(fileName);
				Logger.WriteLine("Saved " + deck.GetSelectedDeckVersion().GetDeckInfo() + " to file: " + fileName, "Export");
			}
		}

		private void BtnClipboard_OnClick(object sender, RoutedEventArgs e)
		{
			var deck = DeckPickerList.SelectedDecks.FirstOrDefault();
			if(deck == null)
				return;
			Clipboard.SetText(Helper.DeckToIdString(deck.GetSelectedDeckVersion()));
			this.ShowMessage("", "copied ids to clipboard");
			Logger.WriteLine("Copied " + deck.GetSelectedDeckVersion().GetDeckInfo() + " to clipboard", "Export");
		}

		private async void BtnClipboardNames_OnClick(object sender, RoutedEventArgs e)
		{
			var deck = DeckPickerList.SelectedDecks.FirstOrDefault();
			if(deck == null || !deck.GetSelectedDeckVersion().Cards.Any())
				return;

			var english = true;
			if(Config.Instance.SelectedLanguage != "enUS")
			{
				try
				{
					english =
						await
						this.ShowMessageAsync("Select language", "", MessageDialogStyle.AffirmativeAndNegative,
						                      new MessageDialogs.Settings
						                      {
							                      AffirmativeButtonText = Helper.LanguageDict.First(x => x.Value == "enUS").Key,
							                      NegativeButtonText = Helper.LanguageDict.First(x => x.Value == Config.Instance.SelectedLanguage).Key
						                      })
						== MessageDialogResult.Affirmative;
				}
				catch(Exception ex)
				{
					Logger.WriteLine(ex.ToString());
				}
			}
			try
			{
				var names =
					deck.GetSelectedDeckVersion()
					    .Cards.ToSortedCardList()
					    .Select(c => (english ? c.Name : c.LocalizedName) + (c.Count > 1 ? " x " + c.Count : ""))
					    .Aggregate((c, n) => c + Environment.NewLine + n);
				Clipboard.SetText(names);
				this.ShowMessage("", "copied names to clipboard");
				Logger.WriteLine("Copied " + deck.GetDeckInfo() + " names to clipboard", "Export");
			}
			catch(Exception ex)
			{
				Logger.WriteLine("Error copying card names to clipboard: " + ex);
				this.ShowMessage("", "Error copying card names to clipboard.");
			}
		}

		private async void BtnExportFromWeb_Click(object sender, RoutedEventArgs e)
		{
			var url = await InputDeckURL();
			if(url == null)
				return;

			var deck = await ImportDeckFromURL(url);

			if(deck != null)
				ExportDeck(deck);
			else
				await this.ShowMessageAsync((string)App.Current.FindResource("Error"), (string)App.Current.FindResource("Could not load deck from specified url"));
		}

		internal void MenuItemMissingDust_OnClick(object sender, RoutedEventArgs e)
		{
			var deck = DeckPickerList.SelectedDecks.FirstOrDefault();
			if(deck == null)
				return;
			this.ShowMissingCardsMessage(deck);
		}

		public void BtnOpenHearthStats_Click(object sender, RoutedEventArgs e)
		{
			var deck = DeckPickerList.SelectedDecks.FirstOrDefault();
			if(deck == null || !deck.HasHearthStatsId)
				return;
			Process.Start(deck.HearthStatsUrl);
		}
	}
}