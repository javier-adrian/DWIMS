# DWIMS Azure Deployment Guide

## Overview

Deploy DWIMS (ASP.NET Core API + Vite frontend) to Azure using free tier services.

| Component | Service | Free Tier |
|---|---|---|
| API | B2ats v2 Linux VM | 750 hours/month |
| Database | Azure Database for MySQL (Flexible Server, B1MS) | 750 hours, 32 GB storage |
| Document Storage | Azure Blob Storage | 5 GB hot LRS |
| Frontend | Azure Static Web Apps | 100 GB bandwidth, 0.5 GB storage |
| Secrets | Azure Key Vault | 10,000 transactions |
| CI/CD | Azure DevOps | Unlimited repos |

---

## Phase 1: Azure Resources

### 1.1 Create Resource Group

```bash
az group create --name dwims-rg --location eastasia
```

### 1.2 Create Key Vault

```bash
az keyvault create --name dwims-kv --resource-group dwims-rg --location eastasia
```

### 1.3 Create Azure Database for MySQL

```bash
az mysql flexible-server create \
  --name dwims-mysql \
  --resource-group dwims-rg \
  --location eastasia \
  --admin-user dwims_admin \
  --admin-password <GENERATE_SECURE_PASSWORD> \
  --sku-name Standard_B1ms \
  --storage-size 32 \
  --tier Burstable \
  --version 8.0.21
```

Allow Azure services to connect:

```bash
az mysql flexible-server firewall-rule create \
  --name dwims-mysql --resource-group dwims-rg \
  --rule-name AllowAllAzureIPs \
  --start-ip-address 0.0.0.0 --end-ip-address 255.255.255.255
```

Create the DWIMS database:

```bash
az mysql flexible-server db create \
  --name DWIMS \
  --resource-group dwims-rg \
  --server-name dwims-mysql
```

### 1.4 Create Azure Blob Storage

```bash
az storage account create \
  --name <unique-storage-name> \
  --resource-group dwims-rg \
  --location eastasia \
  --sku Standard_LRS \
  --kind StorageV2
```

Create the documents container:

```bash
az storage container create \
  --name dwims-documents \
  --account-name <unique-storage-name>
```

### 1.5 Store Secrets in Key Vault

Generate a new JWT secret (the current one is committed to git and must be rotated), then store all secrets:

```bash
az keyvault secret set --vault-name dwims-kv --name JwtSecret --value "<new-jwt-secret>"
az keyvault secret set --vault-name dwims-kv --name GoogleClientId --value "<client-id>"
az keyvault secret set --vault-name dwims-kv --name GoogleClientSecret --value "<client-secret>"
az keyvault secret set --vault-name dwims-kv --name EmailPassword --value "<gmail-app-password>"
az keyvault secret set --vault-name dwims-kv --name BlobStorageConnectionString --value "<connection-string>"
az keyvault secret set --vault-name dwims-kv --name MySQLConnectionString --value "<connection-string>"
```

### 1.6 Create B2ats v2 Linux VM

```bash
az vm create \
  --name dwims-api \
  --resource-group dwims-rg \
  --image Ubuntu2204 \
  --size Standard_B2ats_v2 \
  --admin-user azureuser \
  --authentication-type ssh \
  --ssh-key-values ~/.ssh/id_rsa.pub
```

Open ports:

```bash
az vm open-port --port 80 --resource-group dwims-rg --name dwims-api
az vm open-port --port 443 --resource-group dwims-rg --name dwims-api
```

### 1.7 Create Static Web App (Vite Frontend)

Create via Azure Portal or CLI, pointing to your frontend repo:

```bash
az staticwebapp create \
  --name dwims-frontend \
  --resource-group dwims-rg \
  --source <frontend-repo-url> \
  --branch main \
  --app-location / \
  --api-location "" \
  --output-location dist \
  --token <devops-token>
```

Build command: `npm run build` (Vite default).

---

## Phase 2: Code Changes

### 2.1 Refactor Database Connection String

**File:** `DWIMS.Data/Context.cs`

Remove the hardcoded connection string and accept it from configuration:

- Remove the `private const string Connection = "..."` line
- Accept the connection string via `IConfiguration` or DbContextOptions
- Disable `EnableSensitiveDataLogging()` in production

### 2.2 Replace MinIO/S3 with Azure Blob Storage

**Files to modify:**

| File | Change |
|---|---|
| `DWIMS.Service/DWIMS.Service.csproj` | Remove `AWSSDK.S3`, add `Azure.Storage.Blobs` |
| `DWIMS.Service/Storage/StorageOptions.cs` | Replace S3 fields (AccessKey, SecretKey, ServiceUrl) with BlobConnectionString and ContainerName |
| `DWIMS.Service/Services/StorageService.cs` | Rewrite to use `BlobContainerClient` instead of `IAmazonS3` |
| `DWIMS.App/Program.cs` | Remove `IAmazonS3` registration, register `BlobServiceClient` instead |

### 2.3 Integrate Azure Key Vault

**Files to modify:**

| File | Change |
|---|---|
| `DWIMS.App/DWIMS.App.csproj` | Add `Azure.Identity` and `Azure.Extensions.AspNetCore.Configuration.Secrets` |
| `DWIMS.App/Program.cs` | Add Key Vault as a configuration source |
| `DWIMS.App/appsettings.json` | Remove all secrets |

**Program.cs addition:**

```csharp
builder.Configuration.AddAzureKeyVault(
    new Uri("https://dwims-kv.vault.azure.net/"),
    new DefaultAzureCredential());
```

### 2.4 Create Production Config

Create `DWIMS.App/appsettings.Production.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning"
    }
  },
  "Jwt": {
    "Issuer": "https://<api-domain>",
    "Audience": "https://<api-domain>"
  },
  "App": {
    "BaseUrl": "https://dwims-frontend.azurestaticapps.net"
  }
}
```

### 2.5 Update CORS and JWT

**File:** `DWIMS.App/Program.cs`

- CORS origin: `http://localhost:5173` → `https://dwims-frontend.azurestaticapps.net`
- JWT Issuer/Audience: `https://localhost` → your API production URL

---

## Phase 3: VM Setup

SSH into the VM (`ssh azureuser@<vm-ip>`) and run:

### 3.1 Install Dependencies

```bash
sudo apt update && sudo apt upgrade -y

# Install .NET 10 runtime
sudo apt install -y dotnet-runtime-10.0

# Install Nginx
sudo apt install -y nginx

# Install Certbot for HTTPS
sudo apt install -y certbot python3-certbot-nginx

# Add swap (2 GB memory safety net)
sudo fallocate -l 2G /swapfile
sudo chmod 600 /swapfile
sudo mkswap /swapfile
sudo swapon /swapfile
echo '/swapfile none swap sw 0 0' | sudo tee -a /etc/fstab
```

### 3.2 Deploy the API

```bash
git clone <api-repo-url> /home/azureuser/dwims
cd /home/azureuser/dwims
dotnet publish DWIMS.App/DWIMS.App.csproj -c Release -o /var/www/dwims
```

### 3.3 Create Systemd Service

```bash
sudo tee /etc/systemd/system/dwims.service << 'EOF'
[Unit]
Description=DWIMS API
After=network.target

[Service]
WorkingDirectory=/var/www/dwims
ExecStart=/usr/bin/dotnet DWIMS.App.dll
Restart=always
RestartSec=10
SyslogIdentifier=dwims
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://localhost:5244

[Install]
WantedBy=multi-user.target
EOF

sudo systemctl enable dwims
sudo systemctl start dwims
```

### 3.4 Configure Nginx Reverse Proxy

```bash
sudo tee /etc/nginx/sites-available/dwims << 'EOF'
server {
    listen 80;
    server_name <api-domain-or-ip>;

    location / {
        proxy_pass http://localhost:5244;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
EOF

sudo ln -s /etc/nginx/sites-available/dwims /etc/nginx/sites-enabled/
sudo rm /etc/nginx/sites-enabled/default
sudo nginx -t && sudo systemctl restart nginx
```

### 3.5 Setup HTTPS

```bash
sudo certbot --nginx -d <api-domain>
```

---

## Phase 4: CI/CD (Azure DevOps)

### 4.1 Backend Pipeline

Create `azure-pipelines.yml` in the API repo root:

```yaml
trigger:
  - main

pool:
  vmImage: 'ubuntu-latest'

steps:
  - task: UseDotNet@2
    inputs:
      version: '10.0.x'

  - script: dotnet publish DWIMS.App/DWIMS.App.csproj -c Release -o ./publish
    displayName: 'Publish API'

  - script: |
      ssh -o StrictHostKeyChecking=no azureuser@<vm-ip> "sudo systemctl stop dwims"
      scp -o StrictHostKeyChecking=no -r ./publish/* azureuser@<vm-ip>:/var/www/dwims/
      ssh -o StrictHostKeyChecking=no azureuser@<vm-ip> "sudo systemctl start dwims"
    displayName: 'Deploy to VM'
```

Setup SSH key as a secret variable in Azure DevOps pipeline settings.

### 4.2 Frontend Pipeline

Automatic via Static Web Apps — push to main and it builds/deploy.

---

## Phase 5: Verification

| Check | Command / Action |
|---|---|
| API responds | `curl http://<vm-ip>/swagger` should return Swagger UI |
| MySQL connected | Check API startup logs for no connection errors |
| Blob Storage works | Upload a test PDF template via API, verify in Azure Portal > Storage > Container |
| Frontend loads | Open Static Web App URL in browser |
| CORS working | Login from frontend to API succeeds |
| Key Vault active | Confirm no secrets in appsettings.json, check startup logs |
| HTTPS valid | Both frontend and API serve over HTTPS with valid certificates |
| Service auto-restarts | `sudo systemctl restart dwims` works, service starts on boot |

---

## Local Frontend Development

### Option 1: Vite Proxy (Recommended)

Run the API locally and proxy requests through Vite. No CORS configuration needed.

In `vite.config.js` or `vite.config.ts`:

```js
export default defineConfig({
  server: {
    proxy: {
      '/api': {
        target: 'http://localhost:5244',
        changeOrigin: true,
      }
    }
  }
})
```

Then in your frontend code, use relative paths:

```js
fetch('/api/submissions')  // proxied to http://localhost:5244/api/submissions
```

Instead of absolute URLs:

```js
fetch('http://localhost:5244/api/submissions')  // avoid this
```

### Option 2: API on Azure

Test the frontend locally against the deployed Azure API using an environment variable.

Create a `.env` file in the frontend repo (do not commit):

```env
VITE_API_URL=https://<your-api-domain>
```

Use it in your code:

```js
const apiUrl = import.meta.env.VITE_API_URL;
fetch(`${apiUrl}/api/submissions`);
```

This requires adding `http://localhost:5173` to the CORS allowed origins on the Azure API:

```csharp
policy.WithOrigins(
    "https://dwims-frontend.azurestaticapps.net",
    "http://localhost:5173"  // local dev
)
```

**Use Option 1** for day-to-day work (simpler, no CORS, no latency). Use Option 2 only when testing against the real Azure backend.

---

## Architecture Summary

```
User Browser
    |
    v
Azure Static Web Apps (Vite frontend)
    |
    v (CORS allowed)
Azure VM (B2ats v2, Ubuntu, Nginx + .NET API)
    |
    +---> Azure Database for MySQL (managed)
    +---> Azure Blob Storage (documents)
    +---> Azure Key Vault (secrets)
    +---> Gmail SMTP (email notifications)
    +---> Google OAuth (authentication)
```

---

## Cost Estimate

All services are within the Azure free tier. As long as usage stays within these limits, cost is $0/month:

- VM: 750 hours (full month, one instance)
- MySQL: 750 hours (full month, one instance)
- Blob Storage: 5 GB
- Static Web Apps: 100 GB bandwidth
- Key Vault: 10,000 transactions

Monitor usage in Azure Portal > Cost Management to stay within free tier limits.
