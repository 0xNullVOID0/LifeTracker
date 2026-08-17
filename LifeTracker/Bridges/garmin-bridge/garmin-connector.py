import os
from pathlib import Path
from dotenv import load_dotenv
from garminconnect import Garmin, GarminConnectAuthenticationError, GarminConnectConnectionError

load_dotenv()

TOKEN_DIR = Path.home() / ".garminconnect"
TOKEN_DIR.mkdir(exist_ok=True)

def get_garmin():
    # try existing tokens
    try:
        client = Garmin()
        client.login(str(TOKEN_DIR))
        print("Using saved tokens")
        return client
    except (FileNotFoundError, GarminConnectAuthenticationError, GarminConnectConnectionError):
        print("No valid tokens found, logging in with credentials...")

    # login using credentials and save token
    client = Garmin(
        os.getenv("GARMIN_EMAIL"),
        os.getenv("GARMIN_PASSWORD")
    )
    client.login(str(TOKEN_DIR))
    print("Login successful, tokens saved")
    return client

client = get_garmin()