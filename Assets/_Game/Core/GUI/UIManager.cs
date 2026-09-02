using System.Collections.Generic;
using UnityEngine;
public enum UIID
{
    None = 0,
    HomeView,
    ShopView,
    SettingView,
    // Popups
    SettingsPopup,
    WinPopup,
    LosePopup,

    //test
    TestPopup,
    TestPopup1, TestView,
    Testview1,
}

public class UIManager : ManualSingletonMono<UIManager>
{
    [SerializeField] Transform _viewsRoot;
    [SerializeField] Transform _popupsRoot;
    [SerializeField] GameObject _raycastBlocker; //chặn click xuyên qua popup.

    private Dictionary<UIID, UIView> _allScreenView = new ();
    private Dictionary<UIID, UIPopup> _allScreenPopup = new ();

    private Stack<UIPopup> _popupStack = new ();
    public UIView currentView { get; private set; }

    #region General Functions

    private void Awake()
    {
        base.Awake();
        if (_popupsRoot != null) _popupsRoot.SetAsLastSibling();
        InitUISystem();
    }

    private void InitUISystem()
    {
        // --- XỬ LÝ VIEWS ---
        if (_viewsRoot == null)
        {
            Debug.LogError("[UIManager] _viewsRoot is not assigned!");
        }
        else
        {
            foreach (Transform child in _viewsRoot)
            {
                var view = child.GetComponent<UIView>();
                if (view != null)
                {
                    if (view.id == UIID.None)
                    {
                        Debug.LogError($"[UI Error] View '{child.name}' chưa set ID!");
                        continue;
                    }

                    if (!_allScreenView.ContainsKey(view.id))
                    {
                        _allScreenView.Add(view.id, view);

                        view.Initialize();
                        view.Hide();
                    }
                    else
                    {
                        Debug.LogError($"[UI Error] Duplicate View ID '{view.id}' on object '{view.name}'!");
                    }
                }
            }
        }

        // --- XỬ LÝ POPUPS ---
        if (_popupsRoot == null)
        {
            Debug.LogError("[UIManager] _popupsRoot is not assigned!");
        }
        else
        {
            foreach (Transform child in _popupsRoot)
            {
                var popup = child.GetComponent<UIPopup>();
                if (popup != null)
                {
                    if (popup.id == UIID.None)
                    {
                        Debug.LogError($"[UI Error] Popup '{child.name}' chưa set ID!");
                        continue;
                    }

                    if (!_allScreenPopup.ContainsKey(popup.id))
                    {
                        _allScreenPopup.Add(popup.id, popup);

                        popup.Initialize();
                        popup.Hide();
                    }
                    else
                    {
                        Debug.LogError($"[UI Error] Duplicate Popup ID '{popup.id}' on object '{popup.name}'!");
                    }
                }
            }
        }
    }

    #endregion

    #region View's Functions

    public void ShowView(UIID viewId)
    {
        if (!_allScreenView.TryGetValue(viewId, out var nextView))
        {
            Debug.LogError($"[UIManager] Không tìm thấy View ID: {viewId}");
            return;
        }

        if (currentView == nextView) return; // Tránh bật lại view đang mở

        currentView?.Hide();
        currentView = nextView;
        currentView.transform.SetAsLastSibling();
        currentView.Show();
    }

    public void CloseView(UIID viewId)
    {
        if (_allScreenView.TryGetValue(viewId, out var view))
        {
            if (view.isVisible)
            {
                view.Hide();
                if (currentView == view) currentView = null;
            }
        }
    }

    public UIView GetView(UIID viewId)
    {
        if (_allScreenView.TryGetValue(viewId, out UIView view))
        {
            return view;
        }
        Debug.LogError($"[UIManager] View {viewId} không tìm thấy trong cache!");
        return null;
    }

    #endregion

    #region Popup's Functions


    public void ShowPopup(UIID popupId, bool hidePrevious = true)
    {
        if (!_allScreenPopup.TryGetValue(popupId, out var popup))
        {
            Debug.LogError($"[UIManager] Không tìm thấy Popup ID: {popupId}");
            return;
        }

        if (hidePrevious && _popupStack.Count > 0)
            _popupStack.Peek().Hide();

        // Avoid stacking the same popup multiple times
        if (_popupStack.Contains(popup))
        {
            popup.transform.SetAsLastSibling();
            popup.Show();
            UpdateRaycastBlocker();
            return;
        }

        // Đưa lên trên cùng của Hierarchy để không bị UI khác đè
        popup.transform.SetAsLastSibling();
        popup.hidePreviousOnOpen = hidePrevious;
        popup.Show();
        _popupStack.Push(popup);
        UpdateRaycastBlocker();
    }

    public void CloseAllPopups()
    {
        while (_popupStack.Count > 0) _popupStack.Pop().Hide();

        UpdateRaycastBlocker();
    }

    public void CloseCurrentPopup()
    {
        if (_popupStack.Count > 0)
        {
            var popup = _popupStack.Pop();
            popup.Hide();

            // Hiện lại popup trước
            if (popup.hidePreviousOnOpen && _popupStack.Count > 0) 
                _popupStack.Peek().Show();
        }
        UpdateRaycastBlocker();
    }

    public UIPopup GetPopup(UIID popupId)
    {
        if (_allScreenPopup.TryGetValue(popupId, out UIPopup popup))
        {
            return popup;
        }
        Debug.LogError($"[UIManager] Popup {popupId} không tìm thấy trong cache!");
        return null;
    }

    public UIPopup GetTopPopup()
    {
        return _popupStack.Count > 0 ? _popupStack.Peek() : null;
    }

    #endregion

    #region RaycastBlock
    private void UpdateRaycastBlocker()
    {
        if (_raycastBlocker == null) return;

        bool hasPopup = _popupStack.Count > 0;
        _raycastBlocker.SetActive(hasPopup);

        if (hasPopup)
        {
            _raycastBlocker.transform.SetAsLastSibling();

            // Sau đó Popup mới nhất sẽ nhảy lên trên miếng chặn này
            _popupStack.Peek().transform.SetAsLastSibling();
        }
    }
    #endregion
}