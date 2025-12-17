using System.Security.Cryptography.X509Certificates;
using UnityEngine;

public class Brick : MonoBehaviour
{

    // Reference
    PlayerController controller;
    public SpriteRenderer brickSpriteRenderer { get; private set; }

    public Color[] brickStates;
    public int brickHealth {  get; private set; }

    public bool brickUnbreakable;

    private void Awake()
    {
        this.brickSpriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
       if (!this.brickUnbreakable)
        {
            this.brickHealth = this.brickStates.Length;
            this.brickSpriteRenderer.color = this.brickStates[this.brickHealth - 1];
        }
    }

    private void Hit()
    {
        if (this.brickUnbreakable)
            return;

        this.brickHealth--;

        if (this.brickHealth <= 0)
            this.gameObject.SetActive(false);
        else
            this.brickSpriteRenderer.color = this.brickStates[this.brickHealth - 1];


    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag == "Player" && controller.IsIceForm && controller.IsDashing == true)
        {
            Hit();
        }
    }
}
