using UnityEngine;
using UnityEngine.UI;

// 配料列表行（放在「配料行」预制体上）：信息文本 + 删除按钮。
public class IngredientRow : MonoBehaviour
{
    public Text infoText;
    public Button deleteButton;

    private Ingredient ingredient;

    public void Init(Ingredient ing)
    {
        ingredient = ing;

        if (infoText != null)
            infoText.text = ing.name + "    " + ing.kcal + " kcal/100g    P:" +
                            ing.protein + " F:" + ing.fat + " C:" + ing.carbohydrate;

        if (deleteButton != null)
            deleteButton.onClick.AddListener(OnDelete);
    }

    private void OnDelete()
    {
        DataManager.Instance.RemoveIngredient(ingredient); // OnChanged 会触发列表刷新
    }
}
