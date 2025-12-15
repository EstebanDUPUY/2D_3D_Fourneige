using System;
using System.Collections.Generic;

[Serializable]
public class Game
{
    public string playerName;
    public int levelReached;
    public int totalScore;
    public List<int> levelScores;

    public Game()
    {
        levelScores = new List<int>();
    }
}
