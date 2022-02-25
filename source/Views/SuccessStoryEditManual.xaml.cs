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

namespace SuccessStory.Views
{
    /// <summary>
    /// Logique d'interaction pour SuccessStoryEditManual.xaml
    /// </summary>
    public partial class SuccessStoryEditManual : UserControl
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private static IResourceProvider resources = new ResourceProvider();

        private SuccessStoryDatabase PluginDatabase = SuccessStory.PluginDatabase;
        private GameAchievements gameAchievements;

        private ObservableCollection<Achievements> ListAchievements = new ObservableCollection<Achievements>();

        private bool viewFilter(object item)
        {
            bool b1 = SearchElement.Text.IsNullOrEmpty() ? true : (item as Achievements).Name.RemoveDiacritics().Contains(SearchElement.Text.RemoveDiacritics(), StringComparison.InvariantCultureIgnoreCase);
            bool b2 = !(bool)PART_IncludeDescription.IsChecked ? true : (item as Achievements).Description.RemoveDiacritics().Contains(SearchElement.Text.RemoveDiacritics(), StringComparison.InvariantCultureIgnoreCase);
            bool b3 = !(bool)PART_OnlyLocked.IsChecked ? true : !(item as Achievements).IsUnlock;

            return ((bool)PART_IncludeDescription.IsChecked ? (b1 || b2) : b1) && b3;
        }


        public SuccessStoryEditManual(Game game)
        {
            InitializeComponent();

            gameAchievements = PluginDatabase.Get(game, true);
            LoadData(game);
        }

        private void LoadData(Game GameSelected)
        {
            ListAchievements = Serialization.GetClone(gameAchievements.Items).ToObservable();

            ListAchievements = ListAchievements.OrderBy(x => x.Name).ToObservable();
            lbAchievements.ItemsSource = ListAchievements;

            CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(lbAchievements.ItemsSource);
            view.Filter = viewFilter;

            Filter();
        }


        private void LbAchievements_Loaded(object sender, RoutedEventArgs e)
        {
            int RowDefinied = (int)lbAchievements.Height / 70;
            int ColDefinied = 1;

            this.DataContext = new
            {
                ColDefinied,
                RowDefinied
            };
        }

        private void TextBlock_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            string Text = ((TextBlock)sender).Text;
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

            if (formattedText.Width > textBlock.DesiredSize.Width)
            {
                ((ToolTip)((TextBlock)sender).ToolTip).Visibility = Visibility.Visible;
            }
            else
            {
                ((ToolTip)((TextBlock)sender).ToolTip).Visibility = Visibility.Hidden;
            }
        }


        private void DatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                DatePicker datePicker = sender as DatePicker;

                if (datePicker.SelectedDate != null)
                {
                    CheckBox checkBox = ((Grid)datePicker.Parent).FindName("PART_CbUnlock") as CheckBox;
                    checkBox.IsChecked = true;
                }
            }
            catch
            {

            }
        }

        private void PART_CbUnlock_Click(object sender, RoutedEventArgs e)
        {
            try
            {                
                CheckBox checkBox = sender as CheckBox;
                int index = int.Parse(checkBox.Tag.ToString());
                if ((bool)checkBox.IsChecked)
                {
                    if (((Achievements)lbAchievements.Items[index]).DateWhenUnlocked == null)
                    {
                        ((Achievements)lbAchievements.Items[index]).DateUnlocked = new DateTime(1982, 12, 15, 0, 0, 0, 0);
                    }
                }
                else
                {
                    DatePicker datePicker = ((Grid)checkBox.Parent).FindName("PART_DtUnlock") as DatePicker;
                    datePicker.SelectedDate = null;
                    ((Achievements)lbAchievements.Items[index]).DateUnlocked = default(DateTime);
                }
            }
            catch
            {

            }
        }


        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            ((Window)this.Parent).Close();
        }

        private void ButtonOK_Click(object sender, RoutedEventArgs e)
        {
            gameAchievements.Items = ((ObservableCollection<Achievements>)lbAchievements.ItemsSource).ToList();
            PluginDatabase.Update(gameAchievements);

            ((Window)this.Parent).Close();
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
