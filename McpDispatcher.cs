using Microsoft.SemanticKernel;
using System.Text.Json;
using System.Text.Json.Serialization;

// ========================================
// CLASSES DE RÉPONSE MCP (pour sérialisation correcte)
// ========================================
public class InitializeResponse
{
    [JsonPropertyName("protocolVersion")]
    public string ProtocolVersion { get; set; } = "2024-11-05";
    
    [JsonPropertyName("capabilities")]
    public Capabilities Capabilities { get; set; } = new();
    
    [JsonPropertyName("serverInfo")]
    public ServerInfo ServerInfo { get; set; } = new();
}

public class Capabilities
{
    [JsonPropertyName("tools")]
    public object Tools { get; set; } = new { };
}

public class ServerInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";
    
    [JsonPropertyName("version")]
    public string Version { get; set; } = "";
}

// ========================================
// DISPATCHER : GESTION DU PROTOCOLE MCP
// ========================================
// Cette classe orchestre la communication entre VS Code et Semantic Kernel
// Elle implémente les 3 méthodes principales du protocole MCP

public class McpDispatcher
{
    private readonly Kernel _kernel;
    private readonly SimpleLogger _logger;
    
    public McpDispatcher(Kernel kernel, SimpleLogger logger)
    {
        _kernel = kernel;
        _logger = logger;
    }

    // ========================================
    // INITIALIZE : Handshake initial avec VS Code
    // ========================================
    // VS Code appelle cette méthode au démarrage pour connaître :
    // - La version du protocole supportée
    // - Les capacités du serveur (tools, resources, prompts, etc.)
    // - Les informations du serveur (nom, version)
    public object Initialize()
    {
        _logger.LogInfo("[MCP] Initialize appelé - Handshake avec VS Code");
        var response = new InitializeResponse
        {
            ProtocolVersion = "2024-11-05",
            Capabilities = new Capabilities { Tools = new { } },
            ServerInfo = new ServerInfo 
            { 
                Name = "meteo-dotnet",
                Version = "1.0.0"
            }
        };
        _logger.LogInfo($"[MCP] Initialize - Réponse envoyée avec {_kernel.Plugins.SelectMany(p => p).Count()} outil(s)");
        return response;
    }

    // ========================================
    // TOOLS/LIST : Liste des outils disponibles
    // ========================================
    // VS Code appelle cette méthode pour obtenir la liste des outils
    // Au lieu de définir manuellement le schéma, on l'extrait automatiquement
    // depuis les métadonnées Semantic Kernel (attributs [Description])
    public object ListTools()
    {
        _logger.LogInfo("[MCP] tools/list appelé - Liste des outils disponibles");
        
        // On parcourt tous les plugins et leurs fonctions enregistrées
        var tools = _kernel.Plugins
            .SelectMany(plugin => plugin)
            .Select(function => new
            {
                name = function.Name,
                description = function.Description,
                // Génération du schéma JSON depuis les métadonnées de la fonction
                inputSchema = new
                {
                    type = "object",
                    properties = function.Metadata.Parameters.ToDictionary(
                        param => param.Name,
                        param => new
                        {
                            type = GetJsonType(param.ParameterType),
                            description = param.Description ?? ""
                        }
                    ),
                    required = function.Metadata.Parameters
                        .Where(p => p.IsRequired)
                        .Select(p => p.Name)
                        .ToArray()
                }
            })
            .ToArray();

        _logger.LogInfo($"[MCP] tools/list - {tools.Length} outil(s) retourné(s): {string.Join(", ", tools.Select(t => t.name))}");
        
        return new { tools };
    }

    // ========================================
    // HELPER : Conversion Type .NET -> Type JSON Schema
    // ========================================
    private static string GetJsonType(Type? type)
    {
        if (type == null) return "string";
        if (type == typeof(int) || type == typeof(long) || type == typeof(double) || type == typeof(float) || type == typeof(decimal))
            return "number";
        if (type == typeof(bool)) return "boolean";
        if (type == typeof(string)) return "string";
        return "object";
    }

    // ========================================
    // TOOLS/CALL : Exécution d'un outil
    // ========================================
    // VS Code appelle cette méthode pour exécuter un outil spécifique
    // Paramètres :
    // - name : le nom de l'outil à exécuter
    // - arguments : dictionnaire des arguments (ex: latitude, longitude)
    public async Task<object> CallToolAsync(string name, Dictionary<string, object> arguments)
    {
        _logger.LogInfo($"[MCP] tools/call appelé - Outil: {name}, Arguments: {string.Join(", ", arguments.Select(a => $"{a.Key}={a.Value}"))}");
        
        // ========================================
        // RECHERCHE DYNAMIQUE DE LA FONCTION
        // ========================================
        // Au lieu de hardcoder "get_weather", on cherche la fonction par son nom
        // Cela rend le code générique et extensible
        var function = _kernel.Plugins
            .SelectMany(p => p)
            .FirstOrDefault(f => f.Name == name);

        if (function == null)
        {
            _logger.LogError($"[MCP] Outil '{name}' introuvable");
            throw new Exception($"Tool '{name}' not found");
        }

        // ========================================
        // CONVERSION DES ARGUMENTS
        // ========================================
        // Les arguments arrivent en tant que JsonElement
        // On doit les convertir vers les types attendus par Semantic Kernel
        var kernelArgs = new KernelArguments();
        
        foreach (var arg in arguments)
        {
            // Si c'est un nombre JSON, on le convertit en double
            if (arg.Value is JsonElement element && element.ValueKind == JsonValueKind.Number)
                kernelArgs[arg.Key] = element.GetDouble();
            else
                kernelArgs[arg.Key] = arg.Value;
        }

        // ========================================
        // INVOCATION DU PLUGIN VIA SEMANTIC KERNEL
        // ========================================
        // On utilise InvokeAsync avec la fonction trouvée dynamiquement
        // Semantic Kernel gère automatiquement la conversion des types
        _logger.LogDebug($"[MCP] Invocation de la fonction '{function.Name}'...");
        var result = await function.InvokeAsync(_kernel, kernelArgs);
        _logger.LogInfo($"[MCP] Fonction '{function.Name}' exécutée avec succès");
        
        // ========================================
        // FORMAT DE RETOUR MCP
        // ========================================
        // Le protocole MCP attend un objet avec :
        // - content : tableau de contenus (texte, image, etc.)
        // - isError : booléen indiquant si c'est une erreur
        return new { 
            content = new[] { 
                new { 
                    type = "text",           // Type de contenu : texte brut
                    text = result.ToString() // Résultat de l'exécution
                } 
            },
            isError = false  // Pas d'erreur
        };
    }
}
