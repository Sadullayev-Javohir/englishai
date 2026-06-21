using Domain.Curriculum;
using Infrastructure.Curriculum;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Web;

internal sealed record CurriculumCommand(string Action, Guid? RunId, bool NewRun, int BatchSize)
{
    public static CurriculumCommand? Parse(string[] args)
    {
        if (args.Length == 0 || !args[0].Equals("curriculum", StringComparison.OrdinalIgnoreCase)) return null;
        if (args.Length < 2) throw new ArgumentException("Supported: curriculum start|resume|pause|status|publish.");
        var action = args[1].ToLowerInvariant(); Guid? runId = null; var fresh = false; var batch = int.MaxValue;
        for (var i = 2; i < args.Length; i++)
        {
            if (args[i] == "--new") fresh = true;
            else if (args[i] == "--run-id" && i + 1 < args.Length && Guid.TryParse(args[++i], out var id)) runId = id;
            else if (args[i] == "--batch-size" && i + 1 < args.Length && int.TryParse(args[++i], out var size)) batch = size;
            else throw new ArgumentException($"Unsupported curriculum option: {args[i]}");
        }
        return new(action, runId, fresh, batch);
    }

    public static async Task<int> RunAsync(IServiceProvider services, CurriculumCommand command,
        ILogger logger, CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<EnglishAiDbContext>();
            await DatabaseInitializer.ValidateSchemaAsync(db, cancellationToken);
            var service = scope.ServiceProvider.GetRequiredService<CurriculumBackfillService>();
            switch (command.Action)
            {
                case "start":
                    var runId = await service.StartOrResumeAsync(command.NewRun, cancellationToken);
                    logger.LogInformation("Curriculum run {RunId} stopped or completed.", runId); break;
                case "resume":
                    if (command.RunId is null) throw new ArgumentException("--run-id is required.");
                    while (await service.RunOneBatchAsync(command.RunId.Value, command.BatchSize, cancellationToken) > 0) { }
                    break;
                case "pause":
                    if (command.RunId is null) throw new ArgumentException("--run-id is required.");
                    await service.PauseAsync(command.RunId.Value, cancellationToken); break;
                case "reset-failed":
                    if (command.RunId is null) throw new ArgumentException("--run-id is required.");
                    Console.WriteLine($"Reset {await service.ResetFailedAsync(command.RunId.Value, cancellationToken)} failed items."); break;
                case "revalidate":
                    if (command.RunId is null) throw new ArgumentException("--run-id is required.");
                    Console.WriteLine($"Reset {await service.RevalidateAsync(command.RunId.Value, cancellationToken)} invalid approved items."); break;
                case "publish":
                    if (command.RunId is null) throw new ArgumentException("--run-id is required.");
                    await scope.ServiceProvider.GetRequiredService<CurriculumPublisher>().PublishAsync(command.RunId.Value, cancellationToken); break;
                case "status":
                    var run = command.RunId is { } id
                        ? await db.CurriculumGenerationRuns.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
                        : await db.CurriculumGenerationRuns.OrderByDescending(x => x.CreatedAt).FirstOrDefaultAsync(cancellationToken);
                    if (run is null) { Console.WriteLine("No curriculum run."); break; }
                    var counts = await db.CurriculumGenerationItems.Where(x => x.RunId == run.Id)
                        .GroupBy(x => x.Status).Select(x => new { Status = x.Key, Count = x.Count() }).ToListAsync(cancellationToken);
                    Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new { run.Id, run.Version, run.Provider, run.Model, status = run.Status.ToString(), counts }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
                    break;
                default: throw new ArgumentException("Supported: curriculum start|resume|pause|reset-failed|revalidate|status|publish.");
            }
            return 0;
        }
        catch (Exception ex) { logger.LogCritical(ex, "Curriculum command failed."); return 1; }
    }
}
