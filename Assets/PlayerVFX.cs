using Unity.Mathematics;
using UnityEngine;

public class PlayerVFX : MonoBehaviour
{
    [SerializeField] private ParticleSystem[] FireStateChange;
    [SerializeField] private ParticleSystem[] IceStateChange;

    [SerializeField] PlayerController controller;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        controller.OnFormSwitch += ChangeExplosion;
    }


    private void ChangeExplosion(bool state)
    {
        switch (state)
        {
            case true:
                InstantiatateAllParticleFromArray(IceStateChange);
                Debug.Log("In Ice");
                break;
            case false:
                InstantiatateAllParticleFromArray(FireStateChange);
                Debug.Log("In Fire");
                break;
        }
    }

    private void InstantiatateAllParticleFromArray(ParticleSystem[] arrayParticle)
    {
        for (int i = 0; i < arrayParticle.Length; i++)
        {
            Instantiate(arrayParticle[i], transform.position, Quaternion.identity);
        }
    }
}
