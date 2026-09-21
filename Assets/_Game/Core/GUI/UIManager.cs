using System.Collections.Generic;
using UnityEngine;
public enum UIID
{
    None = 0,
    HomeView,
    ShopView,
    SettingView,
    BattleView,
    // Popups
    SettingsPopup,
    WinPopup,
    LosePopup,

    CharacterPanelView,
    InventoryView
}

public class UIManager : ManualSingletonMono<UIManager>
{
    [SerializeField] Transform _viewsRoot;
    [SerializeField] Transform _popupsRoot;
    [SerializeField] GameObject _raycastBlocker; //chặn click xuyên qua popup.

    private UIScreenService _screenService;
    private UIPopupService _popupService;
    public UIView currentView => _screenService?.Current;

    #region General Functions

    public override void Awake()
    {
        base.Awake();
        if (_popupsRoot != null) _popupsRoot.SetAsLastSibling();
        _screenService = new UIScreenService();
        _popupService = new UIPopupService(_raycastBlocker);
        _screenService.Register(_viewsRoot);
        _popupService.Register(_popupsRoot);
    }

    #endregion

    #region View's Functions

    public void ShowView(UIID viewId)
    {
        _screenService.Show(viewId);
    }

    public void CloseView(UIID viewId)
    {
        if (currentView != null && currentView.id == viewId)
            _screenService.CloseCurrent();
    }

    public UIView GetView(UIID viewId)
    {
        return _screenService.Get(viewId);
    }

    #endregion

    #region Popup's Functions


    public void ShowPopup(UIID popupId, bool hidePrevious = true)
    {
        _popupService.Open(popupId, null, hidePrevious);
    }

    public void CloseAllPopups()
    {
        _popupService.CloseAll();
    }

    public void CloseCurrentPopup()
    {
        _popupService.CloseTop();
    }

    public UIPopup GetPopup(UIID popupId)
    {
        return _popupService.Get(popupId);
    }

    public UIPopup GetTopPopup()
    {
        return _popupService.Top;
    }

    #endregion

}