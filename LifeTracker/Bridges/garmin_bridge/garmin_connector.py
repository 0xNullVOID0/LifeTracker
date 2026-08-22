import os
from pathlib import Path
from dotenv import load_dotenv
from datetime import date
from fastapi import FastAPI, HTTPException, Query, Response
from garminconnect import Garmin, GarminConnectAuthenticationError, GarminConnectConnectionError

load_dotenv()

app = FastAPI(title="Garmin Connect Bridge")

TOKEN_DIR = Path(os.getenv("GARMIN_TOKEN_DIR", str(Path.home() / ".garminconnect")))
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

def resolve_date(date_str: str | None) -> date:
    """None → today. Reject future dates with 404."""
    target = date.today() if date_str is None else date.fromisoformat(date_str)
    if target > date.today():
        raise HTTPException(
            status_code=404,
            detail=f"No data for future date {target.isoformat()}",
        )
    return target


@app.get("/garmin/stress")
def get_stress(
    response: Response,
    date_str: str | None = Query(None, alias="date", description="YYYY-MM-DD; default today"),
):
    target = resolve_date(date_str)
    client = get_garmin_client()
    data = client.get_stress_data(target.isoformat())

    if not data or not data.get("startTimestampGMT"):
        response.status_code = 204
        return None
    return data


@app.get("/garmin/heartrate")
def get_heart_rate(
    response: Response,
    date_str: str | None = Query(None, alias="date", description="YYYY-MM-DD; default today"),
):
    target = resolve_date(date_str)
    client = get_garmin_client()
    data = client.get_heart_rates(target.isoformat())

    if not data or not data.get("startTimestampGMT"):
        response.status_code = 204
        return None
    return data


@app.get("/garmin/sleep")
def get_sleep(
    response: Response,
    date_str: str | None = Query(None, alias="date", description="YYYY-MM-DD; default today"),
):
    target = resolve_date(date_str)
    client = get_garmin_client()
    data = client.get_sleep_data(target.isoformat())

    daily = (data or {}).get("dailySleepDTO") or {}
    if not data or daily.get("sleepTimeSeconds") is None:
        response.status_code = 204
        return None
    return data