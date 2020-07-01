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
using System.Windows.Shapes;

namespace ACNginxConsole
{
    /// <summary>
    /// DanmakuEntry.xaml 的交互逻辑
    /// </summary>
    public partial class DanmakuEntry : Window
    {

        public DanmakuEntry()
        {
            InitializeComponent();
        }

        bool result = false;

        private void AddContinue()
        {
            string website = textBoxWebsite.Text.ToString();
            if (website == "")
            {
                labelDError.Content = "输入不能为空。";
                labelDError.Visibility = Visibility.Visible;
                textBoxWebsite.BorderBrush = Brushes.Red;
                return;
            }
            int addtemp = MainWindow.Add_DanmakuConfig(website);
            switch (addtemp)
            {
                case 0: result = true; this.Close(); break;
                case -1:
                    labelDError.Content = "API连接错误(-1)，检查网址是否正确以及是否联网正常。";
                    labelDError.Visibility = Visibility.Visible;
                    textBoxWebsite.BorderBrush = Brushes.Red;
                    break;
                case -2:
                    labelDError.Content = "API解析错误(-2)，请联系作者。";
                    labelDError.Visibility = Visibility.Visible;
                    textBoxWebsite.BorderBrush = Brushes.Red;
                    break;
            }
           
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            AddContinue();
        }

        private void TextBoxWebsite_TextChanged(object sender, TextChangedEventArgs e)
        {
            var gray = new SolidColorBrush(Color.FromArgb(100, 171, 173, 179));
            textBoxWebsite.BorderBrush = gray;
            labelDError.Visibility = Visibility.Hidden;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            MainWindow.d_ok = result;
        }

        private void Grid_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) AddContinue();
        }
    }
}
