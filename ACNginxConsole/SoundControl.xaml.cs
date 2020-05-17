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
    /// SoundControl.xaml 的交互逻辑
    /// </summary>
    public partial class SoundControl : Window
    {
        public delegate void VolChangedEvt(object sender, VolChangedArgs e);

        public static event VolChangedEvt VCE;

        public static bool MainChange;

        List<SoundController> soundControllers;

        public SoundControl()
        {
            InitializeComponent();
            soundControllers = new List<SoundController>()
            {
                new SoundController(true,false,false),
                new SoundController(true,false,false),
                new SoundController(true,false,false),
            };
            SliderUpdate();
            NameUpdate();
            MainChange = false;
        }

        public class SoundController
        {
            private bool on;
            private bool mute;
            private bool solo;

            public bool On
            {
                get { return on; }
                set { on = value; }
            }

            public bool Mute
            {
                get { return mute; }
                set { mute = value; }
            }

            public bool Solo
            {
                get { return solo; }
                set { solo = value; }
            }

            public SoundController(bool on,bool mu,bool so)
            {
                On = on;
                Mute = mu;
                Solo = so;
            }

        }

        private void SliderUpdate()
        {
            Slider1.Value = MainWindow.Monitors.ElementAt(0).Volume;
            Slider2.Value = MainWindow.Monitors.ElementAt(1).Volume;
            Slider3.Value = MainWindow.Monitors.ElementAt(2).Volume;
        }

        private void SetVolume()
        {
            MainWindow.Monitors.ElementAt(0).Volume = (int)Slider1.Value;
            MainWindow.Monitors.ElementAt(1).Volume = (int)Slider2.Value;
            MainWindow.Monitors.ElementAt(2).Volume = (int)Slider3.Value;
        }

        private void NameUpdate()
        {
            labelSN1.Content =
                (MainWindow.Monitors.ElementAt(0).PlayId < MainWindow.configdata.Count) ?
                MainWindow.configdata.ElementAt(MainWindow.Monitors.ElementAt(0).PlayId).SourceName : "";
            labelSN2.Content =
                (MainWindow.Monitors.ElementAt(1).PlayId < MainWindow.configdata.Count) ?
                MainWindow.configdata.ElementAt(MainWindow.Monitors.ElementAt(1).PlayId).SourceName : "";
            labelSN3.Content =
                (MainWindow.Monitors.ElementAt(2).PlayId < MainWindow.configdata.Count) ?
                MainWindow.configdata.ElementAt(MainWindow.Monitors.ElementAt(2).PlayId).SourceName : "";
        }

        private void Update()
        {
            if (soundControllers.ElementAt(0).On)
            {
                buttonON1.Background = ONBrush;
                buttonON1.Foreground = BlackBrush;
                MainWindow.Monitors.ElementAt(0).Volume = (int)Slider1.Value;
            }
            else
            {
                buttonON1.Background = TranBrush;
                buttonON1.Foreground = WhiteBrush;
                MainWindow.Monitors.ElementAt(0).Volume = 0;
            }
            Slider1.IsEnabled = soundControllers.ElementAt(0).On;

            if (soundControllers.ElementAt(0).Mute)
            {
                buttonMute1.Background = MuteBrush;
                Slider1.Value = 0;
            }
            else
                buttonMute1.Background = TranBrush;

            if (soundControllers.ElementAt(0).Solo)
            {
                buttonSolo1.Background = SoloBrush;
            }
            else
                buttonSolo1.Background = TranBrush;
                   
            if (soundControllers.ElementAt(1).On)
            {
                buttonON2.Background = ONBrush;
                buttonON2.Foreground = BlackBrush;
                MainWindow.Monitors.ElementAt(1).Volume = (int)Slider2.Value;
            }
            else
            {
                buttonON2.Background = TranBrush;
                buttonON2.Foreground = WhiteBrush;
                MainWindow.Monitors.ElementAt(1).Volume = 0;
                
            }
            Slider2.IsEnabled = soundControllers.ElementAt(1).On;

            if (soundControllers.ElementAt(1).Mute)
            {
                buttonMute2.Background = MuteBrush;
                Slider2.Value = 0;
            }
            else
                buttonMute2.Background = TranBrush;

            if (soundControllers.ElementAt(1).Solo)
            {
                buttonSolo2.Background = SoloBrush;
            }
            else
                buttonSolo2.Background = TranBrush;
                    
            if (soundControllers.ElementAt(2).On)
            {
                buttonON3.Background = ONBrush;
                buttonON3.Foreground = BlackBrush;
                MainWindow.Monitors.ElementAt(2).Volume = (int)Slider3.Value;
            }
            else
            {
                buttonON3.Background = TranBrush;
                buttonON3.Foreground = WhiteBrush;
                MainWindow.Monitors.ElementAt(2).Volume = 0;
            }
            Slider3.IsEnabled = soundControllers.ElementAt(2).On;

            if (soundControllers.ElementAt(2).Mute)
            {
                buttonMute3.Background = MuteBrush;
                Slider3.Value = 0;
            }
            else
                buttonMute3.Background = TranBrush;

            if (soundControllers.ElementAt(2).Solo)
                buttonSolo3.Background = SoloBrush;
            else
                buttonSolo3.Background = TranBrush;
                  

        }

        SolidColorBrush TranBrush = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
        SolidColorBrush ONBrush = new SolidColorBrush(Color.FromArgb(255, 250, 235, 91));
        SolidColorBrush MuteBrush = new SolidColorBrush(Color.FromArgb(255, 60, 119, 127));
        SolidColorBrush SoloBrush = new SolidColorBrush(Color.FromArgb(255, 79, 115, 162));
        SolidColorBrush BlackBrush = new SolidColorBrush(Color.FromArgb(255, 0, 0, 0));
        SolidColorBrush WhiteBrush = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));

        private void buttonON1_Click(object sender, RoutedEventArgs e)
        {
            soundControllers.ElementAt(0).On = !soundControllers.ElementAt(0).On;
            Update();
        }

        private void buttonON2_Click(object sender, RoutedEventArgs e)
        {
            soundControllers.ElementAt(1).On = !soundControllers.ElementAt(1).On;
            Update();
        }

        private void buttonON3_Click(object sender, RoutedEventArgs e)
        {
            soundControllers.ElementAt(2).On = !soundControllers.ElementAt(2).On;
            Update();
        }

        private void buttonMute1_Click(object sender, RoutedEventArgs e)
        {
            if(!soundControllers.ElementAt(0).Mute)
                soundControllers.ElementAt(0).Mute = true;
            Update();
        }

        private void buttonMute2_Click(object sender, RoutedEventArgs e)
        {
            if (!soundControllers.ElementAt(1).Mute)
                soundControllers.ElementAt(1).Mute = true;
            Update();
        }

        private void buttonMute3_Click(object sender, RoutedEventArgs e)
        {
            if (!soundControllers.ElementAt(2).Mute)
                soundControllers.ElementAt(2).Mute = true;
            Update();
        }

        private void Solo()
        {
            if (!soundControllers.ElementAt(0).Solo &&
                !soundControllers.ElementAt(1).Solo &&
                !soundControllers.ElementAt(2).Solo)
            {
                soundControllers.ElementAt(0).On = true;
                soundControllers.ElementAt(1).On = true;
                soundControllers.ElementAt(2).On = true;
            }
            else
            {
                soundControllers.ElementAt(0).On = soundControllers.ElementAt(0).Solo;
                soundControllers.ElementAt(1).On = soundControllers.ElementAt(1).Solo;
                soundControllers.ElementAt(2).On = soundControllers.ElementAt(2).Solo;
            }
        }

        private void buttonSolo1_Click(object sender, RoutedEventArgs e)
        {
            soundControllers.ElementAt(0).Solo = !soundControllers.ElementAt(0).Solo;
            Solo();
            Update();
        }

        private void buttonSolo2_Click(object sender, RoutedEventArgs e)
        {
            soundControllers.ElementAt(1).Solo = !soundControllers.ElementAt(1).Solo;
            Solo();
            Update();
        }

        private void buttonSolo3_Click(object sender, RoutedEventArgs e)
        {
            soundControllers.ElementAt(2).Solo = !soundControllers.ElementAt(2).Solo;
            Solo();
            Update();
        }

        private void Slider1_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!MainChange) {
                if (Slider1.Value > 0)
                {
                    soundControllers.ElementAt(0).Mute = false;
                }
                else
                {
                    soundControllers.ElementAt(0).Mute = true;
                }
                //MainWindow.Monitors.ElementAt(0).Volume = (int)Slider1.Value;
                SetVolume();
                VCE?.Invoke(this, new VolChangedArgs { });
                Update();
            }
            
        }

        private void Slider2_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!MainChange)
            {
                if (Slider2.Value > 0)
                {
                    soundControllers.ElementAt(1).Mute = false;
                }
                else
                {
                    soundControllers.ElementAt(1).Mute = true;
                }
                //MainWindow.Monitors.ElementAt(1).Volume = (int)Slider2.Value;
                SetVolume();
                VCE?.Invoke(this, new VolChangedArgs { });
                Update();
            }
        }

        private void Slider3_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!MainChange)
            {
                if (Slider3.Value > 0)
                {
                    soundControllers.ElementAt(2).Mute = false;
                }
                else
                {
                    soundControllers.ElementAt(2).Mute = true;
                }
                //MainWindow.Monitors.ElementAt(2).Volume = (int)Slider3.Value;
                SetVolume();
                VCE?.Invoke(this, new VolChangedArgs { });
                Update();
            }
        }


    }
}
