﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using TombLib.Utils;

namespace TombLib.Wad.Catalog
{
    public class TrCatalog
    {
        private struct Item
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public uint SkinId { get; set; }
            public bool AIObject { get; set; }
        }

        private struct ItemSound
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public bool FixedByDefault { get; set; }
        }

        private struct ItemAnimation
        {
            public uint Item { get; set; }
            public uint Animation { get; set; }
            public string Name { get; set; }
        }

        private struct ItemState
        {
            public uint Item { get; set; }
            public uint State { get; set; }
            public string Name { get; set; }
        }

        private class Game
        {
            internal WadGameVersion Version { get; private set; }
            internal SortedList<uint, Item> Moveables { get; private set; } = new SortedList<uint, Item>();
            internal SortedList<uint, Item> SpriteSequences { get; private set; } = new SortedList<uint, Item>();
            internal SortedList<uint, Item> Statics { get; private set; } = new SortedList<uint, Item>();
            internal SortedList<uint, ItemSound> Sounds { get; private set; } = new SortedList<uint, ItemSound>();
            internal List<ItemAnimation> Animations { get; private set; } = new List<ItemAnimation>();
            internal List<ItemState> States { get; private set; } = new List<ItemState>();

            public Game(WadGameVersion version)
            {
                Version = version;
            }
        }

        private static readonly Dictionary<WadGameVersion, Game> Games = new Dictionary<WadGameVersion, Game>();

        public static int PredictSoundMapSize(WadGameVersion wadVersion, bool IsNg, int numDemoData)
        {
            switch (wadVersion)
            {
                case WadGameVersion.TR1:
                    return 256;
                case WadGameVersion.TR2:
                case WadGameVersion.TR3:
                    return 370;
                case WadGameVersion.TR4_TRNG:
                    return IsNg && numDemoData != 0 ? numDemoData : 370;
                case WadGameVersion.TR5:
                case WadGameVersion.TR5Main:
                    return 450;
                default:
                    throw new ArgumentOutOfRangeException("Unknown game version.");
            }
        }

        public static string GetMoveableName(WadGameVersion version, uint id)
        {
            Game game;
            if (!Games.TryGetValue(version, out game))
                return "Unknown #" + id;
            Item entry;
            if (!game.Moveables.TryGetValue(id, out entry))
                return "Unknown #" + id;
            return game.Moveables[id].Name;
        }

        public static uint GetMoveableSkin(WadGameVersion version, uint id)
        {
            Game game;
            if (!Games.TryGetValue(version, out game))
                return id;
            Item entry;
            if (!game.Moveables.TryGetValue(id, out entry))
                return id;
            return game.Moveables[id].SkinId;
        }

        public static bool IsMoveableAI(WadGameVersion version, uint id)
        {
            Game game;
            if (!Games.TryGetValue(version, out game))
                return false;
            Item entry;
            if (!game.Moveables.TryGetValue(id, out entry))
                return false;

            return entry.AIObject;
        }

        public static string GetStaticName(WadGameVersion version, uint id)
        {
            Game game;
            if (!Games.TryGetValue(version, out game))
                return "Unknown #" + id;
            Item entry;
            if (!game.Statics.TryGetValue(id, out entry))
                return "Unknown #" + id;
            return game.Statics[id].Name;
        }

        public static uint? GetItemIndex(WadGameVersion version, string name, out bool isMoveable)
        {
            Game game;
            if (!Games.TryGetValue(version, out game))
            {
                isMoveable = false;
                return null;
            }

            var entry = game.Moveables.FirstOrDefault(item => item.Value.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
            if (entry.Value.Name != null)
            {
                isMoveable = true;
                return entry.Key;
            }

            entry = game.Statics.FirstOrDefault(item => item.Value.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
            if (entry.Value.Name != null)
            {
                isMoveable = false;
                return entry.Key;
            }

            isMoveable = false;
            return null;
        }

        public static string GetOriginalSoundName(WadGameVersion version, uint id)
        {
            Game game;
            if (!Games.TryGetValue(version, out game))
                return "UNKNOWN_SOUND_" + id;
            ItemSound entry;
            if (!game.Sounds.TryGetValue(id, out entry))
                return "UNKNOWN_SOUND_" + id;
            return game.Sounds[id].Name;
        }

        public static int TryGetSoundInfoIdByDescription(WadGameVersion version, string name)
        {
            var sounds = Games[version].Sounds;
            foreach (var pair in sounds)
                if (pair.Value.Description == name)
                    return (int)pair.Key;
            return -1;
        }

        public static string GetSpriteSequenceName(WadGameVersion version, uint id)
        {
            Game game;
            if (!Games.TryGetValue(version, out game))
                return "Unknown #" + id;
            Item entry;
            if (!game.SpriteSequences.TryGetValue(id, out entry))
                return "Unknown #" + id;
            return game.SpriteSequences[id].Name;
        }

        public static bool IsSoundFixedByDefault(WadGameVersion version, uint id)
        {
            Game game;
            if (!Games.TryGetValue(version, out game))
                return false;
            ItemSound entry;
            if (!game.Sounds.TryGetValue(id, out entry))
                return false;
            return game.Sounds[id].FixedByDefault;
        }

        public static string GetAnimationName(WadGameVersion version, uint objectId, uint animId)
        {
            Game game;
            ItemAnimation entry = new ItemAnimation();
            Games.TryGetValue(version, out game);

            if (game != null)
                entry = game.Animations.FirstOrDefault(item => item.Item == objectId && item.Animation == animId);
            
            if (entry.Name == null)
            {
                var otherGames = Games.Where(g => g.Key != version).ToList();
                otherGames.Reverse();

                foreach (var otherGame in otherGames)
                {
                    entry = otherGame.Value.Animations.FirstOrDefault(item => item.Item == objectId && item.Animation == animId);
                    if (entry.Name != null) break;
                }
            }

            if (entry.Name == null) return "Animation " + animId;
            else return entry.Name;
        }

        public static string GetStateName(WadGameVersion version, uint objectId, uint stateId)
        {
            Game game;
            ItemState entry = new ItemState();
            Games.TryGetValue(version, out game);

            if (game != null)
                entry = game.States.FirstOrDefault(item => item.Item == objectId && item.State == stateId);

            if (entry.Name == null)
            {
                var otherGames = Games.Where(g => g.Key != version).ToList();
                otherGames.Reverse();

                foreach (var otherGame in otherGames)
                {
                    entry = otherGame.Value.States.FirstOrDefault(item => item.Item == objectId && item.State == stateId);
                    if (entry.Name != null) break;
                }
            }

            if (entry.Name == null) return "Unknown state " + stateId;
            else return entry.Name;
        }

        public static int TryToGetStateID(WadGameVersion version, uint objectId, string stateName)
        {
            Game game;
            ItemState entry = new ItemState();
            Games.TryGetValue(version, out game);
            
            if (game != null)
                entry = game.States.FirstOrDefault(item => item.Item == objectId && item.Name.ToLower().Contains(stateName.ToLower()));

            if (entry.Name == null)
                foreach (var otherGame in Games.Where(g => g.Key != version))
                {
                    entry = otherGame.Value.States.FirstOrDefault(item => item.Item == objectId && item.Name.ToLower().Contains(stateName.ToLower()));
                    if (entry.Name != null) break;
                }

            if (entry.Name == null) return -1;
            else return (int)entry.State;
        }

        public static IDictionary<uint, string> GetAllMoveables(WadGameVersion version)
        {
            return Games[version].Moveables.DicSelect(item => item.Value.Name);
        }

        public static IDictionary<uint, string> GetAllStatics(WadGameVersion version)
        {
            return Games[version].Statics.DicSelect(item => item.Value.Name);
        }

        public static IDictionary<uint, string> GetAllSpriteSequences(WadGameVersion version)
        {
            return Games[version].SpriteSequences.DicSelect(item => item.Value.Name);
        }

        public static IDictionary<uint, string> GetAllSounds(WadGameVersion version)
        {
            return Games[version].Sounds.DicSelect(item => item.Value.Name);
        }

        public static IDictionary<uint, string> GetAllFixedByDefaultSounds(WadGameVersion version)
        {
            return Games[version].Sounds
                .DicWhere(sound => sound.Value.FixedByDefault)
                .DicSelect(item => item.Value.Name);
        }

        public static string GetVersionString(WadGameVersion version)
        {
            switch (version)
            {
                case WadGameVersion.TR1:
                    return "Tomb Raider";
                case WadGameVersion.TR2:
                    return "Tomb Raider 2";
                case WadGameVersion.TR3:
                    return "Tomb Raider 3";
                case WadGameVersion.TR4_TRNG:
                    return "Tomb Raider 4";
                case WadGameVersion.TR5:
                    return "Tomb Raider 5";
                case WadGameVersion.TR5Main:
                    return "TR5Main";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static void LoadCatalog(string fileName)
        {
            var document = new XmlDocument();
            document.Load(fileName);

            XmlNodeList gamesNodes = document.DocumentElement.SelectNodes("/game");
            foreach (XmlNode gameNode in document.DocumentElement.ChildNodes)
            {
                if (gameNode.Name != "game")
                    continue;

                var stringVersion = gameNode.Attributes["id"].Value;
                WadGameVersion version;
                if (stringVersion == "TR1")
                    version = WadGameVersion.TR1;
                else if (stringVersion == "TR2")
                    version = WadGameVersion.TR2;
                else if (stringVersion == "TR3")
                    version = WadGameVersion.TR3;
                else if (stringVersion == "TR4")
                    version = WadGameVersion.TR4_TRNG;
                else if (stringVersion == "TR5")
                    version = WadGameVersion.TR5;
                else if (stringVersion == "TR5Main")
                    version = WadGameVersion.TR5Main;
                else
                    continue;

                Game game = new Game(version);

                // Parse moveables
                XmlNode moveables = gameNode.SelectSingleNode("moveables");
                if (moveables != null)
                    foreach (XmlNode moveableNode in moveables.ChildNodes)
                    {
                        if (moveableNode.Name != "moveable")
                            continue;

                        uint id = uint.Parse(moveableNode.Attributes["id"].Value);
                        string name = moveableNode.Attributes["name"].Value;

                        var skinId = id;
                        if (moveableNode.Attributes["use_body_from"] != null)
                            skinId = uint.Parse(moveableNode.Attributes["use_body_from"].Value);

                        bool isAI = false;
                        if (moveableNode.Attributes["ai"] != null)
                            isAI = short.Parse(moveableNode.Attributes["ai"].Value) > 0;

                        game.Moveables.Add(id, new Item { Name = name, SkinId = skinId, AIObject = isAI });
                    }

                // Parse statics
                XmlNode statics = gameNode.SelectSingleNode("statics");
                if (statics != null)
                    foreach (XmlNode staticNode in statics.ChildNodes)
                    {
                        if (staticNode.Name != "static")
                            continue;

                        uint id = uint.Parse(staticNode.Attributes["id"].Value);
                        string name = staticNode.Attributes["name"]?.Value ?? "";
                        game.Statics.Add(id, new Item { Name = name });
                    }

                // Parse sounds
                XmlNode sounds = gameNode.SelectSingleNode("sounds");
                if (sounds != null)
                    foreach (XmlNode soundNode in sounds.ChildNodes)
                    {
                        if (soundNode.Name != "sound")
                            continue;

                        uint id = uint.Parse(soundNode.Attributes["id"].Value);
                        string name = soundNode.Attributes["name"]?.Value ?? "";
                        string description = soundNode.Attributes["description"]?.Value ?? "";
                        bool fixedByDefault = bool.Parse(soundNode.Attributes["fixed_by_default"]?.Value ?? "false");
                        game.Sounds.Add(id, new ItemSound { Name = name, FixedByDefault = fixedByDefault, Description = description });
                    }

                // Parse sprite sequences
                XmlNode spriteSequences = gameNode.SelectSingleNode("sprite_sequences");
                if (spriteSequences != null)
                    foreach (XmlNode spriteSequenceNode in spriteSequences.ChildNodes)
                    {
                        if (spriteSequenceNode.Name != "sprite_sequence")
                            continue;

                        uint id = uint.Parse(spriteSequenceNode.Attributes["id"].Value);
                        string name = spriteSequenceNode.Attributes["name"].Value;
                        game.SpriteSequences.Add(id, new Item { Name = name });
                    }

                // Parse animations
                XmlNode animations = gameNode.SelectSingleNode("animations");
                if (animations != null)
                    foreach (XmlNode originalNameNode in animations.ChildNodes)
                    {
                        if (originalNameNode.Name != "anim")
                            continue;

                        uint item = uint.Parse(originalNameNode.Attributes["item"].Value);
                        uint id = uint.Parse(originalNameNode.Attributes["id"].Value);
                        string name = originalNameNode.Attributes["name"].Value;

                        game.Animations.Add(new ItemAnimation { Name = name, Animation = id, Item = item });
                    }

                // Parse states
                XmlNode states = gameNode.SelectSingleNode("states");
                if (states != null)
                    foreach (XmlNode originalNameNode in states.ChildNodes)
                    {
                        if (originalNameNode.Name != "state")
                            continue;

                        uint item = uint.Parse(originalNameNode.Attributes["item"].Value);
                        uint id = uint.Parse(originalNameNode.Attributes["id"].Value);
                        string name = originalNameNode.Attributes["name"].Value;

                        game.States.Add(new ItemState { Name = name, State = id, Item = item });
                    }
                Games.Add(version, game);
            }
        }
    }
}
