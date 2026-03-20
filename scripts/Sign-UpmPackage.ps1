param(
    [Parameter(Mandatory = $true)]
    [string]$UnityPath,

    [string]$ProjectPath = ".",
    [string]$PackagePath = "Packages/com.jaimecamacho.packagecreator-tool",
    [string]$OutputDirectory = "ReleaseArtifacts",
    [string]$CloudOrganization = $env:UNITY_CLOUD_ORGANIZATION,
    [string]$Username = $env:UNITY_USERNAME,
    [string]$Password = $env:UNITY_PASSWORD
)

$ErrorActionPreference = "Stop"

if (!(Test-Path $UnityPath))
{
    throw "Unity executable not found: $UnityPath"
}

$package = Get-Content -Raw -Path (Join-Path $PackagePath "package.json") | ConvertFrom-Json
$packageName = $package.name
$version = $package.version

New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$resolvedProjectPath = (Resolve-Path $ProjectPath).Path
$resolvedPackagePath = (Resolve-Path $PackagePath).Path
$resolvedOutputDirectory = (Resolve-Path $OutputDirectory).Path
$logFile = Join-Path $resolvedOutputDirectory "sign-upm.log"

$arguments = @(
    "-batchmode",
    "-quit",
    "-projectPath", $resolvedProjectPath,
    "-upmPack", $resolvedPackagePath, $resolvedOutputDirectory,
    "-logfile", $logFile
)

if (![string]::IsNullOrWhiteSpace($CloudOrganization))
{
    $arguments += @("-cloudOrganization", $CloudOrganization)
}

if (![string]::IsNullOrWhiteSpace($Username))
{
    $arguments += @("-username", $Username)
}

if (![string]::IsNullOrWhiteSpace($Password))
{
    $arguments += @("-password", $Password)
}

& $UnityPath @arguments
if ($LASTEXITCODE -ne 0)
{
    if (Test-Path $logFile)
    {
        Get-Content -Path $logFile -Tail 80
    }

    throw "Unity signing/export failed with exit code $LASTEXITCODE"
}

$expectedFile = Join-Path $resolvedOutputDirectory "$packageName-$version.tgz"
if (!(Test-Path $expectedFile))
{
    throw "Signed tarball not found at $expectedFile"
}

Write-Host "Signed package exported to $expectedFile"
