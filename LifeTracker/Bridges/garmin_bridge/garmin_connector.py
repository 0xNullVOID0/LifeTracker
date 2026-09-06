import hmac
import os
from pathlib import Path
from dotenv import load_dotenv
from datetime import date
from fastapi import Depends, FastAPI, Header, HTTPException, Query, Response
from garminconnect import Garmin, GarminConnectAuthenticationError, GarminConnectConnectionError

load_dotenv()

app = FastAPI(title="Garmin Connect Bridge")

BRIDGE_API_KEY = os.getenv("GARMIN_BRIDGE_API_KEY", "")
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
    """None → today. Reject future dates with 400."""
    target = date.today() if date_str is None else date.fromisoformat(date_str)
    if target > date.today():
        raise HTTPException(
            status_code=400,
            detail=f"No data for future date {target.isoformat()}",
        )
    return target

# for checking/comparing API key validity 
def _fixed_equals(left: str, right: str) -> bool:
    a, b = left.encode("utf-8"), right.encode("utf-8")
    if len(a) != len(b):
        return False
    return hmac.compare_digest(a, b)

def require_bridge_api_key(x_api_key = Header(default=None, alias="X-API-Key")):
    if not BRIDGE_API_KEY.strip():
        raise HTTPException(status_code=503, detail="Bridge API key is not configured")
    if not _fixed_equals(x_api_key or "", BRIDGE_API_KEY):
        raise HTTPException(status_code=401, detail="Invalid API key")
 

@app.get("/garmin/stress")
def get_stress(
        response: Response,
        date_str: str | None = Query(None, alias="date", description="YYYY-MM-DD; default today"),
        _auth=Depends(require_bridge_api_key),
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
        _auth=Depends(require_bridge_api_key),
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
        _auth=Depends(require_bridge_api_key),
):
    target = resolve_date(date_str)
    client = get_garmin_client()
    data = client.get_sleep_data(target.isoformat())

    daily = (data or {}).get("dailySleepDTO") or {}
    if not data or daily.get("sleepTimeSeconds") is None:
        response.status_code = 204
        return None
    return data