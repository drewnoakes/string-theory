using System;
using System.Diagnostics;

namespace StringTheory.Avalonia;

public sealed class ProcessInfo
{
    public int Id { get; }
    public string IdText { get; }
    public string ProcessName { get; }
    public string MainWindowTitle { get; }
    public string? FilePath { get; }

    public ProcessInfo(Process process)
    {
        Id = process.Id;
        IdText = process.Id.ToString();
        ProcessName = process.ProcessName;
        MainWindowTitle = process.MainWindowTitle;

        try
        {
            FilePath = process.MainModule?.FileName;
        }
        catch
        {
            // Access denied for some system processes
        }
    }

    public bool MatchesFilter(string filter)
    {
        return IdText.Contains(filter, StringComparison.OrdinalIgnoreCase)
            || ProcessName.Contains(filter, StringComparison.OrdinalIgnoreCase)
            || MainWindowTitle.Contains(filter, StringComparison.OrdinalIgnoreCase)
            || (FilePath?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false);
    }
}
