using System;
using UnityEngine;

public class TriggerCompleteLevel2 : MonoBehaviour
{
    public Action OnLevelCompletedTrigger;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            OnLevelCompletedTrigger?.Invoke();
        }
    }
}
