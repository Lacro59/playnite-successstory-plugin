using CommonPluginsShared;
using Playnite.SDK;
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


        public SuccessStoryEditManual(Game game)
        {
            InitializeComponent();

            gameAchievements = PluginDatabase.Get(game, true);
            LoadData(game);
        }

        private void LoadData(Game GameSelected)
        {
            List<Achievements> ListAchievements = gameAchievements.Items;


            List<ListBoxAchievements> ListBoxAchievements = new List<ListBoxAchievements>();

            for (int i = 0; i < ListAchievements.Count; i++)
            {
                DateTime? dateUnlock = null;
                BitmapImage iconImage = new BitmapImage();

                bool IsGray = false;

                string urlImg = string.Empty;
                try
                {
                    if (ListAchievements[i].DateUnlocked == default(DateTime) || ListAchievements[i].DateUnlocked == null)
                    {
                        if (ListAchievements[i].UrlLocked == string.Empty || ListAchievements[i].UrlLocked == ListAchievements[i].UrlUnlocked)
                        {
                            urlImg = ListAchievements[i].ImageUnlocked;
                            IsGray = true;
                        }
                        else
                        {
                            urlImg = ListAchievements[i].ImageLocked;
                        }
                    }
                    else
                    {
                        urlImg = ListAchievements[i].ImageUnlocked;
                        dateUnlock = ListAchievements[i].DateUnlocked;
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, "SuccessStory", "Error on convert bitmap");
                }

                string NameAchievement = ListAchievements[i].Name;
                bool IsUnlock = dateUnlock != null;

                // Achievement without unlocktime but achieved = 1
                if (dateUnlock == new DateTime(1982, 12, 15, 0, 0, 0, 0))
                {
                    dateUnlock = null;
                }


                string IconImageUnlocked = ListAchievements[i].ImageUnlocked;
                string IconImageLocked = ListAchievements[i].ImageLocked;
                if (IconImageLocked == string.Empty || IconImageLocked == ListAchievements[i].UrlUnlocked)
                {
                    IconImageLocked = ListAchievements[i].ImageUnlocked;
                    IsGray = true;
                }


                ListBoxAchievements.Add(new ListBoxAchievements()
                {
                    Name = NameAchievement,
                    DateUnlock = dateUnlock,
                    EnableRaretyIndicator = PluginDatabase.PluginSettings.EnableRaretyIndicator,
                    Icon = urlImg,
                    IconImage = urlImg,
                    IsGray = IsGray,
                    Description = ListAchievements[i].Description,
                    Percent = ListAchievements[i].Percent,

                    IsUnlock = IsUnlock,
                    IconImageLocked = IconImageLocked,
                    IconImageUnlocked = IconImageUnlocked
                });

                iconImage = null;
            }


            // Sorting 
            lbAchievements.ItemsSource = ListBoxAchievements;
            CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(lbAchievements.ItemsSource);
            view.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
        }



        private void LbAchievements_Loaded(object sender, RoutedEventArgs e)
        {
            int RowDefinied = (int)lbAchievements.Height / 70;

            int ColDefinied = 1;
            double WidthDefinied = lbAchievements.ActualWidth / ColDefinied;

            this.DataContext = new
            {
                WidthDefinied = WidthDefinied,
                ColDefinied = ColDefinied,
                RowDefinied = RowDefinied
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

        private void PART_CbUnlock_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                CheckBox checkBox = sender as CheckBox;
                DatePicker datePicker = ((Grid)checkBox.Parent).FindName("PART_DtUnlock") as DatePicker;
                datePicker.SelectedDate = null;
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
            foreach (ListBoxAchievements el in lbAchievements.ItemsSource)
            {
                int index = gameAchievements.Items.FindIndex(x => x.Name == el.Name);

                if (index > -1)
                {
                    if (el.IsUnlock)
                    {
                        if (el.DateUnlock != null)
                        {
                            gameAchievements.Items[index].DateUnlocked = el.DateUnlock;
                        }
                        else
                        {
                            gameAchievements.Items[index].DateUnlocked = new DateTime(1982,12,15,0,0,0);
                        }
                    }
                    else
                    {
                        gameAchievements.Items[index].DateUnlocked = default(DateTime);
                    }
                }
            }

            gameAchievements.Unlocked = gameAchievements.Items.FindAll(x => x.DateUnlocked != null && x.DateUnlocked != default(DateTime)).Count;
            gameAchievements.Locked = gameAchievements.Total - gameAchievements.Unlocked;
            gameAchievements.Progression = (gameAchievements.Total != 0) ? (int)Math.Ceiling((double)(gameAchievements.Unlocked * 100 / gameAchievements.Total)) : 0;

            PluginDatabase.Update(gameAchievements);

            PlayniteUiHelper.ResetToggle();
            SuccessStory.successStoryUI.RemoveElements();
            var TaskIntegrationUI = Task.Run(() =>
            {
                var dispatcherOp = SuccessStory.successStoryUI.AddElements();
                dispatcherOp.Completed += (s, ev) => { SuccessStory.successStoryUI.RefreshElements(SuccessStoryDatabase.GameSelected); };
            });

            ((Window)this.Parent).Close();
        }
    }
}
