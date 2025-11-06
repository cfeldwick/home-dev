# Docker Compose Samples

Docker orchestration examples for multi-container applications.

## Contents

### docker-compose.yml

Multi-service application composition demonstrating:
- Service definitions and dependencies
- Network configuration
- Volume management
- Environment variables

### Certificate Management

**init-ca-certs.sh** - Script for initializing CA certificates in containerized environments.

Useful for:
- Setting up custom CA certificates
- Corporate proxy configurations
- Self-signed certificate scenarios

## Usage

Start all services:
```bash
docker-compose up -d
```

Stop services:
```bash
docker-compose down
```

View logs:
```bash
docker-compose logs -f
```

Initialize certificates:
```bash
./init-ca-certs.sh
```

## Notes

- Modify service configurations as needed for your environment
- Check volume paths before running
- Ensure required images are available or can be built
