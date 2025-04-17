using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// add penalties??
public class LevelStats
{
    public int levelNumber;
    public int score;
    public float finalTime;
}

public class PlayerManager
{
    public string playerId;
    private Dictionary<int, LevelStats> levelStats = new();

    public void StartLevel(int levelNumber)
    {
        if (!levelStats.ContainsKey(levelNumber))
        {
            levelStats[levelNumber] = new LevelStats
            {
                levelNumber = levelNumber,
                score = 0,
                finalTime = 0f,
            };
        }
    }

    public void AddScore(int levelNumber, int amount)
    {
        if (levelStats.ContainsKey(levelNumber))
        {
            levelStats[levelNumber].score += amount;
        }
    }

    public void RecordFinalTime(int levelNumber, float time)
    {
        if (levelStats.ContainsKey(levelNumber))
        {
            levelStats[levelNumber].finalTime = time;
        }
    }

    public LevelStats GetLevelStats(int levelNumber)
    {
        if (levelStats.ContainsKey(levelNumber))
        {
            return levelStats[levelNumber];
        }

        return null;
    }
}

