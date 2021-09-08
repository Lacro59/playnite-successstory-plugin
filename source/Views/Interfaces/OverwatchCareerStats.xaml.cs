using SuccessStory.Models;
using System;
using System.Collections.Generic;
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

namespace SuccessStory.Views.Interfaces
{
    /// <summary>
    /// Logique d'interaction pour OverwatchCareerStats.xaml
    /// </summary>
    public partial class OverwatchCareerStats : UserControl
    {
        #region Properties
        public string Title
        {
            get { return (string)GetValue(TitlePropertyProperty); }
            set { SetValue(TitlePropertyProperty, value); }
        }

        public static readonly DependencyProperty TitlePropertyProperty = DependencyProperty.Register(
            nameof(Title),
            typeof(string),
            typeof(OverwatchCareerStats),
            new FrameworkPropertyMetadata(null));


        public List<Career> CareerStats
        {
            get { return (List<Career>)GetValue(CareerStatsPropertyProperty); }
            set { SetValue(CareerStatsPropertyProperty, value); }
        }

        public static readonly DependencyProperty CareerStatsPropertyProperty = DependencyProperty.Register(
            nameof(CareerStats),
            typeof(List<Career>),
            typeof(OverwatchCareerStats),
            new FrameworkPropertyMetadata(null));
        #endregion


        public OverwatchCareerStats()
        {
            InitializeComponent();
        }


        private void ListBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (CareerStats != null && CareerStats.Count > 0)
            {
                PART_Title.Text = CareerStats[0].Title;
            }

            var Lb = sender as ListBox;
            
            Lb.ItemsSource = null;
            Lb.Items.Clear();
            Lb.ItemsSource = CareerStats;
        }
    }
}
