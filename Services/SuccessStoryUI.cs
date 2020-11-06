using Newtonsoft.Json;
using Playnite.SDK;
using Playnite.SDK.Models;
using PluginCommon;
using SuccessStory.Views.Interface;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace SuccessStory.Services
{
    public class SuccessStoryUI : PlayniteUiHelper
    {
        private readonly SuccessStorySettings _Settings;
        private readonly SuccessStory _Plugin;

        public override string _PluginUserDataPath { get; set; } = string.Empty;

        public override bool IsFirstLoad { get; set; } = true;

        public override string BtActionBarName { get; set; } = string.Empty;
        public override FrameworkElement PART_BtActionBar { get; set; }

        public override string SpDescriptionName { get; set; } = string.Empty;
        public override FrameworkElement PART_SpDescription { get; set; }

        public override List<CustomElement> ListCustomElements { get; set; } = new List<CustomElement>();


        public SuccessStoryUI(IPlayniteAPI PlayniteApi, SuccessStorySettings Settings, string PluginUserDataPath, SuccessStory Plugin) : base(PlayniteApi, PluginUserDataPath)
        {
            _Settings = Settings;
            _Plugin = Plugin;
            _PluginUserDataPath = PluginUserDataPath;

            BtActionBarName = "PART_ScButton";
            SpDescriptionName = "PART_ScDescriptionIntegration";
        }


        #region BtHeader
        public void AddBtHeader()
        {
            if (_Settings.EnableIntegrationButtonHeader)
            {
                logger.Info("SuccessStory - Add Header button");
                Button btHeader = new SuccessStoryButtonHeader(TransformIcon.Get("SuccessStory"));
                btHeader.SetValue(TextBlock.FontWeightProperty, FontWeights.Bold);
                btHeader.Click += OnBtHeaderClick;
                ui.AddButtonInWindowsHeader(btHeader);
            }
        }

        public void OnBtHeaderClick(object sender, RoutedEventArgs e)
        {
#if DEBUG
            logger.Debug($"SuccessStory - OnBtHeaderClick()");
#endif
            var ViewExtension = new SuccessView(_Plugin, _Settings, _PlayniteApi, _Plugin.GetPluginUserDataPath());
            Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(_PlayniteApi, resources.GetString("LOCSuccessStory"), ViewExtension);
            windowExtension.ShowDialog();

            /*
            SuccessView ViewExtension = null;
            if (settings.EnableRetroAchievementsView && PlayniteTools.IsGameEmulated(PlayniteApi, GameSelected))
            {
                ViewExtension = new SuccessView(this, settings, PlayniteApi, this.GetPluginUserDataPath(), true);
            }
            else
            {
                ViewExtension = new SuccessView(this, settings, PlayniteApi, this.GetPluginUserDataPath(), false);
            }
            Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(PlayniteApi, resources.GetString("LOCSuccessStory"), ViewExtension);
            windowExtension.ShowDialog();
            */
        }
        #endregion


        public override void Initial()
        {
            if (_Settings.EnableIntegrationButton)
            {
#if DEBUG
                logger.Debug($"SuccessStory - InitialBtActionBar()");
#endif
                InitialBtActionBar();
            }

            if (_Settings.EnableIntegrationInDescription)
            {
#if DEBUG
                logger.Debug($"SuccessStory - InitialSpDescription()");
#endif
                InitialSpDescription();
            }

            if (_Settings.EnableIntegrationInCustomTheme)
            {
#if DEBUG
                logger.Debug($"SuccessStory - InitialCustomElements()");
#endif
                InitialCustomElements();
            }
        }

        public override void AddElements()
        {
            if (IsFirstLoad)
            {
#if DEBUG
                logger.Debug($"SuccessStory - IsFirstLoad");
#endif
                Thread.Sleep(1000);
                IsFirstLoad = false;
            }

            Application.Current.Dispatcher.BeginInvoke((Action)delegate
            {
                CheckTypeView();

                if (_Settings.EnableIntegrationButton)
                {
#if DEBUG
                    logger.Debug($"SuccessStory - AddBtActionBar()");
#endif
                    AddBtActionBar();
                }

                if (_Settings.EnableIntegrationInDescription)
                {
#if DEBUG
                    logger.Debug($"SuccessStory - AddSpDescription()");
#endif
                    AddSpDescription();
                }

                if (_Settings.EnableIntegrationInCustomTheme)
                {
#if DEBUG
                    logger.Debug($"SuccessStory - AddCustomElements()");
#endif
                    AddCustomElements();
                }
            });
        }

        public override void RefreshElements(Game GameSelected, bool force = false)
        {
#if DEBUG
            logger.Debug($"SuccessStory - RefreshElements({GameSelected.Name})");
#endif
            
            CancellationTokenSource tokenSource = new CancellationTokenSource();
            CancellationToken ct = tokenSource.Token;

            Task TaskRefresh = Task.Run(() => 
            {
#if DEBUG
                string IsCanceld = string.Empty;

                logger.Debug($"SuccessStory - TaskRefresh() - Start");
                Stopwatch stopwatch = new Stopwatch();
                TimeSpan ts;
                stopwatch.Start();
#endif
                try
                {
                    Initial();

                    // Reset resources
                    List<ResourcesList> resourcesLists = new List<ResourcesList>();
                    resourcesLists.Add(new ResourcesList { Key = "Sc_HasData", Value = false });
                    resourcesLists.Add(new ResourcesList { Key = "Sc_Is100Percent", Value = false });
                    resourcesLists.Add(new ResourcesList { Key = "Sc_Total", Value = 0 });
                    resourcesLists.Add(new ResourcesList { Key = "Sc_TotalDouble", Value = (double)0 });
                    resourcesLists.Add(new ResourcesList { Key = "Sc_TotalString", Value = "0" });
                    resourcesLists.Add(new ResourcesList { Key = "Sc_Unlocked", Value = 0 });
                    resourcesLists.Add(new ResourcesList { Key = "Sc_UnlockedDouble", Value = (double)0 });
                    resourcesLists.Add(new ResourcesList { Key = "Sc_UnlockedString", Value = "0" });
                    resourcesLists.Add(new ResourcesList { Key = "Sc_Locked", Value = 0 });
                    resourcesLists.Add(new ResourcesList { Key = "Sc_LockedDouble", Value = (double)0 });
                    resourcesLists.Add(new ResourcesList { Key = "Sc_LockedString", Value = "0" });

                    resourcesLists.Add(new ResourcesList { Key = "Sc_EnableIntegrationInCustomTheme", Value = _Settings.EnableIntegrationInCustomTheme });
                    resourcesLists.Add(new ResourcesList { Key = "Sc_IntegrationShowGraphic", Value = _Settings.IntegrationShowGraphic });
                    resourcesLists.Add(new ResourcesList { Key = "Sc_IntegrationShowAchievements", Value = _Settings.IntegrationShowAchievements });
                    resourcesLists.Add(new ResourcesList { Key = "Sc_IntegrationShowProgressBar", Value = _Settings.IntegrationShowProgressBar });
                    resourcesLists.Add(new ResourcesList { Key = "Sc_IntegrationShowAchievementsCompactLocked", Value = _Settings.IntegrationShowAchievementsCompactLocked });
                    resourcesLists.Add(new ResourcesList { Key = "Sc_IntegrationShowAchievementsCompactUnlocked", Value = _Settings.IntegrationShowAchievementsCompactUnlocked });
                    ui.AddResources(resourcesLists);


                    // Load data
                    SuccessStory.SelectedGameAchievements = null;
                    string GameSourceName = string.Empty;

                    try
                    {
                        SuccessStory.SelectedGameAchievements = SuccessStory.achievementsDatabase.Get(GameSelected.Id);
                        GameSourceName = PlayniteTools.GetSourceName(GameSelected, _PlayniteApi);
                    }
                    catch (Exception ex)
                    {
                        Common.LogError(ex, "SuccessStory", "Error to load data");
                        _PlayniteApi.Dialogs.ShowErrorMessage(resources.GetString("LOCDatabaseErroTitle"), "SuccessStory");
                    }

                    // Download Achievements if not exist in database.
                    if (SuccessStory.SelectedGameAchievements == null)
                    {
                        logger.Info($"SuccessStory - Download achievements for {GameSelected.Name} - {GameSourceName}");
                        SuccessStory.achievementsDatabase.Add(GameSelected, _Settings);
                        SuccessStory.achievementsDatabase.Initialize();
                        SuccessStory.SelectedGameAchievements = SuccessStory.achievementsDatabase.Get(GameSelected.Id);
                    }

                    if (SuccessStory.SelectedGameAchievements == null)
                    {
                        logger.Warn("SuccessStory - No data for " + GameSelected.Name);
#if DEBUG
                        stopwatch.Stop();
                        ts = stopwatch.Elapsed;
                        logger.Debug($"SuccessStory - TaskRefresh(){IsCanceld} - End - {String.Format("{0:00}:{1:00}.{2:00}", ts.Minutes, ts.Seconds, ts.Milliseconds / 10)}");
#endif
                        return;
                    }

                    if (!SuccessStory.SelectedGameAchievements.HaveAchivements)
                    {
                        logger.Warn("SuccessStory - No achievements for " + GameSelected.Name);
#if DEBUG
                        stopwatch.Stop();
                        ts = stopwatch.Elapsed;
                        logger.Debug($"SuccessStory - TaskRefresh(){IsCanceld} - End - {String.Format("{0:00}:{1:00}.{2:00}", ts.Minutes, ts.Seconds, ts.Milliseconds / 10)}");
#endif
                        return;
                    }

#if DEBUG
                    logger.Debug($"SuccessStory - SuccessStory.SelectedGameAchievements: ({JsonConvert.SerializeObject(SuccessStory.SelectedGameAchievements)})");
#endif
                    resourcesLists.Add(new ResourcesList { Key = "Sc_HasData", Value = true });
                    resourcesLists.Add(new ResourcesList { Key = "Sc_Is100Percent", Value = SuccessStory.SelectedGameAchievements.Is100Percent });
                    resourcesLists.Add(new ResourcesList { Key = "Sc_Total", Value = SuccessStory.SelectedGameAchievements.Total });
                    resourcesLists.Add(new ResourcesList { Key = "Sc_TotalDouble", Value = double.Parse(SuccessStory.SelectedGameAchievements.Total.ToString()) });
                    resourcesLists.Add(new ResourcesList { Key = "Sc_TotalString", Value = SuccessStory.SelectedGameAchievements.Total.ToString() });
                    resourcesLists.Add(new ResourcesList { Key = "Sc_Unlocked", Value = SuccessStory.SelectedGameAchievements.Unlocked });
                    resourcesLists.Add(new ResourcesList { Key = "Sc_UnlockedDouble", Value = double.Parse(SuccessStory.SelectedGameAchievements.Unlocked.ToString()) });
                    resourcesLists.Add(new ResourcesList { Key = "Sc_UnlockedString", Value = SuccessStory.SelectedGameAchievements.Unlocked.ToString() });
                    resourcesLists.Add(new ResourcesList { Key = "Sc_Locked", Value = SuccessStory.SelectedGameAchievements.Locked });
                    resourcesLists.Add(new ResourcesList { Key = "Sc_LockedDouble", Value = double.Parse(SuccessStory.SelectedGameAchievements.Locked.ToString()) });
                    resourcesLists.Add(new ResourcesList { Key = "Sc_LockedString", Value = SuccessStory.SelectedGameAchievements.Locked.ToString() });


                    // If not cancel, show
                    if (!ct.IsCancellationRequested)
                    {
                        ui.AddResources(resourcesLists);

                        Application.Current.Dispatcher.BeginInvoke((Action)delegate
                        {
                            if (_Settings.EnableIntegrationButton)
                            {
#if DEBUG
                                logger.Debug($"SuccessStory - RefreshBtActionBar()");
#endif
                                RefreshBtActionBar();
                            }

                            if (_Settings.EnableIntegrationInDescription)
                            {
#if DEBUG
                                logger.Debug($"SuccessStory - RefreshSpDescription()");
#endif
                                RefreshSpDescription();
                            }

                            if (_Settings.EnableIntegrationInCustomTheme)
                            {
#if DEBUG
                                logger.Debug($"SuccessStory - RefreshCustomElements()");
#endif
                                RefreshCustomElements();
                            }
                        });
                    }
                    else
                    {
#if DEBUG
                        IsCanceld = " canceled";
#endif
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, "SuccessStory", $"Error on TaskRefreshBtActionBar()");
                }
#if DEBUG
                stopwatch.Stop();
                ts = stopwatch.Elapsed;
                logger.Debug($"SuccessStory - TaskRefresh(){IsCanceld} - End - {String.Format("{0:00}:{1:00}.{2:00}", ts.Minutes, ts.Seconds, ts.Milliseconds / 10)}");
#endif
            }, ct);

            taskHelper.Add(TaskRefresh, tokenSource);
        }


        #region BtActionBar
        public override void InitialBtActionBar()
        {
            Application.Current.Dispatcher.BeginInvoke((Action)delegate
            {
                if (PART_BtActionBar != null)
                {
#if DEBUG
                    logger.Debug($"SuccessStory - PART_BtActionBar {PART_BtActionBar.Name}");
#endif
                    PART_BtActionBar.Visibility = Visibility.Collapsed;
                }
            });
        }

        public override void AddBtActionBar()
        {
            if (PART_BtActionBar != null)
            {
#if DEBUG
                logger.Debug($"SuccessStory - PART_BtActionBar allready insert");
#endif
                return;
            }

            FrameworkElement BtActionBar;

            if (_Settings.EnableIntegrationInDescriptionWithToggle)
            {
                if (_Settings.EnableIntegrationButtonDetails)
                {
                    BtActionBar = new SuccessStoryToggleButtonDetails();
                }
                else
                {
                    BtActionBar = new SuccessStoryToggleButton(_Settings);
                }

                ((ToggleButton)BtActionBar).Click += OnBtActionBarToggleButtonClick;
            }
            else
            {
                if (_Settings.EnableIntegrationButtonDetails)
                {
                    BtActionBar = new SuccessStoryButtonDetails();
                }
                else
                {
                    BtActionBar = new SuccessStoryButton(_Settings.EnableIntegrationInDescriptionOnlyIcon);
                }

                ((Button)BtActionBar).Click += OnBtActionBarClick;
            }

            if (!_Settings.EnableIntegrationInDescriptionOnlyIcon)
            {
                BtActionBar.Width = 150;
            }

            BtActionBar.Name = BtActionBarName;
            BtActionBar.Margin = new Thickness(10, 0, 0, 0);

            try
            {
                ui.AddButtonInGameSelectedActionBarButtonOrToggleButton(BtActionBar);
                PART_BtActionBar = IntegrationUI.SearchElementByName(BtActionBarName);
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "SuccessStory", "Error on AddBtActionBar()");
            }
        }

        public override void RefreshBtActionBar()
        {
            if (PART_BtActionBar != null)
            {
                PART_BtActionBar.Visibility = Visibility.Visible;

                if (PART_BtActionBar != null && PART_BtActionBar is SuccessStoryButtonDetails)
                {
                    ((SuccessStoryButtonDetails)PART_BtActionBar).SetScData(SuccessStory.SelectedGameAchievements.Unlocked, SuccessStory.SelectedGameAchievements.Total);
                }
                if (PART_BtActionBar != null && PART_BtActionBar is SuccessStoryToggleButtonDetails)
                {
                    ((SuccessStoryToggleButtonDetails)PART_BtActionBar).SetScData(SuccessStory.SelectedGameAchievements.Unlocked, SuccessStory.SelectedGameAchievements.Total);
                }
            }
            else
            {
                logger.Warn($"SuccessStory - PART_BtActionBar is not defined");
            }
        }


        public void OnBtActionBarClick(object sender, RoutedEventArgs e)
        {
#if DEBUG
            logger.Debug($"SuccessStory - OnBtActionBarClick()");
#endif
            SuccessView ViewExtension = null;
            if (_Settings.EnableRetroAchievementsView && PlayniteTools.IsGameEmulated(_PlayniteApi, SuccessStory.GameSelected))
            {
                ViewExtension = new SuccessView(_Plugin, _Settings, _PlayniteApi, _Plugin.GetPluginUserDataPath(), true, SuccessStory.GameSelected);
            }
            else
            {
                ViewExtension = new SuccessView(_Plugin, _Settings, _PlayniteApi, _Plugin.GetPluginUserDataPath(), false, SuccessStory.GameSelected);
            }
            Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(_PlayniteApi, resources.GetString("LOCSuccessStory"), ViewExtension);
            windowExtension.ShowDialog();
        }

        public void OnCustomThemeButtonClick(object sender, RoutedEventArgs e)
        {
            if (_Settings.EnableIntegrationInCustomTheme)
            {
                string ButtonName = string.Empty;
                try
                {
                    ButtonName = ((Button)sender).Name;
                    if (ButtonName == "PART_ScCustomButton")
                    {
#if DEBUG
                        logger.Debug($"SuccessStory - OnCustomThemeButtonClick()");
#endif
                        OnBtActionBarClick(sender, e);
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, "SuccessStory", "OnCustomThemeButtonClick() error");
                }
            }
        }
        #endregion


        #region SpDescription
        public override void InitialSpDescription()
        {
            Application.Current.Dispatcher.BeginInvoke((Action)delegate
            {
                if (PART_SpDescription != null)
                {
                    PART_SpDescription.Visibility = Visibility.Collapsed;
                }
            });
        }

        public override void AddSpDescription()
        {
            if (PART_SpDescription != null)
            {
#if DEBUG
                logger.Debug($"SuccessStory - PART_SpDescription allready insert");
#endif
                return;
            }

            try
            {
                ScDescriptionIntegration SpDescription = new ScDescriptionIntegration(_Settings, SuccessStory.achievementsDatabase, SuccessStory.SelectedGameAchievements);
                SpDescription.Name = SpDescriptionName;

                ui.AddElementInGameSelectedDescription(SpDescription, _Settings.IntegrationTopGameDetails);
                PART_SpDescription = IntegrationUI.SearchElementByName(SpDescriptionName);

                if (_Settings.EnableIntegrationInDescriptionWithToggle && PART_SpDescription != null)
                {
                    ((ToggleButton)PART_BtActionBar).IsChecked = false;
                    PART_SpDescription.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "SuccessStory", "Error on AddSpDescription()");
            }
        }

        public override void RefreshSpDescription()
        {
            if (PART_SpDescription != null)
            {
                if (SuccessStory.SelectedGameAchievements != null)
                {
                    PART_SpDescription.Visibility = Visibility.Visible;

                    if (PART_SpDescription is ScDescriptionIntegration)
                    {
                        ((ScDescriptionIntegration)PART_SpDescription).SetScData(_Settings, SuccessStory.achievementsDatabase, SuccessStory.SelectedGameAchievements);

                        if (_Settings.EnableIntegrationInDescriptionWithToggle)
                        {
                            ((ToggleButton)PART_BtActionBar).IsChecked = false;
                            PART_SpDescription.Visibility = Visibility.Collapsed;
                        }
                    }
                }
                else
                {
#if DEBUG
                    logger.Debug($"SuccessStory - No data for {SuccessStory.GameSelected.Name}");
#endif
                }
            }
            else
            {
                logger.Warn($"SuccessStory - PART_SpDescription is not defined");
            }
        }
        #endregion


        #region CustomElements
        public override void InitialCustomElements()
        {
            Application.Current.Dispatcher.BeginInvoke((Action)delegate
            {
                foreach (CustomElement customElement in ListCustomElements)
                {
                    customElement.Element.Visibility = Visibility.Collapsed;
                }
            });
        }

        public override void AddCustomElements()
        {
            if (ListCustomElements.Count > 0)
            {
#if DEBUG
                logger.Debug($"SuccessStory - CustomElements allready insert - {ListCustomElements.Count}");
#endif
                return;
            }

            FrameworkElement PART_ScButtonWithJustIcon = null;
            FrameworkElement PART_ScButtonWithTitle = null;
            FrameworkElement PART_ScButtonWithTitleAndDetails = null;

            FrameworkElement PART_Achievements_ProgressBar = null;
            FrameworkElement PART_Achievements_Graphics = null;
            FrameworkElement PART_Achievements_List = null;
            FrameworkElement PART_Achievements_ListCompactUnlocked = null;
            FrameworkElement PART_Achievements_ListCompactLocked = null;
            try
            {
                PART_ScButtonWithJustIcon = IntegrationUI.SearchElementByName("PART_ScButtonWithJustIcon", false, true);
                PART_ScButtonWithTitle = IntegrationUI.SearchElementByName("PART_ScButtonWithTitle", false, true);
                PART_ScButtonWithTitleAndDetails = IntegrationUI.SearchElementByName("PART_ScButtonWithTitleAndDetails", false, true);

                PART_Achievements_ProgressBar = IntegrationUI.SearchElementByName("PART_Achievements_ProgressBar", false, true);
                PART_Achievements_Graphics = IntegrationUI.SearchElementByName("PART_Achievements_Graphics", false, true);
                PART_Achievements_List = IntegrationUI.SearchElementByName("PART_Achievements_List", false, true);
                PART_Achievements_ListCompactUnlocked = IntegrationUI.SearchElementByName("PART_Achievements_ListCompactUnlocked", false, true);
                PART_Achievements_ListCompactLocked = IntegrationUI.SearchElementByName("PART_Achievements_ListCompactLocked", false, true);
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "SuccessStory", $"Error on find custom element");
            }

            if (PART_ScButtonWithJustIcon != null)
            {
                PART_ScButtonWithJustIcon = new SuccessStoryButton(true);
                ((Button)PART_ScButtonWithJustIcon).Click += OnBtActionBarClick;
                try
                {
                    ui.AddElementInCustomTheme(PART_ScButtonWithJustIcon, "PART_ScButtonWithJustIcon");
                    ListCustomElements.Add(new CustomElement { ParentElementName = "PART_ScButtonWithJustIcon", Element = PART_ScButtonWithJustIcon });
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, "SuccessStory", "Error on AddCustomElements()");
                }
            }
            else
            {
#if DEBUG
                logger.Debug($"SuccessStory - PART_ScButtonWithJustIcon not find");
#endif
            }

            if (PART_ScButtonWithTitle != null)
            {
                PART_ScButtonWithTitle = new SuccessStoryButton(false);
                ((Button)PART_ScButtonWithTitle).Click += OnBtActionBarClick;
                try
                {
                    ui.AddElementInCustomTheme(PART_ScButtonWithTitle, "PART_ScButtonWithTitle");
                    ListCustomElements.Add(new CustomElement { ParentElementName = "PART_ScButtonWithTitle", Element = PART_ScButtonWithTitle });
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, "SuccessStory", "Error on AddCustomElements()");
                }
            }
            else
            {
#if DEBUG
                logger.Debug($"SuccessStory - PART_ScButtonWithTitle not find");
#endif
            }

            if (PART_ScButtonWithTitleAndDetails != null)
            {
                PART_ScButtonWithTitleAndDetails = new SuccessStoryButtonDetails();
                ((Button)PART_ScButtonWithTitleAndDetails).Click += OnBtActionBarClick;
                try
                {
                    ui.AddElementInCustomTheme(PART_ScButtonWithTitleAndDetails, "PART_ScButtonWithTitleAndDetails");
                    ListCustomElements.Add(new CustomElement { ParentElementName = "PART_ScButtonWithTitleAndDetails", Element = PART_ScButtonWithTitleAndDetails });
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, "SuccessStory", "Error on AddCustomElements()");
                }
            }
            else
            {
#if DEBUG
                logger.Debug($"SuccessStory - PART_ScButtonWithTitleAndDetails not find");
#endif
            }


            if (PART_Achievements_ProgressBar != null && _Settings.IntegrationShowProgressBar)
            {
                PART_Achievements_ProgressBar = new ScDescriptionIntegration(_Settings, SuccessStory.achievementsDatabase, SuccessStory.SelectedGameAchievements, true, false, false, false, false, true);
                PART_Achievements_ProgressBar.Name = "Achievements_ProgressBar";
                try
                {
                    ui.AddElementInCustomTheme(PART_Achievements_ProgressBar, "PART_Achievements_ProgressBar");
                    ListCustomElements.Add(new CustomElement { ParentElementName = "PART_Achievements_ProgressBar", Element = PART_Achievements_ProgressBar });
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, "SuccessStory", "Error on AddCustomElements()");
                }
            }
            else
            {
#if DEBUG
                logger.Debug($"SuccessStory - PART_Achievements_ProgressBar not find");
#endif
            }

            if (PART_Achievements_Graphics != null && _Settings.IntegrationShowGraphic)
            {
                PART_Achievements_Graphics = new ScDescriptionIntegration(_Settings, SuccessStory.achievementsDatabase, SuccessStory.SelectedGameAchievements, true, true, false, false, false, false);
                PART_Achievements_Graphics.Name = "Achievements_Graphics";
                try
                {
                    ui.AddElementInCustomTheme(PART_Achievements_Graphics, "PART_Achievements_Graphics");
                    ListCustomElements.Add(new CustomElement { ParentElementName = "PART_Achievements_Graphics", Element = PART_Achievements_Graphics });
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, "SuccessStory", "Error on AddCustomElements()");
                }
            }
            else
            {
#if DEBUG
                logger.Debug($"SuccessStory - PART_Achievements_Graphics not find");
#endif
            }

            if (PART_Achievements_List != null && _Settings.IntegrationShowAchievements)
            {
                PART_Achievements_List = new ScDescriptionIntegration(_Settings, SuccessStory.achievementsDatabase, SuccessStory.SelectedGameAchievements, true, false, true, false, false, false);
                PART_Achievements_List.Name = "Achievements_List";
                try
                {
                    ui.AddElementInCustomTheme(PART_Achievements_List, "PART_Achievements_List");
                    ListCustomElements.Add(new CustomElement { ParentElementName = "PART_Achievements_List", Element = PART_Achievements_List });
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, "SuccessStory", "Error on AddCustomElements()");
                }
            }
            else
            {
#if DEBUG
                logger.Debug($"SuccessStory - PART_Achievements_List not find");
#endif
            }

            if (PART_Achievements_ListCompactUnlocked != null && _Settings.IntegrationShowAchievements)
            {
                PART_Achievements_ListCompactUnlocked = new ScDescriptionIntegration(_Settings, SuccessStory.achievementsDatabase, SuccessStory.SelectedGameAchievements, true, false, false, true, false, false);
                PART_Achievements_ListCompactUnlocked.Name = "Achievements_ListCompactUnlocked";
                try
                {
                    ui.AddElementInCustomTheme(PART_Achievements_ListCompactUnlocked, "PART_Achievements_ListCompactUnlocked");
                    ListCustomElements.Add(new CustomElement { ParentElementName = "PART_Achievements_ListCompactUnlocked", Element = PART_Achievements_ListCompactUnlocked });
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, "SuccessStory", "Error on AddCustomElements()");
                }
            }
            else
            {
#if DEBUG
                logger.Debug($"SuccessStory - PART_Achievements_ListCompactUnlocked not find");
#endif
            }

            if (PART_Achievements_ListCompactLocked != null && _Settings.IntegrationShowAchievements)
            {
                PART_Achievements_ListCompactLocked = new ScDescriptionIntegration(_Settings, SuccessStory.achievementsDatabase, SuccessStory.SelectedGameAchievements, true, false, false, false, true, false);
                PART_Achievements_ListCompactLocked.Name = "Achievements_ListCompactLocked";
                try
                {
                    ui.AddElementInCustomTheme(PART_Achievements_ListCompactLocked, "PART_Achievements_ListCompactLocked");
                    ListCustomElements.Add(new CustomElement { ParentElementName = "PART_Achievements_ListCompactLocked", Element = PART_Achievements_ListCompactLocked });
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, "SuccessStory", "Error on AddCustomElements()");
                }
            }
            else
            {
#if DEBUG
                logger.Debug($"SuccessStory - PART_Achievements_ListCompactLocked not find");
#endif
            }
        }

        public override void RefreshCustomElements()
        {
#if DEBUG
            logger.Debug($"SuccessStory - ListCustomElements - {ListCustomElements.Count}");
#endif
            foreach (CustomElement customElement in ListCustomElements)
            {
                try
                {
                    bool isFind = false;

                    if (customElement.Element is SuccessStoryButton)
                    {
#if DEBUG
                        logger.Debug($"SuccessStory - customElement.Element is SuccessStoryButton");
#endif
                        isFind = true;
                        if (SuccessStory.SelectedGameAchievements != null)
                        {
                            customElement.Element.Visibility = Visibility.Visible;
                        }
                        else
                        {
#if DEBUG
                            logger.Debug($"SuccessStory - customElement.Element is SuccessStoryButton with no data");
#endif
                        }
                    }

                    if (customElement.Element is SuccessStoryButtonDetails)
                    {
#if DEBUG
                        logger.Debug($"SuccessStory - customElement.Element is SuccessStoryButtonDetails");
#endif
                        isFind = true;
                        if (SuccessStory.SelectedGameAchievements != null)
                        {
                            customElement.Element.Visibility = Visibility.Visible;
                            ((SuccessStoryButtonDetails)customElement.Element).SetScData(SuccessStory.SelectedGameAchievements.Unlocked, SuccessStory.SelectedGameAchievements.Total);
                        }
                        else
                        {
#if DEBUG
                            logger.Debug($"SuccessStory - customElement.Element is SuccessStoryButton with no data");
#endif
                        }                        
                    }

                    if (customElement.Element is ScDescriptionIntegration)
                    {
#if DEBUG
                        logger.Debug($"SuccessStory - customElement.Element is ScDescriptionIntegration");
#endif
                        isFind = true;
                        if (SuccessStory.SelectedGameAchievements != null)
                        {
                            customElement.Element.Visibility = Visibility.Visible;
                            ((ScDescriptionIntegration)customElement.Element).SetScData(_Settings, SuccessStory.achievementsDatabase, SuccessStory.SelectedGameAchievements);
                        }
                        else
                        {
#if DEBUG
                            logger.Debug($"SuccessStory - customElement.Element is ScDescriptionIntegration with no data");
#endif
                        }
                    }

                    if (!isFind)
                    {
                        logger.Warn($"SuccessStory - RefreshCustomElements({customElement.ParentElementName})");
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, "SuccessStory", $"Error on RefreshCustomElements()");
                }
            }
        }
        #endregion
    }
}
