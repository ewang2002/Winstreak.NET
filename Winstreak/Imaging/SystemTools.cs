using System;
using System.Runtime.InteropServices;

namespace Winstreak.Imaging
{
	/// <summary>
    /// Set of systems tools.
    /// </summary>
    /// 
    /// <remarks><para>The class is a container of different system tools, which are used
    /// across the framework. Some of these tools are platform specific, so their
    /// implementation is different on different platform, like .NET and Mono.</para>
    /// </remarks>
    /// 
    public static class SystemTools
    {
	    /// <summary>
        /// Copy block of unmanaged memory.
        /// </summary>
        /// 
        /// <param name="dst">Destination pointer.</param>
        /// <param name="src">Source pointer.</param>
        /// <param name="count">Memory block's length to copy.</param>
        /// 
        /// <returns>Return's value of <paramref name="dst"/> - pointer to destination.</returns>
        /// 
        /// <remarks><para>This function is required because of the fact that .NET does
        /// not provide any way to copy unmanaged blocks, but provides only methods to
        /// copy from unmanaged memory to managed memory and vise versa.</para></remarks>
        ///
        public static IntPtr CopyUnmanagedMemory(IntPtr dst, IntPtr src, int count)
        {
            unsafe
            {
                CopyUnmanagedMemory((byte*)dst.ToPointer(), (byte*)src.ToPointer(), count);
            }
            return dst;
        }

        /// <summary>
        /// Copy block of unmanaged memory.
        /// </summary>
        /// 
        /// <param name="dst">Destination pointer.</param>
        /// <param name="src">Source pointer.</param>
        /// <param name="count">Memory block's length to copy.</param>
        /// 
        /// <returns>Return's value of <paramref name="dst"/> - pointer to destination.</returns>
        /// 
        /// <remarks><para>This function is required because of the fact that .NET does
        /// not provide any way to copy unmanaged blocks, but provides only methods to
        /// copy from unmanaged memory to managed memory and vise versa.</para></remarks>
        /// 
        public static unsafe byte* CopyUnmanagedMemory(byte* dst, byte* src, int count)
        {
#if NETSTANDARD
            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return memcpy(dst, src, count);
            }
            else
            {
                // for other platforms: copy bytewise
                byte* d = dst;
                byte* s = src;
                for (int i = 0; i < count; ++i, ++d, ++s)
                {
                    *d = *s;
                }
                return dst;
            }
#else
            return memcpy(dst, src, count);
#endif
        }

        /// <summary>
        /// Fill memory region with specified value.
        /// </summary>
        /// 
        /// <param name="dst">Destination pointer.</param>
        /// <param name="filler">Filler byte's value.</param>
        /// <param name="count">Memory block's length to fill.</param>
        /// 
        /// <returns>Return's value of <paramref name="dst"/> - pointer to destination.</returns>
        /// 
        public static IntPtr SetUnmanagedMemory(IntPtr dst, int filler, int count)
        {
            unsafe
            {
                SetUnmanagedMemory((byte*)dst.ToPointer(), filler, count);
            }
            return dst;
        }

        /// <summary>
        /// Fill memory region with specified value.
        /// </summary>
        /// 
        /// <param name="dst">Destination pointer.</param>
        /// <param name="filler">Filler byte's value.</param>
        /// <param name="count">Memory block's length to fill.</param>
        /// 
        /// <returns>Return's value of <paramref name="dst"/> - pointer to destination.</returns>
        /// 
        public static unsafe byte* SetUnmanagedMemory(byte* dst, int filler, int count)
        {
#if NETSTANDARD
            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return memset(dst, filler, count);
            }
            else
            {
                byte* d = dst;
                byte f = (byte)filler;
                for (int i = 0; i < count; ++i, ++d)
                {
                    *d = f;
                }
                return dst;
            }
#else
            return memset(dst, filler, count);
#endif
        }


        // Win32 memory copy function
        [DllImport("ntdll.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern unsafe byte* memcpy(byte* dst, byte* src, int count);

        // Win32 memory set function
        [DllImport("ntdll.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern unsafe byte* memset(byte* dst, int filler, int count);

    }
}