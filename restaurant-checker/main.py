#!/usr/bin/env python3
"""
Restaurant Availability Checker

Continuously monitors restaurant availability on tableagent.com and sends
notifications when tables become available.

Usage:
    python main.py                    # Run continuous checking
    python main.py --once             # Check once and exit
    python main.py --test-notify      # Test notification setup
"""

import argparse
import logging
import signal
import sys
import time
from datetime import datetime

from config import get_settings, Settings
from checker import check_all_restaurants, AvailabilityResult
from notifiers import get_notifier, AvailabilityAlert


# Configure logging
logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s - %(name)s - %(levelname)s - %(message)s",
    handlers=[logging.StreamHandler(sys.stdout)],
)
logger = logging.getLogger(__name__)


class GracefulExit:
    """Handle graceful shutdown on SIGTERM/SIGINT."""

    def __init__(self):
        self.should_exit = False
        signal.signal(signal.SIGTERM, self._exit_handler)
        signal.signal(signal.SIGINT, self._exit_handler)

    def _exit_handler(self, signum, frame):
        logger.info("Received shutdown signal, finishing current check...")
        self.should_exit = True


def send_notification(settings: Settings, result: AvailabilityResult) -> bool:
    """Send notification for an available table."""
    notifier = get_notifier(settings)
    alert = AvailabilityAlert(
        restaurant_name=result.restaurant_name,
        restaurant_url=result.restaurant_url,
        date=result.date,
        time_slots=result.available_times,
        party_size=result.party_size,
    )
    return notifier.send(alert)


def check_and_notify(settings: Settings) -> list[AvailabilityResult]:
    """Run availability check and send notifications for any available tables."""
    logger.info(f"Starting availability check at {datetime.now().isoformat()}")
    logger.info(f"Looking for: {settings.party_size} people on {settings.target_date} around {settings.target_time}")

    results = check_all_restaurants(settings)

    for result in results:
        if result.error:
            logger.error(f"Error checking {result.restaurant_name}: {result.error}")
        elif result.is_available:
            logger.info(f"✅ FOUND AVAILABILITY at {result.restaurant_name}!")
            logger.info(f"   Available times: {', '.join(result.available_times)}")
            send_notification(settings, result)
        else:
            logger.info(f"❌ No availability at {result.restaurant_name}")

    return results


def run_continuous(settings: Settings):
    """Run continuous checking loop."""
    graceful_exit = GracefulExit()

    logger.info("=" * 60)
    logger.info("Restaurant Availability Checker Started")
    logger.info("=" * 60)
    logger.info(f"Checking {len(settings.restaurants)} restaurants")
    logger.info(f"Target: {settings.party_size} people on {settings.target_date} @ {settings.target_time}")
    logger.info(f"Check interval: {settings.check_interval_seconds} seconds")
    logger.info(f"Notification method: {settings.notification_type.value}")
    logger.info("=" * 60)

    check_count = 0
    while not graceful_exit.should_exit:
        check_count += 1
        logger.info(f"\n--- Check #{check_count} ---")

        try:
            check_and_notify(settings)
        except Exception as e:
            logger.exception(f"Error during check: {e}")

        if not graceful_exit.should_exit:
            next_check = datetime.now().timestamp() + settings.check_interval_seconds
            next_check_time = datetime.fromtimestamp(next_check).strftime("%H:%M:%S")
            logger.info(f"Next check at {next_check_time}")

            # Sleep in small increments to respond to shutdown signals faster
            sleep_remaining = settings.check_interval_seconds
            while sleep_remaining > 0 and not graceful_exit.should_exit:
                time.sleep(min(10, sleep_remaining))
                sleep_remaining -= 10

    logger.info("Checker stopped gracefully")


def run_once(settings: Settings) -> int:
    """Run a single check and exit. Returns 0 if availability found, 1 otherwise."""
    results = check_and_notify(settings)
    available = any(r.is_available for r in results)
    return 0 if available else 1


def test_notification(settings: Settings):
    """Send a test notification to verify settings."""
    logger.info("Sending test notification...")

    notifier = get_notifier(settings)
    test_alert = AvailabilityAlert(
        restaurant_name="Test Restaurant",
        restaurant_url="https://example.com/restaurant",
        date=settings.target_date,
        time_slots=["19:00", "19:30", "20:00"],
        party_size=settings.party_size,
    )

    if notifier.send(test_alert):
        logger.info("✅ Test notification sent successfully!")
    else:
        logger.error("❌ Failed to send test notification. Check your settings.")


def main():
    parser = argparse.ArgumentParser(
        description="Monitor restaurant availability on tableagent.com"
    )
    parser.add_argument(
        "--once",
        action="store_true",
        help="Check once and exit (exit code 0 if available, 1 if not)",
    )
    parser.add_argument(
        "--test-notify",
        action="store_true",
        help="Send a test notification and exit",
    )
    parser.add_argument(
        "--date",
        type=str,
        help="Target date (YYYY-MM-DD), overrides TARGET_DATE env var",
    )
    parser.add_argument(
        "--time",
        type=str,
        help="Target time (HH:MM), overrides TARGET_TIME env var",
    )
    parser.add_argument(
        "--party",
        type=int,
        help="Party size, overrides PARTY_SIZE env var",
    )
    parser.add_argument(
        "--interval",
        type=int,
        help="Check interval in seconds, overrides CHECK_INTERVAL_SECONDS env var",
    )
    parser.add_argument(
        "--debug",
        action="store_true",
        help="Enable debug logging",
    )

    args = parser.parse_args()

    if args.debug:
        logging.getLogger().setLevel(logging.DEBUG)

    # Load settings and apply CLI overrides
    settings = get_settings()

    if args.date:
        settings.target_date = args.date
    if args.time:
        settings.target_time = args.time
    if args.party:
        settings.party_size = args.party
    if args.interval:
        settings.check_interval_seconds = args.interval

    if args.test_notify:
        test_notification(settings)
    elif args.once:
        sys.exit(run_once(settings))
    else:
        run_continuous(settings)


if __name__ == "__main__":
    main()
