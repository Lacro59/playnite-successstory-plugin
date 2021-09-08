using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Calendar = System.Windows.Controls.Calendar;
using CalendarMode = System.Windows.Controls.CalendarMode;
using CalendarModeChangedEventArgs = System.Windows.Controls.CalendarModeChangedEventArgs;
using DatePicker = System.Windows.Controls.DatePicker;

namespace CommonPluginsShared.Behaviors
{
    // https://gist.github.com/crclayton/5aa7de782cfca6b59e616331af31c42c
    public class DatePickerCalendar
    {
        public static readonly DependencyProperty IsMonthYearProperty =
            DependencyProperty.RegisterAttached("IsMonthYear", typeof(bool), typeof(DatePickerCalendar),
                                                new PropertyMetadata(OnIsMonthYearChanged));

        public static bool GetIsMonthYear(DependencyObject dobj)
        {
            return (bool)dobj.GetValue(IsMonthYearProperty);
        }

        public static void SetIsMonthYear(DependencyObject dobj, bool value)
        {
            dobj.SetValue(IsMonthYearProperty, value);
        }

        private static void OnIsMonthYearChanged(DependencyObject dobj, DependencyPropertyChangedEventArgs e)
        {
            var datePicker = (DatePicker)dobj;

            Application.Current.Dispatcher
                .BeginInvoke(DispatcherPriority.Loaded,
                             new Action<DatePicker, DependencyPropertyChangedEventArgs>(SetCalendarEventHandlers),
                             datePicker, e);
        }

        private static void SetCalendarEventHandlers(DatePicker datePicker, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue == e.OldValue)
                return;

            if ((bool)e.NewValue)
            {
                datePicker.CalendarOpened += DatePickerOnCalendarOpened;
                datePicker.CalendarClosed += DatePickerOnCalendarClosed;
            }
            else
            {
                datePicker.CalendarOpened -= DatePickerOnCalendarOpened;
                datePicker.CalendarClosed -= DatePickerOnCalendarClosed;
            }
        }

        private static void DatePickerOnCalendarOpened(object sender, RoutedEventArgs routedEventArgs)
        {
            var calendar = GetDatePickerCalendar(sender);
            calendar.DisplayMode = CalendarMode.Year;

            calendar.DisplayModeChanged += CalendarOnDisplayModeChanged;
        }

        private static void DatePickerOnCalendarClosed(object sender, RoutedEventArgs routedEventArgs)
        {
            var datePicker = (DatePicker)sender;
            var calendar = GetDatePickerCalendar(sender);
            datePicker.SelectedDate = calendar.SelectedDate;

            calendar.DisplayModeChanged -= CalendarOnDisplayModeChanged;
        }

        private static void CalendarOnDisplayModeChanged(object sender, CalendarModeChangedEventArgs e)
        {
            var calendar = (Calendar)sender;
            if (calendar.DisplayMode != CalendarMode.Month)
                return;

            calendar.SelectedDate = GetSelectedCalendarDate(calendar.DisplayDate);

            var datePicker = GetCalendarsDatePicker(calendar);
            datePicker.IsDropDownOpen = false;
        }

        private static Calendar GetDatePickerCalendar(object sender)
        {
            var datePicker = (DatePicker)sender;
            var popup = (Popup)datePicker.Template.FindName("PART_Popup", datePicker);
            return ((Calendar)popup.Child);
        }

        private static DatePicker GetCalendarsDatePicker(FrameworkElement child)
        {
            var parent = (FrameworkElement)child.TryFindParent<DatePicker>();

            if (parent is DatePicker)
            {
                return (DatePicker)parent;
            }

            return GetCalendarsDatePicker(parent);
        }

        private static DateTime? GetSelectedCalendarDate(DateTime? selectedDate)
        {
            if (!selectedDate.HasValue)
                return null;
            return new DateTime(selectedDate.Value.Year, selectedDate.Value.Month, 1);
        }
    }

    public class DatePickerDateFormat
    {
        public static readonly DependencyProperty DateFormatProperty =
            DependencyProperty.RegisterAttached("DateFormat", typeof(string), typeof(DatePickerDateFormat),
                                                new PropertyMetadata(OnDateFormatChanged));

        public static string GetDateFormat(DependencyObject dobj)
        {
            return (string)dobj.GetValue(DateFormatProperty);
        }

        public static void SetDateFormat(DependencyObject dobj, string value)
        {
            dobj.SetValue(DateFormatProperty, value);
        }

        private static void OnDateFormatChanged(DependencyObject dobj, DependencyPropertyChangedEventArgs e)
        {
            var datePicker = (DatePicker)dobj;

            Application.Current.Dispatcher.BeginInvoke(
                DispatcherPriority.Loaded, new Action<DatePicker>(ApplyDateFormat), datePicker);
        }
        private static void ApplyDateFormat(DatePicker datePicker)
        {
            var binding = new Binding("SelectedDate")
            {
                RelativeSource = new RelativeSource { AncestorType = typeof(DatePicker) },
                Converter = new DatePickerDateTimeConverter(),
                ConverterParameter = new Tuple<DatePicker, string>(datePicker, GetDateFormat(datePicker)),
                StringFormat = GetDateFormat(datePicker) // This is also new but didnt seem to help
            };

            var textBox = GetTemplateTextBox(datePicker);
            textBox.SetBinding(TextBox.TextProperty, binding);

            textBox.PreviewKeyDown -= TextBoxOnPreviewKeyDown;
            textBox.PreviewKeyDown += TextBoxOnPreviewKeyDown;

            var dropDownButton = GetTemplateButton(datePicker);

            datePicker.CalendarOpened -= DatePickerOnCalendarOpened;
            datePicker.CalendarOpened += DatePickerOnCalendarOpened;

            // Handle Dropdownbutton PreviewMouseUp to prevent issue of flickering textboxes
            dropDownButton.PreviewMouseUp -= DropDownButtonPreviewMouseUp;
            dropDownButton.PreviewMouseUp += DropDownButtonPreviewMouseUp;
        }

        private static ButtonBase GetTemplateButton(DatePicker datePicker)
        {
            return (ButtonBase)datePicker.Template.FindName("PART_Button", datePicker);
        }


        /// <summary>
        ///     Prevents a bug in the DatePicker, where clicking the Dropdown open button results in Text being set to default formatting regardless of StringFormat or binding overrides
        /// </summary>
        private static void DropDownButtonPreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            var fe = sender as FrameworkElement;
            if (fe == null) return;

            var datePicker = fe.TryFindParent<DatePicker>();
            if (datePicker == null || datePicker.SelectedDate == null) return;

            var dropDownButton = GetTemplateButton(datePicker);

            // Dropdown button was clicked
            if (e.OriginalSource == dropDownButton && datePicker.IsDropDownOpen == false)
            {
                // Open dropdown
                datePicker.SetCurrentValue(DatePicker.IsDropDownOpenProperty, true);

                // Mimic everything else in the standard DatePicker dropdown opening *except* setting textbox value 
                datePicker.SetCurrentValue(DatePicker.DisplayDateProperty, datePicker.SelectedDate.Value);

                // Important otherwise calendar does not work
                dropDownButton.ReleaseMouseCapture();

                // Prevent datePicker.cs from handling this event 
                e.Handled = true;
            }
        }



        private static TextBox GetTemplateTextBox(Control control)
        {
            control.ApplyTemplate();
            return (TextBox)control?.Template?.FindName("PART_TextBox", control);
        }

        private static void TextBoxOnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Return)
                return;

            /* DatePicker subscribes to its TextBox's KeyDown event to set its SelectedDate if Key.Return was
             * pressed. When this happens its text will be the result of its internal date parsing until it
             * loses focus or another date is selected. A workaround is to stop the KeyDown event bubbling up
             * and handling setting the DatePicker.SelectedDate. */

            e.Handled = true;

            var textBox = (TextBox)sender;
            var datePicker = (DatePicker)textBox.TemplatedParent;
            var dateStr = textBox.Text;
            var formatStr = GetDateFormat(datePicker);
            datePicker.SelectedDate = DatePickerDateTimeConverter.StringToDateTime(datePicker, formatStr, dateStr);
        }

        private static void DatePickerOnCalendarOpened(object sender, RoutedEventArgs e)
        {
            /* When DatePicker's TextBox is not focused and its Calendar is opened by clicking its calendar button
             * its text will be the result of its internal date parsing until its TextBox is focused and another
             * date is selected. A workaround is to set this string when it is opened. */

            var datePicker = (DatePicker)sender;
            var textBox = GetTemplateTextBox(datePicker);
            var formatStr = GetDateFormat(datePicker);
            textBox.Text = DatePickerDateTimeConverter.DateTimeToString(formatStr, datePicker.SelectedDate);
        }

        private class DatePickerDateTimeConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                var formatStr = ((Tuple<DatePicker, string>)parameter).Item2;
                var selectedDate = (DateTime?)value;
                return DateTimeToString(formatStr, selectedDate);
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                var tupleParam = ((Tuple<DatePicker, string>)parameter);
                var dateStr = (string)value;
                return StringToDateTime(tupleParam.Item1, tupleParam.Item2, dateStr);
            }

            public static string DateTimeToString(string formatStr, DateTime? selectedDate)
            {
                return selectedDate.HasValue ? selectedDate.Value.ToString(formatStr) : null;
            }

            public static DateTime? StringToDateTime(DatePicker datePicker, string formatStr, string dateStr)
            {
                DateTime date;
                var canParse = DateTime.TryParseExact(dateStr, formatStr, CultureInfo.CurrentCulture,
                                                      DateTimeStyles.None, out date);

                if (!canParse)
                    canParse = DateTime.TryParse(dateStr, CultureInfo.CurrentCulture, DateTimeStyles.None, out date);

                return canParse ? date : datePicker.SelectedDate;
            }


        }

    }

    public static class CalendarCommands
    {
        private static readonly SelectTodayCommand _selectTodayCommand = new SelectTodayCommand();

        public static ICommand SelectToday
        {
            get { return _selectTodayCommand; }
        }

        private sealed class SelectTodayCommand : ICommand
        {
            public event EventHandler CanExecuteChanged
            {
                add { CommandManager.RequerySuggested += value; }
                remove { CommandManager.RequerySuggested -= value; }
            }

            public bool CanExecute(object parameter)
            {
                return parameter is Calendar;
            }

            public void Execute(object parameter)
            {
                var calendar = parameter as Calendar;
                if (calendar == null)
                    return;

                var today = DateTime.Today;
                calendar.SelectedDate = today;
                calendar.DisplayDate = today;


                var parent = (FrameworkElement)calendar.TryFindParent<DatePicker>();
                if (parent != null)
                {
                    ((DatePicker)parent).IsDropDownOpen = false;
                }
            }
        }
    }


    public static class FEExten
    {
        /// <summary>
        /// Finds a parent of a given item on the visual tree.
        /// </summary>
        /// <typeparam name="T">The type of the queried item.</typeparam>
        /// <param name="child">A direct or indirect child of the
        /// queried item.</param>
        /// <returns>The first parent item that matches the submitted
        /// type parameter. If not matching item can be found, a null
        /// reference is being returned.</returns>
        public static T TryFindParent<T>(this DependencyObject child)
            where T : DependencyObject
        {
            //get parent item
            DependencyObject parentObject = GetParentObject(child);

            //we've reached the end of the tree
            if (parentObject == null) return null;

            //check if the parent matches the type we're looking for
            T parent = parentObject as T;
            if (parent != null)
            {
                return parent;
            }
            else
            {
                //use recursion to proceed with next level
                return TryFindParent<T>(parentObject);
            }
        }

        /// <summary>
        /// This method is an alternative to WPF's
        /// <see cref="VisualTreeHelper.GetParent"/> method, which also
        /// supports content elements. Keep in mind that for content element,
        /// this method falls back to the logical tree of the element!
        /// </summary>
        /// <param name="child">The item to be processed.</param>
        /// <returns>The submitted item's parent, if available. Otherwise
        /// null.</returns>
        public static DependencyObject GetParentObject(this DependencyObject child)
        {
            if (child == null) return null;

            //handle content elements separately
            ContentElement contentElement = child as ContentElement;
            if (contentElement != null)
            {
                DependencyObject parent = ContentOperations.GetParent(contentElement);
                if (parent != null) return parent;

                FrameworkContentElement fce = contentElement as FrameworkContentElement;
                return fce != null ? fce.Parent : null;
            }

            //also try searching for parent in framework elements (such as DockPanel, etc)
            FrameworkElement frameworkElement = child as FrameworkElement;
            if (frameworkElement != null)
            {
                DependencyObject parent = frameworkElement.Parent;
                if (parent != null) return parent;
            }

            //if it's not a ContentElement/FrameworkElement, rely on VisualTreeHelper
            return VisualTreeHelper.GetParent(child);
        }
    }
}
