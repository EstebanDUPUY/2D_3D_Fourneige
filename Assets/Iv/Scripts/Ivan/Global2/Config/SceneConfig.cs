using UnityEngine;

[CreateAssetMenu(fileName = "SceneConfig", menuName = "Config/Scene Config")]
public class SceneConfig : ScriptableObject
{
    [Header("Home Scene")]
    public string homeScene;

    [Header("Niveaux du jeu")]
    public string[] levels;

    [Header("Scenes Utilitaires")]
    public string[] usefullLevels;

    [Header("Key PlayerPrefs")]
    public string keyTopScoreLevels = "TopScoreLevels_";
}
