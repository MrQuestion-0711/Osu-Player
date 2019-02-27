﻿using Milky.OsuPlayer;
using Milky.OsuPlayer.Data.EF.Model;
using Milky.OsuPlayer.Models;
using Milky.OsuPlayer.Utils;
using OSharp.Beatmap;
using OSharp.Common;
using osu.Shared;
using osu.Shared.Serialization;
using osu_database_reader.BinaryFiles;
using osu_database_reader.Components.Beatmaps;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Milky.OsuPlayer.Data
{
    internal static class BeatmapEntryQuery
    {
        public static OsuDb BeatmapDb { get; set; }

        private static readonly ConcurrentRandom Random = new ConcurrentRandom();

        public static IEnumerable<BeatmapEntry> ByTitleArtist(this IEnumerable<BeatmapEntry> list, string title,
            string artist)
        {
            return list
                .Where(k => k.Title != null && k.Title == title ||
                            k.TitleUnicode != null && k.TitleUnicode == title)
                .Where(k => k.Artist != null && k.Artist == artist ||
                            k.ArtistUnicode != null && k.ArtistUnicode == artist);
        }

        public static IEnumerable<BeatmapEntry> ByKeyword(this IEnumerable<BeatmapEntry> list, string keywordStr)
        {
            if (string.IsNullOrWhiteSpace(keywordStr))
                return list;
            keywordStr = keywordStr.ToLower();
            string[] keywords = keywordStr.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
            return keywords.Aggregate(list,
                (current, keywd) => current.Where(k =>
                    k.Title != null && k.Title.ToLower().Contains(keywd) ||
                    k.TitleUnicode != null && k.TitleUnicode.Contains(keywd) ||
                    k.Artist != null && k.Artist.ToLower().Contains(keywd) ||
                    k.ArtistUnicode != null && k.ArtistUnicode.Contains(keywd) ||
                    k.SongTags != null && k.SongTags.ToLower().Contains(keywd) ||
                    k.SongSource != null && k.SongSource.ToLower().Contains(keywd) ||
                    k.Creator != null && k.Creator.ToLower().Contains(keywd) ||
                    k.Version != null && k.Version.ToLower().Contains(keywd)
                ));
        }

        public static IEnumerable<BeatmapEntry> ByFolder(this IEnumerable<BeatmapEntry> list,
            string folder)
        {
            return list.Where(k => k.FolderName == folder);
        }

        public static BeatmapEntry ByIdentity(this IEnumerable<BeatmapEntry> list,
            MapIdentity identity)
        {
            return list.Where(k => k != null).FirstOrDefault(k => k.FolderName == identity.FolderName && k.Version == identity.Version);
        }

        public static IEnumerable<BeatmapEntry> ByIdentities(this IEnumerable<BeatmapEntry> list,
            IEnumerable<MapIdentity> identities)
        {
            return identities.Select(id => list.FirstOrDefault(k => k.FolderName == id.FolderName && k.Version == id.Version));
        }

        public static BeatmapEntry GetHighestDiff(this IEnumerable<BeatmapEntry> enumerable)
        {
            var dictionary = enumerable.GroupBy(k => k.GameMode).ToDictionary(k => k.Key, k => k.ToList());
            if (dictionary.ContainsKey(GameMode.Standard))
            {
                if (dictionary[GameMode.Standard].All(k => k.DiffStarRatingStandard.ContainsKey(Mods.None)))
                    return dictionary[GameMode.Standard].OrderBy(k => k.DiffStarRatingStandard[Mods.None]).Last();
            }
            if (dictionary.ContainsKey(GameMode.Mania))
            {
                if (dictionary[GameMode.Mania].All(k => k.DiffStarRatingMania.ContainsKey(Mods.None)))
                    return dictionary[GameMode.Mania].OrderBy(k => k.DiffStarRatingMania[Mods.None]).Last();
            }

            if (dictionary.ContainsKey(GameMode.CatchTheBeat))
            {
                if (dictionary[GameMode.CatchTheBeat].All(k => k.DiffStarRatingCtB.ContainsKey(Mods.None)))
                    return dictionary[GameMode.CatchTheBeat].OrderBy(k => k.DiffStarRatingCtB[Mods.None]).Last();
            }

            if (dictionary.ContainsKey(GameMode.Taiko))
            {
                if (dictionary[GameMode.Taiko].All(k => k.DiffStarRatingTaiko.ContainsKey(Mods.None)))
                    return dictionary[GameMode.Taiko].OrderBy(k => k.DiffStarRatingTaiko[Mods.None]).Last();
            }

            Console.WriteLine(@"Get highest difficulty failed.");
            var randKey = dictionary.Keys.ToList()[Random.Next(dictionary.Keys.Count)];
            return dictionary[randKey][dictionary[randKey].Count];
            //enumerable.ToList()[Random.Next(enumerable.Count())];
        }

        public static IEnumerable<BeatmapEntry> GetRecentListFromDb(
            this IEnumerable<BeatmapEntry> list)
        {
            var recent = DbOperate.GetRecent().ToList();
            return recent.ToBeatmapEntries(list);
        }

        public static IEnumerable<BeatmapEntry> SortBy(this IEnumerable<BeatmapEntry> list, SortMode sortMode)
        {
            switch (sortMode)
            {
                case SortMode.Artist:
                default:
                    return list.OrderBy(k => MetaString.GetUnicode(k.Artist, k.ArtistUnicode),
                        StringComparer.InvariantCulture);
                case SortMode.Title:
                    return list.OrderBy(k => MetaString.GetUnicode(k.Title, k.TitleUnicode),
                        StringComparer.InvariantCulture);
            }
        }
        
        public static async Task<bool> TryLoadNewDbAsync(string path)
        {
            try
            {
                await LoadNewDbAsync(path);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static async Task LoadNewDbAsync(string path)
        {
            var db = await ReadDbAsync(path);
            BeatmapDb = db;
            App.SaveConfig();
        }

        private static async Task<OsuDb> ReadDbAsync(string path)
        {
            return await Task.Run(() =>
            {
                var db = new OsuDb();
                using (FileStream fs = new FileStream(path, FileMode.Open))
                {
                    db.ReadFromStream(new SerializationReader(fs));
                }

                return db;
            });
        }
    }
}