#!/bin/bash
# GCP Setup Script for Restaurant Availability Checker
#
# This script sets up:
# 1. Cloud Run service (containerized checker)
# 2. Cloud Scheduler job (triggers checks every 5 minutes)
# 3. Secret Manager secrets (for notification credentials)
#
# Prerequisites:
# - gcloud CLI installed and authenticated
# - Project created and billing enabled
# - Required APIs enabled (see below)
#
# Usage:
#   chmod +x gcp-setup.sh
#   ./gcp-setup.sh

set -e

# Configuration - edit these!
PROJECT_ID="${GCP_PROJECT_ID:-your-project-id}"
REGION="europe-west2"  # London
SERVICE_NAME="restaurant-checker"
SCHEDULE="*/5 * * * *"  # Every 5 minutes

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${GREEN}=== Restaurant Checker GCP Setup ===${NC}\n"

# Check if project ID is set
if [ "$PROJECT_ID" = "your-project-id" ]; then
    echo -e "${RED}Error: Please set GCP_PROJECT_ID environment variable or edit this script${NC}"
    echo "Example: GCP_PROJECT_ID=my-project ./gcp-setup.sh"
    exit 1
fi

echo -e "${YELLOW}Project: $PROJECT_ID${NC}"
echo -e "${YELLOW}Region: $REGION${NC}"
echo ""

# Set the project
gcloud config set project "$PROJECT_ID"

# Enable required APIs
echo -e "${GREEN}Enabling required APIs...${NC}"
gcloud services enable \
    run.googleapis.com \
    cloudbuild.googleapis.com \
    cloudscheduler.googleapis.com \
    secretmanager.googleapis.com \
    containerregistry.googleapis.com

# Create secrets (placeholder - user needs to set values)
echo -e "\n${GREEN}Creating secrets (you'll need to set values later)...${NC}"

create_secret_if_not_exists() {
    SECRET_NAME=$1
    if ! gcloud secrets describe "$SECRET_NAME" --project="$PROJECT_ID" &>/dev/null; then
        echo "Creating secret: $SECRET_NAME"
        echo "placeholder" | gcloud secrets create "$SECRET_NAME" \
            --data-file=- \
            --replication-policy="automatic"
    else
        echo "Secret already exists: $SECRET_NAME"
    fi
}

# Create secrets for notification options
create_secret_if_not_exists "restaurant-checker-slack-webhook"
create_secret_if_not_exists "restaurant-checker-telegram-token"
create_secret_if_not_exists "restaurant-checker-telegram-chat-id"
create_secret_if_not_exists "restaurant-checker-pushover-user"
create_secret_if_not_exists "restaurant-checker-pushover-token"

echo -e "\n${YELLOW}To set a secret value, use:${NC}"
echo "echo 'your-value' | gcloud secrets versions add SECRET_NAME --data-file=-"

# Build and push the container
echo -e "\n${GREEN}Building and pushing container...${NC}"
gcloud builds submit --config cloudbuild.yaml .

# Create service account for Cloud Run
SA_NAME="${SERVICE_NAME}-sa"
SA_EMAIL="${SA_NAME}@${PROJECT_ID}.iam.gserviceaccount.com"

echo -e "\n${GREEN}Creating service account...${NC}"
if ! gcloud iam service-accounts describe "$SA_EMAIL" &>/dev/null; then
    gcloud iam service-accounts create "$SA_NAME" \
        --display-name="Restaurant Checker Service Account"
fi

# Grant secret accessor role
gcloud projects add-iam-policy-binding "$PROJECT_ID" \
    --member="serviceAccount:$SA_EMAIL" \
    --role="roles/secretmanager.secretAccessor" \
    --condition=None

# Deploy to Cloud Run with secrets
echo -e "\n${GREEN}Deploying to Cloud Run...${NC}"
gcloud run deploy "$SERVICE_NAME" \
    --image "gcr.io/$PROJECT_ID/$SERVICE_NAME:latest" \
    --region "$REGION" \
    --platform managed \
    --no-allow-unauthenticated \
    --service-account "$SA_EMAIL" \
    --memory 1Gi \
    --cpu 1 \
    --timeout 300 \
    --max-instances 1 \
    --set-env-vars "HEADLESS=true,TARGET_DATE=2024-12-31,TARGET_TIME=20:00,PARTY_SIZE=4,NOTIFICATION_TYPE=slack" \
    --set-secrets "SLACK_WEBHOOK_URL=restaurant-checker-slack-webhook:latest"

# Get the Cloud Run URL
SERVICE_URL=$(gcloud run services describe "$SERVICE_NAME" --region "$REGION" --format 'value(status.url)')
echo -e "${GREEN}Service deployed at: $SERVICE_URL${NC}"

# Create Cloud Scheduler job
echo -e "\n${GREEN}Setting up Cloud Scheduler...${NC}"

# Delete existing scheduler if exists
gcloud scheduler jobs delete "${SERVICE_NAME}-trigger" \
    --location "$REGION" \
    --quiet 2>/dev/null || true

# Create new scheduler job
gcloud scheduler jobs create http "${SERVICE_NAME}-trigger" \
    --location "$REGION" \
    --schedule "$SCHEDULE" \
    --uri "${SERVICE_URL}" \
    --http-method GET \
    --oidc-service-account-email "$SA_EMAIL" \
    --oidc-token-audience "$SERVICE_URL" \
    --attempt-deadline 300s

echo -e "\n${GREEN}=== Setup Complete! ===${NC}"
echo ""
echo -e "${YELLOW}Next steps:${NC}"
echo "1. Set your notification credentials in Secret Manager:"
echo "   - For Slack: gcloud secrets versions add restaurant-checker-slack-webhook --data-file=-"
echo "   - For Telegram: set both token and chat-id secrets"
echo "   - For Pushover: set both user and token secrets"
echo ""
echo "2. Update environment variables if needed:"
echo "   gcloud run services update $SERVICE_NAME --region $REGION --set-env-vars KEY=VALUE"
echo ""
echo "3. Manually trigger a check:"
echo "   gcloud scheduler jobs run ${SERVICE_NAME}-trigger --location $REGION"
echo ""
echo "4. View logs:"
echo "   gcloud run services logs read $SERVICE_NAME --region $REGION"
