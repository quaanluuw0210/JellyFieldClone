using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TargetUIItem : MonoBehaviour
{
    [SerializeField] private Image colorIcon;
    [SerializeField] private TMP_Text scoreText;


    public JellyColor Color { get; private set; }

    public void Setup(JellyColor color, Sprite iconSprite, int targetScore)
    {
        Color = color;
        if (colorIcon != null && iconSprite != null)
        {
            colorIcon.sprite = iconSprite;
        }

        UpdateScore(targetScore);
    }

    public void UpdateScore(int currentScore)
    {
        if (currentScore <= 0)
        {
            if (scoreText != null) scoreText.text = "0";
         
        }
        else
        {
            if (scoreText != null) scoreText.text = currentScore.ToString();
        }
    }
}