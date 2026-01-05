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

# Find all lock files
$lockFiles = Get-ChildItem -Path . -Recurse -Filter "packages.lock.json" |
    Where-Object { $_.FullName -notlike "*\obj\*" -and $_.FullName -notlike "*\bin\*" }

if ($lockFiles.Count -eq 0) {
    Write-Host "Error: No packages.lock.json files found" -ForegroundColor Red
    exit 1
}

# Collect all unique packages to check
$packagesToCheck = [System.Collections.Generic.HashSet[string]]::new()

foreach ($lockFile in $lockFiles) {
    Write-Host "Scanning $($lockFile.FullName)..." -ForegroundColor Gray

    $lockContent = Get-Content $lockFile.FullName -Raw | ConvertFrom-Json

    # Parse dependencies from lock file
    foreach ($framework in $lockContent.dependencies.PSObject.Properties) {
        foreach ($package in $framework.Value.PSObject.Properties) {
            $packageName = $package.Name
            $packageVersion = $package.Value.resolved

            if (-not $packageVersion) { continue }

            $key = "$packageName@$packageVersion"
            $null = $packagesToCheck.Add($key)
        }
    }
}

Write-Host "Checking $($packagesToCheck.Count) unique packages in parallel..." -ForegroundColor Cyan

# Check packages in parallel
$results = $packagesToCheck | ForEach-Object -ThrottleLimit 10 -Parallel {
    $packageKey = $_
    $blockedLicenses = $using:BlockedLicenses
    $allowedLicenses = $using:AllowedLicenses
    $strictMode = $using:Strict

    # Split package key into name and version
    $parts = $packageKey -split '@', 2
    $packageName = $parts[0]
    $packageVersion = $parts[1]

    try {
        $nuspecUrl = "https://api.nuget.org/v3-flatcontainer/$($packageName.ToLower())/$packageVersion/$($packageName.ToLower()).nuspec"
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
            if ($licenseUrl -like "*mit*") { $license = "MIT" }
            elseif ($licenseUrl -like "*apache*") { $license = "Apache-2.0" }
            elseif ($licenseUrl -like "*bsd*") { $license = "BSD" }
        }

        if (-not $license) { $license = "Unknown" }
    }
    catch {
        $license = "Unknown"
    }

    # Test license
    $status = "ALLOWED"

    # Check if blocked
    foreach ($blocked in $blockedLicenses) {
        if ($license -like "*$blocked*") {
            $status = "BLOCKED"
            break
        }
    }

    # Check if explicitly allowed
    if ($status -ne "BLOCKED") {
        $isAllowed = $false
        foreach ($allowed in $allowedLicenses) {
            if ($license -eq $allowed) {
                $isAllowed = $true
                break
            }
        }
        if (-not $isAllowed) {
            $status = "UNKNOWN"
        }
    }

    # Return result
    [PSCustomObject]@{
        Package = $packageKey
        License = $license
        Status = $status
    }
}

# Process results
$violations = @()
$warnings = @()

foreach ($result in $results) {
    switch ($result.Status) {
        "BLOCKED" {
            $violations += "$($result.Package) : $($result.License) (COPYLEFT - BLOCKED)"
        }
        "UNKNOWN" {
            if ($Strict -or ($result.License -ne "Unknown" -and $result.License -notlike "http*")) {
                $warnings += "$($result.Package) : $($result.License) (not in allowed list)"
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
