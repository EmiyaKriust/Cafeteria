using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

// 生成表格里「单个配料」的行（放在「配料行」预制体上）：配料名文本 + 采购总量输入框。
public class TotalIngredientRow : MonoBehaviour
{
    public Text nameText;
    public InputField totalInput;   // 采购总量（g）

    // 预填：采购总量 = 每人份用量 × 人数
    public void Init(string ingredientName, float gramsPerPerson, int diners)
    {
        if (nameText) nameText.text = ingredientName;
        float total = gramsPerPerson * diners;
        if (totalInput) totalInput.text = total > 0 ? total.ToString("F1", CultureInfo.InvariantCulture) : "";
    }

    public XlsxWriter.IngredientEntry ToEntry()
    {
        float total = 0f;
        if (totalInput != null)
            float.TryParse(totalInput.text, NumberStyles.Float, CultureInfo.InvariantCulture, out total);

        return new XlsxWriter.IngredientEntry
        {
            name = nameText != null ? nameText.text : "",
            totalGrams = total
        };
    }
}
