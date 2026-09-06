#include <Arduino.h>
#include <SensirionI2cScd4x.h>
#include <Wire.h>
#include <WiFi.h>
#include "time.h"
#include <HTTPClient.h>
#include <ArduinoJson.h>
#include <LittleFS.h>
#include "secrets.h"

#ifdef NO_ERROR
#undef NO_ERROR
#endif
#define NO_ERROR 0

SensirionI2cScd4x sensor;

static char errorMessage[64];
static int16_t error;

const char* ssid     = WIFI_SSID;
const char* password = WIFI_PASSWORD;

const char* apiEndpoint = API_ENDPOINT;
const char* deviceID = DEVICE_ID;
const char* apiKey = API_KEY;

// Server and settings for getting current datetime
const char* ntpServer = "pool.ntp.org";
const long  gmtOffset_sec = 3600;       
const int   daylightOffset_sec = 3600; 

unsigned long lastSensorCheck = 0;
const unsigned long sensorInterval = 5000; // check sensor every 5 sec

unsigned long lastSendTime = 0;
const unsigned long sendInterval = 15000; // send every 15 sec

const char* apiLogPath = "/api-sends.log";
const size_t apiLogMaxBytes = 64 * 1024; // rotate when larger than 64 KB due to storage limitations on ESP32

// Vars for storing climate sums to calc average
int sampleCount = 0;
long sumCo2 = 0;
double sumTemp = 0.0;
double sumHumidity = 0.0;


void PrintUint64(uint64_t& value) {
    Serial.print("0x");
    Serial.print((uint32_t)(value >> 32), HEX);
    Serial.print((uint32_t)(value & 0xFFFFFFFF), HEX);
}

void setup() {
    Serial.begin(115200);
    while (!Serial) {
        delay(100);
    }

    if (!LittleFS.begin(false) && !LittleFS.begin(true)) {
        Serial.println("LittleFS mount failed; API payloads will not be logged locally");
    } else {
        Serial.println("LittleFS mounted");
    }

    // Wifi setup — do not block forever so the sensor still runs if the API is down
    WiFi.begin(ssid, password);
    unsigned long wifiStart = millis();
    while (WiFi.status() != WL_CONNECTED && millis() - wifiStart < 15000) {
        delay(500);
        Serial.print(".");
    }
    if (WiFi.status() == WL_CONNECTED) {
        Serial.println("\nWiFi connected");
        Serial.print("IP: ");
        Serial.println(WiFi.localIP());
        configTime(gmtOffset_sec, daylightOffset_sec, ntpServer);
    } else {
        Serial.println("\nWiFi not available at boot; will retry on send");
    }

    // Setup climate sensor
    Wire.begin();
    sensor.begin(Wire, SCD41_I2C_ADDR_62);

    uint64_t serialNumber = 0;
    delay(30);
    
    error = sensor.wakeUp();
    if (error != NO_ERROR) {
        errorToString(error, errorMessage, sizeof errorMessage);
        Serial.println(errorMessage);
    }

    error = sensor.stopPeriodicMeasurement();
    if (error != NO_ERROR) {
        errorToString(error, errorMessage, sizeof errorMessage);
        Serial.println(errorMessage);
    }

    error = sensor.reinit();
    if (error != NO_ERROR) {
        errorToString(error, errorMessage, sizeof errorMessage);
        Serial.println(errorMessage);
    }
    
    error = sensor.getSerialNumber(serialNumber);
    if (error != NO_ERROR) {
        errorToString(error, errorMessage, sizeof errorMessage);
        Serial.println(errorMessage);
        return;
    }

    Serial.print("serial number: ");
    PrintUint64(serialNumber);
    Serial.println();

    // Set temp offset 3c higher than default value
    sensor.setTemperatureOffset(7.00);

    error = sensor.startPeriodicMeasurement();
    if (error != NO_ERROR) {
        errorToString(error, errorMessage, sizeof errorMessage);
        Serial.println(errorMessage);
        return;
    }
}

// Char buffers instead of dynamic strings to be heap safe
bool getLocalTimeString(char* buffer, size_t maxLen) {
    struct tm timeinfo;
    if (!getLocalTime(&timeinfo)) {
        Serial.println("Fout bij ophalen tijd");
        return false;
    }
    strftime(buffer, maxLen, "%Y-%m-%dT%H:%M:%SZ", &timeinfo);
    Serial.print("Tijd: ");
    Serial.println(buffer);

    return true;
}

void rotateApiLogIfNeeded() {
    File existing = LittleFS.open(apiLogPath, FILE_READ);
    if (!existing) {
        return;
    }

    size_t size = existing.size();
    existing.close();

    if (size < apiLogMaxBytes) {
        return;
    }

    LittleFS.remove("/api-sends.prev.log");
    LittleFS.rename(apiLogPath, "/api-sends.prev.log");
    Serial.println("Rotated local API log");
}

void logFailedSend(const char* timeBuffer, const char* jsonBuffer, size_t jsonLen, int httpResponseCode, const char* reason) {
    rotateApiLogIfNeeded();

    File logFile = LittleFS.open(apiLogPath, FILE_APPEND);
    if (!logFile) {
        Serial.println("Failed to open local API log");
        return;
    }

    logFile.print("{\"loggedAt\":\"");
    logFile.print(timeBuffer);
    logFile.print("\",\"httpStatus\":");
    logFile.print(httpResponseCode);
    logFile.print(",\"reason\":\"");
    logFile.print(reason);
    logFile.print("\",\"payload\":");
    logFile.write((const uint8_t*)jsonBuffer, jsonLen);
    logFile.println("}");
    logFile.close();
}

void buildClimatePayload(char* jsonBuffer, size_t jsonBufferLen, size_t* jsonLen, const char* timeBuffer, int co2, double temp, double humidity) {
    JsonDocument json;
    json["Timestamp"] = timeBuffer;
    json["CO2"] = co2;
    json["temperature"] = temp;
    json["humidity"] = humidity;
    *jsonLen = serializeJson(json, jsonBuffer, jsonBufferLen);
}

bool reconnectWifi() {
    if (WiFi.status() == WL_CONNECTED) {
        return true;
    }

    Serial.println("WiFi connection lost, reconnecting...");
    WiFi.disconnect();
    WiFi.begin(ssid, password);
    unsigned long startAttemptTime = millis();

    while (WiFi.status() != WL_CONNECTED && millis() - startAttemptTime < 10000) {
        delay(500);
        Serial.print(".");
    }

    if (WiFi.status() != WL_CONNECTED) {
        Serial.println("\nFailed to reconnect");
        return false;
    }

    Serial.println("\nReconnected");
    configTime(gmtOffset_sec, daylightOffset_sec, ntpServer);
    return true;
}

void fillTimeOrUnavailable(char* timeBuffer, size_t maxLen, bool* hasTime) {
    *hasTime = getLocalTimeString(timeBuffer, maxLen);
    if (*hasTime) {
        return;
    }
    strncpy(timeBuffer, "unavailable", maxLen);
    timeBuffer[maxLen - 1] = '\0';
}

// Send room climate data to .NET backend API
void sendClimateData(int co2, double temp, double humidity) {
    bool wifiOk = reconnectWifi();

    char timeBuffer[32];
    bool hasTime = false;
    fillTimeOrUnavailable(timeBuffer, sizeof(timeBuffer), &hasTime);

    char jsonBuffer[256];
    size_t n = 0;
    buildClimatePayload(jsonBuffer, sizeof(jsonBuffer), &n, timeBuffer, co2, temp, humidity);

    if (!wifiOk) {
        logFailedSend(timeBuffer, jsonBuffer, n, 0, "wifi_reconnect_failed");
        return;
    }

    if (!hasTime) {
        logFailedSend(timeBuffer, jsonBuffer, n, 0, "time_unavailable");
        return;
    }

    HTTPClient http;

    // Prevent pbuf leaks by letting http client manage it's connection internally
    http.begin(apiEndpoint);
    http.addHeader("Content-Type", "application/json");
    http.addHeader("X-Device-ID", deviceID);
    http.addHeader("X-API-Key", apiKey);

    int httpResponseCode = http.POST((uint8_t*)jsonBuffer, n);

    // Only log when POST to API failed
    if (httpResponseCode != 200) {
        logFailedSend(timeBuffer, jsonBuffer, n, httpResponseCode, "http");
    }

    if (httpResponseCode > 0) {
        Serial.print("Send averaged data - Code: ");
        Serial.println(httpResponseCode);
    } else {
        Serial.print("ERROR sending: ");
        Serial.println(httpResponseCode);
    }

    http.end();
    
    // Small delay for network stack socket cleanup
    delay(50);
}

void loop() {
    unsigned long currentMillis = millis();

    // Wait for interval to be elapsed before running
    if (currentMillis - lastSensorCheck < sensorInterval) {
        return; 
    }
    lastSensorCheck = currentMillis;

    bool dataReady = false;
    uint16_t co2Concentration = 0;
    float temperature = 0.0;
    float relativeHumidity = 0.0;

    error = sensor.getDataReadyStatus(dataReady);
    if (error != NO_ERROR || !dataReady) {
        return;
    }

    error = sensor.readMeasurement(co2Concentration, temperature, relativeHumidity);
    if (error != NO_ERROR) {
        return;
    }

    Serial.print("CO2 [ppm]: "); Serial.println(co2Concentration);
    Serial.print("Temp [°C]: "); Serial.println(temperature);
    Serial.print("Hum [RH]: ");  Serial.println(relativeHumidity);
    Serial.println();

    // Increment sum data
    sumCo2 += co2Concentration;
    sumTemp += temperature;
    sumHumidity += relativeHumidity;
    sampleCount++;

    // Only send if send interval has passed
    if (currentMillis - lastSendTime >= sendInterval) {
        lastSendTime = currentMillis; 

        // Calc averages
        int avgCo2 = sumCo2 / sampleCount;
        double avgTemp = sumTemp / sampleCount;
        double avgHumidity = sumHumidity / sampleCount;

        sendClimateData(avgCo2, avgTemp, avgHumidity);

        // Reset sums for next batch
        sumCo2 = 0;
        sumTemp = 0.0;
        sumHumidity = 0.0;
        sampleCount = 0;
    }
}