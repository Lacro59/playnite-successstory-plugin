using System;
using System.Collections.Generic;
using System.IO;
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
    /// Logique d'interaction pour TextBlockWithIcon.xaml
    /// </summary>
    public partial class TextBlockWithIcon : TextBlock
    {
        #region Properties
        public TextBlockWithIconMode Mode
        {
            get { return (TextBlockWithIconMode)GetValue(ModeProperty); }
            set { SetValue(ModeProperty, value); }
        }

        public static readonly DependencyProperty ModeProperty = DependencyProperty.Register(
            nameof(Mode),
            typeof(TextBlockWithIconMode),
            typeof(TextBlockWithIcon),
            new FrameworkPropertyMetadata(TextBlockWithIconMode.IconTextFirstWithText, ControlsPropertyChangedCallback));


        public string Icon
        {
            get { return (string)GetValue(IconProperty); }
            set { SetValue(IconProperty, value); }
        }

        public static readonly DependencyProperty IconProperty = DependencyProperty.Register(
            nameof(Icon),
            typeof(string),
            typeof(TextBlockWithIcon),
            new FrameworkPropertyMetadata(string.Empty, ControlsPropertyChangedCallback));


        public new string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public new static readonly DependencyProperty TextProperty = DependencyProperty.Register(
            nameof(Text),
            typeof(string),
            typeof(TextBlockWithIcon),
            new FrameworkPropertyMetadata(string.Empty, ControlsPropertyChangedCallback));


        public string IconText
        {
            get { return (string)GetValue(IconTextProperty); }
            set { SetValue(IconTextProperty, value); }
        }

        public static readonly DependencyProperty IconTextProperty = DependencyProperty.Register(
            nameof(IconText),
            typeof(string),
            typeof(TextBlockWithIcon),
            new FrameworkPropertyMetadata(string.Empty, ControlsPropertyChangedCallback));
        #endregion


        private static void ControlsPropertyChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is TextBlockWithIcon obj && e.NewValue != e.OldValue)
            {
                obj.SetData();
            }
        }


        public TextBlockWithIcon()
        {
            InitializeComponent();
        }


        private void SetData()
        {
            bool UseIcon = false;
            bool UseIconText = false;
            bool UseText = false;
            bool UseMargin = false;

            switch (Mode)
            {
                case TextBlockWithIconMode.TextOnly:
                    UseText = true;
                    break;

                case TextBlockWithIconMode.IconOnly:
                    UseIcon = true;
                    break;

                case TextBlockWithIconMode.IconWithText:
                    UseIcon = true;
                    UseText = true;
                    UseMargin = !Text.IsNullOrEmpty();
                    break;

                case TextBlockWithIconMode.IconTextOnly:
                    UseIconText = true;
                    break;

                case TextBlockWithIconMode.IconTextWithText:
                    UseIconText = true;
                    UseText = true;
                    UseMargin = !Text.IsNullOrEmpty();
                    break;

                case TextBlockWithIconMode.IconFirstOnly:
                    if (!Icon.IsNullOrEmpty() && File.Exists(Icon))
                    {
                        UseIcon = true;
                    }
                    else
                    {
                        UseIconText = true;
                    }
                    break;

                case TextBlockWithIconMode.IconFirstWithText:
                    if (!Icon.IsNullOrEmpty() && File.Exists(Icon))
                    {
                        UseIcon = true;
                    }
                    else
                    {
                        UseIconText = true;
                    }

                    UseText = true;
                    UseMargin = !Text.IsNullOrEmpty();
                    break;

                case TextBlockWithIconMode.IconTextFirstOnly:
                    if (!IconText.IsNullOrEmpty())
                    {
                        UseIconText = true;
                    }
                    else
                    {
                        UseIcon = true;
                    }
                    break;

                case TextBlockWithIconMode.IconTextFirstWithText:
                    if (!IconText.IsNullOrEmpty())
                    {
                        UseIconText = true;
                    }
                    else
                    {
                        UseIcon = true;
                    }

                    UseText = true;
                    UseMargin = !Text.IsNullOrEmpty();
                    break;
            }

            this.DataContext = new
            {
                UseIcon,
                UseIconText,
                UseText,
                UseMargin,

                Icon,
                Text,
                IconText
            };
        }


        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            double FontSize = PART_Text.FontSize;
            PART_IconText.FontSize = FontSize + 8;
            PART_Icon.Height = FontSize + 12;
        }
    }


    public enum TextBlockWithIconMode
    {
        TextOnly,
        IconOnly, IconWithText, 
        IconTextOnly, IconTextWithText, 
        IconFirstOnly, IconFirstWithText,
        IconTextFirstOnly, IconTextFirstWithText
    }
}
