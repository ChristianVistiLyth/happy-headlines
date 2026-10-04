# Week 36 – ArticleService

A REST API for articles (create, read, update, delete), scaled in two ways:

- **x-axis split:** 3 identical ArticleService instances behind an nginx load balancer.
- **z-axis split:** one PostgreSQL database per continent + one for global news (8 in total).

## How ArticleService scales

```mermaid
flowchart TB
    client["Client<br/>Website, NewsletterService"]
    lb["ArticleLoadBalancer<br/>sends each request to the next instance"]

    subgraph xaxis[" "]
        direction LR
        xlabel["x-axis split<br/>3 identical instances"]
        s1{{"ArticleService<br/>instance 1"}}
        s2{{"ArticleService<br/>instance 2"}}
        s3{{"ArticleService<br/>instance 3"}}
    end

    router["selects the database<br/>by the region in the URL"]

    subgraph zaxis[" "]
        direction LR
        zlabel["z-axis split<br/>one database per region"]
        africa[("ArticleDatabase<br/>Africa")]
        antarctica[("ArticleDatabase<br/>Antarctica")]
        asia[("ArticleDatabase<br/>Asia")]
        europe[("ArticleDatabase<br/>Europe")]
        northAmerica[("ArticleDatabase<br/>North America")]
        oceania[("ArticleDatabase<br/>Oceania")]
        southAmerica[("ArticleDatabase<br/>South America")]
        globalDb[("ArticleDatabase<br/>Global")]
    end

    client --> lb
    lb --> s1 & s2 & s3
    s1 & s2 & s3 --> router
    router --> africa & antarctica & asia & europe & northAmerica & oceania & southAmerica & globalDb

    classDef client fill:#08427b,stroke:#052e56,color:#fff
    classDef container fill:#438dd5,stroke:#2e6295,color:#fff
    classDef note fill:#fff,stroke:#999,stroke-dasharray:3 3,color:#333
    classDef axis fill:none,stroke:none,color:#1168bd,font-weight:bold
    class client client
    class lb,s1,s2,s3,africa,antarctica,asia,europe,northAmerica,oceania,southAmerica,globalDb container
    class router note
    class xlabel,zlabel axis
    style xaxis fill:none,stroke:#1168bd,stroke-dasharray:6 4,color:#1168bd
    style zaxis fill:none,stroke:#1168bd,stroke-dasharray:6 4,color:#1168bd
```

## C4 container diagram

![Containers](docs/images/c4-level2-containers.png)

## Run

```sh
docker compose up -d --build
```

| URL | What |
|-----|------|
| http://localhost:8081/swagger | API docs (through the load balancer) |
| http://localhost:8080 | C4 diagrams |

## Endpoints

| Method | URL |
|--------|-----|
| GET, POST | `/api/regions/{region}/articles` |
| GET, PUT, DELETE | `/api/regions/{region}/articles/{id}` |

Regions: `africa`, `antarctica`, `asia`, `europe`, `northamerica`, `oceania`, `southamerica`, `global`.

Ready-made requests: [ArticleService.http](src/ArticleService/ArticleService.http). The `X-Instance` response header shows which instance answered.
