[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][string]$ReleaseDirectory,
    [Parameter(Mandatory=$true)][ValidatePattern('^k3d-[a-zA-Z0-9-]+$')][string]$Context
)
$ErrorActionPreference = 'Stop'
foreach ($tool in @('docker','kubectl','k3d')) { Get-Command $tool -ErrorAction Stop | Out-Null }
function Invoke-Kubectl {
    param([string[]]$Arguments)
    & kubectl --context $Context @Arguments
    if ($LASTEXITCODE -ne 0) { throw "Kubernetes operation failed: $($Arguments[0])" }
}
$release = (Resolve-Path -LiteralPath $ReleaseDirectory).Path
$allowed = @('smartwms-inventory.tar.gz','smartwms-ui.tar.gz','image-tag.txt','commit.txt')
$verified = @()
foreach ($line in Get-Content -LiteralPath (Join-Path $release 'SHA256SUMS')) {
    if ($line -notmatch '^([a-fA-F0-9]{64})\s+\*?([^\s]+)$') { throw 'Invalid release checksum entry.' }
    $hash = $Matches[1]; $name = $Matches[2]
    if ($name -notin $allowed -or $name -in $verified) { throw 'Unexpected or duplicate release file.' }
    if ((Get-FileHash -LiteralPath (Join-Path $release $name) -Algorithm SHA256).Hash -ne $hash) { throw "Checksum mismatch: $name" }
    $verified += $name
}
if ($verified.Count -ne $allowed.Count) { throw 'Incomplete release bundle.' }
$tag = (Get-Content -Raw -LiteralPath (Join-Path $release 'image-tag.txt')).Trim()
if ($tag -notmatch '^[0-9]+-[a-f0-9]{12}$' -or $tag.Length -gt 40) { throw 'Invalid release tag.' }
Invoke-Kubectl -Arguments @('get','nodes','-o','name')
foreach ($image in @('smartwms-inventory','smartwms-ui')) {
    & docker load -i (Join-Path $release "$image.tar.gz")
    if ($LASTEXITCODE -ne 0) { throw 'Image load failed.' }
    & k3d image import "${image}:$tag" -c $Context.Substring(4)
    if ($LASTEXITCODE -ne 0) { throw 'Image import failed.' }
}
Invoke-Kubectl -Arguments @('apply','-f',(Join-Path $PSScriptRoot 'local/namespace.yaml'))
$secretName = & kubectl --context $Context -n smartwms get secret inventory-database --ignore-not-found -o name
if ($LASTEXITCODE -ne 0) { throw 'Cannot inspect database secret.' }
if (-not $secretName) {
    $bytes = New-Object byte[] 32
    $rng = [Security.Cryptography.RandomNumberGenerator]::Create()
    try { $rng.GetBytes($bytes) } finally { $rng.Dispose() }
    $password = ([BitConverter]::ToString($bytes)).Replace('-','').ToLowerInvariant()
    $secret = @{apiVersion='v1';kind='Secret';metadata=@{name='inventory-database';namespace='smartwms'};type='Opaque';stringData=@{
        password=$password
        'connection-string'="Host=postgres;Database=inventory;Username=smartwms;Password=$password"
    }}
    $secret | ConvertTo-Json -Depth 6 -Compress | & kubectl --context $Context apply -f - | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Secret creation failed.' }
    $secret=$null; $password=$null; $bytes=$null
}
$manifest = & kubectl kustomize (Join-Path $PSScriptRoot 'local')
if ($LASTEXITCODE -ne 0) { throw 'Manifest rendering failed.' }
($manifest -join "`n").Replace('replace-with-verified-tag',$tag) | & kubectl --context $Context apply -f -
if ($LASTEXITCODE -ne 0) { throw 'Manifest application failed.' }
Invoke-Kubectl -Arguments @('-n','smartwms','rollout','status','statefulset/postgres','--timeout=180s')
$jobName = 'inventory-migrate-' + (Get-Date -Format 'yyyyMMddHHmmss')
$job = @{
    apiVersion='batch/v1'; kind='Job'; metadata=@{name=$jobName;namespace='smartwms'}
    spec=@{backoffLimit=0;activeDeadlineSeconds=180;ttlSecondsAfterFinished=600;template=@{
        metadata=@{labels=@{app='inventory-migration'}}
        spec=@{restartPolicy='Never';automountServiceAccountToken=$false;securityContext=@{runAsNonRoot=$true;runAsUser=1654;runAsGroup=1654;seccompProfile=@{type='RuntimeDefault'}}
            containers=@(@{name='migrate';image="smartwms-inventory:$tag";imagePullPolicy='Never';args=@('--migrate','--seed-demo')
                envFrom=@(@{configMapRef=@{name='inventory-config'}})
                env=@(@{name='ConnectionStrings__DefaultConnection';valueFrom=@{secretKeyRef=@{name='inventory-database';key='connection-string'}}})
                securityContext=@{allowPrivilegeEscalation=$false;readOnlyRootFilesystem=$true;capabilities=@{drop=@('ALL')}}
                resources=@{requests=@{cpu='100m';memory='128Mi'};limits=@{cpu='1';memory='512Mi'}}
                volumeMounts=@(@{name='tmp';mountPath='/tmp'})})
            volumes=@(@{name='tmp';emptyDir=@{}})
        }
    }}
}
$job | ConvertTo-Json -Depth 20 -Compress | & kubectl --context $Context apply -f -
if ($LASTEXITCODE -ne 0) { throw 'Migration job creation failed.' }
Invoke-Kubectl -Arguments @('-n','smartwms','wait','--for=condition=complete',"job/$jobName",'--timeout=180s')
foreach ($deployment in @('inventory-api','frontend')) {
    Invoke-Kubectl -Arguments @('-n','smartwms','rollout','status',"deployment/$deployment",'--timeout=180s')
}
Write-Host "Deployment ready on $Context. Verify the HTTP endpoints using a localhost port-forward before switching the dashboard."
Write-Host "kubectl --context $Context -n smartwms port-forward service/frontend 3001:8080 --address 127.0.0.1"
Write-Host 'Check localhost:3001, /api/health/ready (200), and /api/products (401 without a token).'
Write-Host 'For Cognito login, use localhost:3000 after stopping only the Compose frontend, because the image uses the existing callback allowlist.'
