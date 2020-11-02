using System.Windows.Controls;

namespace SuccessStory.Views.Interface
{
    /// <summary>
    /// Logique d'interaction pour SuccessStoryButtonHeader.xaml
    /// </summary>
    public partial class SuccessStoryButtonHeader : Button
    {
        public SuccessStoryButtonHeader(string Content)
        {
            InitializeComponent();

            btHeaderName.Text = Content;
        }
    }
}

