﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Milky.OsuPlayer.Common;
using Milky.OsuPlayer.Media.Audio.Sounds;
using OSharp.Beatmap;
using OSharp.Beatmap.Sections.Timing;

namespace Milky.OsuPlayer.Media.Audio.TrackProvider
{
    class NightcoreTilingTrackProvider : TrackProviderBase
    {
        private static readonly string NC_FINISH = Path.Combine(Domain.DefaultPath, "nightcore-finish.wav");
        private static readonly string NC_KICK = Path.Combine(Domain.DefaultPath, "nightcore-kick.wav");
        private static readonly string NC_CLAP = Path.Combine(Domain.DefaultPath, "nightcore-clap.wav");

        private struct RhythmGroup
        {
            public RhythmGroup(int periodCount, int loopCount, (string, int)[] relativeNode)
            {
                PeriodCount = periodCount;
                LoopCount = loopCount;
                RelativeNodes = relativeNode;
            }

            public int PeriodCount { get; set; }

            public int LoopCount { get; set; }

            public (string fileName, int skipRhythm)[] RelativeNodes { get; set; }
        }

        private readonly Dictionary<int, RhythmGroup> _rhythmDeclarations =
            new Dictionary<int, RhythmGroup>
            {
                [3] = new RhythmGroup(6, 4, new[]
                {
                     (NC_KICK, 2),
                     (NC_CLAP, 1),
                     (NC_KICK, 2),
                     (NC_CLAP, 1),
                }),
                [4] = new RhythmGroup(8, 4, new[]
                {
                     (NC_KICK, 2),
                     (NC_CLAP, 2),
                     (NC_KICK, 2),
                     (NC_CLAP, 2),
                }),
                [5] = new RhythmGroup(5, 8, new[]
                {
                     (NC_KICK, 2),
                     (NC_CLAP, 2),
                     (NC_KICK, 1),
                }),
                [6] = new RhythmGroup(6, 8, new[]
                {
                     (NC_KICK, 2),
                     (NC_CLAP, 2),
                     (NC_KICK, 2),
                }),
                [7] = new RhythmGroup(7, 8, new[]
                {
                     (NC_KICK, 2),
                     (NC_CLAP, 2),
                     (NC_KICK, 2),
                     (NC_CLAP, 1),
                })
            };

        public override IEnumerable<SoundElement> GetSoundElements()
        {
            var timingSection = OsuFile.TimingPoints;
            var redLines = timingSection.TimingList.Where(k => !k.Inherit);
            var allTimings = timingSection.GetInterval(0.5);
            var redlineGroups = redLines
                .Select(k =>
                    (k, allTimings.FirstOrDefault(o => Math.Abs(o.Key - k.Offset) < 0.001).Value)
                )
                .ToList();

            var maxTime = Math.Max(Math.Max(ComponentPlayer.Current.MusicPlayer.Duration, OsuFile.HitObjects.MaxTime),
                timingSection.MaxTime);
            var hitsoundList = new List<SpecificFileSoundElement>();

            for (int i = 0; i < redlineGroups.Count; i++)
            {
                var (currentLine, interval) = redlineGroups[i];
                var startTime = currentLine.Offset;
                var endTime = i == redlineGroups.Count - 1 ? maxTime : redlineGroups[i + 1].k.Offset;
                var rhythm = currentLine.Rhythm;

                double period; // 一个周期的1/2数量
                double loopCount; // 周期总数
                double currentTime = startTime;

                if (!_rhythmDeclarations.ContainsKey(rhythm))
                {
                    rhythm = 4;
                }

                var ncRhythm = _rhythmDeclarations[rhythm];
                period = ncRhythm.PeriodCount * interval;
                loopCount = ncRhythm.LoopCount;
                var exit = false;
                while (!exit)
                {
                    for (int j = 0; j < loopCount; j++)
                    {
                        if (exit) break;
                        if (j == 0) hitsoundList.Add(GetHitsoundAndSkip(ref currentTime, 0, NC_FINISH));
                        foreach (var (fileName, skipRhythm) in ncRhythm.RelativeNodes)
                        {
                            if (exit) break;
                            var ele = GetHitsoundAndSkip(ref currentTime, interval * skipRhythm, fileName);
                            if (ele.Offset >= endTime)
                            {
                                exit = true;
                                break;
                            }

                            hitsoundList.Add(ele);
                        }
                    }
                }
            }

            return hitsoundList;
        }

        private SpecificFileSoundElement GetHitsoundAndSkip(ref double currentTime, double skipTime, string fileName)
        {
            var ele = new SpecificFileSoundElement(1, 0, fileName, currentTime);
            currentTime += skipTime;
            return ele;
        }

        public NightcoreTilingTrackProvider(OsuFile osuFile) : base(osuFile)
        {
        }
    }
}
