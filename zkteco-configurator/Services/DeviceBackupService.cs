using Microsoft.JSInterop;
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
    /// Serializes a device backup to its JSON representation.
    /// </summary>
    /// <param name="backup">The backup to serialize.</param>
    /// <returns>The JSON representation of the backup.</returns>
    private static string SerializeBackup(DeviceBackupPackage backup) => JsonSerializer.Serialize(backup, SerializerOptions);

    /// <summary>
    /// Serializes and saves a device backup by triggering a browser-style download through the BlazorWebView's
    /// JavaScript interop. This works uniformly across all platforms (Windows, Android, iOS, Mac Catalyst) since
    /// it relies only on the webview's file download support rather than any platform-specific save picker.
    /// </summary>
    /// <param name="backup">The backup to save.</param>
    /// <param name="jsRuntime">Used to trigger the browser-style download.</param>
    /// <returns>The file name of the saved backup.</returns>
    /// <exception cref="InvalidOperationException">Downloading the backup via JavaScript interop failed. Future implementation should consider replacing this with a dedicated cross-platform file saver (e.g. FileSaver.Default.SaveAsync()) for devices where this is not supported.</exception>
    public async Task<string> SaveBackupAsync(DeviceBackupPackage backup, IJSRuntime jsRuntime)
    {
        ArgumentNullException.ThrowIfNull(backup);
        ArgumentNullException.ThrowIfNull(jsRuntime);

        var fileName = $"zkteco_backup_{DateTime.Now:yyyyMMdd_HHmm}.json";
        var json = SerializeBackup(backup);

        try
        {
            var base64Content = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(json));
            await jsRuntime.InvokeVoidAsync("fileDownload.downloadFileFromBase64", fileName, base64Content, "application/json");

            return fileName;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Downloading the backup file is not supported on this device.", ex);
        }
    }


    /// <summary>
    /// Reads, deserializes, and validates a device backup file selected via an <see cref="Microsoft.AspNetCore.Components.Forms.InputFile"/>.
    /// </summary>
    /// <param name="stream">The stream containing the JSON contents of the backup file.</param>
    /// <param name="fileName">The name of the selected backup file.</param>
    /// <returns>The selected file name and the deserialized backup.</returns>
    /// <exception cref="InvalidDataException">The selected file is not a valid device backup.</exception>
    public static async Task<DeviceBackupSelection> LoadBackupFromStreamAsync(Stream stream, string fileName)
    {
        ArgumentNullException.ThrowIfNull(stream);

        using var reader = new StreamReader(stream);
        var json = await reader.ReadToEndAsync();
        var backup = DeserializeAndValidate(json);

        return new DeviceBackupSelection(fileName, backup);
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
