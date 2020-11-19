// Art Center Live Console
// Copyright (c) 2019 - 2020 by Art Center, All Rights Reserved.
// GPL-3.0

//========= Platform =============

// C# WPF(Windows Presentation Foundation) 
// .NET Framework 4.5

//========= Update Log ===========

// For more information, please visit:
// https://github.com/LogCreative/ACLiveConsole.

// Ver 6.5.0.0 by Li Zilong
// After VOS, Final Update.

// Ver 3.9.1.0 by Li Zilong
// LAN

// Ver 3.5.0.0 by Li Zilong
// DBS

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
using System.Threading.Tasks;
using System.Net;
using System.Windows.Media.Animation;
using System.Collections.ObjectModel;
using System.Reflection;
using Vlc.DotNet.Core;
using Vlc.DotNet.Wpf;
using BiliDMLib;
using BilibiliDM_PluginFramework;
using Newtonsoft.Json.Linq;
using AForge.Video.DirectShow;
using FFmpegDemo;
using System.Windows.Interop;
using Vlc.DotNet.Core.Interops;
// Begin "Step 4: Basic RadialController menu customization"
// Using directives for RadialController functionality.
using Windows.UI.Input;
using Windows.Storage.Streams;
using System.Collections.Specialized;
using System.Collections;
using Windows.ApplicationModel.Activation;
using System.Security.Cryptography;
using BitConverter;
// End "Step 4: Basic RadialController menu customization"
using JiebaNet.Segmenter;
using WordCloudSharp;
using System.Runtime.InteropServices;
using ColorPicker;
using System.Media;

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

    public delegate void VolChangedEvt(object sender, VolChangedArgs e);

    public class VolChangedArgs
    {

    }

    public delegate void MainSelecChangedEvt(object sender, MainSelecChangedArgs e);

    public class MainSelecChangedArgs
    {

    }

    public delegate void GiftingReceivedEvt(object sender, GiftingReceivedArgs e);

    public class GiftingReceivedArgs
    {
        public DanmakuModel danmu;
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
        DispatcherTimer dispatcherTimerSysTime = new DispatcherTimer();
        DispatcherTimer dispatcherTimerWC = new DispatcherTimer();

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


        DirectoryInfo libDirectory;

        public delegate void MainSelecChangedEvt(object sender, MainSelecChangedArgs e);
        public static event MainSelecChangedEvt MSC;

        #endregion

        #region Surface Dial

        private RadialController radialController;

        bool radialControllerInit = false;

        // Create and configure our radial controller.
        private void InitializeController()
        {
            // Create a reference to the RadialController.
            CreateController();
            // Set rotation resolution to 5 degree of sensitivity.
            radialController.RotationResolutionInDegrees = 5;

            radialController.RotationChanged += RadialController_RotationChanged;
            radialController.ButtonClicked += RadialController_ButtonClicked;

            AddCustomItems();

            radialControllerInit = true;
        }

        // Occurs when the wheel device is rotated while a custom 
        // RadialController tool is active.
        // NOTE: Your app does not receive this event when the RadialController 
        // menu is active or a built-in tool is active
        // Send rotation input to slider of active region.
        private void RadialController_RotationChanged(RadialController sender,
          RadialControllerRotationChangedEventArgs args)
        {
            //System.Diagnostics.Debug.WriteLine("Rotated");
            if (radialController.Menu.GetSelectedMenuItem() == rcsub)
            {
                if (args.RotationDeltaInDegrees < 0) PrevSub();
                else NextSub();
            }
            else if (radialController.Menu.GetSelectedMenuItem() == rcplayer)
            {
                if (args.RotationDeltaInDegrees > 0) ForeNextFile();
                else ForeNextFile(false);
            }
                
            InvalidateVisual();
        }

        // Occurs when the wheel device is pressed and then released 
        // while a customRadialController tool is active.
        // NOTE: Your app does not receive this event when the RadialController 
        // menu is active or a built-in tool is active
        // Send click input to toggle button of active region.
        private void RadialController_ButtonClicked(RadialController sender,
          RadialControllerButtonClickedEventArgs args)
        {
            //System.Diagnostics.Debug.WriteLine("Clicked");
            if (radialController.Menu.GetSelectedMenuItem() == rcsub)
            {
                ClearHistorySub();
            }
            else if (radialController.Menu.GetSelectedMenuItem() == rcplayer)
            {
                if (buttonPlay.IsEnabled)
                    ForePlay();
                else ForeStop();
            } else if (radialController.Menu.GetSelectedMenuItem() == winshow)
            {
                FadeOutAnim(buttonWinTrans, OpacSlider);        //更改窗口不透明度
                focaldephov.Topmost = true;                     //将窗口置顶
            }

            InvalidateVisual();
        }

        [System.Runtime.InteropServices.Guid("1B0535C9-57AD-45C1-9D79-AD5C34360513")]
        [System.Runtime.InteropServices.InterfaceType(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIInspectable)]
        interface IRadialControllerInterop
        {
            RadialController CreateForWindow(
            IntPtr hwnd,
            [System.Runtime.InteropServices.In] ref Guid riid);
        }

        [System.Runtime.InteropServices.Guid("787cdaac-3186-476d-87e4-b9374a7b9970")]
        [System.Runtime.InteropServices.InterfaceType(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIInspectable)]
        interface IRadialControllerConfigurationInterop
        {
            RadialControllerConfiguration GetForWindow(
            IntPtr hwnd,
            [System.Runtime.InteropServices.In] ref Guid riid);
        }

        private void CreateController()
        {
            IRadialControllerInterop interop = (IRadialControllerInterop)System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.GetActivationFactory(typeof(RadialController));
            Guid guid = typeof(RadialController).GetInterface("IRadialController").GUID;

            radialController = interop.CreateForWindow(new WindowInteropHelper(this).Handle, ref guid);
        }

        RadialControllerMenuItem rcsub;
        RadialControllerMenuItem rcplayer;
        RadialControllerMenuItem winshow;

        private void AddCustomItems()
        {
            rcsub = RadialControllerMenuItem.CreateFromFontGlyph("字幕机", "⌨", "Segoe UI Emoji");
            radialController.Menu.Items.Add(rcsub);
            rcplayer = RadialControllerMenuItem.CreateFromFontGlyph("音频电脑", "💻", "Segoe UI Emoji");
            radialController.Menu.Items.Add(rcplayer);
            winshow = RadialControllerMenuItem.CreateFromFontGlyph("窗口显示", "🖥", "Segoe UI Emoji");
            radialController.Menu.Items.Add(winshow);

        }

        #endregion

        #region 配置项类
        public event PropertyChangedEventHandler PropertyChanged;
        public static ObservableCollection<ConfigItem> configdata;//定义配置数据库
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
            private string[] devOptions;
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

            public string[] DevOptions
            {
                get { return devOptions; }
                set { 
                    devOptions = value;
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
                    case 2: this.Type = "自定义"; break;
                    case 3: this.Type = "局域网"; break;
                    case 4: this.Type = "捕获设备"; break;
                    case 5: this.Type = "腾讯云";break;
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
                        //await BililiveAPI.GetPlayUrlAsync(bililive_roomid);

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
                                        //re = new Regex(@"\.flv");
                                        //string result = re.Replace(api_url, ".m3u8");

                                        re = new Regex(@"(?<=live_)\w+");
                                        matches = re.Matches(api_url);
                                        api_url = matches[0].Groups[0].ToString();
                                        string result = "https://cn-jsnt-dx-live-01.bilivideo.com/live-bvc/live_" + api_url + ".m3u8";

                                        System.Diagnostics.Debug.WriteLine(result);
                                        return result;
                                        //https://cn-hbxy-cmcc-live-01.live-play.acgvideo.com/live-bvc/live_

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
                else if (Type =="腾讯云")
                {
                    if (Properties.Settings.Default.CloseYouget == false)
                    {

                        //Regex re = new Regex(@"(?<=rtmp?://live\.bilibili\.com/)\b\w+\b"); //正则表达式
                        try
                        {
                            Regex re = new Regex(@"rtmp");
                            realplay = re.Replace(website, "https");

                            try
                            {
                                re = new Regex(@"livepush");
                                realplay = re.Replace(realplay, "liveplay");

                                try
                                {
                                    realplay = realplay.Split('?')[0];
                                    realplay = realplay + ".flv";
                                }
                                catch
                                {
                                    return "腾讯云地址复制不够完全(-3)";
                                }
                            }
                            catch
                            {
                                return "不是正确的腾讯云推流地址(-2)";
                            }
                        }
                        catch
                        {
                            return "不是正确的rtmp地址(-1)";
                        }
                    }
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
            private string userName;
            private int uid;

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

            public string UserName
            {
                get { return userName; }
                set { userName = value; }
            }

            public int UID
            {
                get { return uid; }
                set { uid = value; }
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
            public DanmakuItem(int userid, string DanmakuIn = "", bool isAdm = false, string un = "")
            {
                Danmaku = DanmakuIn;
                Timepass = 0;
                IsSelected = AutoDanmaku ? true : false;
                IsAdmin = isAdm;
                UserName = un;
                UID = userid;
            }

        }

        #endregion

        SoundControl sc;

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

            // 1000 / 30 = 33.33 ms
            dispatcherTimerSysTime.Tick += DispatcherTimerSysTime_Tick;
            dispatcherTimerSysTime.Interval = new TimeSpan(0, 0, 0, 0, 15);

            labelVer.Content = "版本: " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString() + "\n";
            labelVer.Content += "© Art Center 2019 - 2020, All Rights Reserved." + "\n";
            labelVer.Content += "Based on Open Source Projects: Nginx, ffmpeg, VLC, bililive-dm";

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

            if (Properties.Settings.Default.LowMoni.Equals(true))
            {
                checkBoxLowMoni.IsChecked = true;
                Rec2.Visibility = Visibility.Visible;
            }
            else
            {
                checkBoxLowMoni.IsChecked = false;
                Rec2.Visibility = Visibility.Collapsed;
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

            //danmu.ReceivedDanmaku += b_ReceivedDanmaku;
            //b.ReceivedRoomCount += b_ReceivedRoomCount;

            //Hide_Monitor();
            listBoxDanmaku.MaxHeight = this.Height - 50;

            button7.Visibility = Visibility.Hidden;

            DanmakuContentExp.IsExpanded = false;
            DanmuOverallExp.IsExpanded = false;
            DanmuStyleExp.IsExpanded = false;
            DanmuSetting.IsEnabled = false;

            foreach (FontFamily font in Fonts.SystemFontFamilies)
            {
                ComboBoxItem FontItem = new ComboBoxItem();
                FontItem.Content = font.Source;
                FontItem.FontFamily = font;
                ComboBoxFont.Items.Add(FontItem);
            }

            LabelDanmu.Visibility = Visibility.Hidden;

            textBoxAdd.Visibility = Visibility.Hidden;
            labelAdd.Visibility = Visibility.Hidden;
            textBoxCode.Visibility = Visibility.Hidden;
            labelCode.Visibility = Visibility.Hidden;
            textBoxMan.Visibility = Visibility.Hidden;
            labelMan.Visibility = Visibility.Hidden;
            labelWebsite.Visibility = Visibility.Hidden;
            textBoxWebsite.Visibility = Visibility.Hidden;
            buttonlWHelp.Visibility = Visibility.Hidden;
            labelSourceName.Visibility = Visibility.Hidden;
            textBoxSourceName.Visibility = Visibility.Hidden;
            buttonPlus.Visibility = Visibility.Hidden;

            GridLANTip.Visibility = Visibility.Hidden;

            buttonExtPlayer.IsEnabled = false;

            //监视器初始化
            var currentAssembly = Assembly.GetEntryAssembly();
            var currentDirectory = new FileInfo(currentAssembly.Location).DirectoryName;
            // Default installation path of VideoLAN.LibVLC.Windows
            libDirectory = new DirectoryInfo(System.IO.Path.Combine(currentDirectory, "libvlc\\" + (IntPtr.Size == 4 ? "win-x86" : "win-x64")));

            Monitors = new List<Monitor>();
            for (int i = 0; i < 3; ++i)
            {
                Monitor monitor = new Monitor();

                monitor.SourceProvider = new VlcVideoSourceProvider(this.Dispatcher);
                //monitor.SourceProvider.IsAlphaChannelEnabled = true;  //开alpha通道
                monitor.SourceProvider.CreatePlayer(libDirectory, "--file-logging", "-vvv", "--logfile=Logs.log");
                monitor.SourceProvider.MediaPlayer.Log += new EventHandler<VlcMediaPlayerLogEventArgs>(MediaPlayer_Log);
                monitor.SourceProvider.MediaPlayer.Manager.SetFullScreen(monitor.SourceProvider.MediaPlayer.Manager.CreateMediaPlayer(), true);
                monitor.Volume = 0;
                monitor.SourceProvider.MediaPlayer.EncounteredError += new EventHandler<VlcMediaPlayerEncounteredErrorEventArgs>(MediaPlayer_ErrorEncountered);

                //if (checkBoxLowMoni.IsChecked.Equals(true))
                //{
                //    //不论如何先初始化
                //    monitor.TstRtmp = new tstRtmp();
                //}

                Monitors.Add(monitor);
            }

            SliderTransSec.Value = Properties.Settings.Default.TranSec;

            checkBoxNetwork.IsChecked = Properties.Settings.Default.OpenNetworkCaching;
            Rec4.Visibility = Visibility.Collapsed;
            checkBoxSystemTime.IsChecked = Properties.Settings.Default.SysTime;

            SoundControl.VCE += SoundControl_VCE;

            checkBoxDanmuLink.IsChecked = Properties.Settings.Default.danmuLink;
            Rec2.Visibility = Visibility.Collapsed;

            SliderGiftNum.Value = (double)Properties.Settings.Default.GiftGivingNum;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //加载弹幕设置
            textBoxStoreSec.Text = (Properties.Settings.Default.StoreTime * 0.5).ToString();
            SliderStoreSec.Value = (double)(Properties.Settings.Default.StoreTime * 0.5);
            textBoxRegex.Text = Properties.Settings.Default.Regex;
            BackColorPicker.SelectedColor = Properties.Settings.Default.WinBack;
            focaldephov.WinBack.Color = BackColorPicker.SelectedColor;
            ForeColorPicker.SelectedColor = Properties.Settings.Default.DanmuFore;
            (focaldephov.FindResource("BubbleFore") as SolidColorBrush).Color = ForeColorPicker.SelectedColor;
            ComboBoxDanmuStyle.SelectedIndex = Properties.Settings.Default.DanmuStyle;
            SliderHovTime.Value = Properties.Settings.Default.HoverTime;
            SliderTextSize.Value = Properties.Settings.Default.MaxFontSize;
            SliderLayer.Value = Properties.Settings.Default.LayerNum;
            SliderBlur.Value = Properties.Settings.Default.MaxBlur;
            SliderFactor.Value = Properties.Settings.Default.ScaleFac;
            SliderRatio.Value = Properties.Settings.Default.InitTop;
            ComboBoxFont.Text = Properties.Settings.Default.ForeFont;
            ColorComboBoxBubble.SelectedColor = Properties.Settings.Default.BubbleColor;
            (focaldephov.FindResource("BubbleBack") as SolidColorBrush).Color = ColorComboBoxBubble.SelectedColor;
            FilterRegex = new Regex(regex);
            comboBoxSize.SelectedIndex = Properties.Settings.Default.SizeMode;

            System.Diagnostics.Debug.WriteLine(Environment.OSVersion.Version.Major);

            if(Environment.OSVersion.Version.Major < 10)
            {
                //Windows 10 以下
                checkBoxSurfaceDial.IsChecked = false;
                checkBoxSurfaceDial.IsEnabled = false;
                buttonSurfaceDial.Visibility = Visibility.Collapsed;
                Rec3.Visibility = Visibility.Visible;
            }
            else
            {
                buttonSurfaceDial.Visibility = Visibility.Visible;
                Rec3.Visibility = Visibility.Collapsed;
                checkBoxSurfaceDial.IsChecked = Properties.Settings.Default.SurfaceDial;

                if (Properties.Settings.Default.SurfaceDial)
                    InitializeController();
            }

            SliderSubBazelDist.Value = Properties.Settings.Default.SubBasel;

            GridSubtitler.IsEnabled = false;

            checkBoxSubtitleAlways.IsChecked = Properties.Settings.Default.SubAlways;

            checkBoxTxCloud.IsEnabled = false;

            checkBoxBottomBarAuto.IsChecked = !Properties.Settings.Default.BottomBarAuto;

            TextBoxGiftDanmu.Text = Properties.Settings.Default.GiftGivingCond;
            checkBoxGiftShow.IsChecked = Properties.Settings.Default.GiftGivingShow;

            SliderWCInterval.Value = Properties.Settings.Default.WCInterval;
            dispatcherTimerWC.Tick += DispatcherTimerWC_Tick;
            dispatcherTimerWC.Interval = TimeSpan.FromMinutes(SliderWCInterval.Value);

            string MaskAdd = Properties.Settings.Default.WCMaskAdd;
            if (MaskAdd != "")
            {
                try
                {
                    var img = new ImageSourceConverter().ConvertFromString(MaskAdd) as ImageSource;
                    WCMaskImg.Source = img;
                    WCMaskAdd.Content = MaskAdd;
                    //找到方法后放到线程中：//?有移位
                    //focaldephov.WordCloud.OpacityMask = new ImageBrush(img);
                }
                catch
                {
                    Properties.Settings.Default.WCMaskAdd = "";
                }
            }

            GridWordCloud.IsEnabled = false;
            WCAutoGenerate.IsChecked = Properties.Settings.Default.WCAutoGen;
            checkBoxWordCloudColor.IsChecked = Properties.Settings.Default.WCColor;
            sliderMaxVol.Value = (double)Properties.Settings.Default.WCVol - 1;

            textBoxUpperRight.Text = Properties.Settings.Default.UpperRight;
            checkBoxMute.IsChecked = Properties.Settings.Default.SyncMute;

        }


        #region 主页

        private void Unlock()
        {
            //解锁
            tabItemConfig.Visibility = Visibility.Visible;
            tabItemTest.Visibility = Visibility.Visible;
            tabItemStream.Visibility = Visibility.Visible;
            TabItemSetting.Visibility = Visibility.Visible;
            TabItemScreen.Visibility = Visibility.Visible;
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
            TabItemSetting.Visibility = Visibility.Collapsed;
            TabItemScreen.Visibility = Visibility.Collapsed;
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

            GridLANTip.Visibility = Visibility.Hidden;

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

            GridLANTip.Visibility = Visibility.Hidden;

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
            else if (radioButtonMan.IsChecked == true)
            {
                sourceType = 2;
                //pushCode = textBoxMan.Text;
            }
            else if (radioButtonLAN.IsChecked == true)
            {
                if (radioButtonReceiver.IsChecked == true &&
                    checkBoxTxCloud.IsChecked == true)
                    sourceType = 5;
                else if (radioButtonRecord.IsChecked == true || 
                        radioButtonScreen.IsChecked == true)
                        sourceType = 4;
                     else
                        sourceType = 3;
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
            if (radioButtonMan.IsChecked == false && radioButtonLAN.IsChecked == false)
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
                //Show_Monitor();

                if (editor == 1)
                {
                    textBoxOpt.Text += textBoxMan.Text + "\r\n";
                }
                else
                {
                    configdata.Add(new ConfigItem(configcount, sourceType, textBoxSourceName.Text.ToString(), textBoxMan.Text.ToString(), textBoxWebsite.Text.ToString()));
                    //configdata.Add(new ConfigItem(++configcount, 0, "ceshi","ceshi2"));
                    if (sourceType == 4)
                    {
                        if (textBoxWebsite.Text == "screen://")
                        {
                            configdata[configcount].DevOptions = new string[] {
                            ":screen-fps=30",
                            ":live-caching = 0"
                            };
                        }
                        else
                        {
                            var VidStr = comboBoxVideo.SelectedIndex == -1 ? "" :
                                    comboBoxVideo.SelectedValue.ToString().Replace("System.Windows.Controls.ComboBoxItem: ", "");
                            var AudStr = comboBoxAudio.SelectedIndex == -1 ? "" :
                                    comboBoxAudio.SelectedValue.ToString().Replace("System.Windows.Controls.ComboBoxItem: ", "");

                            configdata[configcount].DevOptions = new string[] {
                            ":dshow-vdev="+ VidStr,
                            ":dshow-adev="+ AudStr,
                            ":live-caching = 100",//本地缓存毫秒数 
                            ":dshow-aspect-ratio=16:9",
                            //":dshow-tuner-country=0",//不设置这个，录像没有声音，原因不明

                        };
                        }
                    }
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
                //textBoxOpt.Focus();
                //进行数据表录入
                textBoxOpt.Text = "";
                for (int i = 1; i < configcount; i++)
                {   //从第二个开始录入
                    if (configdata[i].StreamCode == "")
                        continue;   //跳过空项
                    if (checkBoxSolo.IsChecked.Equals(true) && configdata[i].Type == "局域网"
                        && configdata[i].StreamCode == configdata[i].LiveViewingSite)
                        continue;   //独立时，跳过局域网播流项
                    textBoxOpt.Text += "push " + configdata[i].StreamCode + ";" + "\r\n";
                }
                goodopt = textBoxOpt.Text;
                //changed = false;
                recover_trans();
            }

            //正常写入
            if (textBoxOpt.Text == "" && checkBoxSolo.IsChecked.Equals(false))
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
                buttonWrite.Content = "转换并写入";
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
            comboBoxSource.ItemsSource = configdata;
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

        private void radioButtonLAN_Checked(object sender, RoutedEventArgs e)
        {
            ManualConfig();
            if(radioButtonSender.IsChecked.Equals(true))
                Sender();
            else
                radioButtonSender.IsChecked = true;

            comboBoxVideo.IsEnabled = false;
            comboBoxAudio.IsEnabled = false;

            GridLANTip.Visibility = Visibility.Visible;
        }

        private void Sender()
        {
            textBlockTip.Text = "点击 +1流 后，请把播流地址传给直播主机。";
            //读取本机IP
            string strName = System.Net.Dns.GetHostName();
            IPHostEntry IPs = Dns.GetHostEntry(strName);
            string _ip = "";
            foreach (IPAddress ip in IPs.AddressList)
                if (ip.AddressFamily.ToString() == "InterNetwork")
                    _ip = ip.ToString();
            Debug.WriteLine(_ip);
            textBoxMan.Text = "rtmp://" + _ip + "/live";
            textBoxWebsite.Text = "rtmp://" + _ip + "/live";
            //ImgDownArrow.Visibility = Visibility.Visible;
            ImgRightArrow.Visibility = Visibility.Visible;
            textBoxSourceName.Text = "局域网播流";
            labelMan.Visibility = Visibility.Visible;
            textBoxMan.Visibility = Visibility.Visible;
            textBoxMan.IsReadOnly = true;
            textBoxWebsite.IsReadOnly = true;
        }

        private void radioButtonSender_Checked(object sender, RoutedEventArgs e)
        {
            Sender();
        }

        private void radioButtonReceiver_Checked(object sender, RoutedEventArgs e)
        {
            textBlockTip.Text = "请输入分机的播流地址，然后 +1流 。";
            textBoxWebsite.Text = "";
            textBoxMan.Text = "";
            //ImgDownArrow.Visibility = Visibility.Hidden;
            ImgRightArrow.Visibility = Visibility.Visible;
            labelMan.Visibility = Visibility.Hidden;
            textBoxMan.Visibility = Visibility.Hidden;
            textBoxSourceName.Text = "流" + configcount;
            textBoxWebsite.IsReadOnly = false;

            checkBoxTxCloud.IsEnabled = true;
        }

        private void radioButtonReceiver_Unchecked(object sender, RoutedEventArgs e)
        {
            checkBoxTxCloud.IsEnabled = false;
        }


        private void radioButtonRecord_Checked(object sender, RoutedEventArgs e)
        {
            comboBoxVideo.IsEnabled = true;
            comboBoxAudio.IsEnabled = true;
            textBlockTip.Text = "请选择视频和音频捕获设备，然后 +1 流。";
            labelWebsite.Content = "播流参数";
            textBoxSourceName.Text = "捕获" + configcount;
            textBoxWebsite.IsReadOnly = true;
            textBoxMan.Text = "";
            textBoxWebsite.Text = " :dshow-vdev=\"" + "\" :dshow-adev=\"" + "\"";
            ImgRightArrow.Visibility = Visibility.Hidden;
            RefreshOutput();

            comboBoxVideo.Items.Clear();

            try
            {
                var videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);

                if (videoDevices.Count == 0)
                    throw new ApplicationException();

                foreach (FilterInfo device in videoDevices)
                {
                    comboBoxVideo.Items.Add(device.Name);
                }
            }
            catch
            {

            }

            comboBoxAudio.Items.Clear();

            try
            {
                var audioDevices = new FilterInfoCollection(FilterCategory.AudioInputDevice);

                if (audioDevices.Count == 0)
                    throw new ApplicationException();

                foreach (FilterInfo device in audioDevices)
                {
                    comboBoxAudio.Items.Add(device.Name);
                }
            }
            catch
            {

            }

        }

        private void radioButtonRecord_Unchecked(object sender, RoutedEventArgs e)
        {
            comboBoxVideo.SelectedIndex = -1;
            comboBoxAudio.SelectedIndex = -1;
            comboBoxVideo.IsEnabled = false;
            comboBoxAudio.IsEnabled = false;
            labelWebsite.Content = "播流地址";
        }

        private void RefreshDevArg()
        {
            //Preview Only.
            var VidStr = comboBoxVideo.SelectedIndex == -1 ? "" :
                comboBoxVideo.SelectedValue.ToString().Replace("System.Windows.Controls.ComboBoxItem: ", "");
            var AudStr = comboBoxAudio.SelectedIndex == -1 ? "" :
                comboBoxAudio.SelectedValue.ToString().Replace("System.Windows.Controls.ComboBoxItem: ", "");

            textBoxWebsite.Text = "视频: " + VidStr + " 音频: " + AudStr;

        }

        private void comboBoxVideo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RefreshDevArg();
        }

        private void comboBoxAudio_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RefreshDevArg();
        }

        private void comboBoxVideo_DropDownOpened(object sender, EventArgs e)
        {

        }

        private void radioButtonScreen_Checked(object sender, RoutedEventArgs e)
        {
            textBlockTip.Text = "+1流后，可以监视显示器实时画面。";
            textBoxWebsite.IsReadOnly = true;
            textBoxWebsite.Text = "screen://";
            textBoxSourceName.Text = "桌面" + configcount;
            textBoxMan.Text = "";
            RefreshOutput();
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
                StartLive();
                TestProgress.Value += 10;


                dispatcherTimer.Start();


            }
            catch (Exception e)
            {
                Testlabel.Content = "错误：" + e.Message;
                TestProgress.Foreground = Brushes.Red;

                button1.IsEnabled = false;

                EndLive();//关闭

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


                    EndLive();//关闭

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

                    EndLive();//关闭

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

            EndLive();//关闭

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
            StartLive();
        }

        private void Button_EndLive_Click(object sender, RoutedEventArgs e)
        {
            EndLive();
        }

        private void StartLive()
        {
            Process.Start("Start_Nginx.bat");

            foreach (ConfigItem item in configdata)
            {
                if (item.Type == "局域网")
                {
                    Process.Start("Start_Server.bat");
                    break;
                }
            }
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

            Process.Start("nginx-server\\nginx.exe", "-s stop").WaitForExit();//关闭

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
                if (checkBoxSystemTime.IsChecked.Equals(true))
                    dispatcherTimerSysTime.Start();
                else
                    textBoxLiveTime.Text = "00:00:00";
            }
            else
            {
                dispatcherTimerRefresh.Stop();
                dispatcherTimerSysTime.Stop();
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

                if (checkBoxSystemTime.IsChecked.Equals(false))             //如果勾选系统时间 将不会按照这种方式刷新
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

        int blink = 0;
        SoundPlayer biplayer = new SoundPlayer("bi.wav");

        private void DispatcherTimerSysTime_Tick(object sender, EventArgs e)
        {
            var nowtime = System.DateTime.Now;
            textBoxLiveTime.Text = nowtime.ToString("HH:mm:ss")
                + " " + nowtime.Millisecond.ToString("000");
            if (screenSwitch)
            {
                CreateBitmapFromVisual();
            }
            if (!Properties.Settings.Default.SyncMute)
            {
                //约为每5秒响一次
                if (++blink > 330)
                {
                    biplayer.Play();
                    textBoxLiveTime.Background = Brushes.Yellow;
                    blink = 0;
                }
                else textBoxLiveTime.Background = Brushes.Transparent;
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
            EndLive();//关闭

            StartLive();

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
                        labelLive.Content = "解决程序冲突异常：" + ex.Message;
                    }

                }
                else
                {
                    labelLive.Content = "程序冲突解决结束";
                    EndLive(false);
                    if (checkBox2.IsChecked == false)
                    {
                        labelLive.Content += "，准备开始推流...";

                        StartLive();

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
            //WarPic.Visibility = Visibility.Visible;
            //Rec1.Visibility = Visibility.Visible;
            //labell1.Visibility = Visibility.Visible;
            //labell2.Visibility = Visibility.Visible;
            Rec1.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// 关闭ffmpeg警告信息
        /// </summary>
        private void Warning_Clear()
        {
            Properties.Settings.Default.NUffmpeg = false;
            //WarPic.Visibility = Visibility.Hidden;
            //Rec1.Visibility = Visibility.Hidden;
            //labell1.Visibility = Visibility.Hidden;
            //labell2.Visibility = Visibility.Hidden;
            Rec1.Visibility = Visibility.Collapsed;
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


        private void checkBoxSystemTime_Checked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.SysTime = true;
            Rec4.Visibility = Visibility.Visible;
        }

        private void checkBoxSystemTime_Unchecked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.SysTime = false;
            Rec4.Visibility = Visibility.Collapsed;
        }

        private void checkBoxTxCloud_Checked(object sender, RoutedEventArgs e)
        {
            RefreshOutput();
            labelWebsite.Content = "推流地址";
        }

        private void checkBoxTxCloud_Unchecked(object sender, RoutedEventArgs e)
        {
            RefreshOutput();
            labelWebsite.Content = "播流地址";
        }

        private void checkBoxBottomBarAuto_Checked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.BottomBarAuto = false;
        }

        private void checkBoxBottomBarAuto_Unchecked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.BottomBarAuto = true;
        }

        private void buttonNextFile_Click(object sender, RoutedEventArgs e)
        {
            ForeNextFile();
        }


        private void checkBoxSurfaceDial_Unchecked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.SurfaceDial = false;
        }

        private void SliderGiftNum_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (this.IsLoaded)
                Properties.Settings.Default.GiftGivingNum = (int)SliderGiftNum.Value + 1;
        }

        private void TextBoxGiftDanmu_TextChanged(object sender, TextChangedEventArgs e)
        {
            Properties.Settings.Default.GiftGivingCond = TextBoxGiftDanmu.Text;
        }

        private void checkBoxGiftShow_Checked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.GiftGivingShow = true;
        }

        private void checkBoxGiftShow_Unchecked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.GiftGivingShow = false;
        }

        private void comboBoxAudio_DropDownOpened(object sender, EventArgs e)
        {

        }


        private void checkBoxDanmuLink_Checked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.danmuLink = true;
            Rec2.Visibility = Visibility.Visible;
        }

        private void checkBoxDanmuLink_Unchecked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.danmuLink = false;
            Rec2.Visibility = Visibility.Collapsed;
        }

        private void SliderTransSec_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Properties.Settings.Default.TranSec = SliderTransSec.Value;
        }

        private void checkBoxNetwork_Checked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.OpenNetworkCaching = true;
        }

        private void checkBoxNetwork_Unchecked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.OpenNetworkCaching = false;
        }


        #endregion

        #region 监视器

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

                buttonHelp.Content = "关闭侧栏";

            }
            else
            {
                RightCol_Now = RightCol.Width.Value;

                expandRightCol.Pause();

                (expandRightCol.Children[0] as GridLengthAnimation).From = new GridLength(RightCol_Now, GridUnitType.Pixel);
                (expandRightCol.Children[0] as GridLengthAnimation).To = new GridLength(0, GridUnitType.Pixel);
                (expandRightCol.Children[0] as GridLengthAnimation).Duration = new Duration(TimeSpan.FromSeconds(0.5 * (RightCol_Now / 300)));

                expandRightCol.Begin(RightCol, HandoffBehavior.SnapshotAndReplace, true);

                //LabelLU.Content = "";
                //LabelRU.Content = "";
                //LabelLD.Content = "";

                //ProgressLD.Value = 0;
                //ProgressLU.Value = 0;
                //ProgressRU.Value = 0;

                //ComboSettingLoad = true;
                //comboBoxSource.SelectedIndex = -1;
                //ComboSettingLoad = false;
                buttonHelp.Content = "启动侧栏";

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

        // an universal architecture: PVW -> PGM

        byte selectedItem = 0;
        private void selectItem(byte selec)
        {
            ProgressTran.Visibility = Visibility.Collapsed;
            ComboSettingLoad = true;
            selectedItem = selec;
            if (selec < 4)
            {
                LabelDanmu.Visibility = Visibility.Hidden;
                comboBoxSource.IsEnabled = true;
                if (Monitors.ElementAt(selec - 1).PlayId <= comboBoxSource.Items.Count - 1)
                {
                    comboBoxSource.SelectedIndex = Monitors.ElementAt(selec - 1).PlayId;

                    if (screenSwitch && BackLive.IsSelected)
                    {
                        if (focaldephov.ForeImg.Opacity == 1)
                            ImgFadeOutAnim(buttonForeImg, focaldephov.ForeImg);
                        focaldephov.BackImg.SetBinding(Image.SourceProperty, Monitors.ElementAt(selectedItem - 1).Bing);
                    }
                }
                else
                {
                    comboBoxSource.SelectedIndex = -1;
                }

                if (comboBoxSource.SelectedIndex != -1 && configdata[Monitors.ElementAt(selec - 1).PlayId].Type != "捕获设备")
                    buttonExtPlayer.IsEnabled = Monitors.ElementAt(selectedItem - 1).SourceProvider.MediaPlayer.IsPlaying().Equals(true);

                if (checkBoxNetwork.IsChecked.Equals(true)
                    && Monitors.ElementAt(selec - 1).PlayId<comboBoxSource.Items.Count
                    && configdata[Monitors.ElementAt(selec - 1).PlayId].Type!="捕获设备"
                    )
                {
                    SliderNetwork.Visibility = Visibility.Visible;
                    SliderNetwork.Value = (double)Monitors.ElementAt(selectedItem - 1).Network;
                }
                else
                {
                    SliderNetwork.Visibility = Visibility.Collapsed;
                }

            }
            else
            {
                SliderNetwork.Visibility = Visibility.Collapsed;

                LabelDanmu.Visibility = Visibility.Visible;
                comboBoxSource.IsEnabled = false;
                comboBoxSource.SelectedIndex = -1;
                if (screenSwitch && BackLive.IsSelected)
                {//切为前景图片
                    if (focaldephov.ForeImg.Opacity == 0)
                        ImgFadeOutAnim(buttonForeImg, focaldephov.ForeImg);
                    focaldephov.BackImg.SetBinding(Image.SourceProperty, new Binding());
                }
                else
                {
                    focaldephov.BackImg.SetBinding(Image.SourceProperty, new Binding());
                }
                buttonExtPlayer.IsEnabled = false;
            }
            buttonSolo.IsEnabled = true;
            MonitoringChanged();
            ComboSettingLoad = false;

            buttonSideBySide.BorderThickness = new Thickness(1);
            buttonLRSplit.BorderThickness = new Thickness(1);
            buttonUDSplit.BorderThickness = new Thickness(1);
            buttonaSWindow.BorderThickness = new Thickness(1);
            TranEffect = TranEffects.None;

        }

        private void HardCut(byte to)
        {
            if (sc != null && sc.IsLoaded && Properties.Settings.Default.SmartPA > 0)
            {
                SJLcutter(to, false);
            }
            GridTranEffect.Visibility = Visibility.Collapsed;
            selectItem(to);
            RightCol.Focus();
        }

        private void ButtonLU_Click(object sender, RoutedEventArgs e)
        {
            HardCut(1);
        }

        private void ButtonRU_Click(object sender, RoutedEventArgs e)
        {
            HardCut(2);
        }

        private void ButtonLD_Click(object sender, RoutedEventArgs e)
        {
            HardCut(3);
        }

        private void ButtonRD_Click(object sender, RoutedEventArgs e)
        {
            HardCut(4);
        }

        private void MonitoringChanged()
        {
            BorderLU.BorderBrush.Opacity = (selectedItem == 1 ? 1 : 0);
            BorderRU.BorderBrush.Opacity = (selectedItem == 2 ? 1 : 0);
            BorderLD.BorderBrush.Opacity = (selectedItem == 3 ? 1 : 0);
            BorderRD.BorderBrush.Opacity = (selectedItem == 4 ? 1 : 0);
        }

        private void trantoVis(byte Tranto)
        {
            tranto = Tranto;

            BorderLU.BorderBrush.Opacity = (selectedItem == 1 ? 1 : (tranto == 1 ? 0.5 : 0));
            BorderRU.BorderBrush.Opacity = (selectedItem == 2 ? 1 : (tranto == 2 ? 0.5 : 0));
            BorderLD.BorderBrush.Opacity = (selectedItem == 3 ? 1 : (tranto == 3 ? 0.5 : 0));
            BorderRD.BorderBrush.Opacity = (selectedItem == 4 ? 1 : (tranto == 4 ? 0.5 : 0));

            focaldephov.TransitionImg.Source = focaldephov.BackImg.Source;
            focaldephov.BackImg.Opacity = 0;
            focaldephov.TransitionImg.Opacity = 1;
            if (tranto < 4)
            {
                focaldephov.BackImg.SetBinding(Image.SourceProperty, Monitors.ElementAt(tranto - 1).Bing);
                ProgressTran.Value = 1;
                ProgressTran.Visibility = Visibility.Visible;
            }
            else
            {
                focaldephov.BackImg.Source = null;
                ProgressTran.Visibility = Visibility.Visible;
                ProgressTran.Value = 0;
            }

            //对Fade模式记录原音量
            if (sc != null && sc.IsLoaded)
            {
                switch (selectedItem)
                {
                    case 1: fade_selc_ori = sc.Slider1.Value; break;
                    case 2: fade_selc_ori = sc.Slider2.Value; break;
                    case 3: fade_selc_ori = sc.Slider3.Value; break;
                }

                switch (tranto)
                {
                    case 1: fade_to_ori = sc.Slider1.Value; break;
                    case 2: fade_to_ori = sc.Slider2.Value; break;
                    case 3: fade_to_ori = sc.Slider3.Value; break;
                }
                
            }
            
        }

        Storyboard fadet = new Storyboard();

        private void SJLcutter(byte to, bool maintain)
        {
            if (!maintain)
            {
                if (selectedItem > 0 && selectedItem < 4)
                    SoundControl.soundControllers.ElementAt(selectedItem - 1).On = false;
            }
            if (to < 4)
                SoundControl.soundControllers.ElementAt(to - 1).On = true;
            MSC.Invoke(this, new MainSelecChangedArgs() { });

        }

        private void transition(byte selc)
        {
            //增加入口限制
            if (BackLive.IsSelected)
            {

                if (sc != null && sc.IsLoaded && Properties.Settings.Default.SmartPA == 1)  //J cut
                {
                    SJLcutter(selc, false);
                }

                trantoVis(selc);


                DoubleAnimation opf = new DoubleAnimation()
                {
                    From = 1,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(Properties.Settings.Default.TranSec)         //一个其他量
                };

                //Storyboard.SetTarget(opf, SliderTransSec);
                Storyboard.SetTargetProperty(opf, new PropertyPath("(Slider.Value)"));
                fadet.Children.Add(opf);
                fadet.Completed += Fadet_Completed;
                fadet.Begin(ProgressTran, HandoffBehavior.SnapshotAndReplace, true);

                RightCol.Focus();
            }
            

        }

        private void Fadet_Completed(object sender, EventArgs e)
        {
            double fin = ProgressTran.Value;
            fadet.Remove(ProgressTran);
            ProgressTran.Value = fin;
            if (fin == 0 || fin == 1)
            {
                TranEffect = TranEffects.None;  //归零
            }
            buttonSideBySide.IsEnabled = true;
            buttonLRSplit.IsEnabled = true;
            buttonUDSplit.IsEnabled = true;
            buttonaSWindow.IsEnabled = true;
        }

        byte tranto;

        private void transition_manual(byte selc)
        {
            if (BackLive.IsSelected)
            {
                if (sc != null && sc.IsLoaded && Properties.Settings.Default.SmartPA > 0)  //J cut
                {
                    SJLcutter(selc, true);      //手动时不切 启动另一监视器声音。
                }

                trantoVis(selc);

                GridTranEffect.Visibility = Visibility.Visible;
            }
        }

        double fade_selc_ori, fade_to_ori;

        private void ProgressTran_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (ProgressTran.Visibility == Visibility.Visible)
            {
                if (TranEffect == TranEffects.None)
                    focaldephov.BackImg.Opacity = 1 - ProgressTran.Value;
                else if (cross)
                    focaldephov.BackImg.Opacity = 1;
                else
                    focaldephov.BackImg.Opacity = (ProgressTran.Value > 0.5) ? 2 * (1 - ProgressTran.Value) : 1;
                TranEffectStyle();

                if (sc != null && sc.IsLoaded && Properties.Settings.Default.SmartPA == 3 && tranto != selectedItem)
                {
                    //音频交叉淡化
                    switch (tranto)
                    {
                        case 1: sc.Slider1.Value = fade_to_ori * (1 - ProgressTran.Value); if (sc.Slider1.Value == 0) { sc.Slider1.Value = fade_to_ori; } break;
                        case 2: sc.Slider2.Value = fade_to_ori * (1 - ProgressTran.Value); if (sc.Slider2.Value == 0) { sc.Slider2.Value = fade_to_ori; } break;
                        case 3: sc.Slider3.Value = fade_to_ori * (1 - ProgressTran.Value); if (sc.Slider3.Value == 0) { sc.Slider3.Value = fade_to_ori; } break;
                    }
                    switch (selectedItem)
                    {
                        case 1: sc.Slider1.Value = fade_selc_ori * ProgressTran.Value; if (sc.Slider1.Value == 0) { sc.Slider1.Value = fade_selc_ori; } break;
                        case 2: sc.Slider2.Value = fade_selc_ori * ProgressTran.Value; if (sc.Slider2.Value == 0) { sc.Slider2.Value = fade_selc_ori; } break;
                        case 3: sc.Slider3.Value = fade_selc_ori * ProgressTran.Value; if (sc.Slider3.Value == 0) { sc.Slider3.Value = fade_selc_ori; } break;
                    }
                }

                if (MSC != null)
                    MSC.Invoke(this, new MainSelecChangedArgs() { });

                if (ProgressTran.Value == 0)
                {
                    if (sc != null && sc.IsLoaded && Properties.Settings.Default.SmartPA > 0)  //J/L cut
                    {
                        SJLcutter(tranto, false);
                    }
                    //结束了
                    ProgressTran.Visibility = Visibility.Collapsed;
                    GridTranEffect.Visibility = Visibility.Collapsed;
                    selectItem(tranto);
                }
                else if (ProgressTran.Value == 1)
                {
                    if (sc != null && sc.IsLoaded && Properties.Settings.Default.SmartPA > 0)  //J/L cut
                    {
                        SJLcutter(selectedItem, false);
                    }
                    //取消了
                    ProgressTran.Visibility = Visibility.Collapsed;
                    GridTranEffect.Visibility = Visibility.Collapsed;
                    selectItem(selectedItem);
                }
                
            }
        }

        enum TranEffects { None, SideBySide, LRSplit, UDSplit, SWindow };
        TranEffects TranEffect = TranEffects.None;

        Matrix backMatrix_init, tranMatrix_init;
        Matrix backMatrix_fin, tranMatrix_fin;
        Rect backRect_init, tranRect_init;
        Rect backRect_fin, tranRect_fin;

        private void DefineStates()
        {
            backMatrix_init = focaldephov.BackImg.RenderTransform.Value;
            tranMatrix_init = focaldephov.TransitionImg.RenderTransform.Value;

            backRect_init = focaldephov.BackClipRect.Rect;
            tranRect_init = focaldephov.TranClipRect.Rect;

            //if (ProgressTran.Value > 0.5)
            //{
            switch (TranEffect)
                {
                    case TranEffects.SideBySide:
                        backMatrix_fin = new Matrix(4.0 / 9, 0,
                            0, 4.0 / 9,
                            +(0.3 / 16 + 2.0 / 9) * focaldephov.Width, 0);
                        tranMatrix_fin = new Matrix(4.0 / 9, 0,
                            0, 4.0 / 9,
                            -(0.3 / 16 + 2.0 / 9) * focaldephov.Width, 0);
                        if (buttonSideBySide.BorderThickness.Equals(new Thickness(3)) && focaldephov.ForeImg.Opacity==0||
                        (buttonSideBySide.BorderThickness.Equals(new Thickness(1)) && focaldephov.ForeImg.Opacity == 1))
                        {
                            ImgFadeOutAnim(buttonForeImg, focaldephov.ForeImg);
                        }
                        backRect_fin = new Rect(0, 0, focaldephov.Width, focaldephov.Height);
                        tranRect_fin = new Rect(0, 0, focaldephov.Width, focaldephov.Height);
                        break;
                    case TranEffects.LRSplit:
                        backMatrix_fin = new Matrix(1, 0,
                            0, 1,
                            focaldephov.Width/4, 0
                        );
                        tranMatrix_fin = new Matrix(1, 0, 0, 1, -focaldephov.Width / 4, 0);
                        backRect_fin = new Rect(focaldephov.Width / 4, 0, focaldephov.Width / 2, focaldephov.Height);
                        tranRect_fin = new Rect(focaldephov.Width / 4, 0, focaldephov.Width / 2, focaldephov.Height);
                        break;
                    case TranEffects.UDSplit:
                        backMatrix_fin = new Matrix(1, 0,
                            0, 1,
                            0, focaldephov.Height/4
                        );
                        tranMatrix_fin = new Matrix(1, 0, 0, 1, 0, -focaldephov.Height / 4);
                        backRect_fin = new Rect(0, focaldephov.Height / 4, focaldephov.Width, focaldephov.Height/2);
                        tranRect_fin = new Rect(0, focaldephov.Height / 4, focaldephov.Width, focaldephov.Height/2);
                        break;
                    case TranEffects.SWindow:
                        double border = 1.0 / 16 * focaldephov.Width;
                        backMatrix_fin = new Matrix(2.5 / 9, 0,
                            0, 2.5 / 9,
                            (1.0 / 2 - 1.25 / 9) * focaldephov.Width - border, (1.0 / 2 - 1.25 / 9) * focaldephov.Height - border );
                        tranMatrix_fin = Matrix.Identity;
                        backRect_fin = new Rect(0, 0, focaldephov.Width, focaldephov.Height);
                        tranRect_fin = new Rect(0, 0, focaldephov.Width, focaldephov.Height);
                        break;
                }
            //}
            //else
            //{
            //    backMatrix_fin = Matrix.Identity;
            //    tranMatrix_fin = Matrix.Identity;
            //}

        }

        private void TranEffectStyle()
        {
            //现在已经载入图像
            if (TranEffect != TranEffects.None)
            {
                //定比分点计算
                double progress = 1 - Math.Abs(2 * (ProgressTran.Value - 0.5));
                Matrix backMatrix = new Matrix(
                    backMatrix_init.M11 + (backMatrix_fin.M11 - backMatrix_init.M11) * progress, 0,
                    0, backMatrix_init.M22 + (backMatrix_fin.M22 - backMatrix_init.M22) * progress,
                    backMatrix_init.OffsetX + (backMatrix_fin.OffsetX - backMatrix_init.OffsetX) * progress, backMatrix_init.OffsetY + (backMatrix_fin.OffsetY - backMatrix_init.OffsetY) * progress);
                focaldephov.BackImg.RenderTransform = new MatrixTransform(backMatrix);

                Matrix tranMatrix = new Matrix(
                    tranMatrix_init.M11 + (tranMatrix_fin.M11 - tranMatrix_init.M11) * progress, 0,
                    0, tranMatrix_init.M22 + (tranMatrix_fin.M22 - tranMatrix_init.M22) * progress,
                    tranMatrix_init.OffsetX + (tranMatrix_fin.OffsetX - tranMatrix_init.OffsetX) * progress, tranMatrix_init.OffsetY + (tranMatrix_fin.OffsetY - tranMatrix_init.OffsetY) * progress);
                focaldephov.TransitionImg.RenderTransform = new MatrixTransform(tranMatrix);

                Rect backRect = new Rect(
                    backRect_init.X + (backRect_fin.X - backRect_init.X) * progress,
                    backRect_init.Y + (backRect_fin.Y - backRect_init.Y) * progress,
                    backRect_init.Width + (backRect_fin.Width - backRect_init.Width) * progress,
                    backRect_init.Height + (backRect_fin.Height - backRect_init.Height) * progress
                    );
                focaldephov.BackClipRect.Rect = backRect;

                Rect tranRect = new Rect(
                    tranRect_init.X + (tranRect_fin.X - tranRect_init.X) * progress,
                    tranRect_init.Y + (tranRect_fin.Y - tranRect_init.Y) * progress,
                    tranRect_init.Width + (tranRect_fin.Width - tranRect_init.Width) * progress,
                    tranRect_init.Height + (tranRect_fin.Height - tranRect_init.Height) * progress
                    );
                focaldephov.TranClipRect.Rect = tranRect;

            }
        }

        bool cross = false;

        private void TranEffectChanged()
        {
            //SliderAnim

            DoubleAnimation opf;

            if ((TranEffect == TranEffects.SideBySide && buttonSideBySide.BorderThickness.Equals(new Thickness(3)))||
                (TranEffect == TranEffects.LRSplit && buttonLRSplit.BorderThickness.Equals(new Thickness(3)))||
                (TranEffect == TranEffects.UDSplit && buttonUDSplit.BorderThickness.Equals(new Thickness(3)))||
                (TranEffect == TranEffects.SWindow && buttonaSWindow.BorderThickness.Equals(new Thickness(3)))
                )
            {
                cross = false;

                opf = new DoubleAnimation()
                {
                    From = ProgressTran.Value,          //0.5-->0
                    To = 0,
                    Duration = TimeSpan.FromSeconds(Properties.Settings.Default.TranSec),         //一个其他量
                    EasingFunction = new BackEase()
                    {
                        EasingMode = EasingMode.EaseOut,
                        Amplitude = 0.3
                    }
                };

                buttonSideBySide.BorderThickness = new Thickness(1);
                buttonLRSplit.BorderThickness = new Thickness(1);
                buttonUDSplit.BorderThickness = new Thickness(1);
                buttonaSWindow.BorderThickness = new Thickness(1);

                //反向输出
                backMatrix_init = Matrix.Identity;
                tranMatrix_init = Matrix.Identity;
                backMatrix_fin = focaldephov.BackImg.RenderTransform.Value;
                tranMatrix_fin = focaldephov.TransitionImg.RenderTransform.Value;

                backRect_init = new Rect(0, 0, focaldephov.Width, focaldephov.Height);
                tranRect_init = new Rect(0, 0, focaldephov.Width, focaldephov.Height);
                backRect_fin = focaldephov.BackClipRect.Rect;
                backRect_fin = focaldephov.TranClipRect.Rect;

                RightCol.Focus();
            }
            else
            {
                int SelectedMode = 0;
                if (buttonSideBySide.BorderThickness.Equals(new Thickness(3)))
                    SelectedMode = 1;
                else if (buttonLRSplit.BorderThickness.Equals(new Thickness(3)))
                    SelectedMode = 2;
                else if (buttonUDSplit.BorderThickness.Equals(new Thickness(3)))
                    SelectedMode = 3;
                else if (buttonaSWindow.BorderThickness.Equals(new Thickness(3)))
                    SelectedMode = 4;

                if (SelectedMode > 0 && SelectedMode != (int)TranEffect) 
                {
                    //不相符
                    cross = true;
                }
                else
                {
                    cross = false;
                }

                //Pre-Assemble
                ProgressTran.Visibility = Visibility.Collapsed;
                ProgressTran.Value = 1;
                ProgressTran.Visibility = Visibility.Visible;

                opf = new DoubleAnimation()
                {
                    From = 1,          //1-->0.5
                    To = 0.5,
                    Duration = TimeSpan.FromSeconds(Properties.Settings.Default.TranSec),         //一个其他量
                    EasingFunction = new BackEase()
                    {
                        EasingMode = EasingMode.EaseIn,
                        Amplitude = 0.3
                    }
                };


                buttonSideBySide.BorderThickness =
                (TranEffect == TranEffects.SideBySide) ? new Thickness(3) : new Thickness(1);
                buttonLRSplit.BorderThickness =
                    (TranEffect == TranEffects.LRSplit) ? new Thickness(3) : new Thickness(1);
                buttonUDSplit.BorderThickness =
                    (TranEffect == TranEffects.UDSplit) ? new Thickness(3) : new Thickness(1);
                buttonaSWindow.BorderThickness =
                    (TranEffect == TranEffects.SWindow) ? new Thickness(3) : new Thickness(1);

                DefineStates();

            }

            //if (sc != null && sc.IsLoaded && Properties.Settings.Default.SmartPA > 0)  //J cut
            //{
            //    SJLcutter(selc, true);
            //}


            Storyboard.SetTargetProperty(opf, new PropertyPath("(Slider.Value)"));
            fadet.Children.Add(opf);
            fadet.Completed += Fadet_Completed;
            fadet.Begin(ProgressTran, HandoffBehavior.SnapshotAndReplace, true);

            buttonSideBySide.IsEnabled = false;
            buttonLRSplit.IsEnabled = false;
            buttonUDSplit.IsEnabled = false;
            buttonaSWindow.IsEnabled = false;

        }

        private void buttonSideBySide_Click(object sender, RoutedEventArgs e)
        {
            if (TranEffect != TranEffects.SideBySide)
                TranEffect = TranEffects.SideBySide;
            TranEffectChanged();
        }

        private void buttonLRSplit_Click(object sender, RoutedEventArgs e)
        {
            if (TranEffect != TranEffects.LRSplit)
                TranEffect = TranEffects.LRSplit;
            TranEffectChanged();
        }

        private void buttonUDSplit_Click(object sender, RoutedEventArgs e)
        {
            if (TranEffect != TranEffects.UDSplit)
                TranEffect = TranEffects.UDSplit;
            TranEffectChanged();
        }

        private void buttonaSWindow_Click(object sender, RoutedEventArgs e)
        {
            if (TranEffect != TranEffects.SWindow)
                TranEffect = TranEffects.SWindow;
            TranEffectChanged();
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

        // Reframed. 

        public static List<Monitor> Monitors;

        /// <summary>
        /// 监视器类
        /// </summary>
        public class Monitor
        {
            private int playId;
            private string playStream;
            private VlcVideoSourceProvider sourceProvider;
            //private tstRtmp testRtmp;
            private int volume;
            private double opacity;
            private Binding bing;
            private string[] option;
            private int network;
            //private Thread thPlayer;
            //private BitmapSource bs;
            //private WriteableBitmap wb;

            public int PlayId
            {
                get { return playId; }
                set { playId = value; }
            }

            public string PlayStream
            {
                get { return playStream; }
                set
                {
                    playStream = value;
                }
            }

            public VlcVideoSourceProvider SourceProvider
            {
                get { return sourceProvider; }
                set
                {
                    sourceProvider = value;
                }
            }

            //public tstRtmp TstRtmp
            //{
            //    get { return testRtmp; }
            //    set
            //    {
            //        testRtmp = value;
            //    }
            //}

            //public Thread ThPlayer
            //{
            //    get { return thPlayer; }
            //    set
            //    {
            //        thPlayer = value;
            //    }
            //}

            //public BitmapSource Bs
            //{
            //    get { return bs; }
            //    set
            //    {
            //        bs = value;
            //    }
            //}

            //public WriteableBitmap Wb
            //{
            //    get { return wb; }
            //    set
            //    {
            //        wb = value;
            //    }
            //}

            public int Volume
            {
                get { return volume; }
                set
                {
                    volume = value;
                    sourceProvider.MediaPlayer.Audio.Volume = volume;
                }
            }

            public double Opacity
            {
                get { return opacity; }
                set
                {
                    opacity = value;
                }
            }

            public Binding Bing
            {
                get { return bing; }
                set
                {
                    bing = value;
                }
            }

            public string[] Option
            {
                get { return option; }
                set { option = value; }
            }

            public int Network
            {
                get { return network; }
                set
                {
                    option = new[]
                    {
                        "--network-caching=" + value
                    };
                    network = value;
                }
            }

            /// <summary>
            /// 播放线程执行方法
            /// </summary>
            public unsafe void DeCoding()
            {
                //try
                //{
                //    //
                //    // 更新图片显示
                //    tstRtmp.ShowBitmap show = (width, height, stride, data) =>
                //    {
                //        //
                //    }));
                //    TstRtmp.Start(show,PlayStream);

                //}
                //catch (Exception ex)
                //{
                //    Debug.WriteLine(ex);
                //}
                //finally
                //{
                //    Console.WriteLine("DeCoding exit");
                //    TstRtmp.Stop();

                //    ThPlayer = null;
                //}

            }

            public Monitor()
            {
                PlayId = 1000;
                Bing = new Binding();
                Network = 500;
            }
            //需要在主进程初始化
        }


        bool ComboSettingLoad = false;
        private void ComboBoxSource_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!ComboSettingLoad)
            {
                if (selectedItem < 4)
                {
                    if (comboBoxSource.SelectedIndex != -1 && configdata[comboBoxSource.SelectedIndex].LiveViewingSite!=null)
                    {   //防止访问冲突
                        if (Monitors.ElementAt(selectedItem - 1).SourceProvider.MediaPlayer.IsPlaying().Equals(true))
                            Monitors.ElementAt(selectedItem - 1).SourceProvider.MediaPlayer.Pause();
                        Monitors.ElementAt(selectedItem - 1).PlayStream = configdata[comboBoxSource.SelectedIndex].LiveViewingSite.ToString();
                        Monitors.ElementAt(selectedItem - 1).PlayId = comboBoxSource.SelectedIndex;
                    }
                    if (Monitors.ElementAt(selectedItem - 1).PlayStream != null)
                    {
                        try
                        {
                            //输出图片
                            string SourceName = null;
                            
                            SourceName = configdata[comboBoxSource.SelectedIndex].SourceName.ToString();

                            if (configdata[comboBoxSource.SelectedIndex].Type == "B站" ||
                                configdata[comboBoxSource.SelectedIndex].Type == "微博")
                                Monitors.ElementAt(selectedItem - 1).Network = 1000;
                            else if (configdata[comboBoxSource.SelectedIndex].Type == "捕获设备")
                                Monitors.ElementAt(selectedItem - 1).Option = configdata[comboBoxSource.SelectedIndex].DevOptions;
                            else
                                Monitors.ElementAt(selectedItem - 1).Network = 500;


                            //if (checkBoxLowMoni.IsChecked.Equals(false))
                            //{   //传统 VLC 入口

                            //Monitors.ElementAt(selectedItem - 1).SourceProvider.MediaPlayer.ResetMedia();
                            if (configdata[comboBoxSource.SelectedIndex].Type == "捕获设备")
                            {
                                //Debug.WriteLine(Monitors.ElementAt(selectedItem - 1).Option[0]);
                                if (Monitors.ElementAt(selectedItem - 1).PlayStream == "screen://")
                                {
                                    Monitors.ElementAt(selectedItem - 1).SourceProvider.MediaPlayer.Play(
                                        @"screen://  ",
                                        Monitors.ElementAt(selectedItem - 1).Option
                                        );
                                }
                                else
                                {
                                    Monitors.ElementAt(selectedItem - 1).SourceProvider.MediaPlayer.Play(
                                        @"dshow://  ",
                                        Monitors.ElementAt(selectedItem - 1).Option
                                        );
                                }
                                
                            }
                            else
                                Monitors.ElementAt(selectedItem - 1).SourceProvider.MediaPlayer.Play(
                                    Monitors.ElementAt(selectedItem - 1).PlayStream, Monitors.ElementAt(selectedItem - 1).Option);
                            Monitors.ElementAt(selectedItem - 1).Volume = 0;

                            Monitors.ElementAt(selectedItem - 1).Bing = new Binding();
                            Monitors.ElementAt(selectedItem - 1).Bing.Source = Monitors.ElementAt(selectedItem - 1).SourceProvider;
                            Monitors.ElementAt(selectedItem - 1).Bing.Path = new PropertyPath("VideoSource");
                                
                                 
                            //}
                            //else
                            //{
                            //    Monitors.ElementAt(selectedItem - 1).TstRtmp.Stop();
                            //    Monitors.ElementAt(selectedItem - 1).Bing.Source = Monitors.ElementAt(selectedItem - 1).Wb;
                            //    Monitors.ElementAt(selectedItem - 1).Bing.Path = null;
                            //    //Monitors.ElementAt(selectedItem - 1).ThPlayer = new Thread(Monitors.ElementAt(selectedItem - 1).DeCoding);
                            //    Monitors.ElementAt(selectedItem - 1).ThPlayer = new Thread(Monitors.ElementAt(selectedItem - 1).DeCoding);
                            //    Monitors.ElementAt(selectedItem - 1).ThPlayer.IsBackground = true;
                            //    Monitors.ElementAt(selectedItem - 1).ThPlayer.Start();
                            //}
                            switch (selectedItem)
                            {
                                case 1: LabelLU.Content = "左上:" + SourceName; if(sc!=null&&sc.IsLoaded) sc.labelSN1.Content = SourceName; LiveLU.SetBinding(Image.SourceProperty, Monitors.ElementAt(selectedItem - 1).Bing); break;
                                case 2: LabelRU.Content = "右上:" + SourceName; if (sc != null && sc.IsLoaded) sc.labelSN2.Content = SourceName; LiveRU.SetBinding(Image.SourceProperty, Monitors.ElementAt(selectedItem - 1).Bing); break;
                                case 3: LabelLD.Content = "左下:" + SourceName; if (sc != null && sc.IsLoaded) sc.labelSN3.Content = SourceName; LiveLD.SetBinding(Image.SourceProperty, Monitors.ElementAt(selectedItem - 1).Bing); break;
                                case 4: LabelRD.Content = "右下:" + SourceName; LiveRD.SetBinding(Image.SourceProperty, Monitors.ElementAt(selectedItem - 1).Bing); break;
                            }
                            selectItem(selectedItem);
                            buttonExtPlayer.IsEnabled = true;

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
            LiveSolo();
        }

        private void LiveSolo()
        {
            Storyboard Soloanim = this.FindResource("SoloAnimation") as Storyboard;

            int shrinkcol, shrinkrow;

            

            double state;
            if (buttonRD.IsEnabled)
            {
                state = 0;
                SoloImg.Opacity = 1;
            }
            else
            {
                state = 0.5;
                SoloImg.Opacity = 0.5;
            }

            switch (selectedItem)
            {
                case 1: shrinkcol = 1; shrinkrow = 1; break;
                case 2: shrinkcol = 0; shrinkrow = 1; break;
                case 3: shrinkcol = 1; shrinkrow = 0; break;
                case 4: shrinkcol = 0; shrinkrow = 0; break;
                default: shrinkcol = 1; shrinkrow = 1; break;
            }

            Monitoring.ColumnDefinitions.ElementAt(shrinkcol).Width = new GridLength(state, GridUnitType.Star);
            Monitoring.RowDefinitions.ElementAt(shrinkrow).Height = new GridLength(state, GridUnitType.Star);
            Monitoring.ColumnDefinitions.ElementAt(1-shrinkcol).Width = new GridLength(1 - state, GridUnitType.Star);
            Monitoring.RowDefinitions.ElementAt(1-shrinkrow).Height = new GridLength(1 - state, GridUnitType.Star);

            //Soloanim.Children[0].SetValue(Storyboard.TargetNameProperty, "Col" + shrinkcol);
            //(Soloanim.Children[0] as GridLengthAnimation).To = new GridLength(state, GridUnitType.Star);
            //Soloanim.Children[1].SetValue(Storyboard.TargetNameProperty, "Row" + shrinkrow);
            //(Soloanim.Children[1] as GridLengthAnimation).To = new GridLength(state, GridUnitType.Star);
            //Soloanim.Children[2].SetValue(Storyboard.TargetNameProperty, "Col" + (1 - shrinkcol));
            //(Soloanim.Children[2] as GridLengthAnimation).To = new GridLength(1 - state, GridUnitType.Star);
            //Soloanim.Children[3].SetValue(Storyboard.TargetNameProperty, "Row" + (1 - shrinkrow));
            //(Soloanim.Children[3] as GridLengthAnimation).To = new GridLength(1 - state, GridUnitType.Star);
            //Soloanim.Begin();
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
                LiveRD.Visibility = Visibility.Hidden;              //关闭警报时将监视隐藏
                DismissImg.Opacity = 1;
                //monred = false;
            }
            else
            {
                dismiss = false;
                DismissImg.Opacity = 0.5;
                LiveRD.Visibility = Visibility.Visible; 
            }


        }

        private void checkBoxLowMoni_Checked(object sender, RoutedEventArgs e)
        {
            Rec2.Visibility = Visibility.Visible;
            Properties.Settings.Default.LowMoni = true;
        }

        private void checkBoxLowMoni_Unchecked(object sender, RoutedEventArgs e)
        {
            Rec2.Visibility = Visibility.Collapsed;
            Properties.Settings.Default.LowMoni = false;
        }

        private void ButtonTextFore_Click(object sender, RoutedEventArgs e)
        {
            focaldephov.Topmost = true;
        }

        public delegate void MoniCallBackDele(int selec);

        private void CallBackContinue(int selec)
        {
            Monitors.ElementAt(selec - 1).SourceProvider.MediaPlayer.Play(
                                    Monitors.ElementAt(selec - 1).PlayStream, Monitors.ElementAt(selectedItem - 1).Option);
        }

        public void FFplay(object o)
        {
            var selec = selectedItem;
            Monitors.ElementAt(selectedItem - 1).SourceProvider.MediaPlayer.Stop();
            try
            {
                using (Process process = new System.Diagnostics.Process())
                {
                    process.StartInfo.FileName = "ffplay.exe";
                    process.StartInfo.Arguments = "-fflags nobuffer \""
                    + Monitors.ElementAt(selectedItem - 1).PlayStream + "\""
                    + (Monitors.ElementAt(selectedItem - 1).Volume == 0 ? " -an" : "");
                    // 必须禁用操作系统外壳程序  
                    process.StartInfo.UseShellExecute = true;
                    process.StartInfo.CreateNoWindow = false;
                    process.StartInfo.RedirectStandardOutput = false;

                    process.Start();
                    process.WaitForExit();
                    process.Close();
                }
            }
            catch
            {

            }
            //Process.Start("ffplay.exe", "-fflags nobuffer \""
            //        + Monitors.ElementAt(selectedItem - 1).PlayStream + "\""
            //        + (Monitors.ElementAt(selectedItem - 1).Volume == 0 ? " -an" : "")       //静音
            //        ).WaitForExit();
            MoniCallBackDele mcb = o as MoniCallBackDele;
            mcb(selec);
        }

        private void buttonExtPlayer_Click(object sender, RoutedEventArgs e)
        {
            //打开外置的ffplay窗口

            //加载时显示在第二屏幕上
            var tmpl = Left;
            var tmpt = Top;
            if (AllScreens.Length > 1)
            {//第二屏幕
                Left = PrimaryScreen.WorkingArea.Width;
                Top = 0;
            }

            MoniCallBackDele mcb = CallBackContinue;

            Thread play = new Thread(FFplay);
            play.IsBackground = true;
            play.Start(mcb);

            switch (selectedItem)
            {
                case 1: LabelLU.Content = "按ESC退出低延迟外置监视器"; break;
                case 2: LabelRU.Content = "按ESC退出低延迟外置监视器"; break;
                case 3: LabelLD.Content = "按ESC退出低延迟外置监视器"; break;
                case 4: LabelRD.Content = "按ESC退出低延迟外置监视器"; break;
            }
            buttonExtPlayer.IsEnabled = false;

            //ComboSettingLoad = true;
            //comboBoxSource.SelectedIndex = -1;
            //ComboSettingLoad = false;

            if (focaldephov != null)
            {
                focaldephov.Topmost = true;
            }
            this.Topmost = true;

            Left = tmpl;
            Top = tmpt;

        }


        private void SliderNetwork_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (selectedItem > 0 && selectedItem < 4
                && Monitors.ElementAt(selectedItem - 1).Network != (int)SliderNetwork.Value
                && Monitors.ElementAt(selectedItem - 1) != null)
            {
                Monitors.ElementAt(selectedItem - 1).Network = (int)SliderNetwork.Value;
                if (Monitors.ElementAt(selectedItem - 1).PlayStream != null)
                {
                    if (Monitors.ElementAt(selectedItem - 1).SourceProvider.MediaPlayer.IsPlaying().Equals(true))
                        Monitors.ElementAt(selectedItem - 1).SourceProvider.MediaPlayer.Pause();

                    Monitors.ElementAt(selectedItem - 1).SourceProvider.MediaPlayer.Play(
                                        Monitors.ElementAt(selectedItem - 1).PlayStream, Monitors.ElementAt(selectedItem - 1).Option);
                }
            }

        }


        
        private void AllRefresh()
        {
            //弃用
            //for (int i = 1; i < 4; ++i)
            //{

            //    if (Monitors.ElementAt(i - 1).PlayId < comboBoxSource.Items.Count && 
            //        configdata[Monitors.ElementAt(i - 1).PlayId].Type == "捕获设备")
            //        continue;   //跳过捕获设备

            //    if (Monitors.ElementAt(i - 1).SourceProvider.MediaPlayer.IsPlaying().Equals(true))
            //        Monitors.ElementAt(i - 1).SourceProvider.MediaPlayer.Pause();

            //        Monitors.ElementAt(i - 1).SourceProvider.Dispose();

            //    Monitors.ElementAt(i - 1).SourceProvider = new VlcVideoSourceProvider(this.Dispatcher);
            //    //Monitors.ElementAt(i - 1).SourceProvider.IsAlphaChannelEnabled = true;  //开alpha通道
            //    Monitors.ElementAt(i - 1).SourceProvider.CreatePlayer(libDirectory, "--file-logging", "-vvv", "--logfile=Logs.log");
            //    Monitors.ElementAt(i - 1).SourceProvider.MediaPlayer.Log += new EventHandler<VlcMediaPlayerLogEventArgs>(MediaPlayer_Log);
            //    Monitors.ElementAt(i - 1).SourceProvider.MediaPlayer.Manager.SetFullScreen(Monitors.ElementAt(i - 1).SourceProvider.MediaPlayer.Manager.CreateMediaPlayer(), true);
            //    //Monitors.ElementAt(i - 1).Volume = 0;
            //    Monitors.ElementAt(i - 1).SourceProvider.MediaPlayer.EncounteredError += new EventHandler<VlcMediaPlayerEncounteredErrorEventArgs>(MediaPlayer_ErrorEncountered);

            //    if (Monitors.ElementAt(i - 1).PlayStream != null)
            //    {
            //        //Monitors.ElementAt(selectedItem - 1).SourceProvider.MediaPlayer.ResetMedia();
                    
            //            Monitors.ElementAt(i - 1).SourceProvider.MediaPlayer.Play(
            //                Monitors.ElementAt(i - 1).PlayStream, Monitors.ElementAt(selectedItem - 1).Option);
            //    }
            //}
        }


        private void buttonMonitorRefresh_Click(object sender, RoutedEventArgs e)
        {

            Thread re = new Thread(AllRefresh); //线程安全
            re.IsBackground = true;
            re.Start();
            
        }

        #endregion

        #region 弹幕系统

        private Queue<DanmakuModel> _danmakuQueue = new Queue<DanmakuModel>();

        //private readonly StaticModel Static = new StaticModel();
        //private Thread releaseThread;
        
        // Multiple Danmu
        Queue<DanmakuLoader> danmuqueue = new Queue<DanmakuLoader>();

        //DanmakuLoader danmu = new DanmakuLoader();

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
                        try
                        {
                            if (danmaku.MsgType == MsgTypeEnum.Comment && enable_regex)
                            {
                                if (FilterRegex.IsMatch(danmaku.CommentText)) continue;

                            }
                        }
                        catch
                        {

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
                            catch
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

            if (!Properties.Settings.Default.SysTime || tabControl.SelectedIndex != 3) 
                CreateBitmapFromVisual();

        }

        bool screenSwitch = false;

        private void ScreenSwitch()
        {
            var myblue = new SolidColorBrush(Color.FromArgb(255, 1, 188, 225));
            if (screenSwitch)
            {
                //关闭公屏
                //TODO:淡出
                Close_DanmakuWindow();

                buttonScreenSwitch.Foreground = myblue;
                buttonScreenSwitch.Background = Brushes.White;
                buttonScreenSwitch.Content = "启动公屏";

                screenSwitch = false;
            }
            else
            {
                //TODO：淡入
                Open_DanmakuWindow();

                buttonScreenSwitch.Foreground = Brushes.White;
                buttonScreenSwitch.Background = myblue;
                buttonScreenSwitch.Content = "关闭公屏";

                screenSwitch = true;
            }
            //手动更新字幕机
            focaldephov.labelSubtitler.FontSize = SliderTextSize.Value;
            focaldephov.labelSubtitler.Width = focaldephov.Width * 2 / 3;
            focaldephov.labelSubtitler.Height = SliderTextSize.Value / 0.6;
            Canvas.SetLeft(focaldephov.labelSubtitler, SliderSubBazelDist.Value * focaldephov.Width);
            Canvas.SetTop(focaldephov.labelSubtitler, focaldephov.Height - SliderSubBazelDist.Value * focaldephov.Width);
            //UpperRight
            Canvas.SetTop(focaldephov.labelUpperRight, (SliderSubBazelDist.Value < 0.2 ? SliderSubBazelDist.Value : 0.2) * focaldephov.Height);
            Canvas.SetRight(focaldephov.labelUpperRight, (SliderSubBazelDist.Value < 0.2 ? SliderSubBazelDist.Value : 0.2) * focaldephov.Height + 10);
        }

        private void buttonScreenSwitch_Click(object sender, RoutedEventArgs e)
        {
            ScreenSwitch();
        }

        bool DanmakuSwitch = false;

        private async void ButtonDanmakuSwitch_Click(object sender, RoutedEventArgs e)
        {
            var myblue = new SolidColorBrush(Color.FromArgb(255, 1, 188, 225));
            if (DanmakuSwitch)
            {
                while (danmuqueue.Any())        //队列全部清空
                {
                    var danmul = danmuqueue.Dequeue();
                    danmul.Disconnect();
                }

                //队列不再deQueue, ProcDanmaku 不会再被调用。
                dispatcherTimerDanmaku.Stop();
                //清除弹幕池
                _danmakuQueue.Clear();
                DanmakuPool.Clear();
                //显性清除
                listBoxDanmaku.Items.Clear();

                buttonDanmakuSwitch.Foreground = myblue;
                buttonDanmakuSwitch.Background = Brushes.White;
                buttonDanmakuSwitch.Content = "连接弹幕";

                GridWordCloud.IsEnabled = false;
                if(WCOpacSlider.Value>0) switchWCOpac();

                DanmakuSwitch = false;

            }
            else
            {
                ////只有第一次启动时进行真正链接
                //if (!connected)
                //{
                buttonDanmakuSwitch.Content = "连接中";
                Queue<int> room_idq = new Queue<int>();
                foreach (ConfigItem cit in configdata)
                {
                    if (cit.Type == "B站" && cit.Bililive_roomid > 0)    //有效的B站房间号
                    {
                        //room_id = cit.Bililive_roomid;
                        //break;
                        bool flag = false;
                        for (int i = 0; i < room_idq.Count; ++i)
                            if (cit.Bililive_roomid == room_idq.ElementAt(i))
                                flag = true;    //防止重复
                        if (!flag)
                        {
                            room_idq.Enqueue(cit.Bililive_roomid);
                            if (checkBoxDanmuLink.IsChecked.Equals(true))   //是否只接收一个信号
                                break;
                        }           
                    }
                }
                try
                {
                    if (!room_idq.Any())
                    {
                        throw new Exception();
                    }
                    else
                    {

                        while (room_idq.Any())
                        {
                            DanmakuLoader danmul = new DanmakuLoader();
                            int room_id = room_idq.Dequeue();
                            var connectresult = await danmul.ConnectAsync(room_id);
                            var trytime = 0;
                            if (!connectresult && danmul.Error != null)// 如果连接不成功并且出错了
                            {
                                throw new WebException();
                            }

                            while (!connectresult && sender == null)
                            {
                                if (trytime > 3)
                                    break;
                                else
                                    trytime++;

                                await System.Threading.Tasks.Task.Delay(1000); // 稍等一下
                                connectresult = await danmul.ConnectAsync(room_id);
                            }

                            if (!connectresult)
                                throw new Exception();

                            danmul.ReceivedDanmaku += b_ReceivedDanmaku;
                            danmuqueue.Enqueue(danmul);

                        }

                        listBoxDanmaku.Items.Clear();

                        buttonDanmakuSwitch.Foreground = Brushes.White;
                        buttonDanmakuSwitch.Background = myblue;
                        buttonDanmakuSwitch.Content = "关闭连接";

                        if(!screenSwitch)       //关闭时打开
                            ScreenSwitch();

                        GridWordCloud.IsEnabled = true;
                        //FadeOutAnim(buttonWCOpac, WCOpacSlider);

                        DanmakuSwitch = true;

                        dispatcherTimerDanmaku.Start();

                    }
                }
                catch
                {
                    buttonDanmakuSwitch.Content = "连接失败";
                    await System.Threading.Tasks.Task.Delay(1000);
                    buttonDanmakuSwitch.Content = "连接弹幕";
                }


            }

        }

        const int IMAGE_DPI = 96;

        private void CreateBitmapFromVisual()
        {
            Visual target = focaldephov;
            
            if (target == null)
            {
                return;
            }

            Rect bounds = new Rect(new Point(focaldephov.Left, focaldephov.Top),
                new Size(focaldephov.Width, focaldephov.Height));

            //Rect bounds = VisualTreeHelper.GetDescendantBounds(target);

            RenderTargetBitmap renderTarget = new RenderTargetBitmap((Int32)bounds.Width, (Int32)bounds.Height, IMAGE_DPI, IMAGE_DPI, PixelFormats.Pbgra32);

            DrawingVisual visual = new DrawingVisual();

            using (DrawingContext context = visual.RenderOpen())
            {
                VisualBrush visualBrush = new VisualBrush(target);
                context.DrawRectangle(visualBrush, null, new Rect(new Point(), bounds.Size));
            }

            renderTarget.Render(visual);

            LiveRD.Source = renderTarget;

        }


        static bool AutoDanmaku = true;
        private void ButtonAutoDanmaku_Click(object sender, RoutedEventArgs e)
        {
            var myblue = new SolidColorBrush(Color.FromArgb(255, 1, 188, 225));
            if (!AutoDanmaku)
            {
                buttonAutoDanmaku.Foreground = Brushes.White;
                buttonAutoDanmaku.Background = myblue;
                buttonAutoDanmaku.Content = "自动弹幕";
            }
            else
            {
                buttonAutoDanmaku.Foreground = myblue;
                buttonAutoDanmaku.Background = Brushes.White;
                buttonAutoDanmaku.Content = "手动弹幕";
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
                DanmakuPool.Enqueue(new DanmakuItem(danmakuModel.UserID,danmakuModel.CommentText, danmakuModel.isAdmin, danmakuModel.UserName));

                //抽奖人
                ReceivedGifting?.Invoke(this, new GiftingReceivedArgs() { danmu = danmakuModel });

                if (wcCollecting)
                {
                    //认为是开始统计
                    var seg = new JiebaSegmenter();
                    var segs = seg.Cut(danmakuModel.CommentText);

                    ////使用一个小型字典减少查询的时间复杂度
                    //SortedDictionary<string, int> smalldic = new SortedDictionary<string, int>();
                    //foreach (string s in segs)
                    //{
                    //    if (smalldic.ContainsKey(s)) smalldic[s] += 1;
                    //    else smalldic.Add(s, 1);
                    //}
                    ////与大字典合并
                    //foreach (var d in smalldic.Keys)
                    //{
                    //    if (danmudic.ContainsKey(d)) danmudic[d] += smalldic[d];
                    //    else danmudic.Add(d, smalldic[d]);
                    //}

                    foreach(string s in segs)
                    {
                        if (danmudic.ContainsKey(s)) danmudic[s] += 1;
                        else danmudic.Add(s, 1);
                    }
                }
                
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
                    Properties.Settings.Default.StoreTime = EXPIRE_TIME;

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
        Storyboard WinShrink = new Storyboard();

        private void ButtonDanmakuEntry_Click(object sender, RoutedEventArgs e)
        {
            d_ok = false;
            var danmuEntry = new DanmakuEntry();
            //danmuEntry.Left = PrimaryScreen.WorkingArea.Width / 2;
            //danmuEntry.Top = PrimaryScreen.WorkingArea.Height / 2;
            danmuEntry.ShowDialog();

            if (d_ok == true)
            {
                if (RightCol.Width.Value.Equals(0))
                {
                    WinShrinkAction(RightCol_Last + 15);
                    SideBar();
                }
                //Hide_Monitor();
                selectItem(4);
                LabelDanmu.Visibility = Visibility.Visible;
            }

        }

        private void WinShrinkAction(double ToValue)
        {
            DoubleAnimation WinShrinkAnim = new DoubleAnimation()
            {
                From = this.Width,
                To = ToValue,
                Duration = TimeSpan.FromSeconds(1),
            };
            Storyboard.SetTarget(WinShrinkAnim, this);
            Storyboard.SetTargetProperty(WinShrink, new PropertyPath("(Window.Width)"));
            WinShrink.Children.Add(WinShrinkAnim);
            WinShrink.Completed += WinShrinkAnimation_Completed;
            WinShrink.Begin(this, HandoffBehavior.SnapshotAndReplace, true);

        }

        private void WinShrinkAnimation_Completed(object sender, EventArgs e)
        {
            double tmp_width = this.Width;
            WinShrink.Remove(this);
            this.Width = tmp_width;
        }

        private void ButtonDanmuSetting_Click(object sender, RoutedEventArgs e)
        {
            var blueshallow = new SolidColorBrush(Color.FromArgb(255, 117, 210, 246));
            if(this.Width<= RightCol.Width.Value + 30)
            {
                WinShrinkAction(RightCol.Width.Value + 537);
                tabControl.SelectedIndex = 4;           //转至设置选项卡
                buttonDanmuSetting.Background = blueshallow;
            }
            else if(tabControl.SelectedIndex == 4)
            {
                WinShrinkAction(RightCol.Width.Value + 15);
                buttonDanmuSetting.Background = blueback;
            }
            else
                tabControl.SelectedIndex = 4;           //转至设置选项卡
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
            listBoxDanmaku.MaxHeight = this.Height - 70;
        }

        //TODO: 以后写成一个list用于调用样式列表。

        FocalDepthHover focaldephov = new FocalDepthHover();

        private void Open_DanmakuWindow()
        {
            this.Topmost = true;
            if (sc != null && sc.IsLoaded)
                sc.Topmost = true;
            if(!focaldephov.IsActive)
                
            try
            {
                focaldephov.Show();
            }
            catch
            {
                focaldephov = new FocalDepthHover();
            }
            
            //focaldephov.Opacity = 1;
            //OpacSlider.Value = 1;

            DanmakuContentExp.IsExpanded = true;
            DanmuOverallExp.IsExpanded = true;
            DanmuStyleExp.IsExpanded = true;
            DanmuSetting.IsEnabled = true;

            FadeOutAnim(buttonWinTrans, OpacSlider);

            if (SliderSubTran.Value == 0)
                FadeOutAnim(buttonSubtitler, SliderSubTran);
            else
                focaldephov.labelSubtitler.Opacity = SliderSubTran.Value;

            GridSubtitler.IsEnabled = true;

            switch (Properties.Settings.Default.SizeMode)
            {
                case 0: focaldephov.WindowState = WindowState.Maximized; break;
                case 1:
                    focaldephov.WindowState = WindowState.Normal;
                    focaldephov.Width = 1920;
                    focaldephov.Height = 1080;
                    break;
                case 2:
                    focaldephov.WindowState = WindowState.Normal;
                    focaldephov.Width = 1280;
                    focaldephov.Height = 720;
                    break;
            }
        }

        private void SizeFullScreen_Selected(object sender, RoutedEventArgs e)
        {
            if (focaldephov != null && focaldephov.IsLoaded)
            {
                focaldephov.WindowState = WindowState.Maximized;
            }
            Properties.Settings.Default.SizeMode = 0;
        }

        private void Size1080P_Selected(object sender, RoutedEventArgs e)
        {
            if (focaldephov != null && focaldephov.IsLoaded)
            {
                focaldephov.WindowState = WindowState.Normal;
                focaldephov.Width = 1920;
                focaldephov.Height = 1080;
            }
            Properties.Settings.Default.SizeMode = 1;
        }

        private void Size720P_Selected(object sender, RoutedEventArgs e)
        {
            if (focaldephov != null && focaldephov.IsLoaded)
            {
                focaldephov.WindowState = WindowState.Normal;
                focaldephov.Width = 1280;
                focaldephov.Height = 720;
            }
            Properties.Settings.Default.SizeMode = 2;
        }

        private void Close_DanmakuWindow()
        {
            //TODO:淡出计时
            //focaldephov.Opacity = 0;
            //OpacSlider.Value = 0;

            DanmakuContentExp.IsExpanded = false;
            DanmuOverallExp.IsExpanded = false;
            DanmuStyleExp.IsExpanded = false;
            DanmuSetting.IsEnabled = false;

            FadeOutAnim(buttonWinTrans, OpacSlider);
            this.Topmost = false;

            if (!Properties.Settings.Default.SubAlways)
            {
                FadeOutAnim(buttonSubtitler, SliderSubTran);
                GridSubtitler.IsEnabled = false;
            }
        }

        public static event ReceivedDanmuEvt ReceivedDanmu;
        public static event GiftingReceivedEvt ReceivedGifting;

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
            if (!RightCol.Width.Value.Equals(0))
            {
                //此时已经被初始化
                //关闭所有媒体。
                if (focaldephov != null)
                {
                    focaldephov.BackImg.Source = null;
                    focaldephov.Close();
                }
                
                for (int i = 1; i < 4; ++i)
                {
                    //防止访问冲突
                    if (Monitors.ElementAt(i - 1).SourceProvider!= null)
                    {
                        if (Monitors.ElementAt(i - 1).SourceProvider.MediaPlayer.IsPlaying().Equals(true))
                            Monitors.ElementAt(i - 1).SourceProvider.MediaPlayer.Pause();
                        Monitors.ElementAt(i - 1).SourceProvider.Dispose();
                    }
                        
                    //if (checkBoxLowMoni.IsChecked.Equals(true))
                    //{
                    //    Monitors.ElementAt(i).TstRtmp.Stop();
                    //    Monitors.ElementAt(i).ThPlayer = null;
                    //}
                }
                Monitors.Clear();
            }
            Properties.Settings.Default.Save();
            if (sc != null && sc.IsLoaded)
                sc.Close(); //手动关闭
            if (sourceProvider_fore != null)
            {
                if (sourceProvider_fore.MediaPlayer.IsPlaying().Equals(true))
                    sourceProvider_fore.MediaPlayer.Stop();
                sourceProvider_fore.Dispose();
            }
            GC.Collect();
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

            //focaldephov.Background = new SolidColorBrush(BackColorPicker.SelectedColor);
            focaldephov.WinBack.Color = BackColorPicker.SelectedColor;
            Properties.Settings.Default.WinBack = BackColorPicker.SelectedColor;

        }

        private void OpacSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            focaldephov.Opacity = OpacSlider.Value;
            Properties.Settings.Default.WinOpac = OpacSlider.Value;
        }

        private void ForeColorPicker_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color> e)
        {
            (focaldephov.FindResource("BubbleFore") as SolidColorBrush).Color = ForeColorPicker.SelectedColor;
            //FocalDepthHover.ForeColor = ForeColorPicker.SelectedColor;
            Properties.Settings.Default.DanmuFore = ForeColorPicker.SelectedColor;
        }

        Binding bing_sub;
        private void BackNone_Selected(object sender, RoutedEventArgs e)
        {
            bing_sub = new Binding();
            focaldephov.BackImg.SetBinding(Image.SourceProperty, bing_sub);
            focaldephov.TransitionImg.SetBinding(Image.SourceProperty, bing_sub);   //过渡层清空
        }

        private void BackLive_Selected(object sender, RoutedEventArgs e)
        {
            selectItem(selectedItem);
        }

        private void BackLive_Unselected(object sender, RoutedEventArgs e)
        {
            
        }

        private void BackPic_Selected(object sender, RoutedEventArgs e)
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
                ComboBoxBackImg.SelectedIndex = 0;
                //ComboBoxBackImg.SelectedValue = BackNone;
            }
            else
            {
                focaldephov.BackImg.Source = new ImageSourceConverter().ConvertFromString(openFileDialog.FileName) as ImageSource;
            }
        }

        string PlayStream_local;
        VlcVideoSourceProvider sourceProvider_local;
        private void BackVid_Selected(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog();
            openFileDialog.Title = "选择前景视频";
            openFileDialog.Filter = "mp4|*.mp4|mov|*.mov";
            openFileDialog.FileName = string.Empty;
            openFileDialog.FilterIndex = 1;
            openFileDialog.RestoreDirectory = true;
            openFileDialog.DefaultExt = "mp4";
            System.Windows.Forms.DialogResult result = openFileDialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.Cancel)
            {   //这BUG无伤大雅 不修了
                ComboBoxBackImg.SelectedIndex = 0;
                //ComboBoxBackImg.SelectedValue = BackNone;
            }
            else
            {
                //focaldephov.BackImg.Source = new ImageSourceConverter().ConvertFromString(openFileDialog.FileName) as ImageSource;
                PlayStream_local = openFileDialog.FileName;
                bing_sub = new Binding();
                //var currentAssembly = Assembly.GetEntryAssembly();
                //var currentDirectory = new FileInfo(currentAssembly.Location).DirectoryName;
                //// Default installation path of VideoLAN.LibVLC.Windows
                //var libDirectory = new DirectoryInfo(System.IO.Path.Combine(currentDirectory, "libvlc\\" + (IntPtr.Size == 4 ? "win-x86" : "win-x64")));

                this.sourceProvider_local = new VlcVideoSourceProvider(this.Dispatcher);
                this.sourceProvider_local.CreatePlayer(libDirectory, "--file-logging", "-vvv", "--logfile=Logs.log");
                var mediaOptions = new[]
                {"input-repeat=65535"};     //近乎无穷的循环

                this.sourceProvider_local.MediaPlayer.Play(new Uri(PlayStream_local), mediaOptions);
                this.sourceProvider_local.MediaPlayer.Log += new EventHandler<VlcMediaPlayerLogEventArgs>(MediaPlayer_Log);
                this.sourceProvider_local.MediaPlayer.Manager.SetFullScreen(this.sourceProvider_local.MediaPlayer.Manager.CreateMediaPlayer(), true);
                this.sourceProvider_local.MediaPlayer.Audio.IsMute = true;    //这个版本中被静音
                                                                              //音量接口：this.sourceProvider_local.MediaPlayer.Audio.Volume，本版本暂时不用
                this.sourceProvider_local.MediaPlayer.EncounteredError += new EventHandler<VlcMediaPlayerEncounteredErrorEventArgs>(MediaPlayer_ErrorEncountered);

                //bing_sub = new Binding();
                bing_sub.Source = sourceProvider_local;
                bing_sub.Path = new PropertyPath("VideoSource");
                //输出图片
                focaldephov.BackImg.SetBinding(Image.SourceProperty, bing_sub);
            }
        }


        private void BackVid_Unselected(object sender, RoutedEventArgs e)
        {
            sourceProvider_local.Dispose();
        }

        private void OpacSliderFore_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // focaldephov.GridCanvas.Opacity = OpacSliderFore.Value;
            focaldephov.GridDanmu.Opacity = OpacSliderFore.Value;
            Properties.Settings.Default.DanmuOpac = OpacSlider.Value;
        }

        Binding bing_fore;

        private void ForeNone_Selected(object sender, RoutedEventArgs e)
        {
            bing_fore = new Binding();
            focaldephov.ForeImg.SetBinding(Image.SourceProperty, bing_fore);
        }

        private void ForePic_Selected(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog();
            openFileDialog.Title = "选择背景图像";
            openFileDialog.Filter = "jpg|*.jpg|png|*.png|jpeg|*.jpeg";
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

        public VlcVideoSourceProvider sourceProvider_fore;
        public static int foreVol = 0;
        Binding bing_sub_fore;

        private void ForeVid_Selected(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog();
            openFileDialog.Title = "选择背景视频";
            openFileDialog.Filter = "mp4|*.mp4|mov|*.mov";
            openFileDialog.FileName = string.Empty;
            openFileDialog.FilterIndex = 1;
            openFileDialog.RestoreDirectory = true;
            openFileDialog.DefaultExt = "mp4";
            System.Windows.Forms.DialogResult result = openFileDialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.Cancel)
            {   //这BUG无伤大雅 不修了
                ComboBoxForeImg.SelectedIndex = 0;
                //ComboBoxBackImg.SelectedValue = BackNone;
            }
            else
            {
                //focaldephov.BackImg.Source = new ImageSourceConverter().ConvertFromString(openFileDialog.FileName) as ImageSource;
                string PlayStream = openFileDialog.FileName;
                this.sourceProvider_fore = new VlcVideoSourceProvider(this.Dispatcher);
                this.sourceProvider_fore.CreatePlayer(libDirectory, "--file-logging", "-vvv", "--logfile=Logs.log");
                var mediaOptions = new[]
                {"input-repeat=65535"};

                this.sourceProvider_fore.MediaPlayer.EndReached += MediaPlayer_EndReached_Fore;
                this.sourceProvider_fore.MediaPlayer.Play(new Uri(PlayStream), mediaOptions);
                this.sourceProvider_fore.MediaPlayer.Log += new EventHandler<VlcMediaPlayerLogEventArgs>(MediaPlayer_Log);
                this.sourceProvider_fore.MediaPlayer.Manager.SetFullScreen(this.sourceProvider_fore.MediaPlayer.Manager.CreateMediaPlayer(), true);
                //this.sourceProvider_fore.MediaPlayer.Audio.IsMute = true;    //这个版本中被静音
                //音量接口：this.sourceProvider_fore.MediaPlayer.Audio.Volume，本版本暂时不用
                this.sourceProvider_fore.MediaPlayer.Audio.Volume = foreVol;
                this.sourceProvider_fore.MediaPlayer.EncounteredError += new EventHandler<VlcMediaPlayerEncounteredErrorEventArgs>(MediaPlayer_ErrorEncountered);

                bing_sub_fore = new Binding();
                bing_sub_fore.Source = sourceProvider_fore;
                bing_sub_fore.Path = new PropertyPath("VideoSource");
                //输出图片
                focaldephov.BackImg.SetBinding(Image.SourceProperty, bing_sub_fore);
            }
        }

        private void MediaPlayer_EndReached_Fore(object sender, VlcMediaPlayerEndReachedEventArgs e)
        {
            this.sourceProvider_fore.MediaPlayer.Time = 0;
            this.sourceProvider_fore.MediaPlayer.Play();
        }

        private void ForeVid_Unselected(object sender, RoutedEventArgs e)
        {
            if (sourceProvider_fore != null)
                sourceProvider_fore.Dispose();
        }

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

        private void WCOpacFadeOut_Storyboard_Remove(object sender, EventArgs e)
        {
            Fade_Storyboard_Remove(buttonWCOpac, WCOpacSlider);
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

            FullFadeOut.Duration = TimeSpan.FromSeconds(Properties.Settings.Default.TranSec);//或许考虑一个其他量

            //Storyboard.SetTarget(FullFadeOut, SliderStoreSec);
            Storyboard.SetTargetProperty(FullFadeOut, new PropertyPath("(Slider.Value)"));

            FullFadeOut_sb.Children.Add(FullFadeOut);
            if (sends == OpacSlider)
                FullFadeOut_sb.Completed += OpacFadeOut_Storyboard_Remove;
            else if(sends == OpacSliderFore)
                FullFadeOut_sb.Completed += DanmuFadeOut_Storyboard_Remove;
            else if(sends == WCOpacSlider)
                FullFadeOut_sb.Completed += WCOpacFadeOut_Storyboard_Remove;
            else
                FullFadeOut_sb.Completed += SubtitlerFadeOut_sb_Completed;

            FullFadeOut_sb.Begin(sends, HandoffBehavior.SnapshotAndReplace, true);

            sendb.IsEnabled = false;
        }

        private void SubtitlerFadeOut_sb_Completed(object sender, EventArgs e)
        {
            Fade_Storyboard_Remove_slider(buttonSubtitler, SliderSubTran);
            
            if (AudioStop) {
                AudioStop = false;
                ForeRealStop();
            } 
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

        private void Fade_Storyboard_Remove_slider(object senderbutton, object senderslider)
        {
            var sendb = senderbutton as Button;
            var sends = senderslider as Slider;

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

            FullFadeOut.Duration = TimeSpan.FromSeconds(Properties.Settings.Default.TranSec);//或许考虑一个其他量

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
            FocalDepthHover.SettingModified = true;
            focaldephov.CornerRefreshTimer.Stop();
        }

        private void DanmuBubble_Selected(object sender, RoutedEventArgs e)
        {
            FocalDepthHover.DM_Style = FocalDepthHover.DanmuStyle.Bubble;
            FocalDepthHover.SettingModified = true;
            focaldephov.CornerRefreshTimer.Stop();
        }

        private void DanmuBubbleFloat_Selected(object sender, RoutedEventArgs e)
        {
            FocalDepthHover.DM_Style = FocalDepthHover.DanmuStyle.BubbleFloat;
            FocalDepthHover.SettingModified = true;
            focaldephov.CornerRefreshTimer.Stop();
        }

        private void DanmuBubbleCorner_Selected(object sender, RoutedEventArgs e)
        {
            FocalDepthHover.DM_Style = FocalDepthHover.DanmuStyle.BubbleCorner;
            FocalDepthHover.SettingModified = true;
            focaldephov.CornerRefreshTimer.Start();
        }

        private void DanmuBottomBar_Selected(object sender, RoutedEventArgs e)
        {
            FocalDepthHover.DM_Style = FocalDepthHover.DanmuStyle.BottomBar;
            FocalDepthHover.SettingModified = true;
            focaldephov.CornerRefreshTimer.Stop();
        }

        private void DanmuBottomBarWithUserName_Selected(object sender, RoutedEventArgs e)
        {
            FocalDepthHover.DM_Style = FocalDepthHover.DanmuStyle.BottomBarWithUserName;
            FocalDepthHover.SettingModified = true;
            focaldephov.CornerRefreshTimer.Stop();

        }

        private void SliderHovTime_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            FocalDepthHover.HOVER_TIME = SliderHovTime.Value;
            FocalDepthHover.SettingModified = true;
            Properties.Settings.Default.HoverTime = SliderHovTime.Value;
        }

        private void SliderTextSize_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            FocalDepthHover.FocalPt_inSize = SliderTextSize.Value;
            FocalDepthHover.SettingModified = true;
            Properties.Settings.Default.MaxFontSize = SliderTextSize.Value;
            if (focaldephov != null && focaldephov.IsLoaded)
            {
                focaldephov.labelSubtitler.FontSize = SliderTextSize.Value;
                //focaldephov.labelSubtitler.Width = focaldephov.Width * 2 / 3;
                focaldephov.labelSubtitler.Height = SliderTextSize.Value / 0.6;
                focaldephov.labelUpperRight.FontSize = SliderTextSize.Value;
            }

        }

        private void SliderLayer_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            FocalDepthHover.LAYER_NUM = (int)SliderLayer.Value;
            FocalDepthHover.SettingModified = true;
            Properties.Settings.Default.LayerNum = (int)SliderLayer.Value;

        }

        private void SliderBlur_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            FocalDepthHover.BLUR_MAX = SliderBlur.Value;
            FocalDepthHover.SettingModified = true;
            Properties.Settings.Default.MaxBlur = SliderBlur.Value;
           
        }

        private void SliderFactor_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            FocalDepthHover.DECR_FAC = SliderFactor.Value;
            FocalDepthHover.SettingModified = true;
            Properties.Settings.Default.ScaleFac = SliderFactor.Value;
        }

        private void SliderRatio_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            FocalDepthHover.INIT_TOP = SliderRatio.Value;
            FocalDepthHover.SettingModified = true;
            Properties.Settings.Default.InitTop = SliderRatio.Value;


        }

        // 关闭接口
        //private void ButtonFromFront_Click(object sender, RoutedEventArgs e)
        //{
        //    if (FocalDepthHover.From_Front)
        //    {
        //        ButtonFromFront.Background = bluefore;
        //        ButtonFromFront.BorderBrush = blueback;
        //        FocalDepthHover.From_Front = false;
        //    }
        //    else
        //    {
        //        ButtonFromFront.Background = blueback;
        //        ButtonFromFront.BorderBrush = bluefore;
        //        FocalDepthHover.From_Front = true;
        //    }
        //    FocalDepthHover.SettingModified = true;
        //}

        private void ComboBoxFont_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var FontStr = ComboBoxFont.SelectedValue.ToString().Replace("System.Windows.Controls.ComboBoxItem: ", "");
            FocalDepthHover.ForeFont = 
                new FontFamily(FontStr);
            FocalDepthHover.SettingModified = true;
            Properties.Settings.Default.ForeFont = FontStr;
            if (focaldephov != null && focaldephov.IsLoaded)
            {
                focaldephov.labelSubtitler.FontFamily = new FontFamily(FontStr);
                focaldephov.labelUpperRight.FontFamily = new FontFamily(FontStr);
            }
        }

        private void ColorComboBoxBubble_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color> e)
        {
            (focaldephov.FindResource("BubbleBack") as SolidColorBrush).Color 
                = ColorComboBoxBubble.SelectedColor;
            Properties.Settings.Default.BubbleColor = ColorComboBoxBubble.SelectedColor;
            
        }

        private void TextBoxRegex_TextChanged(object sender, TextChangedEventArgs e)
        {
            //改变按钮态
            buttonRegex.Background = bluefore;
            buttonRegex.Foreground = blueback;
            enable_regex = false;
            
        }

        private void ButtonRegex_Click(object sender, RoutedEventArgs e)
        {
            if (enable_regex)
            {
                buttonRegex.Background = bluefore;
                buttonRegex.Foreground = blueback;
                enable_regex = false;
            }
            else {
                buttonRegex.Background = blueback;
                buttonRegex.Foreground = bluefore;
                enable_regex = true;
                //提交正则表达式
                regex = textBoxRegex.Text;
                FilterRegex = new Regex(regex);
                Properties.Settings.Default.Regex = textBoxRegex.Text;
            }
        }


        #endregion

        private void ListSelecChange(int ind)
        {
            if (ind<= listBoxDanmaku.Items.Count-1)
            {
                if ((listBoxDanmaku.Items[ind] as ListBoxItem).IsSelected)
                    listBoxDanmaku.SelectedItems.Remove(listBoxDanmaku.Items[ind]);
                else
                    listBoxDanmaku.SelectedItems.Add(listBoxDanmaku.Items[ind]);
            }
        }

        private void StackPanelRightCol_KeyDown(object sender, KeyEventArgs e)
        {
            StackPanelRightCol.Focus();
            //侧栏快捷键
            switch (e.Key)
            {
                case Key.Q: HardCut(1); break;
                case Key.W: HardCut(2); break;
                case Key.E: HardCut(3); break;
                case Key.R: HardCut(4); break;
                case Key.A: transition(1); break;
                case Key.S: transition(2); break;
                case Key.D: transition(3); break;
                case Key.F: transition(4); break;
                case Key.Z: transition_manual(1); break;
                case Key.X: transition_manual(2); break;
                case Key.C: transition_manual(3); break;
                case Key.V: transition_manual(4); break;
                case Key.H:
                    if (TranEffect != TranEffects.SideBySide)
                        TranEffect = TranEffects.SideBySide;
                    TranEffectChanged();
                    break;
                case Key.J:
                    if (TranEffect != TranEffects.LRSplit)
                        TranEffect = TranEffects.LRSplit;
                    TranEffectChanged();
                    break;
                case Key.K:
                    if (TranEffect != TranEffects.UDSplit)
                        TranEffect = TranEffects.UDSplit;
                    TranEffectChanged();
                    break;
                case Key.L:
                    if (TranEffect != TranEffects.SWindow)
                        TranEffect = TranEffects.SWindow;
                    TranEffectChanged();
                    break;
                case Key.D1: ListSelecChange(0); break;
                case Key.D2: ListSelecChange(1); break;
                case Key.D3: ListSelecChange(2); break;
                case Key.D4: ListSelecChange(3); break;
                case Key.D5: ListSelecChange(4); break;
                case Key.D6: ListSelecChange(5); break;
                case Key.D7: ListSelecChange(6); break;
                case Key.D8: ListSelecChange(7); break;
                case Key.D9: ListSelecChange(8); break;
                case Key.N: LiveSolo(); break;
                case Key.M:
                    SoundControl.MainChange = true;
                    switch (selectedItem)
                    {
                        case 1: 
                            Monitors.ElementAt(selectedItem - 1).Volume = 0;
                            ProgressLU.Value = 0;
                            if(sc!=null&&sc.IsLoaded) sc.Slider1.Value = 0;
                            break;
                        case 2:
                            Monitors.ElementAt(selectedItem - 1).Volume = 0;
                            ProgressRU.Value = 0;
                            if (sc != null && sc.IsLoaded) sc.Slider2.Value = 0;
                            break;
                        case 3:
                            Monitors.ElementAt(selectedItem - 1).Volume = 0;
                            ProgressLD.Value = 0;
                            if (sc != null && sc.IsLoaded) sc.Slider3.Value = 0;
                            break;
                    }
                    SoundControl.MainChange = false;
                    break;
                case Key.Down:
                    SoundControl.MainChange = true;
                    switch (selectedItem)
                    {
                        case 1:
                            Monitors.ElementAt(selectedItem - 1).Volume -= 2;
                            ProgressLU.Value = Monitors.ElementAt(selectedItem - 1).Volume;
                            if (sc != null && sc.IsLoaded) sc.Slider1.Value = Monitors.ElementAt(selectedItem - 1).Volume;
                            break;
                        case 2:
                            Monitors.ElementAt(selectedItem - 1).Volume -= 2;
                            ProgressRU.Value = Monitors.ElementAt(selectedItem - 1).Volume;
                            if (sc != null && sc.IsLoaded) sc.Slider2.Value = Monitors.ElementAt(selectedItem - 1).Volume;
                            break;
                        case 3:
                            Monitors.ElementAt(selectedItem - 1).Volume -= 2;
                            ProgressLD.Value = Monitors.ElementAt(selectedItem - 1).Volume;
                            if (sc != null && sc.IsLoaded) sc.Slider3.Value = Monitors.ElementAt(selectedItem - 1).Volume;
                            break;
                    }
                    SoundControl.MainChange = false;
                    break;
                case Key.Up:
                    SoundControl.MainChange = true;
                    switch (selectedItem)
                    {
                        case 1:
                            Monitors.ElementAt(selectedItem - 1).Volume += 2;
                            ProgressLU.Value = Monitors.ElementAt(selectedItem - 1).Volume;
                            if (sc != null && sc.IsLoaded) sc.Slider1.Value = Monitors.ElementAt(selectedItem - 1).Volume;
                            break;
                        case 2:
                            Monitors.ElementAt(selectedItem - 1).Volume += 2;
                            ProgressRU.Value = Monitors.ElementAt(selectedItem - 1).Volume;
                            if (sc != null && sc.IsLoaded) sc.Slider2.Value = Monitors.ElementAt(selectedItem - 1).Volume;
                            break;
                        case 3:
                            Monitors.ElementAt(selectedItem - 1).Volume += 2;
                            ProgressLD.Value = Monitors.ElementAt(selectedItem - 1).Volume;
                            if (sc != null && sc.IsLoaded) sc.Slider3.Value = Monitors.ElementAt(selectedItem - 1).Volume;
                            break;
                    }
                    SoundControl.MainChange = false;
                    break;
                case Key.OemMinus:
                    if(ProgressTran.Visibility == Visibility.Visible)
                    {
                        ProgressTran.Value -= 0.02;
                    }
                    break;
                case Key.OemPlus:
                    if (ProgressTran.Visibility == Visibility.Visible)
                    {
                        ProgressTran.Value += 0.02;
                    }
                    break;
                case Key.OemCloseBrackets: NextSub(); break;
                case Key.OemOpenBrackets: PrevSub(); break;
                case Key.OemBackslash: ClearHistorySub(); break;
                case Key.I: ForePlay(); break;
                case Key.O: ForeStop(); break;
                case Key.P: ForeNextFile(); break;
            }
        }


        #region 字幕机

        private void buttonSurfaceDial_Click(object sender, RoutedEventArgs e)
        {
            if(!radialControllerInit)
                InitializeController();
            if (radialController.Menu.Items.Contains(rcsub))
            {
                radialController.Menu.SelectMenuItem(rcsub);
            }
        }

        private void checkBoxSurfaceDial_Checked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.SurfaceDial = true;
        }

        //仍然有bug 换行问题

        private void NextSub()
        {
            string nextstr = textboxNextSub.Text;
            if (!((textboxCurrentSub.Text.Length == 0 || textboxCurrentSub.Text==Environment.NewLine) &&
                    (nextstr.Length == 0 || nextstr.Replace(Environment.NewLine,"").Length==0)))
            {
                RowPrevSub.Height = new GridLength(0.5, GridUnitType.Star);
                RowNextSub.Height = new GridLength(1, GridUnitType.Star);
                textboxPrevSub.AppendText(textboxCurrentSub.Text);
                textboxCurrentSub.Text = textboxNextSub.GetLineText(0);
                if (textboxNextSub.Text.IndexOf(Environment.NewLine) == -1)
                    textboxNextSub.Clear();
                else
                    textboxNextSub.Text =
                        textboxNextSub.Text.Remove(0,
                        textboxNextSub.Text.IndexOf(Environment.NewLine) + Environment.NewLine.Length);
                textboxPrevSub.ScrollToEnd();
                textboxNextSub.ScrollToHome();
            }
        }

        private void PrevSub()
        {
            string prevstr = textboxPrevSub.Text;
            if (!((textboxCurrentSub.Text.Length == 0 || textboxCurrentSub.Text == Environment.NewLine) &&
                    (prevstr.Length == 0 || prevstr.Replace(Environment.NewLine,"").Length==0)))
            {
                RowPrevSub.Height = new GridLength(0.5, GridUnitType.Star);
                RowNextSub.Height = new GridLength(1, GridUnitType.Star);
                textboxNextSub.Text = textboxCurrentSub.Text + (textboxCurrentSub.Text.IndexOf(Environment.NewLine)==-1?Environment.NewLine.ToString():"") + textboxNextSub.Text;
                textboxCurrentSub.Text = textboxPrevSub.GetLineText(textboxPrevSub.GetLastVisibleLineIndex());
                if (textboxPrevSub.Text.IndexOf(Environment.NewLine) == -1)
                    textboxPrevSub.Clear();
                else
                    textboxPrevSub.Text = textboxPrevSub.Text.Remove(textboxPrevSub.Text.LastIndexOf(Environment.NewLine));
                textboxPrevSub.ScrollToEnd();
                textboxNextSub.ScrollToHome();
            }
            else
            {
                RowPrevSub.Height = new GridLength(0,GridUnitType.Pixel);
            }
        }

        private void ClearHistorySub()
        {
            textboxPrevSub.Clear();
            textboxCurrentSub.Clear();
            RowPrevSub.Height = new GridLength(0, GridUnitType.Pixel);
        }

        private void buttonNext_Click(object sender, RoutedEventArgs e)
        {
            NextSub();
        }

        private void buttonPrev_Click(object sender, RoutedEventArgs e)
        {
            PrevSub();
        }

        private void buttonClearHistory_Click(object sender, RoutedEventArgs e)
        {
            ClearHistorySub();
        }

        private void Border_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.OemCloseBrackets: NextSub(); break;
                case Key.OemOpenBrackets: PrevSub(); break;
                case Key.OemBackslash: ClearHistorySub(); break;
                case Key.I: ForePlay(); break;
                case Key.O: ForeStop(); break;
                case Key.P: ForeNextFile(); break;
            }
        }

        private void buttonSubtitler_Click(object sender, RoutedEventArgs e)
        {
            FadeOutAnim(buttonSubtitler, SliderSubTran);
        }

        private void textboxCurrentSub_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (focaldephov != null && focaldephov.IsLoaded)
                focaldephov.labelSubtitler.Content = textboxCurrentSub.Text;
        }

        private void SliderSubTran_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (focaldephov != null && focaldephov.IsLoaded)
                focaldephov.labelSubtitler.Opacity = SliderSubTran.Value;

            if(sc!=null && sc.IsLoaded && 
                Properties.Settings.Default.SmartPA == 3 && AudioStop)
            {
                // Follow the Slider Value.

                sc.Slider4.Value = SliderSubTran.Value * sc_AudioChannel_ori;
                if (sc.Slider4.Value == 0)
                {
                    sc.Slider4.Value = sc_AudioChannel_ori;
                    SoundControl.soundControllers.ElementAt(3).On = false;
                }

                if (MSC != null)
                    MSC.Invoke(this, new MainSelecChangedArgs() { });
            }
        }

        private void SliderSubBazelDist_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (focaldephov != null && focaldephov.IsLoaded)
            {
                Canvas.SetLeft(focaldephov.labelSubtitler, SliderSubBazelDist.Value * focaldephov.Width);
                Canvas.SetTop(focaldephov.labelSubtitler, focaldephov.Height - SliderSubBazelDist.Value * focaldephov.Width);
                //UpperRight
                Canvas.SetTop(focaldephov.labelUpperRight, (SliderSubBazelDist.Value < 0.2 ? SliderSubBazelDist.Value : 0.2) * focaldephov.Height);
                Canvas.SetRight(focaldephov.labelUpperRight, (SliderSubBazelDist.Value < 0.2 ? SliderSubBazelDist.Value : 0.2) * focaldephov.Height + 10);
            }
            if(this.IsLoaded)
                Properties.Settings.Default.SubBasel = SliderSubBazelDist.Value;
        }

        private void checkBoxSubtitleAlways_Checked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.SubAlways = true;
            FadeOutAnim(buttonSubtitler, SliderSubTran);
            GridSubtitler.IsEnabled = true;
        }

        private void checkBoxSubtitleAlways_Unchecked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.SubAlways = false;
        }

        string m_Dir;

        private void buttonOpenFolder_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog m_Dialog = new System.Windows.Forms.FolderBrowserDialog();
            System.Windows.Forms.DialogResult result = m_Dialog.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.Cancel)
            {
                return;
            }
            m_Dir = m_Dialog.SelectedPath.Trim();

            fis.Clear();

            foreach (string appe in supportedFormat)
            {
                string[] files = System.IO.Directory.GetFiles(m_Dir, appe, SearchOption.AllDirectories);
                foreach (string s in files)
                {
                    System.IO.FileInfo fi = null;
                    try
                    {
                        fi = new System.IO.FileInfo(s);
                    }
                    catch (System.IO.FileNotFoundException ex)
                    {
                        Console.WriteLine(ex.Message);
                        continue;
                    }
                    //this.show.Text += fi.Name;
                    fis.Add(fi);
                }
            }
            fis.OrderBy(u => u.Name);
            currentIndex = -1;

            FileSearchBox.IsEnabled = true;
            buttonNextFile.IsEnabled = true;
        }

        private void UpdateFis()
        {
            fis.Clear();
            foreach (string appe in supportedFormat)
            {
                string[] files = System.IO.Directory.GetFiles(m_Dir, appe, SearchOption.AllDirectories);
                foreach (string s in files)
                {
                    System.IO.FileInfo fi = null;
                    try
                    {
                        fi = new System.IO.FileInfo(s);
                    }
                    catch (System.IO.FileNotFoundException ex)
                    {
                        Console.WriteLine(ex.Message);
                        continue;
                    }
                    //this.show.Text += fi.Name;
                    fis.Add(fi);
                }
            }
            fis.OrderBy(u => u.Name);
            currentIndex = -1;
        }

        private FileInfo FindFile(string[] appendix)
        {
            FileInfo output = null;
            string[] files;

            foreach (string appe in appendix)
            {
                files = System.IO.Directory.GetFiles(m_Dir, appe, SearchOption.AllDirectories);
                foreach (string s in files)
                {
                    System.IO.FileInfo fi = null;
                    try
                    {
                        fi = new System.IO.FileInfo(s);
                    }
                    catch (System.IO.FileNotFoundException ex)
                    {
                        Console.WriteLine(ex.Message);
                        continue;
                    }
                    //this.show.Text += fi.Name;
                    string target = FileSearchBox.Text;
                    if ((!target.Contains('.') && fi.Name.Contains(target))||
                        target.Contains('.') && fi.Name.Contains(target.Split('.')[0]))
                    {
                        //TextBlockStatus.Text = "载入：" + fi.Name;
                        output = fi;
                        break;
                    }
                }
                if (output != null)
                {
                    currentIndex = -1;
                    UpdateCurrentInd();
                    break;
                }
            }
            return output;
            
        }

        struct lrcline {
            public string lrc;
            public int startTime;              // 毫秒数

            public lrcline(int st,string lrcstr)
            {
                lrc = lrcstr;
                startTime = st;
            }
        };

        //lrc播放队列
        Queue<lrcline> lrcque = new Queue<lrcline>();
        Queue<int> timerque = new Queue<int>();

        /// <summary>
        /// 将时间字符串转换成int类型
        /// </summary>
        /// <param name="timeStr"></param>
        /// <returns></returns>
        private int strToDouble(string timeStr)
        {
            //输入timeStr时间字符串格式如  01:02.50                      
            string[] s = timeStr.Split(':');//分:秒
            string[] ss = s[1].Split('.');//秒.毫秒
            double min, sec,mss;
            min = Convert.ToDouble(s[0]);
            sec = Convert.ToDouble(s[1]);
            mss = Convert.ToDouble(ss[1]);
            return (int)((min * 60 + sec) * 1000 + mss);
        }

        private void LoadLrc(FileInfo fi)
        {
            
            if (fi!=null&&System.IO.File.Exists(fi.FullName))
            {
                lrcque.Clear();
                timerque.Clear();
                textboxPrevSub.Clear();
                textboxNextSub.Clear();
                try
                {
                    string[] lines = System.IO.File.ReadAllLines(fi.FullName, Encoding.Default);
                    foreach (string line in lines)
                    {
                        //解析歌词文本的每行
                        //parserLine(line);
                        if (line.StartsWith("[ti:"))
                        {
                            try
                            {
                                textboxPrevSub.AppendText("标题：" + line.Substring(4, line.Length - 5) + Environment.NewLine);
                            }
                            catch
                            {
                                textboxPrevSub.AppendText("标题：未知" + Environment.NewLine);
                            }
                        }
                        //取得歌手名
                        else if (line.StartsWith("[ar:"))
                        {
                            try
                            {
                                textboxPrevSub.AppendText("歌手：" + line.Substring(4, line.Length - 5) + Environment.NewLine);
                            }
                            catch
                            {
                                textboxPrevSub.AppendText("歌手：未知" + Environment.NewLine);
                            }
                        }
                        else if (line.StartsWith("[al:"))
                        {
                            try
                            {
                                textboxPrevSub.AppendText("专辑：" + line.Substring(4, line.Length - 5) + Environment.NewLine);
                            }
                            catch
                            {
                                textboxPrevSub.AppendText("专辑：未知" + Environment.NewLine);
                            }
                        }
                        else
                        {
                            //正则表达式
                            string regStr = "\\[(\\d{2}:\\d{2}\\.\\d{2})\\]";//匹配[00:00.00]....
                            Regex regex = new Regex(regStr);
                            string regTimeStr = "\\d{2}:\\d{2}\\.\\d{2}";
                            Regex regexTime = new Regex(regTimeStr);//匹配00:00.00
                            if (regex.IsMatch(line) == true)
                            {
                                //得到当前行匹配的所有内容并再次按正则表达式分割(分割的结果含时间和歌词)存放到数组里                  
                                string[] Content = regex.Split(line);
                                //时间数组.最大20个时间。您可以设置成更大。但是一般最多是3个时间对应一句歌词。本文件底下的"李克勤-红日"歌词文件有5个时间对应一句“哦”歌词的
                                string[] timesStr = new string[20];
                                int currentTime;
                                string currentTxt = null;//歌词(每一行歌词信息是只有一句歌词和可以有多个时间)
                                                         //在内容数组里轮询查找符合正则表达式的时间。找出时间并存放在数组里和找出歌词
                                int i = 0;
                                string correctLyricTxt = null;//正确歌词,因为数组Content里歌词有可能是null
                                foreach (string content in Content)
                                {
                                    //时间匹配正则表达式成功  
                                    if (regexTime.IsMatch(content) == true)
                                    {
                                        timesStr[i] = content;//这是时间
                                    }
                                    else
                                    {
                                        currentTxt = content;//这是歌词  
                                        if (!string.IsNullOrEmpty(content))
                                        {
                                            correctLyricTxt = content;
                                        }
                                    }
                                    i++;
                                }
                                //存放歌词时间和对应的歌词到资源字典里
                                //存放前先把时间转换成double型的毫秒
                                foreach (string time in timesStr)
                                {
                                    if (!string.IsNullOrEmpty(time))
                                    {
                                        try
                                        {
                                            currentTime = strToDouble(time);
                                            //此处模型做了简化，使用队列
                                            lrcque.Enqueue(new lrcline(currentTime, correctLyricTxt));
                                        }
                                        catch
                                        {
                                            //如果以上时间解析错误就不加入.说明该时间有误
                                        }
                                    }
                                }
                            }
                        }
                    }

                    //升序排序
                    lrcque.OrderBy(u => u.startTime);

                    //输出
                    while (lrcque.Any())
                    {
                        lrcline ll = lrcque.Dequeue();
                        timerque.Enqueue(ll.startTime);
                        textboxNextSub.AppendText(ll.lrc + Environment.NewLine);
                    }

                    if (textboxPrevSub.Text.Length > 0)
                    {
                        RowPrevSub.Height = new GridLength(0.5, GridUnitType.Star);
                        RowNextSub.Height = new GridLength(1, GridUnitType.Star);
                    }
                    else
                    {
                        RowPrevSub.Height = new GridLength(0, GridUnitType.Pixel);
                    }

                }
                catch
                {

                }
            }
        }

        FileInfo loadfi = null, loadlrc = null;
        string[] supportedFormat = new string[] { "*.mp3", "*.wav", "*.wma", "*.mp4", "*.mov" };

        private void FileSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {

            //寻找前缀文件
            loadfi = FindFile(supportedFormat);
            if (loadfi != null)
            {
                TextBlockStatus.Text = "载入：" + loadfi.Name;

                //以.为分界符 寻找lrc结尾
                loadlrc = FindFile(new string[] { "*.lrc" });
                //载入lrc文件
                if (loadlrc != null)
                    LoadLrc(loadlrc);

                buttonPlay.IsEnabled = true;
            }

        }

        private string MsToString(int ms)
        {
            int min, sec, mss;
            mss = ms % 1000;
            min = ms / 60000;
            sec = ms / 1000 - min * 60;
            return min.ToString("00") + ":" + sec.ToString("00") + "." + mss.ToString("000");
        }

        private void UpdateProc()
        {
            if (timerque.Any())
            {
                textblockTimeProc.Text = MsToString(currentTime) + "/" + MsToString(timerque.Last());
                sliderProgress.Maximum = timerque.Last();
                sliderProgress.Value = currentTime;
            }
            else
            {
                textblockTimeProc.Text = "00:00.000/00:00.000";
                sliderProgress.Maximum = 0;
                sliderProgress.Value = 0;
                    
            }
        }

        int currentTime;

        delegate void NextSubDelegate();
        delegate void ProgressUpdDelegate();

        private void NextSubDel()
        {
            NextSubDelegate nsd = new NextSubDelegate(NextSub);
            ProgressUpdDelegate pud = new ProgressUpdDelegate(UpdateProc);
            this.Dispatcher.Invoke(nsd);
            this.Dispatcher.Invoke(pud);
        }

        private void LrcProc()
        {
            while (timerque.Any() && !IsSubStopped)
            {
                int tmp = timerque.Dequeue();
                if (tmp > currentTime)
                {
                    Thread.Sleep(tmp - currentTime);
                    NextSubDel();
                    currentTime = tmp;
                }
                else break;
            }
            return ;
        }

        bool IsSubStopped = false;

        double sc_AudioChannel_ori;
        
        private void ForeRealStop()
        {
            if (sourceProvider_fore != null)
                sourceProvider_fore.MediaPlayer.Stop();
            buttonPlay.IsEnabled = true;
            buttonStop.IsEnabled = false;
            if (loadfi != null)
                TextBlockStatus.Text = "停止：" + loadfi.Name;
            else TextBlockStatus.Text = "停止";
            currentTime = 0;
            timerque.Clear();
            IsSubStopped = true;
            textboxCurrentSub.Clear();
            LoadLrc(loadlrc);
            if (currentTime > 0)
                PrevSub();
            
        }

        bool AudioStop = false;

        private void ForeStop()
        {
            if (buttonStop.IsEnabled)
            {
                if (sc!=null && sc.IsLoaded && Properties.Settings.Default.SmartPA == 3)
                {
                    // Fade Out
                    // Nothing happened.
                    // I have to use Slider binding to achieve that.
                    sc_AudioChannel_ori = sc.Slider4.Value;

                    AudioStop = true;

                    if (SliderSubTran.Value > 0)
                        FadeOutAnim(buttonSubtitler, SliderSubTran);
                    //DoubleAnimation audioFadeOut = new DoubleAnimation(sc_AudioChannel_ori, 0,
                    //   TimeSpan.FromSeconds(Properties.Settings.Default.TranSec))
                    //{ EasingFunction = new ExponentialEase() { EasingMode = EasingMode.EaseOut } };

                    //Storyboard AudioFOs = new Storyboard();
                    //Storyboard.SetTarget(audioFadeOut, sc.Slider4);
                    //Storyboard.SetTargetProperty(audioFadeOut, new PropertyPath("(Slider.Value)"));
                    //AudioFOs.Children.Add(audioFadeOut);
                    //AudioFOs.Completed += AudioFOs_Completed;
                    //AudioFOs.Begin();
                }
                else
                {
                    AudioStop = false;

                    ForeRealStop();
                    if (SliderSubTran.Value > 0)
                        FadeOutAnim(buttonSubtitler, SliderSubTran);
                }

                
            }
            
        }

        private void ForePlay()
        {
            if (buttonPlay.IsEnabled)
            {
                // 没开公屏的话自己开
                if (!(focaldephov != null && focaldephov.IsLoaded))
                {
                    ScreenSwitch();
                    //弹出音控台
                    if (sc == null || sc.IsLoaded == false)
                    {
                        sc = new SoundControl();
                        sc.Show();
                        sc.Topmost = true;
                    }
                }

                if (loadfi != null)
                {
                    if (ForePic.IsSelected || ForeVid.IsSelected)
                        ForeNone.IsSelected = true;

                    if (sourceProvider_fore != null && sourceProvider_fore.MediaPlayer.IsPlaying().Equals(true))
                    {
                        sourceProvider_fore.MediaPlayer.Stop();
                    }

                    string PlayStream = loadfi.FullName;
                    this.sourceProvider_fore = new VlcVideoSourceProvider(this.Dispatcher);
                    this.sourceProvider_fore.CreatePlayer(libDirectory, "--file-logging", "-vvv", "--logfile=Logs.log");
                    var mediaOptions = new[]
                    {""};

                    //this.sourceProvider_fore.MediaPlayer.EndReached += MediaPlayer_EndReached_1; ;
                    this.sourceProvider_fore.MediaPlayer.Log += new EventHandler<VlcMediaPlayerLogEventArgs>(MediaPlayer_Log);
                    this.sourceProvider_fore.MediaPlayer.Manager.SetFullScreen(this.sourceProvider_fore.MediaPlayer.Manager.CreateMediaPlayer(), true);
                    //this.sourceProvider_fore.MediaPlayer.Audio.IsMute = true;    //这个版本中被静音
                    //音量接口：this.sourceProvider_fore.MediaPlayer.Audio.Volume，本版本暂时不用
                    this.sourceProvider_fore.MediaPlayer.Audio.Volume = foreVol;
                    this.sourceProvider_fore.MediaPlayer.EncounteredError += new EventHandler<VlcMediaPlayerEncounteredErrorEventArgs>(MediaPlayer_ErrorEncountered);
                    this.sourceProvider_fore.MediaPlayer.Play(new Uri(PlayStream), mediaOptions);

                    bing_sub_fore = new Binding();
                    bing_sub_fore.Source = sourceProvider_fore;
                    bing_sub_fore.Path = new PropertyPath("VideoSource");
                    //输出图片
                    focaldephov.BackImg.SetBinding(Image.SourceProperty, bing_sub_fore);

                    IsSubStopped = false;

                    currentTime = 0;
                    Thread lrctimer = new Thread(new ThreadStart(LrcProc));
                    lrctimer.IsBackground = true;
                    lrctimer.Start();

                    buttonStop.IsEnabled = true;
                    buttonPlay.IsEnabled = false;

                    TextBlockStatus.Text = "播放：" + loadfi.Name;
                    if (SliderSubTran.Value == 0)
                        FadeOutAnim(buttonSubtitler, SliderSubTran);

                    if (sc != null && sc.IsLoaded && Properties.Settings.Default.SmartPA == 3)
                    {
                        SoundControl.soundControllers.ElementAt(3).On = true;
                        if (MSC != null)
                            MSC.Invoke(this, new MainSelecChangedArgs() { });
                    }
                }

            }
        }

        List<FileInfo> fis = new List<FileInfo>();


        int currentIndex = -1;

        private void UpdateCurrentInd()
        {
            if (currentIndex == -1)
            {
                for (int i = 0; i < fis.Count; ++i)
                {
                    FileInfo fi = fis.ElementAt(i);
                    if (fi.Name.Contains(FileSearchBox.Text))
                    {
                        currentIndex = i;
                        break;
                    }
                }
            }
            textblockListNum.Text = currentIndex + "/" + fis.Count;
        }

        private void ForeNextFile(bool next = true)
        {
            UpdateFis();
            UpdateCurrentInd();
            
            if (next)
            {
                if (currentIndex + 1 < fis.Count)
                { FileSearchBox.Text = fis.ElementAt(currentIndex + 1).Name; }
                else
                { FileSearchBox.Text = fis.ElementAt(0).Name; }
            }
            else
            {
                if (currentIndex - 1 >= 0)
                { FileSearchBox.Text = fis.ElementAt(currentIndex - 1).Name;  }
                else
                { FileSearchBox.Text = fis.ElementAt(fis.Count - 1).Name; }
            }

            textblockListNum.Text = currentIndex + "/" + fis.Count;

        }


        private void buttonStop_Click(object sender, RoutedEventArgs e)
        {
            ForeStop();
        }

        private void buttonPlay_Click(object sender, RoutedEventArgs e)
        {
            ForePlay();
        }

        private void PlayerStack_GotFocus(object sender, RoutedEventArgs e)
        {
            if (radialControllerInit && radialController.Menu.Items.Contains(rcplayer))
            {
                radialController.Menu.SelectMenuItem(rcplayer);
            }
        }

        private void SubtitlerStack_GotFocus(object sender, RoutedEventArgs e)
        {
            if (radialControllerInit && radialController.Menu.Items.Contains(rcsub))
            {
                radialController.Menu.SelectMenuItem(rcsub);
            }
        }

        #endregion

        #region 音控台

        private void buttonMonitorSound_Click(object sender, RoutedEventArgs e)
        {
            if (sc == null || sc.IsLoaded==false)
            {
                sc = new SoundControl();
                sc.Show();
                sc.Topmost = true;
            }
        }

        private void SoundControl_VCE(object sender, VolChangedArgs e)
        {
            ProgressLU.Value = Monitors.ElementAt(0).Volume;
            ProgressRU.Value = Monitors.ElementAt(1).Volume;
            ProgressLD.Value = Monitors.ElementAt(2).Volume;
            if (sourceProvider_fore != null)
                this.sourceProvider_fore.MediaPlayer.Audio.Volume = foreVol;
            //Monitors.ElementAt(0).Volume = (int)ProgressLU.Value;
            //Monitors.ElementAt(1).Volume = (int)ProgressRU.Value;
            //Monitors.ElementAt(2).Volume = (int)ProgressLD.Value;
        }

        #endregion

        #region 抽奖

        bool Gifting = false;
        
        private void buttonGift_Click(object sender, RoutedEventArgs e)
        {
            var myblue = new SolidColorBrush(Color.FromArgb(255, 1, 188, 225));
            if (Gifting)
            {
                //终止抽奖
                buttonGift.Background = Brushes.White;
                buttonGift.Foreground = myblue;

                FocalDepthHover.GiftGiving = false;

            }
            else
            {
                //开始抽奖
                buttonGift.Background = myblue;
                buttonGift.Foreground = Brushes.White;

                FocalDepthHover.GiftGiving = true;

            }
            Gifting = !Gifting;

        }

        // TODO: ffmpeg -f gdigrab -i title="FocalDepthHover" "rtmp://127.0.0.1/live"

        #endregion

        #region 词云

        bool wcCollecting = false;
        bool firstActivate = false;

        private void switchWCOpac()
        {

            if (WCOpacSlider.Value > 0)
            {
                dispatcherTimerWC.Stop();
                danmudic.Clear();
                wcCollecting = false;
                firstActivate = false;
                WCAutoGenerate.IsChecked = false;
            }
            else
            {
                wcCollecting = true;
                firstActivate = true;
                if (Properties.Settings.Default.WCAutoGen) GenerateWordCloud();
                else dispatcherTimerWC.Start();
            }

            FadeOutAnim(buttonWCOpac, WCOpacSlider);

        }

        private void buttonWCOpac_Click(object sender, RoutedEventArgs e)
        {
            switchWCOpac();
        }

        private void WCOpacSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (focaldephov != null) focaldephov.WordCloud.Opacity = WCOpacSlider.Value;
        }

        private void buttonWCMask_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog();
            openFileDialog.Title = "选择词云黑白遮罩图像";
            openFileDialog.Filter = "png|*.png|jpg|*.jpg|jpeg|*.jpeg";
            openFileDialog.FileName = string.Empty;
            openFileDialog.FilterIndex = 1;
            openFileDialog.RestoreDirectory = true;
            openFileDialog.DefaultExt = "jpg";
            System.Windows.Forms.DialogResult result = openFileDialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                System.Drawing.Image maskImg = System.Drawing.Image.FromFile(openFileDialog.FileName);
                if (!CheckMaskValid(maskImg))
                {
                    MessageBox.Show("请使用黑白遮罩图像","遮罩颜色空间错误",MessageBoxButton.OK,MessageBoxImage.None,MessageBoxResult.OK,MessageBoxOptions.None);
                    return;
                }
                ImageSource img = new ImageSourceConverter().ConvertFromString(openFileDialog.FileName) as ImageSource;
                WCMaskImg.Source = img;
                WCMaskAdd.Content = openFileDialog.FileName;
                //找到方法后放到线程中：
                //focaldephov.WordCloud.OpacityMask = new ImageBrush(img);

                Properties.Settings.Default.WCMaskAdd = openFileDialog.FileName;
            }
        }

        private void WCAutoGenerate_Checked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.WCAutoGen = true;
            SliderWCInterval.IsEnabled = false;
            dispatcherTimerWC.Stop();
            if(WCOpacSlider.Value>0) GenerateWordCloud();
        }

        private void WCAutoGenerate_Unchecked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.WCAutoGen = false;
            SliderWCInterval.IsEnabled = true;
            if(WCOpacSlider.Value > 0) dispatcherTimerWC.Start();
        }

        private void SliderWCInterval_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Properties.Settings.Default.WCInterval = SliderWCInterval.Value;
            dispatcherTimerWC.Stop();
            dispatcherTimerWC.Interval = TimeSpan.FromMinutes(SliderWCInterval.Value);
            if (WCOpacSlider.Value > 0) dispatcherTimerWC.Start();  //防止自动开启
        }

        Dictionary<string, int> danmudic = new Dictionary<string, int>();

        private void checkBoxWordCloudColor_Checked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.WCColor = true;
        }

        private void checkBoxWordCloudColor_Unchecked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.WCColor = false;
        }

        private void DispatcherTimerWC_Tick(object sender, EventArgs e)
        {
            GenerateWordCloud();
            if (Properties.Settings.Default.WCAutoGen) dispatcherTimerWC.Stop();
        }

        private void buttonGiftViewing_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("Gift.txt");
        }

        private void buttonRankViewing_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("Resources\\WordRank.txt");
        }

        private void sliderMaxVol_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Properties.Settings.Default.WCVol = (int)sliderMaxVol.Value + 1;
        }

        System.Drawing.Image img;
        Storyboard exitsb = new Storyboard();
        double opactmp;

        private void GenerateWordCloud()
        {
            //生成词云
            //根据弹幕存储词云

            //禁用词删除 TODO：加开关
            FileStream fs = new FileStream(@".\Resources\stopwords.txt", FileMode.Open, FileAccess.Read);
            using (StreamReader read = new StreamReader(fs, Encoding.Default))
            {
                string line;
                while ((line = read.ReadLine()) != null)
                    if (danmudic.Keys.Contains(line)) danmudic.Remove(line);
            }

            //获取降序
            Dictionary<string,int> DecDic = danmudic.OrderByDescending(u => u.Value).ToDictionary(o => o.Key, p => p.Value);

            List<string> keyw = new List<string>();
            List<int> freq = new List<int>();

            string rankpath = ".\\Resources\\WordRank.txt";
            File.WriteAllText(rankpath, "词云生成时间：" + DateTime.Now.ToString() + '\n');

            int wordCount = 0;
            foreach(var k in DecDic)
            {
                keyw.Add(k.Key);
                freq.Add(k.Value);
                File.AppendAllText(rankpath, k.Key + '\t' + k.Value + '\n');
                if (++wordCount >= Properties.Settings.Default.WCVol) break;
            }

            //建立词云

            var FontStr = ComboBoxFont.SelectedValue.ToString().Replace("System.Windows.Controls.ComboBoxItem: ", "");
            var maskadd = WCMaskAdd.Content==null?"":WCMaskAdd.Content.ToString();
            bool IsColor = Properties.Settings.Default.WCColor;
            System.Windows.Media.Color ForeColor = ForeColorPicker.SelectedColor;
            System.Drawing.Color forecolor = System.Drawing.Color.FromArgb(ForeColor.A, ForeColor.R, ForeColor.G, ForeColor.B);
            bool IsAuto = WCAutoGenerate.IsChecked.Equals(true) && wcCollecting;

            Task.Factory.StartNew(() =>
            {

                WordCloud wc;

                if (maskadd == "") {
                    if (IsColor)
                        wc = new WordCloud(1280, 720, mask: null, allowVerical: false, fontname: FontStr);
                    else 
                        wc = new WordCloud(1280, 720, mask: null, allowVerical: false, fontname: FontStr, fontColor: forecolor);
                }
                
                else
                {
                    System.Drawing.Image maskImg = System.Drawing.Image.FromFile(maskadd);
                    if (IsColor)
                        wc = new WordCloud(maskImg.Width, maskImg.Height, mask: maskImg, allowVerical: false, fontname: FontStr);
                    else
                        wc = new WordCloud(maskImg.Width, maskImg.Height, mask: maskImg, allowVerical: false, fontname: FontStr, fontColor: forecolor);
                }

                wc.OnProgress += Wc_OnProgress;
                //wc.StepDrawMode = true;
                //wc.OnStepDrawResultImg += ShowResultImage;
                //wc.OnStepDrawIntegralImg += ShowIntegralImage;
                img = wc.Draw(keyw, freq);

                Wc_OnProgress(1);
                this.Dispatcher.BeginInvoke(new Action(() => {
                    labelWCStatus.Content = "绘制完成";
                }));

                //开始下一轮循环
                if (IsAuto)
                {
                    //TODO: 退出动画
                    this.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        if (!firstActivate)
                        {
                            opactmp = WCOpacSlider.Value;
                            DoubleAnimation opan = new DoubleAnimation(opactmp, 0.01, TimeSpan.FromSeconds(Properties.Settings.Default.TranSec * 0.5));
                            //ScaleTransform sc = new ScaleTransform();
                            //focaldephov.WordCloud.RenderTransform = sc;
                            Storyboard.SetTarget(opan, WCOpacSlider);
                            Storyboard.SetTargetProperty(opan, new PropertyPath(Slider.ValueProperty));
                            exitsb.Children.Add(opan);
                            exitsb.Completed += Exitsb_Completed;
                            exitsb.Begin();
                        }
                        else firstActivate = false;
                        
                        GenerateWordCloud();
                    }));

                }
                else
                    ShowResultImage(img);

            });
        }

        private void buttonDisabledVol_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("Resources\\stopwords.txt");
        }

        private void textBoxUpperRight_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (focaldephov != null && focaldephov.IsLoaded)
            {
                focaldephov.labelUpperRight.Text = textBoxUpperRight.Text;
                Properties.Settings.Default.UpperRight = textBoxUpperRight.Text;
            }
        }

        private void buttonUpperRight_Click(object sender, RoutedEventArgs e)
        {
            
            if (focaldephov != null && focaldephov.IsLoaded)
            {
                DoubleAnimation uprifa = new DoubleAnimation();
                if (focaldephov.labelUpperRight.Opacity > 0)
                {
                    //ImgFadeOutAnim(buttonBackImg, focaldephov.BackImg);
                    uprifa.From = focaldephov.labelUpperRight.Opacity;
                    uprifa.To = 0;
                    uprifa.Duration = TimeSpan.FromSeconds(Properties.Settings.Default.TranSec);
                    uprifa.EasingFunction = new BackEase()
                    {
                        Amplitude = 0.3,
                        EasingMode = EasingMode.EaseOut,
                    };
                }
                else
                {
                    uprifa.From = 0;
                    uprifa.To = 1;
                    uprifa.Duration = TimeSpan.FromSeconds(Properties.Settings.Default.TranSec);
                    uprifa.EasingFunction = new BackEase()
                    {
                        Amplitude = 0.3,
                        EasingMode = EasingMode.EaseIn,
                    };
                }

                uprifa.Completed += Uprifa_Completed;
                focaldephov.labelUpperRight.BeginAnimation(OpacityProperty, uprifa, HandoffBehavior.SnapshotAndReplace);
                buttonUpperRight.IsEnabled = false;

            }
        }

        private void Uprifa_Completed(object sender, EventArgs e)
        {
            var myblue = new SolidColorBrush(Color.FromArgb(255, 1, 188, 225));
            if (focaldephov != null && focaldephov.IsLoaded)
            {
                if(focaldephov.labelUpperRight.Opacity > 0)
                {
                    buttonUpperRight.Background = myblue;
                    buttonUpperRight.Foreground = Brushes.White;
                }
                else
                {
                    buttonUpperRight.Background = Brushes.White;
                    buttonUpperRight.Foreground = myblue;
                }
                buttonUpperRight.IsEnabled = true;
            }
        }

        private void checkBoxMute_Checked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.SyncMute = true;
        }

        private void checkBoxMute_Unchecked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.SyncMute = false;
        }

        Storyboard entrysb = new Storyboard();

        private void Exitsb_Completed(object sender, EventArgs e)
        {
            //Remove
            //ColorSlider wcimage = Storyboard.GetTarget((sender as ClockGroup).Timeline.Children[0]) as ColorSlider;
            exitsb.Remove(WCOpacSlider);

            //Debug.WriteLine("Completed");
            ShowResultImage(img);

            DoubleAnimation opan = new DoubleAnimation(0.01, opactmp, TimeSpan.FromSeconds(Properties.Settings.Default.TranSec*0.5));
            
            Storyboard.SetTarget(opan, WCOpacSlider);
            Storyboard.SetTargetProperty(opan, new PropertyPath(Slider.ValueProperty)) ;
            entrysb.Children.Add(opan);
            entrysb.Completed += Entrysb_Completed;
            entrysb.Begin();
        }

        private void Entrysb_Completed(object sender, EventArgs e)
        {
            //Remove
            entrysb.Remove(WCOpacSlider);
            WCOpacSlider.Value = opactmp;
            buttonWCOpac.IsEnabled = true;
        }

        private void ShowResultImage(System.Drawing.Image img)
        {
            System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(img);
            //bmp.MakeTransparent(System.Drawing.Color.White);
            
            //优化
            IntPtr ptr = bmp.GetHbitmap();

            //MemoryStream 才可以被传递！
            using (MemoryStream stream = new MemoryStream())
            {
                bmp.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                bmp.Save(".\\Resources\\WordCloud.png", System.Drawing.Imaging.ImageFormat.Png);
                stream.Position = 0;
                BitmapImage result = new BitmapImage();
                result.BeginInit();
                result.CacheOption = BitmapCacheOption.OnLoad;
                result.StreamSource = stream;
                result.EndInit();
                result.Freeze();
                this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    focaldephov.Dispatcher.BeginInvoke(new Action(delegate
                    {
                        focaldephov.WordCloud.Source = result;
                    }));
                }));
            }
        }

        private void Wc_OnProgress(double progress)
        {
            this.Dispatcher.BeginInvoke(new Action(() => {
                progressWCGen.Value = Math.Min(progress * 100, progressWCGen.Maximum);
                labelWCStatus.Content = "正在绘制...";
            }));
            
        }

        bool CheckMaskValid(System.Drawing.Image mask)
        {
            bool valid;
            using (var bmp = new System.Drawing.Bitmap(mask))
            {
                var bmpdata = bmp.LockBits(new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, bmp.PixelFormat);
                var len = bmpdata.Height * bmpdata.Stride;
                var buf = new byte[len];
                Marshal.Copy(bmpdata.Scan0, buf, 0, len);
                valid = buf.Count(b => b != 0 && b != 255) == 0;
                bmp.UnlockBits(bmpdata);
            }
            return valid;
        }


        #endregion

    }
}