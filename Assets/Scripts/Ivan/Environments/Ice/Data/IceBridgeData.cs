using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Events/Ice/IceBridgeData")]
public class IceBridgeData : ScriptableObject
{
    public Action<IceBridge> OnPlayerEnter;
    public Action OnPlayerExit;

    public void PlayerEnter(IceBridge iceBridge)
    {
        OnPlayerEnter?.Invoke(iceBridge);
    }

    public void PlayerExit()
    {
        OnPlayerExit?.Invoke();
    }
}
