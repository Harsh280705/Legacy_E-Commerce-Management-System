#!/bin/sh
# $1 = route path; clears stale Mono compile cache, recycles app, probes route
rm -rf /tmp/root-temp-aspnet-*
touch /app/Web.config
sleep 25
curl -sS -m 25 -o /dev/null -w "%{http_code}\n" http://localhost:8080/$1
