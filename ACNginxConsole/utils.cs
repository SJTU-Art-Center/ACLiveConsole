using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace BiliDMLib
{
    public static class utils
    {
//        public static byte[] ToBE(this byte[] b)
//        {
//            if (BitConverter.IsLittleEndian)
//            {
//                return b.Reverse().ToArray();
//            }
//            else
//            {
//                return b;
//            }
//        }
//        public static void ReadB(this NetworkStream stream, byte[] buffer, int offset, int count)
//        {
//            if (offset + count > buffer.Length)
//                throw new ArgumentException();
//            int read = 0;
//            while (read < count)
//            {
//                var available = stream.Read(buffer, offset, count - read);
//                if (available == 0)
//                {
//                    throw new ObjectDisposedException(null);
//                }
////                if (available != count)
////                {
////                    throw new NotSupportedException();
////                }
//                read += available;
//                offset += available;

//            }
               
//        }

        public static async Task ReadBAsync(this Stream stream, byte[] buffer, int offset, int count)
        {
            if (offset + count > buffer.Length)
                throw new ArgumentException();
            int read = 0;
            while (read < count)
            {
                var available = await stream.ReadAsync(buffer, offset, count - read);
                if (available == 0)
                {
                    throw new ObjectDisposedException(null);
                }
                //                if (available != count)
                //                {
                //                    throw new NotSupportedException();
                //                }
                read += available;
                offset += available;

            }

        }

    }
}