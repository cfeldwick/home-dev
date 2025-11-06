# Angular Samples

Frontend components and examples using Angular framework.

## Contents

### Components

#### Submit Dialog (`components/submit-dialog.ts`)
A dialog component for submitting risk run jobs with the following features:
- Job submission form with validation
- Material Design dialog integration
- TypeScript strongly-typed implementation
- Full unit test coverage (`submit-dialog.spec.ts`)

#### Parent Window (`components/parent-window.ts`)
Component for managing parent window communication and coordination.

### Docker Setup

**Dockerfile** - Containerized Angular application configuration for deployment.

## Usage

These components are designed for an intraday risk management system but can be adapted for other use cases requiring job submission and window management patterns.

### Running the Angular App

Build and run using Docker:
```bash
docker build -t angular-app .
docker run -p 4200:4200 angular-app
```

### Testing

Run unit tests:
```bash
npm test
```

## Dependencies

- Angular (check package.json for version)
- Angular Material (for dialog components)
- Jasmine/Karma (for testing)
