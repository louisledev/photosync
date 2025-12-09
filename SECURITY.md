# Security Policy

## Automated Security Scanning

PhotoSync uses multiple automated security scanning tools to ensure code quality and security:

### 🔍 Active Security Measures

#### 1. **CodeQL Analysis** ([codeql.yml](.github/workflows/codeql.yml))
- **What**: Static code analysis to find security vulnerabilities
- **Detects**: SQL injection, XSS, command injection, path traversal, etc.
- **Runs**: Every push, PR, and weekly on Mondays at 6 AM UTC
- **Results**: Available in the [Security tab](../../security/code-scanning)

#### 2. **Dependency Scanning** ([codeql.yml](.github/workflows/codeql.yml))
- **What**: Scans NuGet packages for known vulnerabilities
- **Detects**: CVEs in dependencies (direct and transitive)
- **Runs**: Every push and PR
- **Action**: Build fails if vulnerabilities found

#### 3. **Dependency Review** ([codeql.yml](.github/workflows/codeql.yml))
- **What**: Reviews new dependencies in pull requests
- **Detects**: Vulnerable or incompatible licenses (GPL-2.0, GPL-3.0)
- **Runs**: On every pull request
- **Action**: PR blocked if moderate+ severity vulnerabilities found

#### 4. **Secret Scanning** ([codeql.yml](.github/workflows/codeql.yml))
- **What**: Uses TruffleHog to detect leaked secrets
- **Detects**: API keys, passwords, tokens in code
- **Runs**: Every push and PR
- **Action**: Build fails if verified secrets found

#### 5. **Dependabot** ([dependabot.yml](.github/dependabot.yml))
- **What**: Automated dependency updates
- **Runs**: Weekly
- **Action**: Creates PRs for security updates automatically
- **Scope**: NuGet packages, GitHub Actions, npm packages

#### 6. **Security Best Practices Check** ([security-checklist.yml](.github/workflows/security-checklist.yml))
- **What**: Validates security configurations
- **Checks**: Hardcoded secrets, gitignore coverage, permissions
- **Runs**: Every pull request

### 🛡️ Azure Security

#### Runtime Security
- **Microsoft Defender for Cloud**: Monitors deployed resources
- **Managed Identity**: Function Apps access Key Vault without passwords
- **Key Vault**: Stores all secrets (refresh tokens, client secrets)
- **Diagnostic Logging**: Audit logs for all security events

#### Enable Azure Security
Run the security setup script once:
```bash
./scripts/enable-azure-security.sh
```

This configures:
- Microsoft Defender for Cloud (free tier)
- Diagnostic settings for Function Apps and Key Vault
- Security alerts for Function App failures

## Reporting a Vulnerability

If you discover a security vulnerability, please:

1. **Do NOT** open a public GitHub issue
2. Email the details to the repository owner
3. Include:
   - Description of the vulnerability
   - Steps to reproduce
   - Potential impact
   - Suggested fix (if any)

We will respond within 48 hours and work on a fix.

## Security Update Process

1. **Automated**: Dependabot creates PRs for security updates
2. **Review**: Security team reviews and approves
3. **Deploy**: Changes deployed via CI/CD pipeline
4. **Verify**: Post-deployment security verification

## Security Best Practices

### For Contributors

- ✅ Never commit secrets (API keys, passwords, tokens)
- ✅ Use Key Vault references for sensitive configuration
- ✅ Keep dependencies up to date
- ✅ Follow principle of least privilege
- ✅ Enable 2FA on GitHub account
- ✅ Review security alerts promptly

### For Operators

- ✅ Rotate secrets every 90 days
- ✅ Review Azure Security Center recommendations
- ✅ Monitor Application Insights for anomalies
- ✅ Keep Function App runtime updated
- ✅ Review Key Vault access logs monthly

## Security Tools in Use

| Tool | Purpose | Documentation |
|------|---------|--------------|
| **CodeQL** | Code security analysis | [GitHub CodeQL](https://codeql.github.com/) |
| **Dependabot** | Automated dependency updates | [Dependabot](https://docs.github.com/en/code-security/dependabot) |
| **TruffleHog** | Secret detection | [TruffleHog](https://github.com/trufflesecurity/trufflehog) |
| **Dependency Review** | License and vulnerability check | [GitHub Dependency Review](https://docs.github.com/en/code-security/supply-chain-security/understanding-your-software-supply-chain/about-dependency-review) |
| **Microsoft Defender** | Cloud security posture | [Defender for Cloud](https://azure.microsoft.com/en-us/products/defender-for-cloud/) |

## Compliance

- **OWASP Top 10**: Covered by CodeQL analysis
- **CWE Top 25**: Scanned by CodeQL
- **CVE Database**: Checked by Dependabot and NuGet scanner
- **Least Privilege**: Enforced via Managed Identity and Key Vault

## Security Metrics

View security metrics in:
- **GitHub Security tab**: Code scanning, Dependabot, Secret scanning alerts
- **Azure Security Center**: Runtime security posture
- **Application Insights**: Security-related telemetry

## Questions?

For security-related questions, see:
- [GitHub Security Documentation](https://docs.github.com/en/code-security)
- [Azure Security Best Practices](https://docs.microsoft.com/en-us/azure/security/fundamentals/best-practices-and-patterns)
