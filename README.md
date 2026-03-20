# PackageCreator

Unity project and reusable UPM package for scaffolding Unity packages and automating the release flow.

## What it does

- creates a package under `Packages/<reverse-domain-name>`
- generates `Runtime`, `Editor`, `Documentation~` and optional `Samples~`
- generates `README.md`, `CHANGELOG.md`, `LICENSE` and asmdefs
- generates reusable release scripts for `prepare -> sign -> publish`

## Included package

This repository contains:

- `Packages/com.jaimecamacho.packagecreator`

After importing it in another Unity project, open:

- `Tools > JaimeCamachoDev > Package Creator`

## Generated release scripts

The tool can generate these project-level files:

- `scripts/Prepare-UpmRelease.ps1`
- `scripts/Sign-UpmPackage.ps1`
- `scripts/Publish-UpmPackage.ps1`
- `scripts/Publish-SignedTarball.ps1`
- `scripts/Release-UpmPackage.ps1`
- `scripts/Test-UpmPackage.ps1`
- `Release-UpmPackage.bat`
- `Release-UpmPackage-ManualSign.bat`
- `Publish-SignedTarball.bat`

Typical usage:

```powershell
./Release-UpmPackage-ManualSign.bat -Version 1.2.0
```

That flow prepares metadata, waits for the signed `.tgz` export if needed, and then publishes it to npm.
