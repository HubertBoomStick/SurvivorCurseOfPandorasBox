using UnityEngine;

public class ScrollingWaterMaterial : MonoBehaviour
{
    [SerializeField] private Renderer waterRenderer;
    [SerializeField] private float scrollSpeed = 1f;

    private Material waterMaterial;

    private void Start()
    {
        if (waterRenderer == null)
            waterRenderer = GetComponent<Renderer>();

        waterMaterial = waterRenderer.material;
    }

    private void Update()
    {
        if (waterMaterial == null)
            return;

        Vector2 offset = waterMaterial.mainTextureOffset;
        offset.y -= scrollSpeed * Time.deltaTime;
        waterMaterial.mainTextureOffset = offset;
    }
}