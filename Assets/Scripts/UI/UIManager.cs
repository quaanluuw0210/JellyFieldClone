using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [SerializeField] private TargetUIManager targetUIManager;

    public TargetUIManager TargetUIManager => targetUIManager;

    public void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this.gameObject);
        }    
    }

    public void SetUpUI(LevelData level)
    {
        targetUIManager.InitializeUI(level);
    }    
}
