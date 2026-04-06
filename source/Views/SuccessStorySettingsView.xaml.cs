using Playnite.SDK;
using CommonPluginsShared;
using SuccessStory.Clients;
using SuccessStory.Models;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Diagnostics;
using SuccessStory.Services;
using CommonPluginsShared.Models;
using Playnite.SDK.Data;
using System.Windows.Media;
using Playnite.SDK.Models;
using System.IO;
using System.Windows.Documents;
using System.Drawing.Imaging;

namespace SuccessStory.Views
{
    public partial class SuccessStorySettingsView : UserControl
    {
        private static ILogger Logger => LogManager.GetLogger();

        public static SolidColorBrush RarityUncommonColor { get; set; }
        public static SolidColorBrush RarityRareColor { get; set; }
        public static SolidColorBrush RarityUltraRareColor { get; set; }

        public static List<RaConsoleAssociated> RaConsoleAssociateds { get; set; }

        public static CompletionStatus CompletionStatus { get; set; }

        private TextBlock TbControl { get; set; }


        private SuccessStoryDatabase PluginDatabase => SuccessStory.PluginDatabase;

        public static List<Folder> LocalPath { get; set; } = new List<Folder>();
        public static List<Folder> Rpcs3Path { get; set; } = new List<Folder>();
		public static List<Folder> ShadPS4Path { get; set; } = new List<Folder>();

		private List<GameAchievements> IgnoredGames { get; set; }

        public static bool WithoutMessage { get; set; } = false;


        public SuccessStorySettingsView(SuccessStory plugin)
        {
            try
            {
                InitializeComponent();

                SteamPanel.StoreApi = SuccessStory.SteamApi;
                EpicPanel.StoreApi = SuccessStory.EpicApi;
                GogPanel.StoreApi = SuccessStory.GogApi;
                GameJoltPanel.StoreApi = SuccessStory.GameJoltApi;

                PART_WowRegion.Text = PluginDatabase.PluginSettings.Settings.WowRegions.Find(x => x.IsSelected)?.Name;
                PART_WowRealm.Text = PluginDatabase.PluginSettings.Settings.WowRealms.Find(x => x.IsSelected)?.Name;

                PART_SelectorColorPicker.OnlySimpleColor = true;

                RarityUncommonColor = PluginDatabase.PluginSettings.Settings.RarityUncommonColor;
                RarityRareColor = PluginDatabase.PluginSettings.Settings.RarityRareColor;
                RarityUltraRareColor = PluginDatabase.PluginSettings.Settings.RarityUltraRareColor;


                switch (PluginDatabase.PluginSettings.Settings.NameSorting)
                {
                    case "Name":
                        cbDefaultSorting.Text = ResourceProvider.GetString("LOCGameNameTitle");
                        break;
                    case "LastActivity":
                        cbDefaultSorting.Text = ResourceProvider.GetString("LOCLastPlayed");
                        break;
                    case "SourceName":
                        cbDefaultSorting.Text = ResourceProvider.GetString("LOCSourceLabel");
                        break;
                    case "ProgressionValue":
                        cbDefaultSorting.Text = ResourceProvider.GetString("LOCSuccessStorylvGamesProgression");
                        break;
                    default:
                        break;
                }


                _ = Task.Run(() => CheckLogged())
                    .ContinueWith(antecedent =>
                    {
                        Dispatcher.Invoke(new Action(() =>
                        {
                            lIsAuth.Content = antecedent.Result ? ResourceProvider.GetString("LOCCommonLoggedIn") : ResourceProvider.GetString("LOCCommonNotLoggedIn");
                        }));
                    });


                LocalPath = Serialization.GetClone(PluginDatabase.PluginSettings.Settings.LocalPath);
                PART_ItemsControl.ItemsSource = LocalPath;

                Rpcs3Path = Serialization.GetClone(PluginDatabase.PluginSettings.Settings.Rpcs3InstallationFolders);
                PART_ItemsRpcs3Folder.ItemsSource = Rpcs3Path;
                if (Rpcs3Path.Count > 0)
                {
                    PART_ItemsRpcs3Folder.Visibility = Visibility.Visible;
                }

				ShadPS4Path = Serialization.GetClone(PluginDatabase.PluginSettings.Settings.ShadPS4InstallationFolders);
				PART_ItemsShadPS4Folder.ItemsSource = ShadPS4Path;
				if (ShadPS4Path.Count > 0)
				{
					PART_ItemsShadPS4Folder.Visibility = Visibility.Visible;
				}


				// Set ignored game
				IgnoredGames = Serialization.GetClone(PluginDatabase.Database.Where(x => x.IsIgnored).ToList());
                IgnoredGames.Sort((x, y) => x.Name.CompareTo(y.Name));
                PART_IgnoredGames.ItemsSource = IgnoredGames;


                // List features
                PART_FeatureAchievement.ItemsSource = API.Instance.Database.Features.OrderBy(x => x.Name);


                // List completation
                PART_CbCompletation.ItemsSource = API.Instance.Database.CompletionStatuses.ToList();
                PART_CbCompletation.SelectedIndex = API.Instance.Database.CompletionStatuses.ToList()
                    .FindIndex(x => x.Id == PluginDatabase.PluginSettings.Settings.CompletionStatus100Percent?.Id);


                // Set RA console list
                PluginDatabase.PluginSettings.Settings.RaConsoleAssociateds.ForEach(x =>
                {
                    x.GetSelectable();
                });
                PART_LbRaConsole.ItemsSource = PluginDatabase.PluginSettings.Settings.RaConsoleAssociateds;
                RaConsoleAssociateds = PluginDatabase.PluginSettings.Settings.RaConsoleAssociateds;


                SteamPanel.Visibility = PluginDatabase.PluginSettings.Settings.PluginState.SteamIsEnabled ? Visibility.Visible : Visibility.Collapsed;
                EpicPanel.Visibility = PluginDatabase.PluginSettings.Settings.PluginState.EpicIsEnabled ? Visibility.Visible : Visibility.Collapsed;
                GogPanel.Visibility = PluginDatabase.PluginSettings.Settings.PluginState.GogIsEnabled ? Visibility.Visible : Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }
        }


        private void cbDefaultSorting_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            PluginDatabase.PluginSettings.Settings.NameSorting = ((ComboBoxItem)cbDefaultSorting.SelectedItem).Tag.ToString();
        }


        private void ButtonSelectFolder_Click(object sender, RoutedEventArgs e)
        {
            string SelectedFolder = API.Instance.Dialogs.SelectFolder();
            if (!SelectedFolder.IsNullOrEmpty())
            {
                PART_Rpcs3Folder.Text = SelectedFolder;
                PluginDatabase.PluginSettings.Settings.Rpcs3InstallationFolder = SelectedFolder;
            }
        }


        private void Button_Click_Wiki(object sender, RoutedEventArgs e)
        {
            _ = Process.Start((string)((FrameworkElement)sender).Tag);
        }


        #region Tag

        private void ButtonAddTag_Click(object sender, RoutedEventArgs e)
        {
            PluginDatabase.AddTagAllGame();
        }

        private void ButtonRemoveTag_Click(object sender, RoutedEventArgs e)
        {
            PluginDatabase.RemoveTagAllGame();
        }

        #endregion

        #region Exophase

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            lIsAuth.Content = ResourceProvider.GetString("LOCCommonLoginChecking");

            try
            {
                SuccessStory.ExophaseAchievements.ResetCachedIsConnectedResult();
                await SuccessStory.ExophaseAchievements.Login();

                Task task = Task.Run(() => CheckLogged())
                    .ContinueWith(antecedent =>
                    {
                        this.Dispatcher.Invoke(new Action(() =>
                        {
                            lIsAuth.Content = antecedent.Result ? ResourceProvider.GetString("LOCCommonLoggedIn") : ResourceProvider.GetString("LOCCommonNotLoggedIn");
                        }));
                    });
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to authenticate user.");
            }
        }

        private bool CheckLogged()
        {
            return SuccessStory.ExophaseAchievements.IsConnected();
        }

        #endregion

        #region Local

        private void ButtonAddLocalFolder_Click(object sender, RoutedEventArgs e)
        {
            PART_ItemsControl.ItemsSource = null;
            LocalPath.Add(new Folder { FolderPath = "" });
            PART_ItemsControl.ItemsSource = LocalPath;
        }

		private void ButtonSelectLocalFolder_Click(object sender, RoutedEventArgs e)
		{
			int indexFolder = int.Parse(((Button)sender).Tag.ToString());

			string selectedFolder = API.Instance.Dialogs.SelectFolder();
			if (!selectedFolder.IsNullOrEmpty())
			{
				if (IsValidAchievementFolder(selectedFolder))
				{
					PART_ItemsControl.ItemsSource = null;
					LocalPath[indexFolder].FolderPath = selectedFolder;
					PART_ItemsControl.ItemsSource = LocalPath;
				}
				else
				{
					API.Instance.Dialogs.ShowMessage(
						"Select a folder which contains SteamAppID folder(s)\nFor Ali213 and Valve select a folder where the ALI213.ini or valve.ini file is located",
						"Invalid Achievement Folder",
						MessageBoxButton.OK,
						MessageBoxImage.Warning);
				}
			}
		}

		private void DefaultDirs_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (sender is ComboBox comboBox && comboBox.SelectedItem is ComboBoxItem selectedItem)
			{
				string selectedPath = selectedItem.Content.ToString();

				// Resolve environment variables in the path
				selectedPath = Environment.ExpandEnvironmentVariables(selectedPath);

				if (IsValidAchievementFolder(selectedPath))
				{
					PART_ItemsControl.ItemsSource = null;
					LocalPath.Add(new Folder { FolderPath = selectedPath });
					PART_ItemsControl.ItemsSource = LocalPath;
				}
				else
				{
					API.Instance.Dialogs.ShowMessage(
						"The selected default directory does not contain the required files or folders.\nPlease verify the path or select a different directory.",
						"Invalid Achievement Folder",
						MessageBoxButton.OK,
						MessageBoxImage.Warning);

					comboBox.SelectedIndex = -1; // Clear selection
				}
			}
		}

		private void ButtonRemoveLocalFolder_Click(object sender, RoutedEventArgs e)
        {
            int indexFolder = int.Parse(((Button)sender).Tag.ToString());

            PART_ItemsControl.ItemsSource = null;
            LocalPath.RemoveAt(indexFolder);
            PART_ItemsControl.ItemsSource = LocalPath;
		}

		private bool IsValidAchievementFolder(string folderPath)
		{
			if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
				return false;

			// Check for SteamAppId subfolders
			bool hasSteamAppIdFolders = Directory.GetDirectories(folderPath)
				.Any(dir => int.TryParse(Path.GetFileName(dir), out _));

			// Check for ALI213.ini
			bool hasAli213File = File.Exists(Path.Combine(folderPath, "ALI213.ini"));

			// Check for valve.ini
			bool hasValveFile = File.Exists(Path.Combine(folderPath, "valve.ini"));

			return hasSteamAppIdFolders || hasAli213File || hasValveFile;
		}

		#endregion

		#region Rarity configuration

		private void BtPickColor_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                TbControl = ((StackPanel)((FrameworkElement)sender).Parent).Children.OfType<TextBlock>().FirstOrDefault();
                
                if (TbControl.Background is SolidColorBrush)
                {
                    Color color = ((SolidColorBrush)TbControl.Background).Color;
                    PART_SelectorColorPicker.SetColors(color);
                }
                
                PART_SelectorColor.Visibility = Visibility.Visible;
                PART_MiscTab.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }
        }

        private void BtRestore_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                TextBlock tbControl = ((StackPanel)((FrameworkElement)sender).Parent).Children.OfType<TextBlock>().FirstOrDefault();

                switch ((string)((Button)sender).Tag)
                {
                    case "1":
                        tbControl.Background = Brushes.DarkGray;
                        RarityUncommonColor = Brushes.DarkGray;
                        break;

                    case "2":
                        tbControl.Background = Brushes.Gold;
                        RarityRareColor = Brushes.Gold;
                        break;

                    case "3":
                        tbControl.Background = Brushes.MediumPurple;
                        RarityUltraRareColor = Brushes.MediumPurple;
                        break;
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }
        }

        private void PART_TM_ColorOK_Click(object sender, RoutedEventArgs e)
        {
            Color color = default(Color);

            if (TbControl != null)
            {
                if (PART_SelectorColorPicker.IsSimpleColor)
                {
                    color = PART_SelectorColorPicker.SimpleColor;
                    TbControl.Background = new SolidColorBrush(color);

                    switch ((string)TbControl.Tag)
                    {
                        case "1":
                            RarityUncommonColor = new SolidColorBrush(color);
                            break;

                        case "2":
                            RarityRareColor = new SolidColorBrush(color);
                            break;

                        case "3":
                            RarityUltraRareColor = new SolidColorBrush(color);
                            break;
                    }
                }
            }
            else
            {
                Logger.Warn("One control is undefined");
            }

            PART_SelectorColor.Visibility = Visibility.Collapsed;
            PART_MiscTab.Visibility = Visibility.Visible;
        }

        private void PART_TM_ColorCancel_Click(object sender, RoutedEventArgs e)
        {
            PART_SelectorColor.Visibility = Visibility.Collapsed;
            PART_MiscTab.Visibility = Visibility.Visible;
        }


        private void PART_SlidderRare_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            PART_SlidderUncommun.Minimum = PART_SlidderRare.Value;
        }

        private void PART_SlidderUltraRare_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            PART_SlidderRare.Minimum = PART_SlidderUltraRare.Value;
        }

        #endregion


        private void Button_Click_Remove(object sender, RoutedEventArgs e)
        {
            int index = int.Parse(((Button)sender).Tag.ToString());
            GameAchievements gameAchievements = IgnoredGames[index];
            PluginDatabase.SetIgnored(gameAchievements);
            IgnoredGames.RemoveAt(index);

            PART_IgnoredGames.ItemsSource = null;
            PART_IgnoredGames.ItemsSource = IgnoredGames;
        }

        private void PART_CbCompletation_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CompletionStatus = (CompletionStatus)PART_CbCompletation.SelectedItem;
        }

        private void Button_RefreshWowRealm(object sender, RoutedEventArgs e)
        {
            PluginDatabase.PluginSettings.Settings.WowRealms = WowAchievements.GetRealm(PART_WowRegion.Text);
            PART_WowRealm.Text = string.Empty;
        }


        #region Unlocked icon configuration

        private void PART_RemoveCustomIcon_Click(object sender, RoutedEventArgs e)
        {
            PART_IconUnlocked.Source = null;
            ((SuccessStorySettingsViewModel)this.DataContext).Settings.IconCustomLocked = string.Empty;
        }

        private void PART_AddCustomIcon_Click(object sender, RoutedEventArgs e)
        {
            var result = API.Instance.Dialogs.SelectImagefile();
            if (!result.IsNullOrEmpty())
            {
                try
                {
                    File.Copy(result, Path.Combine(PluginDatabase.Paths.PluginUserDataPath, Path.GetFileName(result)), true);
                    PART_IconUnlocked.Source = ImageSourceManagerPlugin.GetImage(Path.Combine(PluginDatabase.Paths.PluginUserDataPath, Path.GetFileName(result)), false);
                    ((SuccessStorySettingsViewModel)this.DataContext).Settings.IconCustomLocked = Path.Combine(PluginDatabase.Paths.PluginUserDataPath, Path.GetFileName(result));
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false);
                }
            }
        }

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            Hyperlink link = (Hyperlink)sender;
            Process.Start((string)link.Tag);
        }

        #endregion

        #region RPCS3 folders

        private void ButtonAddRpcs3Folder_Click(object sender, RoutedEventArgs e)
        {
            PART_ItemsRpcs3Folder.Visibility = Visibility.Visible;
            PART_ItemsRpcs3Folder.ItemsSource = null;
            Rpcs3Path.Add(new Folder { FolderPath = "" });
            PART_ItemsRpcs3Folder.ItemsSource = Rpcs3Path;
        }

        private void ButtonSelectRpcs3Folder_Click(object sender, RoutedEventArgs e)
        {
            int indexFolder = int.Parse(((Button)sender).Tag.ToString());

            string SelectedFolder = API.Instance.Dialogs.SelectFolder();
            if (!SelectedFolder.IsNullOrEmpty())
            {
                PART_ItemsRpcs3Folder.ItemsSource = null;
                Rpcs3Path[indexFolder].FolderPath = SelectedFolder;
                PART_ItemsRpcs3Folder.ItemsSource = Rpcs3Path;
            }
        }

        private void ButtonRemoveRpcs3Folder_Click(object sender, RoutedEventArgs e)
        {
            int indexFolder = int.Parse(((Button)sender).Tag.ToString());

            PART_ItemsRpcs3Folder.ItemsSource = null;
            Rpcs3Path.RemoveAt(indexFolder);
            PART_ItemsRpcs3Folder.ItemsSource = Rpcs3Path;

            if (Rpcs3Path.Count == 0)
            {
                PART_ItemsRpcs3Folder.Visibility = Visibility.Collapsed;
            }
        }

		#endregion

		#region ShadPS4 folders

		private void ButtonAddShadPS4Folder_Click(object sender, RoutedEventArgs e)
		{
			PART_ItemsShadPS4Folder.Visibility = Visibility.Visible;
			PART_ItemsShadPS4Folder.ItemsSource = null;
			ShadPS4Path.Add(new Folder { FolderPath = "" });
			PART_ItemsShadPS4Folder.ItemsSource = ShadPS4Path;
		}

		private void ButtonSelectShadPS4Folder_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				string selectedFolder = API.Instance.Dialogs.SelectFolder();
				if (!selectedFolder.IsNullOrEmpty())
				{
					if (Directory.Exists(selectedFolder))
					{
						// Look for ShadPS4's specific path structure
                        string validGameDataPath = ShadPS4Achievements.GetGameDataPath(selectedFolder);

						if (string.IsNullOrEmpty(validGameDataPath))
						{
							Logger.Warn($"No valid ShadPS4 game_data folder found in {selectedFolder}");
							API.Instance.Dialogs.ShowMessage(
								"Selected folder must be the ShadPS4 installation directory containing 'user/game_data', 'launcher/game_data' or 'shadPS4/game_data' path",
								"Invalid Folder",
								MessageBoxButton.OK,
								MessageBoxImage.Warning
							);
							return;
						}

						// Verify we can find at least one game with trophy data
						bool hasTrophyData = Directory.GetDirectories(validGameDataPath)
							.Any(titleDir => Directory.Exists(Path.Combine(titleDir, "trophyfiles")));

						if (!hasTrophyData)
						{
							Logger.Warn($"No trophy data found in any game folder in {selectedFolder} ({validGameDataPath})");
							API.Instance.Dialogs.ShowMessage(
								"No trophy data found in the selected folder. Make sure games with trophies have been run at least once.",
								"No Trophy Data",
								MessageBoxButton.OK,
								MessageBoxImage.Warning
							);
							return;
						}

						PART_ShadPS4Folder.Text = selectedFolder;
						PluginDatabase.PluginSettings.Settings.ShadPS4InstallationFolder = selectedFolder;
					}
				}
			}
			catch (Exception ex)
			{
				Common.LogError(ex, false, true, PluginDatabase.PluginName);
			}
		}

		private void ButtonRemoveShadPS4Folder_Click(object sender, RoutedEventArgs e)
		{
			int indexFolder = int.Parse(((Button)sender).Tag.ToString());

			PART_ItemsShadPS4Folder.ItemsSource = null;
			ShadPS4Path.RemoveAt(indexFolder);
			PART_ItemsShadPS4Folder.ItemsSource = ShadPS4Path;

			if (ShadPS4Path.Count == 0)
			{
				PART_ItemsShadPS4Folder.Visibility = Visibility.Collapsed;
			}
		}

		#endregion

		#region Xenia folders

		private void ButtonAddXeniaFolder_Click(object sender, RoutedEventArgs e)
		{
			string selectedFolder = API.Instance.Dialogs.SelectFolder();
			if (!selectedFolder.IsNullOrEmpty())
			{
				string xeniaExe = Path.Combine(selectedFolder, "xenia_canary.exe");
				if (File.Exists(xeniaExe))
				{
					selectedFolder = selectedFolder.TrimEnd('\\') + '\\'; // Add trailing slash
					PART_XeniaFolder.Text = selectedFolder;
					PluginDatabase.PluginSettings.Settings.XeniaInstallationFolder = selectedFolder;
					// Initialize Xbox360 achievements environment
					try
					{
						var xbox360Achievements = new Xbox360Achievements(API.Instance, PluginDatabase.PluginSettings.Settings.XeniaInstallationFolder);
						xbox360Achievements.InitializeXeniaEnvironment(xeniaExe);
					}
					catch (Exception ex)
					{
						Common.LogError(ex, false, true, PluginDatabase.PluginName);
						API.Instance.Dialogs.ShowErrorMessage(ResourceProvider.GetString("LOCSuccessStoryXeniaInitError"), "Success Story");
					}
				}
				else
				{
					API.Instance.Dialogs.ShowMessage(
						ResourceProvider.GetString("LOCSuccessStoryXeniaExeNotFound"),
						"Success Story",
						MessageBoxButton.OK,
						MessageBoxImage.Warning);
				}
			}
		}

		private void ButtonSelectXeniaFolder_Click(object sender, RoutedEventArgs e)
		{
			var dialog = new Microsoft.Win32.OpenFileDialog
			{
				Filter = "Xenia Canary|xenia_canary.exe",
				Title = ResourceProvider.GetString("LOCSuccessStorySelectXeniaExe")
			};
			if (dialog.ShowDialog() == true)
			{
				string xeniaFolder = Path.GetDirectoryName(dialog.FileName);
				xeniaFolder = xeniaFolder.TrimEnd('\\') + '\\'; // Add trailing slash
				PART_XeniaFolder.Text = xeniaFolder;
				PluginDatabase.PluginSettings.Settings.XeniaInstallationFolder = xeniaFolder;
				// Initialize Xbox360 achievements environment
				try
				{
					var xbox360Achievements = new Xbox360Achievements(API.Instance, PluginDatabase.PluginSettings.Settings.XeniaInstallationFolder);
					xbox360Achievements.InitializeXeniaEnvironment(dialog.FileName);
				}
				catch (Exception ex)
				{
					Common.LogError(ex, false, true, PluginDatabase.PluginName);
					API.Instance.Dialogs.ShowErrorMessage(ResourceProvider.GetString("LOCSuccessStoryXeniaInitError"), "Success Story");
				}
			}
		}

		#endregion
	}


	public class BooleanAndConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            foreach (object value in values)
            {
                if ((value is bool bVal) && bVal == false)
                {
                    return false;
                }
            }
            return true;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException("BooleanAndConverter is a OneWay converter.");
        }
    }
}