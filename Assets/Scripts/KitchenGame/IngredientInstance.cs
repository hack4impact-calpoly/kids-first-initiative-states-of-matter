using UnityEngine;

public class IngredientInstance : MonoBehaviour
{
    [SerializeField] private IngredientSO data;
    public IngredientSO Data => data;
}