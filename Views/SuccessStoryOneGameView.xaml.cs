using Playnite.SDK;
using Playnite.SDK.Models;
using SuccessStory.Controls;
using SuccessStory.Models;
using SuccessStory.Services;
using SuccessStory.Views.Interface;
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
    /// Logique d'interaction pour SuccessStoryOneGameView.xaml
    /// </summary>
    public partial class SuccessStoryOneGameView : UserControl
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private static IResourceProvider resources = new ResourceProvider();

        private SuccessStoryDatabase PluginDatabase = SuccessStory.PluginDatabase;


        public SuccessStoryOneGameView(Game GameContext)
        {
            InitializeComponent();

            this.DataContext = new
            {
                GameContext
            };
        }
    }
}
