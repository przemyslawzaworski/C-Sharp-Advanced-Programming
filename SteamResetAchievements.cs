// Requires steam_api64.dll and steam_appid.txt
// csc SteamResetAchievements.cs
using System;
using System.Runtime.InteropServices;
using System.Text;

class Program
{
    [DllImport("steam_api64", EntryPoint = "SteamAPI_Init", CallingConvention = CallingConvention.Cdecl)] [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool SteamAPI_Init();

    [DllImport("steam_api64", EntryPoint = "SteamAPI_ManualDispatch_Init", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SteamAPI_ManualDispatch_Init();

    [DllImport("steam_api64", EntryPoint = "SteamAPI_ManualDispatch_RunFrame", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SteamAPI_ManualDispatch_RunFrame(int hSteamPipe);

    [DllImport("steam_api64", EntryPoint = "SteamAPI_GetHSteamPipe", CallingConvention = CallingConvention.Cdecl)]
    public static extern int SteamAPI_GetHSteamPipe();

    [DllImport("steam_api64", EntryPoint = "SteamAPI_ISteamClient_GetISteamUserStats", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr ISteamClient_GetISteamUserStats(IntPtr instancePtr, int hSteamUser, int hSteamPipe, UTF8StringHandle pchVersion);

    [DllImport("steam_api64", EntryPoint = "SteamInternal_CreateInterface", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr SteamInternal_CreateInterface(UTF8StringHandle ver);

    [DllImport("steam_api64", EntryPoint = "SteamAPI_GetHSteamUser", CallingConvention = CallingConvention.Cdecl)]
    public static extern int SteamAPI_GetHSteamUser();

    [DllImport("steam_api64", EntryPoint = "SteamAPI_ISteamUserStats_RequestCurrentStats", CallingConvention = CallingConvention.Cdecl)] [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool ISteamUserStats_RequestCurrentStats(IntPtr instancePtr);

    [DllImport("steam_api64", EntryPoint = "SteamAPI_ISteamUserStats_ResetAllStats", CallingConvention = CallingConvention.Cdecl)] [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool ISteamUserStats_ResetAllStats(IntPtr instancePtr, [MarshalAs(UnmanagedType.I1)] bool bAchievementsToo);

    [DllImport("steam_api64", EntryPoint = "SteamAPI_ISteamUserStats_StoreStats", CallingConvention = CallingConvention.Cdecl)] [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool ISteamUserStats_StoreStats(IntPtr instancePtr);

    static void Main(string[] args)
    {
        bool init = SteamAPI_Init();
        if (!init)
        {
            Console.WriteLine("SteamAPI.Init() failed!"); 
            Environment.Exit(0);
        }
        SteamAPI_ManualDispatch_Init();
        SteamAPI_ManualDispatch_RunFrame(SteamAPI_GetHSteamPipe());
        Console.Clear();
        UTF8StringHandle version = new UTF8StringHandle("STEAMUSERSTATS_INTERFACE_VERSION012");
        IntPtr getSteamClient = SteamInternal_CreateInterface(new UTF8StringHandle("SteamClient020"));
        IntPtr getSteamUserStats = ISteamClient_GetISteamUserStats(getSteamClient, SteamAPI_GetHSteamUser(), SteamAPI_GetHSteamPipe(), version);
        bool requestCurrentStats = ISteamUserStats_RequestCurrentStats(getSteamUserStats);
        bool resetAllStats = ISteamUserStats_ResetAllStats(getSteamUserStats, true);
        bool storeStats = ISteamUserStats_StoreStats(getSteamUserStats);
        Console.WriteLine("Tool for reset Steam game achievements. Author: P.Z.");
        Console.WriteLine("Request current stats: " + requestCurrentStats.ToString());
        Console.WriteLine("Reset all stats: " + resetAllStats.ToString());
        Console.WriteLine("Store all stats: " + storeStats.ToString());
        Console.WriteLine("Press any key to continue... ");
        Console.ReadKey();
    }
}

public class UTF8StringHandle : Microsoft.Win32.SafeHandles.SafeHandleZeroOrMinusOneIsInvalid
{
    public UTF8StringHandle(string text) : base(true)
    {
        if (text == null)
        {
            SetHandle(IntPtr.Zero);
            return;
        }
        byte[] bytes = new byte[Encoding.UTF8.GetByteCount(text) + 1];
        Encoding.UTF8.GetBytes(text, 0, text.Length, bytes, 0);
        IntPtr buffer = Marshal.AllocHGlobal(bytes.Length);
        Marshal.Copy(bytes, 0, buffer, bytes.Length);
        SetHandle(buffer);
    }

    protected override bool ReleaseHandle()
    {
        if (!IsInvalid)
        {
            Marshal.FreeHGlobal(handle);
        }
        return true;
    }
}