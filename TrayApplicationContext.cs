using System.Runtime.InteropServices;

namespace CenteringWindow;

/// <summary>
/// Контекст приложения для работы с иконкой в системном трее.
/// Центрирование окна происходит при клике по иконке.
/// </summary>
public class TrayApplicationContext : ApplicationContext
{
    // Импорт функции GetWindowRect для получения размеров окна.
    [DllImport("user32.dll")]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);

    // Импорт функции MoveWindow для перемещения окна.
    [DllImport("user32.dll")]
    public static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

    // Импорт функции SetForegroundWindow для перевода фокуса на окно.
    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    // Структура для хранения координат окна.
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    private readonly NotifyIcon _trayIcon;
    private readonly WindowFocusTracker _focusTracker;
    // Порог для определения двойного клика (в мс).
    private const int _doubleClickThreshold = 500;
    private DateTime _lastClickTime = DateTime.MinValue;
    // Таймер для ожидания одиночного клика.
    private readonly System.Threading.Timer _clickTimer;
    private bool _disposed = false;

    public TrayApplicationContext(WindowFocusTracker tracker)
    {
        _focusTracker = tracker;

        _clickTimer = new System.Threading.Timer(ClickTimerCallback, null, Timeout.Infinite, Timeout.Infinite);

        _trayIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Visible = true,
            Text = "Centering Window"
        };
        var stream = new MemoryStream(Properties.Resources.Icon);
        _trayIcon.Icon = new Icon(stream);

        // Подписка на события мыши.
        _trayIcon.MouseClick += TrayIconMouseClick;
        _trayIcon.MouseDoubleClick += TrayIconMouseDoubleClick;

        // Создаем контекстное меню с пунктом "Завершить".
        var toolStripMenuItem = new ToolStripMenuItem
        {
            Name = "Завершить",
            Size = new Size(180, 22),
            Text = "Завершить"
        };
        toolStripMenuItem.Click += ExitApplication;

        var contextMenuStrip = new ContextMenuStrip();
        contextMenuStrip.SuspendLayout();
        contextMenuStrip.Items.AddRange([toolStripMenuItem]);
        _trayIcon.ContextMenuStrip = contextMenuStrip;
        contextMenuStrip.ResumeLayout(false);
    }

    private void TrayIconMouseClick(object sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            var now = DateTime.Now;
            if ((now - _lastClickTime).TotalMilliseconds < _doubleClickThreshold)
            {
                return;
            }

            _lastClickTime = now;

            // Запускаем существующий таймер однократно
            _clickTimer.Change(_doubleClickThreshold, Timeout.Infinite);
        }
    }

    private void ClickTimerCallback(object state)
    {
        if ((DateTime.Now - _lastClickTime).TotalMilliseconds >= _doubleClickThreshold)
        {
            CenterActiveWindow(horizontalOnly: true);
        }
    }

    private void TrayIconMouseDoubleClick(object sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            // Отключаем таймер, если он работает
            _clickTimer.Change(Timeout.Infinite, Timeout.Infinite);
            CenterActiveWindow(horizontalOnly: false);
        }
    }

    /// <summary>
    /// Центрирует окно, используя сохраненный дескриптор из WindowFocusTracker.
    /// </summary>
    /// <param name="horizontalOnly">
    /// Если true – центрирование только по горизонтали, иначе – по обоим осям.
    /// </param>
    private void CenterActiveWindow(bool horizontalOnly)
    {
        var hWnd = _focusTracker.ActiveWindow;
        if (hWnd == IntPtr.Zero)
        {
            return;
        }

        if (!GetWindowRect(hWnd, out RECT rect))
        {
            return;
        }

        var windowWidth = rect.Right - rect.Left;
        var windowHeight = rect.Bottom - rect.Top;

        var screen = Screen.FromHandle(hWnd);
        var workArea = screen.WorkingArea;

        var newX = workArea.Left + (workArea.Width - windowWidth) / 2;
        var newY = rect.Top;
        if (!horizontalOnly)
        {
            newY = workArea.Top + (workArea.Height - windowHeight) / 2;
        }

        // Перемещаем окно
        MoveWindow(hWnd, newX, newY, windowWidth, windowHeight, true);
        // Переводим фокус на окно
        SetForegroundWindow(hWnd);
    }

    private void ExitApplication(object sender, EventArgs e)
    {
        _trayIcon.Visible = false;
        Application.Exit();
    }

    protected override void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            _clickTimer?.Dispose();
            _trayIcon?.Dispose();
        }

        _disposed = true;
        base.Dispose(disposing);
    }
}