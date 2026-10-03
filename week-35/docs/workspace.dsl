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
        // People
        publisher = person "Publisher" "A journalist who writes and publishes articles."
        reader = person "Reader" "A person who reads, comments on and subscribes to positive news."

        // Software system with its containers (Level 2)
        happyHeadlines = softwareSystem "Happy Headlines" "Lets publishers write and publish positive news, and lets readers read, comment on and subscribe to it." {
            // Apps
            webapp = container "Webapp" "The webapp where publishers can write and publish articles." "" "WebBrowser"
            website = container "Website" "Shows the ten most recent articles with one focus article in the very top." "" "WebBrowser"

            // Services
            draftService = container "DraftService" "Responsible for saving drafts of articles." "REST API" "Service"
            publisherService = container "PublisherService" "Responsible for handling the publishing of articles." "REST API" "Service"
            profanityService = container "ProfanityService" "Responsible for filtering out profanity in articles and comments." "REST API" "Service"
            articleService = container "ArticleService" "Responsible for collecting articles for (sub)systems that request them." "REST API" "Service"
            commentService = container "CommentService" "Responsible for handling comments on articles." "REST API" "Service"
            subscriberService = container "SubscriberService" "Responsible for handling newsletter subscriptions." "REST API" "Service"
            newsletterService = container "NewsletterService" "Responsible for sending out newsletters to subscribers." "REST API" "Service"

            // Databases
            draftDatabase = container "DraftDatabase" "Stores all drafts of articles." "" "Database"
            profanityDatabase = container "ProfanityDatabase" "Stores all profanity words." "" "Database"
            articleDatabase = container "ArticleDatabase" "Stores all articles that are published on the website." "" "Database"
            commentDatabase = container "CommentDatabase" "Stores all comments that are posted on articles." "" "Database"
            subscriberDatabase = container "SubscriberDatabase" "Stores all newsletter subscribers." "" "Database"

            // Queues
            articleQueue = container "ArticleQueue" "Queue where articles are put in when they are published." "" "Queue"
            subscriberQueue = container "SubscriberQueue" "Queue where new subscribers are put in when they subscribe." "" "Queue"
        }

        // Level 1: System context relationships
        // (defined first, so Structurizr doesn't invent extra arrows from the container relationships below)
        publisher -> happyHeadlines "Writes, saves drafts of and publishes articles using"
        reader -> happyHeadlines "Reads and comments on articles, and subscribes to the newsletter using"
        happyHeadlines -> reader "Sends daily newsletter to"

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
        articleService -> articleDatabase "Fetching and storing articles"

        // Level 2: Reading & commenting
        reader -> website "Reads and comments on articles, and subscribes to the newsletter using"
        website -> articleService "Fetching the latest articles"
        website -> commentService "Posting a comment"
        website -> commentService "Requesting comments"
        commentService -> profanityService "Filtering out profanity"
        commentService -> commentDatabase "Storing a comment"
        commentService -> commentDatabase "Fetching comments"

        // Level 2: Subscribing & newsletter
        website -> subscriberService "Subscribing to the newsletter"
        subscriberService -> subscriberDatabase "Storing subscribers"
        subscriberService -> subscriberQueue "Puts new subscribers into"
        newsletterService -> subscriberQueue "Subscribes to new subscribers"
        newsletterService -> articleQueue "Subscribes to receive the latest news first for immediate newsletter"
        newsletterService -> articleService "Request articles for daily newsletter"
        newsletterService -> subscriberService "Fetching active subscribers"
        newsletterService -> reader "Sends daily newsletter to"
    }

    views {
        systemContext happyHeadlines "SystemContext" "Level 1: Who uses Happy Headlines." {
            include *
            autoLayout lr
        }

        container happyHeadlines "Containers" "Level 2: All containers in Happy Headlines." {
            include *
            autoLayout tb
        }

        container happyHeadlines "DraftingAndPublishing" "Level 2: How a publisher drafts and publishes an article." {
            include publisher webapp draftService draftDatabase publisherService profanityService profanityDatabase articleQueue articleService articleDatabase
            autoLayout lr
        }

        container happyHeadlines "ReadingAndCommenting" "Level 2: How a reader reads articles and posts comments." {
            include reader website articleService articleDatabase commentService commentDatabase profanityService profanityDatabase
            autoLayout lr
        }

        container happyHeadlines "SubscribingAndNewsletter" "Level 2: How a reader subscribes and receives the newsletter." {
            include reader website subscriberService subscriberDatabase subscriberQueue newsletterService articleService articleQueue
            exclude "website -> articleService"
            autoLayout lr
        }

        styles {
            element "Element" {
                color #ffffff
            }
            element "Person" {
                background #9b191f
                shape person
            }
            element "Software System" {
                background #ba1e25
            }
            element "Container" {
                background #d9232b
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
