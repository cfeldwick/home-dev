# Restaurant Availability Checker

Monitors restaurant availability on [tableagent.com](https://tableagent.com) and sends notifications when tables become available.

Built with Selenium for browser automation and supports multiple notification channels.

## Features

- Automated checking of multiple restaurants
- Configurable date, time, and party size
- Multiple notification options:
  - Console output (default)
  - Email (SMTP)
  - Slack webhook
  - Telegram bot
  - Pushover (iOS/Android push notifications)
  - Twilio SMS
- GCP Cloud Run deployment support
- Scheduled checks with Cloud Scheduler

## Quick Start

### Local Development

1. **Install dependencies:**
   ```bash
   cd restaurant-checker
   python -m venv venv
   source venv/bin/activate  # On Windows: venv\Scripts\activate
   pip install -r requirements.txt
   ```

2. **Configure settings:**
   ```bash
   cp .env.example .env
   # Edit .env with your settings
   ```

3. **Run a single check:**
   ```bash
   python main.py --once
   ```

4. **Run continuous checking:**
   ```bash
   python main.py
   ```

### Command Line Options

```bash
python main.py --help

Options:
  --once          Check once and exit
  --test-notify   Send a test notification
  --date DATE     Target date (YYYY-MM-DD)
  --time TIME     Target time (HH:MM)
  --party SIZE    Party size
  --interval SEC  Check interval in seconds
  --debug         Enable debug logging
```

## Notification Setup

### Console (Default)
No configuration needed. Alerts print to stdout.

### Slack
1. Create a Slack app and webhook: https://api.slack.com/messaging/webhooks
2. Set `NOTIFICATION_TYPE=slack`
3. Set `SLACK_WEBHOOK_URL=https://hooks.slack.com/services/...`

### Telegram
1. Create a bot with [@BotFather](https://t.me/botfather)
2. Get your chat ID by messaging the bot and visiting:
   `https://api.telegram.org/bot<TOKEN>/getUpdates`
3. Set `NOTIFICATION_TYPE=telegram`
4. Set `TELEGRAM_BOT_TOKEN` and `TELEGRAM_CHAT_ID`

### Pushover
1. Sign up at https://pushover.net/
2. Create an application to get an API token
3. Set `NOTIFICATION_TYPE=pushover`
4. Set `PUSHOVER_USER_KEY` and `PUSHOVER_API_TOKEN`

### Email
1. Set `NOTIFICATION_TYPE=email`
2. Configure SMTP settings (host, port, user, password)
3. For Gmail, use an [App Password](https://support.google.com/accounts/answer/185833)

### Twilio SMS
1. Sign up at https://www.twilio.com/
2. Get a phone number and API credentials
3. Set `NOTIFICATION_TYPE=twilio_sms`
4. Configure Twilio settings

## GCP Deployment

Deploy to Google Cloud Run with Cloud Scheduler for automated checking.

### Prerequisites
- Google Cloud account with billing enabled
- `gcloud` CLI installed and authenticated

### Deploy

1. **Set your project ID:**
   ```bash
   export GCP_PROJECT_ID=your-project-id
   ```

2. **Run the setup script:**
   ```bash
   chmod +x gcp-setup.sh
   ./gcp-setup.sh
   ```

3. **Configure secrets:**
   ```bash
   # For Slack notifications
   echo 'https://hooks.slack.com/services/XXX' | \
     gcloud secrets versions add restaurant-checker-slack-webhook --data-file=-
   ```

4. **Update environment variables:**
   ```bash
   gcloud run services update restaurant-checker \
     --region europe-west2 \
     --set-env-vars "TARGET_DATE=2024-12-31,NOTIFICATION_TYPE=slack"
   ```

### Manual Deployment (without script)

```bash
# Build and push container
gcloud builds submit --config cloudbuild.yaml .

# Deploy to Cloud Run
gcloud run deploy restaurant-checker \
  --image gcr.io/$PROJECT_ID/restaurant-checker:latest \
  --region europe-west2 \
  --platform managed \
  --memory 1Gi

# Create scheduler job (every 5 minutes)
gcloud scheduler jobs create http restaurant-checker-trigger \
  --location europe-west2 \
  --schedule "*/5 * * * *" \
  --uri "$(gcloud run services describe restaurant-checker --region europe-west2 --format 'value(status.url)')" \
  --http-method GET \
  --oidc-service-account-email your-service-account@project.iam.gserviceaccount.com
```

## Docker (Local)

```bash
# Build
docker build -t restaurant-checker .

# Run with environment file
docker run --env-file .env restaurant-checker

# Run single check
docker run --env-file .env restaurant-checker python main.py --once
```

## Configuration Reference

| Variable | Default | Description |
|----------|---------|-------------|
| `TARGET_DATE` | 2024-12-31 | Date to check (YYYY-MM-DD) |
| `TARGET_TIME` | 20:00 | Preferred time (checks ±1 hour) |
| `PARTY_SIZE` | 4 | Number of guests |
| `CHECK_INTERVAL_SECONDS` | 300 | Time between checks |
| `NOTIFICATION_TYPE` | console | Notification method |
| `HEADLESS` | true | Run browser headless |
| `BROWSER_TIMEOUT` | 30 | Browser timeout (seconds) |

## Troubleshooting

### Chrome/Selenium Issues
- Ensure Chrome is installed (for local development)
- Docker image includes Chrome automatically
- Try setting `HEADLESS=false` for debugging locally

### Page Structure Changes
TableAgent may update their booking widget. If availability detection stops working:
1. Run with `--debug` to see detailed logs
2. Check `checker.py` and update selectors as needed
3. The checker tries multiple strategies to find booking forms

### Rate Limiting
If you're blocked:
- Increase `CHECK_INTERVAL_SECONDS`
- The checker includes realistic browser headers
- Consider adding proxy support if needed

## Project Structure

```
restaurant-checker/
├── main.py           # Entry point, CLI handling
├── checker.py        # Selenium-based availability checker
├── notifiers.py      # Notification providers
├── config.py         # Configuration management
├── requirements.txt  # Python dependencies
├── Dockerfile        # Container build
├── cloudbuild.yaml   # Cloud Build config
├── gcp-setup.sh      # GCP deployment script
├── .env.example      # Example configuration
└── README.md         # This file
```

## Technologies

- Python 3.12+
- Selenium with Chrome WebDriver
- Pydantic for configuration
- Google Cloud Run & Cloud Scheduler
