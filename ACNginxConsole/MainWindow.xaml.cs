// Art Center Live Console
// Copyright (c) 2019 - 2020 by Art Center, All Rights Reserved.
// GPL-3.0

//========= Platform =============

// C# WPF(Windows Presentation Foundation) 
// .NET Framework 4.5

//========= Update Log ===========

// For more information, please visit:
// https://github.com/LogCreative/ACLiveConsole.

// Ver 3.5.0.0 by Li Zilong
// Unreleased.

// Ver 3.0.0.0 by Li Zilong
// Source Code Release Date: 2020/3/8
// Add Monitor;
// Add Datalist;
// Monitor Function Flaw, with manual solutions.

// Ver 2.5.1.0 by Li Zilong
// Source Code Release Date: 2019/11/26
// Add Program Conflict Emergency Solution;
// Add Test with ffmpeg;
// Add Setting;

// Ver 2.0.0.0 by Li Zilong
// Date: 2019/10/4
// UI Initialized;

// Ver 1.1.0.0 by Li Zilong
// Date: 2019/9/4
// Debug Console Core Function;

// Ver 1.0.0.0 by Liu Qianxi, et al.
// Date: 2019/5/23
// Live Tutourial


using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using static System.Windows.Forms.Screen;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Net;
using System.Windows.Media.Animation;
using System.Collections.ObjectModel;
using System.Reflection;
using Vlc.DotNet.Core;
using Vlc.DotNet.Wpf;
using BiliDMLib;
using BilibiliDM_PluginFramework;
using Newtonsoft.Json.Linq;
using BililiveRecorder.Core;

namespace ACNginxConsole
{

    #region GridLengthAnimation
    /// <summary>
    /// GridLengthAnimation Class provides a way to create animation for grid.Width/Height.
    /// </summary>
    internal class GridLengthAnimation : AnimationTimeline
    {
        static GridLengthAnimation()
        {
            FromProperty = DependencyProperty.Register("From", typeof(GridLength), typeof(GridLengthAnimation));
            ToProperty = DependencyProperty.Register("To", typeof(GridLength), typeof(GridLengthAnimation));
        }
        public static readonly DependencyProperty FromProperty;
        public GridLength From
        {
            get
            {
                return (GridLength)GetValue(GridLengthAnimation.FromProperty);
            }
            set
            {
                SetValue(GridLengthAnimation.FromProperty, value);
            }
        }

        public static readonly DependencyProperty ToProperty;
        public GridLength To
        {
            get
            {
                return (GridLength)GetValue(GridLengthAnimation.ToProperty);
            }
            set
            {
                SetValue(GridLengthAnimation.ToProperty, value);
            }
        }

        protected override System.Windows.Freezable CreateInstanceCore()
        {
            return new GridLengthAnimation();
        }

        // .Net Framework 3.5 不支持缓动。已经升级至 4.0 。
        // 如果将来打算升级框架，请见： https://www.cnblogs.com/startewho/articles/6882332.html
        /// <summary>
        /// The <see cref="EasingFunction"/> dependency.
        /// </summary>
        public const string EasingFunctionPropertyName = "EasingFunction";

        public IEasingFunction EasingFuntion
        {
            get
            {
                return (IEasingFunction)GetValue(EasingFunctionProperty);
            }
            set
            {
                SetValue(EasingFunctionProperty, value);
            }
        }

        public static readonly DependencyProperty EasingFunctionProperty = DependencyProperty.Register(
            EasingFunctionPropertyName,
            typeof(IEasingFunction),
            typeof(GridLengthAnimation),
            new UIPropertyMetadata(null)
            );


        public override object GetCurrentValue(object defaultOriginValue, object defaultDestinationValue, AnimationClock animationClock)
        {
            double fromVal = ((GridLength)GetValue(GridLengthAnimation.FromProperty)).Value;
            double toVal = ((GridLength)GetValue(GridLengthAnimation.ToProperty)).Value;

            double progress = animationClock.CurrentProgress.Value;


            IEasingFunction easingFunction = EasingFuntion;
            if (easingFunction != null)
            {
                progress = easingFunction.Ease(progress);
            }

            if (fromVal > toVal)
            {
                return new GridLength((1 - progress) * (fromVal - toVal) + toVal,
                    ((GridLength)GetValue(GridLengthAnimation.FromProperty)).GridUnitType);
            }
            else
                return new GridLength(progress * (toVal - fromVal) + fromVal,
                    ((GridLength)GetValue(GridLengthAnimation.ToProperty)).GridUnitType);
        }

        public override Type TargetPropertyType
        {
            get
            {
                return typeof(GridLength);
            }
        }

    }

    #endregion

    //窗口间发送弹幕的通道
    public delegate void ReceivedDanmuEvt(object sender, ReceivedDanmuArgs e);

    public class ReceivedDanmuArgs
    {
        public MainWindow.DanmakuItem Danmu;
    }

    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {

        #region 全局变量定义

        bool InputError = false;
        DispatcherTimer dispatcherTimer = new DispatcherTimer();
        DispatcherTimer dispatcherTimerBling = new DispatcherTimer();
        DispatcherTimer dispatcherTimerRefresh = new DispatcherTimer();
        DispatcherTimer dispatcherTimerEGY = new DispatcherTimer();
        DispatcherTimer dispatcherTimerMon = new DispatcherTimer();
        DispatcherTimer dispatcherTimerDanmaku = new DispatcherTimer();

        int time_left;

        int red_count;
        bool Isred;
        bool IsGreenTest;
        bool EGY;
        bool Redlight;
        bool Cross_check;

        const double RightCol_Last = 300;    //右侧边栏最后状态

        private bool enable_regex = false;
        private string regex = "";
        private Regex FilterRegex;
        private bool ignorespam_enabled = false;

        #endregion

        #region 配置项类
        public event PropertyChangedEventHandler PropertyChanged;
        static ObservableCollection<ConfigItem> configdata;//定义配置数据库
        public ObservableCollection<ConfigItem> Configdata
        {

            get
            {
                return configdata;
            }
            set
            {
                if (configdata != value)
                {
                    configdata = value;
                    PropertyChanged(this, new PropertyChangedEventArgs(nameof(Configdata)));
                }
            }
        }

        static int configcount = 0;

        public class ConfigItem : INotifyPropertyChanged
        {
            private int id;
            private string type;
            private string sourceName;
            private string streamCode;
            private string liveViewingSite;
            private int bililive_roomid;
            public event PropertyChangedEventHandler PropertyChanged;

            public int Id
            {
                get { return id; }
                set
                {
                    id = value;
                }
            }

            public string Type
            {
                get { return type; }
                set
                {
                    type = value;
                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(ConfigItem)));
                    }
                }
            }

            public string SourceName
            {
                get { return sourceName; }
                set
                {
                    sourceName = value;
                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(ConfigItem)));
                    }
                }
            }

            public string StreamCode
            {
                get { return streamCode; }
                set
                {
                    streamCode = value;
                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(ConfigItem)));
                    }
                }
            }

            //B站弹幕姬专用 获取直播间号码
            public int Bililive_roomid
            {
                get { return bililive_roomid; }
                set
                {
                    bililive_roomid = value;
                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(ConfigItem)));
                    }
                }
            }

            public string LiveViewingSite
            {
                get { return liveViewingSite; }
                set
                {
                    //liveViewingSite = value;
                    if (value != "")
                    {
                        //抓包分析
                        if (Type == "B站")
                        {
                            //对room_id进行赋值
                            bililive_roomid = Get_roomid(value);
                        }
                        liveViewingSite = Get_realplay(Type, value);
                    }
                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(ConfigItem)));
                    }
                }
            }


            public ConfigItem() { }

            public ConfigItem(int id, int typeint, string sourceName, string streamCode, string liveViewingSite = "")
            {
                this.Id = id;
                switch (typeint)
                {
                    case -1: this.Type = "本地"; break;
                    case 0: this.Type = "B站"; break;
                    case 1: this.Type = "微博"; break;
                    default: this.Type = "自定义"; break;
                }
                this.SourceName = sourceName;
                this.StreamCode = streamCode;
                this.LiveViewingSite = liveViewingSite;
            }

            private int Get_roomid(string website)
            {
                System.Net.WebClient client = new WebClient();
                //byte[] page = client.DownloadData(website);
                //string content = System.Text.Encoding.UTF8.GetString(page);
                string content = website;
                Regex re = new Regex(@"(?<=https?://live\.bilibili\.com/)\b\w+\b"); //正则表达式
                try
                {
                    MatchCollection matches = re.Matches(content);
                    string short_id = null;

                    //使用api稳定性不高
                    short_id = matches[0].Groups[0].ToString();
                    string api_url = "https://api.live.bilibili.com/room/v1/Room/room_init?id=" + short_id;
                    string api_content = System.Text.Encoding.UTF8.GetString(client.DownloadData(api_url));

                    re = new Regex(@"(?<=""room_id"":)\b(\w+)\b");
                    try
                    {
                        matches = re.Matches(api_content);
                        return Convert.ToInt32(matches[0].Groups[0].ToString().Trim());
                    }
                    catch
                    {
                        return -2;
                    }
                }
                catch
                {
                    return -1;
                }
            }

            /// <summary>
            /// 抓包函数
            /// </summary>
            /// <param name="Type">网站类型</param>
            /// <param name="website">网站地址</param>
            /// <returns>真正的播流地址</returns>
            private string Get_realplay(string Type, string website)
            {
                string realplay = null;
                if (Type == "B站")
                {
                    if (Properties.Settings.Default.CloseYouget == false)
                    {
                        //该方案暂时失效，可能需要大改。转写自you-get
                        System.Net.WebClient client = new WebClient();
                        //byte[] page = client.DownloadData(website);
                        //string content = System.Text.Encoding.UTF8.GetString(page);
                        string content = website;
                        Regex re = new Regex(@"(?<=https?://live\.bilibili\.com/)\b\w+\b"); //正则表达式
                        try
                        {
                            MatchCollection matches = re.Matches(content);
                            string short_id = null, room_id = null;

                            short_id = matches[0].Groups[0].ToString();
                            string api_url = "https://api.live.bilibili.com/room/v1/Room/room_init?id=" + short_id;
                            string api_content = System.Text.Encoding.UTF8.GetString(client.DownloadData(api_url));

                            re = new Regex(@"(?<=""room_id"":)\b(\w+)\b");
                            try
                            {
                                matches = re.Matches(api_content);


                                room_id = matches[0].Groups[0].ToString();
                                //api_url = "https://api.live.bilibili.com/room/v1/Room/get_info?room_id=" + room_id;
                                //api_content = System.Text.Encoding.UTF8.GetString(client.DownloadData(api_url));

                                api_url = "https://api.live.bilibili.com/room/v1/Room/playUrl?cid=" + room_id + "&quality=0&platform=web";  //这里可以调画质
                                api_content = System.Text.Encoding.UTF8.GetString(client.DownloadData(api_url));

                                //re = new Regex(@"(?<=""url"":)("")*.?("")");
                                re = new Regex(@"(?<=""url"":"")([^""]+)");
                                try
                                {
                                    matches = re.Matches(api_content);


                                    api_url = matches[0].Groups[0].ToString();
                                    re = new Regex(@".*\.flv");
                                    try
                                    {
                                        matches = re.Matches(api_url);

                                        //解析到
                                        api_url = matches[0].Groups[0].ToString();
                                        re = new Regex(@"\.flv");
                                        string result = re.Replace(api_url, ".m3u8");
                                        System.Diagnostics.Debug.WriteLine(result);
                                        return result;
                                        //该方法暂时失效

                                    }
                                    catch (Exception)
                                    {
                                        return "地址替换错误(-4)";
                                    }
                                }
                                catch
                                {
                                    return "解析播流地址错误(-3)";
                                }
                            }
                            catch
                            {
                                return "解析初始化错误(-2)";
                            }
                        }
                        catch
                        {
                            return "网址错误(-1)";    //网址错误
                        }
                    }
                    else
                        realplay = website;
                }
                else if (Type == "微博")
                {
                    realplay = website; //暂时保持不变
                }
                else
                {
                    realplay = website; //暂时保持不变
                }
                return realplay;
            }

        }
        #endregion

        #region 弹幕池


        //时间可以被设置 每次过一个计时器时间将会刷新
        //应当有时间差的存储 通过ListItem的Opacity展示出来

        //弹幕池用来临时存储弹幕 给用户一个选择缓冲的时间
        Queue<DanmakuItem> DanmakuPool = new Queue<DanmakuItem>();

        static int EXPIRE_TIME;
        public class DanmakuItem
        {
            private string danmaku;
            private int timepass;     //经过次数
            private bool isSelected;
            private bool isAdmin;

            public string Danmaku
            {
                get { return danmaku; }
                set
                {
                    danmaku = value;
                }
            }

            public int Timepass
            {
                get { return timepass; }
                set
                {
                    timepass = value;
                }
            }

            public bool IsSelected
            {
                get { return isSelected; }
                set
                {
                    isSelected = value;
                }
            }

            /// <summary>
            /// 是否是房管 包括主播
            /// </summary>
            public bool IsAdmin
            {
                get { return isAdmin; }
                set { isAdmin = value; }
            }

            public double Opacity
            {
                get { return timepass * 1.0 / EXPIRE_TIME; }
            }

            public void Timepass_increasement()
            {
                ++timepass;
            }

            public DanmakuItem() { }
            public DanmakuItem(string DanmakuIn = "", bool isAdm = false)
            {
                Danmaku = DanmakuIn;
                Timepass = 0;
                IsSelected = AutoDanmaku ? true : false;
                IsAdmin = isAdm;
            }

        }

        #endregion

        public MainWindow()
        {

            InitializeComponent();
            ErrorReset();
            LoadTutourial();
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 1);

            IsGreenTest = false;
            Cross_check = false;

            dispatcherTimerBling.Tick += new EventHandler(dispatcherTimerBling_Tick);
            dispatcherTimerBling.Interval = new TimeSpan(0, 0, 0, 0, 500);

            dispatcherTimerRefresh.Tick += new EventHandler(dispatcherTimerRefresh_Tick);
            dispatcherTimerRefresh.Interval = new TimeSpan(0, 0, 0, 1);

            dispatcherTimerEGY.Tick += new EventHandler(dispatcherTimerEGY_Tick);
            dispatcherTimerEGY.Interval = new TimeSpan(0, 0, 0, 0, 500);

            dispatcherTimerMon.Tick += new EventHandler(dispatcherTimerMon_Tick);
            dispatcherTimerMon.Interval = new TimeSpan(0, 0, 0, 10);

            //0.5s 更新一次
            dispatcherTimerDanmaku.Tick += new EventHandler(dispatcherTimerDanmaku_Tick);
            dispatcherTimerDanmaku.Interval = new TimeSpan(0, 0, 0, 0, 500);


            labelVer.Content = "版本: " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString() + "\n";
            labelVer.Content += "© Art Center 2019 - 2020, All Rights Reserved." + "\n";
            labelVer.Content += "Based on Open Source Project: Nginx, ffmpeg, VLC, bililive-dm";

            if (Environment.MachineName.Equals(Properties.Settings.Default.LastComputer))
                Unlock();
            else
                Lock();

            EGY_Reset();

            if (Properties.Settings.Default.NUffmpeg.Equals(true))
            {
                checkBoxtest.IsChecked = true;
                Warning_show();
            }
            else
            {
                checkBoxtest.IsChecked = false;
                Warning_Clear();
            }

            checkBox2.IsChecked = Properties.Settings.Default.Norestart;
            checkBoxFullRange.IsChecked = Properties.Settings.Default.fulltest;
            checkBoxCloseProtect.IsChecked = Properties.Settings.Default.CloseProtection;
            checkBoxCloseSniffing.IsChecked = Properties.Settings.Default.CloseYouget;

            configdata = new ObservableCollection<ConfigItem>
            {
                new ConfigItem(configcount,-1,"本地推流","rtmp://127.0.0.1:1935/live","rtmp://127.0.0.1:1935/live")
            };  //初始化
            configcount++;
            textBoxSourceName.Text = "流" + configcount;

            /* 测试 */
            //configdata.Add(new ConfigItem(++configcount, 0,"ceshi1","ceshi1"));
            //configdata.Add(new ConfigItem(++configcount, 0, "ceshi2", "ceshi2"));

            listViewOpt.ItemsSource = configdata;
            comboBoxSource.ItemsSource = configdata;

            RightCol.Width = new GridLength(0, GridUnitType.Pixel); //右栏宽初始值为0

            danmu.ReceivedDanmaku += b_ReceivedDanmaku;
            //b.ReceivedRoomCount += b_ReceivedRoomCount;

            FilterRegex = new Regex(regex);

            textBoxStoreSec.Text= (Properties.Settings.Default.StoreTime * 0.5).ToString();
            SliderStoreSec.Value = (double)(Properties.Settings.Default.StoreTime * 0.5);

            Hide_Monitor();
            listBoxDanmaku.MaxHeight = this.Height - 50;

            button7.Visibility = Visibility.Hidden;

        }

        #region 主页

        private void Unlock()
        {
            //解锁
            tabItemConfig.Visibility = Visibility.Visible;
            tabItemTest.Visibility = Visibility.Visible;
            tabItemStream.Visibility = Visibility.Visible;
            tabItemTutourial.Visibility = Visibility.Visible;
            TabItemSetting.Visibility = Visibility.Visible;
            button4.Visibility = Visibility.Hidden;
            passwordBox.Visibility = Visibility.Hidden;
            label8.Visibility = Visibility.Hidden;
            buttonSoftHelp.Visibility = Visibility.Visible;
            buttonDanmakuEntry.Visibility = Visibility.Visible;

            Properties.Settings.Default.LastComputer = (string)Environment.MachineName;
            Properties.Settings.Default.Save();
        }

        private void Lock()
        {
            //锁止
            tabItemConfig.Visibility = Visibility.Collapsed;
            tabItemTest.Visibility = Visibility.Collapsed;
            tabItemStream.Visibility = Visibility.Collapsed;
            tabItemTutourial.Visibility = Visibility.Collapsed;
            TabItemSetting.Visibility = Visibility.Collapsed;
            buttonSoftHelp.Visibility = Visibility.Hidden;
            buttonDanmakuEntry.Visibility = Visibility.Hidden;
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            button4.Content = "→";
            button4.Foreground = Brushes.Black;
        }

        private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Verify();
            }
        }

        private void Button4_Click_1(object sender, RoutedEventArgs e)
        {
            Verify();
        }

        private void Verify()
        {
            if (passwordBox.Password == "acbangbangbang")
                Unlock();
            else
            {
                button4.Content = "X";
                button4.Foreground = Brushes.Red;
            }
        }

        private void ButtonSoftHelp_Click(object sender, RoutedEventArgs e)
        {
            //软件帮助
            System.Diagnostics.Process.Start("https://github.com/LogCreative/ACLiveConsole/wiki");
        }

        #endregion

        #region 配置

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            AutoConfig();
            labelWebsite.Content = "直播网址";
        }



        /// <summary>
        /// 自动编码
        /// </summary>
        private void AutoConfig()
        {

            labelAdd.Visibility = Visibility.Visible;
            labelCode.Visibility = Visibility.Visible;
            labelMan.Visibility = Visibility.Visible;
            labelMan.Content = "推流代码";

            textBoxAdd.Visibility = Visibility.Visible;
            textBoxCode.Visibility = Visibility.Visible;
            textBoxMan.Visibility = Visibility.Visible;
            textBoxMan.IsEnabled = false;

            buttonPlus.Visibility = Visibility.Visible;

            textBoxAdd.Text = "";
            textBoxCode.Text = "";
            textBoxSourceName.Text = "流" + configcount;
            textBoxWebsite.Text = "";

            labelSourceName.Visibility = Visibility.Visible;
            textBoxSourceName.Visibility = Visibility.Visible;

            labelWebsite.Visibility = Visibility.Visible;
            textBoxWebsite.Visibility = Visibility.Visible;
            buttonlWHelp.Visibility = Visibility.Visible;

            textBoxAdd.IsReadOnly = false;
            textBoxCode.IsReadOnly = false;
            textBoxSourceName.IsReadOnly = false;
            textBoxWebsite.IsReadOnly = false;

            RefreshOutput();
        }

        /// <summary>
        /// 手动编码（自定义）
        /// </summary>
        private void ManualConfig()
        {

            labelAdd.Visibility = Visibility.Hidden;
            labelCode.Visibility = Visibility.Hidden;
            labelMan.Visibility = Visibility.Visible;
            labelMan.Content = "自定义";

            textBoxAdd.Visibility = Visibility.Hidden;
            textBoxCode.Visibility = Visibility.Hidden;
            textBoxMan.Visibility = Visibility.Visible;
            textBoxMan.IsEnabled = true;

            buttonPlus.Visibility = Visibility.Visible;
            buttonWrite.Visibility = Visibility.Visible;

            labelSourceName.Visibility = Visibility.Visible;
            textBoxSourceName.Visibility = Visibility.Visible;

            labelWebsite.Content = "播流地址";
            labelWebsite.Visibility = Visibility.Visible;
            textBoxWebsite.Visibility = Visibility.Visible;
            buttonlWHelp.Visibility = Visibility.Visible;

            textBoxMan.IsReadOnly = false;
            textBoxSourceName.IsReadOnly = false;
            textBoxWebsite.IsReadOnly = false;

            RefreshOutput();
        }

        int sourceType;
        //string pushCode= null;

        /// <summary>
        /// 刷新输出
        /// </summary>
        private void RefreshOutput()
        {

            InputError = false;
            /* 2.5.1 Style
            ErrorImageAdd.Visibility = Visibility.Hidden;
            labelErrorAdd.Visibility = Visibility.Hidden;
            ErrorImageCode.Visibility = Visibility.Hidden;
            labelErrorCode.Visibility = Visibility.Hidden;
            */
            var BordercolorGray = new SolidColorBrush(Color.FromArgb(100, 171, 173, 179));
            labelAdd.Foreground = Brushes.Black;
            textBoxAdd.BorderBrush = BordercolorGray;
            labelCode.Foreground = Brushes.Black;
            textBoxCode.BorderBrush = BordercolorGray;

            if (radioButtonB.IsChecked == true)
            {
                textBoxMan.Text = textBoxAdd.Text + textBoxCode.Text;
                sourceType = 0;
                //textBoxMan.Text = "push " + pushCode + ";"
            }
            else if (radioButtonWeibo.IsChecked == true)
            {   //微博配置文件
                textBoxMan.Text = textBoxAdd.Text + "/" + textBoxCode.Text;
                sourceType = 1;
                //textBoxMan.Text = "push " + pushCode + ";";
            }
            else
            {
                sourceType = 2;
                //pushCode = textBoxMan.Text;
            }

        }

        private void TextBoxCode_TextChanged_1(object sender, TextChangedEventArgs e)
        {
            RefreshOutput();
        }

        private void TextBoxAdd_TextChanged_1(object sender, TextChangedEventArgs e)
        {
            RefreshOutput();
        }

        private void RadioButtonMan_Checked_1(object sender, RoutedEventArgs e)
        {
            ManualConfig();
        }

        private void RadioButtonWeibo_Checked_1(object sender, RoutedEventArgs e)
        {
            AutoConfig();
            labelWebsite.Content = "播流地址";
        }

        /// <summary>
        /// 错误判定模块
        /// </summary>
        private void ButtonPlus_Click(object sender, RoutedEventArgs e)
        {

            string Errorstring = "";
            if (radioButtonMan.IsChecked == false)
            {
                if (textBoxAdd.Text == "")
                {
                    /* 2.5.1 Style
                    ErrorImageAdd.Visibility = Visibility.Visible;
                    labelErrorAdd.Visibility = Visibility.Visible;
                    */
                    labelAdd.Foreground = Brushes.Red;
                    textBoxAdd.BorderBrush = Brushes.Red;
                    InputError = true;
                    Errorstring += "串流地址 ";
                }
                if (textBoxCode.Text == "")
                {
                    /* 2.5.1 Style
                    ErrorImageCode.Visibility = Visibility.Visible;
                    labelErrorCode.Visibility = Visibility.Visible;
                    */
                    labelCode.Foreground = Brushes.Red;
                    textBoxCode.BorderBrush = Brushes.Red;
                    InputError = true;
                    Errorstring += "串流码 ";
                }
            }

            if (InputError == true)
            {
                buttonWrite.Visibility = Visibility.Visible;
                ErrorImage.Visibility = Visibility.Visible;
                labelError.Content = Errorstring + "为空！";
                labelError.Visibility = Visibility.Visible;
            }
            else
            {
                ErrorReset();
                Show_Monitor();

                if (editor == 1)
                {
                    textBoxOpt.Text += textBoxMan.Text + "\r\n";
                }
                else
                {
                    configdata.Add(new ConfigItem(configcount, sourceType, textBoxSourceName.Text.ToString(), textBoxMan.Text.ToString(), textBoxWebsite.Text.ToString()));
                    //configdata.Add(new ConfigItem(++configcount, 0, "ceshi","ceshi2"));
                    ++configcount;
                    textBoxSourceName.Text = "流" + configcount;
                }

            }

        }


        /// <summary>
        /// 读取现有的option.txt
        /// </summary>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            buttonWrite.Visibility = Visibility.Visible;
            try
            {
                ErrorReset();

                //清空文本框
                textBoxOpt.Text = "";

                // 创建一个 StreamReader 的实例来读取文件 
                // using 语句也能关闭 StreamReader
                using (StreamReader sr = new StreamReader("option.txt"))
                {
                    string line;

                    // 从文件读取并显示行，直到文件的末尾 
                    while ((line = sr.ReadLine()) != null)
                    {
                        textBoxOpt.Text += line + "\r\n";
                    }
                    CheckImage.Visibility = Visibility.Visible;
                    labelError.Content = "读取成功！";
                    labelError.Visibility = Visibility.Visible;
                }
            }
            catch
            {
                ErrorImage.Visibility = Visibility.Visible;
                labelError.Content = "读取错误！";
                labelError.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// option.txt 的错误重置
        /// </summary>
        private void ErrorReset()
        {
            ErrorImage.Visibility = Visibility.Hidden;
            labelError.Content = "No Error";
            labelError.Visibility = Visibility.Hidden;
            CheckImage.Visibility = Visibility.Hidden;
        }

        string goodopt = null;
        //bool changed = false;

        private void ButtonWrite_Click(object sender, RoutedEventArgs e)
        {
            ErrorReset();

            if (editor != 1)
            {
                textBoxOpt.Focus();
                //进行数据表录入
                textBoxOpt.Text = "";
                for (int i = 1; i < configcount; i++)
                {   //从第二个开始录入
                    if (configdata[i].StreamCode == "")
                        continue;   //跳过空项
                    textBoxOpt.Text += "push " + configdata[i].StreamCode + ";" + "\r\n";
                }
                goodopt = textBoxOpt.Text;
                //changed = false;
                recover_trans();
            }
            else
            {
                //正常写入
                if (textBoxOpt.Text == "")
                {
                    ErrorImage.Visibility = Visibility.Visible;
                    labelError.Content = "写入不能为空！";
                    labelError.Visibility = Visibility.Visible;
                }
                else
                {
                    try
                    {
                        File.WriteAllText("option.txt", textBoxOpt.Text);
                        CheckImage.Visibility = Visibility.Visible;
                        labelError.Content = "写入成功！";
                        labelError.Visibility = Visibility.Visible;
                    }
                    catch
                    {
                        ErrorImage.Visibility = Visibility.Visible;
                        labelError.Content = "写入错误！";
                        labelError.Visibility = Visibility.Visible;
                    }
                }
            }
        }

        private void TextBoxOpt_TextChanged(object sender, TextChangedEventArgs e)
        {
            ErrorReset();
            if (textBoxOpt.Text.Equals(goodopt).Equals(false))
                ban_trans();
        }

        private void ButtonRecover_Click(object sender, RoutedEventArgs e)
        {
            recover_trans();
            //changed = false;
        }

        private void ban_trans()
        {

            buttonRecover.Visibility = Visibility.Visible;
            labelEditor.Content = "禁止转换";
            sliderEditor.IsEnabled = false;
        }

        private void recover_trans()
        {
            if (goodopt != null)
            {
                textBoxOpt.Text = goodopt;
            }
            buttonRecover.Visibility = Visibility.Hidden;
            labelEditor.Content = "编辑器转换";
            sliderEditor.IsEnabled = true;
        }

        double editor = 0;    //编辑器选择（中间值）：0 - 数据形式； 1 - 文本形式
        //默认为数据形式，升级后不可降级，除非复原。 有待商榷

        private void SliderEditor_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            editor = sliderEditor.Value;        //单向绑定
            DataCol.Width = new GridLength(1 - editor, GridUnitType.Star);
            SeperateCol.Width = new GridLength((-4 * editor * editor + 4 * editor) * 15, GridUnitType.Pixel);
            TextCol.Width = new GridLength(editor, GridUnitType.Star);
            if (editor == 0)
            {
                sliderEditor.ToolTip = "数据表基本模式";
            }
            else if (editor == 1)
            {
                sliderEditor.ToolTip = "文本框高级模式";
            }
            else
                sliderEditor.ToolTip = "中间值";
            OptOpr(editor);
        }

        private void ListViewOpt_GotFocus(object sender, RoutedEventArgs e)
        {
            if (editor != 0)
            {
                Storyboard SlideOut = this.FindResource("SliderAnimation") as Storyboard;
                (SlideOut.Children[0] as DoubleAnimation).From = sliderEditor.Value;
                (SlideOut.Children[0] as DoubleAnimation).To = 0;
                (SlideOut.Children[0] as DoubleAnimation).Duration = TimeSpan.FromSeconds(0.5 * sliderEditor.Value);
                SlideOut.Begin(sliderEditor, true);
            }
        }

        private void TextBoxOpt_GotFocus(object sender, RoutedEventArgs e)
        {
            if (editor != 1)
            {
                Storyboard SlideIn = this.FindResource("SliderAnimation") as Storyboard;
                (SlideIn.Children[0] as DoubleAnimation).From = sliderEditor.Value;
                (SlideIn.Children[0] as DoubleAnimation).To = 1;
                (SlideIn.Children[0] as DoubleAnimation).Duration = TimeSpan.FromSeconds(0.5 * (1 - sliderEditor.Value));
                SlideIn.Begin(sliderEditor, true);
            }
        }

        private void DoubleAnimation_Completed(object sender, EventArgs e)
        {
            Storyboard Slider = this.FindResource("SliderAnimation") as Storyboard;
            Slider.Remove(sliderEditor);
            sliderEditor.Value = editor;
        }

        private void OptOpr(double editor)
        {
            if (editor == 1)
            {   //文本框模式
                buttonRead.Visibility = Visibility.Visible;
                buttonWrite.Content = "写入";
            }
            else
            {   //数据表模式
                buttonRead.Visibility = Visibility.Hidden;
                buttonWrite.Content = "转换";
            }

        }

        //不能改
        //private void ButtonUpdate_Click(object sender, RoutedEventArgs e)
        //{
        //    if (listViewOpt.SelectedItems!=null)
        //    {
        //        if (radioButtonB.IsChecked == true)
        //            configdata[listViewOpt.SelectedIndex].Type = "B站";
        //        else if (radioButtonWeibo.IsChecked == true)
        //            Configdata[listViewOpt.SelectedIndex].Type = "微博";
        //        else
        //            Configdata[listViewOpt.SelectedIndex].Type = "自定义";

        //        configdata[listViewOpt.SelectedIndex].StreamCode= textBoxMan.Text;
        //        configdata[listViewOpt.SelectedIndex].SourceName= textBoxSourceName.Text;
        //        configdata[listViewOpt.SelectedIndex].LiveViewingSite = textBoxWebsite.Text ;

        //    }
        //}

        private void ListViewOpt_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (listViewOpt.SelectedItems.Count > 0)
            {   //有选项选中
                //buttonUpdate.IsEnabled = true;
                //buttonDelteOne.IsEnabled = true;
                buttonWeb.IsEnabled = true;
                buttonClearSelect.IsEnabled = true;

                textBoxMan.IsReadOnly = true;
                textBoxSourceName.IsReadOnly = true;
                textBoxWebsite.IsReadOnly = true;
                //选取：信息合并后只能是自定义
                radioButtonMan.IsChecked = true;
                //信息填入
                textBoxMan.Text = configdata[listViewOpt.SelectedIndex].StreamCode;
                textBoxSourceName.Text = configdata[listViewOpt.SelectedIndex].SourceName;
                textBoxWebsite.Text = configdata[listViewOpt.SelectedIndex].LiveViewingSite;
            }
            else
            {
                //buttonUpdate.IsEnabled = false;
                //buttonDelteOne.IsEnabled = false;
                buttonWeb.IsEnabled = false;
                buttonClearSelect.IsEnabled = false;
                textBoxMan.IsReadOnly = false;
                textBoxSourceName.IsReadOnly = false;
                textBoxWebsite.IsReadOnly = false;
            }
        }

        //严格的序号定义不允许删除单项
        //private void ButtonDelteOne_Click(object sender, RoutedEventArgs e)
        //{
        //    //因序号问题 只会清空内容
        //    configdata.Remove(configdata[listViewOpt.SelectedIndex]);
        //    //configdata[listViewOpt.SelectedIndex].Type = "";
        //    //configdata[listViewOpt.SelectedIndex].StreamCode = "";
        //    //configdata[listViewOpt.SelectedIndex].SourceName = "";
        //    //configdata[listViewOpt.SelectedIndex].LiveViewingSite = "";
        //}

        private void ButtonDelteAll_Click(object sender, RoutedEventArgs e)
        {
            listViewOpt.SelectedItems.Clear();
            listViewOpt.ItemsSource = null;
            listViewOpt.Items.Clear();
            configcount = 0;
            configdata = new ObservableCollection<ConfigItem> {
                new ConfigItem(configcount,-1,"本地推流","rtmp://127.0.0.1:1935/live","rtmp://127.0.0.1:1935/live")
            };
            ++configcount;
            listViewOpt.ItemsSource = configdata;
            textBoxSourceName.Text = "流" + configcount;
        }

        private void ButtonWeb_Click(object sender, RoutedEventArgs e)
        {
            //打开网页
            try
            {
                System.Diagnostics.Process.Start(configdata[listViewOpt.SelectedIndex].LiveViewingSite);
            }
            catch (Exception)
            {
                MessageBox.Show("无法打开网页。", "错误", 0, MessageBoxImage.Error);
            }
        }

        private void ButtonClearSelect_Click(object sender, RoutedEventArgs e)
        {
            //清除选区
            listViewOpt.SelectedItems.Clear();
        }

        private void ButtonlWHelp_Click(object sender, RoutedEventArgs e)
        {//播流地址的帮助
            System.Diagnostics.Process.Start("https://github.com/LogCreative/ACLiveConsole/wiki/%E5%A6%82%E4%BD%95%E8%8E%B7%E5%8F%96%E6%92%AD%E6%B5%81%E5%9C%B0%E5%9D%80");
        }

        #endregion

        #region 测试

        /// <summary>
        /// 进行测试
        /// </summary>
        private void RunTest()
        {
            dispatcherTimerBling.Start();

            Testlabel.Content = "就绪";
            TestProgress.Foreground = Brushes.Green;
            TestProgress.Value = 0;
            time_left = 10;

            try
            {
                imageProtection.Visibility = Visibility.Hidden;
                labelProtection.Visibility = Visibility.Hidden;
                buttonHelp.IsEnabled = true;

                Testlabel.Content = "建立测试前档案...";
                TestProgress.Value += 10;
                System.IO.File.Copy(@".\logs\error.log", @".\logs\errorcomp1.log", true);

                Testlabel.Content = "打开 Nginx...";
                Process.Start("Nginx.exe");
                TestProgress.Value += 10;


                dispatcherTimer.Start();


            }
            catch (Exception e)
            {
                Testlabel.Content = "错误：" + e.Message;
                TestProgress.Foreground = Brushes.Red;

                button1.IsEnabled = false;

                Process.Start("Nginx.exe", "-s stop");//关闭

                dispatcherTimerBling.Stop();

                button.IsEnabled = true;

            }

        }

        /// <summary>
        /// 测试计时器到时间
        /// </summary>
        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            time_left -= 1;
            if (time_left <= 0)
            {
                try
                {
                    dispatcherTimer.Stop();

                    button1.IsEnabled = false;


                    Process.Start("Nginx.exe", "-s stop").WaitForExit();//关闭

                    Testlabel.Content = "建立测试后档案...";
                    System.IO.File.Copy(@".\logs\error.log", @".\logs\errorcomp2.log", true);
                    TestProgress.Value += 10;

                    Testlabel.Content = "正在比较...";
                    Compare();
                    TestProgress.Value += 10;

                    Testlabel.Content = "正在生成报告...";
                    Highlight();
                    TestProgress.Value += 10;

                    Testlabel.Content = "测试完成";
                    TestProgress.Value += 10;

                    labelUpTime.Content = "更新时间: " + DateTime.Now.ToLocalTime().ToString();

                    dispatcherTimerBling.Stop();

                    if (red_count > 0)
                    {
                        label.Background = Brushes.Red;
                        label.Foreground = Brushes.White;
                        IsGreenTest = true;
                        Testlabel.Content += "，出现异常";
                        imageProtection.Visibility = Visibility.Visible;
                        labelProtection.Content = "保护：测试未通过！";
                        labelProtection.Visibility = Visibility.Visible;
                        if (checkBoxCloseProtect.IsChecked == false)
                        {
                            buttonHelp.IsEnabled = false;
                        }
                    }
                    else
                    {
                        label.Background = Brushes.Green;
                        label.Foreground = Brushes.White;
                        IsGreenTest = true;
                    }

                    button.IsEnabled = true;

                }
                catch (Exception ex)
                {
                    Testlabel.Content = "错误：" + ex.Message;
                    TestProgress.Foreground = Brushes.Red;

                    button1.IsEnabled = false;

                    dispatcherTimerBling.Stop();
                    label.Background = Brushes.Red;
                    label.Foreground = Brushes.White;
                    IsGreenTest = true;

                    Process.Start("Nginx.exe", "-s stop");//关闭

                    button.IsEnabled = true;


                }
            }
            else
            {
                if (time_left != 1)
                {
                    Testlabel.Content = "测试中, 剩余 " + time_left + " 秒...";
                    Testwithffmpeg();
                }
                else
                    Testlabel.Content = "请稍候...";

                TestProgress.Value += 5;

            }
        }

        /// <summary>
        /// 启动测试影片推流
        /// </summary>
        private void Testwithffmpeg()
        {
            if (checkBoxtest.IsChecked == false)
            {
                try
                {
                    using (Process process = new System.Diagnostics.Process())
                    {
                        process.StartInfo.FileName = "ffmpeg.exe";
                        process.StartInfo.Arguments = "-re -i LiveTest.mp4 -vcodec copy -acodec copy -f flv -y rtmp://127.0.0.1:1935/live";
                        // 必须禁用操作系统外壳程序  
                        process.StartInfo.UseShellExecute = false;
                        process.StartInfo.CreateNoWindow = true;
                        process.StartInfo.RedirectStandardOutput = true;

                        process.Start();

                        string output = process.StandardOutput.ReadToEnd();

                        process.WaitForExit();
                        process.Close();
                    }
                }
                catch (Exception ex)
                {
                    Testlabel.Content = ex.Message;
                }
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            button1.IsEnabled = true;
            button.IsEnabled = false;

            RunTest();
        }

        /// <summary>
        /// 使用系统的fc.exe -ivg 比较报告文件
        /// </summary>
        private void Compare()
        {

            this.richTextBox.Document.Blocks.Clear();
            if (checkBoxFullRange.IsChecked == false)
            {
                using (Process process = new System.Diagnostics.Process())
                {
                    process.StartInfo.FileName = "fc";
                    process.StartInfo.Arguments = "/ivg .\\logs\\errorcomp1.log .\\logs\\errorcomp2.log";
                    // 必须禁用操作系统外壳程序  
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.RedirectStandardOutput = true;

                    process.Start();

                    string output = process.StandardOutput.ReadToEnd();

                    if (String.IsNullOrEmpty(output) == false)
                        this.richTextBox.AppendText(output);

                    process.WaitForExit();
                    process.Close();
                }
            }
            else
            {

                TextRange textRange;
                FileStream fs;
                textRange = new TextRange(richTextBox.Document.ContentStart, richTextBox.Document.ContentEnd);
                using (fs = new FileStream(".\\logs\\errorcomp2.log", System.IO.FileMode.OpenOrCreate))
                {
                    textRange.Load(fs, DataFormats.Text);
                }

            }

        }

        //没错下面是抄的

        /// <summary>
        /// 高亮测试报告中的关键词
        /// </summary>
        private void Highlight()
        {

            red_count = 0;

            //删除行,首三行和末尾两行
            int i;

            for (i = 1; i <= 3; ++i)
                richTextBox.Document.Blocks.Remove(richTextBox.Document.Blocks.FirstBlock);
            for (i = 1; i <= 2; ++i)
                richTextBox.Document.Blocks.Remove(richTextBox.Document.Blocks.LastBlock);

            //高亮
            Isred = false;
            foreach (string s in GetBlueKeyWords())
            {
                ChangeColor(System.Windows.Media.Colors.Blue, richTextBox, s);//tx是RichTextBox控件名字
            }

            Isred = true;
            foreach (string s in GetRedKeyWords())
            {
                ChangeColor(System.Windows.Media.Colors.Red, richTextBox, s);//tx是RichTextBox控件名字
            }

            textBoxRedCount.Text = red_count + " ";
        }

        private void Button1_Click(object sender, RoutedEventArgs e)
        {
            button1.IsEnabled = false;

            dispatcherTimer.Stop();

            Process.Start("Nginx.exe", "-s stop");//关闭

            Testlabel.Content = "已经中止测试";
            TestProgress.Foreground = Brushes.Red;

            dispatcherTimerBling.Stop();

            button.IsEnabled = true;
        }


        public void ChangeColor(Color l, RichTextBox richBox, string keyword)
        {
            //设置文字指针为Document初始位置 
            //richBox.Document.FlowDirection 
            TextPointer position = richBox.Document.ContentStart;
            while (position != null)
            {
                //向前搜索,需要内容为Text 
                if (position.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.Text)
                {
                    //拿出Run的Text 
                    string text = position.GetTextInRun(LogicalDirection.Forward);
                    //可能包含多个keyword,做遍历查找 
                    int index = 0;
                    index = text.IndexOf(keyword, 0);
                    if (index != -1)
                    {
                        TextPointer start = position.GetPositionAtOffset(index);
                        TextPointer end = start.GetPositionAtOffset(keyword.Length);
                        position = selecta(l, richBox, keyword.Length, start, end);
                    }

                }
                //文字指针向前偏移 
                position = position.GetNextContextPosition(LogicalDirection.Forward);

            }
        }

        public TextPointer selecta(Color l, RichTextBox richTextBox1, int selectLength, TextPointer tpStart, TextPointer tpEnd)
        {
            TextRange range = richTextBox1.Selection;
            range.Select(tpStart, tpEnd);
            //高亮选择 

            range.ApplyPropertyValue(TextElement.ForegroundProperty, new SolidColorBrush(l));
            range.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Bold);

            if (Isred)
                red_count++;

            return tpEnd.GetNextContextPosition(LogicalDirection.Forward);
        }

        /// <summary>
        /// 蓝色的的关键词
        /// </summary>
        public string[] GetBlueKeyWords()
        {

            string[] res = { "start", "exiting", "started" };
            return res;

        }

        /// <summary>
        /// 红色的的关键词
        /// </summary>
        public string[] GetRedKeyWords()
        {
            string[] res = { "disconnect", "deleteStream", "relay", "emerg" };
            return res;

        }



        /// <summary>
        /// 测试计时器闪灯
        /// </summary>
        private void dispatcherTimerBling_Tick(object sender, EventArgs e)
        {
            if (IsGreenTest)
            {
                label.Background = Brushes.Transparent;
                label.Foreground = Brushes.Black;
                IsGreenTest = false;
            }
            else
            {
                label.Background = Brushes.Green;
                label.Foreground = Brushes.White;
                IsGreenTest = true;
            }
        }

        #endregion

        #region 推流

        // TODO: 推流监测模式应当改为程序内部存储。使用文件读取可能会有损性能。

        private void Button_StartLive_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("Start_Nginx.bat");
        }

        private void Button_EndLive_Click(object sender, RoutedEventArgs e)
        {
            EndLive();
        }

        private void EndLive(bool nginxs = true)
        {
            Process.Start("Nginx.exe", "-s stop");//关闭

            //销毁开始时间记录文件

            if (File.Exists(".\\logs\\start.log") == true)
            {
                try
                {
                    File.Delete(".\\logs\\start.log");
                }
                catch (Exception ex)
                {
                    labelLive.Content = "错误：" + ex.Message;
                }

            }
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (tabControl.SelectedIndex == 3)
            {
                button_StartLive.IsEnabled = false;
                button_EndLive.IsEnabled = false;
                buttonClock.IsEnabled = false;
                textBoxClock.IsEnabled = false;
                labelLive.Content = "初始化...";
                dispatcherTimerRefresh.Start();
            }
            else
            {
                dispatcherTimerRefresh.Stop();

            }
        }

        /// <summary>
        /// 刷新运行状态的计时器
        /// </summary>
        private void dispatcherTimerRefresh_Tick(object sender, EventArgs e)
        {
            if (System.Diagnostics.Process.GetProcessesByName("nginx").ToList().Count > 0)
            {
                labelLive.Content = "Nginx 正在运行...";
                button_StartLive.IsEnabled = false;
                button_EndLive.IsEnabled = true;
                label1.Background = Brushes.Red;
                label1.Foreground = Brushes.White;
                textBoxLiveTime.Foreground = Brushes.Red;
            }
            else
            {
                labelLive.Content = "Nginx 尚未运行";
                button_StartLive.IsEnabled = true;
                button_EndLive.IsEnabled = false;
                label1.Background = Brushes.Transparent;
                label1.Foreground = Brushes.Black;

                textBoxLiveTime.Background = Brushes.Transparent;
                textBoxLiveTime.Foreground = Brushes.Black;
            }
            //前一个需要从文本读取

            string time1 = null;

            // 创建一个 StreamReader 的实例来读取文件 
            // using 语句也能关闭 StreamReader
            try
            {
                using (StreamReader sr = new StreamReader(".\\logs\\start.log"))
                    time1 = sr.ReadLine();

                DateTime starttime = Convert.ToDateTime(time1);

                DateTime nowtime = DateTime.Now;

                textBoxLiveTime.Text = DateDiff(starttime, nowtime);

                //当开始推流时判断
                buttonClock.IsEnabled = true;
                textBoxClock.IsEnabled = true;
                if (endset == true)
                {   //定时器被设置
                    textBoxClock.Text = settime.Subtract(nowtime).Minutes.ToString();
                    labelLive.Content += "并将于 " + textBoxClock.Text + " 分钟后关闭...";
                    buttonClock.BorderBrush = Brushes.Red;
                    if (textBoxClock.Text == "0" && endset == true)
                    {
                        if (checkBoxClosingAnim.IsChecked == true)
                        {
                            labelLive.Content = "正在关闭OBS...";
                            //强制关闭obs
                            if (System.Diagnostics.Process.GetProcessesByName("obs64").ToList().Count > 0)
                            {
                                try
                                {
                                    //强制关闭进程
                                    System.Diagnostics.Process[] ps = System.Diagnostics.Process.GetProcessesByName("obs64");

                                    foreach (System.Diagnostics.Process p in ps)
                                    {
                                        p.Kill();
                                    }
                                }
                                catch (Exception ex)
                                {
                                    throw ex;
                                }

                            }
                            else if (System.Diagnostics.Process.GetProcessesByName("obs32").ToList().Count > 0)
                            {
                                try
                                {
                                    //强制关闭进程
                                    System.Diagnostics.Process[] ps = System.Diagnostics.Process.GetProcessesByName("obs32");

                                    foreach (System.Diagnostics.Process p in ps)
                                    {
                                        p.Kill();
                                    }
                                }
                                catch (Exception ex)
                                {
                                    throw ex;
                                }
                            }

                            //再启动ffmpeg推入

                            labelLive.Content = "正在推流关闭视频...";
                            try
                            {
                                using (Process process = new System.Diagnostics.Process())
                                {
                                    process.StartInfo.FileName = "ffmpeg.exe";
                                    process.StartInfo.Arguments = "-re -i Closing.mp4 -vcodec copy -acodec copy -f flv -y rtmp://127.0.0.1:1935/live";
                                    // 必须禁用操作系统外壳程序  
                                    process.StartInfo.UseShellExecute = false;
                                    process.StartInfo.CreateNoWindow = true;
                                    process.StartInfo.RedirectStandardOutput = true;


                                    process.Start();

                                    string output = process.StandardOutput.ReadToEnd();

                                    process.WaitForExit();
                                    process.Close();
                                }
                            }
                            catch (Exception ex)
                            {
                                Testlabel.Content = ex.Message;
                            }

                        }
                        EndLive();
                        textBoxClock.Text = "";
                        endset = false;
                    }
                }
                else
                    buttonClock.BorderBrush = Brushes.Gray;

            }
            catch (FileNotFoundException)
            {
                labelLive.Content = "Nginx 尚未运行";
                button_StartLive.IsEnabled = true;
                button_EndLive.IsEnabled = false;
                label1.Background = Brushes.Transparent;
                label1.Foreground = Brushes.Black;
                textBoxLiveTime.Background = Brushes.Transparent;
                textBoxLiveTime.Foreground = Brushes.Black;
                buttonClock.IsEnabled = false;
                buttonClock.BorderBrush = Brushes.Gray;
                endset = false;
                textBoxClock.Text = "";
                textBoxClock.IsEnabled = false;
            }
            catch (Exception ex)
            {
                labelLive.Content = "错误：" + ex.Message;
            }


        }

        /// <summary>
        /// 关闭定时器
        /// </summary>
        DateTime settime;
        bool endset = false;
        private void ButtonClock_Click(object sender, RoutedEventArgs e)
        {
            if (endset == false)
            {
                if (textBoxClock.Text != "")
                {
                    //定时器
                    DateTime nowtime = DateTime.Now;
                    settime = nowtime.AddMinutes(clockmin + 1);
                    endset = true;
                }
            }
            else
            {
                textBoxClock.Text = "";
                endset = false;
                clockmin = -1;
            }
        }

        int clockmin = -1;//设定分钟数
        private void TextBoxClock_TextChanged(object sender, TextChangedEventArgs e)
        {//强制数字输入，且为自然数
            TextBox tempbox = sender as TextBox;
            TextChange[] change = new TextChange[e.Changes.Count];
            e.Changes.CopyTo(change, 0);
            int offset = change[0].Offset;
            if (change[0].AddedLength > 0)
            {

                if (tempbox.Text.IndexOf(' ') != -1 || !int.TryParse(tempbox.Text, out clockmin) || clockmin < 0)
                {
                    tempbox.Text = tempbox.Text.Remove(offset, change[0].AddedLength);
                    tempbox.Select(offset, 0);
                }

            }
        }


        /// <summary>
        /// 计算两个日期的时间间隔
        /// </summary>
        /// <param name="DateTime1">第一个日期和时间</param>
        /// <param name="DateTime2">第二个日期和时间</param>
        /// <returns></returns>
        private string DateDiff(DateTime DateTime1, DateTime DateTime2)
        {
            string dateDiff = null;

            TimeSpan ts1 = new TimeSpan(DateTime1.Ticks);
            TimeSpan ts2 = new TimeSpan(DateTime2.Ticks);
            TimeSpan ts = ts1.Subtract(ts2).Duration();

            if (ts.Hours < 10)
                dateDiff = "0";
            dateDiff += ts.Hours.ToString() + ":";

            if (ts.Minutes < 10)
                dateDiff += "0";
            dateDiff += ts.Minutes.ToString() + ":";

            if (ts.Seconds < 10)
                dateDiff += "0";
            dateDiff += ts.Seconds.ToString();

            return dateDiff;
        }

        /// <summary>
        /// 应急预案重置
        /// </summary>
        private void EGY_Reset()
        {
            EGY1_Reset();
            EGY2_Reset();
        }

        /// <summary>
        /// 应急预案：断线重连重置
        /// </summary>
        private void EGY1_Reset()
        {
            EGY = false;
            richTextBox1.Visibility = Visibility.Hidden;
            button2.Visibility = Visibility.Hidden;
            button3.Visibility = Visibility.Hidden;

            buttonEGY.Content = "启用断线重连";
            buttonEGY.Foreground = Brushes.Black;

            dispatcherTimerEGY.Stop();
        }

        /// <summary>
        /// 应急预案：程序冲突重置
        /// </summary>
        private void EGY2_Reset()
        {
            richTextBox2.Visibility = Visibility.Hidden;
            button5.Visibility = Visibility.Hidden;
            buttonCross.Visibility = Visibility.Hidden;

            ButtonQuitAll.Content = "解决程序冲突";
            ButtonQuitAll.Foreground = Brushes.Black;

            EGY = false;

            dispatcherTimerEGY.Stop();
        }

        private void Button2_Click(object sender, RoutedEventArgs e)
        {
            tabControl.SelectedIndex = 1;
        }

        private void Button3_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("Nginx.exe", "-s stop").WaitForExit();//关闭

            Process.Start("Start_Nginx.bat");

            EGY1_Reset();
        }

        private void Button4_Click(object sender, RoutedEventArgs e)
        {
            if (EGY == false)
            {
                richTextBox1.Visibility = Visibility.Visible;
                button2.Visibility = Visibility.Visible;
                button3.Visibility = Visibility.Visible;

                buttonEGY.Content = "解除断线重连";
                buttonEGY.Foreground = Brushes.Red;

                EGY = true;

                dispatcherTimerEGY.Start();
            }
            else
            {
                EGY1_Reset();
            }
        }

        /// <summary>
        /// 推流应急灯闪
        /// </summary>
        private void dispatcherTimerEGY_Tick(object sender, EventArgs e)
        {
            if (!Cross_check)
            {
                if (Redlight == true)
                {
                    label1.Foreground = Brushes.Black;
                    label1.Background = Brushes.Transparent;

                    Redlight = false;
                }
                else
                {
                    label1.Foreground = Brushes.White;
                    label1.Background = Brushes.Red;

                    Redlight = true;
                }
            }
            else
            {
                if (System.Diagnostics.Process.GetProcessesByName("nginx").ToList().Count > 0)
                {
                    try
                    {
                        //强制关闭进程
                        System.Diagnostics.Process[] ps = System.Diagnostics.Process.GetProcessesByName("nginx");

                        foreach (System.Diagnostics.Process p in ps)
                        {
                            labelLive.Content = "正在解决程序冲突...";
                            p.Kill();
                        }
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }

                }
                else
                {
                    labelLive.Content = "程序冲突解决结束";
                    EndLive(false);
                    if (checkBox2.IsChecked == false)
                    {
                        labelLive.Content += "，准备开始推流...";

                        Process.Start("Start_Nginx.bat");

                    }
                    dispatcherTimerRefresh.Start();
                    EGY2_Reset();
                }
            }

        }

        private void ButtonQuitAll_Click(object sender, RoutedEventArgs e)
        {
            if (EGY == false)
            {
                richTextBox2.Visibility = Visibility.Visible;
                button5.Visibility = Visibility.Visible;
                buttonCross.Visibility = Visibility.Visible;

                ButtonQuitAll.Content = "终止解决冲突";
                ButtonQuitAll.Foreground = Brushes.Red;

                EGY = true;
                Cross_check = false;
                Redlight = false;
                dispatcherTimerEGY.Start();

            }
            else
            {
                EGY2_Reset();
            }
        }

        private void ButtonCross_Click(object sender, RoutedEventArgs e)
        {
            Cross_check = true;
            dispatcherTimerRefresh.Stop();
        }
        #endregion

        #region 教程

        /// <summary>
        /// 加载教程
        /// </summary>
        private void LoadTutourial()
        {
            try
            {
                TextRange textRange;
                FileStream fs;
                textRange = new TextRange(richTextBoxTu.Document.ContentStart, richTextBoxTu.Document.ContentEnd);
                using (fs = new FileStream("Tutourial.rtf", System.IO.FileMode.OpenOrCreate))
                {
                    textRange.Load(fs, DataFormats.Rtf);
                }
            }
            catch
            {
                richTextBoxTu.AppendText("\r\n读取错误！");
            }

        }

        #endregion

        #region 设置

        private void CheckBoxtest_Checked(object sender, RoutedEventArgs e)
        {
            Warning_show();
            checkBoxClosingAnim.IsChecked = false;
            checkBoxClosingAnim.IsEnabled = false;
        }

        private void CheckBoxtest_Unchecked(object sender, RoutedEventArgs e)
        {
            Warning_Clear();
            checkBoxClosingAnim.IsEnabled = true;
        }

        /// <summary>
        /// 显示ffmpeg警告信息
        /// </summary>
        private void Warning_show()
        {
            Properties.Settings.Default.NUffmpeg = true;
            WarPic.Visibility = Visibility.Visible;
            Rec1.Visibility = Visibility.Visible;
            labell1.Visibility = Visibility.Visible;
            labell2.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// 关闭ffmpeg警告信息
        /// </summary>
        private void Warning_Clear()
        {
            Properties.Settings.Default.NUffmpeg = false;
            WarPic.Visibility = Visibility.Hidden;
            Rec1.Visibility = Visibility.Hidden;
            labell1.Visibility = Visibility.Hidden;
            labell2.Visibility = Visibility.Hidden;
        }

        private void CheckBox2_Checked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.Norestart = true;
        }

        private void CheckBox2_Unchecked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.Norestart = false;
        }

        private void Button6_Click(object sender, RoutedEventArgs e)
        {
            button7.Visibility = Visibility.Visible;
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.fulltest = true;
        }

        private void CheckBoxFullRange_Unchecked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.fulltest = false;
        }

        private void CheckBoxCloseProtect_Checked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.CloseProtection = true;
        }

        private void CheckBoxCloseProtect_Unchecked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.CloseProtection = false;
        }

        private void Button7_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.Reset();
            button7.Visibility = Visibility.Hidden;
            button6.Content = "所有设置已清除";
            button6.IsEnabled = false;
        }

        private void CheckBoxCloseSniffing_Checked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.CloseYouget = true;
        }

        private void CheckBoxCloseSniffing_Unchecked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.CloseYouget = false;
        }

        #endregion

        #region 监视器

        // TODO: 将窗口的图像使用 WriteableBitmap 覆盖到监视器上。

        double RightCol_Now;

        private void ButtonHelp_Click(object sender, RoutedEventArgs e)
        {
            SideBar();
        }

        private void SideBar()
        {
            Storyboard expandRightCol = this.FindResource("expandRightCol") as Storyboard;
            if (RightCol.Width.Value.Equals(0))
            {
                if (expandRightCol != null)
                {
                    (expandRightCol.Children[0] as GridLengthAnimation).From = new GridLength(0, GridUnitType.Pixel);
                    (expandRightCol.Children[0] as GridLengthAnimation).To = new GridLength(RightCol_Last, GridUnitType.Pixel);

                    expandRightCol.Begin(RightCol, HandoffBehavior.SnapshotAndReplace, true);
                }

                buttonHelp.Content = "关闭监视";
            }
            else
            {
                RightCol_Now = RightCol.Width.Value;

                expandRightCol.Pause();

                (expandRightCol.Children[0] as GridLengthAnimation).From = new GridLength(RightCol_Now, GridUnitType.Pixel);
                (expandRightCol.Children[0] as GridLengthAnimation).To = new GridLength(0, GridUnitType.Pixel);
                (expandRightCol.Children[0] as GridLengthAnimation).Duration = new Duration(TimeSpan.FromSeconds(0.5 * (RightCol_Now / 300)));

                expandRightCol.Begin(RightCol, HandoffBehavior.SnapshotAndReplace, true);

                //关闭所有媒体。

                buttonHelp.Content = "启动监视";
            }
            imageProtection.Visibility = Visibility.Hidden;
            labelProtection.Visibility = Visibility.Hidden;
        }


        private void GridLengthAnimation_Completed(object sender, EventArgs e)
        {
            Storyboard expandRightCol = this.FindResource("expandRightCol") as Storyboard;

            if (expandRightCol != null)
            {
                RightCol_Now = RightCol.Width.Value;
                expandRightCol.Remove(RightCol);    //解除限制
                RightCol.Width = new GridLength(RightCol_Now, GridUnitType.Pixel);  //到达最终值
            }
        }

        byte selectedItem = 0;
        private void selectItem(byte selec)
        {
            selectedItem = selec;
            expander1.IsEnabled = true;
            comboBoxSource.IsEnabled = true;
            buttonSolo.IsEnabled = true;
            MonitoringChanged();
        }

        private void ButtonLU_Click(object sender, RoutedEventArgs e)
        {
            selectItem(1);
        }

        private void ButtonRU_Click(object sender, RoutedEventArgs e)
        {
            selectItem(2);
        }

        private void ButtonLD_Click(object sender, RoutedEventArgs e)
        {
            selectItem(3);
        }

        private void ButtonRD_Click(object sender, RoutedEventArgs e)
        {
            selectItem(4);
        }

        private void MonitoringChanged()
        {
            BorderLU.BorderBrush.Opacity = (selectedItem == 1 ? 1 : 0);
            BorderRU.BorderBrush.Opacity = (selectedItem == 2 ? 1 : 0);
            BorderLD.BorderBrush.Opacity = (selectedItem == 3 ? 1 : 0);
            BorderRD.BorderBrush.Opacity = (selectedItem == 4 ? 1 : 0);
            comboBoxSource.SelectedIndex = -1;
        }

        /* 暂时没有搞清楚*/
        private void ButtonLU_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Monitoring.ColumnDefinitions[1].Width = new GridLength(0, GridUnitType.Star);
                Monitoring.RowDefinitions[1].Height = new GridLength(0, GridUnitType.Star);
            }
        }

        private void ButtonLU_KeyUp(object sender, KeyEventArgs e)
        {
            Monitoring.ColumnDefinitions[1].Width = new GridLength(1, GridUnitType.Star);
            Monitoring.RowDefinitions[1].Height = new GridLength(1, GridUnitType.Star);
        }

        private VlcVideoSourceProvider sourceProvider;
        private void ComboBoxSource_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string PlayStream = null;
            if (comboBoxSource.SelectedIndex != -1)
            {
                PlayStream = configdata[comboBoxSource.SelectedIndex].LiveViewingSite.ToString();
            }
            else
            {
                if (expander1.IsExpanded == false && textboxManStreaming.Text != "")
                {
                    PlayStream = textboxManStreaming.Text;
                    textboxManStreaming.Text = "";
                }
                else
                {
                    textboxManStreaming.Text = "";
                    PlayStream = null;
                }
            }

            if (PlayStream != null)
            {
                try
                {
                    var currentAssembly = Assembly.GetEntryAssembly();
                    var currentDirectory = new FileInfo(currentAssembly.Location).DirectoryName;
                    // Default installation path of VideoLAN.LibVLC.Windows
                    var libDirectory = new DirectoryInfo(System.IO.Path.Combine(currentDirectory, "libvlc\\" + (IntPtr.Size == 4 ? "win-x86" : "win-x64")));

                    this.sourceProvider = new VlcVideoSourceProvider(this.Dispatcher);
                    this.sourceProvider.CreatePlayer(libDirectory, "--file-logging", "-vvv", "--logfile=Logs.log");
                    var mediaOptions = new[]
                    {
                    " :network-caching=2000"
                    };

                    this.sourceProvider.MediaPlayer.Play(PlayStream, mediaOptions);
                    this.sourceProvider.MediaPlayer.Log += new EventHandler<VlcMediaPlayerLogEventArgs>(MediaPlayer_Log);
                    this.sourceProvider.MediaPlayer.Manager.SetFullScreen(this.sourceProvider.MediaPlayer.Manager.CreateMediaPlayer(), true);
                    this.sourceProvider.MediaPlayer.Audio.IsMute = true;    //这个版本中被静音
                                                                            //音量接口：this.sourceProvider.MediaPlayer.Audio.Volume，本版本暂时不用
                    if (configdata[comboBoxSource.SelectedIndex].Type == "本地")
                    {
                        //设置为主监视器
                        this.sourceProvider.MediaPlayer.EncounteredError += new EventHandler<VlcMediaPlayerEncounteredErrorEventArgs>(MediaPlayer_ErrorEncountered);
                    }
                    Binding bing = new Binding();
                    bing.Source = sourceProvider;
                    bing.Path = new PropertyPath("VideoSource");
                    //输出图片
                    string SourceName = null;
                    if (comboBoxSource.SelectedIndex != -1)
                    {
                        SourceName = configdata[comboBoxSource.SelectedIndex].SourceName.ToString();
                    }
                    else
                    {
                        SourceName = "自定义";
                    }
                    switch (selectedItem)
                    {
                        case 1: LabelLU.Content = "左上:" + SourceName; LiveLU.SetBinding(Image.SourceProperty, bing); break;
                        case 2: LabelRU.Content = "右上:" + SourceName; LiveRU.SetBinding(Image.SourceProperty, bing); break;
                        case 3: LabelLD.Content = "左下:" + SourceName; LiveLD.SetBinding(Image.SourceProperty, bing); break;
                        case 4: LabelRD.Content = "右下:" + SourceName; LiveRD.SetBinding(Image.SourceProperty, bing); break;
                    }


                }
                catch (Exception ex)
                {
                    SideBar();  //关闭
                    imageProtection.Visibility = Visibility.Visible;
                    labelProtection.Content = "保护：" + ex.Message;
                    labelProtection.Visibility = Visibility.Visible;
                    //并且考虑传递错误信息 ex
                }
            }

        }

        void MediaPlayer_Log(object sender, VlcMediaPlayerLogEventArgs e)
        {
            string message = "libVlc : " + e.Level + e.Message + e.Module;
            System.Diagnostics.Debug.WriteLine(message);
            //System.Diagnostics.Trace.WriteLine(message);
            //this.textBoxLog.AppendText(message); 失败了
        }

        void MediaPlayer_ErrorEncountered(object sender, VlcMediaPlayerEncounteredErrorEventArgs e)
        {
            if (dismiss == false)
            {
                if (dispatcherTimerMon.IsEnabled == false)
                {
                    dispatcherTimerMon.Start(); //信号灯
                }
            }
        }

        private void Expander1_Collapsed(object sender, RoutedEventArgs e)
        {
            if (textboxManStreaming.Text != "")
            {
                expander1.Header = "自定义播流代码";
                comboBoxSource.SelectedIndex = 0;
                comboBoxSource.SelectedIndex = -1;
            }
        }

        private void Expander1_Expanded(object sender, RoutedEventArgs e)
        {
            expander1.Header = "提交自定义播流代码";
        }

        private void ButtonSolo_Click(object sender, RoutedEventArgs e)
        {
            Storyboard Soloanim = this.FindResource("SoloAnimation") as Storyboard;

            int shrinkcol, shrinkrow;
            switch (selectedItem)
            {
                case 1: shrinkcol = 1; shrinkrow = 1; break;
                case 2: shrinkcol = 0; shrinkrow = 1; break;
                case 3: shrinkcol = 1; shrinkrow = 0; break;
                case 4: shrinkcol = 0; shrinkrow = 0; break;
                default: shrinkcol = 1; shrinkrow = 1; break;
            }

            int state;
            if (buttonLD.IsEnabled)
            {
                state = 0;
                SoloImg.Opacity = 1;
            }
            else
            {
                state = 1;
                SoloImg.Opacity = 0.5;
            }
            Soloanim.Children[0].SetValue(Storyboard.TargetNameProperty, "Col" + shrinkcol);
            (Soloanim.Children[0] as GridLengthAnimation).To = new GridLength(state, GridUnitType.Star);
            Soloanim.Children[1].SetValue(Storyboard.TargetNameProperty, "Row" + shrinkrow);
            (Soloanim.Children[1] as GridLengthAnimation).To = new GridLength(state, GridUnitType.Star);
            Soloanim.Begin();
            buttonLU.IsEnabled = !buttonLU.IsEnabled;
            buttonRU.IsEnabled = !buttonRU.IsEnabled;
            buttonLD.IsEnabled = !buttonLD.IsEnabled;
            buttonRD.IsEnabled = !buttonRD.IsEnabled;

        }

        //bool monred = false;
        bool dismiss = false;

        /// <summary>
        /// 监视灯闪
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dispatcherTimerMon_Tick(object sender, EventArgs e)
        {
            if (dismiss == false)
            {
                //labelMonitor.Foreground = Brushes.White;
                labelMonitor.Background = Brushes.Yellow;

                //monred = true;
                //变成了鬼畜灯
                //if (monred == true)
                //{
                //    labelMonitor.Foreground = Brushes.Black;
                //    labelMonitor.Background = Brushes.Transparent;

                //    monred = false;
                //}
                //else
                //{
                //    labelMonitor.Foreground = Brushes.White;
                //    labelMonitor.Background = Brushes.Red;

                //    monred = true;
                //}
            }
            else
            {
                labelMonitor.Foreground = Brushes.Black;
                labelMonitor.Background = Brushes.Transparent;

                //monred = false;
            }
        }

        private void ButtonDismiss_Click(object sender, RoutedEventArgs e)
        {
            //关闭警报
            if (dismiss == false)
            {
                dismiss = true;
                dispatcherTimerMon.Stop();
                labelMonitor.Foreground = Brushes.Black;
                labelMonitor.Background = Brushes.Transparent;
                DismissImg.Opacity = 1;
                //monred = false;
            }
            else
            {
                dismiss = false;
                DismissImg.Opacity = 0.5;
            }


        }



        #endregion

        #region 弹幕系统

        private Queue<DanmakuModel> _danmakuQueue = new Queue<DanmakuModel>();

        //private readonly StaticModel Static = new StaticModel();
        //private Thread releaseThread;
        DanmakuLoader danmu = new DanmakuLoader();

        int CurrentColorTime = 0;   //到三周期的最小公倍数时循环

        private void dispatcherTimerDanmaku_Tick(object sender, EventArgs e)
        {
            lock (_danmakuQueue)
            {
                var count = 0;
                if (_danmakuQueue.Any())
                {
                    count = (int)Math.Ceiling(_danmakuQueue.Count / 30.0);
                }

                for (var i = 0; i < count; i++)
                {
                    if (_danmakuQueue.Any())
                    {
                        var danmaku = _danmakuQueue.Dequeue();
                        if (danmaku.MsgType == MsgTypeEnum.Comment && enable_regex)
                        {
                            if (FilterRegex.IsMatch(danmaku.CommentText)) continue;

                        }

                        if (danmaku.MsgType == MsgTypeEnum.Comment && ignorespam_enabled)
                        {
                            try
                            {
                                var jobj = (JObject)danmaku.RawDataJToken;
                                if (jobj["info"][0][9].Value<int>() != 0)
                                {
                                    continue;
                                }
                            }
                            catch (Exception ex)
                            {

                            }

                        }
                        ProcDanmaku(danmaku);
                    }
                }
            }

            //    ListBoxItem newDanmakuL = new ListBoxItem();
            //    newDanmakuL.Content = danmakuModel.CommentText;
            //    listBoxDanmaku.Items.Add(newDanmakuL);

            //手动刷新弹幕池和Listbox
            listBoxDanmaku.Items.Clear();

            //Count会在循环之中改变的
            for (int i = 0; i < DanmakuPool.Count; ++i)
            {
                //依序放入 i = index
                //时间增加
                DanmakuPool.ElementAt(i).Timepass_increasement();
                DanmakuItem item = DanmakuPool.ElementAt(i);
                if (item.Timepass >= EXPIRE_TIME)
                {
                    if (item.IsSelected == true)
                    {
                        //TODO：发送弹幕 打到公屏上
                        //Debug.WriteLine(item.Danmaku);
                        Show_Danmaku(item);
                    }
                    DanmakuPool.Dequeue();  //时间不以人定
                    --i;                    //顺序发生改变
                }
                else
                {
                    //添加项目，还没到消失时间
                    ListBoxItem listBoxItem = new ListBoxItem();
                    listBoxItem.Content = item.Danmaku;
                    listBoxItem.Opacity = item.Opacity;
                    listBoxDanmaku.Items.Add(listBoxItem);
                    if (item.IsSelected == true)
                        listBoxDanmaku.SelectedItems.Add(listBoxDanmaku.Items[i]);
                }

            }


            //搞颜色

            //double cycle_r = 2 * Math.PI / 10;
            //double cycle_b = 2 * Math.PI / 15;
            //double cycle_g = 2 * Math.PI / 20;
            //int cycle = 120;
            //byte r, g, b;

            //switch (CA_State)
            //{
            //    case ColorAnimState.None:
            //        break;
            //    case ColorAnimState.BackOnly:
            //        CurrentColorTime = (CurrentColorTime > cycle ? 0 : CurrentColorTime + 1);
            //        r = (byte)((1 + Math.Sin((cycle_r * CurrentColorTime) + Math.Asin(BackColorPicker.SelectedColor.R))) / 2 * 256);
            //        g = (byte)((1 + Math.Sin((cycle_g * CurrentColorTime) + Math.Asin(BackColorPicker.SelectedColor.G))) / 2 * 256);
            //        b = (byte)((1 + Math.Sin((cycle_b * CurrentColorTime) + Math.Asin(BackColorPicker.SelectedColor.B))) / 2 * 256);
            //        BackColorPicker.SelectedColor = Color.FromArgb(255, r, g, b);
            //        break;
            //    case ColorAnimState.ForeOnly:

            //        break;
            //    case ColorAnimState.Both:
            //        //需要考虑反色
            //        break;
            //}

        }

        bool DanmakuSwitch = false;

        private async void ButtonDanmakuSwitch_Click(object sender, RoutedEventArgs e)
        {
            var myblue = new SolidColorBrush(Color.FromArgb(255, 1, 188, 225));
            if (DanmakuSwitch)
            {

                danmu.Disconnect();

                //队列不再deQueue, ProcDanmaku 不会再被调用。
                dispatcherTimerDanmaku.Stop();
                //清除弹幕池
                _danmakuQueue.Clear();
                DanmakuPool.Clear();
                //显性清除
                listBoxDanmaku.Items.Clear();

                //TODO:淡出
                Close_DanmakuWindow();

                buttonDanmakuSwitch.Foreground = myblue;
                buttonDanmakuSwitch.Background = Brushes.White;
                buttonDanmakuSwitch.Content = "启动弹幕";

                DanmakuSwitch = false;

            }
            else
            {
                ////只有第一次启动时进行真正链接
                //if (!connected)
                //{
                buttonDanmakuSwitch.Content = "连接中";
                int room_id = 0;
                foreach (ConfigItem cit in configdata)
                {
                    if (cit.Type == "B站" && cit.Bililive_roomid > 0)    //有效的B站房间号
                    {
                        room_id = cit.Bililive_roomid;
                        break;
                    }
                }
                if (room_id == 0)
                {
                    buttonDanmakuSwitch.Content = "连接失败";
                }
                else
                {
                    var connectresult = await danmu.ConnectAsync(room_id);
                    var trytime = 0;
                    if (!connectresult && danmu.Error != null)// 如果连接不成功并且出错了
                    {
                        buttonDanmakuSwitch.Content = "连接失败";
                    }

                    while (!connectresult && sender == null)
                    {
                        if (trytime > 3)
                            break;
                        else
                            trytime++;

                        await System.Threading.Tasks.Task.Delay(1000); // 稍等一下
                        connectresult = await danmu.ConnectAsync(room_id);
                    }

                    if (connectresult)
                    {

                        listBoxDanmaku.Items.Clear();

                        buttonDanmakuSwitch.Foreground = Brushes.White;
                        buttonDanmakuSwitch.Background = myblue;
                        buttonDanmakuSwitch.Content = "关闭弹幕";

                        DanmakuSwitch = true;

                        dispatcherTimerDanmaku.Start();

                        //TODO：淡入
                        Open_DanmakuWindow();

                    }
                    else
                    {
                        buttonDanmakuSwitch.Content = "连接失败";
                    }
                }

                if (buttonDanmakuSwitch.Content.ToString() == "连接失败")
                {
                    await System.Threading.Tasks.Task.Delay(1000);
                    buttonDanmakuSwitch.Content = "启动弹幕";
                }
                //}

            }

        }

        static bool AutoDanmaku = true;
        private void ButtonAutoDanmaku_Click(object sender, RoutedEventArgs e)
        {
            var myblue = new SolidColorBrush(Color.FromArgb(255, 1, 188, 225));
            if (!AutoDanmaku)
            {
                buttonAutoDanmaku.Foreground = Brushes.White;
                buttonAutoDanmaku.Background = myblue;
                buttonAutoDanmaku.Content = "自动";
            }
            else
            {
                buttonAutoDanmaku.Foreground = myblue;
                buttonAutoDanmaku.Background = Brushes.White;
                buttonAutoDanmaku.Content = "手动";
            }
            AutoDanmaku = !AutoDanmaku;
        }

        private void b_ReceivedDanmaku(object sender, ReceivedDanmakuArgs e)
        {
            //if (!DanmakuSwitch)
            //{   //关闭后不再enQueue,现在改为断连
            lock (_danmakuQueue)
            {
                var danmakuModel = e.Danmaku;
                _danmakuQueue.Enqueue(danmakuModel);
            }
            //}
        }

        /// <summary>
        /// 向弹幕池添加弹幕 这里弹幕池将是显性的
        /// </summary>
        /// <param name="danmakuModel"></param>
        private void ProcDanmaku(DanmakuModel danmakuModel)
        {
            if (danmakuModel.MsgType == MsgTypeEnum.Comment)
            {
                //    ListBoxItem newDanmakuL = new ListBoxItem();
                //    newDanmakuL.Content = danmakuModel.CommentText;
                //    listBoxDanmaku.Items.Add(newDanmakuL);
                DanmakuPool.Enqueue(new DanmakuItem(danmakuModel.CommentText, danmakuModel.isAdmin));
            }
        }

        private void ListBoxDanmaku_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            for (int i = 0; i < listBoxDanmaku.Items.Count; ++i)
                DanmakuPool.ElementAt(i).IsSelected = listBoxDanmaku.SelectedItems.Contains(listBoxDanmaku.Items[i]);

        }

        private void TextBoxStoreSec_TextChanged(object sender, TextChangedEventArgs e)
        {
            //强制数字输入，且为大于2
            //int Storesec = EXPIRE_TIME / 2;
            int Storesec;
            TextBox tempbox = sender as TextBox;
            TextChange[] change = new TextChange[e.Changes.Count];
            e.Changes.CopyTo(change, 0);
            int offset = change[0].Offset;
            if (change[0].AddedLength > 0)
            {

                if (tempbox.Text.IndexOf(' ') != -1 || !int.TryParse(tempbox.Text, out Storesec) || Storesec < 2)
                {
                    tempbox.Text = tempbox.Text.Remove(offset, change[0].AddedLength);
                    tempbox.Select(offset, 0);
                }
                else
                {
                    EXPIRE_TIME = Storesec * 2;

                    if (SliderStoreSec != null)
                    {
                        if(Storesec > (int)SliderStoreSec.Maximum){
                            SliderStoreSec.Value = SliderStoreSec.Maximum;
                        }
                        else
                        {
                            SliderStoreSec.Value = (double)Storesec;
                        }
                    }

                }

            }


        }

        public static bool d_ok;

        private void ButtonDanmakuEntry_Click(object sender, RoutedEventArgs e)
        {
            d_ok = false;
            var danmuEntry = new DanmakuEntry();
            //danmuEntry.Left = PrimaryScreen.WorkingArea.Width / 2;
            //danmuEntry.Top = PrimaryScreen.WorkingArea.Height / 2;
            danmuEntry.ShowDialog();
            if (d_ok == true)
            {
                SideBar();
                Hide_Monitor();
            }

        }

        private void Hide_Monitor()
        {
            label7.Visibility = Visibility.Collapsed;
            ViewboxMonitor.Visibility = Visibility.Collapsed;
            MonitorBtn.Visibility = Visibility.Collapsed;
        }

        private void Show_Monitor()
        {
            label7.Visibility = Visibility.Visible;
            ViewboxMonitor.Visibility = Visibility.Visible;
            MonitorBtn.Visibility = Visibility.Visible;
        }

        public static int Add_DanmakuConfig(string website = "")
        {
            var danmakuitem = new ConfigItem(configcount, 0, "弹幕" + configcount, "", website);
            int r_i = danmakuitem.Bililive_roomid;
            if (r_i > 0)
            {
                configdata.Add(danmakuitem);
                ++configcount;
                //流名称文本框不想改了
                return 0;
            }
            else
            {
                return r_i;
            }
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            //listbox的最大高度限制
            listBoxDanmaku.MaxHeight = this.Height - 50;
        }

        //TODO: 以后写成一个list用于调用样式列表。

        FocalDepthHover focaldephov = new FocalDepthHover();

        private void Open_DanmakuWindow()
        {
            this.Topmost = true;
            focaldephov.Show();
            //focaldephov.Opacity = 1;
            //OpacSlider.Value = 1;
            FadeOutAnim(buttonWinTrans, OpacSlider);
        }

        private void Close_DanmakuWindow()
        {
            //TODO:淡出计时
            //focaldephov.Opacity = 0;
            //OpacSlider.Value = 0;
            FadeOutAnim(buttonWinTrans, OpacSlider);
            this.Topmost = false;
        }

        public static event ReceivedDanmuEvt ReceivedDanmu;

        /// <summary>
        /// 打出弹幕到公屏上，主要接口
        /// 每一种样式只有一个窗口，由弹幕开启事件加载
        /// </summary>
        /// <param name="SendDanmu"></param>
        private void Show_Danmaku(DanmakuItem SendDanmu)
        {
            //TODO: 会向所有的样式发送（设想 暂不实现）, 或者使用 Page 实现切换
            //FocalDepthHover.ReceiveDanmaku(SendDanmu);
            if (ReceivedDanmu != null)
                ReceivedDanmu(this, new ReceivedDanmuArgs() { Danmu = SendDanmu });
        }

        #endregion

        private void Window_Closed(object sender, EventArgs e)
        {
            Properties.Settings.Default.StoreTime = EXPIRE_TIME;
            Properties.Settings.Default.Save();
            Application.Current.Shutdown();
        }

        //AnimMsg windowAnim; //全局的第二窗口
        private void ButtonTest_Click(object sender, RoutedEventArgs e)
        {
            Open_DanmakuWindow();
            //windowAnim = new AnimMsg();
            //if (AllScreens.Length > 1)
            //{//第二屏幕
            //    windowAnim.Left = PrimaryScreen.WorkingArea.Width;
            //    windowAnim.Top = 0;
            //    windowAnim.WindowState = WindowState.Maximized;
            //}
            //else
            //{
            //    windowAnim.WindowState = WindowState.Maximized;
            //}
            //windowAnim.Show();
        }

        #region 景深悬浮设置

        private void BackColorPicker_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color> e)
        {
            if (focaldephov.IsLoaded)
                focaldephov.Background = new SolidColorBrush(BackColorPicker.SelectedColor);
        }

        private void OpacSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (focaldephov.IsLoaded)
                focaldephov.Opacity = OpacSlider.Value;
        }

        private void ForeColorPicker_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color> e)
        {
            (focaldephov.FindResource("BubbleFore") as SolidColorBrush).Color = ForeColorPicker.SelectedColor;
            //FocalDepthHover.ForeColor = ForeColorPicker.SelectedColor;
        }

        Binding bing_sub;
        private void BackNone_Selected(object sender, RoutedEventArgs e)
        {
            bing_sub = new Binding();
            if (focaldephov.IsLoaded)
                focaldephov.BackImg.SetBinding(Image.SourceProperty, bing_sub);
        }

        private VlcVideoSourceProvider sourceProvider_local;
        private void BackLive_Selected(object sender, RoutedEventArgs e)
        {
            string PlayStream = configdata[0].LiveViewingSite.ToString();
            var currentAssembly = Assembly.GetEntryAssembly();
            var currentDirectory = new FileInfo(currentAssembly.Location).DirectoryName;
            // Default installation path of VideoLAN.LibVLC.Windows
            var libDirectory = new DirectoryInfo(System.IO.Path.Combine(currentDirectory, "libvlc\\" + (IntPtr.Size == 4 ? "win-x86" : "win-x64")));

            this.sourceProvider_local = new VlcVideoSourceProvider(this.Dispatcher);
            this.sourceProvider_local.CreatePlayer(libDirectory, "--file-logging", "-vvv", "--logfile=Logs.log");
            var mediaOptions = new[]
            {
                    " :network-caching=2000"
            };

            this.sourceProvider_local.MediaPlayer.Play(PlayStream, mediaOptions);
            this.sourceProvider_local.MediaPlayer.Log += new EventHandler<VlcMediaPlayerLogEventArgs>(MediaPlayer_Log);
            this.sourceProvider_local.MediaPlayer.Manager.SetFullScreen(this.sourceProvider_local.MediaPlayer.Manager.CreateMediaPlayer(), true);
            this.sourceProvider_local.MediaPlayer.Audio.IsMute = true;    //这个版本中被静音
                                                                          //音量接口：this.sourceProvider_local.MediaPlayer.Audio.Volume，本版本暂时不用
            this.sourceProvider_local.MediaPlayer.EncounteredError += new EventHandler<VlcMediaPlayerEncounteredErrorEventArgs>(MediaPlayer_ErrorEncountered);

            bing_sub = new Binding();
            bing_sub.Source = sourceProvider_local;
            bing_sub.Path = new PropertyPath("VideoSource");
            //输出图片
            focaldephov.BackImg.SetBinding(Image.SourceProperty, bing_sub);
        }

        private void BackLive_Unselected(object sender, RoutedEventArgs e)
        {
            sourceProvider_local.Dispose();
        }

        private void BackPic_Selected(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog();
            openFileDialog.Title = "选择背景图像";
            openFileDialog.Filter = "png|*.png|jpg|*.jpg|jpeg|*.jpeg";
            openFileDialog.FileName = string.Empty;
            openFileDialog.FilterIndex = 1;
            openFileDialog.RestoreDirectory = true;
            openFileDialog.DefaultExt = "png";
            System.Windows.Forms.DialogResult result = openFileDialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.Cancel)
            {   //这BUG无伤大雅 不修了
                ComboBoxBackImg.SelectedIndex = 0;
                //ComboBoxBackImg.SelectedValue = BackNone;
            }
            else
            {
                focaldephov.BackImg.Source = new ImageSourceConverter().ConvertFromString(openFileDialog.FileName) as ImageSource;
            }
        }

        private void OpacSliderFore_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (focaldephov.IsLoaded)
                focaldephov.GridCanvas.Opacity = OpacSliderFore.Value;
        }

        Binding bing_fore;

        private void ForeNone_Selected(object sender, RoutedEventArgs e)
        {
            bing_fore = new Binding();
            if (focaldephov.IsLoaded)
                focaldephov.ForeImg.SetBinding(Image.SourceProperty, bing_fore);
        }

        private void ForePic_Selected(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog();
            openFileDialog.Title = "选择前景图像";
            openFileDialog.Filter = "png|*.png|jpg|*.jpg|jpeg|*.jpeg";
            openFileDialog.FileName = string.Empty;
            openFileDialog.FilterIndex = 1;
            openFileDialog.RestoreDirectory = true;
            openFileDialog.DefaultExt = "png";
            System.Windows.Forms.DialogResult result = openFileDialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.Cancel)
            {   //这BUG无伤大雅 不修了
                ComboBoxForeImg.SelectedIndex = 0;
            }
            else
            {
                focaldephov.ForeImg.Source = new ImageSourceConverter().ConvertFromString(openFileDialog.FileName) as ImageSource;
            }
        }


        #endregion

        Storyboard PushOut_sb = new Storyboard();
        
        private void ButtonStoreSec_Click(object sender, RoutedEventArgs e)
        {
            DoubleAnimation PushOut = new DoubleAnimation();
            PushOut.From = SliderStoreSec.Value;
            PushOut.To = SliderStoreSec.Minimum;
            PushOut.Duration = TimeSpan.FromSeconds(FocalDepthHover.HOVER_TIME);
            //Storyboard.SetTarget(PushOut, SliderStoreSec);
            PushOut.EasingFunction = new ExponentialEase()
            {
                EasingMode = EasingMode.EaseOut
            };
            Storyboard.SetTargetProperty(PushOut, new PropertyPath("(Slider.Value)"));

            PushOut_sb.Children.Add(PushOut);
            PushOut_sb.Completed += PushOut_Storyboard_Remove;
            PushOut_sb.Begin(SliderStoreSec,HandoffBehavior.SnapshotAndReplace, true);

            buttonStoreSec.IsEnabled = false;
            //SliderStoreSec.BeginAnimation(Slider.ValueProperty, PushOut);
            
        }

        private void SliderStoreSec_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            
            if (textBoxStoreSec.Text != ((int)SliderStoreSec.Value).ToString())
            {   //防止套娃
                if(SliderStoreSec.Value< SliderStoreSec.Maximum)
                    textBoxStoreSec.Text = ((int)SliderStoreSec.Value).ToString();
            }
            
        }

        private void PushOut_Storyboard_Remove(object sender, EventArgs e)
        {
            PushOut_sb.Remove(SliderStoreSec);
            buttonStoreSec.IsEnabled = true;
        }

        SolidColorBrush blueback = new SolidColorBrush(Color.FromArgb(255, 109, 194, 249));
        SolidColorBrush bluefore = new SolidColorBrush(Color.FromArgb(255, 254, 253, 250));
        //SolidColorBrush redback = new SolidColorBrush(Color.FromArgb(255, 229, 88, 18));
        //SolidColorBrush redfore = new SolidColorBrush(Color.FromArgb(255, 239, 231, 218));
        Storyboard FullFadeOut_sb = new Storyboard();

        private void ButtonWinTrans_Click(object sender, RoutedEventArgs e)
        {
            FadeOutAnim(buttonWinTrans, OpacSlider);
        }

        private void ButtonDanmuTrans_Click(object sender, RoutedEventArgs e)
        {
            FadeOutAnim(buttonDanmuTrans, OpacSliderFore);
        }

        private void OpacFadeOut_Storyboard_Remove(object sender, EventArgs e)
        {
            Fade_Storyboard_Remove(buttonWinTrans, OpacSlider);
        }

        private void DanmuFadeOut_Storyboard_Remove(object sender, EventArgs e)
        {
            Fade_Storyboard_Remove(buttonDanmuTrans, OpacSliderFore);
        }

        private void FadeOutAnim(object senderbutton, object senderslider)
        {
            //FullFadeOut_sb = new Storyboard();

            var sendb = senderbutton as Button;
            var sends = senderslider as Slider;
            DoubleAnimation FullFadeOut = new DoubleAnimation();

            if (sends.Value > sends.Minimum)
            {
                FullFadeOut.From = sends.Value;
                FullFadeOut.To = sends.Minimum;
                FullFadeOut.EasingFunction = new BackEase()
                {
                    Amplitude = 0.3,
                    EasingMode = EasingMode.EaseOut,
                };
            }
            else
            {
                FullFadeOut.From = sends.Value;
                FullFadeOut.To = sends.Maximum;
                FullFadeOut.EasingFunction = new BackEase()
                {
                    Amplitude = 0.3,
                    EasingMode = EasingMode.EaseIn,
                };
            }

            FullFadeOut.Duration = TimeSpan.FromSeconds(1);//或许考虑一个其他量

            //Storyboard.SetTarget(FullFadeOut, SliderStoreSec);
            Storyboard.SetTargetProperty(FullFadeOut, new PropertyPath("(Slider.Value)"));

            FullFadeOut_sb.Children.Add(FullFadeOut);
            if (sends == OpacSlider)
                FullFadeOut_sb.Completed += OpacFadeOut_Storyboard_Remove;
            else
                FullFadeOut_sb.Completed += DanmuFadeOut_Storyboard_Remove;

            FullFadeOut_sb.Begin(sends, HandoffBehavior.SnapshotAndReplace, true);

            sendb.IsEnabled = false;
        }

        private void Fade_Storyboard_Remove(object senderbutton, object senderslider)
        {
            var sendb = senderbutton as Button;
            var sends = senderslider as ColorPicker.ColorSlider;

            var opacs_temp = sends.Value;
            FullFadeOut_sb.Remove(sends);
            sends.Value = opacs_temp;
            sendb.IsEnabled = true;

            //动画结束后对按钮态转换
            if (opacs_temp == sends.Minimum)
            {
                sendb.Background = bluefore;
                sendb.Foreground = blueback;
            }
            else
            {
                sendb.Background = blueback;
                sendb.Foreground = bluefore;
            }
        }

        //enum ColorAnimState { None, BackOnly, ForeOnly, Both };
        //ColorAnimState CA_State = ColorAnimState.None;

        private void ButtonBackColorPicker_Click(object sender, RoutedEventArgs e)
        {
            //可以改
            //BackColorPicker.SelectedColor = Color.FromArgb(255, 255, 255, 255);
            //CA_State = ColorAnimState.BackOnly;
            //没想好
        }



        private void ButtonBackImg_Click(object sender, RoutedEventArgs e)
        {
            ImgFadeOutAnim(buttonBackImg, focaldephov.BackImg);
        }

        private void ButtonForeImg_Click(object sender, RoutedEventArgs e)
        {
            ImgFadeOutAnim(buttonForeImg, focaldephov.ForeImg);
        }

        private void BackImg_Storyboard_Remove(object sender, EventArgs e)
        {
            Img_Storyboard_Remove(buttonBackImg, focaldephov.BackImg);
        }

        private void ForeImg_Storyboard_Remove(object sender, EventArgs e)
        {
            Img_Storyboard_Remove(buttonForeImg, focaldephov.ForeImg);
        }

        Storyboard Img_sb = new Storyboard();
        private void ImgFadeOutAnim(object senderbutton, object senderImg)
        {
            //FullFadeOut_sb = new Storyboard();

            var sendb = senderbutton as Button;
            var sends = senderImg as Image;
            DoubleAnimation FullFadeOut = new DoubleAnimation();

            if (sends.Opacity > 0)
            {
                FullFadeOut.From = sends.Opacity;
                FullFadeOut.To = 0;
                FullFadeOut.EasingFunction = new BackEase()
                {
                    Amplitude = 0.3,
                    EasingMode = EasingMode.EaseOut,
                };
            }
            else
            {
                FullFadeOut.From = sends.Opacity;
                FullFadeOut.To = 1;
                FullFadeOut.EasingFunction = new BackEase()
                {
                    Amplitude = 0.3,
                    EasingMode = EasingMode.EaseIn,
                };
            }

            FullFadeOut.Duration = TimeSpan.FromSeconds(1);//或许考虑一个其他量

            //Storyboard.SetTarget(FullFadeOut, SliderStoreSec);
            Storyboard.SetTargetProperty(FullFadeOut, new PropertyPath("(Image.Opacity)"));

            Img_sb.Children.Add(FullFadeOut);
            if (sends == focaldephov.BackImg)
                Img_sb.Completed += BackImg_Storyboard_Remove;
            else
                Img_sb.Completed += ForeImg_Storyboard_Remove;

            Img_sb.Begin(sends, HandoffBehavior.SnapshotAndReplace, true);

            sendb.IsEnabled = false;
        }

        private void Img_Storyboard_Remove(object senderbutton, object senderImg)
        {
            var sendb = senderbutton as Button;
            var sends = senderImg as Image;

            var opacs_temp = sends.Opacity;
            Img_sb.Remove(sends);
            sends.Opacity = opacs_temp;
            sendb.IsEnabled = true;

            //动画结束后对按钮态转换
            if (opacs_temp == 0)
            {
                sendb.Background = bluefore;
                sendb.Foreground = blueback;
            }
            else
            {
                sendb.Background = blueback;
                sendb.Foreground = bluefore;
            }
        }

        private void DanmuPlain_Selected(object sender, RoutedEventArgs e)
        {
            FocalDepthHover.DM_Style = FocalDepthHover.DanmuStyle.Plain;
        }

        private void DanmuBubble_Selected(object sender, RoutedEventArgs e)
        {
            FocalDepthHover.DM_Style = FocalDepthHover.DanmuStyle.Bubble;
        }

        private void DanmuBubbleFloat_Selected(object sender, RoutedEventArgs e)
        {
            FocalDepthHover.DM_Style = FocalDepthHover.DanmuStyle.BubbleFloat;
        }

        private void DanmuBubbleCorner_Selected(object sender, RoutedEventArgs e)
        {
            FocalDepthHover.DM_Style = FocalDepthHover.DanmuStyle.BubbleCorner;
        }
    }
}