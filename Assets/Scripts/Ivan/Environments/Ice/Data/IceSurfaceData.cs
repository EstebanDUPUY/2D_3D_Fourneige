using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Events/Ice/IceSurfaceData")]
public class IceSurfaceData : ScriptableObject
{
    public Action<IceSurface> OnPlayerEnter;
    public Action OnPlayerExit;

    public void PlayerEnter(IceSurface iceSurface)
    {
        OnPlayerEnter?.Invoke(iceSurface);
    }

    public void PlayerExit()
    {
        OnPlayerExit?.Invoke();
    }
}
