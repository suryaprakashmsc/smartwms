[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$configPath = Join-Path $PSScriptRoot '.env'
if (-not (Test-Path -LiteralPath $configPath)) { throw 'The existing pipeline/.env file is required for public Cognito configuration.' }
$names = @('COGNITO_REGION','COGNITO_USER_POOL_ID','COGNITO_CLIENT_ID','COGNITO_DOMAIN','VITE_REDIRECT_SIGN_IN','VITE_REDIRECT_SIGN_OUT')
$config = @{VITE_REDIRECT_SIGN_IN='http://localhost:3000/callback';VITE_REDIRECT_SIGN_OUT='http://localhost:3000/login'}
foreach ($line in Get-Content -LiteralPath $configPath) {
    if ($line -match '^\s*([A-Z_]+)\s*=\s*(.*?)\s*$' -and $Matches[1] -in $names) {
        $config[$Matches[1]] = $Matches[2].Trim('"').Trim("'")
    }
}
foreach ($name in $names) { if ([string]::IsNullOrWhiteSpace($config[$name])) { throw "Missing public configuration: $name" } }
$commit = & git -C $repo rev-parse --short=12 HEAD
if ($LASTEXITCODE -ne 0 -or $commit -notmatch '^[a-f0-9]{12}$') { throw 'Cannot identify the source checkout.' }
$tag = (Get-Date -Format 'yyyyMMddHHmmss') + '-' + $commit
$argsList = @('compose','-f',(Join-Path $PSScriptRoot 'docker-compose.agent.yml'),'run','--rm','--no-deps',
    '--entrypoint','bash','--workdir','/workspace','--volume',"${repo}:/workspace",'-e',"IMAGE_TAG=$tag")
foreach ($name in $names) { $argsList += @('-e',"$name=$($config[$name])") }
$argsList += @('agent','-lc','bash pipeline/ci-test.sh && bash pipeline/build-images.sh')
Write-Host 'Running tests, image builds and vulnerability scans in the local isolated builder. No AWS publishing.'
& docker @argsList
if ($LASTEXITCODE -ne 0) {
    foreach ($image in @('smartwms-inventory','smartwms-ui')) {
        $report = Join-Path $repo "reports\scans\$image.json"
        if (Test-Path -LiteralPath $report) {
            $scan = Get-Content -LiteralPath $report -Raw | ConvertFrom-Json
            $findings = @($scan.Results | ForEach-Object { $_.Vulnerabilities } | Where-Object { $_ -and $_.Severity -in @('HIGH','CRITICAL') })
            Write-Host "$image saved scan: $($findings.Count) high/critical findings. Full report: $report"
            $findings | Select-Object -First 12 | ForEach-Object {
                $fix = if ($_.FixedVersion) { $_.FixedVersion } else { 'not listed' }
                Write-Host "$($_.Severity) $($_.VulnerabilityID) $($_.PkgName) $($_.InstalledVersion); fix: $fix"
            }
        }
    }
    throw 'Local CI verification failed. Preserve reports and return the error, without secrets.'
}
Write-Host 'Local CI passed. The reports/ and release/ directories contain the evidence and local images.'
Write-Host 'This bundle contains working-directory changes, so its commit.txt alone does not prove committed source. Commit and rebuild in Jenkins before treating it as a release.'
