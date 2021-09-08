using CommonPluginsShared;
using Playnite.SDK;
using System;
using System.Collections.Generic;
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
    /// Logique d'interaction pour ControlPickerGradient.xaml
    /// </summary>
    public partial class ControlPickerGradient : UserControl
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private Point StartPoint = new Point(0, 1);
        private Point EndPoint = new Point(0, 0);

        private double Offset1 = 0;
        private double Offset2 = 1;

        public bool IsSimpleColor = false;
        public bool OnlySimpleColor = false;

        public Color SimpleColor;
        public SolidColorBrush SimpleSolidColorBrush;
        public LinearGradientBrush linearGradientBrush = new LinearGradientBrush();


        public ControlPickerGradient()
        {
            InitializeComponent();
        }


        public void SetColors(Color color)
        {
            IsSimpleColor = true;
            PART_tbGradient.IsChecked = !IsSimpleColor;

            PART_ColorSimple.Visibility = Visibility.Visible;
            PART_ColorPickerGradient.Visibility = Visibility.Hidden;
            PART_GradientDetailsLinearGrid.Visibility = Visibility.Hidden;
            PART_ButtonSetColorGradient.Visibility = Visibility.Hidden;

            SimpleSolidColorBrush = new SolidColorBrush(color);
            SimpleColor = color;
            PART_Border_Color0.Background = new SolidColorBrush(SimpleColor);
            PART_Border_Color1.Background = new SolidColorBrush(SimpleColor);

            Offset1 = 0;
            Offset2 = 1;

            StartPoint = new Point(0, 1);
            EndPoint = new Point(0, 0);

            PART_AkrColorPicker.SelectedColor = SimpleColor;

            SetCursor();
            SetLinearGradientBrush();
        }

        public void SetColors(SolidColorBrush color)
        {
            IsSimpleColor = true;
            PART_tbGradient.IsChecked = !IsSimpleColor;

            PART_ColorSimple.Visibility = Visibility.Visible;
            PART_ColorPickerGradient.Visibility = Visibility.Hidden;
            PART_GradientDetailsLinearGrid.Visibility = Visibility.Hidden;
            PART_ButtonSetColorGradient.Visibility = Visibility.Hidden;
            
            SimpleSolidColorBrush = color;
            PART_SliderOpacity.Value = SimpleSolidColorBrush.Opacity;
            SimpleColor = color.Color;
            PART_Border_Color0.Background = new SolidColorBrush(SimpleColor);
            PART_Border_Color1.Background = new SolidColorBrush(SimpleColor);

            Offset1 = 0;
            Offset2 = 1;

            StartPoint = new Point(0, 1);
            EndPoint = new Point(0, 0);

            PART_AkrColorPicker.SelectedColor = SimpleColor;

            SetCursor();
            SetLinearGradientBrush();
        }

        public void SetColors(LinearGradientBrush linearGradient)
        { 
            IsSimpleColor = false;
            PART_tbGradient.IsChecked = !IsSimpleColor;

            PART_ColorSimple.Visibility = Visibility.Hidden;
            PART_ColorPickerGradient.Visibility = Visibility.Visible;
            PART_GradientDetailsLinearGrid.Visibility = Visibility.Visible;
            PART_ButtonSetColorGradient.Visibility = Visibility.Visible;

            SimpleColor = linearGradient.GradientStops[0].Color;
            PART_Border_Color0.Background = new SolidColorBrush(linearGradient.GradientStops[0].Color);
            PART_Border_Color1.Background = new SolidColorBrush(linearGradient.GradientStops[1].Color);

            Offset1 = linearGradient.GradientStops[0].Offset;
            Offset2 = linearGradient.GradientStops[1].Offset;

            StartPoint = linearGradient.StartPoint;
            EndPoint = linearGradient.EndPoint;

            PART_AkrColorPicker.SelectedColor = SimpleColor;

            SetCursor();
            SetLinearGradientBrush();
        }

        private void SetCursor()
        {
            double XX;
            double YY;

            XX = StartPoint.X * PART_GradientDetailsLinearGrid.RenderSize.Width;
            if (StartPoint.X < PART_GradientDetailsLinearGrid.RenderSize.Width)
            {
                XX = StartPoint.X * PART_GradientDetailsLinearGrid.RenderSize.Width - 0.03;
            }

            YY = StartPoint.Y * PART_GradientDetailsLinearGrid.RenderSize.Height;
            if (StartPoint.Y < PART_GradientDetailsLinearGrid.RenderSize.Height)
            {
                YY = StartPoint.Y * PART_GradientDetailsLinearGrid.RenderSize.Height - 0.03;
            }
            
            XX = XX - PART_GradientDetailsLinearTopThumb.Width;
            YY = YY - PART_GradientDetailsLinearTopThumb.Height;

            PART_GradientDetailsLinearLine.X1 = XX + PART_GradientDetailsLinearTopThumb.Width / 2;
            PART_GradientDetailsLinearLine.Y1 = YY + PART_GradientDetailsLinearTopThumb.Height / 2;

            Canvas.SetLeft(PART_GradientDetailsLinearTopThumb, XX);
            Canvas.SetTop(PART_GradientDetailsLinearTopThumb, YY);

            if (Canvas.GetLeft(PART_GradientDetailsLinearTopThumb) < 0)
                Canvas.SetLeft(PART_GradientDetailsLinearTopThumb, 0);
            if (Canvas.GetTop(PART_GradientDetailsLinearTopThumb) < 0)
                Canvas.SetTop(PART_GradientDetailsLinearTopThumb, 0);
            if (Canvas.GetLeft(PART_GradientDetailsLinearTopThumb) > PART_GradientDetailsLinearGrid.RenderSize.Width - PART_GradientDetailsLinearTopThumb.Width / 2)
                Canvas.SetLeft(PART_GradientDetailsLinearTopThumb, PART_GradientDetailsLinearGrid.RenderSize.Width - PART_GradientDetailsLinearTopThumb.Width / 2);
            if (Canvas.GetTop(PART_GradientDetailsLinearTopThumb) > PART_GradientDetailsLinearGrid.RenderSize.Height - PART_GradientDetailsLinearTopThumb.Height / 2)
                Canvas.SetTop(PART_GradientDetailsLinearTopThumb, PART_GradientDetailsLinearGrid.RenderSize.Height - PART_GradientDetailsLinearTopThumb.Height / 2);


            XX = EndPoint.X * PART_GradientDetailsLinearGrid.RenderSize.Width;
            if (EndPoint.X < PART_GradientDetailsLinearGrid.RenderSize.Width)
            {
                XX = EndPoint.X * PART_GradientDetailsLinearGrid.RenderSize.Width - 0.03;
            }

            YY = EndPoint.Y * PART_GradientDetailsLinearGrid.RenderSize.Height;
            if (EndPoint.Y < PART_GradientDetailsLinearGrid.RenderSize.Height)
            {
                YY = EndPoint.Y * PART_GradientDetailsLinearGrid.RenderSize.Height - 0.03;
            }

            XX = XX - PART_GradientDetailsLinearBottomThumb.Width;
            YY = YY - PART_GradientDetailsLinearBottomThumb.Height;

            PART_GradientDetailsLinearLine.X2 = XX + PART_GradientDetailsLinearBottomThumb.Width / 2;
            PART_GradientDetailsLinearLine.Y2 = YY + PART_GradientDetailsLinearBottomThumb.Height / 2;

            Canvas.SetLeft(PART_GradientDetailsLinearBottomThumb, XX);
            Canvas.SetTop(PART_GradientDetailsLinearBottomThumb, YY);

            if (Canvas.GetLeft(PART_GradientDetailsLinearBottomThumb) < 0)
                Canvas.SetLeft(PART_GradientDetailsLinearBottomThumb, 0);
            if (Canvas.GetTop(PART_GradientDetailsLinearBottomThumb) < 0)
                Canvas.SetTop(PART_GradientDetailsLinearBottomThumb, 0);
            if (Canvas.GetLeft(PART_GradientDetailsLinearBottomThumb) > PART_GradientDetailsLinearGrid.RenderSize.Width - PART_GradientDetailsLinearBottomThumb.Width / 2)
                Canvas.SetLeft(PART_GradientDetailsLinearBottomThumb, PART_GradientDetailsLinearGrid.RenderSize.Width - PART_GradientDetailsLinearBottomThumb.Width / 2);
            if (Canvas.GetTop(PART_GradientDetailsLinearBottomThumb) > PART_GradientDetailsLinearGrid.RenderSize.Height - PART_GradientDetailsLinearBottomThumb.Height / 2)
                Canvas.SetTop(PART_GradientDetailsLinearBottomThumb, PART_GradientDetailsLinearGrid.RenderSize.Height - PART_GradientDetailsLinearBottomThumb.Height / 2);
        }


        private void SetLinearGradientBrush()
        {
            try
            {
                linearGradientBrush = new LinearGradientBrush();
                PART_ColorPickerGradient.Fill = linearGradientBrush;

                linearGradientBrush.StartPoint = StartPoint;
                linearGradientBrush.EndPoint = EndPoint;

                GradientStop gs1 = new GradientStop();
                GradientStop gs2 = new GradientStop();

                gs1.Offset = Offset1;
                gs2.Offset = Offset2;

                gs1.Color = ((SolidColorBrush)PART_Border_Color0.Background).Color;
                gs2.Color = ((SolidColorBrush)PART_Border_Color1.Background).Color;

                linearGradientBrush.GradientStops.Add(gs1);
                linearGradientBrush.GradientStops.Add(gs2);

                PART_ColorPickerGradient.Fill = linearGradientBrush;


                PART_SlidderOffset0.Value = Offset1;
                PART_SlidderOffset1.Value = Offset2;
            }
            catch
            {

            }
        }


        private void PART_Button_Color0_Click(object sender, RoutedEventArgs e)
        {
            PART_Border_Color0.Background = new SolidColorBrush((Color)PART_AkrColorPicker.SelectedColor);
            SetLinearGradientBrush();
        }

        private void PART_Button_Color1_Click(object sender, RoutedEventArgs e)
        {
            PART_Border_Color1.Background = new SolidColorBrush((Color)PART_AkrColorPicker.SelectedColor);
            SetLinearGradientBrush();
        }


        private void PART_GradientDetailsLinearTopThumb_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
            Canvas.SetLeft(PART_GradientDetailsLinearTopThumb, Canvas.GetLeft(PART_GradientDetailsLinearTopThumb) + e.HorizontalChange);
            Canvas.SetTop(PART_GradientDetailsLinearTopThumb, Canvas.GetTop(PART_GradientDetailsLinearTopThumb) + e.VerticalChange);

            if (Canvas.GetLeft(PART_GradientDetailsLinearTopThumb) < 0)
                Canvas.SetLeft(PART_GradientDetailsLinearTopThumb, 0);
            if (Canvas.GetTop(PART_GradientDetailsLinearTopThumb) < 0)
                Canvas.SetTop(PART_GradientDetailsLinearTopThumb, 0);
            if (Canvas.GetLeft(PART_GradientDetailsLinearTopThumb) > PART_GradientDetailsLinearGrid.RenderSize.Width - PART_GradientDetailsLinearTopThumb.Width / 2)
                Canvas.SetLeft(PART_GradientDetailsLinearTopThumb, PART_GradientDetailsLinearGrid.RenderSize.Width - PART_GradientDetailsLinearTopThumb.Width / 2);
            if (Canvas.GetTop(PART_GradientDetailsLinearTopThumb) > PART_GradientDetailsLinearGrid.RenderSize.Height - PART_GradientDetailsLinearTopThumb.Height / 2)
                Canvas.SetTop(PART_GradientDetailsLinearTopThumb, PART_GradientDetailsLinearGrid.RenderSize.Height - PART_GradientDetailsLinearTopThumb.Height / 2);


            StartPoint = new Point(
                Canvas.GetLeft(PART_GradientDetailsLinearTopThumb) + PART_GradientDetailsLinearTopThumb.Width / 2, 
                Canvas.GetTop(PART_GradientDetailsLinearTopThumb) + PART_GradientDetailsLinearTopThumb.Height / 2
                );


            PART_GradientDetailsLinearLine.X1 = StartPoint.X;
            PART_GradientDetailsLinearLine.Y1 = StartPoint.Y;


            double XX;
            double YY;
            if (StartPoint.X >= PART_GradientDetailsLinearGrid.RenderSize.Width)
            {
                XX = Math.Round(StartPoint.X / PART_GradientDetailsLinearGrid.RenderSize.Width, 2);
            }
            else
            {
                XX = Math.Round(StartPoint.X / PART_GradientDetailsLinearGrid.RenderSize.Width - 0.03, 2);
            }
            if (StartPoint.Y >= PART_GradientDetailsLinearGrid.RenderSize.Height)
            {
                YY = Math.Round(StartPoint.Y / PART_GradientDetailsLinearGrid.RenderSize.Height, 2);
            }
            else
            {
                YY = Math.Round(StartPoint.Y / PART_GradientDetailsLinearGrid.RenderSize.Height - 0.03, 2);
            }


            StartPoint = new Point(XX, YY);
            SetLinearGradientBrush();


            test0.Content = "X: " + StartPoint.X + "- Y: " + StartPoint.Y;
        }

        private void PART_GradientDetailsLinearBottomThumb_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
            Canvas.SetLeft(PART_GradientDetailsLinearBottomThumb, Canvas.GetLeft(PART_GradientDetailsLinearBottomThumb) + e.HorizontalChange);
            Canvas.SetTop(PART_GradientDetailsLinearBottomThumb, Canvas.GetTop(PART_GradientDetailsLinearBottomThumb) + e.VerticalChange);

            if (Canvas.GetLeft(PART_GradientDetailsLinearBottomThumb) < 0)
                Canvas.SetLeft(PART_GradientDetailsLinearBottomThumb, 0);
            if (Canvas.GetTop(PART_GradientDetailsLinearBottomThumb) < 0)
                Canvas.SetTop(PART_GradientDetailsLinearBottomThumb, 0);
            if (Canvas.GetLeft(PART_GradientDetailsLinearBottomThumb) > PART_GradientDetailsLinearGrid.RenderSize.Width - PART_GradientDetailsLinearBottomThumb.Width / 2)
                Canvas.SetLeft(PART_GradientDetailsLinearBottomThumb, PART_GradientDetailsLinearGrid.RenderSize.Width - PART_GradientDetailsLinearBottomThumb.Width / 2);
            if (Canvas.GetTop(PART_GradientDetailsLinearBottomThumb) > PART_GradientDetailsLinearGrid.RenderSize.Height - PART_GradientDetailsLinearBottomThumb.Height / 2)
                Canvas.SetTop(PART_GradientDetailsLinearBottomThumb, PART_GradientDetailsLinearGrid.RenderSize.Height - PART_GradientDetailsLinearBottomThumb.Height / 2);

            
            EndPoint = new Point(
                Canvas.GetLeft(PART_GradientDetailsLinearBottomThumb) + PART_GradientDetailsLinearBottomThumb.Width / 2,
                Canvas.GetTop(PART_GradientDetailsLinearBottomThumb) + PART_GradientDetailsLinearBottomThumb.Height / 2
                );


            PART_GradientDetailsLinearLine.X2 = EndPoint.X;
            PART_GradientDetailsLinearLine.Y2 = EndPoint.Y;


            double XX;
            double YY;
            if (EndPoint.X >= PART_GradientDetailsLinearGrid.RenderSize.Width)
            {
                XX = Math.Round(EndPoint.X / PART_GradientDetailsLinearGrid.RenderSize.Width, 2);
            }
            else
            {
                XX = Math.Round(EndPoint.X / PART_GradientDetailsLinearGrid.RenderSize.Width - 0.03, 2);
            }
            if (EndPoint.Y >= PART_GradientDetailsLinearGrid.RenderSize.Height)
            {
                YY = Math.Round(EndPoint.Y / PART_GradientDetailsLinearGrid.RenderSize.Height, 2);
            }
            else
            {
                YY = Math.Round(EndPoint.Y / PART_GradientDetailsLinearGrid.RenderSize.Height - 0.03, 2);
            }


            EndPoint = new Point(XX, YY);
            SetLinearGradientBrush();


            test1.Content = "X: " + EndPoint.X + "- Y: " + EndPoint.Y;
        }


        private void PART_tbGradient_Checked(object sender, RoutedEventArgs e)
        {
            IsSimpleColor = false;

            PART_ColorSimple.Visibility = Visibility.Hidden;
            PART_ColorPickerGradient.Visibility = Visibility.Visible;
            PART_GradientDetailsLinearGrid.Visibility = Visibility.Visible;
            PART_ButtonSetColorGradient.Visibility = Visibility.Visible;


            var BorderElements = UI.FindVisualChildren<Border>(PART_AkrColorPicker);
            var BorderElement = BorderElements?.Where(x => x.Background?.ToString() == new SolidColorBrush(SimpleColor)?.ToString()).FirstOrDefault();
            if (BorderElement != null)
            {
                BorderElement.Visibility = Visibility.Hidden;
            }
        }

        private void PART_tbGradient_Unchecked(object sender, RoutedEventArgs e)
        {
            IsSimpleColor = true;

            PART_ColorSimple.Visibility = Visibility.Visible;
            PART_ColorPickerGradient.Visibility = Visibility.Hidden;
            PART_GradientDetailsLinearGrid.Visibility = Visibility.Hidden;
            PART_ButtonSetColorGradient.Visibility = Visibility.Hidden;


            var BorderElements = UI.FindVisualChildren<Border>(PART_AkrColorPicker);
            var BorderElement = BorderElements?.Where(x => x.Background?.ToString() == new SolidColorBrush(SimpleColor)?.ToString()).FirstOrDefault();
            if (BorderElement != null)
            {
                BorderElement.Visibility = Visibility.Visible;
            }
        }


        private void PART_AkrColorPicker_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            SimpleColor = (Color)PART_AkrColorPicker.SelectedColor;
        }

        private void PART_SlidderOffset0_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Offset1 = (double)Math.Round(PART_SlidderOffset0.Value, 1);
            SetLinearGradientBrush();
        }

        private void PART_SlidderOffset1_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Offset2 = (double)Math.Round(PART_SlidderOffset1.Value, 1);
            SetLinearGradientBrush();
        }

        private void PART_ColorPickerGradient_Loaded(object sender, RoutedEventArgs e)
        {
            SetCursor();
        }

        private void PART_AkrColorPicker_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            PART_GradientSelector.Visibility = Visibility.Visible;
            if (OnlySimpleColor)
            {
                PART_GradientSelector.Visibility = Visibility.Hidden;
            }
        }

        private void PART_SliderOpacity_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                if (SimpleSolidColorBrush != null)
                {
                    SimpleSolidColorBrush = PART_SolidColorBrush;
                }
            }
            catch
            {

            }
        }

        private void PART_SolidColorBrush_Changed(object sender, EventArgs e)
        {
            try
            {
                if (SimpleSolidColorBrush != null)
                {
                    SimpleSolidColorBrush = PART_SolidColorBrush;
                }
            }
            catch
            {

            }
        }
    }
}