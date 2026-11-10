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
