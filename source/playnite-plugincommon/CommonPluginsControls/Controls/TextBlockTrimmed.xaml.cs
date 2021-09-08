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

namespace CommonPluginsControls.Controls
{
    /// <summary>
    /// Logique d'interaction pour TextBlockTrimmed.xaml
    /// </summary>
    public partial class TextBlockTrimmed : TextBlock
    {
        public TextBlockTrimmed()
        {
            InitializeComponent();
        }

        private void TextBlock_MouseEnter(object sender, MouseEventArgs e)
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
                ((ToolTip)((TextBlock)sender).ToolTip).Content = Text;
            }
            else
            {
                ((ToolTip)((TextBlock)sender).ToolTip).Visibility = Visibility.Hidden;
            }
        }
    }
}
