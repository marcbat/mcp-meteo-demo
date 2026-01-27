# MCP Météo Demo

Serveur MCP (Model Context Protocol) pour VS Code fournissant des données météorologiques via l'API Open-Meteo.

## Description

Ce projet est un serveur MCP .NET 8.0 qui s'intègre à VS Code pour fournir des informations météorologiques en temps réel. Il utilise Semantic Kernel pour orchestrer les différentes fonctions et communique avec VS Code via le protocole JSON-RPC sur stdin/stdout.

## Fonctionnalités

Le serveur expose 9 outils météo :

- **`get_weather`** - Météo actuelle (température, vent, conditions)
- **`get_hourly_forecast`** - Prévisions horaires sur 7-16 jours
- **`get_daily_forecast`** - Prévisions journalières (min/max, UV, lever/coucher du soleil)
- **`get_air_quality`** - Qualité de l'air (PM2.5, PM10, NO2, O3, etc.)
- **`get_marine_forecast`** - Conditions marines (vagues, courants, température de l'eau)
- **`get_historical_weather`** - Données historiques entre deux dates
- **`get_geocoding`** - Recherche de coordonnées GPS par nom de ville
- **`get_solar_radiation`** - Rayonnement solaire et ensoleillement
- **`get_agriculture_data`** - Données agricoles (humidité du sol, évapotranspiration)

## Technologies

- **.NET 8.0** - Framework de base
- **Semantic Kernel 1.0.1** - Orchestration des plugins IA
- **Open-Meteo API** - Source de données météo (gratuite, sans clé)
- **MCP Protocol** - Communication avec VS Code

## Structure du projet

```
├── Program.cs          # Point d'entrée et boucle JSON-RPC
├── McpDispatcher.cs    # Gestion du protocole MCP
├── WeatherPlugin.cs    # 9 fonctions météo
└── SimpleLogger.cs     # Logger personnalisé (stderr)
```

## Utilisation

1. Compiler le projet :
   ```bash
   dotnet build
   ```

2. Configurer dans VS Code (settings MCP)

3. Le serveur démarre automatiquement et expose ses outils à l'assistant Copilot

## Exemple

```
Utilisateur : "Quel temps fait-il à New York ?"
Copilot : [Appelle get_weather avec lat=40.71, lon=-74.01]
Résultat : Température -8.8°C, vent 18.5 km/h, ciel couvert
```

## Licence

Projet de démonstration - Libre d'utilisation
