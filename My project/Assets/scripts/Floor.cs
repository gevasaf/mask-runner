using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Floor : MonoBehaviour
{
    public GameManager gameManager;
    public MeshRenderer mr;

    /// <summary>Shader property names for 5 textures.</summary>
    public static readonly string[] TextureNames = { "_Tex1", "_Tex2", "_Tex3", "_Tex4", "_Tex5" };
    /// <summary>Shader property names for 5 opacities.</summary>
    public static readonly string[] OpacityNames = { "_Opacity1", "_Opacity2", "_Opacity3", "_Opacity4", "_Opacity5" };

    Material _floorMaterial;
    Vector2 _tiling = new Vector2(3f, 20f);

    void Start()
    {
        _floorMaterial = mr != null ? mr.material : null;
    }

    void Update()
    {
        if (gameManager != null && gameManager.IsGameOver())
            return;
        if (_floorMaterial == null)
            return;

        float offset = Time.time * gameManager.forwardSpeed;
        Vector2 off = new Vector2(0, -offset * 20f / 120f);
        SetTilingAndOffset(_tiling, off);
    }

    /// <summary>
    /// Set opacity for each layer by index (0-4). Only indices with opacity > 0 are sampled in the shader.
    /// </summary>
    public void SetOpacities(float[] opacities)
    {
        if (_floorMaterial == null)
            _floorMaterial = mr != null ? mr.material : null;
        if (_floorMaterial == null || opacities == null)
            return;
        for (int i = 0; i < 5 && i < opacities.Length; i++)
        {
            if (OpacityNames[i] != null)
                _floorMaterial.SetFloat(OpacityNames[i], Mathf.Clamp01(opacities[i]));
        }
    }

    /// <summary>
    /// Apply the same tiling (and optional offset) to all 5 texture layers.
    /// </summary>
    public void SetTilingAndOffset(Vector2 tiling, Vector2 offset)
    {
        if (_floorMaterial == null)
            return;
        _tiling = tiling;
        for (int i = 0; i < TextureNames.Length; i++)
        {
            if (_floorMaterial.HasProperty(TextureNames[i]))
            {
                _floorMaterial.SetTextureScale(TextureNames[i], tiling);
                _floorMaterial.SetTextureOffset(TextureNames[i], offset);
            }
        }
        if (_floorMaterial.HasProperty("_MainTex"))
        {
            _floorMaterial.SetTextureScale("_MainTex", tiling);
            _floorMaterial.SetTextureOffset("_MainTex", offset);
        }
    }

    /// <summary>
    /// Set tiling used for all textures (used in Update for scroll).
    /// </summary>
    public void SetTiling(Vector2 tiling)
    {
        _tiling = tiling;
    }

    /// <summary>
    /// Get current opacity values (for fading between worlds).
    /// </summary>
    public float[] GetOpacities()
    {
        if (_floorMaterial == null)
            _floorMaterial = mr != null ? mr.material : null;
        float[] o = new float[5];
        if (_floorMaterial == null)
            return o;
        for (int i = 0; i < 5; i++)
        {
            if (_floorMaterial.HasProperty(OpacityNames[i]))
                o[i] = _floorMaterial.GetFloat(OpacityNames[i]);
        }
        return o;
    }
}
