using System;
using System.Collections.Generic;
using System.IO;
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
using Vlc.DotNet.Wpf;

namespace ACNginxConsole
{
    /// <summary>
    /// Monitoring.xaml 的交互逻辑
    /// </summary>
    public partial class Monitoring : UserControl
    {
        public Monitoring()
        {
            InitializeComponent();

            var vlcLibDirectory = new DirectoryInfo(System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "libvlc\\" + (IntPtr.Size == 4 ? "win-x86" : "win-x64")));

            var options = new string[]
            {
                // VLC options
                "--file-logging","-vvv"
            };

            this.MyControl.SourceProvider.CreatePlayer(vlcLibDirectory, options);

            //Plug-in Complete.
            //Load libvlc Lib
            //this.MyControl.SourceProvider.MediaPlayer.Play("LiveTest.mp4");

        }
    }
}
