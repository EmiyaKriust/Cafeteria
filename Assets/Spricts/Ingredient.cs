using System;

// 配料（食材）：保存名字以及每 100g 的营养数据
// 热量单位为 kcal，蛋白质/脂肪/碳水单位为 g，均按“每 100g”计，
// 这样菜肴的总能量与营养素可由“Σ(每100g数值 × 克数 / 100)”反算得到。
[Serializable]
public class Ingredient
{
    public string name;          // 配料名
    public float kcal;           // 热量（每 100g，kcal）
    public float protein;        // 蛋白质（每 100g，g）
    public float fat;            // 脂肪（每 100g，g）
    public float carbohydrate;   // 碳水化合物（每 100g，g）

    public Ingredient() { }

    public Ingredient(string name, float kcal, float protein, float fat, float carbohydrate)
    {
        this.name = name;
        this.kcal = kcal;
        this.protein = protein;
        this.fat = fat;
        this.carbohydrate = carbohydrate;
    }
}
