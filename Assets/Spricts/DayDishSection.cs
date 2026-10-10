using UnityEngine;
using UnityEngine.UI;

// 生成表格里「一个菜肴」的分区（放在「菜肴分区」预制体上）：菜名 + 多个配料行 + 删除本分区按钮。
public class DayDishSection : MonoBehaviour
{
    public Text dishNameText;
    public Transform rowsParent;
    public GameObject rowPrefab;   // 带 TotalIngredientRow 组件的配料行预制体
    public Button removeButton;    // 删除本分区按钮（可选，拖进来自动接线）

    public void Init(string dishName, Dish dish, int diners)
    {
        if (dishNameText) dishNameText.text = dishName;
        foreach (var ing in dish.ingredients)
        {
            var go = Instantiate(rowPrefab, rowsParent);
            var row = go.GetComponent<TotalIngredientRow>();
            if (row != null) row.Init(ing.ingredientName, ing.gramsPerPerson, diners);
        }
    }

    // 删除本分区
    public void RemoveSection()
    {
        Destroy(gameObject);
    }

    private void Start()
    {
        if (removeButton != null)
            removeButton.onClick.AddListener(RemoveSection);
    }

    public XlsxWriter.DishRow ToDishRow()
    {
        var dr = new XlsxWriter.DishRow { dishName = dishNameText != null ? dishNameText.text : "" };
        foreach (var row in rowsParent.GetComponentsInChildren<TotalIngredientRow>())
            dr.ingredients.Add(row.ToEntry());
        return dr;
    }
}
