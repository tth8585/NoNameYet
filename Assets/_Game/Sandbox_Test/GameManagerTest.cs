using UnityEngine;
using UnityEngine.InputSystem;

public class GameManagerTest : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

        //UIManager.Instance.ShowView(UIID.HomeView);
    }

    // Update is called once per frame
    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.iKey.wasPressedThisFrame)
        {
            UIManager.Instance.ShowView(UIID.CharacterPanelView);
        }
        else if (Keyboard.current != null && Keyboard.current.bKey.wasPressedThisFrame)
        {
            UIManager.Instance.ShowView(UIID.InventoryView);
        }
        else if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            UIManager.Instance.CloseView(UIID.CharacterPanelView);
            UIManager.Instance.CloseView(UIID.InventoryView);
        }
    }
}
