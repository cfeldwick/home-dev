#!/usr/bin/env python3
"""
gRPC Client demonstrating HeaderToTrailerMiddleware functionality.

This client intentionally makes an unauthenticated call to demonstrate that
custom headers set by the authentication handler are accessible in the
gRPC trailing metadata thanks to HeaderToTrailerMiddleware.

Without HeaderToTrailerMiddleware:
- Headers set in Response.Headers are lost for gRPC clients
- Clients cannot see authentication challenge details

With HeaderToTrailerMiddleware:
- Headers are copied to gRPC trailers
- Clients can access headers like 'www-authenticate' and custom headers
"""

import grpc
import auth_pb2
import auth_pb2_grpc
import sys


def make_unauthenticated_call():
    """
    Make an unauthenticated gRPC call and display the trailing metadata.
    This demonstrates that custom headers are accessible in trailers.
    """
    print("=" * 70)
    print("Testing UNAUTHENTICATED call (expecting failure)")
    print("=" * 70)

    # Create a channel to the gRPC server
    channel = grpc.insecure_channel('localhost:5000')
    stub = auth_pb2_grpc.AuthServiceStub(channel)

    try:
        # Make a call without authentication
        request = auth_pb2.GetUserInfoRequest(user_id="123")
        response = stub.GetUserInfo(request)

        print(f"✓ Unexpected success: {response}")

    except grpc.RpcError as e:
        print(f"\n✓ Expected failure occurred!")
        print(f"  Status Code: {e.code()}")
        print(f"  Status Message: {e.details()}")

        # This is the key part - accessing trailing metadata
        print(f"\n📋 Trailing Metadata (headers copied by middleware):")
        print("-" * 70)

        trailing_metadata = e.trailing_metadata()

        if trailing_metadata:
            for key, value in trailing_metadata:
                print(f"  {key}: {value}")

            # Highlight specific headers
            www_auth = dict(trailing_metadata).get('www-authenticate')
            custom_test = dict(trailing_metadata).get('x-custom-test')

            print("\n🎯 Key Headers:")
            print("-" * 70)
            if www_auth:
                print(f"  ✓ www-authenticate: {www_auth}")
            else:
                print(f"  ✗ www-authenticate: NOT FOUND")

            if custom_test:
                print(f"  ✓ x-custom-test: {custom_test}")
            else:
                print(f"  ✗ x-custom-test: NOT FOUND")

        else:
            print("  ⚠️  No trailing metadata found!")
            print("  This means HeaderToTrailerMiddleware may not be configured correctly.")

    finally:
        channel.close()


def make_authenticated_call():
    """
    Make an authenticated gRPC call to show successful authentication.
    """
    print("\n" + "=" * 70)
    print("Testing AUTHENTICATED call (expecting success)")
    print("=" * 70)

    channel = grpc.insecure_channel('localhost:5000')
    stub = auth_pb2_grpc.AuthServiceStub(channel)

    try:
        # Make a call with valid authentication token
        metadata = [('authorization', 'Bearer valid-token-12345')]
        request = auth_pb2.GetUserInfoRequest(user_id="123")

        response = stub.GetUserInfo(request, metadata=metadata)

        print(f"\n✓ Success!")
        print(f"  User ID: {response.user_id}")
        print(f"  Username: {response.username}")
        print(f"  Email: {response.email}")

    except grpc.RpcError as e:
        print(f"✗ Unexpected failure: {e.code()} - {e.details()}")

    finally:
        channel.close()


def main():
    """Main entry point."""
    print("\n" + "=" * 70)
    print("gRPC HeaderToTrailerMiddleware Demo Client")
    print("=" * 70)

    # First, demonstrate unauthenticated call showing headers in trailers
    make_unauthenticated_call()

    # Then, demonstrate successful authenticated call
    make_authenticated_call()

    print("\n" + "=" * 70)
    print("Demo Complete")
    print("=" * 70)
    print("\nKey Takeaways:")
    print("1. The unauthenticated call fails with UNAUTHENTICATED status")
    print("2. Custom headers set by the auth handler appear in trailing_metadata")
    print("3. This is only possible because HeaderToTrailerMiddleware copies them")
    print("4. Without the middleware, these headers would be lost to gRPC clients")
    print("=" * 70 + "\n")


if __name__ == "__main__":
    try:
        main()
    except KeyboardInterrupt:
        print("\n\nInterrupted by user")
        sys.exit(0)
    except Exception as e:
        print(f"\n✗ Error: {e}")
        sys.exit(1)
