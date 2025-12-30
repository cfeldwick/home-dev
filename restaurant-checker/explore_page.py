#!/usr/bin/env python3
"""Quick script to explore tableagent.com page structure."""

from selenium import webdriver
from selenium.webdriver.chrome.service import Service as ChromeService
from selenium.webdriver.chrome.options import Options as ChromeOptions
from selenium.webdriver.common.by import By
from selenium.webdriver.support.ui import WebDriverWait
from selenium.webdriver.support import expected_conditions as EC
from webdriver_manager.chrome import ChromeDriverManager
import time

def create_driver():
    options = ChromeOptions()
    options.add_argument("--headless=new")
    options.add_argument("--no-sandbox")
    options.add_argument("--disable-dev-shm-usage")
    options.add_argument("--disable-gpu")
    options.add_argument("--window-size=1920,1080")
    options.add_argument("--disable-blink-features=AutomationControlled")
    options.add_experimental_option("excludeSwitches", ["enable-automation"])
    options.add_argument(
        "user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) "
        "AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36"
    )

    service = ChromeService(ChromeDriverManager().install())
    driver = webdriver.Chrome(service=service, options=options)
    driver.implicitly_wait(10)
    return driver

def explore_page(url):
    print(f"\n{'='*60}")
    print(f"Exploring: {url}")
    print('='*60)

    driver = create_driver()
    try:
        driver.get(url)
        time.sleep(3)  # Let page load fully

        # Get page title
        print(f"\nPage Title: {driver.title}")

        # Look for h1
        try:
            h1 = driver.find_element(By.TAG_NAME, "h1")
            print(f"H1: {h1.text}")
        except:
            print("No H1 found")

        # Look for forms
        forms = driver.find_elements(By.TAG_NAME, "form")
        print(f"\nForms found: {len(forms)}")
        for i, form in enumerate(forms):
            print(f"  Form {i}: id={form.get_attribute('id')}, class={form.get_attribute('class')}")

        # Look for iframes
        iframes = driver.find_elements(By.TAG_NAME, "iframe")
        print(f"\nIframes found: {len(iframes)}")
        for i, iframe in enumerate(iframes):
            print(f"  Iframe {i}: src={iframe.get_attribute('src')}")

        # Look for booking-related elements
        print("\n--- Booking-related elements ---")

        # Buttons with book/reserve text
        buttons = driver.find_elements(By.XPATH,
            "//button | //a[contains(@class, 'btn')] | //input[@type='submit']")
        booking_buttons = [b for b in buttons if any(x in (b.text + str(b.get_attribute('class'))).lower()
                          for x in ['book', 'reserve', 'check', 'availability'])]
        print(f"Booking buttons: {len(booking_buttons)}")
        for b in booking_buttons[:5]:
            print(f"  - text='{b.text}', class={b.get_attribute('class')}, href={b.get_attribute('href')}")

        # Select elements
        selects = driver.find_elements(By.TAG_NAME, "select")
        print(f"\nSelect dropdowns: {len(selects)}")
        for s in selects[:10]:
            options = s.find_elements(By.TAG_NAME, "option")
            opt_texts = [o.text for o in options[:5]]
            print(f"  - id={s.get_attribute('id')}, name={s.get_attribute('name')}, options={opt_texts}")

        # Input fields
        inputs = driver.find_elements(By.TAG_NAME, "input")
        print(f"\nInput fields: {len(inputs)}")
        for inp in inputs[:10]:
            print(f"  - type={inp.get_attribute('type')}, id={inp.get_attribute('id')}, "
                  f"name={inp.get_attribute('name')}, placeholder={inp.get_attribute('placeholder')}")

        # Look for specific booking widget classes
        print("\n--- Looking for booking widgets ---")
        widget_selectors = [
            "[class*='booking']", "[class*='reservation']", "[class*='widget']",
            "[id*='booking']", "[id*='reservation']", "[id*='widget']",
            "[class*='calendar']", "[class*='datepicker']", "[class*='party']",
            "[class*='guest']", "[class*='cover']", "[class*='time']"
        ]
        for selector in widget_selectors:
            try:
                elements = driver.find_elements(By.CSS_SELECTOR, selector)
                if elements:
                    print(f"  {selector}: {len(elements)} elements")
                    for el in elements[:3]:
                        tag = el.tag_name
                        text = el.text[:100].replace('\n', ' ') if el.text else ''
                        print(f"    <{tag}> {text[:80]}...")
            except:
                pass

        # Dump a portion of the page HTML for analysis
        print("\n--- Page structure (key sections) ---")
        body = driver.find_element(By.TAG_NAME, "body")
        # Get main content area
        main_elements = driver.find_elements(By.CSS_SELECTOR, "main, #main, .main, #content, .content, article")
        if main_elements:
            html = main_elements[0].get_attribute('outerHTML')
            # Truncate for readability
            print(html[:3000])
        else:
            # Just get body children
            children = body.find_elements(By.XPATH, "./*")
            for child in children[:5]:
                tag = child.tag_name
                cls = child.get_attribute('class')
                print(f"<{tag} class='{cls}'>")

    finally:
        driver.quit()

# Explore both restaurants
explore_page("https://tableagent.com/west-sussex/efes-town-restaurant-ltd/")
explore_page("https://tableagent.com/west-sussex/efes-restaurant/")
