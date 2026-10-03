workspace "Happy Headlines" "C4 model of the Happy Headlines system." {

    model {
        // People
        publisher = person "Publisher" "A journalist who writes and publishes articles."
        reader = person "Reader" "A person who reads, comments on and subscribes to positive news."

        // Software system
        happyHeadlines = softwareSystem "Happy Headlines" "Lets publishers write and publish positive news, and lets readers read, comment on and subscribe to it."

        // Level 1: System context relationships
        publisher -> happyHeadlines "Writes, saves drafts of and publishes articles using"
        reader -> happyHeadlines "Reads and comments on articles, and subscribes to the newsletter using"
        happyHeadlines -> reader "Sends daily newsletter to"
    }

    views {
        systemContext happyHeadlines "SystemContext" "Level 1: Who uses Happy Headlines." {
            include *
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
        }
    }
}
