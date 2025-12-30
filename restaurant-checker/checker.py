"""
TableAgent availability checker using Selenium.

This module handles the browser automation to check restaurant availability
on tableagent.com.

Page Structure (discovered via explore_page.py):
- Form: id="findtableform"
- Date: input id="reservationdate" (text, MM/DD/YYYY format, has datepicker)
- Time: select id="reservationtime" (populated after date selection)
- Party Size: select id="partysize" (options: "Party Size", "1 person", "2 people", etc.)
- Hours: select id="id_hours" (may populate with available slots)
"""

import logging
import time
from dataclasses import dataclass
from datetime import datetime
from typing import Optional

from selenium import webdriver
from selenium.webdriver.chrome.service import Service as ChromeService
from selenium.webdriver.chrome.options import Options as ChromeOptions
from selenium.webdriver.common.by import By
from selenium.webdriver.common.keys import Keys
from selenium.webdriver.support.ui import WebDriverWait, Select
from selenium.webdriver.support import expected_conditions as EC
from selenium.common.exceptions import (
    TimeoutException,
    NoSuchElementException,
    StaleElementReferenceException,
)
from webdriver_manager.chrome import ChromeDriverManager

from config import Settings


logger = logging.getLogger(__name__)


@dataclass
class AvailabilityResult:
    """Result of an availability check."""
    restaurant_name: str
    restaurant_url: str
    date: str
    party_size: int
    available_times: list[str]
    checked_at: datetime
    error: Optional[str] = None

    @property
    def is_available(self) -> bool:
        """Check if any times are available."""
        return len(self.available_times) > 0


class TableAgentChecker:
    """
    Checks availability on tableagent.com restaurant pages.

    TableAgent booking form structure:
    - Form: id="findtableform"
    - Date: input#reservationdate (MM/DD/YYYY)
    - Time: select#reservationtime
    - Party Size: select#partysize
    """

    def __init__(self, settings: Settings):
        self.settings = settings
        self.driver: Optional[webdriver.Chrome] = None

    def _create_driver(self) -> webdriver.Chrome:
        """Create and configure Chrome WebDriver."""
        options = ChromeOptions()

        if self.settings.headless:
            options.add_argument("--headless=new")

        # Required for running in containers/cloud
        options.add_argument("--no-sandbox")
        options.add_argument("--disable-dev-shm-usage")
        options.add_argument("--disable-gpu")
        options.add_argument("--window-size=1920,1080")

        # Appear more like a regular browser
        options.add_argument("--disable-blink-features=AutomationControlled")
        options.add_experimental_option("excludeSwitches", ["enable-automation"])
        options.add_experimental_option("useAutomationExtension", False)
        options.add_argument(
            "user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) "
            "AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36"
        )

        service = ChromeService(ChromeDriverManager().install())
        driver = webdriver.Chrome(service=service, options=options)
        driver.implicitly_wait(self.settings.browser_timeout)

        return driver

    def __enter__(self):
        """Context manager entry."""
        self.driver = self._create_driver()
        return self

    def __exit__(self, exc_type, exc_val, exc_tb):
        """Context manager exit - cleanup."""
        if self.driver:
            self.driver.quit()
            self.driver = None

    def _handle_cookie_consent(self):
        """Handle cookie consent popups if they appear."""
        try:
            # Common cookie consent button patterns
            cookie_selectors = [
                "button[id*='accept']",
                "button[class*='accept']",
                "#CybotCookiebotDialogBodyButtonAccept",
                ".cookie-accept",
                "[data-action='accept']",
            ]
            for selector in cookie_selectors:
                try:
                    btn = self.driver.find_element(By.CSS_SELECTOR, selector)
                    if btn.is_displayed():
                        btn.click()
                        time.sleep(0.5)
                        logger.debug("Clicked cookie consent button")
                        return
                except NoSuchElementException:
                    continue
        except Exception as e:
            logger.debug(f"Cookie consent handling: {e}")

    def _parse_target_date(self) -> datetime:
        """Parse target date from settings."""
        return datetime.strptime(self.settings.target_date, "%Y-%m-%d")

    def _get_time_range(self) -> tuple[int, int]:
        """Get the hour range to check (target time ± 1 hour)."""
        target_hour = int(self.settings.target_time.split(":")[0])
        return max(0, target_hour - 1), min(23, target_hour + 1)

    def check_availability(self, restaurant_url: str) -> AvailabilityResult:
        """Check availability for a single restaurant."""
        if not self.driver:
            raise RuntimeError("Driver not initialized. Use context manager.")

        target_date = self._parse_target_date()
        available_times = []
        error = None
        restaurant_name = "Unknown Restaurant"

        try:
            logger.info(f"Checking availability at: {restaurant_url}")
            self.driver.get(restaurant_url)
            time.sleep(2)  # Allow page to load

            self._handle_cookie_consent()

            # Get restaurant name from h1
            try:
                h1 = self.driver.find_element(By.TAG_NAME, "h1")
                restaurant_name = h1.text.strip()
            except NoSuchElementException:
                restaurant_name = restaurant_url.split("/")[-2].replace("-", " ").title()

            # Check for the booking form
            try:
                form = self.driver.find_element(By.ID, "findtableform")
                logger.debug("Found booking form: findtableform")
            except NoSuchElementException:
                error = "Booking form not found on page"
                logger.error(error)
                return AvailabilityResult(
                    restaurant_name=restaurant_name,
                    restaurant_url=restaurant_url,
                    date=self.settings.target_date,
                    party_size=self.settings.party_size,
                    available_times=[],
                    checked_at=datetime.now(),
                    error=error,
                )

            # Fill in the booking form
            available_times = self._fill_form_and_check(target_date)

        except TimeoutException as e:
            error = f"Timeout waiting for page elements: {e}"
            logger.error(error)
        except Exception as e:
            error = f"Error checking availability: {e}"
            logger.exception(error)

        return AvailabilityResult(
            restaurant_name=restaurant_name,
            restaurant_url=restaurant_url,
            date=self.settings.target_date,
            party_size=self.settings.party_size,
            available_times=available_times,
            checked_at=datetime.now(),
            error=error,
        )

    def _fill_form_and_check(self, target_date: datetime) -> list[str]:
        """
        Fill in the TableAgent booking form and check available times.

        Form elements:
        - input#reservationdate (MM/DD/YYYY)
        - select#partysize
        - select#reservationtime (populated dynamically)
        """
        wait = WebDriverWait(self.driver, 10)
        available_times = []

        # Step 1: Set party size first
        logger.debug(f"Setting party size to {self.settings.party_size}")
        try:
            party_select = Select(self.driver.find_element(By.ID, "partysize"))
            # Options are like "4 people" or "1 person"
            party_text = f"{self.settings.party_size} people" if self.settings.party_size > 1 else "1 person"
            party_select.select_by_visible_text(party_text)
            time.sleep(0.5)
        except Exception as e:
            logger.warning(f"Could not set party size: {e}")

        # Step 2: Set the date
        date_str = target_date.strftime("%m/%d/%Y")  # MM/DD/YYYY format
        logger.debug(f"Setting date to {date_str}")
        try:
            date_input = self.driver.find_element(By.ID, "reservationdate")
            # Clear and set the date
            date_input.clear()
            date_input.send_keys(date_str)
            # Press tab or click elsewhere to trigger any date validation
            date_input.send_keys(Keys.TAB)
            time.sleep(1)  # Wait for time slots to load

            # Click elsewhere to close any datepicker
            self.driver.find_element(By.TAG_NAME, "body").click()
            time.sleep(0.5)
        except Exception as e:
            logger.warning(f"Could not set date: {e}")

        # Step 3: Wait for time options to load and check what's available
        time.sleep(1.5)  # Give time for AJAX to update time slots

        # Check the reservationtime select for available times
        available_times = self._get_available_times()

        # Also check id_hours if it has options (backup)
        if not available_times:
            try:
                hours_select = self.driver.find_element(By.ID, "id_hours")
                hours_options = hours_select.find_elements(By.TAG_NAME, "option")
                for opt in hours_options:
                    text = opt.text.strip()
                    if text and self._is_time_in_range(text):
                        available_times.append(text)
            except NoSuchElementException:
                pass

        return available_times

    def _get_available_times(self) -> list[str]:
        """Get available time slots from the reservationtime select."""
        available_times = []
        min_hour, max_hour = self._get_time_range()

        try:
            time_select = self.driver.find_element(By.ID, "reservationtime")
            options = time_select.find_elements(By.TAG_NAME, "option")

            for option in options:
                text = option.text.strip()
                value = option.get_attribute("value")

                # Skip placeholder options
                if not text or text.lower() in ["select time", "select a time", "time", ""]:
                    continue
                if not value:
                    continue

                # Check if the option is disabled
                if option.get_attribute("disabled"):
                    continue

                # Check if it's in our target time range
                if self._is_time_in_range(text):
                    available_times.append(text)
                    logger.debug(f"Found available time: {text}")

        except NoSuchElementException:
            logger.warning("Could not find reservationtime select")
        except Exception as e:
            logger.warning(f"Error getting available times: {e}")

        return available_times

    def _is_time_in_range(self, time_text: str) -> bool:
        """Check if a time string falls within our target range."""
        import re

        min_hour, max_hour = self._get_time_range()

        # Parse times like "7:30 PM", "19:30", "11:30 AM"
        match = re.search(r"(\d{1,2}):(\d{2})\s*(AM|PM|am|pm)?", time_text)
        if not match:
            return False

        hour = int(match.group(1))
        period = match.group(3)

        # Convert to 24-hour format
        if period:
            period = period.upper()
            if period == "PM" and hour < 12:
                hour += 12
            elif period == "AM" and hour == 12:
                hour = 0

        return min_hour <= hour <= max_hour


def check_all_restaurants(settings: Settings) -> list[AvailabilityResult]:
    """Check availability at all configured restaurants."""
    results = []

    with TableAgentChecker(settings) as checker:
        for url in settings.restaurants:
            result = checker.check_availability(url)
            results.append(result)

            # Small delay between restaurants
            time.sleep(1)

    return results
