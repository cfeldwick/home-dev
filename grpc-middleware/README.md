# gRPC HeaderToTrailerMiddleware Example

A complete working example demonstrating how to copy HTTP response headers to gRPC trailers in ASP.NET Core, making authentication headers accessible to gRPC clients.

## The Problem

In ASP.NET Core, authentication handlers often set response headers (like `www-authenticate` or custom headers) in `HttpContext.Response.Headers`. However, for gRPC clients, these headers are **not accessible** because:

1. gRPC uses HTTP/2 trailers for metadata after the response
2. HTTP response headers set by middleware are not automatically copied to gRPC trailers
3. Without middleware intervention, gRPC clients cannot see these important headers

This is particularly problematic for authentication scenarios where clients need to know:
- Authentication schemes (`www-authenticate` header)
- Challenge details
- Custom authentication state

## The Solution

The `HeaderToTrailerMiddleware` solves this by:
1. Intercepting gRPC requests
2. Hooking into the response pipeline with `Response.OnStarting()`
3. Copying specified headers from `Response.Headers` to `Response.AppendTrailer()`
4. Making headers accessible in the gRPC client's `trailing_metadata`

## Project Structure

```
grpc-middleware/
├── GrpcService/              # ASP.NET Core gRPC server (.NET 8)
│   ├── Protos/
│   │   └── auth.proto        # gRPC service definition with JSON transcoding
│   ├── Services/
│   │   └── AuthServiceImpl.cs                # gRPC service implementation
│   ├── CustomAuthenticationHandler.cs        # Auth handler that sets headers
│   ├── HeaderToTrailerMiddleware.cs          # The key middleware solution
│   ├── Program.cs                            # Service configuration
│   └── GrpcService.csproj
│
├── client/                   # Python gRPC client
│   ├── client.py             # Demo client showing header access
│   ├── generate_protos.sh    # Script to generate Python gRPC code
│   └── requirements.txt
│
└── README.md
```

## Prerequisites

- **.NET 8 SDK** - [Download here](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Python 3.8+** - [Download here](https://www.python.org/downloads/)

## Quick Start

### 1. Start the gRPC Server

```bash
cd grpc-middleware/GrpcService
dotnet restore
dotnet run
```

The server will start on `http://localhost:5000`. You should see:
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
```

### 2. Set Up Python Client

Open a new terminal:

```bash
cd grpc-middleware/client

# Create a virtual environment (recommended)
python -m venv venv
source venv/bin/activate  # On Windows: venv\Scripts\activate

# Install dependencies
pip install -r requirements.txt

# Generate Python gRPC code from proto files
./generate_protos.sh  # On Windows: bash generate_protos.sh or use WSL
```

### 3. Run the Demo Client

```bash
python client.py
```

## Expected Output

The client demonstrates two scenarios:

### Scenario 1: Unauthenticated Call

The client makes a call without authentication and catches the `UNAUTHENTICATED` error:

```
======================================================================
Testing UNAUTHENTICATED call (expecting failure)
======================================================================

✓ Expected failure occurred!
  Status Code: StatusCode.UNAUTHENTICATED
  Status Message: Missing Authorization header

📋 Trailing Metadata (headers copied by middleware):
----------------------------------------------------------------------
  www-authenticate: Bearer realm="GrpcService"
  x-custom-test: authentication-failed

🎯 Key Headers:
----------------------------------------------------------------------
  ✓ www-authenticate: Bearer realm="GrpcService"
  ✓ x-custom-test: authentication-failed
```

**Key Point:** Without `HeaderToTrailerMiddleware`, these headers would be **lost**!

### Scenario 2: Authenticated Call

The client makes a successful call with a valid token:

```
======================================================================
Testing AUTHENTICATED call (expecting success)
======================================================================

✓ Success!
  User ID: 123
  Username: testuser
  Email: testuser@example.com
```

## How It Works

### 1. Authentication Handler Sets Headers

`CustomAuthenticationHandler.cs` sets headers in `Response.Headers`:

```csharp
Response.Headers["x-custom-test"] = "authentication-failed";
Response.Headers["www-authenticate"] = "Bearer realm=\"GrpcService\"";
```

### 2. Middleware Copies Headers to Trailers

`HeaderToTrailerMiddleware.cs` uses `Response.OnStarting()` to copy headers:

```csharp
context.Response.OnStarting(() =>
{
    foreach (var headerName in _headersToCapture)
    {
        if (context.Response.Headers.TryGetValue(headerName, out var headerValue))
        {
            context.Response.AppendTrailer(headerName, headerValue.ToString());
        }
    }
    return Task.CompletedTask;
});
```

### 3. Correct Middleware Order

The middleware must be positioned correctly in `Program.cs`:

```csharp
app.UseAuthentication();          // 1. Sets headers
app.UseHeaderToTrailer(...);      // 2. Copies headers to trailers
app.UseAuthorization();           // 3. Enforces authorization
```

### 4. Client Accesses Trailing Metadata

Python clients can access headers from `trailing_metadata`:

```python
except grpc.RpcError as e:
    trailing_metadata = e.trailing_metadata()
    www_auth = dict(trailing_metadata).get('www-authenticate')
```

## Key Features Demonstrated

1. ✅ **Custom Authentication Handler** - Sets custom headers in responses
2. ✅ **HeaderToTrailerMiddleware** - Copies headers to gRPC trailers
3. ✅ **Proper Middleware Ordering** - Authentication → Middleware → Authorization
4. ✅ **gRPC with Authentication** - `[Authorize]` attribute on services
5. ✅ **JSON Transcoding** - gRPC service accessible via HTTP/JSON
6. ✅ **Python gRPC Client** - Demonstrates header access via `trailing_metadata`
7. ✅ **gRPC Reflection** - Enables tools like `grpcurl` for testing

## Testing with grpcurl

If you have [grpcurl](https://github.com/fullstorydev/grpcurl) installed:

```bash
# List services
grpcurl -plaintext localhost:5000 list

# Call without auth (will fail, but shows trailers)
grpcurl -plaintext -v \
    localhost:5000 \
    auth.AuthService/GetUserInfo \
    -d '{"user_id": "123"}'

# Call with auth (will succeed)
grpcurl -plaintext \
    -H "Authorization: Bearer valid-token-12345" \
    localhost:5000 \
    auth.AuthService/GetUserInfo \
    -d '{"user_id": "123"}'
```

## Configuration Options

### Customizing Headers to Capture

In `Program.cs`, specify which headers to copy:

```csharp
app.UseHeaderToTrailer("www-authenticate", "x-custom-test", "x-another-header");
```

### Changing Authentication Tokens

Valid tokens must start with `Bearer valid-` (see `CustomAuthenticationHandler.cs`):

```python
metadata = [('authorization', 'Bearer valid-anything')]
```

## Real-World Use Cases

This pattern is useful when:

1. **Authentication Headers** - Making `www-authenticate` accessible to clients
2. **Rate Limiting** - Exposing `X-RateLimit-*` headers in trailers
3. **Custom Metadata** - Passing custom application headers to clients
4. **Debugging** - Exposing correlation IDs or trace IDs
5. **API Versioning** - Communicating version information

## Technical Notes

- **HTTP/2 Only** - gRPC requires HTTP/2; the server is configured accordingly
- **.NET 8** - Uses minimal hosting model and latest gRPC packages
- **OnStarting Hook** - Headers must be copied before response starts
- **Case Insensitive** - Header names are matched case-insensitively
- **gRPC Only** - Middleware only processes `application/grpc` requests

## Troubleshooting

### Headers not appearing in trailers?

1. Check middleware order - must be after `UseAuthentication()`
2. Verify headers are being set by the auth handler (add logging)
3. Ensure the header names match exactly (case-insensitive)
4. Confirm the request is using gRPC protocol (not HTTP/JSON transcoding)

### Server won't start?

- Ensure .NET 8 SDK is installed: `dotnet --version`
- Check port 5000 is not in use: `netstat -an | grep 5000`
- Try restoring packages: `dotnet restore`

### Python client fails?

- Ensure server is running first
- Verify proto generation succeeded: check for `*_pb2.py` files
- Check Python version: `python --version` (needs 3.8+)

## Further Reading

- [gRPC for .NET](https://learn.microsoft.com/en-us/aspnet/core/grpc/)
- [gRPC Authentication](https://learn.microsoft.com/en-us/aspnet/core/grpc/authn-and-authz)
- [gRPC Python](https://grpc.io/docs/languages/python/)
- [HTTP/2 Trailers](https://datatracker.ietf.org/doc/html/rfc7540#section-8.1)

## License

This is a sample/example project for educational purposes.
