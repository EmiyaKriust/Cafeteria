using UnityEngine;

// 菜肴库列表控制器：挂在列表的 Content（或容器）上，自动显示所有菜肴并支持删除。
public class DishListController : MonoBehaviour
{
    public Transform content;       // 行容器
    public GameObject rowPrefab;    // 带 DishRow 组件的行预制体

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

        foreach (var dish in DataManager.Instance.dishes)
        {
            var go = Instantiate(rowPrefab, content);
            var row = go.GetComponent<DishRow>();
            if (row != null) row.Init(dish);
        }
    }
}
