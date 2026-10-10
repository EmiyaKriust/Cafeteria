using UnityEngine;
using UnityEngine.UI;

// 菜肴库面板控制器：菜名输入框 + 配料行列表。
// - 「添加配料」按钮 OnClick → AddIngredientRow()
// - 「保存」按钮 OnClick → CreateDish()
// rowsParent 填配料行的容器（如 Scroll View 的 Content），rowPrefab 填带 DishIngredientRow 的行预制体。
public class DishPanel : MonoBehaviour
{
    [Header("输入")]
    public InputField nameInput;

    [Header("配料行")]
    public Transform rowsParent;   // 配料行容器
    public GameObject rowPrefab;   // 带 DishIngredientRow 组件的行预制体

    [Header("提示（可选）")]
    public Text feedback;

    public void AddIngredientRow()
    {
        if (DataManager.Instance.IngredientNames().Count == 0)
        {
            SetFeedback("请先在配料库中添加配料");
            return;
        }

        var go = Instantiate(rowPrefab, rowsParent);
        var row = go.GetComponent<DishIngredientRow>();
        if (row != null) row.RefreshOptions();
    }

    public void CreateDish()
    {
        string name = (nameInput != null ? nameInput.text : "").Trim();
        if (string.IsNullOrEmpty(name)) { SetFeedback("菜名不能为空"); return; }
        if (DataManager.Instance.FindDish(name) != null) { SetFeedback("已存在同名菜肴：" + name); return; }

        var dish = new Dish(name);
        foreach (var row in rowsParent.GetComponentsInChildren<DishIngredientRow>())
        {
            var di = row.ToDishIngredient();
            if (!string.IsNullOrEmpty(di.ingredientName))
                dish.ingredients.Add(di);
        }
        if (dish.ingredients.Count == 0) { SetFeedback("请至少添加一种配料"); return; }

        DataManager.Instance.AddDish(dish);
        SetFeedback("已保存菜肴：" + name);
    }

    private void SetFeedback(string msg)
    {
        if (feedback) feedback.text = msg;
    }
}
