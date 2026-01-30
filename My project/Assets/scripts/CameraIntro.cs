using UnityEngine;
using TMPro.Examples;

/// <summary>
/// Plays a short intro: camera starts looking at the player, then interpolates to the
/// camera's current (saved) transform. Uses spherical coordinates around the player
/// (distance, angle, pitch); lerps those and converts to xyz. Direction uses Quaternion.Slerp between the two transforms' rotations.
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraIntro : MonoBehaviour
{
    [Tooltip("Transform to orbit around (e.g. Player).")]
    public Transform lookAtTarget;

    [Tooltip("Duration of the intro in seconds.")]
    public float duration = 3f;

    [Tooltip("Initial distance from the player.")]
    public float startDistance = 15f;

    [Tooltip("Initial horizontal angle in degrees (0 = behind player along +Z).")]
    public float startAngle = 0f;

    [Tooltip("Initial pitch (elevation) in degrees; positive = above.")]
    public float startPitch = 25f;

    private Transform camTransform;
    private float endDistance;
    private float endAngle;
    private float endAngleOrbit; // other side of circle so we orbit right into place
    private float endPitch;
    private Quaternion startRotation;
    private Quaternion startRotationSlerp; // negated start so Slerp takes the other arc (turn right)
    private Quaternion endRotation;
    private float startTime;
    private bool introDone;
    private MonoBehaviour followController;

    void Start()
    {
        camTransform = transform;

        if (lookAtTarget == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                lookAtTarget = player.transform;
        }

        if (lookAtTarget == null)
        {
            enabled = false;
            return;
        }

        Vector3 playerPos = lookAtTarget.position;

        // Record end state: distance, angle, pitch from player
        Vector3 offset = camTransform.position - playerPos;
        endDistance = offset.magnitude;
        Vector3 dir = endDistance > 0.001f ? offset / endDistance : Vector3.forward;
        endAngle = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
        endAngleOrbit = endAngle + 360f; // orbit the other way so camera comes from the right
        endPitch = Mathf.Asin(Mathf.Clamp(dir.y, -1f, 1f)) * Mathf.Rad2Deg;

        // End rotation: same look direction but zero roll (horizon level)
        endRotation = Quaternion.LookRotation(camTransform.forward, Vector3.up);

        // Initial position from spherical (distance, angle, pitch) around player
        Vector3 startPos = playerPos + SphericalToCartesian(startDistance, startAngle, startPitch);
        camTransform.position = startPos;
        startRotation = Quaternion.LookRotation((playerPos - startPos).normalized, Vector3.up);
        camTransform.rotation = startRotation;

        // Use the other arc: slerp from -startRotation to endRotation so camera turns right into place (not left)
        startRotationSlerp = new Quaternion(-startRotation.x, -startRotation.y, -startRotation.z, -startRotation.w);

        startTime = Time.time;
        introDone = false;

        followController = GetComponent<CameraController>();
        if (followController != null)
            followController.enabled = false;
    }

    static Vector3 SphericalToCartesian(float distance, float angleDeg, float pitchDeg)
    {
        float pitch = pitchDeg * Mathf.Deg2Rad;
        float angle = angleDeg * Mathf.Deg2Rad;
        float cosPitch = Mathf.Cos(pitch);
        float x = distance * cosPitch * Mathf.Sin(angle);
        float y = distance * Mathf.Sin(pitch);
        float z = distance * cosPitch * Mathf.Cos(angle);
        return new Vector3(x, y, z);
    }

    void LateUpdate()
    {
        if (introDone)
            return;

        float elapsed = Time.time - startTime;
        float t = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;

        // Lerp distance, angle, pitch (angle orbits the other way via endAngleOrbit)
        float dist = Mathf.Lerp(startDistance, endDistance, t);
        float angle = Mathf.LerpAngle(startAngle, endAngleOrbit, t);
        float pitch = Mathf.Lerp(startPitch, endPitch, t);

        Vector3 playerPos = lookAtTarget.position;
        camTransform.position = playerPos + SphericalToCartesian(dist, angle, pitch);

        // Slerp rotation (from negated start so we take the other arc), then strip roll so horizon stays level (z rotation = 0)
        Quaternion slerped = Quaternion.Slerp(startRotationSlerp, endRotation, t);
        Vector3 forward = slerped * Vector3.forward;
        if (forward.sqrMagnitude > 0.001f)
            camTransform.rotation = Quaternion.LookRotation(forward, Vector3.up);

        if (t >= 1f)
        {
            camTransform.position = playerPos + SphericalToCartesian(endDistance, endAngle, endPitch);
            camTransform.rotation = endRotation;
            introDone = true;
            if (followController != null)
                followController.enabled = true;
            enabled = false;
        }
    }
}
