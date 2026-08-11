using System.Text.Json;
using zkteco_configurator.Models;

namespace zkteco_configurator.Services;

/// <summary>
/// Handles persistence of previously connected devices to the local file system.
/// </summary>
public class SavedDeviceService
{
	private const string FolderName = "ZkTecoConfigurator";
	private const string FileName = "saved-devices.json";

	private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };

	private readonly string FolderPath;
	private readonly string FilePath;

	/// <summary>
	/// Initializes a new instance of the <see cref="SavedDeviceService"/> class.
	/// </summary>
	public SavedDeviceService()
	{
		FolderPath = Path.Combine(FileSystem.AppDataDirectory, FolderName);
		FilePath = Path.Combine(FolderPath, FileName);
	}

	/// <summary>
	/// Loads the list of saved devices from the local file system.
	/// </summary>
	public List<SavedDevice> Load()
	{
		try
		{
			if (File.Exists(FilePath) == false)
				return [];

			var json = File.ReadAllText(FilePath);

			return JsonSerializer.Deserialize<List<SavedDevice>>(json) ?? [];
		}
		catch
		{
			return [];
		}
	}

	/// <summary>
	/// Persists the provided list of saved devices to the local file system.
	/// </summary>
	/// <param name="devices">The devices to store.</param>
	/// <returns><see langword="true"/> when the devices were stored successfully; otherwise <see langword="false"/>.</returns>
	public bool Save(IEnumerable<SavedDevice> devices)
	{
		try
		{
			Directory.CreateDirectory(FolderPath);

			var json = JsonSerializer.Serialize(devices, SerializerOptions);

			File.WriteAllText(FilePath, json);

			return true;
		}
		catch
		{
			return false;
		}
	}

	/// <summary>
	/// Adds a new device or updates an existing one that matches by IP address.
	/// </summary>
	/// <param name="device">The device to add or update.</param>
	/// <returns><see langword="true"/> when the device was stored successfully; otherwise <see langword="false"/>.</returns>
	public bool AddOrUpdate(SavedDevice device)
	{
		var devices = Load();
		var existing = devices.FirstOrDefault(x => IsMatch(x, device));

		if (existing != null)
		{
			existing.Port = device.Port;
			existing.UseTcp = device.UseTcp;
			existing.Password = device.Password;
			existing.NickName = device.NickName;
			existing.Description = device.Description;
		}
		else
		{
			devices.Add(device);
		}

		return Save(devices);
	}

	/// <summary>
	/// Removes a device that matches by IP address.
	/// </summary>
	/// <param name="device">The device to remove.</param>
	/// <returns><see langword="true"/> when the device was removed successfully; otherwise <see langword="false"/>.</returns>
	public bool Remove(SavedDevice device)
	{
		var devices = Load();
		var existing = devices.FirstOrDefault(x => IsMatch(x, device));

		if (existing == null)
			return false;

		devices.Remove(existing);

		return Save(devices);
	}

	private static bool IsMatch(SavedDevice left, SavedDevice right) =>
		string.Equals(left.Ip, right.Ip, StringComparison.OrdinalIgnoreCase);
}
