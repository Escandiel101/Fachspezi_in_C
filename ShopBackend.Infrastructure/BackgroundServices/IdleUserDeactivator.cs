using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ShopBackend.Infrastructure.Data; 
using ShopBackend.Domain.Entities;

namespace ShopBackend.Infrastructure.BackgroundServices
{
    public class IdleUserDeactivator : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly TimeSpan _checkInerval = TimeSpan.FromHours(24);

        public IdleUserDeactivator(IServiceProvider services)
        { 
            _services = services; 
        }

        // proteced wieder nur diese Klasse selbst und ihre Kinder dürfen diese Methode sehen. Override wieder StandardMethode überschreiben.
        // Ein CancellationToken ist hier extrem wichtig, da bei einem ServerShutdown oder hier im VS ctrl+C dann der Schreibprozess ohne den AbbruchToken praktisch eine Vollendung
        // zu erzwingen versuchen würde und worst case das ganze System dann einfriert. Mit diesem Token bricht die Schleife bei Eingang des Token = True ab und der Dienst "stirbt" sauber. 
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Schleife läuft solange der Server läuft und prüft dann eben alle 24h.
            while (!stoppingToken.IsCancellationRequested)
            {
                // eigenen scope bauen:
                using (var scope = _services.CreateScope())
                {
                    // context darf hier aus Laufzeitaspekten nicht im Konstruktor geholt werden, sondern muss so über die Infrastructure.Data gefetched werden.
                    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var idleLimit = DateTime.UtcNow.AddMonths(-12);

                    // Alle User finden, die nicht bereits inaktiv geflagged sind und zusätzlich eben 12 Monate seit dem letzten Login nicht eingeloggt waren.
                    var idleUsers = await context.Users
                        .Where(u => u.Role != UserRole.Inactive && u.LastLogin < idleLimit)
                        .ToListAsync(stoppingToken);

                    if (idleUsers.Any())
                    {
                        foreach (var user in idleUsers)
                        {
                            user.Role = UserRole.Inactive;
                        }

                        // Wieder der stoppingToken, wenn ein abbruch-Signal kommt, schließt sich die Schleife und der Dienst beendet sofort. 
                        // Da SQL ACID Regeln folgt, sollte das exakt mittem im Schreiben passieren, bricht es ab, die DB sieht "oh Partner weg" und macht ein Rollback. 
                        // Ganz oder gar nicht.
                        await context.SaveChangesAsync(stoppingToken);
                    }
                }

                //Nach dem Check 24h Pause. (Serverprobleme, Abbruch etc. Nach Neustart => Uhr 0 und der Vorgang wird direkt wieder eingeleitet)
                await Task.Delay(_checkInerval, stoppingToken);
            }
        }

    }
}
