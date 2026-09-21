using UnityEngine;

public class AppManager : MonoBehaviour
{
    public static AppManager Instance;
    public void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this.gameObject);
        }
        DontDestroyOnLoad(gameObject);
    }
}
