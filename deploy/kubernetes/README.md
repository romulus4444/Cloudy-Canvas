# Running Cloudy Canvas on Kubernetes

This folder is a [kustomize](https://kustomize.io/) base with the bot's Deployment and ServiceAccount. It has no namespace, so the cluster chooses where it goes.

## What the cluster must provide

The manifests leave out everything that is specific to one installation. The cluster operator creates these in the target namespace:

| Kind | Name | Contents |
|---|---|---|
| Secret | `cloudy-canvas-secrets` | `DiscordSettings__token` (required), `ManebooruSettings__token` (optional) |
| ConfigMap | `cloudy-canvas-config` | Optional. Other settings as environment variables, e.g. `DiscordSettings__BroadcastUserIds__0`, `LogRetention__RetentionDays` |
| PersistentVolumeClaim | `cloudy-canvas-data` | ReadWriteOnce, about 1Gi. Holds every server's settings and logs |

The volume is not part of these manifests on purpose. Removing or renaming something here can then never delete the bot's data.

The pod runs as UID/GID 1654 with a read-only root filesystem, no capabilities and the `RuntimeDefault` seccomp profile, so it meets the `restricted` Pod Security Standard. It needs outgoing HTTPS (port 443) to Discord and Manebooru, and accepts no incoming connections.

There is exactly one replica, and updates use `Recreate`. Two copies would answer every command twice.

There are no liveness or readiness probes: the bot has no port and the image has no shell. It exits with an error when something fatal happens (for example a wrong token), and Kubernetes restarts it.

## Releases

Pushing a tag like `v1.2.3` on `mane` runs `.github/workflows/release.yml`, which:

1. builds the image and pushes it to `ghcr.io/<owner>/cloudy-canvas:1.2.3` (and `latest` for releases that are not prereleases);
2. sets the image in this folder to that exact digest;
3. pushes this folder to `ghcr.io/<owner>/cloudy-canvas-manifests:1.2.3`.

A cluster running [Flux](https://fluxcd.io/) can follow new releases with an `OCIRepository` that watches `oci://ghcr.io/<owner>/cloudy-canvas-manifests` with a semver range, plus a `Kustomization` that applies it. The manifests artifact is only pushed after the image, so a cluster never sees a release whose image is missing.

Both packages should be public so clusters can pull them without credentials. Packages of a public repository usually are; check under the package settings ("Change visibility") after the first release.

## Trying it locally

```sh
kubectl create namespace cloudy-canvas
kubectl -n cloudy-canvas create secret generic cloudy-canvas-secrets --from-literal=DiscordSettings__token=...
kubectl -n cloudy-canvas apply -f - <<EOF
apiVersion: v1
kind: PersistentVolumeClaim
metadata:
  name: cloudy-canvas-data
spec:
  accessModes: [ReadWriteOnce]
  resources:
    requests:
      storage: 1Gi
EOF
kubectl -n cloudy-canvas apply -k deploy/kubernetes
```
