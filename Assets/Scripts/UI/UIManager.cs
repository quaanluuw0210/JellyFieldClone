using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [SerializeField] private TargetUIManager targetUIManager;
    [SerializeField] private PauseGameUI pauseGameUI;
    [SerializeField] private PopUpUIManager popUpUIManager;
    [SerializeField] private MainMenuManager mainMenuManager;   

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
    public void PauseGame()
    {
        pauseGameUI.PauseGame();
    }    
    public void ResumeGame()
    {
        pauseGameUI.Resume();
    }    
    public void VictoryUI()
    {
        popUpUIManager.PlayVictoryUI();
    }    
    public void LossUI()
    {
        popUpUIManager.PlayLossUI();
    }    

    public void ClosePopUpUI()
    {
        popUpUIManager.CloseUI();
    }  
    
    public void StartGame()
    {
        mainMenuManager.PlayGame();
    }    
    public void BackToMainMenu()
    {
        ClosePopUpUI();
        ResumeGame();
        mainMenuManager.BackMainMenu();
    }    

    public void CloseUIWithNotGamePlay()
    {
        ClosePopUpUI();
        ResumeGame();
    }    
}
