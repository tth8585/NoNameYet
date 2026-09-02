using System.Collections.Generic;
using UnityEngine;

public sealed class UIPopupService
{
    private readonly Dictionary<UIID, UIPopup> _popups = new();
    private readonly List<UIPopup> _stack = new();
    private readonly GameObject _inputBlocker;

    public UIPopup Top => _stack.Count == 0 ? null : _stack[_stack.Count - 1];
    public bool HasOpenPopup => _stack.Count > 0;

    public UIPopupService(GameObject inputBlocker)
    {
        _inputBlocker = inputBlocker;
    }

    public void Register(Transform root)
    {
        _popups.Clear();
        if (root == null)
            return;

        foreach (UIPopup popup in root.GetComponentsInChildren<UIPopup>(true))
        {
            if (!Register(popup))
                continue;

            popup.Initialize();
            popup.Hide();
        }
    }

    public bool Register(UIPopup popup)
    {
        if (popup == null || popup.id == UIID.None)
            return false;

        if (_popups.ContainsKey(popup.id))
        {
            Debug.LogError($"[UIPopupService] Duplicate popup ID: {popup.id}");
            return false;
        }

        _popups.Add(popup.id, popup);
        return true;
    }

    public bool Open(UIID id, object data = null, bool hidePrevious = true)
    {
        if (!_popups.TryGetValue(id, out UIPopup popup))
        {
            Debug.LogError($"[UIPopupService] Popup not found: {id}");
            return false;
        }

        int existingIndex = _stack.IndexOf(popup);
        if (existingIndex >= 0)
        {
            _stack.RemoveAt(existingIndex);
            popup.Show(data);
            _stack.Add(popup);
            RefreshBlocker();
            return true;
        }

        if (hidePrevious && Top != null)
            Top.Hide();

        popup.hidePreviousOnOpen = hidePrevious;
        popup.transform.SetAsLastSibling();
        popup.Show(data);
        _stack.Add(popup);
        RefreshBlocker();
        return true;
    }

    public void CloseTop()
    {
        if (Top == null)
            return;

        UIPopup popup = Top;
        _stack.RemoveAt(_stack.Count - 1);
        popup.Hide();

        if (popup.hidePreviousOnOpen && Top != null)
            Top.Show();

        RefreshBlocker();
    }

    public void CloseAll()
    {
        foreach (UIPopup popup in _stack)
            popup.Hide();

        _stack.Clear();
        RefreshBlocker();
    }

    public UIPopup Get(UIID id)
    {
        _popups.TryGetValue(id, out UIPopup popup);
        return popup;
    }

    private void RefreshBlocker()
    {
        if (_inputBlocker == null)
            return;

        _inputBlocker.SetActive(HasOpenPopup);
        if (!HasOpenPopup)
            return;

        _inputBlocker.transform.SetAsLastSibling();
        Top.transform.SetAsLastSibling();
    }
}
