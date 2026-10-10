using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

// 菜肴配料行（放在「配料行」预制体上）：配料下拉框 + 每人份用量输入框 + 删除本行按钮。
// 预制体建议做成 Assets 里的独立预制体，不要放在 rowsParent 容器下。
public class DishIngredientRow : MonoBehaviour
{
    public Dropdown ingredientDropdown;
    public InputField gramsInput;   // 每人份用量（g）
    public Button removeButton;     // 删除本行按钮（可选，拖进来自动接线）

    // 用配料库里的名字刷新下拉框选项
    public void RefreshOptions()
    {
        if (ingredientDropdown == null) return;
        ingredientDropdown.ClearOptions();
        ingredientDropdown.AddOptions(DataManager.Instance.IngredientNames());
        ingredientDropdown.RefreshShownValue();
    }

    // 删除本行
    public void RemoveRow()
    {
        Destroy(gameObject);
    }

    private void Start()
    {
        if (removeButton != null)
            removeButton.onClick.AddListener(RemoveRow);
    }

    public DishIngredient ToDishIngredient()
    {
        string name = "";
        if (ingredientDropdown != null && ingredientDropdown.options.Count > 0)
            name = ingredientDropdown.options[ingredientDropdown.value].text;

        float grams = 0f;
        if (gramsInput != null)
            float.TryParse(gramsInput.text, NumberStyles.Float, CultureInfo.InvariantCulture, out grams);

        return new DishIngredient(name, grams);
    }
}
