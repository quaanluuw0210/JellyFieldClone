using UnityEngine;

public class MainMenuManager : MonoBehaviour
{
    [SerializeField] private GameObject mainMenu;

    [SerializeField] private GameObject quickChoose;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void PlayGame()
    {
        mainMenu.SetActive(false);
        quickChoose.SetActive(false);
    }    

    public void BackMainMenu()
    {
        mainMenu.SetActive(true);
        quickChoose.SetActive(false);
    }    
    public void QuickChooseUI()
    {
        quickChoose.SetActive(true);
        mainMenu.SetActive(false);
    }    
    public void CloseQuickChooseUI()
    {
        quickChoose.SetActive(false);
        mainMenu.SetActive(true);
    }    
}
