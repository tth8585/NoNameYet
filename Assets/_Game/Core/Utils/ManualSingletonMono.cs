using UnityEngine;

public class ManualSingletonMono<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T instance;
    public static T Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindAnyObjectByType<T>(FindObjectsInactive.Include);

                if (instance == null)
                    Debug.LogError($"Singleton-Cannot find Object with type {typeof(T)}");
            }

            return instance;
        }
    }
    public virtual void Awake()
    {
        if (instance == null)
        {
            instance = (T)(MonoBehaviour)this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }
    protected virtual void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
            Debug.Log($"Destroy Instance {this.GetType()}");
        }
    }
    protected virtual void OnApplicationQuit()
    {
        instance = null;
        Debug.Log($"Destroy Instance {this.GetType()}");
    }
}