using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Selectable))]
public class MenuItemHighlight : MonoBehaviour,
    ISelectHandler, IDeselectHandler,
    IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] Image  highlightImage;
    [SerializeField] Color  normalColor    = new Color(1f, 1f, 1f, 0f);
    [SerializeField] Color  highlightColor = new Color(1f, 1f, 1f, 0.12f);

    bool _hovered;
    bool _selected;

    void Start() => Refresh();

    public void OnSelect      (BaseEventData _) { _selected = true;  Refresh(); }
    public void OnDeselect    (BaseEventData _) { _selected = false; Refresh(); }
    public void OnPointerEnter(PointerEventData _) { _hovered = true;  Refresh(); }
    public void OnPointerExit (PointerEventData _) { _hovered = false; Refresh(); }

    void Refresh()
    {
        if (highlightImage == null) return;
        highlightImage.color = (_hovered || _selected) ? highlightColor : normalColor;
    }
}
