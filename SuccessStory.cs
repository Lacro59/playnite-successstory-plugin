using Newtonsoft.Json;
using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using PluginCommon;
using SuccessStory.Database;
using SuccessStory.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace SuccessStory
{
    public class SuccessStory : Plugin
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private SuccessStorySettings settings { get; set; }

        public override Guid Id { get; } = Guid.Parse("cebe6d32-8c46-4459-b993-5a5189d60788");


        public SuccessStory(IPlayniteAPI api) : base(api)
        {
            settings = new SuccessStorySettings(this);

            // Get plugin's location 
            string pluginFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            // Add plugin localization in application ressource.
            PluginCommon.Localization.SetPluginLanguage(pluginFolder, api.Paths.ConfigurationPath);
        }

        public override IEnumerable<ExtensionFunction> GetFunctions()
        {
            return new List<ExtensionFunction>
            {
                new ExtensionFunction(
                    "Success Story",
                    () =>
                    {
                        // Add code to be execute when user invokes this menu entry.

                        logger.Info("SuccessStory - SuccessStoryView");

                        // Show SuccessView
                        new SuccessView(settings, PlayniteApi, this.GetPluginUserDataPath()).ShowDialog();
                    })
            };
        }

        public override void OnGameInstalled(Game game)
        {
            // Add code to be executed when game is finished installing.
        }

        public override void OnGameStarted(Game game)
        {
            // Add code to be executed when game is started running.
        }

        public override void OnGameStarting(Game game)
        {
            // Add code to be executed when game is preparing to be started.
        }

        public override void OnGameStopped(Game game, long elapsedSeconds)
        {
            // Add code to be executed when game is preparing to be started.

            // Refresh Achievements database for game played.
            AchievementsDatabase AchievementsDatabase = new AchievementsDatabase(PlayniteApi, this.GetPluginUserDataPath());
            AchievementsDatabase.Remove(game);
            AchievementsDatabase.Add(game, settings);
        }

        public override void OnGameUninstalled(Game game)
        {
            // Add code to be executed when game is uninstalled.
        }

        public override void OnApplicationStarted()
        {
            // Add code to be executed when Playnite is initialized.
        }

        public override void OnApplicationStopped()
        {
            // Add code to be executed when Playnite is shutting down.
        }

        public override void OnLibraryUpdated()
        {
            // Add code to be executed when library is updated.

            // Get achievements for the new game added int he library.
            AchievementsDatabase AchievementsDatabase = new AchievementsDatabase(PlayniteApi, this.GetPluginUserDataPath());
            foreach (var game in PlayniteApi.Database.Games)
            {
                if (game.Added == null && ((DateTime)game.Added).ToString("yyyy-MM-dd") == DateTime.Now.ToString("yyyy-MM-dd"))
                {
                    AchievementsDatabase.Remove(game);
                    AchievementsDatabase.Add(game, settings);
                }
            }
        }

        public override void OnGameSelected(GameSelectionEventArgs args)
        {
            if (args.NewValue != null)
            {
                if (args.NewValue.Count == 1)
                {
                    foreach (ListBox lb in FindVisualChildren<ListBox>(Application.Current.MainWindow))
                    {
                        if (lb.Name == "PART_lbAchievements")
                        {
                            List<listAchievements> ListBoxAchievements = new List<listAchievements>();
                            lb.ItemsSource = ListBoxAchievements;

                            try
                            {
                                Game SelectedGame = args.NewValue[0];

                                AchievementsDatabase AchievementsDatabase = new AchievementsDatabase(PlayniteApi, this.GetPluginUserDataPath());
                                AchievementsDatabase.Initialize();
                                GameAchievements SelectedGameAchievements = AchievementsDatabase.Get(SelectedGame.Id);

                                if (SelectedGameAchievements.HaveAchivements)
                                {
                                    List<Achievements> ListAchievements = SelectedGameAchievements.Achievements;



                                    for (int i = 0; i < ListAchievements.Count; i++)
                                    {
                                        DateTime? dateUnlock;
                                        BitmapImage iconImage = new BitmapImage();
                                        FormatConvertedBitmap ConvertBitmapSource = new FormatConvertedBitmap();

                                        bool isGray = false;

                                        iconImage.BeginInit();
                                        if (ListAchievements[i].DateUnlocked == default(DateTime) || ListAchievements[i].DateUnlocked == null)
                                        {
                                            dateUnlock = null;
                                            if (ListAchievements[i].UrlLocked == "")
                                            {
                                                iconImage.UriSource = new Uri(ListAchievements[i].UrlUnlocked, UriKind.RelativeOrAbsolute);
                                                isGray = true;
                                            }
                                            else
                                            {
                                                iconImage.UriSource = new Uri(ListAchievements[i].UrlLocked, UriKind.RelativeOrAbsolute);
                                            }
                                        }
                                        else
                                        {
                                            iconImage.UriSource = new Uri(ListAchievements[i].UrlUnlocked, UriKind.RelativeOrAbsolute);
                                            dateUnlock = ListAchievements[i].DateUnlocked;
                                        }
                                        iconImage.EndInit();

                                        ConvertBitmapSource.BeginInit();
                                        ConvertBitmapSource.Source = iconImage;
                                        if (isGray)
                                        {
                                            ConvertBitmapSource.DestinationFormat = PixelFormats.Gray32Float;
                                        }
                                        ConvertBitmapSource.EndInit();

                                        string NameAchievement = ListAchievements[i].Name;
                                        //if (NameAchievement.Length > 35)
                                        //{
                                        //    NameAchievement = NameAchievement.Substring(0, 35).Trim() + "...";
                                        //}

                                        ListBoxAchievements.Add(new listAchievements()
                                        {
                                            Name = NameAchievement,
                                            NameToolTip = ListAchievements[i].Name,
                                            IsTrimmed = (NameAchievement != ListAchievements[i].Name),
                                            DateUnlock = dateUnlock,
                                            Icon = ConvertBitmapSource,
                                            Description = ListAchievements[i].Description
                                        });

                                        iconImage = null;
                                    }


                                    // Sorting default.
                                    lb.ItemsSource = ListBoxAchievements;
                                    CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(lb.ItemsSource);
                                    view.SortDescriptions.Add(new SortDescription("DateUnlock", ListSortDirection.Descending));
                                    lb.UpdateLayout();
                                }

                                AchievementsDatabase = null;
                            }
                            catch
                            {

                            }
                            //logger.Debug("test: " + SelectedGame.Name + " - " + SelectedGameAchievements.Total);

                            //Label countAchievements = new Label();
                            //countAchievements.Content = SelectedGame.Name + " - " + SelectedGameAchievements.Total;
                            //tb.Children.Clear();
                            //tb.Children.Add(countAchievements);
                            //tb.UpdateLayout();
                        }
                    }
                }
            }
        }

        public static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T)
                    {
                        yield return (T)child;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }


        public override ISettings GetSettings(bool firstRunSettings)
        {
            return settings;
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new SuccessStorySettingsView(PlayniteApi, this.GetPluginUserDataPath(), settings);
        }
    }
}