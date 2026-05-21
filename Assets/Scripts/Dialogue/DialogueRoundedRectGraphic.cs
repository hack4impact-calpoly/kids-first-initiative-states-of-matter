using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class DialogueRoundedRectGraphic : MaskableGraphic
{
    [SerializeField] private float cornerRadius = 14f;
    [SerializeField, Range(2, 16)] private int cornerSegments = 8;

    public float CornerRadius
    {
        get => cornerRadius;
        set
        {
            cornerRadius = Mathf.Max(0f, value);
            SetVerticesDirty();
        }
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        Rect rect = GetPixelAdjustedRect();
        if (rect.width <= 0f || rect.height <= 0f)
            return;

        float radius = Mathf.Min(cornerRadius, rect.width * 0.5f, rect.height * 0.5f);
        Vector2 center = rect.center;

        UIVertex vertex = UIVertex.simpleVert;
        vertex.color = color;
        vertex.position = center;
        vh.AddVert(vertex);

        int steps = Mathf.Max(2, cornerSegments);
        AddCorner(vh, rect.xMax - radius, rect.yMax - radius, radius, 90f, 0f, steps);
        AddCorner(vh, rect.xMax - radius, rect.yMin + radius, radius, 0f, -90f, steps);
        AddCorner(vh, rect.xMin + radius, rect.yMin + radius, radius, -90f, -180f, steps);
        AddCorner(vh, rect.xMin + radius, rect.yMax - radius, radius, 180f, 90f, steps);

        int perimeterCount = vh.currentVertCount - 1;
        for (int i = 1; i <= perimeterCount; i++)
        {
            int next = i == perimeterCount ? 1 : i + 1;
            vh.AddTriangle(0, i, next);
        }
    }

    private void AddCorner(VertexHelper vh, float centerX, float centerY, float radius, float startAngle, float endAngle, int steps)
    {
        for (int i = 0; i <= steps; i++)
        {
            float t = i / (float)steps;
            float angle = Mathf.Lerp(startAngle, endAngle, t) * Mathf.Deg2Rad;

            UIVertex vertex = UIVertex.simpleVert;
            vertex.color = color;
            vertex.position = new Vector2(
                centerX + Mathf.Cos(angle) * radius,
                centerY + Mathf.Sin(angle) * radius);

            vh.AddVert(vertex);
        }
    }
}
