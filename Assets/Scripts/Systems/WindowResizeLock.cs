using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace SuspiciousSauna
{
    public static class WindowResizeLock
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        [DllImport("user32.dll")] static extern IntPtr GetActiveWindow();
        [DllImport("user32.dll")] static extern int GetWindowLong(IntPtr hWnd, int index);
        [DllImport("user32.dll")] static extern int SetWindowLong(IntPtr hWnd, int index, int value);
        [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr hWnd, IntPtr after, int x, int y, int w, int h, uint flags);
        static IntPtr window;
        static int originalStyle;
#endif
        public static void SetLocked(bool locked)
        {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            if (window == IntPtr.Zero) { window = GetActiveWindow(); if (window == IntPtr.Zero) return; originalStyle = GetWindowLong(window, -16); }
            const int resizeFrame = 0x00040000, maximizeBox = 0x00010000;
            int style = locked ? originalStyle & ~resizeFrame & ~maximizeBox : originalStyle;
            SetWindowLong(window, -16, style);
            SetWindowPos(window, IntPtr.Zero, 0, 0, 0, 0, 0x0027);
#endif
        }
    }
}
