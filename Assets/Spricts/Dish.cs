using System;
using System.Collections.Generic;

// 菜肴中的一种配料，以及该配料的“每人份用量”（g），
// 作为生成表格时的默认值（表格里可再按实际采购总量调整）。
[Serializable]
public class DishIngredient
{
    public string ingredientName;  // 配料名（引用配料库）
    public float gramsPerPerson;   // 每人份用量（g）

    public DishIngredient() { }

    public DishIngredient(string name, float grams)
    {
        ingredientName = name;
        gramsPerPerson = grams;
    }
}

// 菜肴：名字 + 配料列表
[Serializable]
public class Dish
{
    public string name;
    public List<DishIngredient> ingredients = new List<DishIngredient>();

    public Dish() { }

    public Dish(string name)
    {
        this.name = name;
    }
}
