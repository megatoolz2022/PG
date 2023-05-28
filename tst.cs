using HtmlAgilityPack;

using System;
using System.Data.Entity;
using System.Reflection;
using System.Security.Policy;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;


public class MyDbContext : DbContext
{
    public MyDbContext()
        : base("Server=srvsql.vzrk.local\\sql;Database=MarketDev;User Id=DevUser;Password=1;")
    {
    }



    public DbSet<Category> Categories { get; set; }
}

class Program
{

    public static Dictionary<int, string> SectionList = new Dictionary<int, string>();
    public static Dictionary<int, string> TempSectionList = new Dictionary<int, string>();


    static int ExtractIdFromUrl(string url)
    {
        var match = Regex.Match(url, "/catalog/(?<id>\\d+)");
        if (match.Success)
        {
            return int.Parse(match.Groups["id"].Value);
        }
        return 0;
    }


    static async Task GetSectionListAsync(string uri, int? parentId = null)
    {
        var httpClient = new HttpClient();
        var html = await httpClient.GetStringAsync(uri);
        var htmlDocument = new HtmlDocument();
        htmlDocument.LoadHtml(html);
        var contentListCategories = htmlDocument.DocumentNode.SelectNodes("//ul[@class='contentListCategories']");
        var listItemsContentListCategories = htmlDocument.DocumentNode.SelectNodes("//ul[@class='contentListCategories']/li");
        var metaKeywords = htmlDocument.DocumentNode.SelectSingleNode("//meta[@name='keywords']")?.GetAttributeValue("content", "");
        var metaDescription = htmlDocument.DocumentNode.SelectSingleNode("//meta[@name='description']")?.GetAttributeValue("content", "");
        var metaTitle = htmlDocument.DocumentNode.SelectSingleNode("//title")?.InnerText;
        var bottomDescription = htmlDocument.DocumentNode.SelectSingleNode("//div[@class='contentDescriptionTextFull']")?.InnerText;
        var description = htmlDocument.DocumentNode.SelectSingleNode("//div[@class='contentDescriptionText']")?.InnerText;
        var itemsInCategory = contentListCategories == null ? true : false;

        if (contentListCategories == null)
        {
            try
            {
                //find a row where idCatalog = parentId (idCatalog is not primarykey) and set ItemsInCategory = 1
                using (var db = new MyDbContext())
                {
                    var categoriesToUpdate = db.Categories.Where(c => c.IdCatalog == parentId);

                    foreach (var cat in categoriesToUpdate)
                    {
                        cat.ItemsInCategory = true;
                    }
                    db.SaveChanges();
                }


            }
            catch (TargetInvocationException ex)
            {
                Console.WriteLine("TargetInvocationException caught: " + ex.Message);
                if (ex.InnerException != null)
                {
                    Console.WriteLine("InnerException: " + ex.InnerException.Message);
                }
            }


            Console.WriteLine("Обнаружена категория без подкатегорий: " + uri);
        }
        if (listItemsContentListCategories != null)
        {
            foreach (var item in listItemsContentListCategories)
            {
                var imgLink = item.SelectSingleNode(".//img").GetAttributeValue("src", "");
                var name = item.SelectSingleNode(".//strong").InnerText;
                var url = item.SelectSingleNode(".//a").GetAttributeValue("href", "");
                var idCatalog = ExtractIdFromUrl(url);

                Console.WriteLine("Имя категории: " + item.Name + " урл категории: " + url + " ид каталога: " + idCatalog);

                if (TempSectionList.ContainsKey(idCatalog))
                {
                    Console.WriteLine("Обнаружен дубль: " + idCatalog + " Имя: " + name);
                }
                else
                {
                    TempSectionList.Add(idCatalog, "https://www.officemag.ru" + url);

                }

                using (var db = new MyDbContext())
                {
                    var cat = new Category(default, name, idCatalog, imgLink, url, parentId, description, bottomDescription, metaKeywords, metaDescription, metaTitle, itemsInCategory);
                    db.Categories.Add(cat);
                    db.SaveChanges();
                }


            }
        }


    }

    static async Task FormUrlListAsync(string? url = null)
    {

        if (url != null)
        {
            await GetSectionListAsync(url);
        }
        foreach (var item in SectionList)
        {
            //// Если значение ключа "final", пропускаем его
            //if (item.Value == "final")
            //    continue;

            // Иначе вызываем "получить список разделов" с текущим ключом и значением
            await GetSectionListAsync(item.Value, item.Key);
        }

        if (TempSectionList.Count > 0)
        {
            SectionList.Clear();
            foreach (var item in TempSectionList)
            {
                SectionList.Add(item.Key, item.Value);
            }
            TempSectionList.Clear();
        }
        else
        {
            SectionList.Clear();
            TempSectionList.Clear();
        }






    }



    static async Task Main(string[] args)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        await FormUrlListAsync("https://www.officemag.ru/catalog/");


        //while (!SectionList.All(item => item.Value == "final"))
        //{

        //        await FormUrlListAsync();
        //}

        while (SectionList.Count > 0)
        {
            await FormUrlListAsync();
        }

    }
}
