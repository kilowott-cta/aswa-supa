# MCP Endpoint Authentication Guide

## Overview
The MCP health check endpoint uses API key authentication via HTTP headers. This approach is secure and works with various tools including n8n, Postman, curl, and your Blazor WASM client.

## Setup

### 1. Configure API Key
Add the `MCP_API_KEY` to your environment variables:

**Local Development (local.settings.json):**
```json
{
  "Values": {
    "MCP_API_KEY": "mcp_sk_live_1a2b3c4d5e6f7g8h9i0j"
  }
}
```

**Azure Portal (Production):**
1. Go to your Function App
2. Settings → Configuration
3. Add new application setting:
   - Name: `MCP_API_KEY`
   - Value: `your-secure-api-key-here`

**Generate a secure API key:**
```bash
# Using openssl
openssl rand -base64 32

# Using Node.js
node -e "console.log(require('crypto').randomBytes(32).toString('base64'))"
```

## Authentication Methods

The endpoint accepts API keys in two ways:

### Option 1: X-API-Key Header (Recommended)
```bash
X-API-Key: mcp_sk_live_1a2b3c4d5e6f7g8h9i0j
```

### Option 2: Authorization Bearer Header
```bash
Authorization: Bearer mcp_sk_live_1a2b3c4d5e6f7g8h9i0j
```

## Testing Examples

### Using curl
```bash
# With X-API-Key header
curl -X POST http://localhost:7071/api/mcp-health-check \
  -H "X-API-Key: mcp_sk_live_1a2b3c4d5e6f7g8h9i0j" \
  -H "Content-Type: application/json" \
  -d '{}'

# With Authorization Bearer header
curl -X POST http://localhost:7071/api/mcp-health-check \
  -H "Authorization: Bearer mcp_sk_live_1a2b3c4d5e6f7g8h9i0j" \
  -H "Content-Type: application/json" \
  -d '{}'
```

### Using Blazor WASM Client
```csharp
var client = HttpClientFactory.CreateClient("ApiClient");
client.DefaultRequestHeaders.Add("X-API-Key", "mcp_sk_live_1a2b3c4d5e6f7g8h9i0j");

var requestBody = new { };
var response = await client.PostAsJsonAsync("/api/mcp-health-check", requestBody);
```

### Using n8n

**HTTP Request Node Configuration:**
- **Method:** POST
- **URL:** `https://your-function-app.azurewebsites.net/api/mcp-health-check`
- **Authentication:** None (we'll use headers)
- **Headers:**
  - Name: `X-API-Key`
  - Value: `mcp_sk_live_1a2b3c4d5e6f7g8h9i0j`
  - Name: `Content-Type`
  - Value: `application/json`
- **Body:** 
  ```json
  {}
  ```

### Using Postman

1. Create new POST request
2. URL: `http://localhost:7071/api/mcp-health-check`
3. Headers tab:
   - Key: `X-API-Key`
   - Value: `mcp_sk_live_1a2b3c4d5e6f7g8h9i0j`
   - Key: `Content-Type`
   - Value: `application/json`
4. Body tab:
   - Select `raw` and `JSON`
   - Enter: `{}`
5. Send

### Using JavaScript/Fetch
```javascript
fetch('http://localhost:7071/api/mcp-health-check', {
  method: 'POST',
  headers: {
    'X-API-Key': 'mcp_sk_live_1a2b3c4d5e6f7g8h9i0j',
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({})
})
.then(res => res.json())
.then(data => console.log(data));
```

## Response Format

### Success Response
```json
{
  "content": [
    {
      "type": "text",
      "text": "{\n  \"success\": true,\n  \"authenticated\": true,\n  \"service\": \"Azure Functions MCP Endpoint\",\n  \"supabase\": {\n    \"connected\": true,\n    \"url\": \"https://widdstpscvgiqghlowar.supabase.co\"\n  },\n  \"timestamp\": \"2026-01-26T12:34:56.789Z\",\n  \"message\": \"✅ MCP endpoint is healthy and authenticated\"\n}"
    }
  ]
}
```

### Error Responses

**Missing API Key (401):**
```json
{
  "content": [
    {
      "type": "text",
      "text": "{\"success\": false, \"error\": \"API key required. Provide via X-API-Key header or Authorization: Bearer header\"}"
    }
  ],
  "isError": true
}
```

**Invalid API Key (401):**
```json
{
  "content": [
    {
      "type": "text",
      "text": "{\"success\": false, \"error\": \"Invalid API key\"}"
    }
  ],
  "isError": true
}
```

## Security Best Practices

1. **Never commit API keys to source control**
   - Use environment variables
   - Add `local.settings.json` to `.gitignore`

2. **Use strong, random API keys**
   - Minimum 32 characters
   - Use cryptographically secure random generation

3. **Rotate API keys regularly**
   - Update in Azure Portal configuration
   - Update in all client applications

4. **Use HTTPS in production**
   - API keys should never be sent over unencrypted connections

5. **Monitor API usage**
   - Log authentication attempts
   - Alert on repeated failures

6. **Consider rate limiting**
   - Add throttling for API key-based requests
   - Implement exponential backoff

## Extending to Other Endpoints

To add API key authentication to other MCP endpoints, call the `ValidateApiKey` method:

```csharp
[Function("my-mcp-endpoint")]
public async Task<IActionResult> MyMcpEndpoint(
    [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req)
{
    // Validate API key
    if (!ValidateApiKey(req, out var errorResponse))
    {
        return errorResponse;
    }
    
    // Your endpoint logic here
}
```
