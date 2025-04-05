using Microsoft.AspNetCore.Components;
using zkteco_attendance_api;

namespace zkteco_configurator.Components.Pages;

public partial class Home : ComponentBase
{
	private readonly PageModel InputModel = new();
	private ZkTeco? ZkTecoClock;

	private class PageModel
	{
		public string? IpAddress { get; set; }
		public int Port { get; set; } = 4_370;
		public bool UseTcp { get; set; } = true;
		public string? Password { get; set; }
	}
}
