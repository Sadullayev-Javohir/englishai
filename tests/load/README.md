# Assistant load profiles

Use an integration host/test JWT only. These profiles never create production users.

- Repeated/cache: `MODE=repeated VUS=100 ITERATIONS=500 BASE_URL=... TEST_JWT=... SESSION_ID=... k6 run assistant-session-load.js`
- Unique/forced-local: start the integration host with providers disabled, then use `MODE=unique` with the same command.
