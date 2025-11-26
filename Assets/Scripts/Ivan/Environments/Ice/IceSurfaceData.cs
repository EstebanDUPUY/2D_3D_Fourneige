using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Events/Environment/IceSurfaceData")]
public class IceSurfaceData : ScriptableObject
{
    public float dragOnIce = 0.2f;
    public float speedMultiplier = 2f;
    public bool killFireForm = true;

    // L'event qui sera déclenché quand le joueur entre
    public Action OnPlayerEnter;

    // Appel de l'event
    public void PlayerEnter()
    {
        OnPlayerEnter?.Invoke();
    }
}
