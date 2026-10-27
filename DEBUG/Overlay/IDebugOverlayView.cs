// Only view-specific controls/snapshot values vary; common tools live in DebugOverlay.
using Godot;
using System.Collections.Generic;

public interface IDebugOverlayView
{
    void BuildControls(VBoxContainer environmentRows, VBoxContainer root);
    void WriteSnapshot(Dictionary<string, object> values);
}
