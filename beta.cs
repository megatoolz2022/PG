using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

using HtmlAgilityPack;

using Microsoft.EntityFrameworkCore;

public class WebScraper
{
    private readonly DataBaseContext _context;

    public WebScraper(DataBaseContext context)
    {
        _context = context;
        ProductLinks = new List<string>();
        CategoryId = 0;
    }
    public List<string> ProductLinks { get; private set; }
    public int CategoryId { get; set; }
    public WebScraper()
    {
        ProductLinks = new List<string>();
        CategoryId = 0;

    }

    public async Task FillProductListAsync(string uri)
    {
        try
        {
            var web = new HtmlWeb();
            var doc = await web.LoadFromWebAsync(uri);
            string categoryID = Regex.Match(uri, @"\d+").Value;

            var ulNode = doc.DocumentNode.SelectSingleNode("//ul[contains(@class, 'listItems--productList')]");

            if (ulNode != null)
            {
                var liNodes = ulNode.SelectNodes(".//li[contains(@class, 'listItem')]");

                foreach (var li in liNodes)
                {
                    var aNode = li.SelectSingleNode(".//a[contains(@class, 'listItemPhoto__link')]");

                    if (aNode != null)
                    {
                        var hrefValue = aNode.GetAttributeValue("href", null);
                        if (hrefValue != null)
                        {
                            ProductLinks.Add(hrefValue);
                        }
                    }
                }
            }

            var nextLinkNode = doc.DocumentNode.SelectSingleNode("//li[contains(@class, 'forw')]/a/@href");

            if (nextLinkNode != null)
            {
                var hrefValue = nextLinkNode.GetAttributeValue("href", null);
                var pagen = Regex.Match(hrefValue, @"PAGEN_1=\d+").Value;
                var nextUrl = "/catalog/" + categoryID + "/?" + pagen;
                if (nextUrl != null)
                {
                    FillProductListAsync("https://www.officemag.ru" + nextUrl + "/");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex.Message);
        }
    }

    public async Task<Product> ProcessProductPageAsync(string uri)
    {
        try
        {
            HttpClient httpClient = new HttpClient();
            var html = await httpClient.GetStringAsync(uri);
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            Product product = new Product();
            product.Uri = uri;

            HtmlNode codeSpan = doc.DocumentNode.SelectSingleNode("//span[contains(@class, 'code')]");
            var productCode = codeSpan?.GetAttributeValue("value", null);

            var uriNumbers = Regex.Match(uri, @"\d+").Value;

            product.MetaTitle = doc.DocumentNode.SelectSingleNode("//title")?.InnerText;
            product.MetaDescription = doc.DocumentNode.SelectSingleNode("//meta[@name='description']")?.GetAttributeValue("content", null);
            product.MetaKeywords = doc.DocumentNode.SelectSingleNode("//meta[@name='keywords']")?.GetAttributeValue("content", null);

            var imageNodes = doc.DocumentNode.SelectNodes("//ul[contains(@class, 'ProductPhotoThumbs')]//li//a[contains(@class, 'ProductPhotoThumb__link')]");
            if (imageNodes != null)
            {
                product.ImageLinks = imageNodes.Select(a => a.GetAttributeValue("href", null)).ToList();
            }

            product.FullProductName = doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'Product__name')]")?.InnerText;
            product.ProductName = product.FullProductName?.Split(',')[0];

            var fullDescriptionNode = doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'infoDescription__full')]");
            if (fullDescriptionNode != null)
            {
                product.FullDescription = string.Join(' ', fullDescriptionNode.SelectNodes(".//p")?.Select(p => p.InnerText) ?? new List<string>());
            }

            product.ShortDescription = doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'infoDescription__about')]//p")?.InnerText;
            product.FeaturesHtml = doc.DocumentNode.SelectSingleNode("//ul[contains(@class, 'infoFeatures')]")?.OuterHtml;


            var productEntity = new ProductEntity
            {
                // ... инициализация свойств ...

                Images = product.ImageLinks.Select(link => new ImageEntity { ImageLink = link, GoodsId = int.Parse(uriNumbers) }).ToList()
            };

            _context.Products.Add(productEntity);
            await _context.SaveChangesAsync();

            return product;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex.Message);
            if (ex.InnerException != null)
            {
                Console.WriteLine("Inner exception: " + ex.InnerException.Message);
            }
            return null;
        }
    }

}


public class Product
{
    public string Uri { get; set; }
    public string MetaTitle { get; set; }
    public string MetaDescription { get; set; }
    public string MetaKeywords { get; set; }
    public List<string> ImageLinks { get; set; }
    public string FullProductName { get; set; }
    public string ProductName { get; set; }
    public string? FullDescription { get; set; }
    public string? ShortDescription { get; set; }
    public string FeaturesHtml { get; set; }
}
public class ProductEntity
{
    [Key]
    public int Id { get; set; }
    public int GoodsId { get; set; }
    public int CategoriesId { get; set; }
    public int ProductCode { get; set; }
    public string Uri { get; set; }
    public string MetaTitle { get; set; }
    public string MetaDescription { get; set; }
    public string MetaKeywords { get; set; }
    public string FullProductName { get; set; }
    public string ProductName { get; set; }
    public string? FullDescription { get; set; }
    public string? ShortDescription { get; set; }
    public string? FeaturesHtml { get; set; }
    public List<ImageEntity> Images { get; set; }
}

public class ImageEntity
{
    public int Id { get; set; }
    public string ImageLink { get; set; }
    public int GoodsId { get; set; }
    public ProductEntity Product { get; set; }
}
public class CategoryEntity
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; }

    public int IdCatalog { get; set; }

    public string ImgLink { get; set; }

    public string Url { get; set; }

    public int? ParentCategoryId { get; set; }

    public string? Description { get; set; }

    public string? BottomDescription { get; set; }

    public string? MetaKeywords { get; set; }

    public string? MetaDescription { get; set; }

    public string? MetaTitle { get; set; }

    public bool? ItemsInCategory { get; set; }
    public object Value { get; }
    public int? ParentId { get; }
}

public class DataBaseContext : DbContext
{
    public DbSet<ProductEntity> Products { get; set; }
    public DbSet<ImageEntity> Images { get; set; }

    public DbSet<CategoryEntity> Categories { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer(@"Server=srvsql.vzrk.local\sql;Database=MarketDev;User Id=DevUser;Password=1;TrustServerCertificate=True;");
    }
}




public class Program
{
    private static readonly DataBaseContext _context = new DataBaseContext();

    static async Task Main(string[] args)
    {
        var scraper = new WebScraper(_context);

        var lst = GetCatalogsWithGoods();

        foreach (var item in lst)
        {
            scraper.CategoryId = item.IdCatalog;
            await scraper.FillProductListAsync("https://www.officemag.ru" + item.Url);
            foreach (var link in scraper.ProductLinks)
            {
                await scraper.ProcessProductPageAsync("https://www.officemag.ru" + link);
            }
            scraper.ProductLinks.Clear();
        }
    }

    static List<CategoryEntity> GetCatalogsWithGoods()
    {
        var idCatalogs = _context.Categories.Where(item => item.ItemsInCategory == true)
            .Select(category => new CategoryEntity { IdCatalog = category.IdCatalog, Url = category.Url })
            .ToList();

        return idCatalogs;
    }
}
