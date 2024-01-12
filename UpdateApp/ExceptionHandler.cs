namespace UpdateApp;

public static class ExceptionHandler {
    public static void PrintException(string message) {
        var separation = new string('-', 30);
        try {
            throw new Exception(message);
        } catch (Exception ex) {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.BackgroundColor = ConsoleColor.White;
            Console.Clear();
            Console.WriteLine("\n程序出现了异常");
            Console.WriteLine(ex.Message);
            Console.Write(separation);
            Console.WriteLine("\n如果这是网络波动引起的，我们建议多尝试几次\n如果此错误持续发生，请向程序提供者反馈\n请在互联网查阅资料后，再进行有效反馈，否则，不正确反馈可能不会被有效处理");
            Console.ResetColor();
            var timer = new Timer(ToggleConsoleColor!, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(500));
            Console.ReadKey();
            timer.Dispose();
        }
    }

    private static void ToggleConsoleColor(object state) {
        Console.ForegroundColor = Console.ForegroundColor == ConsoleColor.Red ? ConsoleColor.White : ConsoleColor.Red;
    }
}