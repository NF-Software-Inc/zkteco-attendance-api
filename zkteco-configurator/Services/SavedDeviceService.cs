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
	public void Save(IEnumerable<SavedDevice> devices)
	{
		Directory.CreateDirectory(FolderPath);

		var json = JsonSerializer.Serialize(devices, SerializerOptions);

		File.WriteAllText(FilePath, json);
	}

	/// <summary>
	/// Adds a new device or updates an existing one that matches by IP, port, and protocol.
	/// </summary>
	/// <param name="device">The device to add or update.</param>
	/// <returns>The updated list of saved devices.</returns>
	public List<SavedDevice> AddOrUpdate(SavedDevice device)
	{
		var devices = Load();
		var existing = devices.FirstOrDefault(x => IsMatch(x, device));

		if (existing != null)
		{
			existing.Password = device.Password;
			existing.NickName = device.NickName;
			existing.Description = device.Description;
		}
		else
		{
			devices.Add(device);
		}

		Save(devices);

		return devices;
	}

	/// <summary>
	/// Removes a device that matches by IP, port, and protocol.
	/// </summary>
	/// <param name="device">The device to remove.</param>
	/// <returns>The updated list of saved devices.</returns>
	public List<SavedDevice> Remove(SavedDevice device)
	{
		var devices = Load();

		devices.RemoveAll(x => IsMatch(x, device));
		Save(devices);

		return devices;
	}

	private static bool IsMatch(SavedDevice left, SavedDevice right) =>
		string.Equals(left.Ip, right.Ip, StringComparison.OrdinalIgnoreCase) &&
		left.Port == right.Port &&
		left.UseTcp == right.UseTcp;
}
