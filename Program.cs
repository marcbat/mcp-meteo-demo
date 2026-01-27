using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using System.Text.Json;
using System.Text.Json.Nodes;

// ========================================
// 1. CONFIGURATION DE L'HÔTE ET DU KERNEL
// ========================================
// On crée un hôte d'application pour gérer l'injection de dépendances et le cycle de vie
var builder = Host.CreateApplicationBuilder(args);

// Configuration des logs pour utiliser stderr au lieu de stdout
// IMPORTANT : stdout est réservé pour JSON-RPC, stderr est libre pour les logs
// VS Code affiche stderr dans la fenêtre de logs du serveur MCP
// On désactive le logger par défaut pour utiliser notre propre logger simplifié
builder.Logging.ClearProviders();

// Configuration du HttpClient pour les appels API externes (Open-Meteo)
builder.Services.AddHttpClient();

// Enregistrement du plugin météo comme service
// Permet l'injection de dépendances et la réutilisation
builder.Services.AddTransient<WeatherPlugin>();

// Configuration de Semantic Kernel avec nos plugins
// Le Kernel est le moteur qui va orchestrer l'exécution de nos outils
builder.Services.AddTransient(sp => 
{
    var kernelBuilder = Kernel.CreateBuilder();
    // Enregistrement du plugin météo qui contient notre fonction get_weather
    // On utilise le service pour bénéficier de l'injection de dépendances
    kernelBuilder.Plugins.AddFromObject(sp.GetRequiredService<WeatherPlugin>());
    return kernelBuilder.Build();
});

// Construction de l'hôte et récupération du Kernel configuré
var host = builder.Build();
var kernel = host.Services.GetRequiredService<Kernel>();

// ========================================
// 2. CONFIGURATION DES FLUX DE COMMUNICATION
// ========================================
// Le protocole MCP utilise stdin/stdout pour communiquer avec VS Code
// Les messages sont au format JSON-RPC, un message par ligne
// On crée un logger simplifié qui écrit directement sur stderr
var logWriter = new StreamWriter(Console.OpenStandardError()) { AutoFlush = true };
var simpleLogger = new SimpleLogger(logWriter);

var dispatcher = new McpDispatcher(kernel, simpleLogger);

simpleLogger.LogInfo("========================================");
simpleLogger.LogInfo("[MCP] Serveur MCP Météo démarré");
simpleLogger.LogInfo("[MCP] En attente de connexion de VS Code...");
simpleLogger.LogInfo("========================================");
using var reader = new StreamReader(Console.OpenStandardInput());
using var writer = new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true };

// ========================================
// 3. BOUCLE PRINCIPALE DE TRAITEMENT DES MESSAGES
// ========================================
// On lit les messages JSON-RPC ligne par ligne depuis stdin
while (true)
{
    // Lecture d'une ligne depuis stdin (message JSON-RPC envoyé par VS Code)
    var line = await reader.ReadLineAsync();
    if (line == null) break; // Fin du stream = VS Code a fermé la connexion

    try
    {
        // ========================================
        // 3.1. PARSING DU MESSAGE JSON-RPC
        // ========================================
        // Un message JSON-RPC contient : jsonrpc, id, method, params
        var request = JsonNode.Parse(line);
        var method = request?["method"]?.GetValue<string>();
        var id = request?["id"];
        var paramsNode = request?["params"];

        object? result = null;

        // ========================================
        // 3.2. ROUTAGE DES MÉTHODES MCP
        // ========================================
        
        if (method == "initialize")
        {
            // INITIALIZE : Première méthode appelée par VS Code au démarrage
            // On retourne les capacités du serveur (ici : tools uniquement)
            result = dispatcher.Initialize();
        }
        else if (method == "tools/list")
        {
            // TOOLS/LIST : VS Code demande la liste des outils disponibles
            // On retourne la description de chaque outil avec son schéma d'entrée
            result = dispatcher.ListTools();
        }
        else if (method == "tools/call")
        {
            // TOOLS/CALL : VS Code demande l'exécution d'un outil
            // On extrait le nom de l'outil et ses arguments
            var name = paramsNode?["name"]?.GetValue<string>() ?? "";
            var arguments = paramsNode?["arguments"]?.Deserialize<Dictionary<string, object>>() ?? new();
            
            // Exécution de l'outil via le dispatcher
            result = await dispatcher.CallToolAsync(name, arguments);
        }

        // ========================================
        // 3.3. ENVOI DE LA RÉPONSE JSON-RPC
        // ========================================
        // Format de réponse JSON-RPC : { jsonrpc, id, result }
        var response = new
        {
            jsonrpc = "2.0",
            id = id?.GetValue<int>(),
            result
        };

        // Envoi de la réponse sur stdout (une ligne JSON)
        await writer.WriteLineAsync(JsonSerializer.Serialize(response));
    }
    catch (Exception ex)
    {
        // ========================================
        // 3.4. GESTION DES ERREURS
        // ========================================
        // En cas d'erreur, on renvoie une réponse d'erreur JSON-RPC
        // Code -32603 = Internal error (erreur serveur)
        var errorResponse = new
        {
            jsonrpc = "2.0",
            id = (int?)null,
            error = new { code = -32603, message = ex.Message }
        };
        await writer.WriteLineAsync(JsonSerializer.Serialize(errorResponse));
    }
}