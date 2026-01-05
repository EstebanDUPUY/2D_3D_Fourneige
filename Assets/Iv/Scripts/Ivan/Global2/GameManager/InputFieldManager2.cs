using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InputFieldManager2 : MonoBehaviour
{
    [Header("Références UI")]
    public TMP_InputField nameInputField;
    public TextMeshProUGUI errorTextTMPro;
    public Button validateButton;
    public TextMeshProUGUI NameForGameTMPro;

    [Header("Caractéristiques")]
    public int maxLength = 20;
    public int minLength = 3;

    [Header("Nom de la clé PlayerPrefs")]
    public string keyPlayerName = "PlayerName";

    public static Action ValidateName;

    private char[] forbiddenChars =
    {
        '@',
        '#',
        '$',
        '%',
        '&',
        '*',
        '/',
        '\\',
        '"',
        '\'',
        ':',
        '?',
        '!',
        ';',
        '§',
        '{',
        '}',
        '[',
        ']',
        '(',
        ')',
        '+',
        '-',
        '|',
        '²',
        '¤',
        '£',
        '€',
        'µ',
        'ù',
        '=',
        ',',
        '`',
    };

    void Awake()
    {
        validateButton.onClick.AddListener(SaveInput);
    }

    public void SaveInput()
    {
        string text = nameInputField.text;

        // 1) Champ vide ?
        if (string.IsNullOrWhiteSpace(text))
        {
            ShowError("Le nom ne peut pas être vide !");
            return;
        }

        // 2) Taille minimale
        if (text.Length < minLength)
        {
            ShowError("Minimum " + minLength + " caractères !");
            return;
        }

        // 3) Taille maximale
        if (text.Length > maxLength)
        {
            ShowError("Maximum " + maxLength + " caractères !");
            return;
        }

        // 4) Caractères interdits
        foreach (char c in forbiddenChars)
        {
            if (text.Contains(c))
            {
                ShowError("Caractère interdit : " + c);
                return;
            }
        }

        // 5) Vérifier si le nom existe déjà
        if (NameExists(text))
        {
            ShowError("Ce nom existe déjà !");
            return;
        }

        // 6) Sauvegarder
        ShowError(""); // retire l’erreur
        Debug.Log("Nom sauvegardé : " + text);
        if (NameForGameTMPro != null)
            NameForGameTMPro.text = text;
        GameManager2.instance.playerName = text;
        ValidateName?.Invoke();
        // GameManager2.instance.GoToFirstScene();
    }

    // --- Vérifie si le nom existe déjà ---
    bool NameExists(string name)
    {
        string list = PlayerPrefs.GetString("AllPlayerNames", "");
        string[] names = list.Split(';').Where(n => n != "").ToArray();
        return names.Contains(name);
    }

    // --- Ajoute un nom dans la liste globale ---
    void AddNameToList(string name)
    {
        string list = PlayerPrefs.GetString("AllPlayerNames", "");
        list += name + ";";
        PlayerPrefs.SetString("AllPlayerNames", list);
    }

    void ShowError(string message)
    {
        if (errorTextTMPro != null)
            errorTextTMPro.text = message;
    }
}
