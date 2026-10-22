param(
    [Parameter(Mandatory = $true)]
    [string]$TarballPath
)

$ErrorActionPreference = "Stop"

if (!(Test-Path $TarballPath))
{
    throw "Tarball not found: $TarballPath"
}

$resolvedTarballPath = (Resolve-Path $TarballPath).Path
$tarEntries = & tar -tf $resolvedTarballPath
if ($LASTEXITCODE -ne 0)
{
    throw "Failed to inspect tarball contents: $resolvedTarballPath"
}

if ($tarEntries -notcontains "package/.attestation.p7m" -and $tarEntries -notcontains ".attestation.p7m")
{
    throw "Tarball does not contain .attestation.p7m. Expected a Unity-signed package."
}

$packageJsonEntry = if ($tarEntries -contains "package/package.json") { "package/package.json" } else { "package.json" }
$package = (& tar -xOf $resolvedTarballPath $packageJsonEntry) | ConvertFrom-Json
$expectedFilename = "$($package.name)-$($package.version).tgz"

if ([System.IO.Path]::GetFileName($resolvedTarballPath) -ne $expectedFilename)
{
    throw "Tarball filename does not match package metadata. Expected '$expectedFilename'."
}

& npm.cmd whoami --registry=https://registry.npmjs.org/ | Out-Null
if ($LASTEXITCODE -ne 0)
{
    throw "npm authentication failed."
}

& npm.cmd publish $resolvedTarballPath --registry=https://registry.npmjs.org/
if ($LASTEXITCODE -ne 0)
{
    throw "npm publish failed with exit code $LASTEXITCODE"
}
