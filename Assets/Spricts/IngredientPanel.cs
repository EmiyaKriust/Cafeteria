using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

// 配料库面板控制器。
// 拖到你的面板 GameObject 上，把输入框/提示文本拖到对应字段，
// 把「保存」按钮的 OnClick 绑定到 CreateIngredient() 即可。
public class IngredientPanel : MonoBehaviour
{
    [Header("输入")]
    public InputField nameInput;      // 名字
    public InputField kcalInput;      // 热量（kcal / 100g）
    public InputField proteinInput;   // 蛋白质（g / 100g）
    public InputField fatInput;       // 脂肪（g / 100g）
    public InputField carbInput;      // 碳水化合物（g / 100g）

    [Header("提示（可选）")]
    public Text feedback;

    public void CreateIngredient()
    {
        string name = (nameInput != null ? nameInput.text : "").Trim();
        if (string.IsNullOrEmpty(name)) { SetFeedback("名字不能为空"); return; }
        if (DataManager.Instance.IngredientNameExists(name)) { SetFeedback("已存在同名配料：" + name); return; }

        DataManager.Instance.AddIngredient(new Ingredient(
            name, Parse(kcalInput), Parse(proteinInput), Parse(fatInput), Parse(carbInput)));

        SetFeedback("已保存配料：" + name);

        if (nameInput) nameInput.text = "";
        if (kcalInput) kcalInput.text = "";
        if (proteinInput) proteinInput.text = "";
        if (fatInput) fatInput.text = "";
        if (carbInput) carbInput.text = "";
    }

    private void SetFeedback(string msg)
    {
        if (feedback) feedback.text = msg;
    }

    private static float Parse(InputField f)
    {
        float v;
        if (f == null) return 0f;
        return float.TryParse(f.text, NumberStyles.Float, CultureInfo.InvariantCulture, out v) ? v : 0f;
    }
}
