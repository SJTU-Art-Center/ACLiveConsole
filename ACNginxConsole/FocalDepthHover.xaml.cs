﻿using System;
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
//using System.Windows.Forms;

namespace ACNginxConsole
{

    /// <summary>
    /// FocalDepthHover.xaml 的交互逻辑
    /// </summary>
    public partial class FocalDepthHover : Window
    {
        // 当前的模型基于 2D
        // TODO: 拥有摄像机功能的 WPF 3D

        public static bool SettingModified = false;        //设置是否被修改

        public enum DanmuStyle { Plain, Bubble, BubbleFloat, BubbleCorner };
        public static DanmuStyle DM_Style = DanmuStyle.Plain;

        public static double HOVER_TIME = 10;       //悬浮时间
        public static double FocalPt_inSize = 32;   //焦点对应的字号
        public static double BLUR_MAX = 10;         //最大模糊程度
        public static int LAYER_NUM = 4;            //层数
        public static double INIT_TOP;              //开始顶距
        public static double DECR_FAC = 0.85;        //缩小系数
        public static bool From_Front = false;      //从前往后
        public static Color ForeColor;              //字体颜色
        public static FontFamily ForeFont;          //字体

        public List<HoverLayer> hoverLayers = new List<HoverLayer>();

        /// <summary>
        /// 悬浮弹幕类
        /// </summary>
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
            /// <param name="Text_Size">最大字号</param>
            /// <param name="Layer_Top">最低顶距</param>
            public HoverLayer(double Text_Size,double Layer_Top) {
                TextSize = Text_Size;
                LayerTop = Layer_Top;
                layer_thumb = new System.Windows.Controls.Primitives.Thumb();
            }

        }

        public DispatcherTimer CornerRefreshTimer = new DispatcherTimer();     //气泡右下刷新计时

        /// <summary>
        /// 初始化
        /// </summary>
        public FocalDepthHover()
        {
            InitializeComponent();

            //加载时显示在第二屏幕上
            if (AllScreens.Length > 1)
            {//第二屏幕
                Left = PrimaryScreen.WorkingArea.Width;
                Top = 0;   
            }
            else
            {
                Left = 0;
                Top = 0;
            }
            switch (Properties.Settings.Default.SizeMode)
            {
                case 0: WindowState = WindowState.Maximized;break;
                case 1: WindowState = WindowState.Normal;Width = 1920;Height = 1080;break;
                case 2: WindowState = WindowState.Normal;Width = 1280;Height = 720;break;
            }
            
            //添加接收事件
            MainWindow.ReceivedDanmu += ReceiveDanmu;

            ForeColor = Color.FromArgb(255, 255, 255, 255);

            CornerRefreshTimer.Tick += CornerRefreshTimer_Tick;

            

        }

        double myright_c;
        double mybottom_c;
        System.Windows.Controls.Primitives.Thumb corner_thumb;

        private void CornerRefreshTimer_Tick(object sender, EventArgs e)
        {
            if (danmakuLabels.Any())
            {
                Label label = danmakuLabels.Dequeue();

                label.Foreground = new SolidColorBrush((this.FindResource("BubbleFore") as SolidColorBrush).Color);

                label.Style = this.FindResource("tipLable") as Style;
                GridCanvas.Children.Add(label);
                Canvas.SetBottom(label, mybottom_c);
                Canvas.SetRight(label, myright_c);

                label.FontSize = 5;     //起点是5号字
                //没有模糊

                var Blur = label.Effect as BlurEffect;

                //故事板
                Storyboard storyboard_Corner = new Storyboard();

                //添加字号动画
                DoubleAnimation SizeAnim = new DoubleAnimation(
                    5, hoverLayers.ElementAt(0).TextSize, 
                    TimeSpan.FromSeconds(hoverLayers.ElementAt(0).Hover_time / 8))
                {
                    EasingFunction = new ExponentialEase()
                    {
                        Exponent = 9,
                        EasingMode = EasingMode.EaseOut
                    }
                };
                Storyboard.SetTarget(SizeAnim, label);
                Storyboard.SetTargetProperty(SizeAnim, new PropertyPath("(Label.FontSize)"));
                storyboard_Corner.Children.Add(SizeAnim);

                //添加Y轴方向的动画
                DoubleAnimation doubleAnimation_c = new DoubleAnimation(
                    mybottom_c, mybottom_c + this.Height / 3,
                    new Duration(TimeSpan.FromSeconds(hoverLayers.ElementAt(0).Hover_time)));
                Storyboard.SetTarget(doubleAnimation_c, label);
                Storyboard.SetTargetProperty(doubleAnimation_c, new PropertyPath("(Canvas.Bottom)"));
                storyboard_Corner.Children.Add(doubleAnimation_c);

                //添加淡出动画
                DoubleAnimation FadeOutAnim = new DoubleAnimation()
                {
                    From = 1,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(hoverLayers.ElementAt(0).Hover_time / 4),
                    BeginTime = TimeSpan.FromSeconds(hoverLayers.ElementAt(0).Hover_time / 4),
                };
                Storyboard.SetTarget(FadeOutAnim, label);
                Storyboard.SetTargetProperty(FadeOutAnim, new PropertyPath("(Label.Opacity)"));
                storyboard_Corner.Children.Add(FadeOutAnim);

                //故事板即将开始
                storyboard_Corner.Completed += new EventHandler(Storyboard_over);
                storyboard_Corner.Begin();
            }
        }

        //二维移动
        private void Corner_thumb_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
            var thumb = sender as System.Windows.Controls.Primitives.Thumb;
            myright_c = Canvas.GetRight(thumb) - e.HorizontalChange;
            mybottom_c = Canvas.GetBottom(thumb) - e.VerticalChange;
            Canvas.SetRight(thumb, myright_c);
            Canvas.SetBottom(thumb, mybottom_c);
        }

        int current_order;  //层

        Queue<Label> danmakuLabels = new Queue<Label>();    //气泡右下需要的弹幕队列

        int seeds = 0;  //随机种子

        /// <summary>
        /// 接收到弹幕 该弹幕将会被立刻打在公屏上
        /// </summary>
        private void ReceiveDanmu(object sender, ReceivedDanmuArgs e)
        {
            System.Diagnostics.Debug.WriteLine(e.Danmu.Danmaku);
            //接收到

            //设置是否被更改
            if (SettingModified)
            {
                Regen_Layers();
                SettingModified = false;
            }
            
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


                    if (From_Front)
                        current_order = (current_order == LAYER_NUM - 1 ? 0 : ++current_order);
                    else
                        current_order = (current_order == 0 ? LAYER_NUM - 1 : --current_order);

                    break;

                case DanmuStyle.BubbleFloat:
                    Random rd = new Random(seeds < 10000 ? ++seeds : 0);    //防止越界

                    label.Style = this.FindResource("tipLable") as Style;
                    GridCanvas.Children.Add(label);
                    double edge = (1 - INIT_TOP) / 2;
                    double myleft = rd.Next((int)(this.Width * edge), (int)(this.Width * (1 - edge)));
                    double mytop = rd.Next((int)(this.Height * edge), (int)(this.Height * (1 - edge)));

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
                    DoubleAnimation doubleAnimation_ffOut = new DoubleAnimation()
                    {
                        From = 1,
                        To = 0,
                        BeginTime = TimeSpan.FromSeconds(hoverLayers.ElementAt(temp_ord_f).Hover_time * 3 / 4),
                        Duration = TimeSpan.FromSeconds(hoverLayers.ElementAt(temp_ord_f).Hover_time * 1 / 4),
                        EasingFunction = new QuadraticEase() { EasingMode = EasingMode.EaseOut },
                    };
                    Storyboard.SetTarget(doubleAnimation_ffOut, label);
                    Storyboard.SetTargetProperty(doubleAnimation_ffOut, new PropertyPath("(Label.Opacity)"));
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
                    // 单层：所有的都在第 0 层
                    // 将显示 LAYER_NUM 个消息气泡

                    danmakuLabels.Enqueue(label);

                    // 下面等待计时器计时
                    // 字号做动画

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
            INIT_TOP = 1.0 / 3;
            Regen_Layers();
            BackClipRect.Rect = new Rect(0, 0, this.Width, this.Height);
            TranClipRect.Rect = new Rect(0, 0, this.Width, this.Height);
        }

        private void Regen_Layers()
        {
            current_order = (From_Front ? 0 : LAYER_NUM - 1);

            //清除原本的Thumb
            for (int i = 0; i < hoverLayers.Count; ++i)
            {
                ThumbCanvas.Children.Remove(hoverLayers.ElementAt(i).Layer_Thumb);
            }

            hoverLayers = new List<HoverLayer>();

            double temp_ts = FocalPt_inSize;
            double tempTop = INIT_TOP * this.Height; 
            for (int i = 0; i < LAYER_NUM; ++i)
            {
                HoverLayer layer = new HoverLayer(temp_ts, tempTop);
                layer.Layer_Thumb.Width = this.ActualWidth;
                layer.Layer_Thumb.Height = temp_ts / 0.75;
                layer.Layer_Thumb.Opacity = 0;
                layer.Layer_Thumb.DragDelta += ThumbLayer_DragDelta;
                layer.Layer_Thumb.Cursor = System.Windows.Input.Cursors.Hand;

                hoverLayers.Add(layer);
                ThumbCanvas.Children.Add(layer.Layer_Thumb);
                Canvas.SetLeft(layer.Layer_Thumb, 0);
                Canvas.SetTop(layer.Layer_Thumb, tempTop);

                temp_ts *= DECR_FAC;
                temp_ts = temp_ts < 1 ? 1 : temp_ts;
                tempTop -= temp_ts / 0.75 + 10;
            }

            CornerRefreshTimer.Interval = TimeSpan.FromSeconds(hoverLayers.ElementAt(0).Hover_time / LAYER_NUM);

            if(ForeFont != null)
            {
                this.FontFamily = ForeFont;
            }

            myright_c = INIT_TOP * this.Width;
            mybottom_c = INIT_TOP * this.Height;
            if (DM_Style == DanmuStyle.BubbleCorner)
            {
                //清除原本的Thumb，避免干扰，层还是被生成的
                for (int i = 0; i < hoverLayers.Count; ++i)
                {
                    ThumbCanvas.Children.Remove(hoverLayers.ElementAt(i).Layer_Thumb);
                }
                ThumbCanvas.Children.Remove(corner_thumb);
                corner_thumb = new System.Windows.Controls.Primitives.Thumb();
                corner_thumb.Width = this.Width / 4;
                corner_thumb.Height = this.Height / 3;
                corner_thumb.Opacity = 0;
                corner_thumb.DragDelta += Corner_thumb_DragDelta;
                corner_thumb.Cursor = System.Windows.Input.Cursors.Hand;
                ThumbCanvas.Children.Add(corner_thumb);
                Canvas.SetRight(corner_thumb, myright_c);
                Canvas.SetBottom(corner_thumb, mybottom_c);
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
