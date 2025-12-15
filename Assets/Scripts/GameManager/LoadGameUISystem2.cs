using UnityEngine;
using UnityEngine.UI;

public class LoadGameUISystem2 : MonoBehaviour
{
    [SerializeField]
    OneSaveGameUISystem2[] slots; // tes 3 slots UI

    [Header("Confirm Delete")]
    [SerializeField]
    GameObject confirmDeleteGamePanel;

    [SerializeField]
    Button yesDeleteGameButton;

    [SerializeField]
    Button noDeleteGameButton;

    int slotToDelete = -1;

    void OnEnable()
    {
        LoadAllGames();

        // Boutons de confirmation
        yesDeleteGameButton.onClick.AddListener(() =>
        {
            if (slotToDelete != -1)
                DeleteGame(slotToDelete);

            confirmDeleteGamePanel.SetActive(false);
            slotToDelete = -1;
        });

        noDeleteGameButton.onClick.AddListener(() =>
        {
            slotToDelete = -1;
            confirmDeleteGamePanel.SetActive(false);
        });
    }

    void LoadAllGames()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            Game g = LoadGameFromSlot(i);
            slots[i].SetGame(g, i);

            int index = i; // nécessaire pour la lambda
            slots[i].OnLoadGame += (game) =>
            {
                GameManager2.instance.LoadGame(game);
            };

            slots[i].OnDeleteGame += (slotIdx) =>
            {
                // Afficher le panel de confirmation
                slotToDelete = slotIdx;
                confirmDeleteGamePanel.SetActive(true);
            };
        }
    }

    Game LoadGameFromSlot(int index)
    {
        if (!PlayerPrefs.HasKey("Slot" + index + "_PlayerName"))
            return null;

        Game g = new Game
        {
            playerName = PlayerPrefs.GetString("Slot" + index + "_PlayerName"),
            levelReached = PlayerPrefs.GetInt("Slot" + index + "_Level"),
            totalScore = PlayerPrefs.GetInt("Slot" + index + "_Score")
        };
        return g;
    }

    void DeleteGame(int slotIndex)
    {
        // Supprimer les données PlayerPrefs
        PlayerPrefs.DeleteKey("Slot" + slotIndex + "_PlayerName");
        PlayerPrefs.DeleteKey("Slot" + slotIndex + "_Level");
        PlayerPrefs.DeleteKey("Slot" + slotIndex + "_Score");
        PlayerPrefs.Save();

        Debug.Log("Slot " + slotIndex + " supprimé !");

        // Réorganiser les slots pour que les vides soient en bas
        CompactSlots();
    }

    void CompactSlots()
    {
        Game[] games = new Game[slots.Length];
        int count = 0;

        // Récupérer toutes les games existantes
        for (int i = 0; i < slots.Length; i++)
        {
            Game g = LoadGameFromSlot(i);
            if (g != null)
            {
                games[count] = g;
                count++;
            }
        }

        // Réécrire les slots dans l'ordre
        for (int i = 0; i < slots.Length; i++)
        {
            if (i < count)
            {
                PlayerPrefs.SetString("Slot" + i + "_PlayerName", games[i].playerName);
                PlayerPrefs.SetInt("Slot" + i + "_Level", games[i].levelReached);
                PlayerPrefs.SetInt("Slot" + i + "_Score", games[i].totalScore);
                slots[i].SetGame(games[i], i);
            }
            else
            {
                PlayerPrefs.DeleteKey("Slot" + i + "_PlayerName");
                PlayerPrefs.DeleteKey("Slot" + i + "_Level");
                PlayerPrefs.DeleteKey("Slot" + i + "_Score");
                slots[i].SetGame(null, i);
            }
        }

        PlayerPrefs.Save();
    }
}
