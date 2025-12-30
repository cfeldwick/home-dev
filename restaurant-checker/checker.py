"""
TableAgent availability checker using Selenium.

This module handles the browser automation to check restaurant availability
on tableagent.com. The page structure was analyzed to find the booking widget.
"""

import logging
import time
from dataclasses import dataclass
from datetime import datetime, timedelta
from typing import Optional

from selenium import webdriver
from selenium.webdriver.chrome.service import Service as ChromeService
from selenium.webdriver.chrome.options import Options as ChromeOptions
from selenium.webdriver.common.by import By
from selenium.webdriver.support.ui import WebDriverWait, Select
from selenium.webdriver.support import expected_conditions as EC
from selenium.common.exceptions import (
    TimeoutException,
    NoSuchElementException,
    ElementClickInterceptedException,
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

    TableAgent uses a booking widget that typically has:
    - Date picker (calendar or dropdown)
    - Time dropdown
    - Party size (covers) selector
    - Available time slots display
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
                "a[id*='accept']",
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
        """
        Check availability for a single restaurant.

        TableAgent booking widgets typically work by:
        1. Selecting date from a calendar widget
        2. Selecting party size (covers)
        3. Viewing available time slots

        The exact selectors may need adjustment based on the specific page structure.
        """
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

            # Try to get restaurant name from page
            try:
                restaurant_name = self.driver.find_element(By.TAG_NAME, "h1").text
            except NoSuchElementException:
                restaurant_name = restaurant_url.split("/")[-2].replace("-", " ").title()

            # Look for the booking widget - TableAgent uses various structures
            # Try multiple approaches to find and interact with the booking form

            available_times = self._check_tableagent_widget(target_date)

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

    def _check_tableagent_widget(self, target_date: datetime) -> list[str]:
        """
        Interact with TableAgent's booking widget to check availability.

        TableAgent restaurants typically have a booking widget with:
        - A date selector (could be calendar or date input)
        - A party size selector
        - Time slot buttons or dropdown

        This method attempts to interact with these elements.
        """
        wait = WebDriverWait(self.driver, 10)
        available_times = []

        # Strategy 1: Look for iframe-based booking widget
        iframes = self.driver.find_elements(By.TAG_NAME, "iframe")
        for iframe in iframes:
            src = iframe.get_attribute("src") or ""
            if "booking" in src.lower() or "reservation" in src.lower():
                logger.debug(f"Found booking iframe: {src}")
                self.driver.switch_to.frame(iframe)
                try:
                    available_times = self._interact_with_booking_form(target_date)
                finally:
                    self.driver.switch_to.default_content()
                if available_times:
                    return available_times

        # Strategy 2: Direct booking form on page
        available_times = self._interact_with_booking_form(target_date)
        if available_times:
            return available_times

        # Strategy 3: Look for "Book" or "Reserve" buttons that open a modal
        book_buttons = self.driver.find_elements(
            By.XPATH,
            "//button[contains(translate(., 'BOOK', 'book'), 'book')] | "
            "//a[contains(translate(., 'BOOK', 'book'), 'book')] | "
            "//button[contains(translate(., 'RESERVE', 'reserve'), 'reserve')] | "
            "//a[contains(translate(., 'RESERVE', 'reserve'), 'reserve')]"
        )
        for btn in book_buttons:
            try:
                if btn.is_displayed():
                    btn.click()
                    time.sleep(1)
                    available_times = self._interact_with_booking_form(target_date)
                    if available_times:
                        return available_times
            except ElementClickInterceptedException:
                continue

        logger.warning("Could not find booking widget on page")
        return available_times

    def _interact_with_booking_form(self, target_date: datetime) -> list[str]:
        """
        Interact with the booking form to check available time slots.

        This tries multiple common patterns for date/party/time selection.
        """
        available_times = []
        wait = WebDriverWait(self.driver, 5)

        try:
            # Step 1: Set party size
            self._set_party_size()

            # Step 2: Set date
            self._set_date(target_date)

            # Step 3: Look for available time slots
            time.sleep(1)  # Allow dynamic content to load
            available_times = self._find_available_times()

        except Exception as e:
            logger.debug(f"Form interaction failed: {e}")

        return available_times

    def _set_party_size(self):
        """Set the party size in the booking form."""
        party_size = self.settings.party_size

        # Try various party size selectors
        selectors = [
            # Dropdown selects
            ("select[name*='party']", "select"),
            ("select[name*='guest']", "select"),
            ("select[name*='cover']", "select"),
            ("select[name*='people']", "select"),
            ("select[id*='party']", "select"),
            ("select[id*='guest']", "select"),
            ("#partySize", "select"),
            ("#covers", "select"),
            ("#guests", "select"),
            # Input fields
            ("input[name*='party']", "input"),
            ("input[name*='guest']", "input"),
            ("input[type='number'][name*='party']", "input"),
        ]

        for selector, elem_type in selectors:
            try:
                elem = self.driver.find_element(By.CSS_SELECTOR, selector)
                if elem.is_displayed():
                    if elem_type == "select":
                        select = Select(elem)
                        # Try to select by value or visible text
                        try:
                            select.select_by_value(str(party_size))
                        except NoSuchElementException:
                            select.select_by_visible_text(str(party_size))
                    else:
                        elem.clear()
                        elem.send_keys(str(party_size))
                    logger.debug(f"Set party size to {party_size}")
                    return
            except NoSuchElementException:
                continue

        # Try clicking +/- buttons
        try:
            current = self.driver.find_element(
                By.XPATH, "//*[contains(@class, 'party') or contains(@class, 'guest')]//span"
            )
            current_val = int(current.text)
            diff = party_size - current_val
            if diff > 0:
                plus_btn = self.driver.find_element(
                    By.XPATH, "//*[contains(@class, 'party') or contains(@class, 'guest')]//button[contains(., '+')]"
                )
                for _ in range(diff):
                    plus_btn.click()
            elif diff < 0:
                minus_btn = self.driver.find_element(
                    By.XPATH, "//*[contains(@class, 'party') or contains(@class, 'guest')]//button[contains(., '-')]"
                )
                for _ in range(abs(diff)):
                    minus_btn.click()
        except Exception:
            pass

        logger.debug("Could not find party size selector")

    def _set_date(self, target_date: datetime):
        """Set the date in the booking form."""
        date_str = target_date.strftime("%Y-%m-%d")
        date_display = target_date.strftime("%d/%m/%Y")
        day = target_date.day
        month = target_date.strftime("%B")
        month_short = target_date.strftime("%b")

        # Try date input field
        date_inputs = [
            "input[type='date']",
            "input[name*='date']",
            "input[id*='date']",
            "#bookingDate",
            "#reservationDate",
        ]

        for selector in date_inputs:
            try:
                elem = self.driver.find_element(By.CSS_SELECTOR, selector)
                if elem.is_displayed():
                    elem.clear()
                    elem.send_keys(date_str)
                    logger.debug(f"Set date to {date_str}")
                    return
            except NoSuchElementException:
                continue

        # Try calendar picker
        try:
            # Click on date field to open calendar
            date_trigger = self.driver.find_element(
                By.XPATH,
                "//*[contains(@class, 'date') or contains(@class, 'calendar')]"
                "[contains(@class, 'picker') or contains(@class, 'input') or contains(@class, 'trigger')]"
            )
            date_trigger.click()
            time.sleep(0.5)

            # Navigate to correct month if needed
            self._navigate_calendar_to_month(target_date)

            # Click on the day
            day_elem = self.driver.find_element(
                By.XPATH, f"//td[contains(@class, 'day') and text()='{day}'] | "
                         f"//div[contains(@class, 'day') and text()='{day}']"
            )
            day_elem.click()
            logger.debug(f"Selected date {day} from calendar")
            return
        except Exception as e:
            logger.debug(f"Calendar picker interaction failed: {e}")

    def _navigate_calendar_to_month(self, target_date: datetime):
        """Navigate calendar widget to the target month."""
        target_month = target_date.strftime("%B %Y")
        target_month_short = target_date.strftime("%b %Y")

        max_clicks = 12
        for _ in range(max_clicks):
            try:
                # Check current month display
                month_display = self.driver.find_element(
                    By.XPATH, "//*[contains(@class, 'month') or contains(@class, 'title')]"
                )
                current_text = month_display.text.strip()

                if target_month in current_text or target_month_short in current_text:
                    return  # We're at the right month

                # Click next month button
                next_btn = self.driver.find_element(
                    By.XPATH, "//button[contains(@class, 'next')] | "
                             "//a[contains(@class, 'next')] | "
                             "//*[@aria-label='Next month']"
                )
                next_btn.click()
                time.sleep(0.3)
            except Exception:
                break

    def _find_available_times(self) -> list[str]:
        """Find available time slots on the page."""
        available_times = []
        min_hour, max_hour = self._get_time_range()

        # Look for time slot elements - various patterns
        time_patterns = [
            # Time slot buttons
            "//button[contains(@class, 'time') or contains(@class, 'slot')]",
            "//a[contains(@class, 'time') or contains(@class, 'slot')]",
            "//div[contains(@class, 'time-slot') or contains(@class, 'timeslot')]",
            # Time in lists
            "//li[contains(@class, 'time') or contains(@class, 'slot')]",
            # Generic time patterns
            "//*[contains(text(), ':00') or contains(text(), ':15') or contains(text(), ':30') or contains(text(), ':45')]",
        ]

        for pattern in time_patterns:
            try:
                elements = self.driver.find_elements(By.XPATH, pattern)
                for elem in elements:
                    text = elem.text.strip()
                    if self._is_valid_time(text, min_hour, max_hour):
                        # Check if it's actually available (not disabled/crossed out)
                        classes = elem.get_attribute("class") or ""
                        if not any(x in classes.lower() for x in ["disabled", "unavailable", "booked", "sold"]):
                            if not elem.get_attribute("disabled"):
                                available_times.append(text)
            except Exception:
                continue

        # Try select dropdown
        try:
            time_select = self.driver.find_element(
                By.CSS_SELECTOR, "select[name*='time'], select[id*='time']"
            )
            select = Select(time_select)
            for option in select.options:
                text = option.text.strip()
                if self._is_valid_time(text, min_hour, max_hour):
                    if option.is_enabled():
                        available_times.append(text)
        except NoSuchElementException:
            pass

        # Deduplicate and sort
        available_times = sorted(set(available_times))
        logger.info(f"Found {len(available_times)} available times: {available_times}")
        return available_times

    def _is_valid_time(self, text: str, min_hour: int, max_hour: int) -> bool:
        """Check if text represents a valid time in our target range."""
        import re
        # Match patterns like "7:30", "19:30", "7:30 PM", "7.30pm"
        time_pattern = r"(\d{1,2})[:\.](\d{2})\s*(am|pm|AM|PM)?"
        match = re.search(time_pattern, text)
        if not match:
            return False

        hour = int(match.group(1))
        period = match.group(3)

        # Convert to 24-hour if needed
        if period:
            period = period.lower()
            if period == "pm" and hour < 12:
                hour += 12
            elif period == "am" and hour == 12:
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
