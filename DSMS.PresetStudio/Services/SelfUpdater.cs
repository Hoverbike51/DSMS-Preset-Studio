using System.Diagnostics;
using System.IO;

namespace DSMS.PresetStudio.Services;

public static class SelfUpdater
{
    public static int Run(string[] arguments)
    {
        if (arguments.Length < 4 || !int.TryParse(arguments[1], out var parentPid)) return 2;
        var target = Path.GetFullPath(arguments[2]);
        var source = Path.GetFullPath(arguments[3]);
        try
        {
            try { Process.GetProcessById(parentPid).WaitForExit(30000); } catch { }
            var backup = Path.Combine(Path.GetTempPath(), "DSMSPresetStudioRollback", DateTime.Now.ToString("yyyyMMdd-HHmmss"));
            Directory.CreateDirectory(backup);
            CopyTree(target, backup, preserveImportedIcons: false, skipMissing: true);
            try
            {
                CopyTree(source, target, preserveImportedIcons: true, skipMissing: false);
            }
            catch
            {
                CopyTree(backup, target, preserveImportedIcons: true, skipMissing: false);
                throw;
            }
            var executable = Directory.EnumerateFiles(target, "DSMS.PresetStudio.exe", SearchOption.TopDirectoryOnly).FirstOrDefault()
                             ?? Directory.EnumerateFiles(target, "DSMS Preset Studio.exe", SearchOption.TopDirectoryOnly).FirstOrDefault();
            if (executable is not null) Process.Start(new ProcessStartInfo(executable) { UseShellExecute = true });
            return 0;
        }
        catch (Exception exception)
        {
            File.WriteAllText(Path.Combine(Path.GetTempPath(), "DSMS-Preset-Studio-update-error.txt"), exception.ToString());
            return 1;
        }
    }

    private static void CopyTree(string source, string destination, bool preserveImportedIcons, bool skipMissing)
    {
        if (!Directory.Exists(source)) { if (skipMissing) return; throw new DirectoryNotFoundException(source); }
        foreach (var directory in Directory.EnumerateDirectories(source, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(source, directory);
            if (preserveImportedIcons && relative.StartsWith(Path.Combine("Icons", "Imported"), StringComparison.OrdinalIgnoreCase)) continue;
            Directory.CreateDirectory(Path.Combine(destination, relative));
        }
        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(source, file);
            if (preserveImportedIcons && relative.StartsWith(Path.Combine("Icons", "Imported"), StringComparison.OrdinalIgnoreCase)) continue;
            var target = Path.Combine(destination, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target, true);
        }
    }
}
