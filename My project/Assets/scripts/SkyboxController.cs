using UnityEngine;

/// <summary>
/// Controls the 6-layer skybox blend. Assign opacities by index; only layers with opacity > 0 are sampled.
/// </summary>
public class SkyboxController : MonoBehaviour
{
    public static readonly string[] OpacityNames = { "_Opacity1", "_Opacity2", "_Opacity3", "_Opacity4", "_Opacity5", "_Opacity6" };

    Material _skyboxMaterial;

    void Start()
    {
        _skyboxMaterial = RenderSettings.skybox;
        if (_skyboxMaterial != null)
            _skyboxMaterial = new Material(_skyboxMaterial); // instance so we don't change project settings asset
        if (_skyboxMaterial != null)
            RenderSettings.skybox = _skyboxMaterial;
    }

    void OnDestroy()
    {
        if (_skyboxMaterial != null)
            Destroy(_skyboxMaterial);
    }

    /// <summary>
    /// Set opacity for each layer by index (0-5).
    /// </summary>
    public void SetOpacities(float[] opacities)
    {
        if (_skyboxMaterial == null)
            _skyboxMaterial = RenderSettings.skybox;
        if (_skyboxMaterial == null || opacities == null)
            return;
        for (int i = 0; i < 6 && i < opacities.Length; i++)
        {
            if (OpacityNames[i] != null && _skyboxMaterial.HasProperty(OpacityNames[i]))
                _skyboxMaterial.SetFloat(OpacityNames[i], Mathf.Clamp01(opacities[i]));
        }
    }

    /// <summary>
    /// Get current opacity values (for fading between worlds).
    /// </summary>
    public float[] GetOpacities()
    {
        if (_skyboxMaterial == null)
            _skyboxMaterial = RenderSettings.skybox;
        float[] o = new float[6];
        if (_skyboxMaterial == null)
            return o;
        for (int i = 0; i < 6; i++)
        {
            if (_skyboxMaterial.HasProperty(OpacityNames[i]))
                o[i] = _skyboxMaterial.GetFloat(OpacityNames[i]);
        }
        return o;
    }
}
