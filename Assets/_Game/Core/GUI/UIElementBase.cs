using UnityEngine;

public abstract class UIElementBase : MonoBehaviour
{
    [SerializeField] public UIID id = UIID.None;

    private CanvasGroup _canvasGroup;
    private bool _initialized;

    public bool isVisible { get; private set; }
    public bool IsVisible => isVisible;

    public virtual void Initialize()
    {
        if (_initialized)
            return;

        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();

        gameObject.SetActive(true);
        _canvasGroup.alpha = 0f;
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;
        isVisible = false;
        _initialized = true;
    }

    public virtual void Show(object data = null)
    {
        if (!_initialized)
            Initialize();

        if (isVisible)
            return;

        isVisible = true;
        _canvasGroup.alpha = 1f;
        _canvasGroup.interactable = true;
        _canvasGroup.blocksRaycasts = true;
        gameObject.SetActive(true);
        OnShown();
    }

    public virtual void Hide()
    {
        if (!_initialized)
            Initialize();

        isVisible = false;
        _canvasGroup.alpha = 0f;
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;
        OnHide();
    }

    public virtual void OnShown() { }
    public virtual void OnHide() { }
}
