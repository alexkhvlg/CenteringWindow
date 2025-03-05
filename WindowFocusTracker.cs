using System.Runtime.InteropServices;
using System.Text;

namespace CenteringWindow;

/// <summary>
/// Класс для отслеживания активного окна с использованием WinEventHook.
/// Отслеживает событие смены активного окна и сохраняет последний дескриптор, исключая системный трей.
/// </summary>
public class WindowFocusTracker : IDisposable
{
    // Импорт функции SetWinEventHook для установки хуков.
    [DllImport("user32.dll")]
    private static extern IntPtr SetWinEventHook(
        uint eventMin,
        uint eventMax,
        IntPtr hmodWinEventProc,
        WinEventDelegate lpfnWinEventProc,
        uint idProcess,
        uint idThread,
        uint dwFlags);

    // Импорт функции UnhookWinEvent для удаления хуков.
    [DllImport("user32.dll")]
    private static extern bool UnhookWinEvent(IntPtr hWinEventHook);

    // Импорт функции GetClassName для получения имени класса окна.
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern int GetClassName(
        IntPtr hWnd,
        StringBuilder lpClassName,
        int nMaxCount);

    // Импорт функции GetWindowText для получения заголовка окна (для отладки).
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern int GetWindowText(
        IntPtr hWnd,
        StringBuilder lpString,
        int nMaxCount);

    // Константы для событий WinAPI.
    private const uint EVENT_SYSTEM_FOREGROUND = 0x0003;
    private const uint WINEVENT_OUTOFCONTEXT = 0x0000;
    private const int MaxClassNameLength = 256;

    // Делегат для WinEventHook.
    private delegate void WinEventDelegate(
        IntPtr hWinEventHook,
        uint eventType,
        IntPtr hwnd,
        int idObject,
        int idChild,
        uint dwEventThread,
        uint dwmsEventTime);

    // Хук на событие и делегат, который держится в памяти.
    private IntPtr _hook = IntPtr.Zero;
    private readonly WinEventDelegate _winEventDelegate;

    // Поле для хранения последнего активного окна.
    private IntPtr _lastWindow = IntPtr.Zero;
    // Объект для синхронизации доступа.
    private readonly object _lockObj = new();

    /// <summary>
    /// При создании объекта начинается отслеживание активного окна.
    /// </summary>
    public WindowFocusTracker()
    {
        _winEventDelegate = new WinEventDelegate(WinEventProc);
        _hook = SetWinEventHook(
            EVENT_SYSTEM_FOREGROUND,
            EVENT_SYSTEM_FOREGROUND,
            IntPtr.Zero,
            _winEventDelegate,
            0,
            0,
            WINEVENT_OUTOFCONTEXT);
    }

    /// <summary>
    /// Метод, вызываемый при смене активного окна.
    /// Фильтрует окно системного трея (Shell_TrayWnd) и сохраняет дескриптор.
    /// </summary>
    private void WinEventProc(
        IntPtr hWinEventHook,
        uint eventType,
        IntPtr hwnd,
        int idObject,
        int idChild,
        uint dwEventThread,
        uint dwmsEventTime)
    {
        if (hwnd == IntPtr.Zero)
        {
            return;
        }

        // Получаем имя класса окна.
        var classNameBuilder = new StringBuilder(MaxClassNameLength);
        GetClassName(hwnd, classNameBuilder, MaxClassNameLength);
        var className = classNameBuilder.ToString();

        // Фильтруем окно системного трея ("Shell_TrayWnd").
        if (string.Equals(className, "Shell_TrayWnd", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        // Сохраняем дескриптор активного окна.
        lock (_lockObj)
        {
            _lastWindow = hwnd;
        }
    }

    /// <summary>
    /// Свойство для получения последнего активного окна.
    /// </summary>
    public IntPtr ActiveWindow
    {
        get
        {
            lock (_lockObj)
            {
                return _lastWindow;
            }
        }
    }

    /// <summary>
    /// Получает заголовок последнего активного окна (для отладки).
    /// </summary>
    public string? GetLastWindowTitle()
    {
        const int nChars = 256;
        if (ActiveWindow == IntPtr.Zero)
        {
            return null;
        }

        var buff = new StringBuilder(nChars);
        if (GetWindowText(ActiveWindow, buff, nChars) > 0)
        {
            return buff.ToString();
        }

        return null;
    }

    /// <summary>
    /// Освобождает ресурсы, связанные с хуком.
    /// </summary>
    public void Dispose()
    {
        if (_hook != IntPtr.Zero)
        {
            UnhookWinEvent(_hook);
            _hook = IntPtr.Zero;
        }
    }
}
