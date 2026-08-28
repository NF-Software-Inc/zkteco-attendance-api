using easy_blazor_bulma;
using Microsoft.JSInterop;
using System.Text;
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
    /// Serializes and saves a device backup.
    /// </summary>
    /// <param name="backup">The backup data to save.</param>
    /// <param name="jsRuntime">An IJSRuntime object to facilitate the download.</param>
    /// <returns>The file name of the saved backup.</returns>
    /// <exception cref="InvalidOperationException">Downloading the backup via JavaScript interop failed. Future implementation should consider replacing this with a dedicated cross-platform file saver (e.g. FileSaver.Default.SaveAsync()) for devices where this is not supported.</exception>
    public async Task<string> SaveBackupAsync(DeviceBackupPackage backup, IJSRuntime jsRuntime)
    {
        var fileName = $"zkteco_backup_{backup.Settings?.DeviceName}_{DateTime.Now:yyyyMMdd_HHmm}.json";
        var json = JsonSerializer.Serialize(backup, SerializerOptions);

        try
        {
            var url = $"data:application/json;base64,{Convert.ToBase64String(Encoding.UTF8.GetBytes(json))}";

			await jsRuntime.DownloadFile(fileName, url);
            return fileName;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Downloading the backup file is not supported on this device.", ex);
        }
    }

    /// <summary>
    /// Reads, deserializes, and validates a device backup file.
    /// </summary>
    /// <param name="stream">The stream containing the JSON contents of the backup file.</param>
    /// <exception cref="InvalidDataException">The selected file is not a valid device backup.</exception>
    public static async Task<DeviceBackupPackage> LoadBackupFromStreamAsync(Stream stream)
    {
		DeviceBackupPackage? backup;

		using var reader = new StreamReader(stream);
        var json = await reader.ReadToEndAsync();

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
		else if (string.IsNullOrWhiteSpace(backup.SchemaVersion))
			throw new InvalidDataException("The selected file is missing the device backup schema version.");
		else if (backup.SchemaVersion != DeviceBackupPackage.CurrentSchemaVersion)
			throw new InvalidDataException($"Unsupported device backup schema version '{backup.SchemaVersion}'. Expected '{DeviceBackupPackage.CurrentSchemaVersion}'.");

		return backup;
    }
}
