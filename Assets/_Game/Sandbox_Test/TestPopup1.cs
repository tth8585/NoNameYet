using UnityEngine;

public class TestPopup1 : UIPopup
{
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

}
