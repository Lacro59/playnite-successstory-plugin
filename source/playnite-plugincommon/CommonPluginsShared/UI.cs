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
    // TODO Used?
    /*
    public class CustomElement
    {
        public string ParentElementName { get; set; }
        public FrameworkElement Element { get; set; }
    }
    */

    public class UI
    {
        private static ILogger logger = LogManager.GetLogger();


        public bool AddResources(List<ResourcesList> ResourcesList)
        {
            Common.LogDebug(true, $"AddResources() - {Serialization.ToJson(ResourcesList)}");

            string ItemKey = string.Empty;

            foreach (ResourcesList item in ResourcesList)
            {
                ItemKey = item.Key;

                try
                {
                    try
                    {
                        Application.Current.Resources.Add(item.Key, item.Value);
                    }
                    catch
                    {
                        Type TypeActual = Application.Current.Resources[ItemKey].GetType();
                        Type TypeNew = item.Value.GetType();

                        if (TypeActual != TypeNew)
                        {
                            if ((TypeActual.Name == "SolidColorBrush" || TypeActual.Name == "LinearGradientBrush")
                                && (TypeNew.Name == "SolidColorBrush" || TypeNew.Name == "LinearGradientBrush"))
                            {
                            }
                            else
                            {
                                logger.Warn($"Different type for {ItemKey}");
                                continue;
                            }
                        }

                        Application.Current.Resources[ItemKey] = item.Value;
                    }
                }
                catch (Exception ex)
                {
                    logger.Error($"Error in AddResources({ItemKey})");
                    Common.LogError(ex, true, $"Error in AddResources({ItemKey})");
                }
            }
            return true;
        }

        // TODO Used?
        /*
        #region Header button
        private FrameworkElement btHeaderChild = null;

        public void AddButtonInWindowsHeader(Button btHeader)
        {
            try
            {
                // Add search element if not allready find
                if (btHeaderChild == null)
                {
                    btHeaderChild = SearchElementByName("PART_ButtonSteamFriends");
                }

                // Not find element
                if (btHeaderChild == null)
                {
                    logger.Error("btHeaderChild [PART_ButtonSteamFriends] not find");
                    return;
                }

                // Add in parent if good type
                if (btHeaderChild.Parent is DockPanel)
                {
                    btHeader.Width = btHeaderChild.ActualWidth;
                    btHeader.Height = btHeaderChild.ActualHeight;
                    DockPanel.SetDock(btHeader, Dock.Right);

                    // Add button 
                    DockPanel btHeaderParent = (DockPanel)btHeaderChild.Parent;
                    for (int i = 0; i < btHeaderParent.Children.Count; i++)
                    {
                        if (((FrameworkElement)btHeaderParent.Children[i]).Name == "PART_ButtonSteamFriends")
                        {
                            btHeaderParent.Children.Insert((i - 1), btHeader);
                            btHeaderParent.UpdateLayout();
                            i = btHeaderParent.Children.Count;

                            Common.LogDebug(true, $"btHeader [{btHeader.Name}] insert");
                        }
                    }
                }
                else
                {
                    logger.Error("btHeaderChild.Parent is not a DockPanel element");
                    return;
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, $"Error on AddButtonInWindowsHeader({btHeader.Name})");
                return;
            }
        }
        #endregion


        #region GameSelectedActionBar button
        private FrameworkElement btGameSelectedActionBarChild = null;

        public void AddButtonInGameSelectedActionBarButtonOrToggleButton(FrameworkElement btGameSelectedActionBar)
        {
            try
            {
                btGameSelectedActionBarChild = SearchElementByName("PART_ButtonMoreActions", true);

                // Not find element
                if (btGameSelectedActionBarChild == null)
                {
                    logger.Error("btGameSelectedActionBarChild [PART_ButtonMoreActions] not find");
                    return;
                }

                var PART_ButtonEditGame = SearchElementByName("PART_ButtonEditGame", btGameSelectedActionBarChild.Parent);

                // Button size
                btGameSelectedActionBar.Height = btGameSelectedActionBarChild.ActualHeight;
                btGameSelectedActionBar.Margin = btGameSelectedActionBarChild.Margin;

                if (double.IsNaN(btGameSelectedActionBar.Width))
                {
                    btGameSelectedActionBar.Width = btGameSelectedActionBarChild.ActualWidth;
                    if (PART_ButtonEditGame != null)
                    {
                        btGameSelectedActionBar.Width = PART_ButtonEditGame.ActualWidth;
                    }
                }

                // Add in parent if good type
                if (btGameSelectedActionBarChild.Parent is StackPanel)
                {
                    // Add button 
                    ((StackPanel)(btGameSelectedActionBarChild.Parent)).Children.Add(btGameSelectedActionBar);
                    ((StackPanel)(btGameSelectedActionBarChild.Parent)).UpdateLayout();

                    Common.LogDebug(true, $"(StackPanel)btGameSelectedActionBar [{btGameSelectedActionBar.Name}] insert");
                }

                if (btGameSelectedActionBarChild.Parent is Grid)
                {
                    StackPanel spContener = (StackPanel)SearchElementByName("PART_spContener", true);

                    // Add StackPanel contener
                    if (((Grid)btGameSelectedActionBarChild.Parent).ColumnDefinitions.Count == 3)
                    {
                        var columnDefinitions = new ColumnDefinition();
                        columnDefinitions.Width = GridLength.Auto;
                        ((Grid)(btGameSelectedActionBarChild.Parent)).ColumnDefinitions.Add(columnDefinitions);

                        spContener = new StackPanel();
                        spContener.Name = "PART_spContener";
                        spContener.Orientation = Orientation.Horizontal;
                        spContener.SetValue(Grid.ColumnProperty, 3);

                        ((Grid)(btGameSelectedActionBarChild.Parent)).Children.Add(spContener);
                        ((Grid)(btGameSelectedActionBarChild.Parent)).UpdateLayout();
                    }

                    // Add button 
                    spContener.Children.Add(btGameSelectedActionBar);
                    spContener.UpdateLayout();

                    Common.LogDebug(true, $"(Grid)btGameSelectedActionBar [{btGameSelectedActionBar.Name}] insert");
                }

                return;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, $"Error on AddButtonInGameSelectedActionBarButtonOrToggleButton({btGameSelectedActionBar.Name})");
                return;
            }
        }

        public void RemoveButtonInGameSelectedActionBarButtonOrToggleButton(string btGameSelectedActionBarName)
        {
            try
            {
                // Not find element
                if (btGameSelectedActionBarChild == null)
                {
                    logger.Error("btGameSelectedActionBarChild [PART_ButtonMoreActions] not find");
                    return;
                }

                FrameworkElement spContener = SearchElementByName("PART_spContener", btGameSelectedActionBarChild.Parent);

                // Remove in parent if good type
                if (btGameSelectedActionBarChild.Parent is StackPanel && btGameSelectedActionBarChild != null)
                {
                    StackPanel btGameSelectedParent = ((StackPanel)(btGameSelectedActionBarChild.Parent));
                    for (int i = 0; i < btGameSelectedParent.Children.Count; i++)
                    {
                        if (((FrameworkElement)btGameSelectedParent.Children[i]).Name == btGameSelectedActionBarName)
                        {
                            btGameSelectedParent.Children.Remove(btGameSelectedParent.Children[i]);
                            btGameSelectedParent.UpdateLayout();

                            Common.LogDebug(true, $"(StackPanel)btGameSelectedActionBar [{btGameSelectedActionBarName}] remove");
                        }
                    }
                }

                if (btGameSelectedActionBarChild.Parent is Grid && spContener != null)
                {
                    for (int i = 0; i < ((StackPanel)spContener).Children.Count; i++)
                    {
                        if (((FrameworkElement)((StackPanel)spContener).Children[i]).Name == btGameSelectedActionBarName)
                        {
                            ((StackPanel)spContener).Children.Remove(((StackPanel)spContener).Children[i]);
                            spContener.UpdateLayout();

                            Common.LogDebug(true, $"(Grid)btGameSelectedActionBar [{btGameSelectedActionBarName}] remove");
                        }
                    }
                }

                return;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, $"Error on RemoveButtonInGameSelectedActionBarButtonOrToggleButton({btGameSelectedActionBarName})");
                return;
            }
        }
        #endregion


        #region GameSelectedDescription
        private FrameworkElement elGameSelectedDescriptionContener = null;

        public void AddElementInGameSelectedDescription(FrameworkElement elGameSelectedDescription, bool isTop = false, bool ShowTitle = true)
        {
            try
            {
                var PART_ElemDescription = SearchElementByName("PART_ElemDescription", true);

                // Not find element
                if (PART_ElemDescription == null)
                {
                    logger.Warn("elGameSelectedDescriptionContener [PART_ElemDescription] not find");
                    return;
                }

                elGameSelectedDescriptionContener = (FrameworkElement)PART_ElemDescription.Parent;
                var PART_ElemNotes = SearchElementByName("PART_ElemNotes", elGameSelectedDescriptionContener);

                // Not find element
                if (elGameSelectedDescriptionContener == null)
                {
                    logger.Warn("elGameSelectedDescriptionContener.Parent [PART_ElemDescription] not find");
                    return;
                }

                if (PART_ElemNotes != null && isTop)
                {
                    elGameSelectedDescription.Margin = PART_ElemNotes.Margin;
                }
                else if (PART_ElemNotes != null)
                {
                    PART_ElemDescription.Margin = PART_ElemNotes.Margin;
                    elGameSelectedDescription.Margin = PART_ElemDescription.Margin;
                }

                // Add in parent if good type
                if (elGameSelectedDescriptionContener is StackPanel)
                {
                    var tempTextBlock = Tools.FindVisualChildren<TextBlock>(PART_ElemDescription).FirstOrDefault();
                    if (tempTextBlock != null)
                    {
                        FrameworkElement PART_Title = (FrameworkElement)elGameSelectedDescription.FindName("PART_Title");
                        if (PART_Title != null && ShowTitle)
                        {
                            PART_Title.Margin = tempTextBlock.Margin;
                        }
                        else
                        {
                            elGameSelectedDescription.Margin = tempTextBlock.Margin;
                        }
                    }

                    // Add FrameworkElement 
                    if (isTop)
                    {
                        ((StackPanel)elGameSelectedDescriptionContener).Children.Insert(0, elGameSelectedDescription);
                    }
                    else
                    {
                        ((StackPanel)elGameSelectedDescriptionContener).Children.Add(elGameSelectedDescription);
                    }
                    ((StackPanel)elGameSelectedDescriptionContener).UpdateLayout();

                    Common.LogDebug(true, $"elGameSelectedDescriptionContener [{elGameSelectedDescription.Name}] insert");
                    return;
                }

                if (elGameSelectedDescriptionContener is DockPanel)
                {
                    elGameSelectedDescription.SetValue(DockPanel.DockProperty, Dock.Top);

                    var tempSeparator = Tools.FindVisualChildren<Separator>(PART_ElemDescription).FirstOrDefault();
                    if (tempSeparator != null)
                    {
                        FrameworkElement PART_Separator = (FrameworkElement)elGameSelectedDescription.FindName("PART_Separator");
                        if (PART_Separator != null && ShowTitle)
                        {
                            PART_Separator.Margin = tempSeparator.Margin;
                        }
                        else
                        {
                        }
                    }

                    // Add FrameworkElement 
                    if (isTop)
                    {
                        ((DockPanel)elGameSelectedDescriptionContener).Children.Insert(1, elGameSelectedDescription);
                    }
                    else
                    {
                        ((DockPanel)elGameSelectedDescriptionContener).Children.Add(elGameSelectedDescription);
                    }
                    ((DockPanel)elGameSelectedDescriptionContener).UpdateLayout();

                    Common.LogDebug(true, $"elGameSelectedDescriptionContener [{elGameSelectedDescription.Name}] insert");
                    return;
                }

                logger.Warn($"elGameSelectedDescriptionContener is {elGameSelectedDescriptionContener.ToString()}");
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, $"Error on AddElementInGameSelectedDescription({elGameSelectedDescription.Name})");
                return;
            }
        }

        public void RemoveElementInGameSelectedDescription(string elGameSelectedDescriptionName)
        {
            try
            {
                // Not find element
                if (elGameSelectedDescriptionContener == null)
                {
                    logger.Error("elGameSelectedDescriptionContener [PART_ElemDescription] not find");
                    return;
                }

                // Remove in parent if good type
                if (elGameSelectedDescriptionContener is StackPanel)
                {
                    StackPanel elGameSelectedParent = ((StackPanel)(elGameSelectedDescriptionContener));
                    for (int i = 0; i < elGameSelectedParent.Children.Count; i++)
                    {
                        if (((FrameworkElement)elGameSelectedParent.Children[i]).Name == elGameSelectedDescriptionName)
                        {
                            elGameSelectedParent.Children.Remove(elGameSelectedParent.Children[i]);
                            elGameSelectedParent.UpdateLayout();

                            Common.LogDebug(true, $"elGameSelectedDescriptionName [{elGameSelectedDescriptionName}] remove");
                        }
                    }
                }

                if (elGameSelectedDescriptionContener is DockPanel)
                {
                    DockPanel elGameSelectedParent = ((DockPanel)(elGameSelectedDescriptionContener));
                    for (int i = 0; i < elGameSelectedParent.Children.Count; i++)
                    {
                        if (((FrameworkElement)elGameSelectedParent.Children[i]).Name == elGameSelectedDescriptionName)
                        {
                            elGameSelectedParent.Children.Remove(elGameSelectedParent.Children[i]);
                            elGameSelectedParent.UpdateLayout();

                            Common.LogDebug(true, $"elGameSelectedDescriptionName [{elGameSelectedDescriptionName}] remove");
                        }
                    }
                }

                return;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, $"Error on RemoveElementInGameSelectedDescription({elGameSelectedDescriptionName})");
                return;
            }
        }
        #endregion


        #region Custom theme
        private List<FrameworkElement> ListCustomElement = new List<FrameworkElement>();

        public void AddElementInCustomTheme(FrameworkElement ElementInCustomTheme, string ElementParentInCustomThemeName)
        {
            try
            {
                FrameworkElement ElementCustomTheme = SearchElementByName(ElementParentInCustomThemeName, false, true);

                // Not find element
                if (ElementCustomTheme == null)
                {
                    logger.Error($"ElementCustomTheme [{ElementParentInCustomThemeName}] not find");
                    return;
                }

                // Add in parent if good type
                if (ElementCustomTheme is StackPanel)
                {
                    if (!double.IsNaN(ElementCustomTheme.Height))
                    {
                        ElementInCustomTheme.Height = ElementCustomTheme.Height;
                    }

                    if (!double.IsNaN(ElementCustomTheme.Width))
                    {
                        ElementInCustomTheme.Width = ElementCustomTheme.Width;
                    }

                    // Add FrameworkElement 
                    ((StackPanel)ElementCustomTheme).Children.Add(ElementInCustomTheme);
                    ((StackPanel)ElementCustomTheme).UpdateLayout();

                    ListCustomElement.Add(ElementCustomTheme);

                    Common.LogDebug(true, $"ElementCustomTheme [{ElementCustomTheme.Name}] insert");
                }
                else
                {
                    logger.Error($"ElementCustomTheme is not a StackPanel element");
                    return;
                }

                return;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, $"Error on AddElementInCustomTheme({ElementParentInCustomThemeName})");
                return;
            }
        }

        public void ClearElementInCustomTheme(string ElementParentInCustomThemeName)
        {
            try
            {
                foreach (FrameworkElement ElementCustomTheme in ListCustomElement)
                {
                    // Not find element
                    if (ElementCustomTheme == null)
                    {
                        logger.Error($"ElementCustomTheme [{ElementParentInCustomThemeName}] not find");
                    }

                    // Add in parent if good type
                    if (ElementCustomTheme is StackPanel)
                    {
                        // Clear FrameworkElement 
                        ((StackPanel)ElementCustomTheme).Children.Clear();
                        ((StackPanel)ElementCustomTheme).UpdateLayout();

                        Common.LogDebug(true, $"ElementCustomTheme [{ElementCustomTheme.Name}] clear");
                    }
                    else
                    {
                        logger.Error($"ElementCustomTheme is not a StackPanel element");
                    }
                }

                ListCustomElement = new List<FrameworkElement>();
                return;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, $"Error on ClearElementInCustomTheme({ElementParentInCustomThemeName})");
                ListCustomElement = new List<FrameworkElement>();
                return;
            }
        }
        #endregion




        #region GameSelectedInfoBarFS
        private FrameworkElement spInfoBarFS = null;

        public void AddStackPanelInGameSelectedInfoBarFS(FrameworkElement spGameSelectedInfoBarFS)
        {
            try
            {
                FrameworkElement tempElement = SearchElementByName("PART_ButtonContext");
                if (tempElement != null)
                {
                    tempElement = (FrameworkElement)tempElement.Parent;
                    tempElement = (FrameworkElement)tempElement.Parent;

                    spInfoBarFS = Tools.FindVisualChildren<StackPanel>(tempElement).FirstOrDefault();

                    // Not find element
                    if (spInfoBarFS == null)
                    {
                        logger.Error("btGameSelectedInfoBarFS Parent [PART_ButtonContext] not find");
                        return;
                    }

                    // Add element 
                    if (spInfoBarFS is StackPanel)
                    {
                        ((StackPanel)(spInfoBarFS)).Children.Add(spGameSelectedInfoBarFS);
                        ((StackPanel)(spInfoBarFS)).UpdateLayout();

                        Common.LogDebug(true, $"(StackPanel)btGameSelectedActionBarFS [{spGameSelectedInfoBarFS.Name}] insert");
                    }
                }
                else
                {
                    logger.Error("btGameSelectedInfoBarFS [PART_ButtonContext] not find");
                    return;
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
            }
        }

        public void RemoveStackPanelInGameSelectedInfoBarFS(string spGameSelectedInfoBarNameFS)
        {
            try
            {
                // Not find element
                if (spInfoBarFS == null)
                {
                    logger.Error("btGameSelectedInfoBarFS [PART_ButtonContext] not find");
                    return;
                }

                // Remove in parent if good type
                if (spInfoBarFS is StackPanel)
                {
                    for (int i = 0; i < ((StackPanel)spInfoBarFS).Children.Count; i++)
                    {
                        if (((FrameworkElement)((StackPanel)spInfoBarFS).Children[i]).Name == spGameSelectedInfoBarNameFS)
                        {
                            ((StackPanel)spInfoBarFS).Children.Remove(((StackPanel)spInfoBarFS).Children[i]);
                            ((StackPanel)spInfoBarFS).UpdateLayout();

                            Common.LogDebug(true, $"(StackPanel)btGameSelectedInfoBarFS [{spGameSelectedInfoBarNameFS}] remove");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
            }
        }
        #endregion


        #region GameSelectedActionBarFS
        private FrameworkElement btGameSelectedActionBarChildFS = null;

        public void AddButtonInGameSelectedActionBarButtonOrToggleButtonFS(FrameworkElement btGameSelectedActionBarFS)
        {
            try
            {
                btGameSelectedActionBarChildFS = SearchElementByName("PART_ButtonContext", true);

                // Not find element
                if (btGameSelectedActionBarChildFS == null)
                {
                    logger.Error("btGameSelectedActionBarChildFS [PART_ButtonContext] not find");
                    return;
                }

                btGameSelectedActionBarFS.Height = btGameSelectedActionBarChildFS.ActualHeight;
                if (btGameSelectedActionBarFS.Name == "PART_ButtonContext")
                {
                    btGameSelectedActionBarFS.Width = btGameSelectedActionBarChildFS.ActualWidth;
                }

                // Not find element
                if (btGameSelectedActionBarChildFS == null)
                {
                    logger.Error("btGameSelectedActionBarChildFS [PART_ButtonContext] not find");
                    return;
                }

                // Add in parent if good type
                if (btGameSelectedActionBarChildFS.Parent is StackPanel)
                {
                    // Add button 
                    ((StackPanel)(btGameSelectedActionBarChildFS.Parent)).Children.Add(btGameSelectedActionBarFS);
                    ((StackPanel)(btGameSelectedActionBarChildFS.Parent)).UpdateLayout();

                    Common.LogDebug(true, $"(StackPanel)btGameSelectedActionBarFS [{btGameSelectedActionBarFS.Name}] insert");
                }

                return;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, $"Error on AddButtonInGameSelectedActionBarButtonOrToggleButtonFS({btGameSelectedActionBarFS.Name})");
                return;
            }
        }

        public void RemoveButtonInGameSelectedActionBarButtonOrToggleButtonFS(string btGameSelectedActionBarNameFS)
        {
            try
            {
                // Not find element
                if (btGameSelectedActionBarChildFS == null)
                {
                    logger.Error("btGameSelectedActionBarChildFS [PART_ButtonContext] not find");
                    return;
                }

                // Remove in parent if good type
                if (btGameSelectedActionBarChildFS.Parent is StackPanel && btGameSelectedActionBarChildFS != null)
                {
                    StackPanel btGameSelectedParent = ((StackPanel)(btGameSelectedActionBarChildFS.Parent));
                    for (int i = 0; i < btGameSelectedParent.Children.Count; i++)
                    {
                        if (((FrameworkElement)btGameSelectedParent.Children[i]).Name == btGameSelectedActionBarNameFS)
                        {
                            btGameSelectedParent.Children.Remove(btGameSelectedParent.Children[i]);
                            btGameSelectedParent.UpdateLayout();

                            Common.LogDebug(true, $"(StackPanel)btGameSelectedActionBarChildFS [{btGameSelectedActionBarNameFS}] remove");
                        }
                    }
                }

                return;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, $"Error on RemoveButtonInGameSelectedActionBarButtonOrToggleButton({btGameSelectedActionBarNameFS})");
                return;
            }
        }
        #endregion


        #region GameSelectedDescription
        //private FrameworkElement elGameSelectedDescriptionContenerFS = null;

        public void AddElementInGameSelectedDescriptionFS(FrameworkElement elGameSelectedDescriptionFS)
        {
            try
            {

            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, $"Error on AddElementInGameSelectedDescriptionFS({elGameSelectedDescriptionFS.Name})");
                return;
            }
        }

        public void RemoveElementInGameSelectedDescriptionFS(string elGameSelectedDescriptionFSName)
        {
            try
            {

            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, $"Error on RemoveElementInGameSelectedDescriptionFS({elGameSelectedDescriptionFSName})");
                return;
            }
        }
        #endregion
        */


        /// <summary>
        /// Gel all controls in depObj
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="depObj"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Get control's parent by type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="child"></param>
        /// <remarks>https://www.infragistics.com/community/blogs/b/blagunas/posts/find-the-parent-control-of-a-specific-type-in-wpf-and-silverlight</remarks>
        /// <returns></returns>
        public static T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            //get parent item
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);

            //we've reached the end of the tree
            if (parentObject == null) return null;

            //check if the parent matches the type we're looking for
            if (parentObject is T parent)
            {
                return parent;
            }
            else
            {
                return FindParent<T>(parentObject);
            }
        }

        [Obsolete("Use UI.FindParent<T>", true)]
        public static T GetAncestorOfType<T>(FrameworkElement child) where T : FrameworkElement
        {
            var parent = VisualTreeHelper.GetParent(child);
            if (parent != null && !(parent is T))
            {
                return (T)GetAncestorOfType<T>((FrameworkElement)parent);
            }
            return (T)parent;
        }


        private static FrameworkElement SearchElementByNameInExtander(object control, string ElementName)
        {
            if (control is FrameworkElement)
            {
                if (((FrameworkElement)control).Name == ElementName)
                {
                    return (FrameworkElement)control;
                }


                var children = LogicalTreeHelper.GetChildren((FrameworkElement)control);
                foreach (object child in children)
                {
                    if (child is FrameworkElement)
                    {
                        if (((FrameworkElement)child).Name == ElementName)
                        {
                            return (FrameworkElement)child;
                        }

                        var subItems = LogicalTreeHelper.GetChildren((FrameworkElement)child);
                        foreach (object subItem in subItems)
                        {
                            if (subItem.ToString().ToLower().Contains("expander") || subItem.ToString().ToLower().Contains("tabitem"))
                            {
                                FrameworkElement tmp = null;

                                if (subItem.ToString().ToLower().Contains("expander"))
                                {
                                    tmp = SearchElementByNameInExtander(((Expander)subItem).Content, ElementName);
                                }

                                if (subItem.ToString().ToLower().Contains("tabitem"))
                                {
                                    tmp = SearchElementByNameInExtander(((TabItem)subItem).Content, ElementName);
                                }

                                if (tmp != null)
                                {
                                    return tmp;
                                }
                            }
                            else
                            {
                                var tmp = SearchElementByNameInExtander(child, ElementName);
                                if (tmp != null)
                                {
                                    return tmp;
                                }
                            }
                        }
                    }
                }
            }
            return null;
        }

        public static FrameworkElement SearchElementByName(string ElementName, bool MustVisible = false, bool ParentMustVisible = false, int counter = 1)
        {
            return SearchElementByName(ElementName, Application.Current.MainWindow, MustVisible, ParentMustVisible, counter);
        }

        public static FrameworkElement SearchElementByName(string ElementName, DependencyObject dpObj, bool MustVisible = false, bool ParentMustVisible = false, int counter = 1)
        {
            FrameworkElement ElementFind = null;

            int count = 0;

            if (ElementFind == null)
            {
                foreach (FrameworkElement el in UI.FindVisualChildren<FrameworkElement>(dpObj))
                {
                    if (el.ToString().ToLower().Contains("expander") || el.ToString().ToLower().Contains("tabitem"))
                    {
                        FrameworkElement tmpEl = null;

                        if (el.ToString().ToLower().Contains("expander"))
                        {
                            tmpEl = SearchElementByNameInExtander(((Expander)el).Content, ElementName);
                        }

                        if (el.ToString().ToLower().Contains("tabitem"))
                        {
                            tmpEl = SearchElementByNameInExtander(((TabItem)el).Content, ElementName);
                        }

                        if (tmpEl != null)
                        {
                            if (tmpEl.Name == ElementName)
                            {
                                if (!MustVisible)
                                {
                                    if (!ParentMustVisible)
                                    {
                                        ElementFind = tmpEl;
                                        break;
                                    }
                                    else if (((FrameworkElement)el.Parent).IsVisible)
                                    {
                                        ElementFind = tmpEl;
                                        break;
                                    }
                                }
                                else if (tmpEl.IsVisible)
                                {
                                    if (!ParentMustVisible)
                                    {
                                        ElementFind = tmpEl;
                                        break;
                                    }
                                    else if (((FrameworkElement)el.Parent).IsVisible)
                                    {
                                        ElementFind = tmpEl;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    else if (el.Name == ElementName)
                    {
                        count++;

                        if (!MustVisible)
                        {
                            if (!ParentMustVisible)
                            {
                                if (count == counter)
                                {
                                    ElementFind = el;
                                    break;
                                }
                            }
                            else if (((FrameworkElement)((FrameworkElement)((FrameworkElement)((FrameworkElement)((FrameworkElement)el.Parent).Parent).Parent).Parent).Parent).IsVisible)
                            {
                                if (count == counter)
                                {
                                    ElementFind = el;
                                    break;
                                }
                            }
                        }
                        else if (el.IsVisible)
                        {
                            if (!ParentMustVisible)
                            {
                                if (count == counter)
                                {
                                    ElementFind = el;
                                    break;
                                }
                            }
                            else if (((FrameworkElement)((FrameworkElement)((FrameworkElement)((FrameworkElement)((FrameworkElement)el.Parent).Parent).Parent).Parent).Parent).IsVisible)
                            {
                                if (count == counter)
                                {
                                    ElementFind = el;
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            return ElementFind;
        }


        // TODO Used?
        /*
        private static bool SearchElementInsert(List<FrameworkElement> SearchList, string ElSearchName)
        {
            foreach (FrameworkElement el in SearchList)
            {
                if (ElSearchName == el.Name)
                {
                    return true;
                }
            }
            return false;
        }

        private static bool SearchElementInsert(List<FrameworkElement> SearchList, FrameworkElement ElSearch)
        {
            foreach (FrameworkElement el in SearchList)
            {
                if (ElSearch.Name == el.Name)
                {
                    return true;
                }
            }
            return false;
        }
        */


        public static void SetControlSize(FrameworkElement ControlElement)
        {
            SetControlSize(ControlElement, 0, 0);
        }

        public static void SetControlSize(FrameworkElement ControlElement, double DefaultHeight, double DefaultWidth)
        {
            try
            {
                UserControl ControlParent = UI.FindParent<UserControl>(ControlElement);
                FrameworkElement ControlContener = (FrameworkElement)ControlParent.Parent;

                Common.LogDebug(true, $"SetControlSize({ControlElement.Name}) - parent.name: {ControlContener.Name} - parent.Height: {ControlContener.Height} - parent.Width: {ControlContener.Width} - parent.MaxHeight: {ControlContener.MaxHeight} - parent.MaxWidth: {ControlContener.MaxWidth}");

                // Set Height
                if (!double.IsNaN(ControlContener.Height))
                {
                    ControlElement.Height = ControlContener.Height;
                }
                else if (DefaultHeight != 0)
                {
                    ControlElement.Height = DefaultHeight;
                }
                // Control with MaxHeight
                if (!double.IsNaN(ControlContener.MaxHeight))
                {
                    if (ControlElement.Height > ControlContener.MaxHeight)
                    {
                        ControlElement.Height = ControlContener.MaxHeight;
                    }
                }


                // Set Width
                if (!double.IsNaN(ControlContener.Width))
                {
                    ControlElement.Width = ControlContener.Width;
                }
                else if (DefaultWidth != 0)
                {
                    ControlElement.Width = DefaultWidth;
                }
                // Control with MaxWidth
                if (!double.IsNaN(ControlContener.MaxWidth))
                {
                    if (ControlElement.Width > ControlContener.MaxWidth)
                    {
                        ControlElement.Width = ControlContener.MaxWidth;
                    }
                }
            }
            catch
            {

            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <remarks>https://stackoverflow.com/questions/1585462/bubbling-scroll-events-from-a-listview-to-its-parent</remarks>
        public static void HandlePreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (!e.Handled)
            {
                var scrollViewer = UI.FindVisualChildren<ScrollViewer>((FrameworkElement)sender).FirstOrDefault();

                if (scrollViewer == null)
                {
                    return;
                }

                var scrollPos = scrollViewer.ContentVerticalOffset;
                if ((scrollPos == scrollViewer.ScrollableHeight && e.Delta < 0) || (scrollPos == 0 && e.Delta > 0))
                {
                    e.Handled = true;
                    var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta);
                    eventArg.RoutedEvent = UIElement.MouseWheelEvent;
                    eventArg.Source = sender;
                    var parent = ((Control)sender).Parent as UIElement;
                    parent.RaiseEvent(eventArg);
                }
            }
        }
    }



    public class ResourcesList
    {
        public string Key { get; set; }
        public object Value { get; set; }
    }
}
