using UnityEngine;

/// <summary>
/// Spawns a one-shot particle blast with configurable size and color.
/// Attach to a prefab that has a ParticleSystem (or child). Call Initialize after instantiate.
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class ParticleBlast : MonoBehaviour
{
    private ParticleSystem ps;
    private float destroyDelay;

    /// <summary>
    /// Configure and play the blast. Call this right after instantiating the prefab.
    /// </summary>
    /// <param name="size">Scale of the blast (affects particle start size and emission).</param>
    /// <param name="color">Color of the particles.</param>
    public void Initialize(float size, Color color)
    {
        ps = GetComponent<ParticleSystem>();
        if (ps == null)
            ps = GetComponentInChildren<ParticleSystem>();

        if (ps == null)
        {
            Destroy(gameObject);
            return;
        }

        var main = ps.main;
        main.startColor = new ParticleSystem.MinMaxGradient(color);

        // Scale the whole object so size parameter controls blast radius
        transform.localScale = Vector3.one * size;

        ps.Clear(true);
        ps.Play(true);

        float startLifetime = main.startLifetime.constant;
        if (main.startLifetime.mode == ParticleSystemCurveMode.TwoConstants)
            startLifetime = main.startLifetime.constantMax;
        destroyDelay = main.duration + startLifetime;
        if (destroyDelay < 0.5f)
            destroyDelay = 1.5f;
        Destroy(gameObject, destroyDelay);
    }
}
