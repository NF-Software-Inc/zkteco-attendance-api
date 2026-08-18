using System.Text.Json;
using zkteco_configurator.Models;

namespace zkteco_configurator.Services;

/// <summary>
/// Saves and loads versioned device backup files.
/// </summary>
public sealed class DeviceBackupService
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };

    /// <summary>
    /// Serializes and saves a device backup to a location selected by the user.
    /// </summary>
    /// <param name="backup">The backup to save.</param>
    /// <returns>The full path of the saved backup file, or <see langword="null"/> when the user cancels.</returns>
    /// <exception cref="PlatformNotSupportedException">A save picker is unavailable on the current platform.</exception>
    public async Task<string?> SaveBackupAsync(DeviceBackupPackage backup)
    {
        ArgumentNullException.ThrowIfNull(backup);

        var fileName = $"zkteco_backup_{DateTime.Now:yyyyMMdd_HHmm}.json";
        var json = JsonSerializer.Serialize(backup, SerializerOptions);

#if WINDOWS
        var appWindow = Application.Current?.Windows is { Count: > 0 } windows
            ? windows[0]?.Handler?.PlatformView as Microsoft.UI.Xaml.Window
            : null;

        if (appWindow == null)
            throw new InvalidOperationException("Unable to get the active app window for backup export.");

        var picker = new Windows.Storage.Pickers.FileSavePicker
        {
            SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary,
            SuggestedFileName = Path.GetFileNameWithoutExtension(fileName),
        };

        picker.FileTypeChoices.Add("JSON file", [".json"]);

        var windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(appWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, windowHandle);

        var file = await picker.PickSaveFileAsync();

        if (file == null)
            return null;

        await Windows.Storage.FileIO.WriteTextAsync(file, json);

        return file.Path;
#else
        throw new PlatformNotSupportedException("Backup export with a save picker is currently supported on Windows only.");
#endif
    }

}
