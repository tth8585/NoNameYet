using UnityEngine;

public class GameManagerTest : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

        UIManager.Instance.ShowView(UIID.HomeView);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
