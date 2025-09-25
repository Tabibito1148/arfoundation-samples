using UnityEngine;

public class Ball : MonoBehaviour
{
    private Color ballColor;

    public void Initialize(Color color)
    {
        ballColor = color;

        Material mat = GetComponent<Renderer>().material;

        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", color * 0.3f);
    }

    public Color GetColor()
    {
        return ballColor;
    }
}