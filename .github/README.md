# GitHub Workflows

This directory contains GitHub Actions workflows for automating deployments and other tasks.

## Available Workflows

### Deploy Regex Tester to GitHub Pages

**File:** `workflows/deploy-regex-tester.yml`

Automatically deploys the regex-tester tool to GitHub Pages when changes are pushed to the main branch.

**Triggers:**
- Push to `main` branch (when `regex-tester/` files or the workflow itself changes)
- Manual trigger via workflow_dispatch

**Permissions Required:**
- `contents: read` - To checkout the repository
- `pages: write` - To deploy to GitHub Pages
- `id-token: write` - For GitHub Pages deployment authentication

**Setup:**
1. Enable GitHub Pages in repository Settings → Pages
2. Select "GitHub Actions" as the source
3. Workflow will run automatically on next push to main

**Deployment URL:**
Once configured, the site will be available at:
```
https://[your-username].github.io/home-dev/
```

## Adding New Workflows

When adding new workflows to this directory:

1. Create a new `.yml` file in `workflows/`
2. Define clear trigger conditions
3. Set appropriate permissions
4. Document the workflow in this README
5. Test the workflow before merging to main

## Workflow Best Practices

- Use specific action versions (e.g., `@v4`) for reproducibility
- Set minimal required permissions
- Add concurrency controls for deployment workflows
- Use path filters to avoid unnecessary runs
- Document any required repository settings or secrets
