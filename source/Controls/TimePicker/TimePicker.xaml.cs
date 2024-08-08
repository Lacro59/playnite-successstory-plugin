using CommonPluginsShared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace TemperatureMeasurementTool
{
    /// <summary>
    /// Interaktionslogik für TimePicker.xaml
    /// </summary>
    public partial class TimePicker : UserControl
    {
        public static readonly RoutedEvent TimeChangedEvent = EventManager.RegisterRoutedEvent("TimeChanged", RoutingStrategy.Direct, typeof(RoutedEventHandler), typeof(TimePicker));

        public event RoutedEventHandler TimeChanged
        {
            add { AddHandler(TimeChangedEvent, value); }
            remove { RemoveHandler(TimeChangedEvent, value); }
        }

        public static readonly DependencyProperty HourTimeProperty = DependencyProperty.Register(
            nameof(HourTime),
            typeof(string),
            typeof(TimePicker),
            new FrameworkPropertyMetadata(DateTime.Now.ToString("HH"))
        );
        public string HourTime
        {
            get => (string)GetValue(HourTimeProperty);
            set => SetValue(HourTimeProperty, value);
        }

        public static readonly DependencyProperty MinuteTimeProperty = DependencyProperty.Register(
            nameof(MinuteTime),
            typeof(string),
            typeof(TimePicker),
            new FrameworkPropertyMetadata(DateTime.Now.ToString("mm"))
        );
        public string MinuteTime
        {
            get => (string)GetValue(MinuteTimeProperty);
            set => SetValue(MinuteTimeProperty, value);
        }

        public static readonly DependencyProperty SecondTimeProperty = DependencyProperty.Register(
            nameof(SecondTime),
            typeof(string),
            typeof(TimePicker),
            new FrameworkPropertyMetadata(DateTime.Now.ToString("ss"))
        );
        public string SecondTime
        {
            get => (string)GetValue(SecondTimeProperty);
            set => SetValue(SecondTimeProperty, value);
        }

        public static readonly DependencyProperty TimeProperty = DependencyProperty.Register(
            nameof(Time),
            typeof(DateTime?),
            typeof(TimePicker),
            new FrameworkPropertyMetadata(null, NewTime)
        );
        public DateTime? Time
        {
            get => (DateTime?)GetValue(TimeProperty);
            set => SetValue(TimeProperty, value);
        }
        private static void NewTime(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            try
            {
                TimePicker control = (TimePicker)obj;
                if (args.NewValue != null && (DateTime)args.NewValue != default && !args.NewValue.ToString().Contains("0001"))
                {
                    control.HourTime = ((DateTime)args.NewValue).ToLocalTime().ToString("HH");
                    control.MinuteTime = ((DateTime)args.NewValue).ToLocalTime().ToString("mm");
                    control.SecondTime = ((DateTime)args.NewValue).ToLocalTime().ToString("ss");
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, "SuccessStory");
            }
        }

        public TimePicker()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Zählt die Stunden hoch
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnUpHour_OnClick(object sender, RoutedEventArgs e)
        {
            int value = Convert.ToInt32(Hour.Text);
            if (value < 23)
            {
                Hour.Text = (value + 1) <= 9 ? "0" + (++value).ToString() : (++value).ToString();
            }
            else if (value == 23)
            {
                Hour.Text = "01";
            }

            RaiseEvent(new RoutedEventArgs(TimeChangedEvent));
        }

        /// <summary>
        /// Zählt die Minuten hoch
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnUpMinute_OnClick(object sender, RoutedEventArgs e)
        {
            int value = Convert.ToInt32(Minute.Text);
            if (value < 59)
            {
                Minute.Text = (value + 1) <= 9 ? "0" + (++value).ToString() : (++value).ToString();
            }
            else if (value == 59)
            {
                Minute.Text = "01";
            }

            RaiseEvent(new RoutedEventArgs(TimeChangedEvent));
        }

        private void BtnUpSeconde_OnClick(object sender, RoutedEventArgs e)
        {
            int value = Convert.ToInt32(Seconde.Text);
            if (value < 59)
            {
                Seconde.Text = (value + 1) <= 9 ? "0" + (++value).ToString() : (++value).ToString();
            }
            else if (value == 59)
            {
                Seconde.Text = "01";
            }

            RaiseEvent(new RoutedEventArgs(TimeChangedEvent));
        }

        /// <summary>
        /// Zählt die Stunden runter
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnDownHour_OnClick(object sender, RoutedEventArgs e)
        {
            int value = Convert.ToInt32(Hour.Text);
            if (value > 0)
            {
                Hour.Text = (value - 1) <= 9 ? "0" + (--value).ToString() : (--value).ToString();
            }
            else if (value == 0)
            {
                Hour.Text = "23";
            }

            RaiseEvent(new RoutedEventArgs(TimeChangedEvent));
        }

        /// <summary>
        /// Zählt die Minuten runter
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnDownMinute_OnClick(object sender, RoutedEventArgs e)
        {
            int value = Convert.ToInt32(Minute.Text);
            if (value > 0)
            {
                Minute.Text = (value - 1) <= 9 ? "0" + (--value).ToString() : (--value).ToString();
            }
            else if (value == 0)
            {
                Minute.Text = "59";
            }

            RaiseEvent(new RoutedEventArgs(TimeChangedEvent));
        }

        private void BtnDownSeconde_OnClick(object sender, RoutedEventArgs e)
        {
            int value = Convert.ToInt32(Seconde.Text);
            if (value > 0)
            {
                Seconde.Text = (value - 1) <= 9 ? "0" + (--value).ToString() : (--value).ToString();
            }
            else if (value == 0)
            {
                Seconde.Text = "59";
            }

            RaiseEvent(new RoutedEventArgs(TimeChangedEvent));
        }

        /// <summary>
        /// ToDo: Change return Value to DateTime
        /// </summary>
        /// <returns></returns>
        public int GetValueAsDateTime()
        {
            return 1230;
        }

        /// <summary>
        /// Gibt den Wert zurück
        /// </summary>
        /// <returns></returns>
        public string GetValueAsString()
        {
            return Hour.Text + ":" + Minute.Text + ":" + Seconde.Text;
        }

        /// <summary>
        /// Setzt die Uhrzeit programmatically
        /// </summary>
        public void SetValueAsString(string hour, string minute, string seconde)
        {
            //TODO Check if hour and Minute is correct Value (Regex)
            Hour.Text = hour;
            Minute.Text = minute;
            Seconde.Text = seconde;

            RaiseEvent(new RoutedEventArgs(TimeChangedEvent));
        }

        /// <summary>
        /// Wenn das Stundenfeld Fokus erlangt
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Hour_OnGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            Hour.SelectAll();
        }

        /// <summary>
        /// Wenn das Minutenfeld Fokus erlangt
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Minute_OnGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            Minute.SelectAll();
        }

        private void Seconde_OnGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            Seconde.SelectAll();
        }

        /// <summary>
        /// Wenn der User die linke MausTaste in dieses Feld rein klickt
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Minute_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!Minute.IsKeyboardFocusWithin)
            {
                e.Handled = true;
                Minute.Focus();
            }
        }

        private void Seconde_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!Seconde.IsKeyboardFocusWithin)
            {
                e.Handled = true;
                Seconde.Focus();
            }
        }

        /// <summary>
        /// Wenn der User die linke MausTaste in dieses Feld rein klickt
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Hour_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!Hour.IsKeyboardFocusWithin)
            {
                e.Handled = true;
                Hour.Focus();
            }
        }

        /// <summary>
        /// Wenn der Fokus aus dem Minutenfeld kommt
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Minute_OnLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            int resultMinute = Convert.ToInt32(Minute.Text);
            if (resultMinute > 59)
            {
                Minute.Text = "59";
            }

            RaiseEvent(new RoutedEventArgs(TimeChangedEvent));
        }

        private void Seconde_OnLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            int resultSeconde = Convert.ToInt32(Seconde.Text);
            if (resultSeconde > 59)
            {
                Seconde.Text = "59";
            }

            RaiseEvent(new RoutedEventArgs(TimeChangedEvent));
        }

        /// <summary>
        /// Überprüft die EIngabder der Minute
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Minute_OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = Regex.IsMatch(e.Text, "[^0-9]+");

            RaiseEvent(new RoutedEventArgs(TimeChangedEvent));
        }

        private void Seconde_OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = Regex.IsMatch(e.Text, "[^0-9]+");

            RaiseEvent(new RoutedEventArgs(TimeChangedEvent));
        }

        /// <summary>
        /// Überprüft die Eingabe in das Stundenfeld
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Hour_OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = Regex.IsMatch(e.Text, "[^0-9]+");

            RaiseEvent(new RoutedEventArgs(TimeChangedEvent));
        }

        /// <summary>
        /// Wenn der Fokus aus dem Feld verschwindet
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Hour_OnLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            int resultHour = Convert.ToInt32(Hour.Text);
            if (resultHour > 23)
            {
                Hour.Text = "23";
            }

            RaiseEvent(new RoutedEventArgs(TimeChangedEvent));
        }

        private void UIElement_OnMouseEnter(object sender, MouseEventArgs e)
        {
            ((Button)sender).Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF00A8DE"));
        }

        private void UIElement_OnMouseLeave(object sender, MouseEventArgs e)
        {
            ((Button)sender).Foreground = Brushes.White;
        }
    }
}
