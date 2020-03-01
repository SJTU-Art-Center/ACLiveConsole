using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ACNginxConsole
{
    /// <summary>
    /// Monitor.xaml 的交互逻辑
    /// </summary>
    public partial class Monitor : Page
    {
        public Monitor()
        {
            InitializeComponent();
        }

        private void TextBoxName_TextChanged(object sender, TextChangedEventArgs e)
        {
            labelCaption.Content = "+ 添加监视器： " + textBoxName.Text;
        }
    }
}
