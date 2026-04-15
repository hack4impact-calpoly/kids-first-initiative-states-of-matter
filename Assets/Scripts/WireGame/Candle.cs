using UnityEngine;

public class Candle : MonoBehaviour, IOutputDevice
{
    public SpriteRenderer candleSprite;
    public GameObject flame;
    private bool isLit = false;

    void Start()
    {
        if (flame != null)
            flame.SetActive(false);
    }

    public void Light()
    {
        if (!isLit)
        {
            isLit = true;
            if (flame != null)
                flame.SetActive(true);
        }
    }

    public void Extinguish()
    {
        isLit = false;
        if (flame != null)
            flame.SetActive(false);
    }
}