namespace ArticleCacheFiller.Models;

/// <summary>
/// The filler's own copy of an article: the same fields as in ArticleService, so the JSON written to the
/// cache is exactly what ArticleService reads back.
/// </summary>
public class Article
{
    public Guid Id { get; set; }
    public required string Title { get; set; }
    public required string Content { get; set; }
    public required string Author { get; set; }
    public DateTime PublishedAt { get; set; }
    public required string Region { get; set; }
}
