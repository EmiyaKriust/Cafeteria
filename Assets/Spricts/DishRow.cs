using System.Text;
using UnityEngine;
using UnityEngine.UI;

// 菜肴列表行（放在「菜肴行」预制体上）：信息文本 + 删除按钮。
public class DishRow : MonoBehaviour
{
    public Text infoText;
    public Button deleteButton;

    private Dish dish;

    public void Init(Dish d)
    {
        dish = d;

        if (infoText != null)
        {
            var sb = new StringBuilder();
            sb.Append(d.name).Append("（");
            for (int i = 0; i < d.ingredients.Count; i++)
            {
                if (i > 0) sb.Append("、");
                sb.Append(d.ingredients[i].ingredientName);
                sb.Append(" ").Append(d.ingredients[i].gramsPerPerson).Append("g");
            }
            sb.Append("）");
            infoText.text = sb.ToString();
        }

        if (deleteButton != null)
            deleteButton.onClick.AddListener(OnDelete);
    }

    private void OnDelete()
    {
        DataManager.Instance.RemoveDish(dish); // OnChanged 会触发列表刷新
    }
}
