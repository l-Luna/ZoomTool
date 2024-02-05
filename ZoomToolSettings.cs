using Quintessential.Settings;

namespace ZoomTool;

public class ZoomToolSettings{

	[SettingsLabel("Zoom In")]
	public Keybinding ZoomIn = new(",", control: true);
	
	[SettingsLabel("Zoom Out")]
	public Keybinding ZoomOut = new(".", control: true);
}