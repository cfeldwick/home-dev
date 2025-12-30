# Home Dev - Code Samples & Snippets

A collection of code samples, snippets, and proof-of-concept implementations for reference across various projects.

## Purpose

This repository serves as a personal library of:
- Working code examples for different technologies
- Reusable snippets and patterns
- Quick prototypes and experiments
- Reference implementations

## Repository Structure

```
home-dev/
├── angular/              # Angular frontend samples
├── elastic/              # Elastic Stack & .NET logging samples
├── docker-compose/       # Docker orchestration examples
├── selenium/             # Selenium testing & automation
├── regex-tester/         # Browser-based regex testing tool
├── restaurant-checker/   # Restaurant availability monitor
├── razorlight/           # Miscellaneous utilities
└── docs/                 # Documentation & requirements
```

## Available Samples

### 🅰️ Angular Components
**Location:** `angular/`

- **Submit Dialog** - Job submission dialog component with TypeScript implementation and unit tests
- **Parent Window** - Parent window communication component
- **Dockerfile** - Containerized Angular application setup

**Technologies:** TypeScript, Angular, Jasmine (testing)

---

### 📊 Elastic Stack & .NET
**Location:** `elastic/`

- **.NET Web Application** - ASP.NET Core 9.0 application with Serilog structured logging
  - Elastic Common Schema (ECS) formatting
  - Console and file sinks
  - Production and development configurations
- **Fake Elastic Server** - FastAPI-based mock Elastic server for testing

**Technologies:** C#, .NET 9.0, Serilog, Python, FastAPI

---

### 🐳 Docker Compose
**Location:** `docker-compose/`

- **Service Orchestration** - Multi-container application setup
- **Certificate Management** - CA certificate initialization script

**Technologies:** Docker, Docker Compose, Bash

---

### 🧪 Selenium Testing
**Location:** `selenium/`

- **Jupyter Notebooks** - Interactive Selenium test examples and automation scripts

**Technologies:** Python, Selenium, Jupyter

---

### 🔍 Regex Tester
**Location:** `regex-tester/`

- **Browser-based Regex Tool** - Interactive regex pattern testing with real-time visual feedback
  - Dynamic table display with columns for each capture group
  - Named and numbered capture group support
  - Live regex validation with helpful tooltips
  - Syntax error highlighting and analysis
  - Regex flags support (g, i, m, s)
  - Modern, responsive UI with gradient design
  - No backend required - runs entirely in browser

**Technologies:** HTML5, CSS3, Vanilla JavaScript

---

### 🔧 Utilities
**Location:** `razorlight/`

- **Merge Utility** - File/data merging tools

---

### 🍽️ Restaurant Checker
**Location:** `restaurant-checker/`

- **TableAgent Availability Monitor** - Automated restaurant table availability checker
  - Selenium-based browser automation for tableagent.com
  - Multiple notification channels (Slack, Telegram, Pushover, Email, SMS)
  - GCP Cloud Run deployment with Cloud Scheduler
  - Configurable date, time, and party size
  - Docker support for containerized deployment

**Technologies:** Python, Selenium, Pydantic, Docker, GCP Cloud Run

---

## Usage

Browse to the relevant directory and examine the code samples. Each major section may contain its own README with additional context.

To use a sample:
1. Navigate to the appropriate directory
2. Review the code and any local documentation
3. Copy/adapt the code to your target project
4. Modify as needed for your specific use case

## Quick Reference

| Need | Location | Files |
|------|----------|-------|
| Angular dialog component | `angular/components/` | `submit-dialog.ts`, `submit-dialog.spec.ts` |
| .NET structured logging | `elastic/WebApplication/` | `Program.cs`, `appsettings.json` |
| Mock Elastic server | `elastic/FakeElasticServer/` | `main.py` |
| Docker multi-service setup | `docker-compose/` | `docker-compose.yml` |
| Selenium automation | `selenium/` | `*.ipynb` |
| Regex pattern testing | `regex-tester/` | `index.html` |
| Restaurant availability | `restaurant-checker/` | `main.py`, `checker.py` |

## Contributing

This is a personal reference repository. Feel free to add new samples in appropriate directories:
- Create technology-specific folders as needed
- Include brief comments explaining the purpose
- Add tests if the sample demonstrates testing patterns
- Update this README when adding new major samples

## Notes

- Samples may not be production-ready without modification
- Check dependencies and versions before using in production projects
- Some samples may be experiments or work-in-progress
