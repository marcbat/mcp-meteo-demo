// ========================================
// LOGGER SIMPLIFIÉ POUR STDERR
// ========================================
// Logger simple qui écrit directement sur stderr sans verbosité
// Permet d'avoir des logs propres dans VS Code

public class SimpleLogger
{
    private readonly StreamWriter _writer;
    
    public SimpleLogger(StreamWriter writer) => _writer = writer;
    
    public void LogInfo(string message)
    {
        _writer.WriteLine($"[INFO] {DateTime.Now:HH:mm:ss} {message}");
    }
    
    public void LogDebug(string message)
    {
        _writer.WriteLine($"[DEBUG] {DateTime.Now:HH:mm:ss} {message}");
    }
    
    public void LogError(string message)
    {
        _writer.WriteLine($"[ERROR] {DateTime.Now:HH:mm:ss} {message}");
    }
}
