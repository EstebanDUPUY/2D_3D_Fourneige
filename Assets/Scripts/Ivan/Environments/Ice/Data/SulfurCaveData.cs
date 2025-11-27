using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Events/Ice/SulfurCaveData")]
public class SulfurCaveData : ScriptableObject
{
    public Action<SulfurCave> OnPlayerEnter;
    public Action OnPlayerExit;

    public void PlayerEnter(SulfurCave sulfurCave)
    {
        OnPlayerEnter?.Invoke(sulfurCave);
    }

    public void PlayerExit()
    {
        OnPlayerExit?.Invoke();
    }
}
