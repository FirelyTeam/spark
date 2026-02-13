# Spark FHIR Server Helm Chart

Helm chart for deploying [Spark FHIR Server](https://github.com/FirelyTeam/spark) on Kubernetes.

For getting started and local development, see the [k8s README](../../README.md).

## Prerequisites

- Kubernetes 1.19+
- Helm 3.2.0+
- PV provisioner support (if persistence is enabled)

## Installation

```bash
helm install spark ./k8s/helm/spark \
  --set mongodb.auth.password=<your-password>
```

### With Ingress

```bash
helm install spark ./k8s/helm/spark \
  --set mongodb.auth.password=<your-password> \
  --set ingress.enabled=true \
  --set ingress.hosts[0].host=fhir.example.com \
  --set spark.endpoint=https://fhir.example.com/fhir
```

### With TLS (cert-manager + Traefik)

```bash
helm install spark ./k8s/helm/spark \
  --namespace spark --create-namespace \
  --set mongodb.auth.password=<your-password> \
  --set ingress.enabled=true \
  --set ingress.hosts[0].host=fhir.example.com \
  --set ingress.tls[0].secretName=spark-tls \
  --set ingress.tls[0].hosts[0]=fhir.example.com \
  --set spark.endpoint=https://fhir.example.com/fhir
```

### With External MongoDB

```bash
helm install spark ./k8s/helm/spark \
  --set mongodb.enabled=false \
  --set externalMongodb.connectionString="mongodb://user:pass@mongodb.example.com:27017/spark?authSource=admin"
```

### Uninstall

```bash
helm uninstall spark
```

## Configuration

| Parameter | Description | Default |
|-----------|-------------|---------|
| `replicaCount` | Number of Spark replicas | `1` |
| `image.repository` | Spark image repository | `sparkfhir/spark` |
| `image.tag` | Spark image tag | `r4-latest` |
| `image.pullPolicy` | Image pull policy | `IfNotPresent` |
| `spark.endpoint` | FHIR endpoint URL | `http://localhost:8080/fhir` |
| `service.type` | Kubernetes service type | `ClusterIP` |
| `service.port` | Service port | `80` |
| `ingress.enabled` | Enable ingress | `false` |
| `ingress.className` | Ingress class name | `traefik` |
| `ingress.annotations` | Ingress annotations (e.g., cert-manager) | `{}` |
| `ingress.hosts[].host` | Ingress hostname | **Required** when enabled |
| `ingress.tls[].secretName` | TLS secret name | Required for HTTPS |
| `ingress.tls[].hosts` | Hosts for TLS certificate | Required for HTTPS |
| `resources.limits.memory` | Memory limit | `1Gi` |
| `resources.limits.cpu` | CPU limit | `1000m` |
| `resources.requests.memory` | Memory request | `256Mi` |
| `resources.requests.cpu` | CPU request | `100m` |
| `mongodb.enabled` | Deploy MongoDB | `true` |
| `mongodb.auth.username` | MongoDB username | `spark` |
| `mongodb.auth.password` | MongoDB password | Required unless `existingSecret` |
| `mongodb.auth.existingSecret` | Use existing secret for credentials | `""` |
| `mongodb.auth.existingSecretUsernameKey` | Key for username in secret | `MONGO_INITDB_ROOT_USERNAME` |
| `mongodb.auth.existingSecretPasswordKey` | Key for password in secret | `MONGO_INITDB_ROOT_PASSWORD` |
| `mongodb.persistence.enabled` | Enable persistence | `true` |
| `mongodb.persistence.size` | PVC size | `5Gi` |
| `mongodb.persistence.storageClass` | Storage class | `""` (default) |
| `externalMongodb.connectionString` | External MongoDB URI | Required if `mongodb.enabled=false` |
| `externalMongodb.existingSecret` | Use existing secret for connection string | `""` |
| `externalMongodb.existingSecretKey` | Key in secret containing connection string | `connectionString` |
| `extraEnvFrom` | Extra env vars from secrets/configmaps | `[]` |
| `serviceAccount.create` | Create service account | `false` |
| `serviceAccount.name` | Service account name | `""` (generated) |

See [values.yaml](values.yaml) for all parameters.

## Extra Environment Variables

Inject additional configuration from secrets or configmaps using `extraEnvFrom`. This is useful for OAuth providers, API keys, or other sensitive settings that override `appsettings.json`.

### Example: GitHub OAuth

```bash
# Create secret with GitHub OAuth credentials
kubectl create secret generic spark-github-oauth -n spark \
  --from-literal=GitHub__ClientId=your-client-id \
  --from-literal=GitHub__ClientSecret=your-client-secret \
  --from-literal=GitHub__AdminUsers__0=admin-user-1 \
  --from-literal=GitHub__AdminUsers__1=admin-user-2

# Deploy with extraEnvFrom
helm upgrade spark ./k8s/helm/spark \
  --set 'extraEnvFrom[0].secretRef.name=spark-github-oauth'
```

For Argo CD, add the parameter:
```bash
--helm-set 'extraEnvFrom[0].secretRef.name=spark-github-oauth'
```

**Note:** Use double underscores (`__`) to represent nested configuration keys. ASP.NET Core automatically maps `GitHub__ClientId` to `GitHub:ClientId`.

## Helm Commands

```bash
# Lint
helm lint ./k8s/helm/spark

# Render templates
helm template spark ./k8s/helm/spark

# Dry run
helm install spark ./k8s/helm/spark --dry-run --debug

# Upgrade
helm upgrade spark ./k8s/helm/spark --set mongodb.auth.password=<password>
```

## Argo CD

Deploy with Argo CD for GitOps-based deployment. **Important:** Never pass passwords directly as Helm parameters - always use Kubernetes secrets.

### Prerequisites

Create secrets before deploying:

```bash
# Create namespace
kubectl create namespace spark

# Generate a secure password
MONGO_PASSWORD=$(openssl rand -base64 24)

# Create MongoDB credentials (used by MongoDB pod)
kubectl create secret generic spark-mongodb-credentials \
  --namespace spark \
  --from-literal=MONGO_INITDB_ROOT_USERNAME=spark \
  --from-literal=MONGO_INITDB_ROOT_PASSWORD="$MONGO_PASSWORD"

# Create connection string secret (used by Spark to connect)
# Note: URL-encode special characters in password
ENCODED_PASSWORD=$(python3 -c "import urllib.parse; print(urllib.parse.quote('$MONGO_PASSWORD', safe=''))")
kubectl create secret generic spark-mongodb-connection \
  --namespace spark \
  --from-literal=connectionString="mongodb://spark:${ENCODED_PASSWORD}@spark-mongodb.spark.svc.cluster.local:27017/spark?authSource=admin"
```

### Optional: GitHub OAuth

```bash
kubectl create secret generic spark-github-oauth -n spark \
  --from-literal=GitHub__ClientId=your-client-id \
  --from-literal=GitHub__ClientSecret=your-client-secret \
  --from-literal=GitHub__AdminUsers__0=admin-user-1 \
  --from-literal=GitHub__AdminUsers__1=admin-user-2
```

### Deploy with Argo CD CLI

```bash
argocd app create spark \
  --repo https://github.com/FirelyTeam/spark.git \
  --revision r4/master \
  --path k8s/helm/spark \
  --dest-server https://kubernetes.default.svc \
  --dest-namespace spark \
  --helm-set 'mongodb.auth.existingSecret=spark-mongodb-credentials' \
  --helm-set 'externalMongodb.existingSecret=spark-mongodb-connection' \
  --helm-set 'extraEnvFrom[0].secretRef.name=spark-github-oauth' \
  --helm-set 'spark.endpoint=https://fhir.example.com/fhir' \
  --sync-policy automated \
  --self-heal
```

### Deploy with Application Manifest

```yaml
apiVersion: argoproj.io/v1alpha1
kind: Application
metadata:
  name: spark
  namespace: argocd
spec:
  project: default
  source:
    repoURL: https://github.com/FirelyTeam/spark.git
    targetRevision: r4/master
    path: k8s/helm/spark
    helm:
      parameters:
        - name: mongodb.auth.existingSecret
          value: spark-mongodb-credentials
        - name: externalMongodb.existingSecret
          value: spark-mongodb-connection
        - name: extraEnvFrom[0].secretRef.name
          value: spark-github-oauth
        - name: spark.endpoint
          value: https://fhir.example.com/fhir
  destination:
    server: https://kubernetes.default.svc
    namespace: spark
  syncPolicy:
    automated:
      prune: true
      selfHeal: true
    syncOptions:
      - CreateNamespace=true
```

### Security Notes

- **Never** pass `mongodb.auth.password` as a Helm parameter in Argo CD - it will be visible in the UI
- Use `mongodb.auth.existingSecret` for MongoDB pod initialization
- Use `externalMongodb.existingSecret` for Spark's connection string
- Use `extraEnvFrom` for additional secrets (OAuth, API keys, etc.)
- Create all secrets before deploying the Argo CD Application
