using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Events/Ice/SulfurCaveData")]
public class SulfurCaveData : ScriptableObject
{
    public Action<SulfurCave> OnPlayerEnter;

    public void PlayerEnter(SulfurCave sulfurCave)
    {
        OnPlayerEnter?.Invoke(sulfurCave);
    }
}
