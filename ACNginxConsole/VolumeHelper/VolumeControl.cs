using System;
using System.Runtime.InteropServices;

namespace WPFSetVolume.VolumeHelper
{
    public class VolumeControl
    {
        private const string MMDeviceEnumeratorCLSID = "BCDE0395-E52F-467C-8E3D-C4579291692E";
        private const string IAudioEndpointVolumeIID = "5CDF2C82-841E-4546-9722-0CF74078229A";
        private const uint DEVICE_STATE_ACTIVE = 0x00000001;

        public IAudioEndpointVolume AudioEndpoint = null;

        public VolumeControl()
        {
            var deviceEnumeratorType = Type.GetTypeFromCLSID(new Guid(MMDeviceEnumeratorCLSID));

            var deviceEnumerator = (IMMDeviceEnumerator)Activator.CreateInstance(deviceEnumeratorType);

            IMMDevice device;

            deviceEnumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eCommunications, out device);

            Guid iid = new Guid(IAudioEndpointVolumeIID);

            object objInterface = null;
            int result = device.Activate(iid, (uint)CLSCTX.CLSCTX_INPROC_SERVER, IntPtr.Zero, out objInterface);

            AudioEndpoint = objInterface as IAudioEndpointVolume;
        }
        /// <summary>
        /// 设置系统音量
        /// </summary>
        /// <param name="volume">音量（0-100）</param>
        /// <returns></returns>
        public void SetVolume(int volume)
        {
            float level = 0.0f;
            if (volume >= 100)
            {
                level = 1f;
            }
            else if (volume < 100 && volume >= 0)
            {
                level = volume / 100.0f;
            }
            else
            {
                level = 0f;
            }
            //设置主音量
            AudioEndpoint.SetMasterVolumeLevelScalar(level, Guid.NewGuid());
        }
        /// <summary>
        /// 获取当前音量
        /// </summary>
        /// <returns>音量（0-100）</returns>
        public int GetVolume()
        {
            float level = 0.0f;
            //获取系统主音量
            AudioEndpoint.GetMasterVolumeLevelScalar(out level);
            //float存在精度丢失的问题，此处将float乘以100后进行四舍五入
            return int.Parse((level * 100).ToString("0"));
        }
        private bool _isMuted = false;
        /// <summary>
        /// 是否为静音状态
        /// </summary>
        public bool IsMuted
        {
            get
            {
                AudioEndpoint.GetMute(out _isMuted);
                return _isMuted;
            }
            set
            {
                _isMuted = value;
                AudioEndpoint.SetMute(_isMuted, Guid.NewGuid());
            }
        }
    }


    [Guid("A95664D2-9614-4F35-A746-DE8DB63617E6")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public partial interface IMMDeviceEnumerator
    {
        [PreserveSig]
        int EnumAudioEndpoints(
            [In] [MarshalAs(UnmanagedType.I4)] EDataFlow dataFlow,
            [In] [MarshalAs(UnmanagedType.U4)] UInt32 stateMask,
            [Out] [MarshalAs(UnmanagedType.Interface)] out IMMDeviceCollection devices);

        [PreserveSig]
        int GetDefaultAudioEndpoint(
            [In] [MarshalAs(UnmanagedType.I4)] EDataFlow dataFlow,
            [In] [MarshalAs(UnmanagedType.I4)] ERole role,
            [Out] [MarshalAs(UnmanagedType.Interface)] out IMMDevice device);

        [PreserveSig]
        int GetDevice(
            [In] [MarshalAs(UnmanagedType.LPWStr)] string endpointId,
            [Out] [MarshalAs(UnmanagedType.Interface)] out IMMDevice device);

        [PreserveSig]
        int RegisterEndpointNotificationCallback(
            [In] [MarshalAs(UnmanagedType.Interface)] IMMNotificationClient client);

        [PreserveSig]
        int UnregisterEndpointNotificationCallback(
            [In] [MarshalAs(UnmanagedType.Interface)] IMMNotificationClient client);
    }

    [Guid("0BD7A1BE-7A1A-44DB-8397-CC5392387B5E")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public partial interface IMMDeviceCollection
    {
        [PreserveSig]
        int GetCount(
            [Out] [MarshalAs(UnmanagedType.U4)] out UInt32 count);

        [PreserveSig]
        int Item(
            [In] [MarshalAs(UnmanagedType.U4)] UInt32 index,
            [Out] [MarshalAs(UnmanagedType.Interface)] out IMMDevice device);
    }

    [Guid("5CDF2C82-841E-4546-9722-0CF74078229A")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public partial interface IAudioEndpointVolume
    {
        [PreserveSig]
        int RegisterControlChangeNotify(
            [In] [MarshalAs(UnmanagedType.Interface)] IAudioEndpointVolumeCallback client);

        [PreserveSig]
        int UnregisterControlChangeNotify(
            [In] [MarshalAs(UnmanagedType.Interface)] IAudioEndpointVolumeCallback client);

        [PreserveSig]
        int GetChannelCount(
            [Out] [MarshalAs(UnmanagedType.U4)] out UInt32 channelCount);

        [PreserveSig]
        int SetMasterVolumeLevel(
            [In] [MarshalAs(UnmanagedType.R4)] float level,
            [In] [MarshalAs(UnmanagedType.LPStruct)] Guid eventContext);

        [PreserveSig]
        int SetMasterVolumeLevelScalar(
            [In] [MarshalAs(UnmanagedType.R4)] float level,
            [In] [MarshalAs(UnmanagedType.LPStruct)] Guid eventContext);

        [PreserveSig]
        int GetMasterVolumeLevel(
            [Out] [MarshalAs(UnmanagedType.R4)] out float level);

        [PreserveSig]
        int GetMasterVolumeLevelScalar(
            [Out] [MarshalAs(UnmanagedType.R4)] out float level);

        [PreserveSig]
        int SetChannelVolumeLevel(
            [In] [MarshalAs(UnmanagedType.U4)] UInt32 channelNumber,
            [In] [MarshalAs(UnmanagedType.R4)] float level,
            [In] [MarshalAs(UnmanagedType.LPStruct)] Guid eventContext);

        [PreserveSig]
        int SetChannelVolumeLevelScalar(
            [In] [MarshalAs(UnmanagedType.U4)] UInt32 channelNumber,
            [In] [MarshalAs(UnmanagedType.R4)] float level,
            [In] [MarshalAs(UnmanagedType.LPStruct)] Guid eventContext);

        [PreserveSig]
        int GetChannelVolumeLevel(
            [In] [MarshalAs(UnmanagedType.U4)] UInt32 channelNumber,
            [Out] [MarshalAs(UnmanagedType.R4)] out float level);

        [PreserveSig]
        int GetChannelVolumeLevelScalar(
            [In] [MarshalAs(UnmanagedType.U4)] UInt32 channelNumber,
            [Out] [MarshalAs(UnmanagedType.R4)] out float level);

        [PreserveSig]
        int SetMute(
            [In] [MarshalAs(UnmanagedType.Bool)] Boolean isMuted,
            [In] [MarshalAs(UnmanagedType.LPStruct)] Guid eventContext);

        [PreserveSig]
        int GetMute(
            [Out] [MarshalAs(UnmanagedType.Bool)] out Boolean isMuted);

        [PreserveSig]
        int GetVolumeStepInfo(
            [Out] [MarshalAs(UnmanagedType.U4)] out UInt32 step,
            [Out] [MarshalAs(UnmanagedType.U4)] out UInt32 stepCount);

        [PreserveSig]
        int VolumeStepUp(
            [In] [MarshalAs(UnmanagedType.LPStruct)] Guid eventContext);

        [PreserveSig]
        int VolumeStepDown(
            [In] [MarshalAs(UnmanagedType.LPStruct)] Guid eventContext);

        [PreserveSig]
        int QueryHardwareSupport(
            [Out] [MarshalAs(UnmanagedType.U4)] out UInt32 hardwareSupportMask);

        [PreserveSig]
        int GetVolumeRange(
            [Out] [MarshalAs(UnmanagedType.R4)] out float volumeMin,
            [Out] [MarshalAs(UnmanagedType.R4)] out float volumeMax,
            [Out] [MarshalAs(UnmanagedType.R4)] out float volumeStep);
    }

    [Guid("D666063F-1587-4E43-81F1-B948E807363F")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public partial interface IMMDevice
    {
        [PreserveSig]
        int Activate(
            [In] [MarshalAs(UnmanagedType.LPStruct)] Guid interfaceId,
            [In] [MarshalAs(UnmanagedType.U4)] UInt32 classContext,
            [In, Optional] IntPtr activationParams, // TODO: Update to use PROPVARIANT and test properly.
            [Out] [MarshalAs(UnmanagedType.IUnknown)] out object instancePtr);

        [PreserveSig]
        int OpenPropertyStore(
            [In] [MarshalAs(UnmanagedType.U4)] UInt32 accessMode,
            [Out] [MarshalAs(UnmanagedType.Interface)] out IPropertyStore properties);

        [PreserveSig]
        int GetId(
            [Out] [MarshalAs(UnmanagedType.LPWStr)] out string strId);

        [PreserveSig]
        int GetState(
            [Out] [MarshalAs(UnmanagedType.U4)] out UInt32 deviceState);
    }

    [Guid("886d8eeb-8cf2-4446-8d02-cdba1dbdcf99")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IPropertyStore
    {
        [PreserveSig]
        int GetCount(
            [Out] [MarshalAs(UnmanagedType.U4)] out UInt32 propertyCount);

        [PreserveSig]
        int GetAt(
            [In] [MarshalAs(UnmanagedType.U4)] UInt32 propertyIndex,
            [Out] out PROPERTYKEY propertyKey);

        [PreserveSig]
        int GetValue(
            [In] ref PROPERTYKEY propertyKey,
            [Out] out PROPVARIANT value);

        [PreserveSig]
        int SetValue(
            [In] ref PROPERTYKEY propertyKey,
            [In] ref PROPVARIANT value);

        [PreserveSig]
        int Commit();
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PROPVARIANT
    {
        public short vt;
        public short wReserved1;
        public short wReserved2;
        public short wReserved3;
        public VariantData Data;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct VariantData
    {
        [FieldOffset(0)]
        public bool AsBoolean;

        [FieldOffset(0)]
        public UInt32 AsUInt32;

        [FieldOffset(0)]
        public IntPtr AsStringPtr;

        [FieldOffset(4)]
        public IntPtr AsFormatPtr;
    }


    [StructLayout(LayoutKind.Sequential)]
    public struct PROPERTYKEY
    {
        public Guid fmtid;
        public int pid;
    }

    [Guid("7991EEC9-7E89-4D85-8390-6C703CEC60C0")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public partial interface IMMNotificationClient
    {
        void OnDeviceStateChanged(
            [MarshalAs(UnmanagedType.LPWStr)] string deviceId,
            [MarshalAs(UnmanagedType.U4)] UInt32 newState);

        void OnDeviceAdded(
            [MarshalAs(UnmanagedType.LPWStr)] string deviceId);

        void OnDeviceRemoved(
            [MarshalAs(UnmanagedType.LPWStr)] string deviceId);

        void OnDefaultDeviceChanged(
            [MarshalAs(UnmanagedType.I4)] EDataFlow dataFlow,
            [MarshalAs(UnmanagedType.I4)] ERole deviceRole,
            [MarshalAs(UnmanagedType.LPWStr)] string defaultDeviceId);

        void OnPropertyValueChanged(
            [MarshalAs(UnmanagedType.LPWStr)] string deviceId, PROPERTYKEY propertyKey);
    }

    public enum EDataFlow
    {
        eRender = 0,
        eCapture = 1,
        eAll = 2
    }

    public enum CLSCTX : uint
    {
        CLSCTX_INPROC_SERVER = 0x1,
        CLSCTX_INPROC_HANDLER = 0x2,
        CLSCTX_LOCAL_SERVER = 0x4,
        CLSCTX_INPROC_SERVER16 = 0x8,
        CLSCTX_REMOTE_SERVER = 0x10,
        CLSCTX_INPROC_HANDLER16 = 0x20,
        CLSCTX_RESERVED1 = 0x40,
        CLSCTX_RESERVED2 = 0x80,
        CLSCTX_RESERVED3 = 0x100,
        CLSCTX_RESERVED4 = 0x200,
        CLSCTX_NO_CODE_DOWNLOAD = 0x400,
        CLSCTX_RESERVED5 = 0x800,
        CLSCTX_NO_CUSTOM_MARSHAL = 0x1000,
        CLSCTX_ENABLE_CODE_DOWNLOAD = 0x2000,
        CLSCTX_NO_FAILURE_LOG = 0x4000,
        CLSCTX_DISABLE_AAA = 0x8000,
        CLSCTX_ENABLE_AAA = 0x10000,
        CLSCTX_FROM_DEFAULT_CONTEXT = 0x20000,
        CLSCTX_ACTIVATE_32_BIT_SERVER = 0x40000,
        CLSCTX_ACTIVATE_64_BIT_SERVER = 0x80000,
        CLSCTX_ENABLE_CLOAKING = 0x100000,
        CLSCTX_PS_DLL = 0x80000000
    }

    public enum ERole
    {
        eConsole = 0,
        eMultimedia = 1,
        eCommunications = 2
    }
}
