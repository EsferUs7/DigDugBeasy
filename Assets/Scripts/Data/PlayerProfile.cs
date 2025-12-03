using System;

[Serializable]
public class PlayerProfile
{
    public string playerName = "Player";
    public int totalScore;
    public CampaignProgress[] campaignProgress;
    public CustomLevel[] customLevels;
}

[Serializable]
public class CampaignProgress
{
    public string levelId;
    public bool completed;
    public int bestScore;
    public int bestTime;
}

[Serializable]
public class CustomLevel
{
    public string levelName;
    public string levelDataPath;
    public DateTime creationDate;
}