# Easy Password Validator

[![MIT](https://img.shields.io/github/license/thirstyape/zkteco-attendance-api)](https://github.com/thirstyape/zkteco-attendance-api/blob/master/LICENSE)
[![NuGet](https://img.shields.io/nuget/v/zkteco-attendance-api.svg)](https://www.nuget.org/packages/ZkTeco.Attendance.API/)

This project was created to provide an easy to use interface to interact with ZKTeco attendance recording devices for .NET.

It allows querying device details, querying user details, querying attendance records, creating users, deleting users, and more.

## Getting Started

These instuctions can be used to acquire and implement the library.

### Installation

To use this library:

* Clone a copy of the repository
* Reference the [NuGet package](https://www.nuget.org/packages/ZkTeco.Attendance.API/)

### Usage

**Basic Example**

The following example provides a complete use case. This example makes use of the most basic configuration.

```csharp
var clock = new ZkTeco("192.168.1.1");

if (clock.Connect())
    Console.WriteLine("Connected to ZKTeco clock at 192.168.1.1!");
else
    Console.WriteLine("Failed...");

Console.WriteLine("Name: " + clock.GetDeviceName() ?? "Uh-oh...");
Console.WriteLine("IP: " + clock.GetDeviceIp() ?? "Uh-oh...");
Console.WriteLine("Subnet: " + clock.GetDeviceSubnetMask() ?? "Uh-oh...");
Console.WriteLine("Gateway: " + clock.GetDeviceGatewayIp() ?? "Uh-oh...");
Console.WriteLine("MAC: " + clock.GetDeviceMac() ?? "Uh-oh...");
Console.WriteLine("Serial: " + clock.GetDeviceSerial() ?? "Uh-oh...");
Console.WriteLine("Firmware: " + clock.GetFirmwareVersion() ?? "Uh-oh...");
Console.WriteLine("Platform: " + clock.GetDevicePlatform() ?? "Uh-oh...");

var details = clock.GetStorageDetails();

if (details != null)
{
    Console.WriteLine("Users " + details.Users);
    Console.WriteLine("Available Users " + details.AvailableUsers);
    Console.WriteLine("Max Users " + details.MaximumUsers);

    Console.WriteLine("Records " + details.Records);
    Console.WriteLine("Available Records " + details.AvailableRecords);
    Console.WriteLine("Max Records " + details.MaximumRecords);
    
    Console.WriteLine("Fingerprints " + details.Fingers);
    Console.WriteLine("Available Fingerprints " + details.AvailableFingers);
    Console.WriteLine("Max Fingerprints " + details.MaximumFingers);
}

clock.Disconnect();
```

## Authors

* **NF Software Inc.**

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details

## Acknowledgments

Thank you to:
* [fananimi](https://github.com/fananimi) for the [pyzk](https://github.com/fananimi/pyzk) project and great examples of communications
