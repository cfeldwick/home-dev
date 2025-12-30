"""Notification providers for restaurant availability alerts."""

import logging
import smtplib
from abc import ABC, abstractmethod
from email.mime.text import MIMEText
from email.mime.multipart import MIMEMultipart
from dataclasses import dataclass
from typing import Optional

import requests

from config import Settings, NotificationType


logger = logging.getLogger(__name__)


@dataclass
class AvailabilityAlert:
    """Represents an availability alert to send."""
    restaurant_name: str
    restaurant_url: str
    date: str
    time_slots: list[str]
    party_size: int

    def format_message(self) -> str:
        """Format alert as human-readable message."""
        slots = ", ".join(self.time_slots)
        return (
            f"🍽️ Table Available!\n\n"
            f"Restaurant: {self.restaurant_name}\n"
            f"Date: {self.date}\n"
            f"Available times: {slots}\n"
            f"Party size: {self.party_size}\n\n"
            f"Book now: {self.restaurant_url}"
        )

    def format_subject(self) -> str:
        """Format alert subject line."""
        return f"Table Available: {self.restaurant_name} on {self.date}"


class Notifier(ABC):
    """Base class for notification providers."""

    @abstractmethod
    def send(self, alert: AvailabilityAlert) -> bool:
        """Send a notification. Returns True if successful."""
        pass


class ConsoleNotifier(Notifier):
    """Print notifications to console (for testing/debugging)."""

    def send(self, alert: AvailabilityAlert) -> bool:
        print("\n" + "=" * 50)
        print("🔔 AVAILABILITY ALERT!")
        print("=" * 50)
        print(alert.format_message())
        print("=" * 50 + "\n")
        return True


class EmailNotifier(Notifier):
    """Send notifications via SMTP email."""

    def __init__(self, settings: Settings):
        self.host = settings.smtp_host
        self.port = settings.smtp_port
        self.user = settings.smtp_user
        self.password = settings.smtp_password
        self.email_to = settings.email_to
        self.email_from = settings.email_from or settings.smtp_user

    def send(self, alert: AvailabilityAlert) -> bool:
        if not all([self.host, self.user, self.password, self.email_to]):
            logger.error("Email settings not configured")
            return False

        try:
            msg = MIMEMultipart()
            msg["From"] = self.email_from
            msg["To"] = self.email_to
            msg["Subject"] = alert.format_subject()
            msg.attach(MIMEText(alert.format_message(), "plain"))

            with smtplib.SMTP(self.host, self.port) as server:
                server.starttls()
                server.login(self.user, self.password)
                server.send_message(msg)

            logger.info(f"Email sent to {self.email_to}")
            return True
        except Exception as e:
            logger.error(f"Failed to send email: {e}")
            return False


class SlackNotifier(Notifier):
    """Send notifications via Slack webhook."""

    def __init__(self, settings: Settings):
        self.webhook_url = settings.slack_webhook_url

    def send(self, alert: AvailabilityAlert) -> bool:
        if not self.webhook_url:
            logger.error("Slack webhook URL not configured")
            return False

        try:
            payload = {
                "text": alert.format_subject(),
                "blocks": [
                    {
                        "type": "header",
                        "text": {"type": "plain_text", "text": "🍽️ Table Available!"}
                    },
                    {
                        "type": "section",
                        "fields": [
                            {"type": "mrkdwn", "text": f"*Restaurant:*\n{alert.restaurant_name}"},
                            {"type": "mrkdwn", "text": f"*Date:*\n{alert.date}"},
                            {"type": "mrkdwn", "text": f"*Times:*\n{', '.join(alert.time_slots)}"},
                            {"type": "mrkdwn", "text": f"*Party Size:*\n{alert.party_size}"},
                        ]
                    },
                    {
                        "type": "actions",
                        "elements": [
                            {
                                "type": "button",
                                "text": {"type": "plain_text", "text": "Book Now"},
                                "url": alert.restaurant_url,
                                "style": "primary"
                            }
                        ]
                    }
                ]
            }

            response = requests.post(self.webhook_url, json=payload, timeout=10)
            response.raise_for_status()
            logger.info("Slack notification sent")
            return True
        except Exception as e:
            logger.error(f"Failed to send Slack notification: {e}")
            return False


class TelegramNotifier(Notifier):
    """Send notifications via Telegram bot."""

    def __init__(self, settings: Settings):
        self.bot_token = settings.telegram_bot_token
        self.chat_id = settings.telegram_chat_id

    def send(self, alert: AvailabilityAlert) -> bool:
        if not all([self.bot_token, self.chat_id]):
            logger.error("Telegram settings not configured")
            return False

        try:
            url = f"https://api.telegram.org/bot{self.bot_token}/sendMessage"
            payload = {
                "chat_id": self.chat_id,
                "text": alert.format_message(),
                "parse_mode": "HTML",
                "disable_web_page_preview": False
            }

            response = requests.post(url, json=payload, timeout=10)
            response.raise_for_status()
            logger.info("Telegram notification sent")
            return True
        except Exception as e:
            logger.error(f"Failed to send Telegram notification: {e}")
            return False


class PushoverNotifier(Notifier):
    """Send notifications via Pushover (iOS/Android push notifications)."""

    def __init__(self, settings: Settings):
        self.user_key = settings.pushover_user_key
        self.api_token = settings.pushover_api_token

    def send(self, alert: AvailabilityAlert) -> bool:
        if not all([self.user_key, self.api_token]):
            logger.error("Pushover settings not configured")
            return False

        try:
            response = requests.post(
                "https://api.pushover.net/1/messages.json",
                data={
                    "token": self.api_token,
                    "user": self.user_key,
                    "title": alert.format_subject(),
                    "message": alert.format_message(),
                    "url": alert.restaurant_url,
                    "url_title": "Book Now",
                    "priority": 1,  # High priority
                    "sound": "magic"
                },
                timeout=10
            )
            response.raise_for_status()
            logger.info("Pushover notification sent")
            return True
        except Exception as e:
            logger.error(f"Failed to send Pushover notification: {e}")
            return False


class TwilioSMSNotifier(Notifier):
    """Send notifications via Twilio SMS."""

    def __init__(self, settings: Settings):
        self.account_sid = settings.twilio_account_sid
        self.auth_token = settings.twilio_auth_token
        self.from_number = settings.twilio_from_number
        self.to_number = settings.twilio_to_number

    def send(self, alert: AvailabilityAlert) -> bool:
        if not all([self.account_sid, self.auth_token, self.from_number, self.to_number]):
            logger.error("Twilio settings not configured")
            return False

        try:
            from twilio.rest import Client

            client = Client(self.account_sid, self.auth_token)
            # SMS has 160 char limit, so keep it brief
            message = (
                f"Table at {alert.restaurant_name}! "
                f"{alert.date} @ {', '.join(alert.time_slots[:2])}. "
                f"Book: {alert.restaurant_url}"
            )

            client.messages.create(
                body=message[:160],
                from_=self.from_number,
                to=self.to_number
            )
            logger.info(f"SMS sent to {self.to_number}")
            return True
        except Exception as e:
            logger.error(f"Failed to send SMS: {e}")
            return False


def get_notifier(settings: Settings) -> Notifier:
    """Factory function to get the appropriate notifier based on settings."""
    notifiers = {
        NotificationType.CONSOLE: ConsoleNotifier,
        NotificationType.EMAIL: EmailNotifier,
        NotificationType.SLACK: SlackNotifier,
        NotificationType.TELEGRAM: TelegramNotifier,
        NotificationType.PUSHOVER: PushoverNotifier,
        NotificationType.TWILIO_SMS: TwilioSMSNotifier,
    }

    notifier_class = notifiers.get(settings.notification_type, ConsoleNotifier)

    # Console notifier doesn't need settings
    if notifier_class == ConsoleNotifier:
        return ConsoleNotifier()

    return notifier_class(settings)
