using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Events/Ice/IceBridgeData")]
public class IceBridgeData : ScriptableObject
{
    public Action<IceBridge> OnPlayerEnter;

    public void PlayerEnter(IceBridge iceBridge)
    {
        OnPlayerEnter?.Invoke(iceBridge);
    }
}
