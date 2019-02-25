using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace DoSTool
{
    public static class HelperFunctions
    {
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;

        public const int WM_VSCROLL = 0x115;
        public const int SB_BOTTOM = 7;

        public static Form1 FirstForm;
        public static List<Form1> FormList;

        internal static class NativeMethods
        {
            [DllImport("user32.dll", CharSet = CharSet.Auto)]
            internal static extern IntPtr SendMessage(IntPtr hWnd, int wMsg, UIntPtr wParam, IntPtr lParam);

            [DllImport("user32.dll", CharSet = CharSet.Auto)]
            internal static extern bool ReleaseCapture();
        }

        public static void ScrollToBottom(RichTextBox tb)
        {
            NativeMethods.SendMessage(tb.Handle, WM_VSCROLL, (UIntPtr)SB_BOTTOM, IntPtr.Zero);
        }
    }
}
