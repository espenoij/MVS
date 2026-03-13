using System;
using System.Runtime.InteropServices;

namespace MVS
{
    public static class LivoxLidarApi
    {
        private const string DllName = "livox_lidar_sdk_shared";

        // Initialize the SDK with the path to a JSON config file.
        // host_ip and log_cfg_info may be passed as IntPtr.Zero.
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool LivoxLidarSdkInit(
            [MarshalAs(UnmanagedType.LPStr)] string path,
            IntPtr host_ip,
            IntPtr log_cfg_info);

        // Uninitialize the SDK and release all resources.
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void LivoxLidarSdkUninit();

        // Register the point cloud data callback.
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void SetLivoxLidarPointCloudCallBack(
            LivoxLidarPointCloudCallback cb,
            IntPtr client_data);

        // Register the device state-change info callback.
        // Provides handle -> IP mapping when a LiDAR is detected.
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void SetLivoxLidarInfoChangeCallback(
            LivoxLidarInfoChangeCallback cb,
            IntPtr client_data);

        // ── Callback delegates ────────────────────────────────────────────────

        // void(*)(uint32_t handle, uint8_t dev_type,
        //         LivoxLidarEthernetPacket* data, void* client_data)
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void LivoxLidarPointCloudCallback(
            uint handle,
            byte dev_type,
            IntPtr data,
            IntPtr client_data);

        // void(*)(uint32_t handle, const LivoxLidarStateInfo* info,
        //         void* client_data)
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void LivoxLidarInfoChangeCallback(
            uint handle,
            IntPtr info,
            IntPtr client_data);

        // ── LivoxLidarEthernetPacket byte offsets ─────────────────────────────
        // version(1)+length(2)+time_interval(2)+dot_num(2)+udp_cnt(2)
        // +frame_cnt(1)+data_type(1)+time_type(1)+reserved(12)+crc32(4)+timestamp(8) = 36
        public const int PacketOffsetDotNum   = 5;   // uint16_t
        public const int PacketOffsetDataType = 10;  // uint8_t
        public const int PacketHeaderSize     = 36;  // first data byte

        // ── Point data type constants ─────────────────────────────────────────
        public const byte CartesianCoordinate16 = 0x00; // int16 x,y,z in cm  (8 bytes/pt)
        public const byte CartesianCoordinate32 = 0x01; // int32 x,y,z in mm  (14 bytes/pt)
        public const byte SphericalCoordinate   = 0x02; // uint32 depth in mm  (10 bytes/pt)

        public const int SizeCartesianHighPoint = 14;
        public const int SizeCartesianLowPoint  = 8;
        public const int SizeSphericalPoint     = 10;

        // ── LivoxLidarStateInfo byte offsets ──────────────────────────────────
        // handle(4) + dev_type(1) + lidar_ip[16]
        public const int StateInfoOffsetIp = 5;  // char[16] null-terminated

        // ── Helpers ───────────────────────────────────────────────────────────

        /// <summary>
        /// Read a uint16_t in little-endian order from an unmanaged pointer.
        /// </summary>
        public static ushort ReadUInt16(IntPtr ptr, int offset)
        {
            return (ushort)(Marshal.ReadByte(ptr, offset) |
                            (Marshal.ReadByte(ptr, offset + 1) << 8));
        }

        /// <summary>
        /// Read a uint32_t in little-endian order from an unmanaged pointer.
        /// </summary>
        public static uint ReadUInt32(IntPtr ptr, int offset)
        {
            return (uint)(Marshal.ReadByte(ptr, offset) |
                          (Marshal.ReadByte(ptr, offset + 1) << 8) |
                          (Marshal.ReadByte(ptr, offset + 2) << 16) |
                          (Marshal.ReadByte(ptr, offset + 3) << 24));
        }

        /// <summary>
        /// Read a int16_t in little-endian order from an unmanaged pointer.
        /// </summary>
        public static short ReadInt16(IntPtr ptr, int offset)
        {
            return (short)ReadUInt16(ptr, offset);
        }

        /// <summary>
        /// Read a int32_t in little-endian order from an unmanaged pointer.
        /// </summary>
        public static int ReadInt32(IntPtr ptr, int offset)
        {
            return (int)ReadUInt32(ptr, offset);
        }

        /// <summary>
        /// Read a null-terminated ASCII string from an unmanaged pointer.
        /// </summary>
        public static string ReadAsciiString(IntPtr ptr, int offset, int maxLen)
        {
            byte[] buf = new byte[maxLen];
            for (int i = 0; i < maxLen; i++)
            {
                buf[i] = Marshal.ReadByte(ptr, offset + i);
                if (buf[i] == 0)
                    return System.Text.Encoding.ASCII.GetString(buf, 0, i);
            }
            return System.Text.Encoding.ASCII.GetString(buf, 0, maxLen);
        }

        /// <summary>
        /// Compute Euclidean range in mm from a 32-bit cartesian point at dataPtr+offset.
        /// </summary>
        public static double CartesianHighRange(IntPtr dataPtr, int offset)
        {
            double x = ReadInt32(dataPtr, offset);
            double y = ReadInt32(dataPtr, offset + 4);
            double z = ReadInt32(dataPtr, offset + 8);
            return Math.Sqrt(x * x + y * y + z * z);
        }

        /// <summary>
        /// Compute Euclidean range in mm from a 16-bit cartesian point at dataPtr+offset.
        /// Coordinates are in cm, result is converted to mm.
        /// </summary>
        public static double CartesianLowRange(IntPtr dataPtr, int offset)
        {
            double x = ReadInt16(dataPtr, offset)     * 10.0;
            double y = ReadInt16(dataPtr, offset + 2) * 10.0;
            double z = ReadInt16(dataPtr, offset + 4) * 10.0;
            return Math.Sqrt(x * x + y * y + z * z);
        }

        /// <summary>
        /// Read range in mm from a spherical point at dataPtr+offset.
        /// </summary>
        public static double SphericalRange(IntPtr dataPtr, int offset)
        {
            return ReadUInt32(dataPtr, offset);
        }
    }
}
