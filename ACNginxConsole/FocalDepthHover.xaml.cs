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
using System.ComponentModel;

namespace ACNginxConsole
{

    /// <summary>
    /// FocalDepthHover.xaml 的交互逻辑
    /// </summary>
    public partial class FocalDepthHover : Window
    {
        // 当前的模型基于 2D
        // TODO: 拥有摄像机功能的 WPF 3D

        public enum DanmuStyle { Plain, Bubble, BubbleFloat, BubbleCorner };
        public static DanmuStyle DM_Style = DanmuStyle.Plain;

        public static double HOVER_TIME = 10;       //悬浮时间
        public static double FocalPt_inSize = 32;   //焦点对应的字号
        public static int BLUR_MAX = 10;            //最大模糊程度
        public static int LAYER_NUM = 4;            //层数
        public static double INIT_TOP;              //开始顶距
        public static double DECR_FAC = 0.9;        //缩小系数
        public static bool From_Front = false;      //从前往后
        public static Color ForeColor;              //字体颜色

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

        //气泡消息框类
        public class PeakedAdorner
        {

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

            ForeColor = Color.FromArgb(255, 255, 255, 255);
        }

        int current_order;  //层

        //Queue<Label> danmakuLabels = new Queue<Label>();

        int seeds = 0;

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
            
            label.Foreground = new SolidColorBrush((this.FindResource("BubbleFore") as SolidColorBrush).Color);
            switch (DM_Style)
            {
                
                case DanmuStyle.Bubble:
                case DanmuStyle.Plain:
                    if (DM_Style == DanmuStyle.Bubble)
                        label.Style = this.FindResource("tipLable") as Style;
                
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
                        this.Width, -label.Content.ToString().Length * hoverLayers.ElementAt(temp_ord).TextSize / 0.75,
                        new Duration(TimeSpan.FromSeconds(hoverLayers.ElementAt(temp_ord).Hover_time)));
                    Storyboard.SetTarget(doubleAnimation, label);
                    Storyboard.SetTargetProperty(doubleAnimation, new PropertyPath("(Canvas.Left)"));
                    storyboard.Children.Add(doubleAnimation);

                    //故事板即将开始
                    storyboard.Completed += new EventHandler(Storyboard_over);
                    storyboard.Begin();

                    //danmakuLabels.Enqueue(label);

                    if (From_Front)
                        current_order = (current_order == LAYER_NUM - 1 ? 0 : ++current_order);
                    else
                        current_order = (current_order == 0 ? LAYER_NUM - 1 : --current_order);

                    break;

                case DanmuStyle.BubbleFloat:
                    Random rd = new Random(seeds++);

                    label.Style = this.FindResource("tipLable") as Style;
                    GridCanvas.Children.Add(label);
                    double myleft = rd.Next(0, (int)this.Width * 7 / 8);
                    double mytop = rd.Next((int)this.Height / 2, (int)this.Height * 3 / 4);

                    int temp_ord_f = (e.Danmu.IsAdmin ? 0 : current_order);

                    Canvas.SetLeft(label, myleft);
                    Canvas.SetTop(label, mytop);
                    label.FontSize = hoverLayers.ElementAt(temp_ord_f).TextSize;
                    label.Effect = new BlurEffect() { Radius = hoverLayers.ElementAt(temp_ord_f).Blur };

                    //故事板
                    Storyboard storyboard_Float = new Storyboard();

                    //淡入
                    DoubleAnimation doubleAnimation_ffIn = new DoubleAnimation(0, 1,
                        new Duration(TimeSpan.FromSeconds(hoverLayers.ElementAt(temp_ord_f).Hover_time * 1 / 4)));
                    Storyboard.SetTarget(doubleAnimation_ffIn, label);
                    Storyboard.SetTargetProperty(doubleAnimation_ffIn, new PropertyPath("(Label.Opacity)"));
                    doubleAnimation_ffIn.EasingFunction = new QuadraticEase()
                    {
                        EasingMode = EasingMode.EaseIn
                    };
                    storyboard_Float.Children.Add(doubleAnimation_ffIn);

                    //添加Y轴方向的动画
                    DoubleAnimation doubleAnimation_f = new DoubleAnimation(
                        mytop, mytop - this.Height / 3,
                        new Duration(TimeSpan.FromSeconds(hoverLayers.ElementAt(temp_ord_f).Hover_time)));
                    Storyboard.SetTarget(doubleAnimation_f, label);
                    Storyboard.SetTargetProperty(doubleAnimation_f, new PropertyPath("(Canvas.Top)"));
                    doubleAnimation_f.EasingFunction = new ExponentialEase()
                    {
                        EasingMode = EasingMode.EaseOut
                    };
                    storyboard_Float.Children.Add(doubleAnimation_f);

                    //淡出
                    DoubleAnimation doubleAnimation_ffOut = new DoubleAnimation(1, 0,
                        new Duration(TimeSpan.FromSeconds(hoverLayers.ElementAt(temp_ord_f).Hover_time)));
                    Storyboard.SetTarget(doubleAnimation_ffOut, label);
                    Storyboard.SetTargetProperty(doubleAnimation_ffOut, new PropertyPath("(Label.Opacity)"));
                    doubleAnimation_ffOut.EasingFunction = new QuadraticEase()
                    {
                        EasingMode = EasingMode.EaseOut
                    };
                    storyboard_Float.Children.Add(doubleAnimation_ffOut);

                    //故事板即将开始
                    storyboard_Float.Completed += new EventHandler(Storyboard_over);
                    storyboard_Float.Begin();

                    if (From_Front)
                        current_order = (current_order == LAYER_NUM - 1 ? 0 : ++current_order);
                    else
                        current_order = (current_order == 0 ? LAYER_NUM - 1 : --current_order);

                    break;

                case DanmuStyle.BubbleCorner:
                    //单层：所有的都在第 0 层
                    //需要好好构造一下
                    label.Style = this.FindResource("tipLable") as Style;
                    GridCanvas.Children.Add(label);
                    double mytop_c = this.Height - hoverLayers.ElementAt(0).TextSize / 0.6;

                    Canvas.SetRight(label, 100);
                    Canvas.SetTop(label, mytop_c);
                    label.FontSize = hoverLayers.ElementAt(0).TextSize;
                    label.Effect = new BlurEffect() { Radius = hoverLayers.ElementAt(0).Blur };

                    //故事板
                    Storyboard storyboard_Corner = new Storyboard();

                    //添加Y轴方向的动画
                    DoubleAnimation doubleAnimation_c = new DoubleAnimation(
                        mytop_c, mytop_c - this.Height / 3,
                        new Duration(TimeSpan.FromSeconds(hoverLayers.ElementAt(0).Hover_time)));
                    Storyboard.SetTarget(doubleAnimation_c, label);
                    Storyboard.SetTargetProperty(doubleAnimation_c, new PropertyPath("(Canvas.Top)"));
                    storyboard_Corner.Children.Add(doubleAnimation_c);

                    //故事板即将开始
                    storyboard_Corner.Completed += new EventHandler(Storyboard_over);
                    storyboard_Corner.Begin();

                    break;
            }
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
            INIT_TOP = this.ActualHeight / 3;
            Regen_Layers();
        }

        private void Regen_Layers()
        {
            current_order = (From_Front ? 0 : LAYER_NUM - 1);

            hoverLayers = new List<HoverLayer>();

            double temp_ts = FocalPt_inSize;
            double tempTop = INIT_TOP; 
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

            //识别调整层
            for (int i=0;i<hoverLayers.Count;++i)
            {
                if(thumb == hoverLayers.ElementAt(i).Layer_Thumb)
                {
                    hoverLayers.ElementAt(i).LayerTop = newTop;
                    break;
                }
            }

        }
    }
}
