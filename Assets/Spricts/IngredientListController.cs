using UnityEngine;

// 配料库列表控制器：挂在列表的 Content（或容器）上，自动显示所有配料并支持删除。
// 订阅 DataManager.OnChanged，数据一变就自动刷新。
public class IngredientListController : MonoBehaviour
{
    public Transform content;       // 行容器（通常是 Scroll View 的 Content）
    public GameObject rowPrefab;    // 带 IngredientRow 组件的行预制体

    private void OnEnable()
    {
        Refresh();
        DataManager.OnChanged += Refresh;
    }

    private void OnDisable()
    {
        DataManager.OnChanged -= Refresh;
    }

    public void Refresh()
    {
        if (content == null || rowPrefab == null) return;

        for (int i = content.childCount - 1; i >= 0; i--)
            Destroy(content.GetChild(i).gameObject);

        foreach (var ing in DataManager.Instance.ingredients)
        {
            var go = Instantiate(rowPrefab, content);
            var row = go.GetComponent<IngredientRow>();
            if (row != null) row.Init(ing);
        }
    }
}
