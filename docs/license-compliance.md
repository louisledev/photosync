# License Compliance

This document describes how the project enforces license compliance to prevent the use of copyleft and weak copyleft licenses.

## Overview

The project uses automated license checking via GitHub Actions to ensure all NuGet dependencies use permissive licenses only. This prevents legal issues related to copyleft licenses (GPL, LGPL, MPL, etc.) that could require releasing your source code.

## Blocked Licenses (Copyleft)

The following licenses are **blocked** and will cause CI builds to fail:

### Strong Copyleft
- **GPL-2.0, GPL-3.0** (and -only variants) - GNU General Public License (requires derivative works to be GPL)
- **AGPL-3.0** (and -only variant) - Affero GPL (like GPL but also covers network use)
- **OSL-3.0** - Open Software License

### Weak Copyleft
- **LGPL-2.1, LGPL-3.0** (and -only variants) - Lesser GPL (requires changes to the library to be released)
- **MPL-2.0** - Mozilla Public License (file-level copyleft)
- **EPL-1.0, EPL-2.0** - Eclipse Public License
- **CDDL-1.0, CDDL-1.1** - Common Development and Distribution License
- **CPL-1.0** - Common Public License
- **EUPL-1.2** - European Union Public License
- **CC-BY-SA-4.0** - Creative Commons ShareAlike

## How It Works

### GitHub Actions Dependency Review

The license check is implemented using the [`actions/dependency-review-action`](https://github.com/actions/dependency-review-action) in the Security workflow.

**Workflow file:** [.github/workflows/security.yml](../.github/workflows/security.yml)

```yaml
dependency-review:
  name: Dependency & License Review
  runs-on: ubuntu-latest

  steps:
    - name: Dependency Review
      uses: actions/dependency-review-action@v4
      with:
        fail-on-severity: moderate
        deny-licenses: GPL-2.0, GPL-3.0, GPL-2.0-only, GPL-3.0-only, ...
```

**Features:**
- Automatically analyzes dependency changes in the dependency graph
- Blocks all copyleft licenses listed above
- Fails on moderate or higher severity vulnerabilities
- **Runs only on pull requests** (requires base/head branch comparison to detect changes)

### NuGet Lock Files

The project uses NuGet lock files (`packages.lock.json`) to track exact dependency versions:

```xml
<RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>
<RestoreLockedMode>true</RestoreLockedMode>
```

- `RestorePackagesWithLockFile`: Generates lock files during restore
- `RestoreLockedMode`: Enforces lock files and fails if out of sync

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

3. Create a pull request - the dependency-review action will automatically check licenses

4. If copyleft licenses are detected:
   - Find an alternative package with a permissive license
   - Or get legal approval and update the workflow configuration

## Adding License Exceptions

If, after legal review, an exception is granted for a specific package with a blocked license:

1. Document the exception and justification in your team's architectural or risk documentation
2. Record who approved the exception and under which conditions (e.g., "build-time only, not shipped")
3. Remove the specific license from the `deny-licenses` list in [.github/workflows/security.yml](../.github/workflows/security.yml)
4. Add a comment explaining the exception

**Note:** Be very cautious with exceptions, as copyleft licenses can have significant legal implications.

## Verifying Current Dependencies

To see all dependencies and their licenses:

```bash
# List all packages
dotnet list package

# Check for vulnerabilities
dotnet list package --vulnerable --include-transitive

# View dependency graph on GitHub
# Go to Insights -> Dependency graph -> Dependencies
```

## Best Practices

1. **Always check licenses** before adding new dependencies
2. **Review PRs carefully** - the dependency-review action will flag license issues
3. **Keep lock files updated** when changing dependencies
4. **Prefer well-known packages** with clear licensing
5. **Review transitive dependencies** - your direct dependencies may pull in copyleft packages

## Troubleshooting

### Build fails with "lock file is out of sync"

```bash
dotnet restore --force-evaluate
```

### How to check a specific package's license

1. Visit https://www.nuget.org/packages/[PackageName]
2. Look for the "License" section in the package details
3. Check the package's GitHub repository

### CI fails on dependency review

The dependency-review action will provide details about which packages have blocked licenses. You'll need to:
1. Remove the offending package
2. Find an alternative with a permissive license
3. Or seek legal approval for an exception

## Resources

- [GitHub Dependency Review Action](https://github.com/actions/dependency-review-action)
- [NuGet Package Licenses](https://learn.microsoft.com/en-us/nuget/reference/nuspec#license)
- [SPDX License List](https://spdx.org/licenses/)
- [Choose a License](https://choosealicense.com/)
- [Open Source Licenses Comparison](https://opensource.org/licenses)

## Maintenance

Review and update the blocked license list in the workflow file periodically as:
- New licenses emerge
- Legal requirements change
- Package ecosystems evolve
