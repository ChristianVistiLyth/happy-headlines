workspace "Happy Headlines" "C4 model of the Happy Headlines system." {

    properties {
        // Week 35 is technology-neutral: technologies are added when they are chosen in later weeks
        "structurizr.inspection.model.relationship.technology" "ignore"
        "structurizr.inspection.model.container.technology" "ignore"
        // Documentation and decisions live in the README, not in Structurizr
        "structurizr.inspection.model.softwaresystem.documentation" "ignore"
        "structurizr.inspection.model.softwaresystem.decisions" "ignore"
    }

    model {
        // People (Reader is defined first so autoLayout keeps both people above the system)
        reader = person "Reader" "A person who reads, comments on and subscribes to positive news."
        publisher = person "Publisher" "A journalist who writes and publishes articles."

        // Software system with its containers (Level 2)
        happyHeadlines = softwareSystem "Happy Headlines" "Lets publishers write and publish positive news, and lets readers read, comment on and subscribe to it." {
            // Apps
            webapp = container "Webapp" "The webapp where publishers can write and publish articles." "" "WebBrowser"
            website = container "Website" "Shows the ten most recent articles with one focus article in the very top." "" "WebBrowser"

            // Swimlanes: each lane has its own service(s) and database(s) and shares nothing with the other lanes
            group "Article swimlane" {
                articleLoadBalancer = container "ArticleLoadBalancer" "Spreads requests across the three ArticleService instances (round-robin)." "" "LoadBalancer"
                articleService = container "ArticleService" "Responsible for collecting articles for (sub)systems that request them. Runs as 3 identical instances (x-axis split)." "REST API" "Service" {
                    // Level 3: the components (classes) inside ArticleService
                    articlesController = component "ArticlesController" "REST endpoints: list articles (one page, or everything since a time), get, create, update and delete one." "ASP.NET Core controller"
                    articleQueueSubscriber = component "ArticleQueueSubscriber" "Stores every article published on the ArticleQueue in the database of its region." "BackgroundService, EasyNetQ"
                    articleDatabaseRouter = component "ArticleDatabaseRouter" "The z-axis coordinator: picks the database of the article's region (7 continents + Global)." "C# class"
                    articleDbContext = component "ArticleDbContext" "Reads and writes the Articles table of one region's database." "Entity Framework Core"
                    articleCacheClient = component "ArticleCache" "Cache-first reads of Global articles; removes an article from the cache when it is edited or deleted." "C# class, StackExchange.Redis"
                    cacheMetrics = component "CacheMetrics" "Counts cache hits, misses and read times for the Grafana dashboard." "OpenTelemetry metrics (shared Monitoring library)"
                }
                articleDatabase = container "ArticleDatabase" "Stores all articles that are published on the website. Split into one database per continent + one global (z-axis split)." "" "Database"
                articleCache = container "ArticleCache" "Holds the Global articles of the last 14 days, so most reads never reach the database." "Redis" "Cache"
                articleCacheFiller = container "ArticleCacheFiller" "Offline process: every 5 minutes it loads the Global articles of the last 14 days into ArticleCache." "Worker" "Service"
            }
            group "Comment swimlane" {
                commentService = container "CommentService" "Responsible for handling comments on articles. Rejects new comments while ProfanityService is unavailable." "REST API" "Service"
                commentDatabase = container "CommentDatabase" "Stores all comments that are posted on articles." "" "Database"
                commentCache = container "CommentCache" "Holds the comments of the 30 most recently read articles (least recently used is removed)." "Redis" "Cache"
            }
            group "Draft swimlane" {
                draftService = container "DraftService" "Responsible for saving drafts of articles." "REST API" "Service"
                draftDatabase = container "DraftDatabase" "Stores all drafts of articles." "" "Database"
            }
            group "Profanity swimlane" {
                profanityService = container "ProfanityService" "Responsible for filtering out profanity in articles and comments." "REST API" "Service"
                profanityDatabase = container "ProfanityDatabase" "Stores all profanity words." "" "Database"
            }

            group "Subscriber swimlane" {
                subscriberService = container "SubscriberService" "Responsible for handling subscribers. Hidden behind a release toggle: switched on and off without redeploying." "REST API" "Service"
                subscriberDatabase = container "SubscriberDatabase" "Stores all subscribers." "" "Database"
                subscriberQueue = container "SubscriberQueue" "Queue where subscribers are put in when they subscribe." "" "Queue"
            }
            group "Newsletter swimlane" {
                newsletterService = container "NewsletterService" "Responsible for sending out newsletters to subscribers." "REST API" "Service"
            }

            // Services
            publisherService = container "PublisherService" "Responsible for handling the publishing of articles." "REST API" "Service"

            // Queues
            articleQueue = container "ArticleQueue" "Queue where articles are put in when they are published." "" "Queue"
        }

        // External software systems
        emailSystem = softwareSystem "Email System" "Delivers emails to readers." "External"

        // Level 1: System context relationships
        // (defined first, so Structurizr doesn't invent extra arrows from the container relationships below)
        publisher -> happyHeadlines "Writes, saves drafts of and publishes articles using"
        reader -> happyHeadlines "Reads and comments on articles, and subscribes to the newsletter using"
        happyHeadlines -> emailSystem "Sends newsletters using"
        emailSystem -> reader "Delivers newsletters to"

        // Level 2: Drafting & publishing
        publisher -> webapp "Writes and publishes articles using"
        webapp -> draftService "Saving a draft"
        webapp -> draftService "Fetching drafts"
        draftService -> draftDatabase "Storing and fetching drafts"
        webapp -> publisherService "Publishing an article"
        publisherService -> profanityService "Filtering out profanity"
        publisherService -> articleQueue "Puts the published article into"
        profanityService -> profanityDatabase "Fetching profanity words"
        articleService -> articleQueue "Subscribes to the latest articles in order to persist them"
        articleLoadBalancer -> articleService "Forwards each request to one of the three instances"
        articleService -> articleDatabase "Fetching and storing articles in the database of the article's region"

        // Level 3: inside ArticleService
        articleLoadBalancer -> articlesController "Forwards each request to one of the three instances"
        articlesController -> articleCacheClient "Reading a Global article first from, removing it when edited or deleted"
        articlesController -> cacheMetrics "Recording each hit or miss"
        articlesController -> articleDatabaseRouter "Asking for the database of the region in the URL"
        articleQueueSubscriber -> articleQueue "Subscribes to published articles"
        articleQueueSubscriber -> articleDatabaseRouter "Asking for the database of the article's region"
        articleDatabaseRouter -> articleDbContext "Opens one for the chosen region"
        articleDbContext -> articleDatabase "Reads and writes articles"
        articleCacheClient -> articleCache "Reads and removes cached articles"
        articleService -> articleCache "Reading a Global article first from, removing it when edited or deleted"
        articleCacheFiller -> articleDatabase "Fetching the Global articles of the last 14 days"
        articleCacheFiller -> articleCache "Filling with the fetched articles"

        // Level 2: Reading & commenting
        reader -> website "Reads and comments on articles, and subscribes to the newsletter using"
        website -> articleLoadBalancer "Fetching the latest articles"
        website -> commentService "Posting a comment"
        website -> commentService "Requesting comments"
        commentService -> profanityService "Filtering out profanity (circuit breaker)"
        commentService -> commentDatabase "Storing a comment"
        commentService -> commentDatabase "Fetching comments"
        commentService -> commentCache "Reading comments first from, filling it on a miss"

        // Level 2: Subscribing & newsletter
        website -> subscriberService "Subscribe to newsletters"
        website -> subscriberService "Unsubscribe from newsletters"
        subscriberService -> subscriberDatabase "Storing a subscriber"
        subscriberService -> subscriberDatabase "Removing a subscriber"
        subscriberService -> subscriberQueue "When a subscriber subscribes, the subscriber will be put into this queue"
        newsletterService -> subscriberQueue "Send a welcome mail to new subscribers"
        newsletterService -> articleQueue "Subscribes to receive the latest news first for immediate newsletter"
        newsletterService -> articleLoadBalancer "Request articles for daily newsletter"
        newsletterService -> subscriberService "Sends request to get subscribers (circuit breaker)"
        newsletterService -> emailSystem "Sends newsletters using"
    }

    views {
        properties {
            // Hide the timestamp under each diagram
            "structurizr.metadata" "false"
        }

        systemContext happyHeadlines "SystemContext" "Level 1: Who uses Happy Headlines." {
            include *
            autoLayout tb
        }

        component articleService "ArticleServiceComponents" "Level 3: The components inside ArticleService." {
            include *
            autoLayout tb
        }

        // Manual layout: box positions are stored in workspace.json
        container happyHeadlines "Containers" "Level 2: All containers in Happy Headlines." {
            include *
        }

        // Standard C4 colours
        styles {
            element "Element" {
                color #ffffff
            }
            element "Person" {
                background #08427b
                shape person
            }
            element "Software System" {
                background #1168bd
            }
            element "Container" {
                background #438dd5
            }
            element "External" {
                background #999999
            }
            element "WebBrowser" {
                shape WebBrowser
            }
            element "Service" {
                shape Hexagon
            }
            element "Database" {
                shape Cylinder
            }
            element "Queue" {
                shape Pipe
            }
            element "Cache" {
                shape Cylinder
            }
            element "Component" {
                background #85bbf0
                color #000000
            }
        }
    }

    configuration {
        scope softwaresystem
    }
}
