using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPFSetVolume.VolumeHelper
{
    public class VolumeHelper
    {
        private static VolumeControl vControl;
        private static AudioEndpointVolumeCallback stateChangeCall;

        private static bool isInit = false;
        private static event Action VolumeStatChange;

        private VolumeHelper(){}

        public static void Init()
        {
            if (isInit)
            {
                return;
            }
            vControl = new VolumeControl();
            stateChangeCall = new AudioEndpointVolumeCallback();
            //注册回调函数
            stateChangeCall.VolumeStatChange += new AudioEndpointVolumeCallback.VolumeStatChangeDelegateHandle(stateChangeCall_VolumeStatChange);
            //将回调对象绑定到IAudioEndpointVolume
            vControl.AudioEndpoint.RegisterControlChangeNotify(stateChangeCall);

            isInit = true;
        }

        private static int stateChangeCall_VolumeStatChange()
        {
            if (VolumeStatChange!=null)
            {
                VolumeStatChange();
            }
            return 0;
        }

        public static void AddVolumeChangeNotify(Action method)
        {
            VolumeStatChange += method;
        }

        public static bool IsMute()
        {
            if (!isInit)
            {
                throw new ArgumentNullException("VolumeHelper is not call Init");
            }
            return vControl.IsMuted;
        }

        public static int GetVolume()
        {
            if (!isInit)
            {
                throw new ArgumentNullException("VolumeHelper is not call Init");
            }
            return vControl.GetVolume();
        }

        public static void SetMute(bool ismute=true)
        {
            if (!isInit)
            {
                throw new ArgumentNullException("VolumeHelper is not call Init");
            }
            vControl.IsMuted = ismute;
        }

        public static void SetVolume(int v)
        {
            if (!isInit)
            {
                throw new ArgumentNullException("VolumeHelper is not call Init");
            }
            vControl.SetVolume(v);
        }

    }
}
