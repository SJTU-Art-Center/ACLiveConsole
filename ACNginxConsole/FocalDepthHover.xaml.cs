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
using System.Diagnostics;
using System.Windows.Controls.Primitives;
using Windows.UI.Text;
using System.Security.Cryptography;
using BitConverter;
using Windows.System;
using System.Text.RegularExpressions;
using System.IO;
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
        public static bool GiftGiving = false;             //是否正在抽奖

        public enum DanmuStyle { Plain, Bubble, BubbleFloat, BubbleCorner, BottomBar, BottomBarWithUserName };
        public static DanmuStyle DM_Style = DanmuStyle.Plain;

        public static double HOVER_TIME = 10;       //悬浮时间
        public static double FocalPt_inSize = 32;   //焦点对应的字号
        public static double BLUR_MAX = 10;         //最大模糊程度
        public static int LAYER_NUM = 4;            //层数
        public static double INIT_TOP;              //开始顶距
        public static double DECR_FAC = 0.85;       //缩小系数
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

        public double CalcLabelWidth(string input)
        {
            int ChineseNum = GetChineseLeng(input);
            int OtherNum = input.Length - ChineseNum;
            return FocalPt_inSize * ChineseNum * 1.2 + FocalPt_inSize * OtherNum * 0.6;
        }

        public int GetChineseLeng(string StrContert)
        {
            if (string.IsNullOrEmpty(StrContert))
                return 0;
            //此处进行的是一些转义符的替换,以免照成统计错误.
            StrContert = StrContert.Replace("\n", "").Replace("\r", "").Replace("\t", "");
            //替换掉内容中的数字,英文,空格.方便进行字数统计.
            return getByteLengthByChinese(StrContert);
            //return Regex.Matches(StrContert, @"[0-9][0-9'\-.]*").Count + iChinese;//中文中的字数统计需要加上数字统计.
        }
        /// <summary>
        /// 返回字节数
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        private int getByteLengthByChinese(string s)
        {
            int l = 0;
            char[] q = s.ToCharArray();
            for (int i = 0; i < q.Length; i++)
            {
                if ((int)q[i] >= 0x4E00 && (int)q[i] <= 0x9FA5) // 汉字
                {
                    l += 1;
                }
            }
            return l;
        }

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

            Opacity = 0;
            CanvasBottomBar.Visibility = Visibility.Hidden;
            GridGiftGiving.Visibility = Visibility.Hidden;

        }

        

        double myright_c;
        double mybottom_c;
        System.Windows.Controls.Primitives.Thumb corner_thumb;
        Storyboard bottombars = new Storyboard();

        private void CornerRefreshTimer_Tick(object sender, EventArgs e)
        {
            if (danmakuLabels.Any())
            {
                Label label = danmakuLabels.Dequeue();

                label.Foreground = new SolidColorBrush((this.FindResource("BubbleFore") as SolidColorBrush).Color);

                if (DM_Style == DanmuStyle.BubbleCorner) {
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
        }

        bool bottomBarPoped = false;

        private void BottomBarPopUp()
        {

            TranslateTransform popupTrans = new TranslateTransform();
            CanvasBottomBar.RenderTransform = popupTrans;

            DoubleAnimation popup_d = new DoubleAnimation(CanvasBottomBar.Height, 0, 
                TimeSpan.FromSeconds(Properties.Settings.Default.TranSec));
            popupTrans.BeginAnimation(TranslateTransform.YProperty, popup_d);

            bottomBarPoped = true;
        }

        private void BottomBarPushDown()
        {
            bottomBarPoped = false;

            TranslateTransform pushdownTrans = new TranslateTransform();
            CanvasBottomBar.RenderTransform = pushdownTrans;

            DoubleAnimation pushdown_d = new DoubleAnimation(0, CanvasBottomBar.Height, 
                TimeSpan.FromSeconds(Properties.Settings.Default.TranSec));
            pushdownTrans.BeginAnimation(TranslateTransform.YProperty, pushdown_d);

        }

        // 保持速度
        // v = Width / HOVER_TIME

        private void BottomFirstStage()
        {
            // 第一段：进入段
            // 结束后触发下一个弹幕出队，留一定空隙
            CanvasBottomBar.Visibility = Visibility.Visible;

            if (danmakuLabels.Any())
            {
                var label = danmakuLabels.Dequeue();
                label.FontSize = FocalPt_inSize;
                label.Foreground = new SolidColorBrush((this.FindResource("BubbleFore") as SolidColorBrush).Color);
                CanvasBottomBar.Children.Add(label);
                Canvas.SetTop(label, 0);

                double labelWidth = CalcLabelWidth(label.Content.ToString());
                DoubleAnimation doubleAnimation_b = new DoubleAnimation(
                    this.Width, this.Width - labelWidth,
                    new Duration(TimeSpan.FromSeconds(HOVER_TIME * labelWidth / this.Width)));
                    //new Duration(TimeSpan.FromSeconds(0.5)));                
                Storyboard.SetTarget(doubleAnimation_b, label);
                Storyboard.SetTargetProperty(doubleAnimation_b, new PropertyPath("(Canvas.Left)"));
                Storyboard bottoms = new Storyboard();
                bottoms.Children.Add(doubleAnimation_b);
                bottoms.Completed += Bottoms_Completed;
                bottoms.Begin();

            }
        }

        private void Bottoms_Completed(object sender, EventArgs e)
        {
            // 第二段：展示段 

            Label label = Storyboard.GetTarget((sender as ClockGroup).Timeline.Children[0]) as Label;
            double labelWidth = CalcLabelWidth(label.Content.ToString());
            DoubleAnimation doubleAnimation_b = new DoubleAnimation(
                        this.Width - labelWidth, -labelWidth,
                        new Duration(TimeSpan.FromSeconds(HOVER_TIME)));
                        //new Duration(TimeSpan.FromSeconds(1.5)));
            Storyboard.SetTarget(doubleAnimation_b, label);
            Storyboard.SetTargetProperty(doubleAnimation_b, new PropertyPath("(Canvas.Left)"));
            Storyboard bottomsii = new Storyboard();
            bottomsii.Children.Add(doubleAnimation_b);
            bottomsii.Completed += Bottomsii_Completed;
            bottomsii.Begin();

            // 触发下一个弹幕
            BottomFirstStage();

        }

        private void Bottomsii_Completed(object sender, EventArgs e)
        {
            // 第三段：退出段
            // 结束后销毁，如果队列为空，则底栏塞回

            Label label = Storyboard.GetTarget((sender as ClockGroup).Timeline.Children[0]) as Label;
            CanvasBottomBar.Children.Remove(label);

            if (!danmakuLabels.Any() && Properties.Settings.Default.BottomBarAuto && CanvasBottomBar.Children.Count == 0)
            {
                if (bottomBarPoped)
                    BottomBarPushDown();
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

        bool GiftGivingPoped = false;
        List<string> GiftingNameList = new List<string>();      //抽奖名单
        Queue<string> GodChoosenQueue = new Queue<string>();    //天选之人

        DateTime prevDanmu;

        struct NameLabel {
            public Label nameL;
            public double intervalL;
        };

        // 队列
        Queue<NameLabel> queueNameLabel = new Queue<NameLabel>();

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
                case DanmuStyle.BottomBar:

                    danmakuLabels.Enqueue(label);

                    // 链式反应的引子
                    if (CanvasBottomBar.Children.Count == 0)
                    {
                        BottomFirstStage();
                        // 初始化：弹出段
                        // 如果没有元素，则弹出底栏
                        if (!bottomBarPoped && Properties.Settings.Default.BottomBarAuto)
                            BottomBarPopUp();
                    }

                    break;

                case DanmuStyle.BottomBarWithUserName:
                    //多进一个Label
                    Label labelUserName = new Label();
                    labelUserName.Content = e.Danmu.UserName;
                    labelUserName.Foreground = new SolidColorBrush((this.FindResource("BubbleFore") as SolidColorBrush).Color);
                    labelUserName.FontWeight = System.Windows.FontWeights.Light;
                    labelUserName.Opacity = 0.5;

                    danmakuLabels.Enqueue(labelUserName);
                    danmakuLabels.Enqueue(label);

                    // 链式反应的引子
                    if (CanvasBottomBar.Children.Count == 0)
                    {
                        BottomFirstStage();
                        // 初始化：弹出段
                        // 如果没有元素，则弹出底栏
                        if (!bottomBarPoped && Properties.Settings.Default.BottomBarAuto)
                            BottomBarPopUp();
                    }

                    break;
                    
            }

            // TODO: 精确度太差，要直接插近道
            if (GiftGiving)
            {
                string cond = Properties.Settings.Default.GiftGivingCond;
                if (cond == "" || cond == e.Danmu.Danmaku)
                {
                    GiftingNameList.Add(e.Danmu.UserName);

                    Label currentLabel = new Label();
                    currentLabel.Content = e.Danmu.UserName;
                    currentLabel.Foreground = new SolidColorBrush((this.FindResource("BubbleFore") as SolidColorBrush).Color);
                    currentLabel.FontSize = FocalPt_inSize;
                    currentLabel.FontFamily = ForeFont;
                    currentLabel.FontWeight = System.Windows.FontWeights.Light;

                    NameLabel curnLabel;
                    curnLabel.nameL = currentLabel;
                    curnLabel.intervalL = Properties.Settings.Default.TranSec;

                    //CanvasNameField.Children.Add(currentLabel);
                    //Canvas.SetLeft(currentLabel, 0);

                    if (!GiftGivingPoped)
                    {
                        //开始抽奖
                        // GridGiftGiving.Width = this.Width * (INIT_TOP > 0.33 ? INIT_TOP : 0.33);

                        //清空抽奖池
                        GiftingNameList.Clear();
                        GodChoosenQueue.Clear();
                        CanvasNameField.Children.Clear();
                        queueNameLabel.Clear();

                        //展开抽奖界面
                        GiftGivingPop();

                        prevDanmu = DateTime.Now;
                        queueNameLabel.Enqueue(curnLabel);

                    } else
                    {
                        curnLabel.intervalL = DateTime.Now.Subtract(prevDanmu).TotalSeconds;
                        prevDanmu = DateTime.Now;
                        queueNameLabel.Enqueue(curnLabel);
                    }

                    if (CanvasNameField.Children.Count == 0)  //引发链式反应
                        NameFirstStage();
                    else if(Canvas.GetTop(CanvasNameField.Children[0]) == 0)    //防卡兵
                    { CanvasNameField.Children.Clear();NameFirstStage(); }

                }

            } else if (GiftingNameList.Any())
            {
                //开始抽选
                if (ColExpanded) {
                    ColExpand();
                    LabelGiftGiving.Content = "抽选中";
                }

                int danmuNum = GiftingNameList.Count;
                
                //天选抽奖
                GodChoosenQueue.Clear();
                GodChoosing();
                int chosenCount = GodChoosenQueue.Count;
                double rate = (double)chosenCount / danmuNum * 100;

                //展开抽奖界面
                GiftGivingExpand();

                string ChosenName = null;
                string logText = "\n抽奖时间：" + DateTime.Now.ToString() + '\n' +
                    "抽奖人次：" + danmuNum + "\t中奖人数：" + chosenCount + "\t中奖率：" + string.Format("{0:F2}", rate) + "%\n";

                //天选者出，显示抽奖获得者
                while (GodChoosenQueue.Any())
                {
                    var gn = GodChoosenQueue.Dequeue();
                    ChosenName += gn + ' ';
                    logText += gn + '\n';
                }
                //DisplayChosenName(GodChoosenQueue.Dequeue());// 展示中奖人
                DisplayChosenName(ChosenName);

                //记录日志
                try
                {
                    File.AppendAllText("Gift.txt", logText);
                    if (Properties.Settings.Default.GiftGivingShow)
                        System.Diagnostics.Process.Start("Gift.txt");
                }
                catch
                {

                }

                // 关闭抽奖界面
                GiftGivingPush();
            }

        }

        private void GiftGivingPop()
        {
            TranslateTransform popupTrans = new TranslateTransform();
            GridGiftGiving.RenderTransform = popupTrans;
            GridGiftGiving.Height = FocalPt_inSize / 0.6;
            GridGiftGiving.Background = new SolidColorBrush(Color.FromArgb(255,
                (byte)(WinBack.Color.R * 0.7),
                (byte)(WinBack.Color.G * 0.7),
                (byte)(WinBack.Color.B * 0.7)));

            if (GridGiftGiving.Width >= this.Width) GiftGivingExpand();

            DoubleAnimation popup_d = new DoubleAnimation(GridGiftGiving.Width, 0,
                TimeSpan.FromSeconds(Properties.Settings.Default.TranSec))
            { EasingFunction = new BackEase() { EasingMode = EasingMode.EaseIn } };

            LabelGiftGiving.Content = "抽奖中";
            LabelGiftGiving.FontFamily = ForeFont;
            LabelGiftGiving.FontSize = FocalPt_inSize;
            LabelGiftGiving.Foreground = new SolidColorBrush(ForeColor);
            LabelGiftGiving.Background = new SolidColorBrush(Color.FromArgb(255,
                (byte)(WinBack.Color.R * 0.5),
                (byte)(WinBack.Color.G * 0.5),
                (byte)(WinBack.Color.B * 0.5)));

            GridGiftGiving.Visibility = Visibility.Visible;
            popupTrans.BeginAnimation(TranslateTransform.XProperty, popup_d);

            GiftGivingPoped = true;
        }

        bool ColExpanded = false;

        private void ColExpand()
        {
            //分栏动画
            Storyboard ExpandNameCol = this.FindResource("ExpandNameCol") as Storyboard;

            if (!ColExpanded)
            {
                (ExpandNameCol.Children[0] as GridLengthAnimation).From = new GridLength(0, GridUnitType.Star);
                (ExpandNameCol.Children[0] as GridLengthAnimation).To = new GridLength(2, GridUnitType.Star);
                (ExpandNameCol.Children[0] as GridLengthAnimation).EasingFuntion = new BackEase() { EasingMode = EasingMode.EaseIn };

                ExpandNameCol.Begin(ColGiftName);
                ColExpanded = true;
            }
            else
            {
                (ExpandNameCol.Children[0] as GridLengthAnimation).From = new GridLength(2, GridUnitType.Star);
                (ExpandNameCol.Children[0] as GridLengthAnimation).To = new GridLength(0, GridUnitType.Star);
                (ExpandNameCol.Children[0] as GridLengthAnimation).EasingFuntion = new BackEase() { EasingMode = EasingMode.EaseIn };

                ExpandNameCol.Begin(ColGiftName);
                ColExpanded = false;
            }
        }

        // 维持时间
        // prev(default) -> prev + cur(interval) -> cur + next(interval) -> next(default)

        private void NameFirstStage()
        {
            if(!ColExpanded) ColExpand();
            LabelGiftGiving.Content = "抽奖中";
            if (queueNameLabel.Any())
            {
                //首名字出现
                NameLabel nl = queueNameLabel.Dequeue();
                double intervalDanmu = nl.intervalL;

                Storyboard sbn = new Storyboard();

                Label currentLabel = nl.nameL;
                CanvasNameField.Children.Add(currentLabel);
                Canvas.SetLeft(currentLabel, 0);

                DoubleAnimation doubleAnimation_f = new DoubleAnimation(
                            GridGiftGiving.Height, 0,
                            new Duration(TimeSpan.FromSeconds(intervalDanmu)));
                Storyboard.SetTarget(doubleAnimation_f, currentLabel);
                Storyboard.SetTargetProperty(doubleAnimation_f, new PropertyPath("(Canvas.Top)"));
                //doubleAnimation_f.EasingFunction = new ExponentialEase()
                //{
                //    EasingMode = EasingMode.EaseIn
                //};

                sbn.Children.Add(doubleAnimation_f);

                sbn.Completed += Sbn_Completed;
                sbn.Begin();
            }
        }

        private void Sbn_Completed(object sender, EventArgs e)
        {
            if((sender as ClockGroup).Timeline.Children.Count > 0)
            {
                Label prevLabel = Storyboard.GetTarget((sender as ClockGroup).Timeline.Children[0]) as Label;
                //CanvasNameField.Children.Remove(prevLabel);

                Storyboard sbn = new Storyboard();
                Storyboard sbnleave = new Storyboard();

                double intervalDanmu = Properties.Settings.Default.TranSec;

                if (queueNameLabel.Any() && GiftGiving)         //防止抽完还进
                {
                    NameLabel nl = queueNameLabel.Dequeue();
                    intervalDanmu = nl.intervalL;

                    Label currentLabel = nl.nameL;
                    CanvasNameField.Children.Add(currentLabel);
                    Canvas.SetLeft(currentLabel, 0);

                    DoubleAnimation doubleAnimation_f = new DoubleAnimation(
                                GridGiftGiving.Height, 0,
                                new Duration(TimeSpan.FromSeconds(intervalDanmu)));
                    Storyboard.SetTarget(doubleAnimation_f, currentLabel);
                    Storyboard.SetTargetProperty(doubleAnimation_f, new PropertyPath("(Canvas.Top)"));
                    //doubleAnimation_f.EasingFunction = new ExponentialEase()
                    //{
                    //    EasingMode = EasingMode.EaseIn
                    //};

                    sbn.Children.Add(doubleAnimation_f);
                    sbn.Completed += Sbn_Completed;
                    sbn.Begin();

                }

                //离开
                //Label prevLabel = (CanvasNameField.Children[0] as Label);
                DoubleAnimation doubleAnimation_p = new DoubleAnimation(
                        0, -GridGiftGiving.Height,
                        new Duration(TimeSpan.FromSeconds(intervalDanmu)));
                Storyboard.SetTarget(doubleAnimation_p, prevLabel);
                Storyboard.SetTargetProperty(doubleAnimation_p, new PropertyPath("(Canvas.Top)"));
                //doubleAnimation_p.EasingFunction = new ExponentialEase()
                //{
                //    EasingMode = EasingMode.EaseOut
                //};

                sbnleave.Children.Add(doubleAnimation_p);
                sbnleave.Completed += Sbnleave_Completed;
                sbnleave.Begin();

            }
            
        }

        private void Sbnleave_Completed(object sender, EventArgs e)
        {
            Label prevLabel = Storyboard.GetTarget((sender as ClockGroup).Timeline.Children[0]) as Label;
            CanvasNameField.Children.Remove(prevLabel);
        }

        private void GiftGivingExpand()
        {
            DoubleAnimation widthani = new DoubleAnimation() { Duration = TimeSpan.FromSeconds(Properties.Settings.Default.TranSec) };

            if (GridGiftGiving.Width < this.Width)
            {
                widthani.From = this.Width * (INIT_TOP > 0.33 ? INIT_TOP : 0.33);
                widthani.To = this.Width;
                widthani.EasingFunction = new BackEase() { EasingMode = EasingMode.EaseIn };
            }
            else
            {
                widthani.From = this.Width;
                widthani.To = this.Width * (INIT_TOP > 0.33 ? INIT_TOP : 0.33);
                widthani.EasingFunction = new BackEase() { EasingMode = EasingMode.EaseOut };
            }

            TranslateTransform expandTrans = new TranslateTransform();
            GridGiftGiving.RenderTransform = expandTrans;

            GridGiftGiving.BeginAnimation(WidthProperty, widthani);
            if (!ColExpanded) ColExpand();
            //expandTrans.BeginAnimation(TranslateTransform.XProperty, popup_d);
            LabelGiftGiving.Content = "中奖者";
            
        }

        private void DisplayChosenName(string name)
        {
            CanvasNameField.Children.Clear();

            Label GodChosenLabel = new Label();
            GodChosenLabel.Content = name;
            GodChosenLabel.Foreground = new SolidColorBrush((this.FindResource("BubbleFore") as SolidColorBrush).Color);
            GodChosenLabel.FontSize = FocalPt_inSize;
            GodChosenLabel.FontFamily = ForeFont;
            GodChosenLabel.FontWeight = System.Windows.FontWeights.Light;

            CanvasNameField.Children.Add(GodChosenLabel);
            Canvas.SetTop(GodChosenLabel, 0);

            double NameFieldWidth = this.Width * (INIT_TOP > 0.33 ? INIT_TOP : 0.33);

            if (CalcLabelWidth(name) > NameFieldWidth)
            {
                Storyboard pushsb = new Storyboard();

                DoubleAnimationUsingKeyFrames push_in = new DoubleAnimationUsingKeyFrames();
                push_in.KeyFrames.Add(new EasingDoubleKeyFrame(NameFieldWidth * 0.5, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0)), new BackEase() { EasingMode = EasingMode.EaseIn }));
                push_in.KeyFrames.Add(new LinearDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(Properties.Settings.Default.TranSec + HOVER_TIME / 4))));
                push_in.KeyFrames.Add(new LinearDoubleKeyFrame(NameFieldWidth - CalcLabelWidth(name), KeyTime.FromTimeSpan(TimeSpan.FromSeconds(Properties.Settings.Default.TranSec + HOVER_TIME*1.3))));     //你算不清楚我把时间拉长

                Storyboard.SetTarget(push_in, GodChosenLabel);
                Storyboard.SetTargetProperty(push_in, new PropertyPath("(Canvas.Left)"));
                pushsb.Children.Add(push_in);
                pushsb.Begin();
            }
            else
                Canvas.SetLeft(GodChosenLabel, 0);

        }

        private void GiftGivingPush()
        {
            TranslateTransform popupTrans = new TranslateTransform();
            GridGiftGiving.RenderTransform = popupTrans;

            DoubleAnimationUsingKeyFrames push_d = new DoubleAnimationUsingKeyFrames();
            KeyTime k1 = KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0));
            KeyTime k2 = KeyTime.FromTimeSpan(TimeSpan.FromSeconds(HOVER_TIME));
            KeyTime k3 = KeyTime.FromTimeSpan(TimeSpan.FromSeconds(HOVER_TIME + Properties.Settings.Default.TranSec));

            push_d.KeyFrames.Add(new LinearDoubleKeyFrame() { Value = 0, KeyTime = k1 });
            push_d.KeyFrames.Add(new LinearDoubleKeyFrame() { Value = 0, KeyTime = k2 });
            push_d.KeyFrames.Add(new EasingDoubleKeyFrame() { Value = -this.Width, KeyTime = k3 });

            popupTrans.BeginAnimation(TranslateTransform.XProperty, push_d);

            ColExpanded = false;
            GiftGivingPoped = false;
        }

        private void GodChoosing()
        {
            //真随机数
            RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
            byte[] data = new byte[8];

            // 防止越界
            int length = (GiftingNameList.Count > Properties.Settings.Default.GiftGivingNum ? Properties.Settings.Default.GiftGivingNum : GiftingNameList.Count);

            for (int i = 0; i < length; i++)
            {
                rng.GetBytes(data);
                int rnd = (int)Math.Round(Math.Abs(EndianBitConverter.BigEndian.ToInt64(data, 0)) / (decimal)long.MaxValue * (GiftingNameList.Count - 1), 0);

                if (!GiftingNameList.Any()) break;

                if (GodChoosenQueue.Contains(GiftingNameList.ElementAt(rnd)))
                    --i;
                else
                    GodChoosenQueue.Enqueue(GiftingNameList.ElementAt(rnd));
                GiftingNameList.RemoveAt(rnd);
            }

            GiftingNameList.Clear();
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

            if (DM_Style == DanmuStyle.BottomBar||DM_Style==DanmuStyle.BottomBarWithUserName)
            {
                //CanvasBottomBar.Visibility = Visibility.Visible;
                CanvasBottomBar.Height = FocalPt_inSize / 0.6;
                //清除原本的Thumb，避免干扰，层还是被生成的
                for (int i = 0; i < hoverLayers.Count; ++i)
                {
                    ThumbCanvas.Children.Remove(hoverLayers.ElementAt(i).Layer_Thumb);
                }

                if (CanvasBottomBar.Children.Count == 0)
                {
                    // 初始化：弹出段
                    // 如果没有元素，则弹出底栏
                    if (!bottomBarPoped)
                        BottomBarPopUp();

                    // 链式反应的引子
                    BottomFirstStage();
                }
            }
            else
            {
                //CanvasBottomBar.Visibility = Visibility.Hidden;

                if (bottomBarPoped)
                    BottomBarPushDown();

            }

            GridGiftGiving.Width = this.Width * (INIT_TOP > 0.33 ? INIT_TOP : 0.33);

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
