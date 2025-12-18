using UnityEngine;

public class LandslideZone : MonoBehaviour
{
    Rock[] rocks;
    public GameObject gameObjectRocks;

    public bool landslideActive;

    void Awake()
    {
        rocks = gameObjectRocks.GetComponentsInChildren<Rock>();
    }

    void Update()
    {
        if (landslideActive)
        {
            foreach (var rock in rocks)
            {
                if (rock.isUp)
                {
                    Debug.Log("is Up");
                    rock.Fall();
                }
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            landslideActive = true;
        }
    }

    void OnTriggerExit(Collider other)
    {
        landslideActive = false;
    }
}
