# Week 37 – CommentService, ProfanityService and swimlanes

- **CommentService** stores comments on articles. Before a comment is saved, its text is sent to ProfanityService.
- **ProfanityService** replaces prohibited words with asterisks and manages the word list.
- **Swimlanes:** Article, Comment and Profanity each have their own service(s) and database(s), and share nothing.
- **Circuit breaker:** if ProfanityService keeps failing, CommentService stops calling it for 15 seconds
  and rejects new comments with `503 Service Unavailable`. Reading comments keeps working.

## Swimlanes and the circuit breaker

```mermaid
flowchart LR
    client["Client<br/>Website"]

    subgraph articleLane["Article swimlane"]
        articleLb["ArticleLoadBalancer"]
        articleService{{"ArticleService<br/>3 instances"}}
        articleDb[("ArticleDatabase<br/>8 regions")]
        articleLb --> articleService --> articleDb
    end

    subgraph commentLane["Comment swimlane"]
        commentService{{"CommentService"}}
        commentDb[("CommentDatabase")]
        breaker["circuit breaker<br/>after repeated failures:<br/>stop calling ProfanityService<br/>for 15 s, reject new<br/>comments with 503"]
        commentService -- "store / read<br/>comments" --> commentDb
        commentService -- "new<br/>comment" --> breaker
    end

    subgraph profanityLane["Profanity swimlane"]
        profanityService{{"ProfanityService"}}
        profanityDb[("ProfanityDatabase")]
        profanityService --> profanityDb
    end

    client -- "read articles" --> articleLb
    client -- "post / read<br/>comments" --> commentService
    breaker -- "filter the text<br/>(the only call<br/>between lanes)" --> profanityService

    classDef client fill:#08427b,stroke:#052e56,color:#fff
    classDef container fill:#438dd5,stroke:#2e6295,color:#fff
    classDef note fill:#fff,stroke:#999,stroke-dasharray:3 3,color:#333
    class client client
    class articleLb,articleService,articleDb,commentService,commentDb,profanityService,profanityDb container
    class breaker note
    style articleLane fill:none,stroke:#1168bd,stroke-dasharray:6 4,color:#1168bd
    style commentLane fill:none,stroke:#1168bd,stroke-dasharray:6 4,color:#1168bd
    style profanityLane fill:none,stroke:#1168bd,stroke-dasharray:6 4,color:#1168bd
```

## C4 container diagram

![Containers](docs/images/c4-level2-containers.png)

## Run

```sh
docker compose up -d --build
```

| URL | What |
|-----|------|
| http://localhost:8081/swagger | ArticleService (through the load balancer) |
| http://localhost:8082/swagger | CommentService |
| http://localhost:8083/swagger | ProfanityService |
| http://localhost:8080 | C4 diagrams |

## Circuit breaker demo

1. `docker compose stop profanity-service`
2. Post a comment a few times: `503`, and after 3 failures the answer is instant – the circuit is open.
   `docker compose logs comment-service` shows `Circuit OPEN`.
3. Reading comments still works.
4. `docker compose start profanity-service`, wait 15 seconds and post again: `201 Created`.
   The log shows `Circuit HALF-OPEN` and `Circuit CLOSED`.

Ready-made requests: [CommentService.http](src/CommentService/CommentService.http), [ProfanityService.http](src/ProfanityService/ProfanityService.http).
