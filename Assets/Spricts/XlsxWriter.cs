using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using UnityEngine;

// 纯 C# 生成单日带量菜谱 .xlsx（不依赖第三方库）。
// 表格结构与示例文件一致：
//   标题行（星期X（X月X日）） | 表头（菜肴名称/配料/克数/能量/营养素含量） | 逐菜肴列出配料
// 其中“克数”= 采购总量 ÷ 用餐人数（向下取整）；能量与营养素由配料每 100g 数据反算。
public static class XlsxWriter
{
    // 单个配料的采购数据
    [Serializable]
    public class IngredientEntry
    {
        public string name;        // 配料名
        public float totalGrams;   // 采购总量（g）
    }

    // 单日中的一个菜肴
    [Serializable]
    public class DishRow
    {
        public string dishName;
        public List<IngredientEntry> ingredients = new List<IngredientEntry>();
    }

    private static readonly string[] COLS = { "A", "B", "C", "D", "E", "F", "G" };

    // 生成单日带量菜谱，返回保存的文件完整路径。
    //
    // 参数：
    //   title   表格标题，如 "星期一（9月7日）"
    //   diners  用餐人数（>0）
    //   dishes  当日菜肴列表；每个菜肴含 dishName 与多个配料，
    //           每个配料填 totalGrams = 采购总量(g)，表格里的"克数" = 总量 ÷ 人数（向下取整）
    //
    // 调用示例：
    //   var rows = new List<XlsxWriter.DishRow>();
    //   var dr = new XlsxWriter.DishRow { dishName = "麻辣小肉块" };
    //   dr.ingredients.Add(new XlsxWriter.IngredientEntry { name = "油豆腐", totalGrams = 60000 });
    //   dr.ingredients.Add(new XlsxWriter.IngredientEntry { name = "带皮夹心肉", totalGrams = 35000 });
    //   rows.Add(dr);
    //   string path = XlsxWriter.GenerateDayTable("星期一（9月7日）", 1000, rows);
    public static string GenerateDayTable(string title, int diners, List<DishRow> dishes)
    {
        if (diners <= 0) diners = 1;

        // 预先计算每个菜肴：每人份克数（向下取整）、能量、营养素
        var computed = new List<ComputedDish>();
        foreach (var dish in dishes)
        {
            var cd = new ComputedDish { dishName = dish.dishName };
            foreach (var ing in dish.ingredients)
            {
                float grams = Mathf.FloorToInt(ing.totalGrams / diners);
                var ingData = DataManager.Instance != null ? DataManager.Instance.FindIngredient(ing.name) : null;
                float kcal100 = ingData != null ? ingData.kcal : 0f;
                float p100 = ingData != null ? ingData.protein : 0f;
                float f100 = ingData != null ? ingData.fat : 0f;
                float c100 = ingData != null ? ingData.carbohydrate : 0f;

                cd.ingredientNames.Add(ing.name);
                cd.grams.Add((int)grams);
                cd.energy += kcal100 * grams / 100f;
                cd.protein += p100 * grams / 100f;
                cd.fat += f100 * grams / 100f;
                cd.carbohydrate += c100 * grams / 100f;
            }
            computed.Add(cd);
        }

        // 组装 sheet1.xml
        string sheetXml = BuildSheetXml(title, computed);

        // 写入 xlsx 压缩包
        string dir = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        string safeTitle = SanitizeFileName(title);
        string path = Path.Combine(dir, "带量菜谱-" + safeTitle + ".xlsx");

        using (var fs = new FileStream(path, FileMode.Create))
        using (var zip = new ZipArchive(fs, ZipArchiveMode.Create))
        {
            AddEntry(zip, "[Content_Types].xml", ContentTypesXml());
            AddEntry(zip, "_rels/.rels", RootRelsXml());
            AddEntry(zip, "xl/workbook.xml", WorkbookXml());
            AddEntry(zip, "xl/_rels/workbook.xml.rels", WorkbookRelsXml());
            AddEntry(zip, "xl/styles.xml", StylesXml());
            AddEntry(zip, "xl/worksheets/sheet1.xml", sheetXml);
        }

        return path;
    }

    // 计算后的菜肴数据
    private class ComputedDish
    {
        public string dishName;
        public List<string> ingredientNames = new List<string>();
        public List<int> grams = new List<int>();
        public float energy;
        public float protein;
        public float fat;
        public float carbohydrate;
    }

    private static string BuildSheetXml(string title, List<ComputedDish> dishes)
    {
        var sb = new StringBuilder();
        sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
        sb.Append("<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">");
        sb.Append("<sheetViews><sheetView workbookViewId=\"0\"/></sheetViews>");
        sb.Append("<sheetFormatPr defaultRowHeight=\"15\"/>");
        sb.Append("<sheetData>");

        var merges = new List<string>();
        merges.Add("A1:G1");

        // 第 1 行：标题（合并 A1:G1）
        sb.Append("<row r=\"1\" ht=\"24\">");
        sb.Append(StrCell("A1", 1, title));
        for (int c = 1; c < COLS.Length; c++)
            sb.Append(EmptyCell(COLS[c] + "1", 1));
        sb.Append("</row>");

        // 第 2 行：表头（菜肴名称 合并 A2:C2）
        sb.Append("<row r=\"2\" ht=\"20\">");
        sb.Append(StrCell("A2", 2, "菜肴名称"));
        sb.Append(EmptyCell("B2", 2));
        sb.Append(EmptyCell("C2", 2));
        sb.Append(StrCell("D2", 2, "配料"));
        sb.Append(StrCell("E2", 2, "克数"));
        sb.Append(StrCell("F2", 2, "能量"));
        sb.Append(StrCell("G2", 2, "营养素含量"));
        sb.Append("</row>");
        merges.Add("A2:C2");

        int row = 3;
        foreach (var dish in dishes)
        {
            int firstRow = row;
            int count = Mathf.Max(1, dish.ingredientNames.Count);

            for (int i = 0; i < count; i++)
            {
                sb.Append("<row r=\"").Append(row).Append("\">");

                // A：菜肴名（仅首行）；B、C 为合并空白
                if (i == 0)
                    sb.Append(StrCell("A" + row, 3, dish.dishName));
                else
                    sb.Append(EmptyCell("A" + row, 3));
                sb.Append(EmptyCell("B" + row, 3));
                sb.Append(EmptyCell("C" + row, 3));

                // D：配料名；E：克数
                sb.Append(StrCell("D" + row, 3, dish.ingredientNames[i]));
                sb.Append(NumCell("E" + row, 3, dish.grams[i]));

                // F：能量（仅首行）；G：营养素（仅首行）
                if (i == 0)
                {
                    sb.Append(StrCell("F" + row, 3, Mathf.FloorToInt(dish.energy) + "kcal"));
                    sb.Append(StrCell("G" + row, 4, NutrientText(dish)));
                }
                else
                {
                    sb.Append(EmptyCell("F" + row, 3));
                    sb.Append(EmptyCell("G" + row, 3));
                }

                sb.Append("</row>");
                row++;
            }

            int lastRow = row - 1;
            merges.Add("A" + firstRow + ":C" + lastRow);
            merges.Add("F" + firstRow + ":F" + lastRow);
            merges.Add("G" + firstRow + ":G" + lastRow);
        }

        sb.Append("</sheetData>");

        if (merges.Count > 0)
        {
            sb.Append("<mergeCells count=\"").Append(merges.Count).Append("\">");
            foreach (var m in merges)
                sb.Append("<mergeCell ref=\"").Append(m).Append("\"/>");
            sb.Append("</mergeCells>");
        }

        sb.Append("</worksheet>");
        return sb.ToString();
    }

    private static string NutrientText(ComputedDish dish)
    {
        return "P:" + dish.protein.ToString("F1") +
               "\nF:" + dish.fat.ToString("F1") +
               "\nC:" + dish.carbohydrate.ToString("F1");
    }

    // ---------- 单元格构建 ----------

    private static string StrCell(string refName, int style, string text)
    {
        return "<c r=\"" + refName + "\" s=\"" + style + "\" t=\"inlineStr\"><is><t xml:space=\"preserve\">" +
               Escape(text) + "</t></is></c>";
    }

    private static string NumCell(string refName, int style, int value)
    {
        return "<c r=\"" + refName + "\" s=\"" + style + "\"><v>" + value + "</v></c>";
    }

    private static string EmptyCell(string refName, int style)
    {
        return "<c r=\"" + refName + "\" s=\"" + style + "\"/>";
    }

    private static string Escape(string s)
    {
        return s.Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("\n", "&#10;");
    }

    private static string SanitizeFileName(string s)
    {
        foreach (var ch in Path.GetInvalidFileNameChars())
            s = s.Replace(ch, '_');
        return s;
    }

    // ---------- 固定 XML 部件 ----------

    private static string ContentTypesXml()
    {
        return "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
            "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
            "<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>" +
            "<Default Extension=\"xml\" ContentType=\"application/xml\"/>" +
            "<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>" +
            "<Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>" +
            "<Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/>" +
            "</Types>";
    }

    private static string RootRelsXml()
    {
        return "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
            "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
            "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/>" +
            "</Relationships>";
    }

    private static string WorkbookXml()
    {
        return "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
            "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" " +
            "xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">" +
            "<sheets><sheet name=\"Sheet1\" sheetId=\"1\" r:id=\"rId1\"/></sheets>" +
            "</workbook>";
    }

    private static string WorkbookRelsXml()
    {
        return "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
            "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
            "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/>" +
            "<Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/>" +
            "</Relationships>";
    }

    private static string StylesXml()
    {
        return "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
            "<styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">" +
            "<fonts count=\"3\">" +
            "<font><name val=\"宋体\"/><sz val=\"11\"/></font>" +
            "<font><name val=\"宋体\"/><b/><sz val=\"11\"/></font>" +
            "<font><name val=\"宋体\"/><b/><sz val=\"14\"/></font>" +
            "</fonts>" +
            "<fills count=\"3\">" +
            "<fill><patternFill patternType=\"none\"/></fill>" +
            "<fill><patternFill patternType=\"gray125\"/></fill>" +
            "<fill><patternFill patternType=\"solid\"><fgColor rgb=\"FFD9E1F2\"/><bgColor indexed=\"64\"/></patternFill></fill>" +
            "</fills>" +
            "<borders count=\"2\">" +
            "<border><left/><right/><top/><bottom/><diagonal/></border>" +
            "<border><left style=\"thin\"><color rgb=\"FF000000\"/></left>" +
            "<right style=\"thin\"><color rgb=\"FF000000\"/></right>" +
            "<top style=\"thin\"><color rgb=\"FF000000\"/></top>" +
            "<bottom style=\"thin\"><color rgb=\"FF000000\"/></bottom><diagonal/></border>" +
            "</borders>" +
            "<cellStyleXfs count=\"1\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/></cellStyleXfs>" +
            "<cellXfs count=\"5\">" +
            "<xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\"/>" +
            "<xf numFmtId=\"0\" fontId=\"2\" fillId=\"0\" borderId=\"1\" xfId=\"0\" applyAlignment=\"1\"><alignment horizontal=\"center\" vertical=\"center\"/></xf>" +
            "<xf numFmtId=\"0\" fontId=\"1\" fillId=\"2\" borderId=\"1\" xfId=\"0\" applyAlignment=\"1\"><alignment horizontal=\"center\" vertical=\"center\"/></xf>" +
            "<xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"1\" xfId=\"0\" applyAlignment=\"1\"><alignment horizontal=\"center\" vertical=\"center\"/></xf>" +
            "<xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"1\" xfId=\"0\" applyAlignment=\"1\"><alignment horizontal=\"center\" vertical=\"center\" wrapText=\"1\"/></xf>" +
            "</cellXfs>" +
            "</styleSheet>";
    }

    private static void AddEntry(ZipArchive zip, string name, string content)
    {
        var entry = zip.CreateEntry(name, System.IO.Compression.CompressionLevel.Fastest);
        using (var sw = new StreamWriter(entry.Open(), new UTF8Encoding(false)))
        {
            sw.Write(content);
        }
    }
}
