using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class FireObstacleController : MonoBehaviour
{
    private Rigidbody rb;
    private float forceGeyser;
    private bool inGeyser;
    private bool inBodyGeyser;

    private Fused_Player state;

    private Vector3 posPlayerOnScreen;

    [SerializeField] public VolumeProfile profil;
    [SerializeField] private bool isBrumeActivated = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        state = GetComponent<FezPlayerController>();
    }

    void Update()
    {
        posPlayerOnScreen = Camera.main.WorldToViewportPoint(transform.position);

        if (isBrumeActivated && profil.TryGet(out Vignette vignette))
        {
            vignette.center.value = new Vector2(posPlayerOnScreen.x, posPlayerOnScreen.y);

            if (state.currentState == FezPlayerController.States.Fire)
                vignette.intensity.value = 0.2f;
            else
                vignette.intensity.value = 1f;
        }
    }


    public void OnChangeLight()
    {
        if (profil.TryGet(out Vignette vignette))
        {
            if (state.currentState == FezPlayerController.States.Fire)
                vignette.intensity.value = 0.2f;
            else
                vignette.intensity.value = 1f;
        }
    }
}
