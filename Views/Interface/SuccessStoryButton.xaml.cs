using System.Windows;
using System.Windows.Controls;

namespace SuccessStory.Views.Interface
{
    /// <summary>
    /// Logique d'interaction pour SuccessStoryToggleButton.xaml
    /// </summary>
    public partial class SuccessStoryButton : Button
    {
        public SuccessStoryButton(bool EnableIntegrationInDescriptionOnlyIcon)
        {
            InitializeComponent();

            if (EnableIntegrationInDescriptionOnlyIcon)
            {
                PART_ButtonIcon.Visibility = Visibility.Visible;
                PART_ButtonText.Visibility = Visibility.Collapsed;
            }
            else
            {
                PART_ButtonIcon.Visibility = Visibility.Collapsed;
                PART_ButtonText.Visibility = Visibility.Visible;
            }
        }
    }
}
