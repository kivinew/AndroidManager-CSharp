using System.Diagnostics;
using System.Runtime.InteropServices;

namespace AndroidManager.Services;

/// <summary>
/// Открывает папку в файловом менеджере ОС. Единая точка для вкладок «Приложения» и «Скрипты».
/// </summary>
internal static class FolderOpener
{
    /// <returns>true, если запуск не вызвал исключения (окно открывает сама оболочка).</returns>
    public static bool Open(string path, Action<string>? log = null)
    {
        string dir;
        try
        {
            dir = Path.GetFullPath(path);
            Directory.CreateDirectory(dir);
        }
        catch (Exception ex)
        {
            log?.Invoke($"Не удалось подготовить папку «{path}»: {ex.Message}");
            return false;
        }

        log?.Invoke($"Открытие папки: {dir}");

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // 1) ShellExecute по пути папки (verb "open"): оболочка сама запускает проводник
            //    на правильном рабочем столе, CreateProcess для explorer.exe не используется,
            //    поэтому ошибка 0xc0000142 невозможна.
            if (TryStart(log, "ShellExecute (путь папки)",
                    new ProcessStartInfo { FileName = dir, UseShellExecute = true }))
                return true;

            // 2) Запасной вариант: explorer.exe тоже через ShellExecute, путь в кавычках.
            return TryStart(log, "ShellExecute (explorer.exe)",
                new ProcessStartInfo { FileName = "explorer.exe", Arguments = $"\"{dir}\"", UseShellExecute = true });
        }

        return TryStart(log, RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "open" : "xdg-open",
            new ProcessStartInfo
            {
                FileName = RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "open" : "xdg-open",
                Arguments = $"\"{dir}\"",
                UseShellExecute = false,
            });
    }

    private static bool TryStart(Action<string>? log, string method, ProcessStartInfo psi)
    {
        try
        {
            // Для ShellExecute Process.Start часто возвращает null (проводник уже запущен) — это не ошибка.
            using var _ = Process.Start(psi);
            log?.Invoke($"{method}: OK");
            return true;
        }
        catch (Exception ex)
        {
            var code = ex is System.ComponentModel.Win32Exception w ? $" (Win32 {w.NativeErrorCode}, 0x{w.NativeErrorCode:X})" : "";
            log?.Invoke($"{method}: ошибка {ex.GetType().Name}: {ex.Message}{code}");
            return false;
        }
    }
}
