# Azure VM deployment

The frontend stays on Firebase Hosting. One Ubuntu VM runs Caddy, the .NET API and MySQL through Docker Compose. Caddy exposes HTTP/HTTPS; API port 8080 and MySQL port 3306 are not published on the host. MySQL data, bearer-token protection keys and TLS state use named volumes on the VM's persistent managed OS disk.

This is a single-server deployment for the academic project. It does not create cloud resources automatically. Availability of a size, regional capacity, credits and the displayed price depend on the Azure subscription.

## 1. Create Azure resources in the portal

1. Select the intended Azure subscription. Create resource group `rg-vigia-dev` in the chosen region.
2. Create virtual network `vnet-vigia`, address space `10.20.0.0/16`, and subnet `snet-app`, `10.20.1.0/24`, in that group and region.
3. Create network security group `nsg-vigia`. Add inbound TCP rules for SSH 22 from your current public IP `/32`, and HTTP 80 / HTTPS 443 from Internet. Keep the default inbound deny for other traffic. Do not open 3306 or 8080.
4. Create a Standard static IPv4 public IP, `pip-vigia`. Assign an available DNS name label. Record the actual full DNS name displayed by Azure; use it as `API_DOMAIN` later. A custom domain pointing to this IP also works.
5. Create VM `vm-vigia-api` using Ubuntu Server 24.04 LTS, x64 Gen2, SSH public-key authentication, user `azureuser`, and the resources above. Keep the image's supported security defaults. Choose a size with about 2 vCPU and 4 GiB RAM for the initial API/MySQL workload, subject to availability and price. Do not select a Spot instance for a scheduled academic demonstration.
6. Choose a persistent managed OS disk, for example a 64 GiB Standard SSD. Do not store the database on a temporary disk. This setup does not require a load balancer, Bastion, Kubernetes or a separate managed MySQL service.
7. Review the VM, disk and public-IP charges before selecting Create. Download and protect the private SSH key if Azure generated it. Never commit it.

Resource names and address ranges above are project choices. Review the actual price and region rather than treating the sample configuration as a free offer. VM deletion can also delete its OS disk; named volumes do not protect against deletion of the underlying disk.

Official references: [Linux VM portal quickstart](https://learn.microsoft.com/en-us/azure/virtual-machines/linux/quick-create-portal), [network security groups](https://learn.microsoft.com/en-us/azure/virtual-network/network-security-groups-overview), [public IP DNS labels](https://learn.microsoft.com/en-us/azure/virtual-network/ip-services/public-ip-addresses), [VM billing states](https://learn.microsoft.com/en-us/azure/virtual-machines/states-billing).

## 2. Connect and install Docker

Use the SSH command shown by Azure under VM > Connect, selecting the downloaded key and actual public IP. Example from PowerShell:

```powershell
ssh -i 'C:\path\vm-vigia-api_key.pem' azureuser@PUBLIC_IP
```

Install Docker Engine and the Compose plugin from the [official Ubuntu instructions](https://docs.docker.com/engine/install/ubuntu/). Check `sudo docker version` and `sudo docker compose version` before continuing.

## 3. Transfer the committed backend

From the local backend repository, after the Azure feature has been committed:

```powershell
New-Item -ItemType Directory -Force .local | Out-Null
git archive --format=tar.gz --output=.local/vigia-source.tar.gz HEAD
scp -i 'C:\path\vm-vigia-api_key.pem' .local/vigia-source.tar.gz azureuser@PUBLIC_IP:/home/azureuser/
```

The archive uses committed source, excluding uncommitted private settings and ignored local files. On the VM:

```bash
mkdir -p /home/azureuser/vigia
tar -xzf /home/azureuser/vigia-source.tar.gz -C /home/azureuser/vigia
cd /home/azureuser/vigia/deploy/azure
umask 077
cp .env.example .env
chmod 600 .env
nano .env
```

Set `API_DOMAIN` to the real public DNS name without `https://` or a path. Set an operational email for certificate registration. Generate three different passwords, for example by running `openssl rand -hex 24` three times, for the MySQL application user, MySQL root and the initial administrator. Paste them only into the protected `.env`; keep their secure copy. Hex passwords avoid connection-string and dotenv separator issues. Keep this file outside Git.

Do not reuse the local demonstration credentials. `OPENAPI_ENABLED=true` keeps Scalar available for the academic requirement; change it to false if API documentation should be disabled later.

## 4. Deploy in a controlled sequence

Run from `deploy/azure`:

```bash
sudo bash deploy.sh
```

The script validates configuration, builds the API, waits for MySQL, applies migrations in a one-off process, provisions the initial administrator, then starts the API and Caddy. It stops on a failing command. Normal Production startup does not apply migrations or seed demo data.

Migration startup checks for this migration chain before changing an existing nonempty schema. It rejects databases created with EnsureCreated or partially initialized outside the chain. Use a fresh database or a separately reviewed transition, rather than bypassing that check.

Administrator provisioning creates an ADMIN account only if no administrator exists. Re-running it neither resets an existing password nor promotes an existing normal account. The bootstrap password is passed only to the one-off provisioning container, not to the long-running API.

Caddy requests and renews a public TLS certificate once DNS points to this VM and the required ports are reachable. See [automatic HTTPS requirements](https://caddyserver.com/docs/automatic-https). The API trusts forwarded headers only from the configured proxy address; keep `ReverseProxy__KnownProxy` and the Caddy IP aligned if changing the Docker subnet.

## 5. Verify before connecting Firebase

```bash
sudo docker compose ps
sudo docker compose logs --tail=100 api caddy
curl --fail https://YOUR_DNS_NAME/health
```

`/health` checks database connectivity, not schema completeness. Verify the migration step and an authenticated query as well. Scalar is at `https://YOUR_DNS_NAME/scalar/v1`; the API base is `https://YOUR_DNS_NAME/api/v1`. Sign in with the administrator configured in `.env`. It can use all three frontend perspectives of that same account.

Fresh production deployment has no sample projects, orders or stock. Decide which reviewed business data to load before the academic demonstration. Do not import the old EnsureCreated schema or local demo users blindly. Importing selected business data and its relationships is a separate step from applying the schema.

For the future frontend release, configure:

```dotenv
VITE_LEARNING_PLATFORM_API_URL=https://YOUR_DNS_NAME/api/v1
VITE_USE_IN_MEMORY_API=false
```

The existing Firebase origin is allowed by API CORS. Add a changed hosting origin explicitly if needed. Build and deploy the frontend only after the HTTPS API and authenticated requests pass validation. This Azure feature does not publish a new frontend release.

## Persistence and maintenance

- Named volumes retain MySQL data, certificate state and Data Protection keys when containers are recreated. Do not run `docker compose down --volumes` on a database you want to keep.
- Back up MySQL and protect the key ring before replacing the VM or its disk. A single VM is not an off-VM backup.
- MySQL initialization environment variables create users and passwords on an empty data volume; editing `.env` does not rotate credentials in an existing database.
- For an update, back up first, transfer the reviewed committed version and run the controlled deployment sequence. Future incompatible schema changes require their own rollout plan.
- To release compute resources, stop/deallocate the VM through Azure. Persistent disks and public IPs can still incur charges. Keep the VM available during the scheduled evaluation.

Local `scripts/start-local.ps1` remains a separate development helper. It creates a dedicated local demo database and accounts; it is not the Azure deployment script and is not executed by Rider's normal Run button.

## Validation before provisioning

The API was published and exercised natively in Production against a new isolated MySQL database. `scripts/verify-production.ps1` checks separate migrations, administrator provisioning and repeat safety, authentication, absence of demo records, opt-in Scalar, Firebase CORS and token validity after restarting with the same key ring. It requires PowerShell 7 and local MySQL; it verifies the isolated connection-string database before running commands and retains its new database for inspection.

Compose configuration and deployment-script syntax were validated. Docker Desktop's Linux engine was not running on the development machine, so the image and Compose services have not yet been executed there. Build and run them on the VM and verify TLS, volumes and authentication before declaring the Azure deployment complete.
