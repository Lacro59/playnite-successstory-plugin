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
    /// Logique d'interaction pour ComboBoxRemovable.xaml
    /// </summary>
    public partial class ComboBoxRemovable : ComboBox
    {
        public event RoutedEventHandler ClearEvent;


        public ComboBoxRemovable()
        {
            InitializeComponent();
        }


        private void PART_ClearButton_Click(object sender, RoutedEventArgs e)
        {
            this.Text = string.Empty;
            this.SelectedIndex = -1;

            this.ClearEvent?.Invoke(sender, e);
        }
    }
}
