using UnityEngine;

public class PauseGameUI : MonoBehaviour
{
    [SerializeField] private GameObject pauseGameUI;

    public void PauseGame()
    {
        pauseGameUI.SetActive(true);
    }   
    
    public void Resume()
    {
        pauseGameUI.SetActive(false);
    }    
}
