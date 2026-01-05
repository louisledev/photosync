#!/usr/bin/env pwsh
# PowerShell script to check NuGet package licenses
# Works cross-platform and integrates better with .NET tooling

param(
    [switch]$Strict = $false
)

$ErrorActionPreference = "Stop"

Write-Host "Checking NuGet package licenses..." -ForegroundColor Cyan

# Copyleft licenses to block
$BlockedLicenses = @(
    "GPL-2.0", "GPL-3.0", "GPL-2.0-only", "GPL-3.0-only",
    "AGPL-3.0", "AGPL-3.0-only",
    "LGPL-2.1", "LGPL-3.0", "LGPL-2.1-only", "LGPL-3.0-only",
    "MPL-2.0",
    "EPL-1.0", "EPL-2.0",
    "CDDL-1.0", "CDDL-1.1",
    "CPL-1.0",
    "OSL-3.0",
    "EUPL-1.2",
    "CC-BY-SA-4.0"
)

# Allowed permissive licenses
$AllowedLicenses = @(
    "MIT",
    "Apache-2.0",
    "BSD-2-Clause", "BSD-3-Clause",
    "ISC",
    "0BSD",
    "CC0-1.0",
    "Unlicense",
    "MS-PL", "MS-RL",
    "CC-BY-4.0",
    "Python-2.0",
    "Zlib",
    "BSL-1.0"
)

function Test-License {
    param($License)

    foreach ($blocked in $BlockedLicenses) {
        if ($License -like "*$blocked*") {
            return "BLOCKED"
        }
    }

    foreach ($allowed in $AllowedLicenses) {
        if ($License -eq $allowed) {
            return "ALLOWED"
        }
    }

    return "UNKNOWN"
}

function Get-PackageLicense {
    param($PackageName, $Version)

    try {
        $nuspecUrl = "https://api.nuget.org/v3-flatcontainer/$($PackageName.ToLower())/$Version/$($PackageName.ToLower()).nuspec"
        $response = Invoke-WebRequest -Uri $nuspecUrl -UseBasicParsing -TimeoutSec 5
        [xml]$nuspec = $response.Content

        # Try to get license expression first (SPDX format)
        $license = $nuspec.package.metadata.license.'#text'
        if (-not $license) {
            $license = $nuspec.package.metadata.license.InnerText
        }

        # Fallback to licenseUrl
        if (-not $license) {
            $licenseUrl = $nuspec.package.metadata.licenseUrl
            if ($licenseUrl -like "*mit*") { return "MIT" }
            if ($licenseUrl -like "*apache*") { return "Apache-2.0" }
            if ($licenseUrl -like "*bsd*") { return "BSD" }
        }

        return $license
    }
    catch {
        return "Unknown"
    }
}

# Find all lock files
$lockFiles = Get-ChildItem -Path . -Recurse -Filter "packages.lock.json" |
    Where-Object { $_.FullName -notlike "*\obj\*" -and $_.FullName -notlike "*\bin\*" }

if ($lockFiles.Count -eq 0) {
    Write-Host "Error: No packages.lock.json files found" -ForegroundColor Red
    exit 1
}

$violations = @()
$warnings = @()
$checked = @{}

foreach ($lockFile in $lockFiles) {
    Write-Host "Checking $($lockFile.FullName)..." -ForegroundColor Gray

    $lockContent = Get-Content $lockFile.FullName -Raw | ConvertFrom-Json

    # Parse dependencies from lock file
    foreach ($framework in $lockContent.dependencies.PSObject.Properties) {
        foreach ($package in $framework.Value.PSObject.Properties) {
            $packageName = $package.Name
            $packageVersion = $package.Value.resolved

            if (-not $packageVersion) { continue }

            $key = "$packageName@$packageVersion"
            if ($checked.ContainsKey($key)) { continue }
            $checked[$key] = $true

            Write-Host "  Checking $key..." -ForegroundColor DarkGray

            $license = Get-PackageLicense -PackageName $packageName -Version $packageVersion
            $status = Test-License -License $license

            switch ($status) {
                "BLOCKED" {
                    $violations += "$key : $license (COPYLEFT - BLOCKED)"
                }
                "UNKNOWN" {
                    if ($Strict -or ($license -ne "Unknown" -and $license -notlike "http*")) {
                        $warnings += "$key : $license (not in allowed list)"
                    }
                }
            }
        }
    }
}

Write-Host ""

if ($violations.Count -gt 0) {
    Write-Host "❌ License violations found (copyleft licenses detected):" -ForegroundColor Red
    $violations | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
    Write-Host ""
    exit 1
}

if ($warnings.Count -gt 0) {
    Write-Host "⚠️  Warnings (unknown or unlisted licenses):" -ForegroundColor Yellow
    $warnings | ForEach-Object { Write-Host "  $_" -ForegroundColor Yellow }
    Write-Host ""
}

Write-Host "✅ All packages use permissive licenses" -ForegroundColor Green
exit 0
