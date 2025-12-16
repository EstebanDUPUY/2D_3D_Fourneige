using UnityEngine;

public class PlayerVisuals : MonoBehaviour
{
    [Header("Renderer")]
    public SpriteRenderer spriteRenderer;

    [Header("Materials")]
    public Material fireMaterial;
    public Material iceMaterial;

    private PlayerController player;

    void Awake()
    {
        player = GetComponentInParent<PlayerController>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void OnEnable()
    {
        player.OnFormSwitch += OnFormChanged;
    }

    void OnDisable()
    {
        player.OnFormSwitch -= OnFormChanged;
    }

    void Start()
    {
        // Appliquer la forme de départ
        OnFormChanged(player.IsFireForm);
    }

    void OnFormChanged(bool isFire)
    {
        spriteRenderer.material = isFire ? fireMaterial : iceMaterial;
    }
}