using UnityEngine;
using UnityEngine.UI;

public class UIView:MonoBehaviour
{
    [SerializeField] public UIID id;
    [HideInInspector] public bool isVisible = false;
    private CanvasGroup _canvasGroup;

    private bool _isInitialized = false;

    public virtual void Initialize()
    {
        if (_isInitialized) return;

        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null) Debug.LogError($"[UI Error] {gameObject.name} thiếu CanvasGroup!");
        gameObject.SetActive(true);
        _isInitialized = true;
       
    }
    public virtual void Show(object data = null)
    {
        // Bật CanvasGroup (nếu có)
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 1;
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.interactable = true;
        }
        isVisible = true;
        OnShown();
    }

    public virtual void Hide()
    {
        // Tắt CanvasGroup (nếu có)
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 0;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;
        }

        isVisible = false;
        OnHide();
    }
    public virtual void OnShown()
    {

    }

    public virtual void OnHide()
    {

    }
}
