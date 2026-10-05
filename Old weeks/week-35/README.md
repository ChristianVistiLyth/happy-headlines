# Week 35 – C4 diagrams

The first two C4 levels of Happy Headlines.

## Level 1 – System context

![System context](docs/images/c4-level1-context.png)

## Level 2 – Containers

![Containers](docs/images/c4-level2-containers.png)

## Assumptions

- Newsletters are sent through an external Email System.
- NewsletterService reads new subscribers from the SubscriberQueue.
- No technologies yet – they are chosen in later weeks.

## Run

```sh
docker compose up
```

Open http://localhost:8080
