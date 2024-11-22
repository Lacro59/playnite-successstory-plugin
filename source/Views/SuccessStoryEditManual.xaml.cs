using CommonPluginsShared;
using CommonPluginsShared.Extensions;
using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using SuccessStory.Models;
using SuccessStory.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TemperatureMeasurementTool;

namespace SuccessStory.Views
{
    /// <summary>
    /// Logique d'interaction pour SuccessStoryEditManual.xaml
    /// </summary>
    public partial class SuccessStoryEditManual : UserControl
    {
        private SuccessStoryDatabase PluginDatabase => SuccessStory.PluginDatabase;
        private GameAchievements GameAchievements { get; set; }
        private Game GameContext { get; set; }

        private ObservableCollection<Achievement> ListAchievements { get; set; } = new ObservableCollection<Achievement>();

        private bool ViewFilter(object item)
        {
            bool b1 = SearchElement.Text.IsNullOrEmpty() || (item as Achievement).Name.RemoveDiacritics().Contains(SearchElement.Text.RemoveDiacritics(), StringComparison.InvariantCultureIgnoreCase);
            bool b2 = !(bool)PART_IncludeDescription.IsChecked || ((item as Achievement).Description?.RemoveDiacritics().Contains(SearchElement.Text.RemoveDiacritics(), StringComparison.InvariantCultureIgnoreCase) ?? false);
            bool b3 = !(bool)PART_OnlyLocked.IsChecked || !(item as Achievement).IsUnlock;

            return ((bool)PART_IncludeDescription.IsChecked ? (b1 || b2) : b1) && b3;
        }


        public SuccessStoryEditManual(Game game)
        {
            try
            {
                InitializeComponent();

                GameContext = game;
                GameAchievements = PluginDatabase.Get(game, true);
                LoadData();
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }
        }

        private void LoadData()
        {
            ListAchievements = Serialization.GetClone(GameAchievements.Items).ToObservable();

            ListAchievements = ListAchievements.OrderBy(x => x.Name).ToObservable();
            lbAchievements.ItemsSource = ListAchievements;

            CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(lbAchievements.ItemsSource);
            view.Filter = ViewFilter;

            Filter();
        }


        private void LbAchievements_Loaded(object sender, RoutedEventArgs e)
        {
            int RowDefinied = (int)lbAchievements.Height / 80;
            int ColDefinied = 1;

            DataContext = new
            {
                ColDefinied,
                RowDefinied
            };
        }

        private void TextBlock_MouseEnter(object sender, MouseEventArgs e)
        {
            TextBlock textBlock = (TextBlock)sender;

            Typeface typeface = new Typeface(
                textBlock.FontFamily,
                textBlock.FontStyle,
                textBlock.FontWeight,
                textBlock.FontStretch);

            FormattedText formattedText = new FormattedText(
                textBlock.Text,
                System.Threading.Thread.CurrentThread.CurrentCulture,
                textBlock.FlowDirection,
                typeface,
                textBlock.FontSize,
                textBlock.Foreground,
                VisualTreeHelper.GetDpi(this).PixelsPerDip);

            ((ToolTip)((TextBlock)sender).ToolTip).Visibility = formattedText.Width > textBlock.DesiredSize.Width ? Visibility.Visible : Visibility.Hidden;
        }


        private void DatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                DatePicker datePicker = sender as DatePicker;
                if (datePicker.SelectedDate != null)
                {
                    datePicker.Tag = true;
                    CheckBox checkBox = ((Grid)datePicker.Parent).FindName("PART_CbUnlock") as CheckBox;
                    checkBox.IsChecked = true;

                    TimePicker timePicker = ((Grid)datePicker.Parent).FindName("PART_Time") as TimePicker;
                    int index = int.Parse(timePicker.Tag.ToString());
                    DateTime dt = (DateTime)((Achievement)lbAchievements.Items[index]).DateUnlocked;
                    string[] dtTime = timePicker.GetValueAsString().Split(':');
                    ((Achievement)lbAchievements.Items[index]).DateUnlocked = new DateTime(dt.Year, dt.Month, dt.Day, int.Parse(dtTime[0]), int.Parse(dtTime[1]), int.Parse(dtTime[2])).ToUniversalTime();
                }
                else
                {
                    datePicker.Tag = false;
                }
            }
            catch { }
        }

        private void PART_Time_TimeChanged(object sender, RoutedEventArgs e)
        {
            try
            {
                TimePicker timePicker = sender as TimePicker;
                int index = int.Parse(timePicker.Tag.ToString());
                CheckBox checkBox = ((Grid)timePicker.Parent).FindName("PART_CbUnlock") as CheckBox;
                if ((bool)checkBox.IsChecked)
                {
                    DateTime dt = (DateTime)((Achievement)lbAchievements.Items[index]).DateUnlocked;
                    string[] dtTime = timePicker.GetValueAsString().Split(':');
                    ((Achievement)lbAchievements.Items[index]).DateUnlocked = new DateTime(dt.Year, dt.Month, dt.Day, int.Parse(dtTime[0]), int.Parse(dtTime[1]), int.Parse(dtTime[2])).ToUniversalTime();
                }
            }
            catch { }
        }

        private void PART_CbUnlock_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                CheckBox checkBox = sender as CheckBox;
                int index = int.Parse(checkBox.Tag.ToString());
                if ((bool)checkBox.IsChecked)
                {
                    if (((Achievement)lbAchievements.Items[index]).DateWhenUnlocked == null)
                    {
                        ((Achievement)lbAchievements.Items[index]).DateUnlocked = new DateTime(1982, 12, 15, 0, 0, 0, 0);
                    }
                }
                else
                {
                    DatePicker datePicker = ((Grid)checkBox.Parent).FindName("PART_DtUnlock") as DatePicker;
                    datePicker.SelectedDate = null;
                    ((Achievement)lbAchievements.Items[index]).DateUnlocked = default;
                }
            }
            catch { }
        }


        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            ((Window)Parent).Close();
        }

        private void ButtonOK_Click(object sender, RoutedEventArgs e)
        {
            GameAchievements.Items = ((ObservableCollection<Achievement>)lbAchievements.ItemsSource).ToList();
            PluginDatabase.Update(GameAchievements);

            PluginDatabase.ChangeCompletionStatus(GameContext);
            ((Window)Parent).Close();
        }


        #region Filter
        private void Filter()
        {
            if (lbAchievements?.ItemsSource == null)
            {
                return;
            }

            CollectionViewSource.GetDefaultView(lbAchievements.ItemsSource).Refresh();
        }

        private void SearchElement_TextChanged(object sender, TextChangedEventArgs e)
        {
            Filter();
        }


        private void PART_OnlyLocked_Checked(object sender, RoutedEventArgs e)
        {
            Filter();
        }

        private void PART_OnlyLocked_Unchecked(object sender, RoutedEventArgs e)
        {
            Filter();
        }

        private void PART_IncludeDescription_Checked(object sender, RoutedEventArgs e)
        {
            Filter();
        }

        private void PART_IncludeDescription_Unchecked(object sender, RoutedEventArgs e)
        {
            Filter();
        }
        #endregion
    }
}
