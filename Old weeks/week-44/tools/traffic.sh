#!/bin/sh
# Demo traffic for the cache dashboard: reads articles and comments the way readers would.
# Most reads go to a few popular items (cache hits), some go to rarely read ones (cache misses).
#   start: docker compose --profile demo up -d traffic
#   stop:  docker compose stop traffic

ARTICLES=http://article-load-balancer/api/regions/global/articles
COMMENTS=http://comment-service:8080/api/articles

# 85 % of article reads: the two recent Global articles, which the ArticleCacheFiller put in the cache (hits).
# 15 %: an article that is not in the cache, e.g. an old one (misses).
article() {
  if [ $((RANDOM % 100)) -lt 85 ]; then
    echo "00000008-0000-0000-0000-00000000000$((RANDOM % 2 + 1))"
  else
    cat /proc/sys/kernel/random/uuid
  fi
}

# Comments of 40 articles: the 10 popular ones get 80 % of the reads, the 30 others the rest.
# The CommentCache has room for 30, so LRU keeps the popular ones and throws out rarely read ones.
comment_article() {
  if [ $((RANDOM % 10)) -lt 8 ]; then n=$((RANDOM % 10)); else n=$((10 + RANDOM % 30)); fi
  printf "00000000-0000-0000-0000-%012d" "$n"
}

echo "Sending traffic - stop with: docker compose stop traffic"
while true; do
  curl -s -o /dev/null "$ARTICLES/$(article)"
  curl -s -o /dev/null "$COMMENTS/$(comment_article)/comments"
  sleep 0.2
done
