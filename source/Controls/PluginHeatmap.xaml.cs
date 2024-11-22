using CommonPluginsControls.Controls;
using CommonPluginsShared.Collections;
using CommonPluginsShared.Controls;
using CommonPluginsShared.Extensions;
using CommonPluginsShared.Interfaces;
using MoreLinq;
using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using SuccessStory.Models;
using SuccessStory.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SuccessStory.Controls
{
    /// <summary>
    /// Logique d'interaction pour PluginHeatmap.xaml
    /// </summary>
    public partial class PluginHeatmap : PluginUserControlExtend
    {
        private SuccessStoryDatabase PluginDatabase => SuccessStory.PluginDatabase;
        internal override IPluginDatabase pluginDatabase => PluginDatabase;

        private PluginHeatmapDataContext ControlDataContext = new PluginHeatmapDataContext();
        internal override IDataContext controlDataContext
        {
            get => ControlDataContext;
            set => ControlDataContext = (PluginHeatmapDataContext)controlDataContext;
        }

        public List<KeyValuePair<string, List<(Game Game, Achievement Achievement)>>> Data
        {
            get => (List<KeyValuePair<string, List<(Game Game, Achievement Achievement)>>>)GetValue(DataProperty);
            set => SetValue(DataProperty, value);
        }

        public static readonly DependencyProperty DataProperty = DependencyProperty.Register(
            nameof(Data),
            typeof(List<KeyValuePair<string, List<(Game Game, Achievement Achievement)>>>),
            typeof(PluginUserControlExtendBase),
            new FrameworkPropertyMetadata(null, DataPropertyChangedCallback));

        private static void DataPropertyChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is PluginHeatmap obj && e.NewValue != e.OldValue)
            {
                obj.SetData(null, null);
            }
        }

        public PluginHeatmap()
        {
            InitializeComponent();
            DataContext = ControlDataContext;

            _ = Task.Run(() =>
            {
                // Wait extension database are loaded
                _ = System.Threading.SpinWait.SpinUntil(() => PluginDatabase.IsLoaded, -1);

                _ = Dispatcher?.BeginInvoke((Action)delegate
                {
                    PluginDatabase.PluginSettings.PropertyChanged += PluginSettings_PropertyChanged;
                    PluginDatabase.Database.ItemUpdated += Database_ItemUpdated;
                    PluginDatabase.Database.ItemCollectionChanged += Database_ItemCollectionChanged;
                    API.Instance.Database.Games.ItemUpdated += Games_ItemUpdated;

                    // Apply settings
                    PluginSettings_PropertyChanged(null, null);
                });
            });
        }


        public override void SetDefaultDataContext()
        {
            ControlDataContext.Settings = PluginDatabase.PluginSettings.Settings;
            bool IsActivated = PluginDatabase.PluginSettings.Settings.EnableIntegrationList;

            if (IgnoreSettings)
            {
                IsActivated = true;
            }

            ControlDataContext.IsActivated = IsActivated;
        }


        public override void SetData(Game newContext, PluginDataBaseGameBase PluginGameData)
        {
            PART_Data.Children.Clear();
            Data?.ForEach(x =>
            {
                Border border = new Border
                {
                    CornerRadius = (CornerRadius)ResourceProvider.GetResource("ControlCornerRadius"),
                    BorderThickness = (Thickness)ResourceProvider.GetResource("ControlBorderThickness"),
                    BorderBrush = (Brush)ResourceProvider.GetResource("NormalBorderBrush")
                };
                StackPanel stackPanel = new StackPanel() { ToolTip = x.Key + (x.Value.Count() != 0 ? $": {x.Value.Count()}" : string.Empty), Width = 20, Height = 20, Margin = new Thickness(2) };
                TextBlock textBlock = new TextBlock { Text = x.Value.Count() != 0 ? x.Value.Count().ToString() : string.Empty, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
                _ = border.Child = stackPanel;
                _ = stackPanel.Children.Add(textBlock);
                _ = PART_Data.Children.Add(border);
            });
        }
    }


    public class PluginHeatmapDataContext : ObservableObject, IDataContext
    {
        private SuccessStorySettings _settings;
        public SuccessStorySettings Settings { get => _settings; set => SetValue(ref _settings, value); }

        private bool isActivated;
        public bool IsActivated { get => isActivated; set => SetValue(ref isActivated, value); }
    }
}
