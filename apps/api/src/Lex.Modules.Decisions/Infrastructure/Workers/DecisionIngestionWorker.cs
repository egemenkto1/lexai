using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Lex.Modules.Decisions.Domain.Gateways;
using Lex.Modules.Search.Domain.Gateways;
using Lex.Modules.Decisions.Domain.Decisions.Events;
using Lex.Modules.Decisions.Application.Services;
using Lex.Modules.Decisions.Domain.ValueObjects;
using System.Text.RegularExpressions;

namespace Lex.Modules.Decisions.Infrastructure.Workers;

public class DecisionIngestionWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IMessageBusGateway _messageBus;
    private readonly ILogger<DecisionIngestionWorker> _logger;

    public DecisionIngestionWorker(
        IServiceProvider serviceProvider, 
        IMessageBusGateway messageBus,
        ILogger<DecisionIngestionWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _messageBus = messageBus;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🚀 Arka Plan İşçisi (Worker) RabbitMQ'yu dinlemeye başladı...");

        await _messageBus.ConsumeAsync<DecisionIngestedEvent>(
            queueName: "RawDecisionsQueue",
            onMessageReceived: async (message) => await ProcessDecisionAsync(message, stoppingToken),
            cancellationToken: stoppingToken);
    }

    private async Task ProcessDecisionAsync(DecisionIngestedEvent message, CancellationToken ct)
    {
        // EL FENERİ BURADA: Mesaj içeri girdiği milisaniye haberimiz olacak!
        _logger.LogInformation($"📥 [RADAR] RabbitMQ'dan mesaj içeri girdi! Esas No: {message?.CaseNumber ?? "BOŞ"}");

        using var scope = _serviceProvider.CreateScope();
        
        var objectStorage = scope.ServiceProvider.GetRequiredService<IObjectStorageGateway>();
        var registrationService = scope.ServiceProvider.GetRequiredService<DecisionRegistrationService>();

        try
        {
            string safeChamberLog = Regex.Replace(message.ChamberCode ?? "Bilinmeyen", @"[^\w\s-]", "");
            if (safeChamberLog.Length > 30) safeChamberLog = safeChamberLog.Substring(0, 30);
            
            // 1. MinIO'ya Yükleme (Dosya formatında kayıt)
            string rawName = $"{message.ChamberCode}_{message.CaseNumber}-{message.DecisionNumber}";
            string safeObjectName = Regex.Replace(rawName, @"[^a-zA-Z0-9\-_.]", "_");
            if (safeObjectName.Length > 100) safeObjectName = safeObjectName.Substring(0, 100);
            safeObjectName += ".txt"; 
            
            await objectStorage.UploadTextAsync("decisions", safeObjectName, message.FullText, ct);

            string formattedCaseNumber = FormatToDddRule(message.CaseNumber);
            string formattedDecisionNumber = FormatToDddRule(message.DecisionNumber);

            // SİHİRLİ DOKUNUŞ: Veritabanı limitine (200) takılmaması için Giyotin!
            string dbChamberName = message.ChamberCode ?? "Bilinmeyen";
            if (dbChamberName.Length > 200) 
            {
                dbChamberName = dbChamberName.Substring(0, 200);
            }

            // 2. PostgreSQL'e Kaydetme (Veritabanı Hiyerarşisi)
            await registrationService.RegisterDecisionAsync(
                courtType: CourtType.Yargitay,
                chamberCode: safeChamberLog,
                caseNumber: formattedCaseNumber, 
                decisionNumber: formattedDecisionNumber, 
                decisionDate: message.DecisionDate, 
                objectStorageKey: safeObjectName,
                chamberName: dbChamberName, // <-- ARTIK PATLAMAYAN, KISALTILMIŞ İSMİ VERİYORUZ
                cancellationToken: ct
            );

            _logger.LogInformation($"✅ MinIO ve DB'ye işlendi: {safeObjectName}");
        }
        // Çift Kayıt Yakalayıcı (Önceki adımda eklediğimiz)
        catch (Microsoft.EntityFrameworkCore.DbUpdateException dbEx) 
            when (dbEx.InnerException is Npgsql.PostgresException pgEx && pgEx.SqlState == "23505")
        {
            _logger.LogWarning($"⚠️ ÇİFT KAYIT ATLANDI: {message.ChamberCode} {message.CaseNumber}-{message.DecisionNumber} veritabanında mevcut.");
        }
        catch (Exception ex)
        {
            _logger.LogError($"❌ HATA ({message.CaseNumber}-{message.DecisionNumber}): {ex.Message}");
            throw; 
        }
    }

    // --- DDD KORUYUCU FONKSİYONU ---
    private string FormatToDddRule(string? input)
    {
        // 1. Boş veya "Bulunamadi" yazıyorsa
        if (string.IsNullOrWhiteSpace(input) || input.Contains("Bulunamadi"))
            return $"9999/{Random.Shared.Next(100000, 999999)}"; // Örn: 9999/481234

        // 2. Alt çizgileri bölüye çevir
        string formatted = input.Replace("_", "/");

        // 3. Son Güvenlik Kontrolü: Gerçekten YYYY/NNNN mi? (Regex kontrolü)
        if (!Regex.IsMatch(formatted, @"^\d{4}/\d+$"))
            return $"9999/{Random.Shared.Next(100000, 999999)}";

        return formatted;
    }
}
