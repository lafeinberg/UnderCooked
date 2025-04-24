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

public class PlayerManager : MonoBehaviour
{
    public string playerId;
    private Dictionary<int, LevelStats> levelStats = new();
    private int currentInstructionIndex = 0;
    private int currentLevel;

    private GameManager gameManager;

    void Start()
    {
        gameManager = FindObjectOfType<GameManager>();
    }

    public void StartLevel(int levelNumber)
    {
        currentLevel = levelNumber;

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

    public void OnPlayerAction(InstructionType actionType, string target)
    {
        var instruction = gameManager.GetInstruction(currentInstructionIndex);

        if (instruction.type == actionType && instruction.targetObject == target)
        {
            Debug.Log($"player {playerId} completed: {instruction.description}");

            currentInstructionIndex++;

            if (currentInstructionIndex >= gameManager.GetInstructionCount())
            {
                Debug.Log($"player {playerId} completed level {currentLevel}");
                // todo: trigger level complete player logic
            }
            else
            {
                Debug.Log($"Next step: {gameManager.GetInstruction(currentInstructionIndex).description}");
            }
        }
    }

}

