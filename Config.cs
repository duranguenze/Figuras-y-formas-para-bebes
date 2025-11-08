using System.Text.Json;
using System.Text.Json.Serialization;

namespace Keyer
{
 public sealed class KeyerConfig
 {
 public KioskConfig kiosk { get; set; } = new();
 public InputConfig input { get; set; } = new();
 public VisualConfig visual { get; set; } = new();
 public SoundConfig sound { get; set; } = new();
 public ActionsConfig actions { get; set; } = new();

 public static KeyerConfig Load(string path)
 {
 var json = File.ReadAllText(path);
 var opts = new JsonSerializerOptions
 {
 PropertyNameCaseInsensitive = true,
 ReadCommentHandling = JsonCommentHandling.Skip,
 AllowTrailingCommas = true
 };
 return JsonSerializer.Deserialize<KeyerConfig>(json, opts) ?? new KeyerConfig();
 }
 }

 public sealed class KioskConfig
 {
 public bool topMost { get; set; } = true;
 public bool hideCursor { get; set; } = false;
 public bool startFullscreen { get; set; } = true;
 public bool blockWindowsKey { get; set; } = true;
 public bool blockAltF4 { get; set; } = true;
 public bool blockAltTab { get; set; } = true;
 }
 public sealed class InputConfig
 {
 public bool blockAllKeysToOS { get; set; } = true;
 public string exitCombo { get; set; } = "Ctrl+Shift+Q";
 public bool alsoAllowCtrlAltDelToExit { get; set; } = true; // NOTE: cannot intercept CAD from usermode
 }
 public sealed class VisualConfig
 {
 public bool showKeyOverlay { get; set; } = true;
 public int overlayFontSize { get; set; } =200;
 public string overlayTextColor { get; set; } = "#FFFFFF";
 public string overlayBackColor { get; set; } = "#000000";
 public double overlayBackOpacity { get; set; } =0.6;
 public int overlayAutoHideMs { get; set; } =600;
 }
 public sealed class SoundConfig
 {
 public bool beepOnKey { get; set; } = true;
 public int beepFrequency { get; set; } =880;
 public int beepDurationMs { get; set; } =80;
 }

 public sealed class ActionsConfig
 {
 public Dictionary<string, KeyAction> keys { get; set; } = new();
 public Dictionary<string, KeyGroup> groups { get; set; } = new();
 }

 public sealed class KeyGroup
 {
 public List<string> keys { get; set; } = new();
 public KeyAction action { get; set; } = new();
 }

 public sealed class KeyAction
 {
 public string type { get; set; } = "none"; // showImage, playSound, animation
 public string value { get; set; } = string.Empty;
 }
}
