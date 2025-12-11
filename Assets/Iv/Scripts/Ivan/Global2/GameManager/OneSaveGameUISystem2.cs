using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OneSaveGameUISystem2 : MonoBehaviour
{
    Game game; // La save assignée à ce slot
    int slotIndex; // L’indice du slot (0,1,2)

    [SerializeField]
    Button loadThisGameButton;

    [SerializeField]
    Button deleteGameButton;

    [SerializeField]
    TextMeshProUGUI playerNameText;

    public Action<Game> OnLoadGame; // Callback quand le joueur clique
    public Action<int> OnDeleteGame; // Callback quand on supprime la save

    void Awake()
    {
        loadThisGameButton.onClick.AddListener(() =>
        {
            OnLoadGame?.Invoke(game);
        });

        deleteGameButton.onClick.AddListener(() =>
        {
            OnDeleteGame?.Invoke(slotIndex);
        });
    }

    public void SetGame(Game game, int index)
    {
        this.game = game;
        this.slotIndex = index;
        RefreshUI();
    }

    void RefreshUI()
    {
        if (game == null)
        {
            playerNameText.text = "— Vide —";
            loadThisGameButton.interactable = false;
            deleteGameButton.interactable = false;
        }
        else
        {
            playerNameText.text = game.playerName;
            loadThisGameButton.interactable = true;
            deleteGameButton.interactable = true;
        }
    }
}
