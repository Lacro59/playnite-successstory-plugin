using CommonPluginsShared;
using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using SuccessStory.Models;
using SuccessStory.Services;
using System;
using System.Collections.Generic;
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

        private List<Achievements> ListAchievements = new List<Achievements>();


        public SuccessStoryEditManual(Game game)
        {
            InitializeComponent();

            gameAchievements = PluginDatabase.Get(game, true);
            LoadData(game);
        }

        private void LoadData(Game GameSelected)
        {
            ListAchievements = Serialization.GetClone(gameAchievements.Items);
            ListAchievements = ListAchievements.OrderByDescending(x => x.Name).ToList();
            lbAchievements.ItemsSource = ListAchievements;
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
            gameAchievements.Items = (List<Achievements>)lbAchievements.ItemsSource;

            gameAchievements.Unlocked = gameAchievements.Items.FindAll(x => x.DateUnlocked != null && x.DateUnlocked != default(DateTime)).Count;
            gameAchievements.Locked = gameAchievements.Total - gameAchievements.Unlocked;
            gameAchievements.Progression = (gameAchievements.Total != 0) ? (int)Math.Ceiling((double)(gameAchievements.Unlocked * 100 / gameAchievements.Total)) : 0;

            PluginDatabase.Update(gameAchievements);

            ((Window)this.Parent).Close();
        }


        private void SearchElement_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (SearchElement.Text.IsNullOrEmpty())
            {
                lbAchievements.ItemsSource = ListAchievements;
            }
            else
            {
                lbAchievements.ItemsSource = ListAchievements.FindAll(x => x.Name.ToLower().Contains(SearchElement.Text.ToLower()));
            }
        }
    }
}
