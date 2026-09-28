#!/usr/bin/env bash
set -euo pipefail
: "${IMAGE_TAG:?IMAGE_TAG is required}"
[[ "$IMAGE_TAG" =~ ^[a-zA-Z0-9][a-zA-Z0-9_.-]+$ ]] || exit 2
name="smartwms-${IMAGE_TAG}"
postgres='postgres:16-alpine@sha256:721873c34ceb9f8d8fc265984940dc982404c105f19ad51be9fdc5970a6080ea'
cleanup() {
  docker rm -f "$name-tests" "$name-db" "$name-ui-tests" >/dev/null 2>&1 || true
  docker network rm "$name-net" >/dev/null 2>&1 || true
}
trap cleanup EXIT
mkdir -p reports/backend reports/frontend
docker build --target backend-tests -f pipeline/ci.Dockerfile -t "smartwms-tests:$IMAGE_TAG" .
docker build --target frontend-tests -f pipeline/ci.Dockerfile -t "smartwms-ui-tests:$IMAGE_TAG" .
docker network create "$name-net" >/dev/null
# This random password protects only the disposable test DB; never use application credentials.
test_password=$(od -An -N24 -tx1 /dev/urandom | tr -d ' \n')
docker run -d --name "$name-db" --network "$name-net" --network-alias db --tmpfs /var/lib/postgresql/data \
  -e POSTGRES_PASSWORD="$test_password" -e POSTGRES_DB=smartwms_tests \
  --health-cmd 'pg_isready -U postgres -d smartwms_tests' --health-interval 2s --health-retries 30 "$postgres" >/dev/null
for attempt in $(seq 1 60); do
  [ "$(docker inspect --format '{{.State.Health.Status}}' "$name-db")" = healthy ] && break
  [ "$attempt" -lt 60 ] || { echo 'Test database failed to start'; exit 1; }
  sleep 2
done
docker create --name "$name-tests" --network "$name-net" \
  -e SMARTWMS_TEST_DATABASE="Host=db;Database=smartwms_tests;Username=postgres;Password=$test_password" \
  "smartwms-tests:$IMAGE_TAG" >/dev/null
backend_status=0
docker start -a "$name-tests" || backend_status=$?
docker cp "$name-tests:/reports/." reports/backend/
[ "$backend_status" -eq 0 ] || exit "$backend_status"
docker create --name "$name-ui-tests" "smartwms-ui-tests:$IMAGE_TAG" >/dev/null
frontend_status=0
docker start -a "$name-ui-tests" || frontend_status=$?
docker cp "$name-ui-tests:/app/test-results/." reports/frontend/
exit "$frontend_status"
