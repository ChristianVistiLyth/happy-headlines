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
                articleService = container "ArticleService" "Responsible for collecting articles for (sub)systems that request them. Runs as 3 identical instances (x-axis split)." "REST API" "Service"
                articleDatabase = container "ArticleDatabase" "Stores all articles that are published on the website. Split into one database per continent + one global (z-axis split)." "" "Database"
            }
            group "Comment swimlane" {
                commentService = container "CommentService" "Responsible for handling comments on articles. Rejects new comments while ProfanityService is unavailable." "REST API" "Service"
                commentDatabase = container "CommentDatabase" "Stores all comments that are posted on articles." "" "Database"
            }
            group "Profanity swimlane" {
                profanityService = container "ProfanityService" "Responsible for filtering out profanity in articles and comments." "REST API" "Service"
                profanityDatabase = container "ProfanityDatabase" "Stores all profanity words." "" "Database"
            }

            // Services
            draftService = container "DraftService" "Responsible for saving drafts of articles." "REST API" "Service"
            publisherService = container "PublisherService" "Responsible for handling the publishing of articles." "REST API" "Service"
            subscriberService = container "SubscriberService" "Responsible for handling newsletter subscriptions." "REST API" "Service"
            newsletterService = container "NewsletterService" "Responsible for sending out newsletters to subscribers." "REST API" "Service"

            // Databases
            draftDatabase = container "DraftDatabase" "Stores all drafts of articles." "" "Database"
            subscriberDatabase = container "SubscriberDatabase" "Stores all newsletter subscribers." "" "Database"

            // Queues
            articleQueue = container "ArticleQueue" "Queue where articles are put in when they are published." "" "Queue"
            subscriberQueue = container "SubscriberQueue" "Queue where new subscribers are put in when they subscribe." "" "Queue"
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

        // Level 2: Reading & commenting
        reader -> website "Reads and comments on articles, and subscribes to the newsletter using"
        website -> articleLoadBalancer "Fetching the latest articles"
        website -> commentService "Posting a comment"
        website -> commentService "Requesting comments"
        commentService -> profanityService "Filtering out profanity (circuit breaker)"
        commentService -> commentDatabase "Storing a comment"
        commentService -> commentDatabase "Fetching comments"

        // Level 2: Subscribing & newsletter
        website -> subscriberService "Subscribing to the newsletter"
        subscriberService -> subscriberDatabase "Storing subscribers"
        subscriberService -> subscriberQueue "Puts new subscribers into"
        newsletterService -> subscriberQueue "Subscribes to new subscribers"
        newsletterService -> articleQueue "Subscribes to receive the latest news first for immediate newsletter"
        newsletterService -> articleLoadBalancer "Request articles for daily newsletter"
        newsletterService -> subscriberService "Fetching active subscribers"
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
        }
    }

    configuration {
        scope softwaresystem
    }
}
