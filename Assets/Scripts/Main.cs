using UnityEngine;

public class Main : MonoBehaviour
{
    static public Main Instance;

    public int wiresCount; // wires total
    public GameObject winText;
    private int count = 0; // number of wires connected

    private void Awake()
    {
        Instance = this;
    }

    public void LightOn(int points) {
        count = count + points;
        if (count == wiresCount)
        {
            winText.SetActive(true);
        }
    }
}
