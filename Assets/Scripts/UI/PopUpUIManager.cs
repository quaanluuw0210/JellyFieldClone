using TMPro;
using UnityEngine;

public class PopUpUIManager : MonoBehaviour
{
    [SerializeField] private TMP_Text text;
    [SerializeField] private GameObject nextLevelButton;
    [SerializeField] private GameObject replayButton;
    [SerializeField] private GameObject popUpUI;

    void Start()
    {

    }

    void Update()
    {

    }

    public void PlayVictoryUI()
    {
        OpenUI();
        text.text = "VICTORY";
        text.color = Color.yellow;

        nextLevelButton.SetActive(true); 
        replayButton.SetActive(true);    
    }

    public void PlayLossUI()
    {
        OpenUI();
        text.text = "DEFEAT";     
        text.color = Color.red;   

        nextLevelButton.SetActive(false); 
        replayButton.SetActive(true);     
    }

    public void OpenUI()
    {
        popUpUI.SetActive(true);
    }

    public void CloseUI()
    {
        popUpUI.SetActive(false);
    }
}