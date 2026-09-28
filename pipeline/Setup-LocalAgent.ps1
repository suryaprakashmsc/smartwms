[CmdletBinding()]
param(
    [string]$Username = 'admin',
    [switch]$SkipAgentBuild
)
$ErrorActionPreference = 'Stop'
$compose = Join-Path $PSScriptRoot 'docker-compose.agent.yml'
function Invoke-Docker {
    param([string[]]$Arguments)
    & docker @Arguments
    if ($LASTEXITCODE -ne 0) { throw "Docker operation failed: $($Arguments[0])" }
}
function ConvertFrom-JenkinsXml {
    param(
        [Parameter(Mandatory=$true)][object]$Content,
        [ValidateSet('configuration','launch')][string]$ResponseKind = 'configuration'
    )
    $inputSource = $null
    $textSource = $null
    $decoder = $null
    $reader = $null
    $parseStage = 'decode'
    $declaredVersion = 'absent'
    try {
        # Windows PowerShell can return JNLP/XML content as bytes. Let the XML
        # reader detect a BOM, with strict UTF-8 as Jenkins' default encoding.
        if ($Content -is [byte[]]) {
            $inputSource = New-Object IO.MemoryStream(,$Content)
            $decoder = New-Object IO.StreamReader($inputSource, (New-Object Text.UTF8Encoding($false, $true)), $true)
            $xmlText = $decoder.ReadToEnd()
        } elseif ($Content -is [string]) {
            $xmlText = $Content
        } else {
            throw 'Unsupported response type.'
        }
        $xmlText = $xmlText.TrimStart([char]0xFEFF)
        if ($xmlText -match '^<\?xml\s+version\s*=\s*(["''])(1\.[01])\1') { $declaredVersion = $Matches[2] }
        # Jenkins XStream emits XML 1.1. These node fields use the XML 1.0
        # subset: normalize only the declaration, then retain strict parsing.
        # XML 1.1-only characters/entities remain invalid and are rejected.
        $xmlText = [regex]::Replace($xmlText, '^<\?xml\s+version\s*=\s*(["''])1\.1\1', '<?xml version="1.0"')
        $parseStage = 'parse'
        $settings = New-Object Xml.XmlReaderSettings
        $settings.DtdProcessing = [Xml.DtdProcessing]::Prohibit
        $settings.XmlResolver = $null
        $textSource = New-Object IO.StringReader($xmlText)
        $reader = [Xml.XmlReader]::Create($textSource, $settings)
        $document = New-Object Xml.XmlDocument
        $document.XmlResolver = $null
        $document.Load($reader)
        return ,$document
    } catch {
        # The launch response contains a secret: never include its body in errors.
        $failure = $_.Exception.GetBaseException()
        $location = if ($failure -is [Xml.XmlException]) { "line $($failure.LineNumber), column $($failure.LinePosition)" } else { 'not available' }
        throw "Jenkins returned invalid or unsupported XML ($ResponseKind; stage=$parseStage; XML=$declaredVersion; type=$($Content.GetType().Name); location=$location; exception=$($failure.GetType().Name)). Response content was withheld because it may contain an agent secret."
    } finally {
        if ($reader) { $reader.Dispose() }
        if ($textSource) { $textSource.Dispose() }
        if ($decoder) { $decoder.Dispose() }
        if ($inputSource) { $inputSource.Dispose() }
    }
}
Write-Host 'Creates a trusted local Jenkins agent and a separate privileged Docker builder.'
Write-Host 'The builder has no host Docker socket, published port, AWS credentials or Kubernetes credentials.'
$headers = @{}
$password = $null
$credential = $null
$base = 'http://localhost:8090'
$session = New-Object Microsoft.PowerShell.Commands.WebRequestSession
try {
    Write-Host "[1/5] Enter the existing Jenkins password for '$Username' in this terminal. Input is hidden."
    $password = Read-Host -Prompt 'Jenkins password (never paste it in chat)' -AsSecureString
    if ($password.Length -eq 0) { throw 'No password entered.' }
    $credential = New-Object System.Management.Automation.PSCredential($Username, $password)
    $headers.Authorization = 'Basic ' + [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes(
        $credential.UserName + ':' + $credential.GetNetworkCredential().Password))
    Write-Host '[2/5] Signing in to local Jenkins (30-second request timeout).'
    $crumb = Invoke-RestMethod -TimeoutSec 30 -Uri "$base/crumbIssuer/api/json" -Headers $headers -WebSession $session
    $headers[$crumb.crumbRequestField] = $crumb.crumb
    $jenkinsfile = [IO.File]::ReadAllText((Join-Path $PSScriptRoot 'Jenkinsfile'))
    if ([string]::IsNullOrWhiteSpace($jenkinsfile)) { throw 'The local Jenkinsfile is empty.' }
    # The validator reads a text request parameter, not an uploaded file part.
    $body = @{ jenkinsfile = $jenkinsfile }
    $validation = (Invoke-WebRequest -TimeoutSec 30 -UseBasicParsing -Method Post -Uri "$base/pipeline-model-converter/validate" -Headers $headers -WebSession $session -ContentType 'application/x-www-form-urlencoded; charset=utf-8' -Body $body).Content
    if ($validation -notmatch 'Jenkinsfile successfully validated') { throw "Jenkins pipeline validation failed: $validation" }
    Write-Host 'Jenkins declarative pipeline validation passed.'
    Write-Host '[3/5] Checking the Docker network and agent image.'
    Invoke-Docker -Arguments @('network', 'inspect', 'pipeline_default', '--format', '{{.Name}}')
    if ($SkipAgentBuild) {
        Invoke-Docker -Arguments @('image', 'inspect', 'smartwms-jenkins-agent:local', '--format', '{{.Id}}')
        Write-Host 'Reusing the existing agent image; build skipped.'
    } else {
        Invoke-Docker -Arguments @('build', '--progress=plain', '-t', 'smartwms-jenkins-agent:local', (Join-Path $PSScriptRoot 'agent'))
        Write-Host 'Agent image build command returned successfully.'
    }
    Write-Host '[4/5] Checking the Jenkins agent registration.'
    $nodes = Invoke-RestMethod -TimeoutSec 30 -Uri "$base/computer/api/json" -Headers $headers -WebSession $session
    if ('smartwms-local' -notin @($nodes.computer.displayName)) {
        $xml = @'
<slave>
  <name>smartwms-local</name>
  <description>Trusted local SmartWMS builds; isolated Docker daemon, no cloud credentials.</description>
  <remoteFS>/home/jenkins/agent</remoteFS>
  <numExecutors>1</numExecutors>
  <mode>EXCLUSIVE</mode>
  <retentionStrategy class="hudson.slaves.RetentionStrategy$Always"/>
  <launcher class="hudson.slaves.JNLPLauncher"><webSocket>true</webSocket></launcher>
  <label>smartwms-local</label>
  <nodeProperties/>
</slave>
'@
        Invoke-WebRequest -TimeoutSec 30 -UseBasicParsing -Method Post -Uri "$base/computer/createItem?name=smartwms-local" -Headers $headers -WebSession $session -ContentType 'application/xml' -Body $xml | Out-Null
    } else {
        [xml]$existing = ConvertFrom-JenkinsXml -ResponseKind configuration -Content (Invoke-WebRequest -TimeoutSec 30 -UseBasicParsing -Uri "$base/computer/smartwms-local/config.xml" -Headers $headers -WebSession $session).Content
        if ($existing.slave.numExecutors -ne '1' -or $existing.slave.mode -ne 'EXCLUSIVE' -or
            $existing.slave.remoteFS -ne '/home/jenkins/agent' -or $existing.slave.label -ne 'smartwms-local' -or
            $existing.slave.launcher.class -ne 'hudson.slaves.JNLPLauncher') {
            throw 'An existing agent differs from the reviewed configuration. Review it instead of overwriting it.'
        }
    }
    $secretDir = Join-Path $PSScriptRoot '.secrets'
    New-Item -ItemType Directory -Force -Path $secretDir | Out-Null
    # Build only the discretionary access rules. Reapplying a fetched complete
    # descriptor can ask Windows PowerShell to persist audit/owner information.
    $acl = New-Object Security.AccessControl.DirectorySecurity
    $acl.SetAccessRuleProtection($true, $false)
    foreach ($identity in @([Security.Principal.WindowsIdentity]::GetCurrent().User, [Security.Principal.SecurityIdentifier]'S-1-5-18')) {
        $acl.AddAccessRule((New-Object Security.AccessControl.FileSystemAccessRule($identity, 'FullControl', 'ContainerInherit,ObjectInherit', 'None', 'Allow')))
    }
    if ($PSVersionTable.PSEdition -eq 'Desktop') {
        [IO.Directory]::SetAccessControl($secretDir, $acl)
    } else {
        [IO.FileSystemAclExtensions]::SetAccessControl((New-Object IO.DirectoryInfo($secretDir)), $acl)
    }
    $savedAcl = Get-Acl -LiteralPath $secretDir
    $savedRules = @($savedAcl.GetAccessRules($true, $true, [Security.Principal.SecurityIdentifier]))
    $allowedSids = @([Security.Principal.WindowsIdentity]::GetCurrent().User.Value, 'S-1-5-18')
    if (-not $savedAcl.AreAccessRulesProtected -or $savedRules.Count -ne 2 -or
        @($savedRules | Where-Object { $_.IdentityReference.Value -notin $allowedSids -or
            $_.AccessControlType -ne 'Allow' -or $_.FileSystemRights -ne 'FullControl' -or
            $_.IsInherited -or $_.InheritanceFlags -ne 'ContainerInherit,ObjectInherit' }).Count) {
        throw 'Private agent folder permissions could not be verified. No launch secret was retrieved.'
    }
    [xml]$launch = ConvertFrom-JenkinsXml -ResponseKind launch -Content (Invoke-WebRequest -TimeoutSec 30 -UseBasicParsing -Uri "$base/computer/smartwms-local/jenkins-agent.jnlp" -Headers $headers -WebSession $session).Content
    $agentSecret = [string]$launch.jnlp.'application-desc'.argument[0]
    if ($agentSecret -notmatch '^[a-fA-F0-9]{64}$') { throw 'Unexpected agent launch response; no secret saved.' }
    [IO.File]::WriteAllText((Join-Path $secretDir 'agent-secret'), $agentSecret, (New-Object Text.UTF8Encoding($false)))
    $agentSecret = $null
    $launch = $null
    Write-Host '[5/5] Starting the local builder and agent, then waiting for connection.'
    Invoke-Docker -Arguments @('compose', '-f', $compose, 'config', '--quiet')
    Invoke-Docker -Arguments @('compose', '-f', $compose, 'up', '-d', '--no-build')
    for ($attempt = 0; $attempt -lt 60; $attempt++) {
        $node = Invoke-RestMethod -TimeoutSec 30 -Uri "$base/computer/smartwms-local/api/json" -Headers $headers -WebSession $session
        if (-not $node.offline) {
            Write-Host 'The dedicated smartwms-local agent is online. Return to Codex before the first build.'
            return
        }
        Start-Sleep -Seconds 3
    }
    throw 'Agent did not become online. Inspect its Jenkins node log and local Docker container logs; do not share secrets.'
} finally {
    $headers.Clear()
    $credential = $null
    if ($password) { $password.Dispose() }
    $password = $null
    $agentSecret = $null
    $launch = $null
}
