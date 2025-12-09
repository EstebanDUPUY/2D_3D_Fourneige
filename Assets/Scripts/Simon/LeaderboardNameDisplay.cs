using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Globalization;

public class LeaderboardNameDisplay : MonoBehaviour
{
    public TMP_InputField inputPseudo; // Le champ où le joueur tape son nom
    public TextMeshProUGUI nameText; // le Text qui affiche le nom

    public void ValiderPseudo()
    {
        string pseudo = inputPseudo.text;  // Récupère ce que le joueur a écrit
        nameText.text = pseudo;            // L’affiche dans le Text
        
        // (optionnel) sauvegarder
        PlayerPrefs.SetString("PlayerName", pseudo);
    }

}

//Ce script récupère le nom sauvegardé et l’affiche quand le leaderboard apparaît.
//PlayerPrefs.GetString("PlayerName") pour afficher name dans UI.