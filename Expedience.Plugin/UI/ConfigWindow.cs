using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace Expedience.UI
{
	public class ConfigWindow : Window, IDisposable
	{
		private readonly LocalDbManager _localDbManager;

		public ConfigWindow(LocalDbManager localDbManager) :
			base($"Expedience v{Service.Plugin.GetVersion()} Settings", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
		{
			_localDbManager = localDbManager;

			SizeConstraints = new WindowSizeConstraints
			{
				MinimumSize = new Vector2(430, 330),
				MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
			};
		}

		public override void Draw()
		{
			var userName = Service.Plugin.GetUserName();

			ImGui.LabelText(userName, "User Name: ");
			ImGui.SameLine();
		}

		public void Dispose()
		{
			
		}
	}
}
