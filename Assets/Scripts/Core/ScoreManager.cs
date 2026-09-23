using System.Collections.Generic;
using Unity.Jobs;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;

    private Dictionary<JellyColor, int> scoreDict;

    public void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void Start()
    {
        scoreDict = new Dictionary<JellyColor, int>();
    }

    public void InitLevelScores(LevelData levelData)
    {
        scoreDict.Clear();

        if (levelData == null || levelData.targetScores == null) return;

        foreach (var target in levelData.targetScores)
        {
            if (!scoreDict.ContainsKey(target.color))
            {
                scoreDict.Add(target.color, target.requiredScore);
            }
        }
    }

    public void AddRequired(JellyColor color,int score)
    {
        if(scoreDict == null)
        {
            return;
        }
        if(scoreDict.ContainsKey(color))
        {
            scoreDict[color] = score;
        }
        else
        {
            scoreDict.Add(color, score);
        }
    }
    public void RemoveColorScore(JellyColor color,int score)
    {
        if(scoreDict == null||scoreDict.ContainsKey(color)==false)
        {
            return;
        }
        if (scoreDict[color] <score||score<0)
        {
            return;
        }
        scoreDict[color]-=score;

        UIManager.Instance.TargetUIManager.UpdateTargetScore(color, scoreDict[color]);
    }

    public bool IsPlayerWin()
    {
       
        if (scoreDict == null || scoreDict.Count == 0)
        {
            return false;
        }


        foreach (var score in scoreDict.Values)
        {
            
            if (score > 0)
            {
                return false;
            }
        }
        return true;
    }
    public int GetScoreOfColor(JellyColor color)
    {
        if(scoreDict.ContainsKey(color)==false)
        {
            return -1;
        }
        return scoreDict[color];
    }
}
