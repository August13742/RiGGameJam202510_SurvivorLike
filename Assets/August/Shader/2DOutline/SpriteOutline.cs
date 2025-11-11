using UnityEngine;

[ExecuteInEditMode]
[DisallowMultipleComponent]
public class SpriteOutline : MonoBehaviour
{
    public Color color = Color.white;

    [Range(0, 16)]
    public int outlineSize = 1;

    private SpriteRenderer spriteRenderer;
    private MaterialPropertyBlock mpb;

    void OnEnable()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        UpdateOutline(true);
    }

    void OnDisable()
    {
        UpdateOutline(false);
    }

    private void UpdateOutline(bool outline)
    {
        mpb ??= new MaterialPropertyBlock();
        spriteRenderer.GetPropertyBlock(mpb);
        mpb.SetFloat("_Outline", outline ? 1f : 0);
        mpb.SetColor("_OutlineColor", color);
        mpb.SetFloat("_OutlineSize", outlineSize);
        spriteRenderer.SetPropertyBlock(mpb);
    }

    public void SetOutlineColor(Color newColor)
    {
        color = newColor;
        UpdateOutline(true);
    }

    public void SetOutlineSize(float newSize)
    {
        outlineSize = (int)newSize;
        UpdateOutline(true);
    }
}