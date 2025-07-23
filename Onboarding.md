# Emergency Management BC - ESS Modernization Developer Environment Setup

[![License](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](LICENSE)

## Quick Start

This guide will help you set up a local development environment.

### Prerequisites

- Windows Terminal (recommended)
- .NET SDK
- Node.js version 18.x
- npm
- Git
- Docker or Podman (optional - for Redis)

### Initial Setup

1. **Clone the Repository**
```bash
git clone -c core.symlinks=true https://github.com/<user>/embc-ess-mod.git
```

2. **Configure Development Certificates**
```bash
dotnet dev-certs https --trust
```

3. **Optional: Configure .NET User Secrets**

If you intend to run the APIs locally, you will need to configure user secrets for the service you are working on.

The values for the user secrets for each project can be found in OpenShift by navigating to the appropriate environment and inspecting the secrets for the respective project.

Set up user secrets for each API using the following templates. Configure the secrets file by right-clicking the `.csproj` for the respective project, and select `Manage User Secrets`, or run these commands from the respective project directories:

```bash
dotnet user-secrets init
```

For the user secrets file structure, please see the Confluence page for this project or ask a developer on the team.

User secrets are stored in:
- Windows: `%APPDATA%\Microsoft\UserSecrets\<user_secrets_id>\secrets.json`
- macOS/Linux: `~/.microsoft/usersecrets/<user_secrets_id>/secrets.json`

The `<user_secrets_id>` can be found in the project file (.csproj) under the `UserSecretsId` property.

### Optional: Redis Cluster Setup

If your development requires a Redis cluster, follow these steps:

1. Create a `redis` directory and add node configurations:

`redis-0.conf` (port 6380):
```
port 6380
appendonly no
cluster-enabled yes
cluster-require-full-coverage no
cluster-node-timeout 15000
cluster-config-file /data/nodes.conf
cluster-migration-barrier 1
masteruser default
protected-mode no
save 900 1
save 300 10
save 60 10000
```

Create `redis-1.conf` and `redis-2.conf` with the same content, updating ports to 6381 and 6382 respectively.

2. Start Redis containers:
```bash
# Pull Redis image
podman pull redis:6

# Start nodes (using host network for cluster communication)
podman run -d --rm --name redis-0 -v .\redis\:/conf --net host -p 6380:6380 redis redis-server /conf/redis-0.conf
podman run -d --rm --name redis-1 -v .\redis\:/conf --net host -p 6381:6381 redis redis-server /conf/redis-1.conf
podman run -d --rm --name redis-2 -v .\redis\:/conf --net host -p 6382:6382 redis redis-server /conf/redis-2.conf

# Create cluster
podman exec -it redis-0 redis-cli --cluster create --cluster-replicas 0 localhost:6380 localhost:6381 localhost:6382
```

### Running the Application

#### Option 1: Using Windows Terminal (Windows only)
Use the provided Windows Terminal shortcuts in the root folder:
- `wt-registrants.ps1` - Opens registrants portal
- `wt-responders.ps1` - Opens responders portal

#### Option 2: Manual Setup

There are two ways to run the portals:

**A. Running with Remote APIs (Recommended for most development)**

You can run either portal using the SUPPORT or PROJECT configurations, which connect to remote APIs:

Responders Portal:
```bash
cd embc-ess-mod/responders/src/UI/embc-responder

npm install

npm run start-sup    # Run with SUPPORT configuration
# OR
npm run start-prj    # Run with PROJECT configuration
```

Registrants Portal:
```bash
cd embc-ess-mod/registrants/src/UI/embc-registrant

npm install

npm run start-sup    # Run with SUPPORT configuration
# OR
npm run start-prj    # Run with PROJECT configuration
```

**B. Running with Local APIs**

If you need to develop against local APIs, you'll need to run multiple components in separate terminal windows:

1. ESS Backend (Required for local API development):
```bash
cd embc-ess-mod/ess/src/API/EMBC.ESS.Host
dotnet watch
```

2. Portal-specific API:

For Responders:
```bash
cd embc-ess-mod/responders/src/API/EMBC.Responders.API
dotnet watch
```

For Registrants:
```bash
cd embc-ess-mod/registrants/src/API/EMBC.Registrants.API
dotnet watch
```

3. Portal UI with local configuration:

For Responders:
```bash
cd embc-ess-mod/responders/src/UI/embc-responder

npm install

npm run start    # Runs with local API configuration
```

For Registrants:
```bash
cd embc-ess-mod/registrants/src/UI/embc-registrant

npm install

npm run start    # Runs with local API configuration
```

### Troubleshooting JWT/OIDC Authentication Errors

If you encounter errors like:

```
HTTP POST /EMBC.Utilities.Messaging.Grpc.Dispatcher/Dispatch responded 401
error authenticating token
Microsoft.IdentityModel.Tokens.SecurityTokenInvalidIssuerException: IDX10204: Unable to validate issuer. validationParameters.ValidIssuer is null or whitespace AND validationParameters.ValidIssuers is null or empty.
```

or

```
Microsoft.IdentityModel.Tokens.SecurityTokenSignatureKeyNotFoundException: IDX10500: Signature validation failed. No security keys were provided to validate the signature.
```

**Solution:**

- The application must be able to bind these values to the JwtBearer authentication options. If these are missing or not set, authentication will fail with the above errors.

- In code, you may see this line:
  ```csharp
  opts.MetadataAddress = "https://<Introspection-authority-OIDC>/.well-known/openid-configuration";
  ```


## Project Structure

```
├── ess/           # Shared ESS backend service
├── suppliers/     # Suppliers portal & API
├── registrants/   # Registrants portal & API
├── responders/    # Responders portal & API
├── oauth-server/  # OAuth/OIDC service
└── shared/        # Shared libraries
```

## Contributing

This project uses symlinks to share code libraries. For an existing repository, configure symlinks:

```bash
git config core.symlinks true
git reset --hard
```

### Code Quality Requirements

**C# Services:**
```xml
<TreatWarningsAsErrors>true</TreatWarningsAsErrors>
<AnalysisMode>Default</AnalysisMode>
<EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
<Nullable>enable</Nullable>
```

**Angular Applications:**
Run `npm run lint -- --fix` before committing changes.

## Support

For bugs/issues/feature requests, email: essmodernization@gov.bc.ca

## License

[Apache License 2.0](LICENSE)

---

[Full Contributing Guidelines](./CONTRIBUTING.md) | [Code of Conduct](./CODE_OF_CONDUCT.md)