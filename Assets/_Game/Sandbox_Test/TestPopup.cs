using TMPro;
using UnityEngine;

public class TestPopup : UIPopup
{
    public TextMeshProUGUI txtInfo;
    public override void Initialize()
    {
        base.Initialize();
        Debug.LogError("Init Popup " + this.GetType().ToString());
    }
    public void ClosePopup()
    {
        UIManager.Instance.CloseCurrentPopup();
    }

    public void CloseAllPopup()
    {
        UIManager.Instance.CloseAllPopups();
    }

    public void OpenNextPopup()
    {
        UIManager.Instance.ShowPopup(UIID.TestPopup1);
    }
    public override void OnShown()
    {
        base.OnShown();
    }
}
