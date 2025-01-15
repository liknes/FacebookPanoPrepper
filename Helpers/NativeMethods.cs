using System.Runtime.InteropServices;

namespace FacebookPanoPrepper.Helpers
{
    internal static class NativeMethods
    {
        [DllImport("user32.dll")]
        private static extern int SetScrollInfo(IntPtr hwnd, int fnBar, ref SCROLLINFO lpsi, bool fRedraw);

        [StructLayout(LayoutKind.Sequential)]
        private struct SCROLLINFO
        {
            public uint cbSize;
            public uint fMask;
            public int nMin;
            public int nMax;
            public uint nPage;
            public int nPos;
            public int nTrackPos;
        }

        public static void SetScrollBarColors(IntPtr handle, Color background, Color thumb)
        {
            SCROLLINFO si = new SCROLLINFO();
            si.cbSize = (uint)Marshal.SizeOf(si);
            si.fMask = 0x1; // SIF_ALL

            SetScrollInfo(handle, 0, ref si, true);  // SB_HORZ
            SetScrollInfo(handle, 1, ref si, true);  // SB_VERT
        }
    }
}
