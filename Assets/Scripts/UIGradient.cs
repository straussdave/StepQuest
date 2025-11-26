using UnityEngine;
using UnityEngine.UI;

[AddComponentMenu("UI/Effects/Gradient")]
public class UIGradient : BaseMeshEffect
{
    public Color topColor = Color.white;
    public Color bottomColor = Color.black;

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive() || vh.currentVertCount == 0)
            return;

        UIVertex v = new UIVertex();

        // get bounds
        float topY = float.MinValue;
        float bottomY = float.MaxValue;

        for (int i = 0; i < vh.currentVertCount; i++)
        {
            vh.PopulateUIVertex(ref v, i);
            if (v.position.y > topY) topY = v.position.y;
            if (v.position.y < bottomY) bottomY = v.position.y;
        }

        float height = topY - bottomY;

        for (int i = 0; i < vh.currentVertCount; i++)
        {
            vh.PopulateUIVertex(ref v, i);

            float normalized = (v.position.y - bottomY) / height;
            v.color = Color.Lerp(bottomColor, topColor, normalized);

            vh.SetUIVertex(v, i);
        }
    }
}
