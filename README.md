# Happy Headlines

Semester project: a distributed news system that spreads positive news, built one week at a time.

Each `week-XX` folder is a complete, standalone snapshot of the system as it was handed in that week.
The current week is the last row; earlier weeks are in `Old weeks/`.

How changes are made (issue → branch → pull request): [COLLABORATION.md](COLLABORATION.md).

| Week | Topic | Folder |
|------|-------|--------|
| 35 | C4 diagrams: context and container level | [week-35](Old%20weeks/week-35) |
| 36 | ArticleService: 3 instances behind a load balancer, one database per continent | [week-36](Old%20weeks/week-36) |
| 37 | CommentService and ProfanityService in separate swimlanes, with a circuit breaker | [week-37](Old%20weeks/week-37) |
| 38 | DraftService, plus central logging and tracing for all services (OpenTelemetry, Grafana) | [week-38](Old%20weeks/week-38) |
| 39 | Publishing through a queue (RabbitMQ), with traces that stay whole across services | [week-39](Old%20weeks/week-39) |
| 40 | ArticleCache and CommentCache (Redis), and a cache hit-ratio dashboard (Grafana, Prometheus) | [week-40](Old%20weeks/week-40) |
| 43 | CI/CD: GitHub Actions builds every service and delivers versioned images to ghcr.io | [week-43](Old%20weeks/week-43) |
| 44 | SubscriberService behind a release toggle, with fault isolation from NewsletterService | [week-44](Old%20weeks/week-44) |
| 45 | Sustainability: three Green Architecture Framework tactics, measured before and after | [week-45](week-45) |
