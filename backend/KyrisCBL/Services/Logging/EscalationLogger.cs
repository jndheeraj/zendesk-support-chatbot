using KyrisCBL.Config;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace KyrisCBL.Services.Logging
{
    public class EscalationLogger
    {
        private readonly ILogger<EscalationLogger> _logger;
        private readonly string _logFilePath;

        public EscalationLogger(ILogger<EscalationLogger> logger, IOptions<LoggingSettings> loggingSettings)
        {
            _logger = logger;

            var logsDir = loggingSettings.Value.EscalationLogPath;
            Directory.CreateDirectory(logsDir);

            _logFilePath = Path.Combine(logsDir, "escalations.jsonl");
        }

        public void Log(string userInput, string botResponse, string business)
        {
            // 1. Log to standard output (e.g., console, file via ILogger)
            _logger.LogWarning("Escalation triggered for {Business} | Input: {UserInput} | GPT Response: {BotResponse}",
                business, userInput, botResponse);

            // 2. Also persist to JSON file
            var logEntry = new
            {
                timestamp = DateTime.UtcNow,
                business,
                userInput,
                botResponse
            };

            var json = JsonSerializer.Serialize(logEntry) + Environment.NewLine;

            File.AppendAllText(_logFilePath, json);
        }
    }
}
