#include <Arduino.h>
#include <SensirionI2cScd4x.h>
#include <Wire.h>
#include <WiFi.h>
#include "time.h"
#include <HTTPClient.h>
#include <ArduinoJson.h>

#ifdef NO_ERROR
#undef NO_ERROR
#endif
#define NO_ERROR 0

SensirionI2cScd4x sensor;

static char errorMessage[64];
static int16_t error;

const char* ssid     = "default";
const char* password = "default";

const char* apiEndpoint = "http://192.168.1.246:5071/API/room-climate";

// Server and settings for getting current datetime
const char* ntpServer = "pool.ntp.org";
const long  gmtOffset_sec = 3600;       
const int   daylightOffset_sec = 3600; 

unsigned long lastSensorCheck = 0;
const unsigned long sensorInterval = 5000; // check sensor every 5 sec

unsigned long lastSendTime = 0;
const unsigned long sendInterval = 15000; // send every 15 sec

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

    // Wifi setup 
    WiFi.begin(ssid, password);
    while (WiFi.status() != WL_CONNECTED) {
        delay(500);
        Serial.print(".");
    }
    Serial.println("\nWiFi connected");
    Serial.print("IP: ");
    Serial.println(WiFi.localIP());

    // Set time from online server
    configTime(gmtOffset_sec, daylightOffset_sec, ntpServer);

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
    strftime(buffer, maxLen, "%d-%m-%Y %H:%M:%S", &timeinfo);
    Serial.print("Tijd: ");
    Serial.println(buffer);

    return true;
}

// Send room climate data to .NET backend API
void sendClimateData(int co2, double temp, double humidity) {
    if (WiFi.status() != WL_CONNECTED) {
        Serial.println("WiFi connection lost, reconnecting...");
        WiFi.begin(ssid, password);
        unsigned long startAttemptTime = millis();

        while (WiFi.status() != WL_CONNECTED && millis() - startAttemptTime < 10000) {
            delay(500);
            Serial.print(".");
        }
        
        if (WiFi.status() != WL_CONNECTED) {
            Serial.println("\nFailed to reconnect");
            return;
        }
        Serial.println("\nReconnected");
    }

    char timeBuffer[32];
    if (!getLocalTimeString(timeBuffer, sizeof(timeBuffer))) {
        return;
    }

    HTTPClient http;

    // Prevent pbuf leaks by letting http client manage it's connection internally
    http.begin(apiEndpoint);
    http.addHeader("Content-Type", "application/json");

    JsonDocument json;
    json["TimestampString"] = timeBuffer;
    json["CO2"] = co2;
    json["Temperature"] = temp;
    json["Humidity"] = humidity;

    char jsonBuffer[256];
    size_t n = serializeJson(json, jsonBuffer, sizeof(jsonBuffer));

    int httpResponseCode = http.POST((uint8_t*)jsonBuffer, n);

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
