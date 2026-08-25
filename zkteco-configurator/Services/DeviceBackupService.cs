using System.Text.Json;
using zkteco_configurator.Models;

namespace zkteco_configurator.Services;

/// <summary>
/// Represents the result of a user selecting a backup file to import.
/// </summary>
/// <param name="FileName">The name of the selected backup file.</param>
/// <param name="Backup">The deserialized and validated device backup package.</param>
public sealed record DeviceBackupSelection(string FileName, DeviceBackupPackage Backup);

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

    /// <summary>
    /// Lets the user select a device backup file, then deserializes and validates it.
    /// </summary>
    /// <returns>The selected file name and the deserialized backup, or <see langword="null"/> when the user cancels.</returns>
    /// <exception cref="PlatformNotSupportedException">An open picker is unavailable on the current platform.</exception>
    /// <exception cref="InvalidDataException">The selected file is not a valid device backup.</exception>
    public async Task<DeviceBackupSelection?> LoadBackupAsync()
    {
#if WINDOWS
        var appWindow = Application.Current?.Windows is { Count: > 0 } windows
            ? windows[0]?.Handler?.PlatformView as Microsoft.UI.Xaml.Window
            : null;

        if (appWindow == null)
            throw new InvalidOperationException("Unable to get the active app window for backup import.");

        var picker = new Windows.Storage.Pickers.FileOpenPicker
        {
            SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary,
        };

        picker.FileTypeFilter.Add(".json");

        var windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(appWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, windowHandle);

        var file = await picker.PickSingleFileAsync();

        if (file == null)
            return null;

        var json = await Windows.Storage.FileIO.ReadTextAsync(file);
        var backup = DeserializeAndValidate(json);

        return new DeviceBackupSelection(file.Name, backup);
#else
        throw new PlatformNotSupportedException("Backup import with an open picker is currently supported on Windows only.");
#endif
    }

    /// <summary>
    /// Deserializes and validates the JSON contents of a device backup file.
    /// </summary>
    /// <param name="json">The JSON contents of the backup file.</param>
    /// <returns>The deserialized <see cref="DeviceBackupPackage"/>.</returns>
    /// <exception cref="InvalidDataException">The JSON does not represent a valid device backup.</exception>
    private static DeviceBackupPackage DeserializeAndValidate(string json)
    {
        DeviceBackupPackage? backup;

        try
        {
            backup = JsonSerializer.Deserialize<DeviceBackupPackage>(json, SerializerOptions);
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException("The selected file is not valid JSON.", ex);
        }

        if (backup == null || backup.DeviceInfo == null)
            throw new InvalidDataException("The selected file does not contain valid device information.");

        if (string.IsNullOrWhiteSpace(backup.SchemaVersion))
            throw new InvalidDataException("The selected file is missing the device backup schema version.");

        if (backup.SchemaVersion != DeviceBackupPackage.CurrentSchemaVersion)
            throw new InvalidDataException($"Unsupported device backup schema version '{backup.SchemaVersion}'. Expected '{DeviceBackupPackage.CurrentSchemaVersion}'.");

        return backup;
    }
}
