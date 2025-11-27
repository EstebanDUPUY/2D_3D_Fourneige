using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Events/Ice/IceWallData")]
public class IceWallData : ScriptableObject
{
    public Action<IceWall> OnPlayerEnter;
    public Action OnPlayerExit;

    public void PlayerEnter(IceWall iceWall)
    {
        OnPlayerEnter?.Invoke(iceWall);
    }

    public void PlayerExit()
    {
        OnPlayerExit?.Invoke();
    }
}
