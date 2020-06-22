using System.Windows.Controls.Primitives;


namespace SuccessStory.Views.Interface
{
    /// <summary>
    /// Logique d'interaction pour SuccessStoryToggleButtonDetails.xaml
    /// </summary>
    public partial class SuccessStoryToggleButtonDetails : ToggleButton
    {
        public SuccessStoryToggleButtonDetails(int Unlocked, int Total)
        {
            InitializeComponent();

            sc_labelButton.Content = Unlocked + "/" + Total;

            sc_pbButton.Value = Unlocked;
            sc_pbButton.Maximum = Total;
        }
    }
}
