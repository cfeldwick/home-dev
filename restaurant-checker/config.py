"""Configuration management for restaurant availability checker."""

from pydantic_settings import BaseSettings
from pydantic import Field
from typing import Optional
from enum import Enum


class NotificationType(str, Enum):
    """Supported notification types."""
    CONSOLE = "console"         # Just print to console (default)
    EMAIL = "email"             # SMTP email
    SLACK = "slack"             # Slack webhook
    TELEGRAM = "telegram"       # Telegram bot
    PUSHOVER = "pushover"       # Pushover push notifications
    TWILIO_SMS = "twilio_sms"   # Twilio SMS


class Settings(BaseSettings):
    """Application settings loaded from environment variables."""

    # Restaurants to check
    restaurants: list[str] = Field(
        default=[
            "https://tableagent.com/west-sussex/efes-town-restaurant-ltd/",
            "https://tableagent.com/west-sussex/efes-restaurant/",
        ],
        description="List of restaurant URLs to check"
    )

    # Booking parameters
    target_date: str = Field(
        default="2025-12-31",
        description="Target date in YYYY-MM-DD format"
    )
    target_time: str = Field(
        default="20:00",
        description="Preferred time (will check ±1 hour)"
    )
    party_size: int = Field(
        default=4,
        description="Number of guests"
    )

    # Check frequency
    check_interval_seconds: int = Field(
        default=300,  # 5 minutes
        description="How often to check (in seconds)"
    )

    # Notification settings
    notification_type: NotificationType = Field(
        default=NotificationType.CONSOLE,
        description="How to send notifications"
    )

    # Email settings (for EMAIL notification)
    smtp_host: Optional[str] = None
    smtp_port: int = 587
    smtp_user: Optional[str] = None
    smtp_password: Optional[str] = None
    email_to: Optional[str] = None
    email_from: Optional[str] = None

    # Slack settings (for SLACK notification)
    slack_webhook_url: Optional[str] = None

    # Telegram settings (for TELEGRAM notification)
    telegram_bot_token: Optional[str] = None
    telegram_chat_id: Optional[str] = None

    # Pushover settings (for PUSHOVER notification)
    pushover_user_key: Optional[str] = None
    pushover_api_token: Optional[str] = None

    # Twilio settings (for TWILIO_SMS notification)
    twilio_account_sid: Optional[str] = None
    twilio_auth_token: Optional[str] = None
    twilio_from_number: Optional[str] = None
    twilio_to_number: Optional[str] = None

    # Browser settings
    headless: bool = Field(
        default=True,
        description="Run browser in headless mode"
    )
    browser_timeout: int = Field(
        default=30,
        description="Browser timeout in seconds"
    )

    # GCP settings
    gcp_project_id: Optional[str] = None
    use_gcp_secrets: bool = Field(
        default=False,
        description="Load secrets from GCP Secret Manager"
    )

    class Config:
        env_file = ".env"
        env_file_encoding = "utf-8"
        # Allow environment variables with underscores to map to nested fields
        env_nested_delimiter = "__"


def get_settings() -> Settings:
    """Get application settings."""
    return Settings()
