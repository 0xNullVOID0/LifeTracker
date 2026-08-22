FROM python:3.12-slim

WORKDIR /

# install uv
COPY --from=ghcr.io/astral-sh/uv:latest /uv /usr/local/bin/uv

# project metadata
COPY pyproject.toml uv.lock ./

RUN uv sync --frozen --no-dev

# app source
COPY . .

# for storing oauth token
ENV GARMIN_TOKEN_DIR=/data/garminconnect
RUN mkdir -p /data/garminconnect

EXPOSE 9002

CMD ["uv", "run", "uvicorn", "garmin_connector:app", "--host", "0.0.0.0", "--port", "9002"]