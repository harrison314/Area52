# Deployment

# Deployment on Ubuntu with MongoDB
Deployment and install on _Ubuntu Server 22.04_ with MongoDb.

Install prerequisites and .NET: 
```
sudo apt-get install -y aspnetcore-runtime-6.0
sudo apt-get install -y unzip
```

Install Apache2:
```
sudo apt install apache2

sudo a2enmod ssl
sudo a2enmod proxy
sudo a2enmod proxy_wstunnel
sudo a2enmod proxy_http
sudo a2enmod proxy_balancer
sudo a2enmod headers
sudo systemctl restart apache2
```

Install MongoDb (see more details <https://www.mongodb.com/docs/manual/tutorial/install-mongodb-on-ubuntu/>):
```
sudo apt-get install gnupg curl
curl -fsSL https://pgp.mongodb.com/server-6.0.asc | sudo gpg -o /usr/share/keyrings/mongodb-server-6.0.gpg --dearmor
echo "deb [ arch=amd64,arm64 signed-by=/usr/share/keyrings/mongodb-server-6.0.gpg ] https://repo.mongodb.org/apt/ubuntu jammy/mongodb-org/6.0 multiverse" | sudo tee /etc/apt/sources.list.d/mongodb-org-6.0.list

sudo apt-get update
sudo apt-get install -y mongodb-org

sudo systemctl daemon-reload
sudo systemctl start mongod
sudo systemctl enable mongod
```

For crate empty database use command `mongosh` and run `use Area52Db`, for exit `quit`.

Install Area52:
```
sudo mkdir -p /opt/Area52/bin
sudo mkdir -p /opt/Area52/log

sudo groupadd --system areauser
sudo adduser --system --ingroup areauser --no-create-home --disabled-login areauser

```

Extract Area52:
```
sudo unzip Area52.zip -d /opt/Area52/bin/

sudo chown -R areauser:areauser /opt/Area52/bin
sudo chown -R areauser:areauser /opt/Area52/log

sudo find /opt/Area52/bin -type f -exec chmod u=r,g=r {} \;
sudo find /opt/Area52/bin -type d -exec chmod u=rwx,g=rx {} \;
sudo chmod u=rwx,g=rx /opt/Area52/log
```

Consigure `appsetings.json` and configure listening port:
```
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://127.0.0.1:5080"
      }
    }
  }
```

Create systemd unit file `/etc/systemd/system/area52.service`:

```
[Unit]
Description=Area52

[Service]
WorkingDirectory=/opt/Area52/bin/
ExecStart=/usr/bin/dotnet /opt/Area52/bin/Area52.dll
Restart=always

RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=area52

User=areauser
Group=areauser

Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false

[Install]
WantedBy=multi-user.target
```
Enable and start service:
```
sudo systemctl daemon-reload
sudo systemctl enable area52
sudo systemctl start area52
```

!!tu pokracovat od sudo systemctl enable area52

Create apache2 conf file `/etc/apache2/sites-enabled/area52.conf`
```
<VirtualHost *:80>
  ServerName www.area52.local
  ServerAlias *.area52.local
  ErrorLog ${APACHE_LOG_DIR}/area52-error.log
  CustomLog ${APACHE_LOG_DIR}/area52-access.log common
      
  RequestHeader set "X-Forwarded-Proto" expr=%{REQUEST_SCHEME}s

  ProxyPreserveHost     On
  ProxyPass             / http://127.0.0.1:5080/
  ProxyPassReverse      / http://127.0.0.1:5080/

  ProxyRequests       On
  ProxyPreserveHost   On
  ProxyPassMatch      ^/_blazor/(.*) http://localhost:5080/_blazor/$1
  ProxyPass           /_blazor ws://localhost:5080/_blazor
  ProxyPass           / http://localhost:5080/
  ProxyPassReverse    / http://localhost:5080/
</VirtualHost>
```

For test configuration use `apachectl configtest`. And restart Apache `sudo systemctl restart apache2`.


For configuration firewall:

```
sudo apt-get install ufw

sudo ufw allow 22/tcp
sudo ufw allow 80/tcp
sudo ufw allow 443/tcp

sudo ufw enable
```
