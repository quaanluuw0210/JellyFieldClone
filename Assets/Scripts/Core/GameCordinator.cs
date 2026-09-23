using UnityEngine;

public class GameCordinator : MonoBehaviour
{

    public void Awake()
    {
    }

    public void StartGame()
    {
        GameManager.instance.PlayCurrentLevel();
        UIManager.Instance.StartGame();
    }    

    public void RePlay()
    {
        GameManager.instance.RestartLevel();
        UIManager.Instance.CloseUIWithNotGamePlay();
    }    
    public void PlayNextLevel()
    {
        GameManager.instance.NextLevel();
        UIManager.Instance.CloseUIWithNotGamePlay();
    }    
    public void PauseGame()
    {
        UIManager.Instance.PauseGame();
    }    
    public void ResumeGame()
    {
        UIManager.Instance.ResumeGame();
    }    
    public void BackToMainMenu()
    {
        UIManager.Instance.BackToMainMenu();
    }    
}
