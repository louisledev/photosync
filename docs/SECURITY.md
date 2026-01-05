# Security Policy

## Automated Security Scanning

PhotoSync uses multiple automated security scanning tools to ensure code quality and security:

### 🔍 Active Security Measures

#### 1. **CodeQL Analysis** (GitHub Advanced Security)
- **What**: Static code analysis to find security vulnerabilities
- **Detects**: SQL injection, XSS, command injection, path traversal, etc.
- **Runs**: Automatically by GitHub Advanced Security
- **Results**: Available in the [Security tab](../../security/code-scanning)
- **Note**: Managed by GitHub, not in workflow files

#### 2. **Dependency Review** ([security.yml](.github/workflows/security.yml))
- **What**: Reviews dependencies for vulnerabilities and incompatible licenses
- **Detects**: Vulnerable or incompatible licenses (GPL, AGPL, LGPL, MPL, etc.)
- **Runs**: On every pull request (requires base/head comparison)
- **Action**: PR blocked if moderate+ severity vulnerabilities or copyleft licenses found

#### 3. **NuGet Vulnerability Scan** ([security.yml](.github/workflows/security.yml))
- **What**: Scans NuGet packages for known vulnerabilities
- **Detects**: CVEs in dependencies (direct and transitive)
- **Runs**: Every push, PR, weekly schedule, and manual dispatch
- **Action**: Build fails if vulnerabilities found, uploads report artifact

#### 4. **Infrastructure as Code Scanning** ([security.yml](.github/workflows/security.yml))
- **What**: Microsoft Security DevOps scans Terraform code
- **Detects**: Security misconfigurations, best practice violations
- **Runs**: Every push, PR, weekly schedule, and manual dispatch
- **Tools**: Checkov, Terrascan, Template Analyzer
- **Results**: Available in the [Security tab](../../security/code-scanning)

#### 5. **Secret Scanning** (GitHub Advanced Security)
- **What**: Detects leaked secrets in code
- **Detects**: API keys, passwords, tokens
- **Runs**: Automatically by GitHub Advanced Security
- **Action**: Alerts sent to repository maintainers
- **Note**: Managed by GitHub, not in workflow files

#### 6. **Dependabot** ([dependabot.yml](.github/dependabot.yml))
- **What**: Automated dependency updates
- **Runs**: Weekly
- **Action**: Creates PRs for security updates automatically
- **Scope**: NuGet packages, GitHub Actions, npm packages

### 🛡️ Azure Security

#### Runtime Security (Automated via Terraform)
- **Log Analytics Workspace**: Centralized logging for all resources
- **Diagnostic Settings**: Automatic logging for Function Apps and Key Vault
- **Security Alerts**: Automated alerts for failures and anomalies
- **Managed Identity**: Function Apps access Key Vault without passwords
- **Key Vault**: Stores all secrets (refresh tokens, client secrets)

#### Configured Automatically
All security monitoring is configured automatically when you run `terraform apply`:
- ✅ Log Analytics workspace creation
- ✅ Diagnostic settings for Function Apps
- ✅ Diagnostic settings for Key Vault (audit logs)
- ✅ Alert rules for HTTP 5xx errors
- ✅ Alert rules for Key Vault access failures
- ✅ Alert rules for unusual file processing activity

No manual setup required!

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

| Tool | Purpose | Where Configured |
|------|---------|------------------|
| **CodeQL** | Code security analysis | GitHub Advanced Security (automatic) |
| **Dependabot** | Automated dependency updates | [dependabot.yml](.github/dependabot.yml) |
| **Dependency Review** | License and vulnerability check for PRs | [security.yml](.github/workflows/security.yml) |
| **NuGet Scanner** | .NET-specific vulnerability detection | [security.yml](.github/workflows/security.yml) |
| **Microsoft Security DevOps** | IaC scanning (Terraform) | [security.yml](.github/workflows/security.yml) |
| **Secret Scanning** | Leaked credential detection | GitHub Advanced Security (automatic) |
| **Microsoft Defender** | Cloud security posture | [Azure Portal](https://azure.microsoft.com/en-us/products/defender-for-cloud/) |

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
- [License Compliance Documentation](license-compliance.md)
