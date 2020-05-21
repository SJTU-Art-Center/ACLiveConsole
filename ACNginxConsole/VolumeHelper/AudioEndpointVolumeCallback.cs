using System;
using System.Runtime.InteropServices;

namespace WPFSetVolume.VolumeHelper
{
    public class AudioEndpointVolumeCallback : IAudioEndpointVolumeCallback
    {
        private int _currentVolume = 0;
        /// <summary>
        /// 当前音量
        /// </summary>
        public int CurrentVolume
        {
            get { return _currentVolume; }
        }
        private bool _isMuted = false;
        /// <summary>
        /// 静音状态
        /// </summary>
        public bool IsMuted
        {
            get { return _isMuted; }
        }

        public delegate int VolumeStatChangeDelegateHandle();
        /// <summary>
        /// 音量、静音状态改变事件
        /// </summary>
        public event VolumeStatChangeDelegateHandle VolumeStatChange;
        public int OnNotify(IntPtr dataPtr)
        {
            AUDIO_VOLUME_NOTIFICATION_DATA notificationData = (AUDIO_VOLUME_NOTIFICATION_DATA)System.Runtime.InteropServices.Marshal.PtrToStructure(dataPtr, typeof(AUDIO_VOLUME_NOTIFICATION_DATA));
            _currentVolume = int.Parse((notificationData.MasterVolume * 100).ToString("0"));
            _isMuted = notificationData.IsMuted;
            if (VolumeStatChange != null)
            {
                VolumeStatChange();
            }
            return 0;
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct AUDIO_VOLUME_NOTIFICATION_DATA
    {
        public Guid EventContext;

        [MarshalAs(UnmanagedType.Bool)]
        public bool IsMuted;

        [MarshalAs(UnmanagedType.R4)]
        public float MasterVolume;

        [MarshalAs(UnmanagedType.U4)]
        public UInt32 ChannelCount;

        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.R4, SizeConst = 1)]
        public float[] ChannelVolumes;
    }

    [Guid("657804FA-D6AD-4496-8A60-352752AF4F89")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public partial interface IAudioEndpointVolumeCallback
    {
        [PreserveSig]
        int OnNotify(
            [In] IntPtr notificationData);
    }
}
