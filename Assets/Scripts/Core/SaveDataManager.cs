using UnityEngine;

public class SaveDataManager : MonoBehaviour
{
    public static SaveDataManager Instance;
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
    public void Start()
    {
        
    }

    public void LoadGamePlayScene()
    {
        
    }    
}
