using UnityEngine;

public class PauseGameUI : MonoBehaviour
{
    [SerializeField] private GameObject pauseGameUI;
    [SerializeField] private GameObject backgroundGameUI;
    public void PauseGame()
    {
        pauseGameUI.SetActive(true);
        backgroundGameUI.SetActive(true);
    }   
    
    public void Resume()
    {
        pauseGameUI.SetActive(false);
        backgroundGameUI.SetActive(false);
    }    
}
