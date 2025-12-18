using UnityEngine;

public class PlayerVisuals : MonoBehaviour
{
    public SpriteRenderer spriteRenderer;

    [Header("Materials")]
    public Material iceMaterial;
    public Material fireMaterial;

    private PlayerController player;

    void Awake()
    {
        player = GetComponentInParent<PlayerController>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void OnEnable()
    {
        if (player != null)
            player.OnFormSwitch += OnFormChanged;
    }

    void OnDisable()
    {
        if (player != null)
            player.OnFormSwitch -= OnFormChanged;
    }

    void Start()
    {
        OnFormChanged(player.IsIceForm);
    }

    void OnFormChanged(bool isIce)
    {
        if (spriteRenderer == null) return;

        Material target = isIce ? iceMaterial : fireMaterial;

        if (target != null && spriteRenderer.sharedMaterial != target)
        {
            spriteRenderer.sharedMaterial = target;
        }
    }
}
