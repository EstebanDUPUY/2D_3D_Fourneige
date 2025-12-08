using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class DialogueScene : MonoBehaviour
{
    [System.Serializable]
    public class LigneDialogue
    {
        public string nom;
        public Sprite sprite;
        [TextArea] public string phrase;
    }

    public LigneDialogue[] dialogues;

    public TextMeshProUGUI nomUI;
    public TextMeshProUGUI phraseUI;
    public Image spriteUI;

    public Button boutonContinue;

    public string sceneSuivante;
    private int index = 0;

    void Start()
    {
        AfficherDialogue();
    }

    void AfficherDialogue()
    {
        nomUI.text = dialogues[index].nom;
        phraseUI.text = dialogues[index].phrase;
        spriteUI.sprite = dialogues[index].sprite;
    }

    public void Suivant()
    {
        boutonContinue.interactable = false;

        index++;

        if (index >= dialogues.Length)
        {
            if (!string.IsNullOrEmpty(sceneSuivante))
            {
                SceneManager.LoadScene(sceneSuivante);
            }
            return;
        }

        AfficherDialogue();
        boutonContinue.interactable = true;
    }
}
