using System.Runtime.InteropServices;

namespace UpdateApp;
#pragma warning disable CA1416
public static class ConsoleHelper {
    // ReSharper disable InconsistentNaming
    private const int STD_OUTPUT_HANDLE = -11;
    private const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetStdHandle(int handle);

    [DllImport("kernel32.dll")]
    private static extern bool GetConsoleMode(IntPtr handle, out uint mode);

    [DllImport("kernel32.dll")]
    private static extern bool SetConsoleMode(IntPtr handle, uint mode);

    public static void DisableConsoleWrapping() {
        // 检查当前操作系统是否为 Linux
        var isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

        if (isLinux)
            // 使用 ANSI 转义序列禁用换行
            Console.Write("\x1b[?7l");
        else
            DisableConsoleWrappingForWindows();
    }

    private static void DisableConsoleWrappingForWindows() {
        // 获取标准输出句柄
        var stdHandle = GetStdHandle(STD_OUTPUT_HANDLE);

        // 获取当前控制台模式
        GetConsoleMode(stdHandle, out var mode);

        // 启用虚拟终端处理
        mode |= ENABLE_VIRTUAL_TERMINAL_PROCESSING;

        // 设置新的控制台模式
        SetConsoleMode(stdHandle, mode);

        //禁用控制台换行
        //Console.BufferWidth = short.MaxValue - 1;

        //打满byd缓冲区
        Console.SetBufferSize(200, 500);
    }
}