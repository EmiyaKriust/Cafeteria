using UnityEngine;

// 页面切换：挂在 Canvas 上，把各页面（首页/菜肴库/配料库/生成表格）拖到 pages 数组。
// 每个导航按钮的 OnClick 绑定 ShowPage(int)，参数填对应页面下标（0/1/2/3）。
public class PanelSwitcher : MonoBehaviour
{
    public GameObject[] pages;   // 0=首页 1=菜肴库 2=配料库 3=生成表格（顺序可自定义）

    private void Start()
    {
        ShowPage(0); // 默认显示首页
    }

    public void ShowPage(int index)
    {
        for (int i = 0; i < pages.Length; i++)
            if (pages[i] != null)
                pages[i].SetActive(i == index);
    }

    // 也支持直接传页面对象
    public void ShowPage(GameObject target)
    {
        foreach (var p in pages)
            if (p != null)
                p.SetActive(p == target);
    }
}
