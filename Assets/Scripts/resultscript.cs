using TMPro;
using UnityEngine;

public class resultscript : MonoBehaviour
{   
    public TMP_Text resultText;
    public GameObject tryAgainButton;

    public void ShowSuccess()
    {
        resultText.text = "Congratulations!";
    }

    public void ShowFailure()
    {
        resultText.text = "Try Again";

    }


}
