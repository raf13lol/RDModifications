using BepInEx.Configuration;
using HarmonyLib;
using BepInEx.Logging;
using UnityEngine;
using System;
using System.Runtime.InteropServices;

namespace RDModifications;

[Modification]
public class WindowTransparency
{
    public static bool properEnabled => enabled != null && enabled.Value;

    public static ConfigEntry<bool> enabled;
    public static ConfigEntry<byte> opacity;

    public static ManualLogSource logger;

    public static bool Init(ConfigFile config, ManualLogSource logging)
    {
        logger = logging;
        if (Application.platform != RuntimePlatform.WindowsPlayer)
            return false;

        enabled = config.Bind("WindowTransparency", "Enabled", false,
        "If you should be able to control the window's opacity.");

        opacity = config.Bind("WindowTransparency", "Opacity", (byte)128,
        "What the window's opacity should be. (0-255)");

        return enabled.Value;
    }

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
            if (win == null || win.ToInt32() == 0)
                return;

            SetWindowLongA(win, GWL_EXSTYLE, GetWindowLongA(win, GWL_EXSTYLE) | WS_EX_LAYERED);
            SetLayeredWindowAttributes(win, 0xFF000000, opacity.Value, LWA_ALPHA);

            doneItYet = true;
        }
    }
}
