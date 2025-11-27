using NUnit.Framework;
using UnityEngine;

public class FireObstacleController : MonoBehaviour
{
    private Rigidbody rb;
    private float forceGeyser;
    private bool inGeyser;
    private bool inBodyGeyser;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (inGeyser)
            rb.AddForce(0, forceGeyser, 0, ForceMode.Impulse);
    }
    public void GeyserAction(float force, bool isInGeyser, bool isInBody)
    {
        if (inBodyGeyser != isInBody && inGeyser) return;

        forceGeyser = force;
        inGeyser = isInGeyser;
        inBodyGeyser = isInBody;
    }
}
