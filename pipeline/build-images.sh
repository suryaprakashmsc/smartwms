#!/usr/bin/env bash
set -euo pipefail
: "${IMAGE_TAG:?}" "${COGNITO_REGION:?}" "${COGNITO_USER_POOL_ID:?}" "${COGNITO_CLIENT_ID:?}" "${COGNITO_DOMAIN:?}"
: "${VITE_REDIRECT_SIGN_IN:?}" "${VITE_REDIRECT_SIGN_OUT:?}"
[[ "$IMAGE_TAG" =~ ^[a-zA-Z0-9][a-zA-Z0-9_.-]+$ ]] || exit 2
scanner='aquasec/trivy@sha256:62b1e65e8869bc4b4c6aa4fa2b21595256c7c2f6018a9d9ad61caf87187c1969'
scan_name="smartwms-scan-$IMAGE_TAG"
trap 'docker rm -f "$scan_name" >/dev/null 2>&1 || true' EXIT
mkdir -p release reports/scans
# A new attempt must not leave a previous bundle marked as verified.
rm -f release/SHA256SUMS
# Reports always describe this attempt, even when a build fails before scanning.
rm -f reports/scans/smartwms-inventory.json reports/scans/smartwms-ui.json
docker build -f src/services/Inventory.API/API/Dockerfile -t "smartwms-inventory:$IMAGE_TAG" src/services/Inventory.API
docker build -f src/frontend/smartwms-ui/Dockerfile -t "smartwms-ui:$IMAGE_TAG" \
  --build-arg VITE_API_BASE_URL=/api \
  --build-arg VITE_COGNITO_REGION="$COGNITO_REGION" \
  --build-arg VITE_COGNITO_USER_POOL_ID="$COGNITO_USER_POOL_ID" \
  --build-arg VITE_COGNITO_CLIENT_ID="$COGNITO_CLIENT_ID" \
  --build-arg VITE_COGNITO_DOMAIN="$COGNITO_DOMAIN" \
  --build-arg VITE_REDIRECT_SIGN_IN="$VITE_REDIRECT_SIGN_IN" \
  --build-arg VITE_REDIRECT_SIGN_OUT="$VITE_REDIRECT_SIGN_OUT" src/frontend/smartwms-ui
scan_failed=0
for image in smartwms-inventory smartwms-ui; do
  docker save "$image:$IMAGE_TAG" -o "release/$image.tar"
  docker create --name "$scan_name" --mount type=volume,source=smartwms-trivy-cache,target=/root/.cache/trivy \
    "$scanner" image --input /tmp/image.tar --no-progress --timeout 10m \
    --scanners vuln --severity HIGH,CRITICAL --exit-code 1 --format json --output /tmp/scan.json >/dev/null
  docker cp "release/$image.tar" "$scan_name:/tmp/image.tar"
  result=0
  docker start -a "$scan_name" || result=$?
  docker cp "$scan_name:/tmp/scan.json" "reports/scans/$image.json"
  docker rm "$scan_name" >/dev/null
  if [ "$result" -ne 0 ]; then
    echo "Image scan failed: $image (reports/scans/$image.json)"
    scan_failed=1
  fi
done
[ "$scan_failed" -eq 0 ] || { echo 'Release blocked. Review both image scan reports.'; exit 1; }
for image in smartwms-inventory smartwms-ui; do gzip -f "release/$image.tar"; done
printf '%s\n' "$IMAGE_TAG" > release/image-tag.txt
git -c safe.directory="$PWD" rev-parse HEAD > release/commit.txt
(cd release && sha256sum *.tar.gz image-tag.txt commit.txt > SHA256SUMS)
