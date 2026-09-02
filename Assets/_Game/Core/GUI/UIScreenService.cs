using System.Collections.Generic;
using UnityEngine;

public sealed class UIScreenService
{
    private readonly Dictionary<UIID, UIView> _screens = new();
    private readonly Stack<UIID> _history = new();

    public UIView Current { get; private set; }

    public void Register(Transform root)
    {
        _screens.Clear();
        if (root == null)
            return;

        foreach (UIView screen in root.GetComponentsInChildren<UIView>(true))
        {
            if (!Register(screen))
                continue;

            screen.Initialize();
            screen.Hide();
        }
    }

    public bool Register(UIView screen)
    {
        if (screen == null || screen.id == UIID.None)
            return false;

        if (_screens.ContainsKey(screen.id))
        {
            Debug.LogError($"[UIScreenService] Duplicate screen ID: {screen.id}");
            return false;
        }

        _screens.Add(screen.id, screen);
        return true;
    }

    public bool Show(UIID id, object data = null, bool remember = true)
    {
        if (!_screens.TryGetValue(id, out UIView next))
        {
            Debug.LogError($"[UIScreenService] Screen not found: {id}");
            return false;
        }

        if (Current == next)
        {
            next.Show(data);
            return true;
        }

        if (remember && Current != null)
            _history.Push(Current.id);

        Current?.Hide();
        Current = next;
        Current.transform.SetAsLastSibling();
        Current.Show(data);
        return true;
    }

    public bool GoBack()
    {
        while (_history.Count > 0)
        {
            UIID previous = _history.Pop();
            if (_screens.ContainsKey(previous))
                return Show(previous, null, false);
        }

        return false;
    }

    public void CloseCurrent()
    {
        Current?.Hide();
        Current = null;
    }

    public UIView Get(UIID id)
    {
        _screens.TryGetValue(id, out UIView screen);
        return screen;
    }
}
