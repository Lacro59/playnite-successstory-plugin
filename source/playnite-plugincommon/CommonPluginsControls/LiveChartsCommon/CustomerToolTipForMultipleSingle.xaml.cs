using LiveCharts;
using LiveCharts.Wpf;
using System.ComponentModel;


namespace CommonPluginsControls.LiveChartsCommon
{
    /// <summary>
    /// Logique d'interaction pour CustomerForMultipleSingle.xaml
    /// </summary>
    public partial class CustomerToolTipForMultipleSingle : IChartTooltip
    {
        public TooltipSelectionMode? SelectionMode { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;


        #region Properties
        private TooltipData _data;
        public TooltipData Data
        {
            get
            {
                return _data;
            }
            set
            {
                _data = value;
                OnPropertyChanged("Data");
            }
        }
        #endregion


        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }


        public CustomerToolTipForMultipleSingle()
        {
            InitializeComponent();

            //LiveCharts will inject the tooltip data in the Data property
            //your job is only to display this data as required

            DataContext = this;
        }
    }
}
