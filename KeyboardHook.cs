using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Keyer
{
 internal static class KeyboardHook
 {
 public enum KeyEventKind { KeyDown, KeyUp }
 public sealed class LowLevelKeyEvent
 {
 public Keys Key { get; init; }
 public bool IsExtended { get; init; }
 public bool IsInjected { get; init; }
 public bool Alt { get; init; }
 public bool Ctrl { get; init; }
 public bool Shift { get; init; }
 public KeyEventKind Kind { get; init; }
 }

 public delegate bool KeyFilter(LowLevelKeyEvent e);

 private static IntPtr _hookId = IntPtr.Zero;
 private static LowLevelKeyboardProc? _proc;
 private static KeyFilter? _filter;

 public static void Start(KeyFilter filter)
 {
 Stop();
 _filter = filter;
 _proc = HookCallback; // keep delegate alive
 using var curProcess = Process.GetCurrentProcess();
 using var curModule = curProcess.MainModule!;
 _hookId = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, GetModuleHandle(curModule.ModuleName),0);
 if (_hookId == IntPtr.Zero)
 throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
 }

 public static void Stop()
 {
 if (_hookId != IntPtr.Zero)
 {
 UnhookWindowsHookEx(_hookId);
 _hookId = IntPtr.Zero;
 }
 _proc = null;
 _filter = null;
 }

 private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
 {
 if (nCode >=0 && _filter != null)
 {
 int msg = wParam.ToInt32();
 if (msg == WM_KEYDOWN || msg == WM_SYSKEYDOWN || msg == WM_KEYUP || msg == WM_SYSKEYUP)
 {
 var info = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
 var key = (Keys)info.vkCode;
 var kind = (msg == WM_KEYDOWN || msg == WM_SYSKEYDOWN) ? KeyEventKind.KeyDown : KeyEventKind.KeyUp;
 var e = new LowLevelKeyEvent
 {
 Key = key,
 IsExtended = (info.flags &0x01) !=0,
 IsInjected = (info.flags &0x10) !=0,
 Alt = IsDown(Keys.Menu),
 Ctrl = IsDown(Keys.ControlKey),
 Shift = IsDown(Keys.ShiftKey),
 Kind = kind
 };
 bool suppress = _filter(e);
 if (suppress)
 return (IntPtr)1; // non-zero to eat the keystroke
 }
 }
 return CallNextHookEx(_hookId, nCode, wParam, lParam);
 }

 private static bool IsDown(Keys key)
 {
 short s = GetAsyncKeyState((int)key);
 return (s &0x8000) !=0;
 }

 private const int WH_KEYBOARD_LL =13;
 private const int WM_KEYDOWN =0x0100;
 private const int WM_KEYUP =0x0101;
 private const int WM_SYSKEYDOWN =0x0104;
 private const int WM_SYSKEYUP =0x0105;

 [StructLayout(LayoutKind.Sequential)]
 private struct KBDLLHOOKSTRUCT
 {
 public int vkCode;
 public int scanCode;
 public int flags;
 public int time;
 public IntPtr dwExtraInfo;
 }

 private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

 [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
 private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

 [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
 [return: MarshalAs(UnmanagedType.Bool)]
 private static extern bool UnhookWindowsHookEx(IntPtr hhk);

 [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
 private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

 [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
 private static extern IntPtr GetModuleHandle(string lpModuleName);

 [DllImport("user32.dll")]
 private static extern short GetAsyncKeyState(int vKey);
 }
}
