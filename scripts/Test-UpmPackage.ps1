param(
    [string]$PackagePath = "Packages/com.jaimecamacho.packagecreator-tool"
)

$ErrorActionPreference = "Stop"

$packageJsonPath = Join-Path $PackagePath "package.json"
if (!(Test-Path $packageJsonPath))
{
    throw "Missing package.json at $packageJsonPath"
}

$package = Get-Content -Raw -Path $packageJsonPath | ConvertFrom-Json

if ([string]::IsNullOrWhiteSpace($package.name))
{
    throw "package.json is missing 'name'."
}

if ($package.name -notmatch '^com\.[a-z0-9\-]+(\.[a-z0-9\-]+)+$')
{
    throw "Package name '$($package.name)' is not a valid Unity reverse-domain package name."
}

if ([string]::IsNullOrWhiteSpace($package.version))
{
    throw "package.json is missing 'version'."
}

if ($package.version -notmatch '^\d+\.\d+\.\d+(-[0-9A-Za-z\.-]+)?$')
{
    throw "Package version '$($package.version)' is not valid semver."
}

if (!(Test-Path "README.md") -or !(Test-Path "CHANGELOG.md") -or !(Test-Path "LICENSE"))
{
    throw "Root README.md, CHANGELOG.md and LICENSE must exist."
}

Write-Host "UPM package validation passed for $($package.name) $($package.version)"
