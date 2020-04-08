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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Windows.Media.Effects;
using static System.Windows.Forms.Screen;

namespace ACNginxConsole
{

    /// <summary>
    /// FocalDepthHover.xaml 的交互逻辑
    /// </summary>
    public partial class FocalDepthHover : Window
    {
        // 当前的模型基于 2D
        // TODO: 拥有摄像机功能的 WPF 3D

        public static double HOVER_TIME = 10;       //悬浮时间
        public static double FocalPt_inSize = 32;   //焦点对应的字号
        public static int BLUR_MAX = 10;            //最大模糊程度
        public static int LAYER_NUM = 4;            //层数
        public static double INIT_TOP;              //开始顶距
        public static double DECR_FAC = 0.9;        //缩小系数

        public List<HoverLayer> hoverLayers;

        // 悬浮弹幕类
        public class HoverLayer
        {
            private double textSize;    //字号
            private double layerTop;    //顶距

            //在本模型中，使用平方反比律来推算高斯模糊程度
            //字号和高斯模糊程度都是平方量，两者直接成比例关系

            private System.Windows.Controls.Primitives.Thumb layer_thumb;

            public double TextSize
            {
                get { return textSize; }
                set { textSize = value; }
            }

            public double LayerTop
            {
                get { return layerTop; }
                set { layerTop = value; }
            }

            public System.Windows.Controls.Primitives.Thumb Layer_Thumb
            {
                get { return layer_thumb; }
                set { layer_thumb = value; }
            }

            public double Blur  //高斯模糊程度
            {
                get {
                    return (1 - textSize / FocalPt_inSize) * BLUR_MAX;
                }
            }

            public double Hover_time
            {
                get
                {   //最前面的将是 HOVER_TIME/2 , 0大小的字符其时间为 HOVER_TIME
                    return textSize / FocalPt_inSize * (-HOVER_TIME / 2) + HOVER_TIME;
                }
            }

            HoverLayer() { }
            /// <summary>
            /// 创建一层悬浮层
            /// </summary>
            /// <param name="Text_Size"></param>
            /// <param name="Layer_Top"></param>
            public HoverLayer(double Text_Size,double Layer_Top) {
                TextSize = Text_Size;
                LayerTop = Layer_Top;
                layer_thumb = new System.Windows.Controls.Primitives.Thumb();
            }

        }

        public FocalDepthHover()
        {
            InitializeComponent();

            //加载时显示在第二屏幕上
            if (AllScreens.Length > 1)
            {//第二屏幕
                Left = PrimaryScreen.WorkingArea.Width;
                Top = 0;
                WindowState = WindowState.Maximized;
            }
            else
            {
                WindowState = WindowState.Maximized;
            }

            //添加接收事件
            MainWindow.ReceivedDanmu += ReceiveDanmu;

            current_order = 0;

        }

        int current_order;  //层

        //Queue<Label> danmakuLabels = new Queue<Label>();

        /// <summary>
        /// 接收到弹幕 该弹幕将会被立刻打在公屏上
        /// </summary>
        private void ReceiveDanmu(object sender, ReceivedDanmuArgs e)
        {
            System.Diagnostics.Debug.WriteLine(e.Danmu.Danmaku);
            //接收到

            //创建对象
            Label label = new Label();
            label.Content = e.Danmu.Danmaku;
            label.Foreground = Brushes.White;

            //初始状态
            GridCanvas.Children.Add(label);
            Canvas.SetLeft(label, GridCanvas.Width);

            //主播和房管优先放在第一层
            int temp_ord = (e.Danmu.IsAdmin ? 0 : current_order);
            
            Canvas.SetTop(label, hoverLayers.ElementAt(temp_ord).LayerTop);
            label.FontSize = hoverLayers.ElementAt(temp_ord).TextSize;
            label.Effect = new BlurEffect() { Radius = hoverLayers.ElementAt(temp_ord).Blur };
            
            //故事板
            Storyboard storyboard = new Storyboard();

            //添加X轴方向的动画
            DoubleAnimation doubleAnimation = new DoubleAnimation(
                this.Width, -label.Content.ToString().Length* hoverLayers.ElementAt(temp_ord).TextSize / 0.75 , 
                new Duration(TimeSpan.FromSeconds(hoverLayers.ElementAt(temp_ord).Hover_time)));
            Storyboard.SetTarget(doubleAnimation, label);
            Storyboard.SetTargetProperty(doubleAnimation, new PropertyPath("(Canvas.Left)"));
            storyboard.Children.Add(doubleAnimation);

            //故事板即将开始
            storyboard.Completed += new EventHandler(Storyboard_over);
            storyboard.Begin();
            
            //danmakuLabels.Enqueue(label);

            if (current_order == LAYER_NUM - 1)
                current_order = 0;
            else
                ++current_order;
        }

        private void Storyboard_over(object sender,EventArgs e)
        {
            //寻找绑定的对象
            Label LabeltoRemove = 
                Storyboard.GetTarget((sender as ClockGroup).Timeline.Children[0]) as Label;

            //先进先出一定移除最先进的。队列不再使用，改为寻找发送者。
            //Label LabeltoRemove = danmakuLabels.Dequeue();

            GridCanvas.Children.Remove(LabeltoRemove);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            INIT_TOP = this.ActualHeight / 2;
            Regen_Layers();
        }

        private void Regen_Layers()
        {
            hoverLayers = new List<HoverLayer>();

            double temp_ts = FocalPt_inSize;
            double tempTop = INIT_TOP; //初始高度
            for (int i = 0; i < LAYER_NUM; ++i)
            {
                HoverLayer layer = new HoverLayer(temp_ts, tempTop);
                layer.Layer_Thumb.Width = this.ActualWidth;
                layer.Layer_Thumb.Height = temp_ts / 0.75;
                layer.Layer_Thumb.Opacity = 0;
                layer.Layer_Thumb.DragDelta += ThumbLayer_DragDelta;

                hoverLayers.Add(layer);
                ThumbCanvas.Children.Add(layer.Layer_Thumb);
                Canvas.SetLeft(layer.Layer_Thumb, 0);
                Canvas.SetTop(layer.Layer_Thumb, tempTop);

                temp_ts *= DECR_FAC;
                tempTop -= temp_ts / 0.75 + 10;
            }
        }

        //只需要进行纵向移动
        private void ThumbLayer_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
            var thumb = sender as System.Windows.Controls.Primitives.Thumb;
            double newTop = Canvas.GetTop(thumb) + e.VerticalChange;
            Canvas.SetTop(thumb, newTop);

            //使用高度识别，不是唯一标识符了，但是只要缩小系数不为1就可以。
            //之后可以使用派生模板的方法自己制作
            for (int i=0;i<hoverLayers.Count;++i)
            {
                if (thumb.Height == hoverLayers.ElementAt(i).TextSize / 0.75)
                {
                    hoverLayers.ElementAt(i).LayerTop = newTop;
                    break;
                }
            }

        }
    }
}
