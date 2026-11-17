import argparse
import json
import os
import secrets
import urllib.error
import urllib.parse
import urllib.request
from pathlib import Path

parser = argparse.ArgumentParser(description="Register a public Builder Projects OAuth client for an exact Blackbird callback.")
parser.add_argument("--redirect-uri", required=True)
parser.add_argument("--output", required=True, help="New private file for client credentials; existing files are never overwritten")
args = parser.parse_args()
uri = urllib.parse.urlparse(args.redirect_uri)
if uri.scheme != "https" or not uri.hostname or uri.fragment or uri.username or uri.password:
    parser.error("Use the exact HTTPS redirect URI supplied by your Blackbird administrator, without fragments or embedded credentials.")
output = Path(args.output).resolve()
if output.exists():
    parser.error("Output file already exists. Choose a new filename.")
payload = {
    "client_name": "Blackbird Builder.io Projects",
    "redirect_uris": [args.redirect_uri],
    "grant_types": ["authorization_code", "refresh_token"],
    "response_types": ["code"],
    "token_endpoint_auth_method": "none",
    "scope": "builder:projects:read builder:agents:run builder:ai:invoke offline_access"
}
request = urllib.request.Request("https://mcp.builder.io/oauth/register", data=json.dumps(payload).encode(), headers={"Content-Type": "application/json"}, method="POST")
try:
    with urllib.request.urlopen(request, timeout=60) as response:
        registered = json.load(response)
except urllib.error.HTTPError as error:
    parser.exit(1, f"Builder registration failed (HTTP {error.code}). Ask the Builder administrator to verify registration access.\n")
except (urllib.error.URLError, TimeoutError, ValueError):
    parser.exit(1, "Builder registration failed or returned unreadable data. A registration may already exist; check with your administrator before retrying.\n")
registered["pkce_signing_key"] = secrets.token_urlsafe(32)
with os.fdopen(os.open(output, os.O_WRONLY | os.O_CREAT | os.O_EXCL, 0o600), "w") as target:
    json.dump(registered, target, indent=2)
    target.write("\n")
if not registered.get("client_id") or registered.get("token_endpoint_auth_method") != "none":
    parser.exit(1, "Builder did not issue a compatible public-client registration. The response was saved privately; this app requires token_endpoint_auth_method=none.\n")
print("Registration saved privately. Enter client_id and pkce_signing_key into the Blackbird Projects (OAuth) connection. Do not commit or share the file.")
