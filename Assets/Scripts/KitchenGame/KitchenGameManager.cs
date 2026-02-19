using UnityEngine;
using TMPro;

public class KitchenGameManager : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private KitchenLevelStatsSO stats; // STANDARD #3: No hardcoded constants
    [SerializeField] private IngredientSO targetIngredient; // For this level: Chocolate

    [Header("Scene References")]
    [SerializeField] private HeatController heatSource;
    [SerializeField] private MockPotController pot;
    [SerializeField] private GameObject winText;
    [SerializeField] private GameObject failText;

    private float currentMeltPercent = 0f;
    private bool hasWon = false;

    private void Update()
    {
        if (hasWon || stats == null) return;

        // Is the correct ingredient in the pot?
        if (pot.ContainsIngredient(targetIngredient))
        {
            float heatFactor = heatSource.CurrentHeat / 100f;

            if (heatFactor > 0)
            {
                // Physics: Progress = Speed (from SO) * Heat * Time
                currentMeltPercent += stats.meltSpeed * heatFactor * Time.deltaTime;
                currentMeltPercent = Mathf.Clamp(currentMeltPercent, 0f, stats.winThreshold);
            }
        }
        // FAIL CONDITION: Heat is high but chocolate is missing
        else if (heatSource.CurrentHeat > 10)
        {
            failText.SetActive(true);
        }

        CheckWin();
    }

    private void CheckWin()
    {
        if (currentMeltPercent >= stats.winThreshold)
        {
            hasWon = true;
            winText.SetActive(true);
            failText.SetActive(false);
        }
    }
}