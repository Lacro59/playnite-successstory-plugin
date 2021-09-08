using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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
    /// Logique d'interaction pour SliderWithPointer.xaml
    /// </summary>
    public partial class SliderWithPointer : Slider
    {
        public SolidColorBrush ThumbFill
        {
            get { return (SolidColorBrush)GetValue(ThumbFillProperty); }
            set { SetValue(ThumbFillProperty, value); }
        }

        public static readonly DependencyProperty ThumbFillProperty = DependencyProperty.Register(
            nameof(ThumbFill),
            typeof(SolidColorBrush),
            typeof(SliderWithPointer),
            new FrameworkPropertyMetadata(default(SolidColorBrush)));


        public SliderWithPointer()
        {
            InitializeComponent();
        }
    }



    public class ElementToTrianglePointsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            FrameworkElement element = value as FrameworkElement;
            PointCollection points = new PointCollection();
            Action fillPoints = () =>
            {
                points.Clear();
                points.Add(new Point(element.ActualWidth / 2, 0));
                points.Add(new Point(element.ActualWidth, element.ActualHeight));
                points.Add(new Point(0, element.ActualHeight));
            };
            fillPoints();
            element.SizeChanged += (s, ee) => fillPoints();
            return points;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
