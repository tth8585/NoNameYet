using TMPro;
using UnityEngine;

public class TestView1:UIView
{
    [SerializeField] private TMP_InputField inputField;
    public override void Initialize()
    {
        base.Initialize();
        Debug.LogError("Init View " + this.GetType().ToString());
    }
    public void OpenNextView()
    {
        UIManager.Instance.ShowView(UIID.TestView);
    }
    public void OpenFirstPopup()
    {
        UIManager.Instance.ShowPopup(UIID.TestPopup);
    }

}
