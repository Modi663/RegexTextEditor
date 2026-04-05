using Microsoft.UI.Xaml;
using System;
using WinRT.Interop;

namespace RegexTextEditor.Helpers
{
    public static class WindowHandleHelper
    {
        public static IntPtr GetHandle(Window window)
        {
            return WindowNative.GetWindowHandle(window);
        }
    }
}