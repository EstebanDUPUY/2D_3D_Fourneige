using UnityEngine;

public class PlayerVisuals : MonoBehaviour
{
    [Header("Sprite")]
    public SpriteRenderer spriteRenderer;

    [Header("Materials")]
    public Material iceMaterial;
    public Material fireMaterial;

    [Header("FX Parents")]
    public GameObject iceFX;
    public GameObject fireFX;

    private PlayerController player;

    void Awake()
    {
        player = GetComponentInParent<PlayerController>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
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
        UpdateSprite(isIce);
        UpdateFX(isIce);
    }

    void UpdateSprite(bool isIce)
    {
        if (spriteRenderer == null) return;

        Material target = isIce ? iceMaterial : fireMaterial;

        if (spriteRenderer.sharedMaterial != target)
            spriteRenderer.sharedMaterial = target;
    }

    void UpdateFX(bool isIce)
    {
        if (iceFX != null)
            iceFX.SetActive(isIce);

        if (fireFX != null)
            fireFX.SetActive(!isIce);
    }
}
