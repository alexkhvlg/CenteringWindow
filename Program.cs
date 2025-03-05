namespace CenteringWindow;

// ќсновной класс программы.
static class Program
{
    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        // —оздаем экземпл€р WindowFocusTracker, который начнет отслеживать активное окно.
        using var focusTracker = new WindowFocusTracker();
        using var trayContext = new TrayApplicationContext(focusTracker);
        Application.Run(trayContext);
    }
}