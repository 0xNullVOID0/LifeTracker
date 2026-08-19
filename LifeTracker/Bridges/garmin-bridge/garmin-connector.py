import os
from pathlib import Path
from dotenv import load_dotenv
from datetime import date
from fastapi import FastAPI, HTTPException
from garminconnect import Garmin, GarminConnectAuthenticationError, GarminConnectConnectionError

load_dotenv()

app = FastAPI(title="Garmin Connect Bridge")

TOKEN_DIR = Path.home() / ".garminconnect"
TOKEN_DIR.mkdir(exist_ok=True)


def get_garmin_client() -> Garmin:
    # try login using existing tokens
    try:
        client = Garmin()
        client.login(str(TOKEN_DIR))
        return client
    except (FileNotFoundError, GarminConnectAuthenticationError, GarminConnectConnectionError):
        pass

    email = os.getenv("GARMIN_EMAIL")
    password = os.getenv("GARMIN_PASSWORD")

    if not email or not password:
        raise HTTPException(
            status_code=500,
            detail="No valid tokens and GARMIN_EMAIL/GARMIN_PASSWORD not set in .env",
        )

    # login using credentials and save token
    try:
        client = Garmin(email, password)
        client.login(str(TOKEN_DIR))
        return client
    except Exception as e:
        raise HTTPException(status_code=401, detail=f"Garmin login failed: {str(e)}")

@app.get("/garmin/health")
def health():
   return {"status": "ok"}

@app.get("/garmin/stress/{date_str}")
def get_stress(date_str: str):
    target_date = date.today().isoformat() if date_str == "today" else date_str
    client = get_garmin_client()
    return client.get_stress_data(target_date)


@app.get("/garmin/heartrate/{date_str}")
def get_heart_rate(date_str: str):
    target_date = date.today().isoformat() if date_str == "today" else date_str
    client = get_garmin_client()
    return client.get_heart_rates(target_date)