# License Compliance

This document describes how the project enforces license compliance to prevent the use of copyleft and weak copyleft licenses.

## Overview

The project uses automated license checking to ensure all NuGet dependencies use permissive licenses only. This prevents legal issues related to copyleft licenses (GPL, LGPL, MPL, etc.) that could require releasing your source code.

## Blocked Licenses (Copyleft)

The following licenses are **blocked** and will cause CI builds to fail:

### Strong Copyleft
- **GPL-2.0, GPL-3.0** - GNU General Public License (requires derivative works to be GPL)
- **AGPL-3.0** - Affero GPL (like GPL but also covers network use)
- **OSL-3.0** - Open Software License

### Weak Copyleft
- **LGPL-2.1, LGPL-3.0** - Lesser GPL (requires changes to the library to be released)
- **MPL-2.0** - Mozilla Public License (file-level copyleft)
- **EPL-1.0, EPL-2.0** - Eclipse Public License
- **CDDL-1.0, CDDL-1.1** - Common Development and Distribution License
- **CPL-1.0** - Common Public License
- **EUPL-1.2** - European Union Public License
- **CC-BY-SA-4.0** - Creative Commons ShareAlike

## Allowed Licenses (Permissive)

The following licenses are explicitly **allowed**:

- **MIT** - Most common permissive license
- **Apache-2.0** - Includes patent grant
- **BSD-2-Clause, BSD-3-Clause** - Berkeley Software Distribution licenses
- **ISC** - Internet Systems Consortium license (similar to MIT)
- **0BSD** - Zero-Clause BSD (public domain equivalent)
- **CC0-1.0** - Creative Commons Public Domain
- **Unlicense** - Public domain dedication
- **MS-PL, MS-RL** - Microsoft Public/Reciprocal License
- **CC-BY-4.0** - Creative Commons Attribution (no ShareAlike)
- **Python-2.0** - Python Software Foundation License
- **Zlib** - zlib/libpng License
- **BSL-1.0** - Boost Software License

## How It Works

### 1. Lock Files

The project uses NuGet lock files (`packages.lock.json`) to track exact dependency versions:

```xml
<RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>
<RestoreLockedMode>true</RestoreLockedMode>
```

- `RestorePackagesWithLockFile`: Generates lock files during restore
- `RestoreLockedMode`: Enforces lock files and fails if out of sync

### 2. License Check Script

The license check is implemented in PowerShell (cross-platform):

```bash
pwsh ./scripts/check-licenses.ps1
```

Features:
- Reads `packages.lock.json` files
- Fetches license info from NuGet API in parallel (10 concurrent requests)
- Blocks copyleft licenses
- Warns about unknown licenses
- Fast: checks 137 packages in ~3 seconds

### 3. CI Integration

The license check runs automatically in CI after dependency restore:

```yaml
- name: Check package licenses
  run: pwsh ./scripts/check-licenses.ps1
```

The build will **fail** if any copyleft licenses are detected.

## Updating Dependencies

When updating NuGet packages:

1. Update the `.csproj` file with new versions:
   ```xml
   <PackageReference Include="PackageName" Version="1.2.3" />
   ```

2. Regenerate lock files:
   ```bash
   dotnet restore --force-evaluate
   ```

3. Run license check locally:
   ```bash
   pwsh ./scripts/check-licenses.ps1
   ```

4. If copyleft licenses are detected:
   - Find an alternative package with a permissive license
   - Or get legal approval and add an exception (see below)

## Handling Unknown Licenses

Some packages may show up as warnings with "unknown" licenses. These need manual verification:

1. Check the package's repository or NuGet page
2. Verify the license is permissive
3. If acceptable, add to the allowed list in the script:

**In `check-licenses.ps1`:**
```powershell
$AllowedLicenses = @(
    # ... existing licenses ...
    "NewLicenseName"
)
```

## Adding License Exceptions

If you need to use a package with a copyleft license (after legal review):

1. Document the exception and justification
2. Add the package to an exception list in the script
3. Update this documentation with the rationale

**Example:**
```powershell
# Exceptions (with justification)
$ExceptionPackages = @{
    "SomeGPLPackage" = "Approved by legal team - used only for build tooling, not distributed"
}
```

## Verifying Current Dependencies

To see all dependencies and their licenses:

```bash
# List all packages
dotnet list package

# Check for vulnerabilities
dotnet list package --vulnerable --include-transitive

# Run license check
pwsh ./scripts/check-licenses.ps1
```

## Best Practices

1. **Always check licenses** before adding new dependencies
2. **Run license check locally** before committing
3. **Keep lock files updated** when changing dependencies
4. **Prefer well-known packages** with clear licensing
5. **Review transitive dependencies** - your direct dependencies may pull in copyleft packages

## Troubleshooting

### Build fails with "lock file is out of sync"

```bash
dotnet restore --force-evaluate
```

### License check shows many unknowns

Older Microsoft packages may not have proper license metadata. These are generally MIT licensed. You can verify on:
- https://github.com/dotnet/
- https://www.nuget.org/packages/[PackageName]

### CI fails but local check passes

Ensure you've committed the updated `packages.lock.json` files.

## Resources

- [NuGet Package Licenses](https://learn.microsoft.com/en-us/nuget/reference/nuspec#license)
- [SPDX License List](https://spdx.org/licenses/)
- [Choose a License](https://choosealicense.com/)
- [Open Source Licenses Comparison](https://opensource.org/licenses)

## Maintenance

Review and update the allowed/blocked license lists periodically as:
- New licenses emerge
- Legal requirements change
- Package ecosystems evolve
