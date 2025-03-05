namespace CenteringWindow;

// �������� ����� ���������.
static class Program
{
    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        // ������� ��������� WindowFocusTracker, ������� ������ ����������� �������� ����.
        using var focusTracker = new WindowFocusTracker();
        using var trayContext = new TrayApplicationContext(focusTracker);
        Application.Run(trayContext);
    }
}