# Deploying SMPP.Web behind Apache (Linux)

## 1. Build the publish output (done locally, produces `publish/linux-x64`)

```
dotnet publish src/SMPP.Web/SMPP.Web.csproj -c Release -r linux-x64 --self-contained true -o publish/linux-x64
```

## 2. Ship it to the server

```
rsync -avz publish/linux-x64/ user@server:/var/www/smpp-web/
ssh user@server "chmod +x /var/www/smpp-web/SMPP.Web && chown -R www-data:www-data /var/www/smpp-web"
```

Configuration lives in **`src/SMPP.Web/appsettings.Production.json`**, not in environment
variables. `ASPNETCORE_ENVIRONMENT=Production` (set by the systemd unit) makes ASP.NET load
it on top of `appsettings.json`, and `dotnet publish` copies it into the bundle.

It is gitignored, because it holds the live database password and the superadmin seed
password. On a fresh clone, create it from the template:

```
cp src/SMPP.Web/appsettings.Production.example.json src/SMPP.Web/appsettings.Production.json
# then fill in the REPLACE_ME values
```

Fill it in **before** publishing — the file is baked into the bundle, so treat the bundle
and the zip as carrying live credentials.

`ConnectionStrings:Default` must point at **`smpp_bulk_db_new`** — the same database the
legacy Laravel app and the SMPP daemon use. The daemon polls `under_process` there; pointed
anywhere else the app will accept sends that never leave the building.

`appsettings.Development.json` and the `.example.json` template are deleted from the publish
output by a post-publish target, so local credentials never reach the server.

## 3. Schema (handled automatically at startup)

Nothing to run by hand. `Database:AutoMigrate` ships `true`, and on startup the app picks a
path based on what it finds:

- **Shared `smpp_bulk_db_new`** (the daemon's `historys`/`under_process` are present but there
  is no EF migration history) — creates only the tables the app owns (Identity, `Campaigns`,
  `Payments`, `SpamKeywords`, `Transactions`) from a script embedded in `SMPP.Infrastructure`,
  then stamps the baseline migrations as applied. `historys` and `under_process` are never
  touched; the app maps onto them exactly as the daemon defines them.
- **A database EF owns outright** (empty, no legacy tables) — applies the migrations normally.
- **Already current** — does nothing.

Migrations added after the baseline apply normally on both kinds of database.

Do **not** run `dotnet ef database update` against `smpp_bulk_db_new`. The baseline migrations
were written for a database EF created from scratch; replayed there they would rename a new
`Histories` table onto the live `historys`, recreate `under_process`, and add a foreign key that
legacy rows cannot satisfy. That is exactly what the startup path exists to avoid.

The app refuses to start if MySQL has `lower_case_table_names` set to a non-zero value while
legacy `campaigns`/`spam_keywords` tables exist — its `Campaigns`/`SpamKeywords` tables would
silently resolve to the legacy ones. Check with `SELECT @@global.lower_case_table_names`.

Back up before the first start regardless: MySQL does not roll back DDL.

To take the schema into your own hands, set `Database__AutoMigrate=false` and apply
`src/SMPP.Infrastructure/Persistence/Scripts/CreateAppTables.sql` yourself.

## 4. Install the systemd service

```
sudo cp deploy/systemd/smpp-web.service /etc/systemd/system/
sudo systemctl daemon-reload
sudo systemctl enable --now smpp-web
sudo systemctl status smpp-web
```

Kestrel listens on `127.0.0.1:5083` only — not exposed publicly, Apache is the front door.

## 5. Install the Apache vhost

```
sudo a2enmod proxy proxy_http headers rewrite
sudo cp deploy/apache/smpp-web.conf /etc/apache2/sites-available/
# edit ServerName to your real domain first
sudo a2ensite smpp-web
sudo apache2ctl configtest && sudo systemctl reload apache2
```

For HTTPS, either run `sudo certbot --apache -d smpp.example.com` (it will
rewrite the vhost for you — just re-add the `ProxyPass`/`RequestHeader` lines
into the `:443` block it creates), or use the commented-out HTTPS block
already in `deploy/apache/smpp-web.conf`.

## 6. Publish the REST API and its Swagger page

Everything the portal does is also a REST endpoint under `/api/v1`, documented by a Swagger
page the same host serves. Once the vhost above is up, they are already online:

- **Swagger UI** — `https://your-domain/swagger`
- **OpenAPI document** — `https://your-domain/swagger/v1/swagger.json`

Two settings in `appsettings.Production.json` matter before you publish:

```jsonc
"Jwt": {
  // Required. The app refuses to start without it - a missing or short key would mean
  // tokens anyone can forge. Generate with: openssl rand -base64 48
  "Key": "..."
},
"Api": {
  // Becomes the server in the OpenAPI document, so "Try it out" targets the public host
  // rather than the 127.0.0.1:5083 Kestrel sees behind Apache.
  "PublicBaseUrl": "https://your-domain",
  "ContactEmail": "support@your-domain",
  "EnableSwagger": true,
  // Empty means any browser origin may call the API. Safe, because the API authenticates
  // with bearer tokens rather than cookies. List origins explicitly to lock it down.
  "AllowedCorsOrigins": []
}
```

Rotating `Jwt:Key` invalidates every token already issued — that is how you revoke them all.
Set `Api:EnableSwagger` to `false` to keep the API but take the documentation page offline.

How a client authenticates:

```bash
# A person, with portal credentials
curl -X POST https://your-domain/api/v1/auth/login \
  -H 'Content-Type: application/json' \
  -d '{"identifier":"someaccount","password":"..."}'

# A server-to-server integration, with the account's API credentials
# (Superadmin issues them: POST /api/v1/accounts/{id}/api-credentials)
curl -X POST https://your-domain/api/v1/auth/token \
  -H 'Content-Type: application/json' \
  -d '{"tokenId":"...","secretKey":"..."}'

# then, on every other call
curl https://your-domain/api/v1/messages/send-policy -H "Authorization: Bearer $TOKEN"
```

Sending answers `202` with a `batchId` once the batch is accepted and charged; delivery is the
daemon's job, so per-recipient outcomes are polled from
`GET /api/v1/history?campaignBatchId={batchId}`.

The pre-existing `POST /api/send-message-api` and `GET|POST /api/getStatus` are unchanged —
same routes, same headers, same response bodies — so integrations already built against them,
and the daemon's delivery callback, keep working.

## Notes

- `Program.cs` now calls `UseForwardedHeaders` (X-Forwarded-For/Proto) so
  `UseHttpsRedirection`/cookies/auth behave correctly behind Apache's proxy —
  required since Apache terminates TLS and forwards plain HTTP to Kestrel.
- The MySQL DB and any file-upload storage (`uploads/payments/`) are not part
  of the publish output — provision the DB separately and make sure the
  service user (`www-data`) can write to the app's `wwwroot/uploads` path.
- The app shares `smpp_bulk_db_new` with the legacy Laravel app. It owns its own
  tables outright, and reads/writes `historys` and `under_process` alongside the
  daemon. It does not read the legacy `users`, `campaigns`, or `spam_keywords`
  tables — accounts live in `AspNetUsers` and are separate from legacy logins.
- Because accounts are separate, `historys` rows written before the cutover carry
  `creater_id` values from the legacy `users` table, which do not correspond to
  `AspNetUsers.Id`. Pre-cutover history will therefore be attributed to the wrong
  account (or to none) in the History page. Decide up front whether to map the old
  ids across, or to show only rows created after the cutover.
