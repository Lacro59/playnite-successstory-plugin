using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Data;

namespace CommonPluginsShared
{
    public class PlayniteUiHelper
    {
        public static readonly ILogger logger = LogManager.GetLogger();
        public static IResourceProvider resources = new ResourceProvider();


        // TODO Used?
        /*
        public readonly IPlayniteAPI _PlayniteApi;
        public abstract string _PluginUserDataPath { get; set; }

        public UI ui = new UI();
        public readonly TaskHelper taskHelper = new TaskHelper();

        public abstract bool IsFirstLoad { get; set; }

        private StackPanel PART_ElemDescription = null;


        // BtActionBar
        public abstract string BtActionBarName { get; set; }
        public abstract FrameworkElement PART_BtActionBar { get; set; }

        // SpDescription
        public abstract string SpDescriptionName { get; set; }
        public abstract FrameworkElement PART_SpDescription { get; set; }

        // CustomElement
        public abstract List<CustomElement> ListCustomElements { get; set; }


        // SpInfoBarFS
        public abstract string SpInfoBarFSName { get; set; }
        public abstract FrameworkElement PART_SpInfoBarFS { get; set; }

        // BtActionBarFS
        public abstract string BtActionBarFSName { get; set; }
        public abstract FrameworkElement PART_BtActionBarFS { get; set; }



        public PlayniteUiHelper(IPlayniteAPI PlayniteApi, string PluginUserDataPath)
        {
            _PlayniteApi = PlayniteApi;
        }


        public abstract void Initial();
        public abstract void RefreshElements(Game GameSelected, bool force = false);
        public void RemoveElements()
        {
            RemoveBtActionBar();
            RemoveSpDescription();
            RemoveCustomElements();

            RemoveSpInfoBarFS();
            RemoveBtActionBarFS();
        }


        #region DesktopMode
        public abstract DispatcherOperation AddElements();

        public abstract void InitialBtActionBar();
        public abstract void AddBtActionBar();
        public abstract void RefreshBtActionBar();
        public void RemoveBtActionBar()
        {
            if (!BtActionBarName.IsNullOrEmpty())
            {
                ui.RemoveButtonInGameSelectedActionBarButtonOrToggleButton(BtActionBarName);
                PART_BtActionBar = null;
            }
            else
            {
                logger.Warn($"RemoveBtActionBar() without BtActionBarName");
            }
        }


        public abstract void InitialSpDescription();
        public abstract void AddSpDescription();
        public abstract void RefreshSpDescription();
        public void RemoveSpDescription()
        {
            if (!SpDescriptionName.IsNullOrEmpty())
            {
                ui.RemoveElementInGameSelectedDescription(SpDescriptionName);
                PART_SpDescription = null;
                PART_ElemDescription = null;
            }
            else
            {
                logger.Warn($"RemoveSpDescription() without SpDescriptionrName");
            }
        }


        public abstract void InitialCustomElements();
        public abstract void AddCustomElements();
        public abstract void RefreshCustomElements();
        public void RemoveCustomElements()
        {
            foreach (CustomElement customElement in ListCustomElements)
            {
                ui.ClearElementInCustomTheme(customElement.ParentElementName);
            }
            ListCustomElements = new List<CustomElement>();
        }


        public void CheckTypeView()
        {
            if (PART_BtActionBar != null)
            {
                try
                {
                    FrameworkElement BtActionBarParent = (FrameworkElement)PART_BtActionBar.Parent;

                    if (BtActionBarParent != null)
                    {
                        if (!BtActionBarParent.IsVisible)
                        {
                            RemoveBtActionBar();
                        }
                    }
                    else
                    {
                        logger.Warn("BtActionBarParent is null");
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, $"Error on BtActionBar.CheckTypeView({PART_BtActionBar.Name})");
                }
            }

            if (PART_SpDescription != null)
            {
                try
                {
                    FrameworkElement SpDescriptionParent = (FrameworkElement)PART_SpDescription.Parent;

                    if (SpDescriptionParent != null)
                    {
                        if (!SpDescriptionParent.IsVisible)
                        {
                            RemoveSpDescription();
                        }
                    }
                    else
                    {
                        logger.Warn("SpDescriptionParent is null");
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, $"Error on SpDescription.CheckTypeView({PART_SpDescription.Name})");
                }
            }

            if (ListCustomElements.Count > 0)
            {
                bool isVisible = false;

                foreach (CustomElement customElement in ListCustomElements)
                {
                    try
                    {
                        FrameworkElement customElementParent = (FrameworkElement)((FrameworkElement)((FrameworkElement)((FrameworkElement)customElement.Element.Parent).Parent).Parent).Parent;

                        if (customElementParent != null)
                        {
                            // TODO Perfectible
                            if (!customElementParent.IsVisible)
                            {
                                //RemoveCustomElements();
                                //break;
                            }
                            else
                            {
                                isVisible = true;
                                break;
                            }
                        }
                        else
                        {
                            logger.Warn($"customElementParent is null for {customElement.Element.Name}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Common.LogError(ex, false, $"Error on customElement.CheckTypeView({customElement.Element.Name})");
                    }
                }

                if (!isVisible)
                {
                    RemoveCustomElements();
                }
            }
        }
        #endregion


        #region FullScreenMode
        public abstract DispatcherOperation AddElementsFS();

        public abstract void InitialSpInfoBarFS();
        public abstract void AddSpInfoBarFS();
        public abstract void RefreshSpInfoBarFS();
        public void RemoveSpInfoBarFS()
        {
            if (!SpInfoBarFSName.IsNullOrEmpty())
            {
                ui.RemoveStackPanelInGameSelectedInfoBarFS(SpInfoBarFSName);
                PART_SpInfoBarFS = null;
            }
            else
            {
                logger.Warn($"RemoveSpInfoBarFS() without SpInfoBarFSName");
            }
        }


        public abstract void InitialBtActionBarFS();
        public abstract void AddBtActionBarFS();
        public abstract void RefreshBtActionBarFS();
        public void RemoveBtActionBarFS()
        {
            if (!BtActionBarName.IsNullOrEmpty())
            {
                ui.RemoveButtonInGameSelectedActionBarButtonOrToggleButtonFS(BtActionBarFSName);
                PART_BtActionBarFS = null;
            }
            else
            {
                logger.Warn($"RemoveBtActionBarFS() without BtActionBarFSName");
            }
        }
        #endregion


        public static void ResetToggle()
        {
            Common.LogDebug(true, "ResetToggle()");

            try
            {
                FrameworkElement PART_GaButton = UI.SearchElementByName("PART_GaButton", true);
                if (PART_GaButton != null && PART_GaButton is ToggleButton && (bool)((ToggleButton)PART_GaButton).IsChecked)
                {
                    Common.LogDebug(true, "Reset PART_GaButton");

                    ((ToggleButton)PART_GaButton).IsChecked = false;
                    ((ToggleButton)PART_GaButton).RaiseEvent(new RoutedEventArgs(ToggleButton.ClickEvent));
                    return;
                }

                FrameworkElement PART_ScButton = UI.SearchElementByName("PART_ScButton", true);
                if (PART_ScButton != null && PART_ScButton is ToggleButton && (bool)((ToggleButton)PART_ScButton).IsChecked)
                {
                    Common.LogDebug(true, "Reset PART_ScButton");

                    ((ToggleButton)PART_ScButton).IsChecked = false;
                    ((ToggleButton)PART_ScButton).RaiseEvent(new RoutedEventArgs(ToggleButton.ClickEvent));
                    return;
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
            }

            Common.LogDebug(true, "No ResetToggle()");
        }


        public void OnBtActionBarToggleButtonClick(object sender, RoutedEventArgs e)
        {
            Common.LogDebug(true, $"OnBtActionBarToggleButtonClick()");

            try
            {
                if (PART_ElemDescription == null)
                {
                    PART_ElemDescription = (StackPanel)UI.SearchElementByName("PART_ElemDescription", false, true);
                }

                if (PART_ElemDescription == null)
                {
                    logger.Warn("PART_ElemDescription not find on OnBtActionBarToggleButtonClick()");
                    return;
                }

                dynamic PART_ElemDescriptionParent = (FrameworkElement)PART_ElemDescription.Parent;

                //if (NotesVisibility == null)
                //{
                //    FrameworkElement PART_ElemNotes = UI.SearchElementByName("PART_ElemNotes");
                //    if (PART_ElemNotes != null)
                //    {
                //        NotesVisibility = PART_ElemNotes.Visibility;
                //    }
                //}

                FrameworkElement PART_GaButton = UI.SearchElementByName("PART_GaButton", true);
                FrameworkElement PART_ScButton = UI.SearchElementByName("PART_ScButton", true);

                ToggleButton tgButton = sender as ToggleButton;

                if ((bool)(tgButton.IsChecked))
                {
                    for (int i = 0; i < PART_ElemDescriptionParent.Children.Count; i++)
                    {
                        if (((FrameworkElement)PART_ElemDescriptionParent.Children[i]).Name == "PART_GaDescriptionIntegration" && tgButton.Name == "PART_GaButton")
                        {
                            ((FrameworkElement)PART_ElemDescriptionParent.Children[i]).Visibility = Visibility.Visible;

                            // Uncheck other integration ToggleButton
                            if (PART_ScButton is ToggleButton)
                            {
                                ((ToggleButton)PART_ScButton).IsChecked = false;
                            }
                        }
                        else if (((FrameworkElement)PART_ElemDescriptionParent.Children[i]).Name == "PART_ScDescriptionIntegration" && tgButton.Name == "PART_ScButton")
                        {
                            ((FrameworkElement)PART_ElemDescriptionParent.Children[i]).Visibility = Visibility.Visible;

                            // Uncheck other integration ToggleButton
                            if (PART_GaButton is ToggleButton)
                            {
                                ((ToggleButton)PART_GaButton).IsChecked = false;
                            }
                        }
                        else
                        {
                            ((FrameworkElement)PART_ElemDescriptionParent.Children[i]).Visibility = Visibility.Collapsed;
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < PART_ElemDescriptionParent.Children.Count; i++)
                    {
                        if (((FrameworkElement)PART_ElemDescriptionParent.Children[i]).Name == "PART_GaDescriptionIntegration")
                        {
                            if (PART_GaButton is ToggleButton)
                            {
                                if ((bool)((ToggleButton)PART_GaButton).IsChecked)
                                {
                                    ((FrameworkElement)PART_ElemDescriptionParent.Children[i]).Visibility = Visibility.Visible;
                                }
                                else
                                {
                                    ((FrameworkElement)PART_ElemDescriptionParent.Children[i]).Visibility = Visibility.Collapsed;
                                }
                            }
                            else
                            {
                                if (tgButton.Name == "PART_ScButton" && !(bool)tgButton.IsChecked)
                                {
                                    if ((string)((FrameworkElement)PART_ElemDescriptionParent.Children[i]).Tag == "data")
                                    {
                                        ((FrameworkElement)PART_ElemDescriptionParent.Children[i]).Visibility = Visibility.Visible;
                                    }
                                }
                            }
                        }
                        else if (((FrameworkElement)PART_ElemDescriptionParent.Children[i]).Name == "PART_ScDescriptionIntegration")
                        {
                            if (PART_ScButton is ToggleButton)
                            {
                                if ((bool)((ToggleButton)PART_ScButton).IsChecked)
                                {
                                    ((FrameworkElement)PART_ElemDescriptionParent.Children[i]).Visibility = Visibility.Visible;
                                }
                                else
                                {
                                    ((FrameworkElement)PART_ElemDescriptionParent.Children[i]).Visibility = Visibility.Collapsed;
                                }
                            }
                            else
                            {
                                if (tgButton.Name == "PART_GaButton" && !(bool)tgButton.IsChecked)
                                {
                                    if ((string)((FrameworkElement)PART_ElemDescriptionParent.Children[i]).Tag == "data")
                                    {
                                        ((FrameworkElement)PART_ElemDescriptionParent.Children[i]).Visibility = Visibility.Visible;
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (((FrameworkElement)PART_ElemDescriptionParent.Children[i]).Name == "PART_ElemNotes")
                            {
                                FrameworkElement PART_TextNotes = (FrameworkElement)((FrameworkElement)PART_ElemDescriptionParent.Children[i]).FindName("PART_TextNotes");

                                ((FrameworkElement)PART_ElemDescriptionParent.Children[i]).Visibility = Visibility.Collapsed;
                                if (PART_TextNotes != null)
                                {
                                    if (PART_TextNotes is TextBox && ((TextBox)PART_TextNotes).Text != string.Empty)
                                    {
                                        ((FrameworkElement)PART_ElemDescriptionParent.Children[i]).Visibility = Visibility.Visible;
                                    }
                                }
                            }
                            else if (((FrameworkElement)PART_ElemDescriptionParent.Children[i]).Name == "PART_ElemDescription")
                            {
                                ((FrameworkElement)PART_ElemDescriptionParent.Children[i]).Visibility = Visibility.Visible;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
            }
        }
        */

        /// <summary>
        /// Can to exit window with escape  key
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void HandleEsc(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                if (sender is Window)
                {
                    e.Handled = true;
                    ((Window)sender).Close();
                }
            }
        }

        /// <summary>
        /// Create Window with Playnite SDK
        /// </summary>
        /// <param name="PlayniteApi"></param>
        /// <param name="Title"></param>
        /// <param name="ViewExtension"></param>
        /// <param name="windowCreationOptions"></param>
        /// <returns></returns>
        public static Window CreateExtensionWindow(IPlayniteAPI PlayniteApi, string Title, UserControl ViewExtension, WindowCreationOptions windowCreationOptions = null)
        {
            // Default window options
            if (windowCreationOptions == null)
            {
                windowCreationOptions = new WindowCreationOptions
                {
                    ShowMinimizeButton = false,
                    ShowMaximizeButton = false,
                    ShowCloseButton = true
                };
            }

            Window windowExtension = PlayniteApi.Dialogs.CreateWindow(windowCreationOptions);

            windowExtension.Title = Title;
            windowExtension.ShowInTaskbar = false;
            windowExtension.ResizeMode = ResizeMode.NoResize;
            windowExtension.Owner = PlayniteApi.Dialogs.GetCurrentAppWindow();
            windowExtension.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            windowExtension.Content = ViewExtension;

            // TODO Still useful to add margin?
            if (!double.IsNaN(ViewExtension.Height) && !double.IsNaN(ViewExtension.Width))
            {
                windowExtension.Height = ViewExtension.Height + 25;
                windowExtension.Width = ViewExtension.Width;
            }
            else if (!double.IsNaN(ViewExtension.MinHeight) && !double.IsNaN(ViewExtension.MinWidth) && ViewExtension.MinHeight > 0 && ViewExtension.MinWidth > 0)
            {
                windowExtension.Height = ViewExtension.MinHeight + 25;
                windowExtension.Width = ViewExtension.MinWidth;
            }
            else
            {
                // TODO A black border is visible; SDK problem?
                windowExtension.SizeToContent = SizeToContent.WidthAndHeight;
            }

            // Add escape event
            windowExtension.PreviewKeyDown += new KeyEventHandler(HandleEsc);

            return windowExtension;
        }
    }
}
