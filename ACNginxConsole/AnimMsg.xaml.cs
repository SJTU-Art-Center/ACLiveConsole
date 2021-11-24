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
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace ACNginxConsole
{
    /// <summary>
    /// AnimMsg.xaml 的交互逻辑
    /// </summary>
    public partial class AnimMsg : Window
    {
        class MsgElement
        {
            //对话框对象
            //MsgBox msgb;

            //构造函数
            MsgElement() { //msgb = new MsgBox(); 
            }
            public MsgElement(string MsgText)
            {
                //msgb = new MsgBox(MsgText);
            }
        }

        public AnimMsg()
        {
            InitializeComponent();
            //MsgElement Msg1 = new MsgElement("别折腾了");
            //MsgElement Msg2 = new MsgElement("笑死我了");

            Storyboard styb = this.FindResource("StoryboardFloat") as Storyboard;
            
            styb.Begin();

            //MsgElement Msg1 = new MsgElement("别折腾了");
            //this.AddChild(Msg1);

        }
    }
}
