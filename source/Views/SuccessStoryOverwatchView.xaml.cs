using Playnite.SDK;
using Playnite.SDK.Models;
using SuccessStory.Services;
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

namespace SuccessStory.Views
{
    /// <summary>
    /// Logique d'interaction pour SuccessStoryOverwatchView.xaml
    /// </summary>
    public partial class SuccessStoryOverwatchView : UserControl
    {
        public SuccessStoryOverwatchView(Game game)
        {
            InitializeComponent();

            DataContext = new
            {
                GameContext = game
            };
        }
    }
}
