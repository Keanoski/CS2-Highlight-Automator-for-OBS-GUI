using System;
using System.Diagnostics;

namespace HighlightReel.Services
{
    public static class CS2Launcher
    {
        public static (Process? Process, string? ErrorMessage) Launch(string exePath, string demoPath)
        {
            try
            {
                var info = new ProcessStartInfo
                {
                    FileName = exePath,
                    Arguments = $"-insecure -condebug +playdemo \"{demoPath}\"",
                    UseShellExecute = false,
                    RedirectStandardInput = true
                };

                var process = Process.Start(info);
                return (process, null);
            }
            catch (Exception ex)
            {
                return (null, $"Failed to launch CS2: {ex.Message}");
            }
        }
    }
}