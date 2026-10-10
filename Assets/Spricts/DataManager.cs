using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

// 数据管理：负责配料/菜肴的增删与 JSON 持久化，数据保存后下次启动自动复用。
// 用法：从你自己的任意脚本里直接调用 DataManager.Instance.xxx 即可，无需在场景中手动挂载。
public class DataManager : MonoBehaviour
{
    private static DataManager _instance;

    // 懒加载单例：首次访问时自动创建
    public static DataManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<DataManager>();
                if (_instance == null)
                    _instance = new GameObject("DataManager").AddComponent<DataManager>();
            }
            return _instance;
        }
    }

    public List<Ingredient> ingredients = new List<Ingredient>();
    public List<Dish> dishes = new List<Dish>();

    // 数据变化时触发（列表控制器订阅它来自动刷新）
    public static System.Action OnChanged;

    private string ingredientPath;
    private string dishPath;

    [Serializable] private class IngredientList { public List<Ingredient> items = new List<Ingredient>(); }
    [Serializable] private class DishList { public List<Dish> items = new List<Dish>(); }

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        ingredientPath = Path.Combine(Application.persistentDataPath, "ingredients.json");
        dishPath = Path.Combine(Application.persistentDataPath, "dishes.json");
        Load();
    }

    // ---------- 持久化 ----------

    private void Load()
    {
        ingredients = LoadIngredients();
        dishes = LoadDishes();
    }

    private List<Ingredient> LoadIngredients()
    {
        if (!File.Exists(ingredientPath)) return new List<Ingredient>();
        var w = JsonUtility.FromJson<IngredientList>(File.ReadAllText(ingredientPath, Encoding.UTF8));
        return (w != null && w.items != null) ? w.items : new List<Ingredient>();
    }

    private List<Dish> LoadDishes()
    {
        if (!File.Exists(dishPath)) return new List<Dish>();
        var w = JsonUtility.FromJson<DishList>(File.ReadAllText(dishPath, Encoding.UTF8));
        return (w != null && w.items != null) ? w.items : new List<Dish>();
    }

    private void Save()
    {
        try
        {
            File.WriteAllText(ingredientPath, JsonUtility.ToJson(new IngredientList { items = ingredients }, true), Encoding.UTF8);
            File.WriteAllText(dishPath, JsonUtility.ToJson(new DishList { items = dishes }, true), Encoding.UTF8);
        }
        catch (Exception e)
        {
            Debug.LogError("保存数据失败: " + e.Message);
        }

        if (OnChanged != null) OnChanged();
    }

    // ---------- 配料操作 ----------

    public void AddIngredient(Ingredient ing)
    {
        if (ing == null || string.IsNullOrEmpty(ing.name)) return;
        ingredients.Add(ing);
        Save();
    }

    public void RemoveIngredient(Ingredient ing)
    {
        if (ingredients.Remove(ing)) Save();
    }

    public Ingredient FindIngredient(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        foreach (var i in ingredients)
            if (i.name == name) return i;
        return null;
    }

    public bool IngredientNameExists(string name)
    {
        return FindIngredient(name) != null;
    }

    public List<string> IngredientNames()
    {
        var list = new List<string>();
        foreach (var i in ingredients) list.Add(i.name);
        return list;
    }

    // ---------- 菜肴操作 ----------

    public void AddDish(Dish dish)
    {
        if (dish == null || string.IsNullOrEmpty(dish.name)) return;
        dishes.Add(dish);
        Save();
    }

    public void RemoveDish(Dish dish)
    {
        if (dishes.Remove(dish)) Save();
    }

    public Dish FindDish(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        foreach (var d in dishes)
            if (d.name == name) return d;
        return null;
    }

    public List<string> DishNames()
    {
        var list = new List<string>();
        foreach (var d in dishes) list.Add(d.name);
        return list;
    }
}
