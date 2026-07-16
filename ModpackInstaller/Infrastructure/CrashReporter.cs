using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ModpackInstaller.Infrastructure;

public static class CrashReporter {
    public static void Log(Exception? ex, string source) {
        if (ex == null)
            return;

        try {
            var logFolder = Path.Combine(
                AppVariables.InstallerRoot,
                "crash_reports");

            Directory.CreateDirectory(logFolder);

            var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            var logPath = Path.Combine(logFolder, $"crash-{timestamp}.log");

            var appVersion =
                Assembly.GetExecutingAssembly()
                        .GetName()
                        .Version?.ToString() ?? "unknown";

            var report = $"""
                          ========== Modpack Installer Crash Report ==========
                          Timestamp     : {DateTime.Now}
                          Source        : {source}
                          App Version   : {appVersion}
                          .NET Version  : {Environment.Version}
                          OS            : {RuntimeInformation.OSDescription}
                          OS Arch       : {RuntimeInformation.OSArchitecture}
                          Process Arch  : {RuntimeInformation.ProcessArchitecture}
                          Machine Name  : {Environment.MachineName}
                          User          : {Environment.UserName}
                          ====================================================

                          {ex}

                          ====================================================
                          """;

            File.WriteAllText(logPath, report);
        }
        catch {
            // never throw from crash handler
        }
    }
}