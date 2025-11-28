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

    private _FezPlayerController state;

    [SerializeField] public VolumeProfile profil;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        state = GetComponent<_FezPlayerController>();
    }

  
    public void OnChangeLight()
    {
        if (profil.TryGet<Vignette>(out Vignette vignette))
        {
            if (state.currentState == _FezPlayerController.States.Fire)
                vignette.intensity.value = 0.2f;
            else
                vignette.intensity.value = 1f;
        }
    }
}
