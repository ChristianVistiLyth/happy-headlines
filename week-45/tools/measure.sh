#!/bin/sh
# GAF process tactic "Measure and Experiment": the same measurement before and after each tactic.
# Energy can't be measured from inside Docker, so we measure what drives it: MB per image and bytes per request.
#
#   cd week-45
#   docker compose up -d --build
#   sh tools/measure.sh
#
# The first run loads a fixed test set into the eight article databases: 300 articles per region, one every
# 2.4 hours over the last 30 days (about 10 per day), each with ~2 KB of text. Running it again doesn't add more.

ARTICLES=http://localhost:8081/api/regions
REGIONS="Africa Antarctica Asia Europe NorthAmerica Oceania SouthAmerica Global"

# Region name -> its database container (article-db-north-america, ...)
db_of() { echo "article-db-$(echo "$1" | sed 's/\([a-z]\)\([A-Z]\)/\1-\2/g' | tr 'A-Z' 'a-z')"; }

echo "== Test data"
for region in $REGIONS; do
  docker compose exec -T "$(db_of "$region")" psql -q -U postgres -d articles -c "
    INSERT INTO \"Articles\" (\"Id\", \"Title\", \"Content\", \"Author\", \"PublishedAt\", \"Region\")
    SELECT gen_random_uuid(), 'Good news number ' || g, repeat('A positive story about people helping people. ', 45),
           'Measurement', now() - g * interval '144 minutes', '$region'
    FROM generate_series(1, 300) AS g
    WHERE NOT EXISTS (SELECT 1 FROM \"Articles\" WHERE \"Author\" = 'Measurement');" || exit 1
done
echo "300 measurement articles per region (loaded once)"

echo
echo "== Image sizes (tactic: Reduce Bundle Size)"
for image in article-service article-cache-filler comment-service profanity-service draft-service \
             publisher-service newsletter-service subscriber-service; do
  bytes=$(docker image inspect "ghcr.io/christianvistilyth/happy-headlines/$image:latest" --format '{{.Size}}')
  echo "$image $((bytes / 1000000)) MB"
done | awk '{ print; total += $2 } END { print "total", total, "MB" }'

echo
echo "== The article list, one region (tactic: Defer Fetching)"
curl -s -o /dev/null -w "GET Global/articles: %{size_download} bytes\n" "$ARTICLES/Global/articles"

echo
echo "== One daily newsletter = the article list of all 8 regions (tactic: Reduce Network Package Size)"
since=$(date -u -d '24 hours ago' +%Y-%m-%dT%H:%M:%SZ)
total=0
for region in $REGIONS; do
  bytes=$(curl -s -o /dev/null -w '%{size_download}' "$ARTICLES/$region/articles?since=$since")
  total=$((total + bytes))
done
echo "8 x GET {region}/articles?since=<24 hours ago>: $total bytes"
