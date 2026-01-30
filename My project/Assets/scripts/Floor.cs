using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Floor : MonoBehaviour
{
    public GameManager gameManager;
    public MeshRenderer mr;

    Material _floorMaterial;

    void Start()
    {
        _floorMaterial = mr.material;
    }

    void Update()
    {
        // Stop floor animation when game is over
        if (gameManager != null && gameManager.IsGameOver())
        {
            return;
        }

        float offset = Time.time * gameManager.forwardSpeed;
        _floorMaterial.mainTextureOffset = new Vector2(0, -offset * 22.0f / 120);
    }
}
