# Sustainability – three tactics from the Green Architecture Framework

Week 45 asks for three tactics from Bærbak's [Green Architecture Framework (GAF)](https://baerbak.cs.au.dk/papers/gaf.html)
that Happy Headlines didn't use yet, with the code before and after.

Already in the project before week 45, so not counted here: *Cache Data Closer to the User* (ArticleCache and
CommentCache, week 40), *Utilize Geographical Sharding* (one article database per continent, week 36),
*Independent Scaling of Modular Architecture* (microservices, 3 ArticleService instances) and database indexes
(*Use Efficient Algorithms*).

**How we measure** (GAF process tactic *Measure and Experiment*): energy itself can't be measured from inside
Docker on Windows, so [tools/measure.sh](tools/measure.sh) measures what drives it: **MB per image** and **bytes
per request**. Fewer bytes stored, sent and parsed means less work for disks, networks and CPUs. The numbers below
are those stand-ins, not joules. The script loads a fixed test set first: 300 articles per region over the last
30 days (about 10 a day), each with ~2 KB of text.

| Tactic | GAF category | Before | After | Saved |
|---|---|---|---|---|
| [Reduce Bundle Size](#1-reduce-bundle-size) | Avoid Unnecessary Resources | 1,912 MB (8 images) | 1,039 MB | **46 %** |
| [Reduce Network Package Size](#2-reduce-network-package-size) | Avoid Unnecessary Resources | 5,395,668 bytes per daily newsletter | 165,830 bytes | **97 %** |
| [Defer Fetching](#3-defer-fetching) | Avoid Unnecessary Resources | 673,895 bytes per article list | 18,447 bytes | **97 %** |

## 1. Reduce Bundle Size

*"Deployment units like docker containers need to be transferred to the executing machine, and the larger these
bundles are, the more energy is spent on transport, storage, and execution."* GAF's own example is a Docker base
image: Ubuntu + JDK 914 MB versus Alpine + JRE 148 MB.

**Why here:** every service image was built on the Debian-based ASP.NET runtime and was about 240 MB. The pipeline
(week 43) builds and pushes 8 of them on every push to `main`, and every server pulls them – so the bundle size is
paid again and again.

**Before** – all 8 Dockerfiles, e.g. [ArticleService/Dockerfile](src/ArticleService/Dockerfile):

```dockerfile
# Runtime stage: only the published app on the smaller ASP.NET runtime image (listens on port 8080)
FROM mcr.microsoft.com/dotnet/aspnet:10.0
```

**After:**

```dockerfile
# Runtime stage: only the published app on the small Alpine-based ASP.NET runtime image (listens on port 8080).
# GAF "Reduce Bundle Size": Alpine instead of the Debian-based default roughly halves the image.
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine
```

**Result** (`docker image inspect`, uncompressed size):

| Image | Before | After |
|---|---|---|
| article-service | 243 MB | 133 MB |
| article-cache-filler | 240 MB | 131 MB |
| comment-service | 243 MB | 134 MB |
| profanity-service | 239 MB | 130 MB |
| draft-service | 239 MB | 130 MB |
| publisher-service | 234 MB | 125 MB |
| newsletter-service | 234 MB | 125 MB |
| subscriber-service | 240 MB | 131 MB |
| **total** | **1,912 MB** | **1,039 MB** (−46 %) |

**Trade-off:** Alpine uses a smaller C library (musl) and runs .NET in *invariant globalization* mode (no
country-specific number and date formats). Happy Headlines doesn't format anything per culture, and every service
was tested on the new images: all requests worked and no service logged an error.

## 2. Reduce Network Package Size

*"Every byte transferred costs energy, so avoid transferring unnecessary data."*

**Why here:** the daily newsletter needs the number of articles from the last 24 hours. It asked ArticleService for
**every** article in all 8 regions – full text included – and then threw almost all of them away on its own side.

**Before** – ArticleService sends the whole region
([ArticlesController.cs](src/ArticleService/Controllers/ArticlesController.cs)):

```csharp
[HttpGet]
public async Task<ActionResult<List<Article>>> GetAll(Region region)
{
    await using var db = router.Open(region);
    return await db.Articles.AsNoTracking().OrderByDescending(a => a.PublishedAt).ToListAsync();
}
```

… and NewsletterService filters after the download
([DailyNewsletter.cs](src/NewsletterService/Newsletters/DailyNewsletter.cs)):

```csharp
var latest = await articles.GetArticlesAsync(region, cancellationToken);
articleCount += latest.Count(a => a.PublishedAt >= since);
```

**After** – the filter moves to where the data is. ArticleService takes `?since=` and only sends newer articles
(the database index on `PublishedAt` finds them):

```csharp
// GAF "Reduce Network Package Size": ?since=<time> sends only the articles published after that time,
// so a caller that needs the last 24 hours doesn't download the whole region (the index on PublishedAt helps)
[HttpGet]
public async Task<ActionResult<List<Article>>> GetAll(Region region, DateTime? since)
{
    await using var db = router.Open(region);
    var articles = db.Articles.AsNoTracking();
    if (since is not null)
    {
        articles = articles.Where(a => a.PublishedAt > since.Value.ToUniversalTime());
    }

    return await articles.OrderByDescending(a => a.PublishedAt).ToListAsync();
}
```

```csharp
var latest = await articles.GetArticlesSinceAsync(region, since, cancellationToken);
articleCount += latest.Count;
```

**Result** (one daily newsletter = 8 requests, one per region):

| | Before | After |
|---|---|---|
| bytes downloaded | 5,395,668 | 165,830 (−97 %) |
| articles counted | 89 | 89 – same newsletter |

Less is also read from the 8 databases, serialized to JSON and parsed again, so all three parties – the databases,
ArticleService and NewsletterService – do less work. The saving grows with the archive: the old request got bigger
every day, the new one stays at "one day of articles".

## 3. Defer Fetching

*"Transferring data that is never used is a waste of energy. So to combat that defer the fetching until the data
is actually used by the user. One such technique for lists of data is to use pagination."*

**Why here:** the website shows *"the ten most recent articles"*, but the article list sent **every** article in
the region – 300 in the test set, and more every day.

**Before** – the whole region, newest first:

```csharp
return await articles.OrderByDescending(a => a.PublishedAt).ToListAsync();
```

**After** – one page at a time ([ArticlesController.cs](src/ArticleService/Controllers/ArticlesController.cs)).
Without parameters a caller gets the 10 newest articles; the next page is only fetched when someone asks for it:

```csharp
//   ?page=1&pageSize=10    GAF "Defer Fetching": one page of the newest articles; the next page is only
//                          fetched if someone asks for it. Without parameters: the 10 newest, as on the website.
[HttpGet]
public async Task<ActionResult<List<Article>>> GetAll(Region region, DateTime? since,
    [Range(1, int.MaxValue)] int page = 1, [Range(1, MaxPageSize)] int pageSize = DefaultPageSize)
{
    await using var db = router.Open(region);
    var articles = db.Articles.AsNoTracking().OrderByDescending(a => a.PublishedAt);

    if (since is not null)
    {
        // A time window is already limited by time, so it isn't cut into pages
        return await articles.Where(a => a.PublishedAt > since.Value.ToUniversalTime()).ToListAsync();
    }

    return await articles.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
}

private const int DefaultPageSize = 10;     // the website shows the ten most recent articles
private const int MaxPageSize = 50;         // a bigger page is answered with 400 Bad Request
```

`Skip`/`Take` become `OFFSET`/`LIMIT` in the SQL, so the database also reads only one page.

**Result** (`GET /api/regions/Global/articles` without parameters):

| | Before | After |
|---|---|---|
| articles in the answer | 300 + the sample articles | 10 |
| bytes | 673,895 | 18,447 (−97 %) |

**Trade-off:** a reader who scrolls far back needs several requests instead of one, and `pageSize` is capped at 50
so a client can't ask for everything at once. Because most readers only look at the newest articles, far fewer
bytes are sent in total.

## Considered, but not chosen

- **Reduce Logging** (*Accept Lower Fidelity*): keeping, say, only 25 % of the traces would save energy, but
  monitoring is a requirement of its own ("design to be monitored"), and a missing trace is exactly the one you
  need when something breaks. A trade-off we chose not to make.
- **Use Low-footprint Data Formats** (compressing the JSON answers): fewer bytes, but every answer then costs CPU
  to compress and decompress. After tactics 2 and 3 the answers are already small, so the gain would be small.

## Measure it yourself

```sh
cd week-45
docker compose up -d --build
sh tools/measure.sh
```
