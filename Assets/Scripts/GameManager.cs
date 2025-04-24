using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    // different for every round?
    public float matchDuration = 180f;
    private float timer = 0f;
    private bool matchRunning = false;

    public int currentLevel = 1;
    private Dictionary<string, PlayerManager> players = new();
    public InstructionSet currentLevelInstructions;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Update()
    {
        if (matchRunning)
        {
            timer += Time.deltaTime;
            if (timer >= matchDuration)
            {
                EndMatch();
            }
        }
        else
        {
            timer = 0f;
        }
    }

    public void RegisterPlayer(string playerId)
    {
        if (!players.ContainsKey(playerId))
        {
            players[playerId] = new PlayerManager { playerId = playerId };
        }
        players[playerId].StartLevel(currentLevel);
    }

    public void AddScore(string playerId, int amount) =>
        players[playerId].AddScore(currentLevel, amount);


    public void StartMatch()
    {

        foreach (var player in players.Values)
        {
            player.StartLevel(currentLevel);
        }

        Debug.Log($"Level {currentLevel} started!");
    }

    public void EndMatch()
    {
        matchRunning = false;

        foreach (var player in players.Values)
        {
            player.RecordFinalTime(currentLevel, timer);
        }

        Debug.Log($"Level {currentLevel} complete!");
        foreach (var player in players)
        {
            var stats = player.Value.GetLevelStats(currentLevel);
            Debug.Log($"{player.Key} - Score: {stats.score}, Time: {stats.finalTime}");
        }
    }

    public Instruction GetInstruction(int index)
    {
        return currentLevelInstructions.instructions[index];
    }

    public int GetInstructionCount()
    {
        return currentLevelInstructions.instructions.Count;
    }
}
