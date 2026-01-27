using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.Globalization;

// ========================================
// PLUGIN MÉTÉO : LOGIQUE MÉTIER
// ========================================
// Ce plugin contient la logique de récupération des données météo
// Il utilise l'API Open-Meteo (gratuite, sans clé API requise)

public class WeatherPlugin
{
    private readonly HttpClient _httpClient;
    
    public WeatherPlugin(HttpClient httpClient) => _httpClient = httpClient;

    // ========================================
    // FONCTION KERNEL : get_weather (météo actuelle)
    // ========================================
    [KernelFunction("get_weather")]
    [Description("Récupère la météo actuelle pour des coordonnées GPS.")]
    public async Task<string> GetWeatherAsync(
        [Description("Latitude du lieu")] double latitude,
        [Description("Longitude du lieu")] double longitude)
    {
        var url = $"https://api.open-meteo.com/v1/forecast?latitude={FormatCoordinate(latitude)}&longitude={FormatCoordinate(longitude)}&current_weather=true";
        return await _httpClient.GetStringAsync(url);
    }

    // ========================================
    // FONCTION KERNEL : get_hourly_forecast (prévisions horaires)
    // ========================================
    [KernelFunction("get_hourly_forecast")]
    [Description("Récupère les prévisions météo horaires pour les 7 prochains jours avec température, humidité, précipitations, vent et nébulosité.")]
    public async Task<string> GetHourlyForecastAsync(
        [Description("Latitude du lieu")] double latitude,
        [Description("Longitude du lieu")] double longitude,
        [Description("Nombre de jours de prévision (1-16, défaut: 7)")] int forecastDays = 7)
    {
        if (forecastDays < 1) forecastDays = 1;
        if (forecastDays > 16) forecastDays = 16;
        
        var url = $"https://api.open-meteo.com/v1/forecast?latitude={FormatCoordinate(latitude)}&longitude={FormatCoordinate(longitude)}" +
                  $"&hourly=temperature_2m,relative_humidity_2m,precipitation,weather_code,wind_speed_10m,wind_direction_10m,cloud_cover" +
                  $"&forecast_days={forecastDays}&timezone=auto";
        return await _httpClient.GetStringAsync(url);
    }

    // ========================================
    // FONCTION KERNEL : get_daily_forecast (prévisions journalières)
    // ========================================
    [KernelFunction("get_daily_forecast")]
    [Description("Récupère les prévisions météo journalières avec températures min/max, précipitations totales, lever/coucher du soleil et index UV.")]
    public async Task<string> GetDailyForecastAsync(
        [Description("Latitude du lieu")] double latitude,
        [Description("Longitude du lieu")] double longitude,
        [Description("Nombre de jours de prévision (1-16, défaut: 7)")] int forecastDays = 7)
    {
        if (forecastDays < 1) forecastDays = 1;
        if (forecastDays > 16) forecastDays = 16;
        
        var url = $"https://api.open-meteo.com/v1/forecast?latitude={FormatCoordinate(latitude)}&longitude={FormatCoordinate(longitude)}" +
                  $"&daily=weather_code,temperature_2m_max,temperature_2m_min,sunrise,sunset,uv_index_max,precipitation_sum,rain_sum,showers_sum,snowfall_sum,wind_speed_10m_max,wind_gusts_10m_max" +
                  $"&forecast_days={forecastDays}&timezone=auto";
        return await _httpClient.GetStringAsync(url);
    }

    // ========================================
    // FONCTION KERNEL : get_air_quality (qualité de l'air)
    // ========================================
    [KernelFunction("get_air_quality")]
    [Description("Récupère les données de qualité de l'air (PM2.5, PM10, NO2, O3, SO2, CO) pour des coordonnées GPS.")]
    public async Task<string> GetAirQualityAsync(
        [Description("Latitude du lieu")] double latitude,
        [Description("Longitude du lieu")] double longitude)
    {
        var url = $"https://air-quality-api.open-meteo.com/v1/air-quality?latitude={FormatCoordinate(latitude)}&longitude={FormatCoordinate(longitude)}" +
                  $"&current=pm10,pm2_5,carbon_monoxide,nitrogen_dioxide,sulphur_dioxide,ozone,us_aqi,european_aqi";
        return await _httpClient.GetStringAsync(url);
    }

    // ========================================
    // FONCTION KERNEL : get_marine_forecast (météo marine)
    // ========================================
    [KernelFunction("get_marine_forecast")]
    [Description("Récupère les prévisions maritimes avec hauteur des vagues, direction, période et température de l'eau pour des coordonnées GPS.")]
    public async Task<string> GetMarineForecastAsync(
        [Description("Latitude du lieu")] double latitude,
        [Description("Longitude du lieu")] double longitude)
    {
        var url = $"https://marine-api.open-meteo.com/v1/marine?latitude={FormatCoordinate(latitude)}&longitude={FormatCoordinate(longitude)}" +
                  $"&current=wave_height,wave_direction,wave_period,ocean_current_velocity,ocean_current_direction" +
                  $"&hourly=wave_height,wave_direction,wave_period,ocean_temperature_80m";
        return await _httpClient.GetStringAsync(url);
    }

    // ========================================
    // FONCTION KERNEL : get_historical_weather (météo historique)
    // ========================================
    [KernelFunction("get_historical_weather")]
    [Description("Récupère les données météo historiques pour une période donnée (format: YYYY-MM-DD).")]
    public async Task<string> GetHistoricalWeatherAsync(
        [Description("Latitude du lieu")] double latitude,
        [Description("Longitude du lieu")] double longitude,
        [Description("Date de début (format: YYYY-MM-DD)")] string startDate,
        [Description("Date de fin (format: YYYY-MM-DD)")] string endDate)
    {
        var url = $"https://archive-api.open-meteo.com/v1/archive?latitude={FormatCoordinate(latitude)}&longitude={FormatCoordinate(longitude)}" +
                  $"&start_date={startDate}&end_date={endDate}" +
                  $"&daily=weather_code,temperature_2m_max,temperature_2m_min,precipitation_sum,rain_sum,snowfall_sum,wind_speed_10m_max" +
                  $"&timezone=auto";
        return await _httpClient.GetStringAsync(url);
    }

    // ========================================
    // FONCTION KERNEL : get_geocoding (géocodage)
    // ========================================
    [KernelFunction("get_geocoding")]
    [Description("Recherche les coordonnées GPS d'une ville par son nom. Retourne latitude, longitude, pays et altitude.")]
    public async Task<string> GetGeocodingAsync(
        [Description("Nom de la ville à rechercher")] string cityName,
        [Description("Nombre de résultats à retourner (1-100, défaut: 5)")] int count = 5)
    {
        if (count < 1) count = 1;
        if (count > 100) count = 100;
        
        var url = $"https://geocoding-api.open-meteo.com/v1/search?name={Uri.EscapeDataString(cityName)}&count={count}&language=fr&format=json";
        return await _httpClient.GetStringAsync(url);
    }

    // ========================================
    // FONCTION KERNEL : get_solar_radiation (rayonnement solaire)
    // ========================================
    [KernelFunction("get_solar_radiation")]
    [Description("Récupère les données de rayonnement solaire et durée d'ensoleillement pour des coordonnées GPS.")]
    public async Task<string> GetSolarRadiationAsync(
        [Description("Latitude du lieu")] double latitude,
        [Description("Longitude du lieu")] double longitude)
    {
        var url = $"https://api.open-meteo.com/v1/forecast?latitude={FormatCoordinate(latitude)}&longitude={FormatCoordinate(longitude)}" +
                  $"&hourly=shortwave_radiation,direct_radiation,diffuse_radiation,direct_normal_irradiance" +
                  $"&daily=sunrise,sunset,sunshine_duration,daylight_duration" +
                  $"&timezone=auto";
        return await _httpClient.GetStringAsync(url);
    }

    // ========================================
    // FONCTION KERNEL : get_agriculture_data (données agricoles)
    // ========================================
    [KernelFunction("get_agriculture_data")]
    [Description("Récupère les données utiles pour l'agriculture: température du sol, humidité du sol, évapotranspiration et probabilité de précipitations.")]
    public async Task<string> GetAgricultureDataAsync(
        [Description("Latitude du lieu")] double latitude,
        [Description("Longitude du lieu")] double longitude)
    {
        var url = $"https://api.open-meteo.com/v1/forecast?latitude={FormatCoordinate(latitude)}&longitude={FormatCoordinate(longitude)}" +
                  $"&hourly=soil_temperature_0cm,soil_temperature_6cm,soil_moisture_0_to_1cm,soil_moisture_1_to_3cm,et0_fao_evapotranspiration,precipitation_probability" +
                  $"&daily=et0_fao_evapotranspiration,precipitation_probability_max" +
                  $"&timezone=auto";
        return await _httpClient.GetStringAsync(url);
    }

    // ========================================
    // HELPER : Formatage des coordonnées
    // ========================================
    private static string FormatCoordinate(double coordinate)
    {
        return coordinate.ToString(CultureInfo.InvariantCulture);
    }
}
