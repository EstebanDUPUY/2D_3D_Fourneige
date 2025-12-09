using UnityEngine;

public class PlayerNameManager : MonoBehaviour
{
    public void SaveName(string playerName)
    {
        PlayerPrefs.SetString("PlayerName", playerName);
        PlayerPrefs.Save();
    }

    public string GetName()
    {
        return PlayerPrefs.GetString("PlayerName", "Unknown");
    }
}
