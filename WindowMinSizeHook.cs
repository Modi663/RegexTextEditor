using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using WinRT.Interop;

namespace RegexTextEditor
{
    internal static class WindowMinSizeHook
    {
        private const int GWL_WNDPROC = -4;
        private const int WM_GETMINMAXINFO = 0x0024;

        private delegate IntPtr WndProcDelegate(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        private static readonly Dictionary<IntPtr, WndProcDelegate> ProcMap = new();
        private static readonly Dictionary<IntPtr, IntPtr> OldProcMap = new();
        private static readonly Dictionary<IntPtr, (int Width, int Height)> MinSizeMap = new();

        public static void Attach(Window window, int minWidth, int minHeight)
        {
            IntPtr hwnd = WindowNative.GetWindowHandle(window);

            if (ProcMap.ContainsKey(hwnd))
            {
                MinSizeMap[hwnd] = (minWidth, minHeight);
                return;
            }

            MinSizeMap[hwnd] = (minWidth, minHeight);

            WndProcDelegate newProc = (hWnd, msg, wParam, lParam) =>
            {
                if (msg == WM_GETMINMAXINFO)
                {
                    MINMAXINFO mmi = Marshal.PtrToStructure<MINMAXINFO>(lParam);
                    var minSize = MinSizeMap[hWnd];

                    mmi.ptMinTrackSize.x = minSize.Width;
                    mmi.ptMinTrackSize.y = minSize.Height;

                    Marshal.StructureToPtr(mmi, lParam, true);
                    return IntPtr.Zero;
                }

                return CallWindowProc(OldProcMap[hWnd], hWnd, msg, wParam, lParam);
            };

            ProcMap[hwnd] = newProc;

            IntPtr newProcPtr = Marshal.GetFunctionPointerForDelegate(newProc);
            OldProcMap[hwnd] = SetWindowLongPtr(hwnd, GWL_WNDPROC, newProcPtr);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MINMAXINFO
        {
            public POINT ptReserved;
            public POINT ptMaxSize;
            public POINT ptMaxPosition;
            public POINT ptMinTrackSize;
            public POINT ptMaxTrackSize;
        }

        [DllImport("user32.dll", EntryPoint = "CallWindowProcW", SetLastError = true)]
        private static extern IntPtr CallWindowProc(
            IntPtr lpPrevWndFunc,
            IntPtr hWnd,
            int msg,
            IntPtr wParam,
            IntPtr lParam);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
        private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongW", SetLastError = true)]
        private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

        private static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr newProc)
        {
            if (IntPtr.Size == 8)
                return SetWindowLongPtr64(hWnd, nIndex, newProc);

            return new IntPtr(SetWindowLong32(hWnd, nIndex, newProc.ToInt32()));
        }
    }
}