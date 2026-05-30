using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Forwards UI pointer enter/exit events to a shared hover controller.
/// </summary>
public sealed class UIPointerHoverRelay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField]
    private DeterministicSelectionHoverController _hoverController;

    public void OnPointerEnter(PointerEventData eventData)
    {
        _hoverController?.NotifyPointerEnter();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _hoverController?.NotifyPointerExit();
    }
}
