#!/usr/bin/env node

/**
 * Helper script to obtain OAuth refresh tokens for personal Microsoft accounts
 *
 * Usage:
 *   node get-refresh-token.js <CLIENT_ID> <CLIENT_SECRET>
 *
 * This script:
 * 1. Opens a browser for the user to sign in with their Microsoft account
 * 2. Receives the authorization code via local callback server
 * 3. Exchanges the code for tokens
 * 4. Displays the refresh token (store this in Azure Key Vault)
 */

const http = require('http');
const url = require('url');
const { spawn } = require('child_process');

// Configuration
const REDIRECT_URI = 'http://localhost:8080/callback';
const TENANT_ID = 'common'; // 'common' works for personal Microsoft accounts
// Use the .default scope which includes all delegated permissions configured in the app
const SCOPES = 'https://graph.microsoft.com/.default offline_access';

// Parse command line arguments
const [,, clientId, clientSecret] = process.argv;

if (!clientId || !clientSecret) {
    console.error('Usage: node get-refresh-token.js <CLIENT_ID> <CLIENT_SECRET>');
    console.error('\nExample:');
    console.error('  node get-refresh-token.js abc123-456-789 MySecret123');
    process.exit(1);
}

// Build authorization URL
const authUrl = `https://login.microsoftonline.com/${TENANT_ID}/oauth2/v2.0/authorize?` +
    `client_id=${encodeURIComponent(clientId)}&` +
    `response_type=code&` +
    `redirect_uri=${encodeURIComponent(REDIRECT_URI)}&` +
    `scope=${encodeURIComponent(SCOPES)}&` +
    `response_mode=query`;

console.log('='.repeat(80));
console.log('Microsoft OAuth Refresh Token Helper');
console.log('='.repeat(80));
console.log('');
console.log('This script will help you obtain a refresh token for a Microsoft account.');
console.log('');
console.log('Steps:');
console.log('1. Your browser will open to Microsoft login');
console.log('2. Sign in with the account you want to authorize');
console.log('3. Grant permissions when prompted');
console.log('4. The refresh token will be displayed here');
console.log('');
console.log('Press ENTER to continue...');

// Wait for user to press ENTER
process.stdin.once('data', () => {
    console.log('\nOpening browser for authentication...\n');
    openBrowser(authUrl);
    startCallbackServer();
});

function openBrowser(url) {
    const platform = process.platform;
    let command;

    if (platform === 'darwin') {
        command = spawn('open', [url]);
    } else if (platform === 'win32') {
        command = spawn('cmd', ['/c', 'start', url]);
    } else {
        command = spawn('xdg-open', [url]);
    }

    command.on('error', (err) => {
        console.error('Failed to open browser automatically.');
        console.error('Please open this URL manually:');
        console.error(url);
    });
}

function startCallbackServer() {
    const server = http.createServer(async (req, res) => {
        const parsedUrl = url.parse(req.url, true);

        if (parsedUrl.pathname === '/callback') {
            const code = parsedUrl.query.code;
            const error = parsedUrl.query.error;

            if (error) {
                sendResponse(res, 400, `
                    <html>
                        <body style="font-family: Arial, sans-serif; padding: 40px;">
                            <h1 style="color: #d32f2f;">❌ Authentication Failed</h1>
                            <p>Error: ${error}</p>
                            <p>${parsedUrl.query.error_description || ''}</p>
                            <p>You can close this window.</p>
                        </body>
                    </html>
                `);
                console.error(`\n❌ Authentication failed: ${error}`);
                console.error(parsedUrl.query.error_description || '');
                server.close();
                process.exit(1);
                return;
            }

            if (!code) {
                sendResponse(res, 400, `
                    <html>
                        <body style="font-family: Arial, sans-serif; padding: 40px;">
                            <h1 style="color: #d32f2f;">❌ Error</h1>
                            <p>No authorization code received.</p>
                            <p>You can close this window.</p>
                        </body>
                    </html>
                `);
                console.error('\n❌ No authorization code received');
                server.close();
                process.exit(1);
                return;
            }

            console.log('✓ Authorization code received!');
            console.log('✓ Exchanging code for tokens...\n');

            try {
                const tokens = await exchangeCodeForTokens(code);

                sendResponse(res, 200, `
                    <html>
                        <body style="font-family: Arial, sans-serif; padding: 40px; background: #f5f5f5;">
                            <div style="background: white; padding: 30px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1);">
                                <h1 style="color: #4caf50;">✅ Success!</h1>
                                <p>Refresh token has been generated successfully.</p>
                                <p>Check your terminal for the token.</p>
                                <p style="margin-top: 30px; color: #666;">You can close this window now.</p>
                            </div>
                        </body>
                    </html>
                `);

                console.log('='.repeat(80));
                console.log('SUCCESS! Tokens retrieved:');
                console.log('='.repeat(80));
                console.log('');
                console.log('REFRESH TOKEN (save this securely in Azure Key Vault):');
                console.log('-'.repeat(80));
                console.log(tokens.refresh_token);
                console.log('-'.repeat(80));
                console.log('');
                console.log('ACCESS TOKEN (valid for ~1 hour, for testing only):');
                console.log(tokens.access_token.substring(0, 50) + '...');
                console.log('');
                console.log('Expires in:', tokens.expires_in, 'seconds');
                console.log('');
                console.log('='.repeat(80));
                console.log('Next steps:');
                console.log('1. Store the refresh token in Azure Key Vault:');
                console.log('');
                console.log('   az keyvault secret set \\');
                console.log('     --vault-name YOUR_VAULT_NAME \\');
                console.log('     --name source1-refresh-token \\');
                console.log('     --value "' + tokens.refresh_token + '"');
                console.log('');
                console.log('2. Configure your Function App to use refresh token auth');
                console.log('3. See PERSONAL_ACCOUNTS_SETUP.md for full instructions');
                console.log('='.repeat(80));

                setTimeout(() => {
                    server.close();
                    process.exit(0);
                }, 2000);
            } catch (error) {
                sendResponse(res, 500, `
                    <html>
                        <body style="font-family: Arial, sans-serif; padding: 40px;">
                            <h1 style="color: #d32f2f;">❌ Token Exchange Failed</h1>
                            <p>${error.message}</p>
                            <p>Check the terminal for more details.</p>
                            <p>You can close this window.</p>
                        </body>
                    </html>
                `);
                console.error('\n❌ Failed to exchange code for tokens:');
                console.error(error.message);
                console.error(error.stack);
                server.close();
                process.exit(1);
            }
        } else {
            sendResponse(res, 404, '<html><body><h1>404 Not Found</h1></body></html>');
        }
    });

    server.listen(8080, () => {
        console.log('✓ Local callback server started on http://localhost:8080');
        console.log('✓ Waiting for authentication...\n');
    });

    server.on('error', (err) => {
        if (err.code === 'EADDRINUSE') {
            console.error('\n❌ Port 8080 is already in use.');
            console.error('Please close any other applications using this port and try again.');
        } else {
            console.error('\n❌ Server error:', err.message);
        }
        process.exit(1);
    });
}

async function exchangeCodeForTokens(code) {
    const tokenUrl = `https://login.microsoftonline.com/${TENANT_ID}/oauth2/v2.0/token`;

    const params = new URLSearchParams();
    params.append('client_id', clientId);
    params.append('client_secret', clientSecret);
    params.append('code', code);
    params.append('redirect_uri', REDIRECT_URI);
    params.append('grant_type', 'authorization_code');
    params.append('scope', SCOPES);

    const response = await fetch(tokenUrl, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/x-www-form-urlencoded',
        },
        body: params.toString(),
    });

    if (!response.ok) {
        const errorData = await response.json();
        throw new Error(`Token exchange failed: ${errorData.error} - ${errorData.error_description}`);
    }

    return await response.json();
}

function sendResponse(res, statusCode, html) {
    res.writeHead(statusCode, { 'Content-Type': 'text/html' });
    res.end(html);
}
