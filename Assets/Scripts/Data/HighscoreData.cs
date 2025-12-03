using System;
using System.Collections.Generic;

[Serializable]
public class HighscoreTable
{
    public List<HighscoreEntry> entries = new List<HighscoreEntry>();
}

[Serializable]
public class HighscoreEntry
{
    public string playerName;
    public int score;
    public string date;
}