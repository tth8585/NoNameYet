using TMPro;
using UnityEngine;
using UnityEngine.UIElements;

public class TestView : UIView
{
    [SerializeField] private TextMeshProUGUI txtInfo;
    
    public override void Initialize()
    {
        base.Initialize();
        Debug.LogError("Init View "+this.GetType().ToString());
    }

    public void CloseView()
    {
        UIManager.Instance.CloseView(id);
        UIManager.Instance.ShowView(UIID.Testview1);
    }

    public override void OnShown()
    {
    }
}
