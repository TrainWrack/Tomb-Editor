﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using TombLib.Utils;
using TombLib.Wad;
using TombLib.Wad.Catalog;

namespace TombLib.LevelData.Compilers.TombEngine
{
    public sealed partial class LevelCompilerTombEngine : LevelCompiler
    {
        private readonly Dictionary<Room, TombEngine_room> _tempRooms = new Dictionary<Room, TombEngine_room>(new ReferenceEqualityComparer<Room>());

        private class ComparerFlyBy : IComparer<tr4_flyby_camera>
        {
            public int Compare(tr4_flyby_camera x, tr4_flyby_camera y)
            {
                if (x.Sequence != y.Sequence)
                    return x.Sequence > y.Sequence ? 1 : -1;

                if (x.Index == y.Index)
                    return 0;

                return x.Index > y.Index ? 1 : -1;
            }
        }

        private readonly ScriptIdTable<IHasScriptID> _scriptingIdsTable;
        private readonly List<ushort> _floorData = new List<ushort>();
        private readonly List<TombEngine_mesh> _meshes = new List<TombEngine_mesh>();
        private readonly List<uint> _meshPointers = new List<uint>();
        private readonly List<TombEngine_animation> _animations = new List<TombEngine_animation>();
        private readonly List<tr_state_change> _stateChanges = new List<tr_state_change>();
        private readonly List<tr_anim_dispatch> _animDispatches = new List<tr_anim_dispatch>();
        private readonly List<short> _animCommands = new List<short>();
        private readonly List<int> _meshTrees = new List<int>();
        private readonly List<TombEngine_keyframe> _frames = new List<TombEngine_keyframe>();
        private List<TombEngine_moveable> _moveables = new List<TombEngine_moveable>();
        private readonly List<TombEngine_staticmesh> _staticMeshes = new List<TombEngine_staticmesh>();

        private List<TombEngine_sprite_texture> _spriteTextures = new List<TombEngine_sprite_texture>();
        private List<tr_sprite_sequence> _spriteSequences = new List<tr_sprite_sequence>();
        private readonly List<TombEngine_camera> _cameras = new List<TombEngine_camera>();
        private readonly List<tr4_flyby_camera> _flyByCameras = new List<tr4_flyby_camera>();
        private readonly List<TombEngine_sound_source> _soundSources = new List<TombEngine_sound_source>();
        private List<TombEngine_box> _boxes = new List<TombEngine_box>();
        private List<TombEngine_overlap> _overlaps = new List<TombEngine_overlap>();
        private List<TombEngine_zone> _zones = new List<TombEngine_zone>();

        private readonly List<tr_item> _items = new List<tr_item>();
        private List<tr_ai_item> _aiItems = new List<tr_ai_item>();

        private TombEngineTexInfoManager _textureInfoManager;

        // Temporary dictionaries for mapping editor IDs to level IDs
        private Dictionary<MoveableInstance, int> _moveablesTable;
        private Dictionary<CameraInstance, int> _cameraTable;
        private Dictionary<SinkInstance, int> _sinkTable;
        private Dictionary<MoveableInstance, int> _aiObjectsTable;
        private Dictionary<SoundSourceInstance, int> _soundSourcesTable;
        private Dictionary<FlybyCameraInstance, int> _flybyTable;
        private Dictionary<StaticInstance, int> _staticsTable;

        // Collected game limits
        private Dictionary<Limit, int> _limits;

        public LevelCompilerTombEngine(Level level, string dest, IProgressReporter progressReporter)
            : base(level, dest, progressReporter)
        {
            _scriptingIdsTable = level.GlobalScriptingIdsTable.Clone();

            _limits = new Dictionary<Limit, int>();
            foreach (Limit limit in Enum.GetValues(typeof(Limit)))
                _limits.Add(limit, TrCatalog.GetLimit(level.Settings.GameVersion, limit));
        }

        public override CompilerStatistics CompileLevel()
        {
            ReportProgress(0, "Tomb Engine Level Compiler");

            if (_level.Settings.Wads.All(wad => wad.Wad == null))
                throw new NotSupportedException("A wad must be loaded to compile the final level.");

            _textureInfoManager = new TombEngineTexInfoManager(_level, _progressReporter);
        
            // Prepare level data in parallel to the sounds
            ConvertWad2DataToTr4();
            BuildRooms();

            // Compile textures
            ReportProgress(30, "Packing textures");
            _textureInfoManager.LayOutAllData();

            ReportProgress(35, "   Number of TexInfos: " + _textureInfoManager.TexInfoCount);
            ReportProgress(35, "   Number of anim texture sequences: " + _textureInfoManager.AnimatedTextures.Count);
            if (_textureInfoManager.TexInfoCount > 32767)
                _progressReporter.ReportWarn("TexInfo number overflow, maximum is 32767. Please reduce level complexity.");
            
            GetAllReachableRooms();
            BuildPathFindingData();
            PrepareSoundSources();
            PrepareItems();
            BuildCamerasAndSinks();
            BuildFloorData();
            BuildSprites();

            PrepareRoomsBuckets();
            PrepareMeshBuckets();

            _progressReporter.ReportInfo("\nWriting level file...\n");

            WriteLevelTombEngine();
            
            // Needed to make decision about backup (delete or restore)
            _compiledSuccessfully = true;

            // Return statistics
            return new CompilerStatistics
            {
                BoxCount = _boxes.Count,
                OverlapCount = _overlaps.Count,
                ObjectTextureCount = _textureInfoManager.TexInfoCount,
            };
        }

        private void PrepareSoundSources()
        {
            ReportProgress(40, "Building sound sources");

            _soundSourcesTable = new Dictionary<SoundSourceInstance, int>(new ReferenceEqualityComparer<SoundSourceInstance>());

            foreach (var room in _level.Rooms.Where(room => room != null))
                foreach (var obj in room.Objects.OfType<SoundSourceInstance>())
                    _soundSourcesTable.Add(obj, _soundSourcesTable.Count);

            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var instance in _soundSourcesTable.Keys)
            {
                if (instance.IsEmpty)
                    continue;

                WadSoundInfo soundInfo = _level.Settings.WadTryGetSoundInfo(instance.SoundId);
                if (soundInfo == null)
                {
                    _progressReporter.ReportWarn("Sound (" + instance.SoundNameToDisplay + ") for sound source in room '" + instance.Room + "' at '" + instance.Position + "' is missing.");
                    continue;
                }

                ushort flags = 0;

                switch (instance.PlayMode)
                {
                    case SoundSourcePlayMode.Automatic:
                        flags = instance.Room.Alternated ? (instance.Room.IsAlternate ? (ushort)0x40 : (ushort)0x80) : (ushort)0xC0;
                        break;
                    case SoundSourcePlayMode.Always:
                        flags = 0xC0;
                        break;
                    case SoundSourcePlayMode.OnlyInBaseRoom:
                        flags = 0x80;
                        break;
                    case SoundSourcePlayMode.OnlyInAlternateRoom:
                        flags = 0x40;
                        break;
                }

                Vector3 position = instance.Room.WorldPos + instance.Position;
                _soundSources.Add(new TombEngine_sound_source
                {
                    X = (int)Math.Round(position.X),
                    Y = (int)-Math.Round(position.Y),
                    Z = (int)Math.Round(position.Z),
                    SoundID = (ushort)instance.SoundId,
                    Flags = flags
                });
            }

            ReportProgress(41, "    Number of sound sources: " + _soundSources.Count);
        }

        private void BuildCamerasAndSinks()
        {
            ReportProgress(46, "Building cameras and sinks");

            {
                int cameraSinkID = 0;
                int flybyID = 0;
                _cameraTable = new Dictionary<CameraInstance, int>(new ReferenceEqualityComparer<CameraInstance>());
                _sinkTable = new Dictionary<SinkInstance, int>(new ReferenceEqualityComparer<SinkInstance>());
                _flybyTable = new Dictionary<FlybyCameraInstance, int>(new ReferenceEqualityComparer<FlybyCameraInstance>());
                foreach (var room in _level.Rooms.Where(room => room != null))
                {
                    foreach (var obj in room.Objects.OfType<CameraInstance>())
                        _cameraTable.Add(obj, cameraSinkID++);
                    foreach (var obj in room.Objects.OfType<FlybyCameraInstance>())
                        _flybyTable.Add(obj, flybyID++);
                }
                foreach (var room in _level.Rooms.Where(room => room != null))
                {
                    foreach (var obj in room.Objects.OfType<SinkInstance>())
                        _sinkTable.Add(obj, cameraSinkID++);
                }
            }

            foreach (var instance in _cameraTable.Keys)
            {
                Vector3 position = instance.Room.WorldPos + instance.Position;
                _cameras.Add(new TombEngine_camera
                {
                    X = (int)Math.Round(position.X),
                    Y = (int)-Math.Round(position.Y),
                    Z = (int)Math.Round(position.Z),
                    Room = (short)_roomsRemappingDictionary[instance.Room],
                    Flags = instance.Fixed ? 1 : 0
                });
            }

            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var instance in _sinkTable.Keys)
            {
                int xSector = (int)Math.Floor(instance.Position.X / 1024);
                int zSector = (int)Math.Floor(instance.Position.Z / 1024);

                var tempRoom = _tempRooms[instance.Room];
                Vector3 position = instance.Room.WorldPos + instance.Position;

                int boxIndex = tempRoom.Sectors[tempRoom.NumZSectors * xSector + zSector].BoxIndex;
                
                _cameras.Add(new TombEngine_camera
                {
                    X = (int)Math.Round(position.X),
                    Y = (int)-Math.Round(position.Y),
                    Z = (int)Math.Round(position.Z),
                    Room = instance.Strength,
                    Flags = boxIndex
                });
            }

            foreach (var instance in _flybyTable.Keys)
            {
                Vector3 direction = instance.GetDirection();
                Vector3 position = instance.Room.WorldPos + instance.Position;
                ushort rollTo65536 = (ushort)(65536 - Math.Round(Math.Max(0, Math.Min(ushort.MaxValue, instance.Roll * (65536.0 / 360.0)))));
                _flyByCameras.Add(new tr4_flyby_camera
                {
                    X = (int)Math.Round(position.X),
                    Y = (int)Math.Round(-position.Y),
                    Z = (int)Math.Round(position.Z),
                    Room = _roomsRemappingDictionary[instance.Room],
                    FOV = (ushort)Math.Round(Math.Max(0, Math.Min(ushort.MaxValue, instance.Fov * (65536.0 / 360.0)))),
                    Roll = unchecked((short)rollTo65536),
                    Timer = (ushort)instance.Timer,
                    Speed = (ushort)Math.Round(Math.Max(0, Math.Min(ushort.MaxValue, instance.Speed * 655.0f))),
                    Sequence = (byte)instance.Sequence,
                    Index = (byte)instance.Number,
                    Flags = instance.Flags,
                    DirectionX = (int)Math.Round(position.X + 1024 * direction.X),
                    DirectionY = (int)-Math.Round(position.Y + 1024 * direction.Y),
                    DirectionZ = (int)Math.Round(position.Z + 1024 * direction.Z),
                });
            }
            _flyByCameras.Sort(new ComparerFlyBy());

            // Check camera duplicates
            int lastSeq   = -1;
            int lastIndex = -1;

            for (int i = 0; i < _flyByCameras.Count; i++)
            {
                if(_flyByCameras[i].Sequence != lastSeq)
                {
                    lastSeq = _flyByCameras[i].Sequence;
                    lastIndex = -1;
                }

                if (_flyByCameras[i].Index == lastIndex && _flyByCameras[i].Sequence == lastSeq)
                    _progressReporter.ReportWarn("Warning: flyby sequence " + _flyByCameras[i].Sequence + " has duplicated camera with ID " + lastIndex);
                lastIndex = _flyByCameras[i].Index;
            }

            ReportProgress(47, "    Number of cameras: " + _cameraTable.Count);
            ReportProgress(47, "    Number of flyby cameras: " + _flyByCameras.Count);
            ReportProgress(47, "    Number of sinks: " + _sinkTable.Count);
        }

        private static TombEngine_room_sector GetSector(TombEngine_room room, int x, int z)
        {
            return room.Sectors[room.NumZSectors * x + z];
        }

        private static void SaveSector(TombEngine_room room, int x, int z, TombEngine_room_sector sector)
        {
            room.Sectors[room.NumZSectors * x + z] = sector;
        }

        private void GetAllReachableRooms()
        {
            foreach (var room in _level.Rooms.Where(r => r != null))
                _tempRooms[room].ReachableRooms = new List<Room>();

            foreach (var room in _level.Rooms.Where(r => r != null))
            {
                GetAllReachableRoomsUp(room, room);
                GetAllReachableRoomsDown(room, room);
            }
        }

        private void GetAllReachableRoomsUp(Room baseRoom, Room currentRoom)
        {
            // Wall portals
            foreach (var p in currentRoom.Portals)
            {
                if (p.Direction == PortalDirection.Floor || p.Direction == PortalDirection.Ceiling)
                    continue;

                var tempRoom = _tempRooms[baseRoom];
                if (!tempRoom.ReachableRooms.Contains(p.AdjoiningRoom))
                    tempRoom.ReachableRooms.Add(p.AdjoiningRoom);
            }

            // Ceiling portals
            foreach (var p in currentRoom.Portals)
            {
                if (p.Direction != PortalDirection.Ceiling)
                    continue;

                var tempRoom = _tempRooms[baseRoom];
                if (tempRoom.ReachableRooms.Contains(p.AdjoiningRoom))
                    continue;

                tempRoom.ReachableRooms.Add(p.AdjoiningRoom);
                GetAllReachableRoomsUp(baseRoom, p.AdjoiningRoom);
            }
        }

        private void GetAllReachableRoomsDown(Room baseRoom, Room currentRoom)
        {
            // portali laterali
            foreach (var p in currentRoom.Portals)
            {
                if (p.Direction == PortalDirection.Floor || p.Direction == PortalDirection.Ceiling)
                    continue;

                var tempRoom = _tempRooms[baseRoom];
                if (!tempRoom.ReachableRooms.Contains(p.AdjoiningRoom))
                    tempRoom.ReachableRooms.Add(p.AdjoiningRoom);
            }

            foreach (var p in currentRoom.Portals)
            {
                if (p.Direction != PortalDirection.Floor)
                    continue;

                var tempRoom = _tempRooms[baseRoom];
                if (tempRoom.ReachableRooms.Contains(p.AdjoiningRoom))
                    continue;

                tempRoom.ReachableRooms.Add(p.AdjoiningRoom);
                GetAllReachableRoomsDown(baseRoom, p.AdjoiningRoom);
            }
        }

        private void FindBottomFloor(ref Room room, ref int x, ref int z)
        {
            while (room.GetFloorRoomConnectionInfo(new VectorInt2(x, z)).TraversableType == Room.RoomConnectionType.FullPortal)
            {
                var sector = room.Blocks[x, z];
                x += room.Position.X - sector.FloorPortal.AdjoiningRoom.Position.X;
                z += room.Position.Z - sector.FloorPortal.AdjoiningRoom.Position.Z;
                room = sector.FloorPortal.AdjoiningRoom;
            }
        }

        private short GetMostDownFloor(Room room, int x, int z)
        {
            FindBottomFloor(ref room, ref x, ref z);
            return (short)(_tempRooms[room].AuxSectors[x, z].LowestFloor * 256);
        }

        private bool FindMonkeyFloor(Room room, int x, int z)
        {
            FindBottomFloor(ref room, ref x, ref z);
            return room.Blocks[x, z].HasFlag(BlockFlags.Monkey);
        }

        private void PrepareItems()
        {
            bool isNewTR = _level.Settings.GameVersion > TRVersion.Game.TR3;

            ReportProgress(42, "Building items table");

            _moveablesTable = new Dictionary<MoveableInstance, int>(new ReferenceEqualityComparer<MoveableInstance>());
            _aiObjectsTable = new Dictionary<MoveableInstance, int>(new ReferenceEqualityComparer<MoveableInstance>());

            foreach (Room room in _level.Rooms.Where(room => room != null))
                foreach (var instance in room.Objects.OfType<MoveableInstance>())
                {
                    WadMoveable wadMoveable = _level.Settings.WadTryGetMoveable(instance.WadObjectId);
                    if (wadMoveable == null)
                    {
                        _progressReporter.ReportWarn("Moveable '" + instance + "' was not included in the level because it is missing the *.wad file.");
                        continue;
                    }

                    Vector3 position = instance.Room.WorldPos + instance.Position;
                    double angle = Math.Round(instance.RotationY * (65536.0 / 360.0));
                    ushort angleInt = unchecked((ushort)Math.Max(0, Math.Min(ushort.MaxValue, angle)));

                    // Split AI objects and normal objects (only for TR4+)
                    if (isNewTR && TrCatalog.IsMoveableAI(_level.Settings.GameVersion, wadMoveable.Id.TypeId))
                    {
                        _aiItems.Add(new tr_ai_item
                        {
                            X = (int)Math.Round(position.X),
                            Y = (int)-Math.Round(position.Y),
                            Z = (int)Math.Round(position.Z),
                            ObjectID = checked((ushort)instance.WadObjectId.TypeId),
                            Room = (ushort)_roomsRemappingDictionary[instance.Room],
                            Angle = angleInt,
                            OCB = instance.Ocb,
                            Flags = (ushort)(instance.CodeBits << 1)
                        });
                        _aiObjectsTable.Add(instance, _aiObjectsTable.Count);
                    }
                    else
                    {
                        int flags = (instance.CodeBits << 9) | (instance.ClearBody ? 0x80 : 0) | (instance.Invisible ? 0x100 : 0);
                        ushort color = instance.Color.Equals(Vector3.One) ? (ushort)0xFFFF : PackColorTo16Bit(instance.Color);

                        // Substitute ID is needed to convert visible menu items to pick-up sprites in TR1-2
                        var realID = TrCatalog.GetSubstituteID(_level.Settings.GameVersion, instance.WadObjectId.TypeId);

                        _items.Add(new tr_item
                        {
                            X = (int)Math.Round(position.X),
                            Y = (int)-Math.Round(position.Y),
                            Z = (int)Math.Round(position.Z),
                            ObjectID = checked((ushort)realID),
                            Room = (short)_roomsRemappingDictionary[instance.Room],
                            Angle = angleInt,
                            Intensity1 = color,
                            Ocb = isNewTR ? instance.Ocb : unchecked((short)color),
                            Flags = unchecked((ushort)flags)
                        });
                        _moveablesTable.Add(instance, _moveablesTable.Count);
                    }
                }

            // Sort AI objects and put all LARA_START_POS objects (last AI object by ID) in front
            if (_level.Settings.GameVersion > TRVersion.Game.TR3)
            {
                _aiItems = _aiItems.OrderByDescending(item => item.ObjectID).ThenBy(item => item.OCB).ToList();
                ReportProgress(45, "    Number of AI objects: " + _aiItems.Count);
            }

            ReportProgress(45, "    Number of items: " + _items.Count);

            int maxSafeItemCount, maxItemCount;
            switch(_level.Settings.GameVersion)
            {
                case TRVersion.Game.TRNG:
                    maxSafeItemCount = 255;
                    maxItemCount = 1023;
                    break;
                case TRVersion.Game.TombEngine:
                    maxSafeItemCount = 1023;
                    maxItemCount = 32767;
                    break;
                default:
                    maxSafeItemCount = 255;
                    maxItemCount = 255;
                    break;
            }

            if (_items.Count > maxItemCount)
            {
                var warnString = "Level has more than " + maxItemCount + " moveables. This will lead to crash" +
                                 (_level.Settings.GameVersion == TRVersion.Game.TR4 ? ", unless you're using TREP." : ".");
                _progressReporter.ReportWarn(warnString);
            }

            if (_items.Count > maxSafeItemCount)
                _progressReporter.ReportWarn("Moveable count is beyond " + maxSafeItemCount + ", which may lead to savegame handling issues.");
        }


        public string GetTRNGVersion()
        {
            var buffer = PathC.GetDirectoryNameTry(_level.Settings.MakeAbsolute(_level.Settings.GameExecutableFilePath)) + "\\Tomb_NextGeneration.dll";
            if (File.Exists(buffer))
            {
                buffer = (FileVersionInfo.GetVersionInfo(buffer)).ProductVersion;
                _progressReporter.ReportInfo("TRNG found, version is " + buffer);
            }
            else
            {
                buffer = "1,3,0,6";
                _progressReporter.ReportWarn("Tomb_NextGeneration.dll wasn't found in game directory. Probably you're using TRNG target on vanilla TR4/TRLE?");
            }
            return buffer;
        }
    }
}
