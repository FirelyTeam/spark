# Running Spark FHIR Server on Kubernetes

Deploy Spark FHIR server with MongoDB to Kubernetes using Helm.

## Quick Start

```bash
# Install k3d
# macOS
brew install k3d
# Windows (PowerShell)
winget install k3d
# Linux
curl -s https://raw.githubusercontent.com/k3d-io/k3d/main/install.sh | bash

# Create cluster
k3d cluster create spark-dev

# Install Spark
helm install spark ./k8s/helm/spark \
  --set mongodb.auth.password=devpassword

# Wait for pods to be ready
kubectl wait --for=condition=ready pod -l app.kubernetes.io/name=spark --timeout=120s

# Access Spark
kubectl port-forward svc/spark 8080:80
curl http://localhost:8080/fhir/metadata
```

## Local Development

### Run full stack in k3d

```bash
# Check status
kubectl get pods

# View Spark logs
kubectl logs -f deployment/spark

# View MongoDB logs
kubectl logs -f statefulset/spark-mongodb

# Shell into Spark pod
kubectl exec -it deployment/spark -- /bin/sh

# Shell into MongoDB
kubectl exec -it spark-mongodb-0 -- mongosh

# Uninstall
helm uninstall spark

# Delete cluster
k3d cluster delete spark-dev
```

### Run MongoDB in k3d, Spark locally

```bash
# Install Spark without the web deployment
helm install spark ./k8s/helm/spark \
  --set mongodb.auth.password=devpassword \
  --set replicaCount=0

# Port forward MongoDB
kubectl port-forward svc/spark-mongodb 27017:27017

# Run Spark locally with hot reload
cd src/Spark.Web
dotnet watch run
```

### Build and test local image

```bash
# Build image
docker build -t localhost:5000/spark:dev -f .docker/linux/Spark.Dockerfile .

# Create cluster with registry
k3d cluster create spark-dev --registry-create spark-registry:5000

# Push to local registry
docker push localhost:5000/spark:dev

# Install with local image
helm install spark ./k8s/helm/spark \
  --set image.repository=localhost:5000/spark \
  --set image.tag=dev \
  --set mongodb.auth.password=devpassword
```

## Production

### Prerequisites

- **Traefik** ingress controller
- **cert-manager** for automatic TLS certificates
- **ClusterIssuer** e.g., `letsencrypt-prod`

### Deploy with TLS

```bash
helm install spark ./k8s/helm/spark \
  --namespace spark --create-namespace \
  -f k8s/helm/spark/values-production.yaml \
  --set mongodb.auth.password=$(openssl rand -base64 32) \
  --set ingress.hosts[0].host=fhir.yourdomain.com \
  --set ingress.tls[0].secretName=spark-tls \
  --set ingress.tls[0].hosts[0]=fhir.yourdomain.com \
  --set spark.endpoint=https://fhir.yourdomain.com/fhir
```

### DNS Setup

Point your domain to the ingress controller's external IP or load balancer:

```bash
# Find the external IP/hostname
kubectl get svc -n <ingress-namespace>  # Look for LoadBalancer type

# Or for hostNetwork ingress, use node IPs
kubectl get nodes -o wide
```

Create DNS records:
- **A Record**: `fhir.yourdomain.com` → `<load-balancer-ip>`
- **Or CNAME**: `fhir.yourdomain.com` → `<load-balancer-hostname>`

### Verify TLS Certificate

```bash
# Check certificate status
kubectl get certificate -n spark

# Check certificate details
kubectl describe certificate spark-tls -n spark
```

For production workloads, consider using an **external MongoDB** with proper backup and high availability.

### Gateway API (Traefik with Gateway)

If your cluster uses Traefik with **Gateway API** instead of standard Ingress, you'll need additional configuration.

The Helm chart creates a standard Kubernetes Ingress, but Gateway API requires:

1. **ReferenceGrant** - Allow the Gateway to access the TLS secret:

```yaml
apiVersion: gateway.networking.k8s.io/v1beta1
kind: ReferenceGrant
metadata:
  name: allow-traefik-gateway-spark-tls
  namespace: spark
spec:
  from:
    - group: gateway.networking.k8s.io
      kind: Gateway
      namespace: traefik
  to:
    - group: ""
      kind: Secret
      name: spark-tls
```

2. **Gateway listener** - Add HTTPS listener for your domain:

```bash
kubectl patch gateway traefik-gateway -n traefik --type=json -p='[
  {"op": "add", "path": "/spec/listeners/-", "value": {
    "name": "https-spark",
    "port": 8443,
    "protocol": "HTTPS",
    "hostname": "fhir.yourdomain.com",
    "tls": {
      "mode": "Terminate",
      "certificateRefs": [{"kind": "Secret", "name": "spark-tls", "namespace": "spark"}]
    },
    "allowedRoutes": {"namespaces": {"from": "All"}}
  }}
]'
```

3. **HTTPRoute** - Route traffic to Spark:

```yaml
apiVersion: gateway.networking.k8s.io/v1
kind: HTTPRoute
metadata:
  name: spark
  namespace: spark
spec:
  parentRefs:
    - name: traefik-gateway
      namespace: traefik
      sectionName: https-spark
  hostnames:
    - fhir.yourdomain.com
  rules:
    - matches:
        - path:
            type: PathPrefix
            value: /
      backendRefs:
        - name: spark
          port: 80
```

## Troubleshooting

```bash
# Check pod status
kubectl get pods -n spark

# View events
kubectl get events -n spark --sort-by='.lastTimestamp'

# Describe pod
kubectl describe pod <pod-name> -n spark

# Test MongoDB connection
kubectl exec -it spark-mongodb-0 -n spark -- mongosh --eval "db.adminCommand('ping')"

# Check Spark logs
kubectl logs -f deployment/spark -n spark
```

### TLS Certificate Issues

If you see "TRAEFIK DEFAULT CERT" instead of your Let's Encrypt certificate:

1. Check if certificate is ready:
   ```bash
   kubectl get certificate -n spark
   ```

2. Verify the secret contains the correct cert:
   ```bash
   kubectl get secret spark-tls -n spark -o jsonpath='{.data.tls\.crt}' | \
     base64 -d | openssl x509 -noout -subject -issuer
   ```

3. If using Gateway API, ensure ReferenceGrant and HTTPRoute are configured (see Gateway API section above).

## Helm Chart Reference

See [helm/spark/README.md](helm/spark/README.md) for full configuration options.
