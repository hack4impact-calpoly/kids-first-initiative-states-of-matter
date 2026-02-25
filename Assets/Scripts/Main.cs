using UnityEngine;

public class Main : MonoBehaviour
{
    static public Main Instance;

    public int wiresCount; // wires total
    public GameObject block;
    private int count = 0; // number of wires connected
    private Renderer blockRenderer;

    private void Awake()
    {
        Instance = this;
        blockRenderer = block.GetComponent<Renderer>();
    }

    public void LightOn(int points) {
        count = count + points;
        if (count == wiresCount)
        {
            block.SetActive(true);
            blockRenderer.material.color = Color.green;
        }
    }
}
