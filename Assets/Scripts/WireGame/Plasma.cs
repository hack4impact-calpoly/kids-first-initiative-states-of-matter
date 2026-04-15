using UnityEngine;

public class Plasma : MonoBehaviour, IOutputDevice
{
    public SpriteRenderer plasmaSprite;
    public Color activatedColor = Color.magenta;
    private Color originalColor;
    private bool isActivated = false;

    void Start()
    {
        if (plasmaSprite != null)
            originalColor = plasmaSprite.color;
    }

    public void Activate()
    {
        if (!isActivated)
        {
            isActivated = true;
            if (plasmaSprite != null)
                plasmaSprite.color = activatedColor;
        }
    }

    public void Deactivate()
    {
        isActivated = false;
        if (plasmaSprite != null)
            plasmaSprite.color = originalColor;
    }
}