using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

// 生成表格面板控制器：用餐人数 + 标题 + 选择当日菜肴 + 生成。
// - 「添加菜肴」按钮 OnClick → AddDish()
// - 「生成表格」按钮 OnClick → Generate()
// 下拉框 dishSelector 会自动在 OnEnable 时刷新菜肴选项。
public class GeneratePanel : MonoBehaviour
{
    [Header("设置")]
    public InputField dinersInput;          // 用餐人数
    public InputField titleInput;           // 标题（星期X（X月X日）），留空用今天
    public Dropdown dishSelector;           // 当日菜肴选择

    [Header("已添加菜肴")]
    public Transform dishesParent;          // 菜肴分区容器
    public GameObject dishSectionPrefab;    // 带 DayDishSection 组件的分区预制体

    [Header("提示（可选）")]
    public Text feedback;

    private void OnEnable()
    {
        RefreshDishSelector();
    }

    public void RefreshDishSelector()
    {
        if (dishSelector == null) return;
        dishSelector.ClearOptions();
        var names = DataManager.Instance.DishNames();
        if (names.Count == 0) names.Add("（暂无菜肴）");
        dishSelector.AddOptions(names);
        dishSelector.RefreshShownValue();
    }

    public void AddDish()
    {
        if (dishSelector == null || dishSelector.options.Count == 0) return;
        string dishName = dishSelector.options[dishSelector.value].text;
        if (dishName.StartsWith("（")) return;

        var dish = DataManager.Instance.FindDish(dishName);
        if (dish == null || dish.ingredients.Count == 0) return;

        int diners = ParseInt(dinersInput);
        if (diners <= 0) diners = 1;

        var go = Instantiate(dishSectionPrefab, dishesParent);
        var section = go.GetComponent<DayDishSection>();
        if (section != null) section.Init(dishName, dish, diners);
    }

    public void Generate()
    {
        int diners = ParseInt(dinersInput);
        if (diners <= 0) { SetFeedback("用餐人数必须大于 0"); return; }

        string title = titleInput != null ? titleInput.text.Trim() : "";
        if (string.IsNullOrEmpty(title)) title = TodayTitle();

        var rows = new List<XlsxWriter.DishRow>();
        foreach (var s in dishesParent.GetComponentsInChildren<DayDishSection>())
            rows.Add(s.ToDishRow());

        if (rows.Count == 0) { SetFeedback("请先添加至少一个菜肴"); return; }

        try
        {
            string path = XlsxWriter.GenerateDayTable(title, diners, rows);
            SetFeedback("已生成：" + path);
        }
        catch (Exception e)
        {
            SetFeedback("生成失败：" + e.Message);
            Debug.LogException(e);
        }
    }

    private void SetFeedback(string msg)
    {
        if (feedback) feedback.text = msg;
    }

    private static int ParseInt(InputField f)
    {
        int v;
        if (f == null) return 0;
        return int.TryParse(f.text, out v) ? v : 0;
    }

    private static string TodayTitle()
    {
        var now = DateTime.Now;
        string[] weeks = { "星期日", "星期一", "星期二", "星期三", "星期四", "星期五", "星期六" };
        return weeks[(int)now.DayOfWeek] + "（" + now.Month + "月" + now.Day + "日）";
    }
}
