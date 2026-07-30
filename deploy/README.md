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

Set real values for `ConnectionStrings__Default` and `SmsGateway__BaseUrl` in
`deploy/systemd/smpp-web.service` (or in `/etc/systemd/system/smpp-web.service`
on the server) before starting it — `appsettings.json` ships with both blank.

## 3. Install the systemd service

```
sudo cp deploy/systemd/smpp-web.service /etc/systemd/system/
sudo systemctl daemon-reload
sudo systemctl enable --now smpp-web
sudo systemctl status smpp-web
```

Kestrel listens on `127.0.0.1:5083` only — not exposed publicly, Apache is the front door.

## 4. Install the Apache vhost

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

## Notes

- `Program.cs` now calls `UseForwardedHeaders` (X-Forwarded-For/Proto) so
  `UseHttpsRedirection`/cookies/auth behave correctly behind Apache's proxy —
  required since Apache terminates TLS and forwards plain HTTP to Kestrel.
- The MySQL DB and any file-upload storage (`uploads/payments/`) are not part
  of the publish output — provision the DB separately and make sure the
  service user (`www-data`) can write to the app's `wwwroot/uploads` path.
