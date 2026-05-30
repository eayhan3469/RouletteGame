using System.Collections;
using UnityEngine;

/// <summary>
/// Expands a deterministic selection panel while the pointer is hovering
/// over either the compact trigger area or the expanded content.
/// </summary>
public sealed class DeterministicSelectionHoverController : MonoBehaviour
{
    [SerializeField]
    private GameObject _expandedPanel;

    [SerializeField]
    [Min(0f)]
    private float _closeDelay = 0.05f;

    private int _hoverSourceCount;
    private Coroutine _hideRoutine;

    private void OnEnable()
    {
        _hoverSourceCount = 0;
        SetExpanded(false);
    }

    private void OnDisable()
    {
        _hoverSourceCount = 0;

        if (_hideRoutine != null)
        {
            StopCoroutine(_hideRoutine);
            _hideRoutine = null;
        }

        SetExpanded(false);
    }

    public void NotifyPointerEnter()
    {
        _hoverSourceCount++;

        if (_hideRoutine != null)
        {
            StopCoroutine(_hideRoutine);
            _hideRoutine = null;
        }

        SetExpanded(true);
    }

    public void NotifyPointerExit()
    {
        _hoverSourceCount = Mathf.Max(0, _hoverSourceCount - 1);

        if (_hoverSourceCount > 0)
        {
            return;
        }

        if (_hideRoutine != null)
        {
            StopCoroutine(_hideRoutine);
        }

        _hideRoutine = StartCoroutine(HideAfterDelay());
    }

    private IEnumerator HideAfterDelay()
    {
        if (_closeDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(_closeDelay);
        }
        else
        {
            yield return null;
        }

        _hideRoutine = null;

        if (_hoverSourceCount == 0)
        {
            SetExpanded(false);
        }
    }

    private void SetExpanded(bool isExpanded)
    {
        if (_expandedPanel == null)
        {
            Debug.LogWarning("DeterministicSelectionHoverController is missing the expanded panel reference.");
            return;
        }

        _expandedPanel.SetActive(isExpanded);
    }
}
