#!/bin/bash
# Script to generate Python gRPC code from .proto files

echo "Generating Python gRPC code from proto files..."

# Generate Python code from the proto file
python -m grpc_tools.protoc \
    -I../GrpcService/Protos \
    --python_out=. \
    --grpc_python_out=. \
    ../GrpcService/Protos/auth.proto

echo "✓ Generated auth_pb2.py and auth_pb2_grpc.py"
echo "Done!"
