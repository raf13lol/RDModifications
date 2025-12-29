using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace RDModifications;

[Modification("If the window's opacity should be able to be controlled.", false, (int)RuntimePlatform.WindowsPlayer)]
public class WindowTransparency : Modification
{
	[Configuration<byte>(128, "What the window's opacity should be. (0-255)")]
    public static ConfigEntry<byte> Opacity;

    [HarmonyPatch(typeof(scnMenu), "Update")]
    private class DoTheCodeHerePatch
    {
        public static bool doneItYet = false;

        public static void Prefix()
        {
            if (doneItYet)
                return;

            // native functions
            [DllImport("User32.dll")]
            static extern IntPtr FindWindowA(string lpClassName, string lpWindowName);
            [DllImport("User32.dll")]
            static extern int GetWindowLongA(IntPtr hWnd, int nIndex);
            [DllImport("User32.dll")]
            static extern int SetWindowLongA(IntPtr hWnd, int nIndex, int dwNewLong);
            [DllImport("User32.dll")]
            static extern int SetLayeredWindowAttributes(IntPtr hWnd, uint crKey, byte bAlpha, uint dwFlags);
            // [DllImport("User32.dll")]
            // static extern int UpdateLayeredWindow(IntPtr hWnd,    IntPtr? hdcDst, IntPtr? pptDst, IntPtr? psize,
            //                                     IntPtr? hdcSrc, IntPtr? pptSrc, uint crKey,     IntPtr? pblend, uint dwFlags);
            // native consts
            const int GWL_EXSTYLE = -20;
            const int WS_EX_LAYERED = 0x00080000;
            const int LWA_ALPHA = 0x00000002;
            IntPtr NULL = new(0);
            IntPtr win = FindWindowA(null, "Rhythm Doctor");
            if (win == null || win == NULL || win.ToInt32() == 0)
                return;

            SetWindowLongA(win, GWL_EXSTYLE, GetWindowLongA(win, GWL_EXSTYLE) | WS_EX_LAYERED);
            SetLayeredWindowAttributes(win, 0xFF000000, Opacity.Value, LWA_ALPHA);

            doneItYet = true;
        }
    }
}
