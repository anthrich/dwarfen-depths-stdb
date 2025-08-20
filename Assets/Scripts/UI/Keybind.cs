using UnityEngine;
using UnityEngine.UIElements;

[UxmlElement]
public partial class KeybindElement : VisualElement
{
    [UxmlAttribute]
    public string Key { get; set; } = "W";

    public KeybindElement()
    {
        var template = Resources.Load<VisualTreeAsset>("Keybind");
        template.CloneTree(this);
        RegisterCallback<AttachToPanelEvent>(UpdateContent);
    }

    private void UpdateContent(AttachToPanelEvent evt)
    {
        this.Q<Label>("Key").text = Key;
    }
}
