using DemoFile;
using DemoFile.Sdk;
using HighlightReel.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace HighlightReel.Services
{
    internal record KillEvent(string PlayerName, int Tick, int Round, string Weapon);
    internal record DamageEvent(string PlayerName, int Tick, int Damage);

    public static class HighlightFinder
    {
        private const int TickRate = 64;
        private const int MaxTickGap = 10 * TickRate;
        private const int MinKills = 4;

        public static async Task<List<Highlight>> FindHighlightsAsync(string demoPath)
        {
            var allKills = new List<KillEvent>();
            var allDamage = new List<DamageEvent>();
            var demo = new CsDemoParser();
            int currentRound = 0;

            demo.Source1GameEvents.RoundStart += e => currentRound++;

            demo.Source1GameEvents.PlayerDeath += e =>
            {
                var attackerName = e.Attacker?.PlayerName;
                if (attackerName != null)
                {
                    allKills.Add(new KillEvent(
                        attackerName,
                        demo.CurrentDemoTick.Value,
                        currentRound,
                        e.Weapon ?? "unknown"
                    ));
                }
            };

            demo.Source1GameEvents.PlayerHurt += e =>
            {
                var attackerName = e.Attacker?.PlayerName;
                if (attackerName != null && e.Player != null && attackerName != e.Player.PlayerName)
                {
                    allDamage.Add(new DamageEvent(
                        attackerName,
                        demo.CurrentDemoTick.Value,
                        e.DmgHealth + e.DmgArmor
                    ));
                }
            };

            using var stream = File.OpenRead(demoPath);
            var reader = DemoFileReader.Create(demo, stream);
            await reader.ReadAllAsync();

            return ExtractHighlights(allKills, allDamage);
        }

        private static List<Highlight> ExtractHighlights(List<KillEvent> allKills, List<DamageEvent> allDamage)
        {
            var highlights = new List<Highlight>();
            var killsByPlayer = allKills.GroupBy(k => k.PlayerName);

            foreach (var playerKills in killsByPlayer)
            {
                var sortedKills = playerKills.OrderBy(k => k.Tick).ToList();
                if (sortedKills.Count < MinKills) continue;

                var currentStreak = new List<KillEvent>();
                foreach (var kill in sortedKills)
                {
                    if (!currentStreak.Any() || kill.Tick - currentStreak.Last().Tick <= MaxTickGap)
                    {
                        currentStreak.Add(kill);
                    }
                    else
                    {
                        AddHighlightIfValid(highlights, currentStreak, allDamage);
                        currentStreak = new List<KillEvent> { kill };
                    }
                }
                AddHighlightIfValid(highlights, currentStreak, allDamage);
            }

            return highlights.OrderBy(h => h.StartTick).ToList();
        }

        private static void AddHighlightIfValid(List<Highlight> highlights, List<KillEvent> streak, List<DamageEvent> allDamage)
        {
            if (streak.Count < MinKills) return;

            var firstKill = streak.First();
            var lastKill = streak.Last();

            int totalDamageInStreak = allDamage
                .Where(d => d.PlayerName == firstKill.PlayerName && d.Tick >= firstKill.Tick && d.Tick <= lastKill.Tick)
                .Sum(d => d.Damage);

            var weaponsUsed = streak.Select(k => k.Weapon.Replace("weapon_", "")).Distinct();

            highlights.Add(new Highlight
            {
                PlayerName = firstKill.PlayerName,
                StartTick = firstKill.Tick,
                EndTick = lastKill.Tick,
                KillCount = streak.Count,
                RoundNumber = firstKill.Round,
                TotalDamage = totalDamageInStreak,
                WeaponsUsed = string.Join(", ", weaponsUsed)
            });
        }
    }
}