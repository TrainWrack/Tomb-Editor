﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TombLib.LevelData;
using TombLib.Utils;

namespace TombLib.NG
{
    public static class NgParameterInfo
    {
        public static IEnumerable<TriggerType> GetTriggerTypeRange(LevelSettings levelSettings)
        {
            yield return TriggerType.Trigger;
            yield return TriggerType.Pad;
            yield return TriggerType.Switch;
            yield return TriggerType.Key;
            yield return TriggerType.Pickup;
            yield return TriggerType.Heavy;
            yield return TriggerType.Antipad;
            yield return TriggerType.Combat;
            yield return TriggerType.Dummy;
            yield return TriggerType.Antitrigger;

            if (levelSettings.GameVersion >= TRVersion.Game.TR4)
            {
                yield return TriggerType.HeavySwitch;
                yield return TriggerType.HeavyAntitrigger;
            }

            if (levelSettings.GameVersion == TRVersion.Game.TRNG)
                yield return TriggerType.ConditionNg;
            else
            {
                if (levelSettings.GameVersion >= TRVersion.Game.TR4)
                    yield return TriggerType.Monkey;

                if (levelSettings.GameVersion == TRVersion.Game.TR5)
                    yield return TriggerType.Skeleton;

                if (levelSettings.GameVersion.Legacy() == TRVersion.Game.TR5)
                {
                    yield return TriggerType.TightRope;
                    yield return TriggerType.Crawl;
                    yield return TriggerType.Climb;
                }
            }
        }

        public static IEnumerable<TriggerTargetType> GetTargetTypeRange(LevelSettings levelSettings, TriggerType triggerType)
        {
            if (triggerType == TriggerType.ConditionNg)
            {
                yield return TriggerTargetType.ParameterNg;
            }
            else
            {
                yield return TriggerTargetType.Object;
                yield return TriggerTargetType.Camera;
                yield return TriggerTargetType.Sink;
                yield return TriggerTargetType.FlipMap;
                yield return TriggerTargetType.FlipOn;
                yield return TriggerTargetType.FlipOff;
                yield return TriggerTargetType.Target;
                yield return TriggerTargetType.FinishLevel;
                yield return TriggerTargetType.PlayAudio;
                yield return TriggerTargetType.FlipEffect;
                yield return TriggerTargetType.Secret;

                if (levelSettings.GameVersion >= TRVersion.Game.TR4)
                    yield return TriggerTargetType.FlyByCamera;
                if (levelSettings.GameVersion == TRVersion.Game.TRNG)
                {
                    yield return TriggerTargetType.ActionNg;
                    yield return TriggerTargetType.FmvNg;
                    yield return TriggerTargetType.TimerfieldNg;
                }

                if (levelSettings.GameVersion == TRVersion.Game.TombEngine)
                {
                    yield return TriggerTargetType.LuaScript;
                }
            }
        }

        public static NgParameterRange ToParameterRange(this IEnumerable @this)
        {
            var sortedDictionary = new SortedDictionary<ushort, TriggerParameterUshort>();
            foreach (object value in @this)
                sortedDictionary.Add((ushort)value, new TriggerParameterUshort((ushort)value, value.ToString()));
            return new NgParameterRange(sortedDictionary);
        }

        public static NgParameterRange GetTargetRange(LevelSettings levelSettings, TriggerType triggerType, TriggerTargetType targetType, ITriggerParameter timer)
        {
            switch (triggerType)
            {
                case TriggerType.ConditionNg:
                    if (!(timer is TriggerParameterUshort))
                        return new NgParameterRange(NgParameterKind.Empty);
                    NgTriggerSubtype conditionSubtriggerType = NgCatalog.ConditionTrigger.MainList.TryGetOrDefault(((TriggerParameterUshort)timer).Key);
                    return conditionSubtriggerType?.Target ?? new NgParameterRange(NgParameterKind.Empty);

                default:
                    switch (targetType)
                    {
                        case TriggerTargetType.Object:
                            return new NgParameterRange(NgParameterKind.MoveablesInLevel);

                        case TriggerTargetType.Camera:
                            return new NgParameterRange(NgParameterKind.CamerasInLevel);

                        case TriggerTargetType.Sink:
                            return new NgParameterRange(NgParameterKind.SinksInLevel);

                        case TriggerTargetType.Target:
                            // Actually it is possible to not only target Target objects, but all movables.
                            // This is also useful: It makes sense to target egg a trap or an enemy.
                            return new NgParameterRange(NgParameterKind.MoveablesInLevel);

                        case TriggerTargetType.FlyByCamera:
                            return new NgParameterRange(NgParameterKind.FlybyCamerasInLevel);

                        case TriggerTargetType.FlipEffect:
                            if (levelSettings.GameVersion == TRVersion.Game.TRNG)
                                return new NgParameterRange(NgCatalog.FlipEffectTrigger.MainList.DicSelect(e => (TriggerParameterUshort)e.Value));
                            else if (levelSettings.GameVersion == TRVersion.Game.TR4)
                                return new NgParameterRange(NgCatalog.FlipEffectTrigger.MainList
                                    .DicWhere(entry => entry.Value.Name.Contains("OldFlip"))
                                    .DicSelect(e => (TriggerParameterUshort)e.Value));
                            else
                                return new NgParameterRange(NgParameterKind.AnyNumber);

                        case TriggerTargetType.ActionNg:
                            if (!(timer is TriggerParameterUshort))
                                return new NgParameterRange(NgParameterKind.Empty);
                            NgTriggerSubtype actionSubtriggerType = NgCatalog.ActionTrigger.MainList.TryGetOrDefault(((TriggerParameterUshort)timer).Key);
                            return actionSubtriggerType?.Target ?? new NgParameterRange(NgParameterKind.Empty);

                        case TriggerTargetType.TimerfieldNg:
                            return NgCatalog.TimerFieldTrigger;

                        case TriggerTargetType.LuaScript:
                            if (levelSettings.GameVersion == TRVersion.Game.TombEngine)
                                return new NgParameterRange(NgParameterKind.LuaFunctions);
                           else
                                return new NgParameterRange(NgParameterKind.AnyNumber);

                        default:
                            return new NgParameterRange(NgParameterKind.AnyNumber);
                    }
            }
        }

        public static NgParameterRange GetTimerRange(LevelSettings levelSettings, TriggerType triggerType, TriggerTargetType targetType, ITriggerParameter target)
        {
            switch (triggerType)
            {
                case TriggerType.ConditionNg:
                    return new NgParameterRange(NgCatalog.ConditionTrigger.MainList.DicSelect(e => (TriggerParameterUshort)e.Value));

                default:
                    switch (targetType)
                    {
                        case TriggerTargetType.FlipEffect:
                            if (!(target is TriggerParameterUshort))
                                return new NgParameterRange(NgParameterKind.Empty);
                            NgTriggerSubtype flipEffectSubtriggerType = NgCatalog.FlipEffectTrigger.MainList.TryGetOrDefault(((TriggerParameterUshort)target).Key);
                            return flipEffectSubtriggerType?.Timer ?? new NgParameterRange(NgParameterKind.Empty);

                        case TriggerTargetType.ActionNg:
                            return new NgParameterRange(NgCatalog.ActionTrigger.MainList.DicSelect(e => (TriggerParameterUshort)e.Value));

                        case TriggerTargetType.TimerfieldNg:
                            return new NgParameterRange(NgParameterKind.Empty);

                        default:
                            return new NgParameterRange(NgParameterKind.AnyNumber);
                    }
            }
        }

        public static NgParameterRange GetExtraRange(LevelSettings levelSettings, TriggerType triggerType, TriggerTargetType targetType, ITriggerParameter target, ITriggerParameter timer)
        {
            switch (triggerType)
            {
                case TriggerType.ConditionNg:
                    if (!(timer is TriggerParameterUshort))
                        return new NgParameterRange(NgParameterKind.Empty);
                    NgTriggerSubtype conditionSubtriggerType = NgCatalog.ConditionTrigger.MainList.TryGetOrDefault(((TriggerParameterUshort)timer).Key);
                    return conditionSubtriggerType?.Extra ?? new NgParameterRange(NgParameterKind.Empty);

                default:
                    switch (targetType)
                    {
                        case TriggerTargetType.FlipEffect:
                            if (!(target is TriggerParameterUshort))
                                return new NgParameterRange(NgParameterKind.Empty);
                            NgTriggerSubtype flipEffectSubtriggerType = NgCatalog.FlipEffectTrigger.MainList.TryGetOrDefault(((TriggerParameterUshort)target).Key);
                            return flipEffectSubtriggerType?.Extra ?? new NgParameterRange(NgParameterKind.Empty);

                        case TriggerTargetType.ActionNg:
                            if (!(timer is TriggerParameterUshort))
                                return new NgParameterRange(NgParameterKind.Empty);
                            NgTriggerSubtype actionSubtriggerType = NgCatalog.ActionTrigger.MainList.TryGetOrDefault(((TriggerParameterUshort)timer).Key);
                            return actionSubtriggerType?.Extra ?? new NgParameterRange(NgParameterKind.Empty);

                        default:
                            return new NgParameterRange(NgParameterKind.Empty);
                    }
            }
        }

        public static bool TriggerIsValid(LevelSettings levelSettings, TriggerInstance trigger)
        {
            if (!GetTriggerTypeRange(levelSettings).Contains(trigger.TriggerType))
                return false;
            if (!GetTargetTypeRange(levelSettings, trigger.TriggerType).Contains(trigger.TargetType))
                return false;
            if (!GetTargetRange(levelSettings, trigger.TriggerType, trigger.TargetType, trigger.Timer).ParameterMatches(trigger.Target, false))
                return false;
            if (!GetTimerRange(levelSettings, trigger.TriggerType, trigger.TargetType, trigger.Target).ParameterMatches(trigger.Timer, false))
                return false;
            if (!GetExtraRange(levelSettings, trigger.TriggerType, trigger.TargetType, trigger.Target, trigger.Timer).ParameterMatches(trigger.Extra, false))
                return false;
            return true;
        }

        public delegate ushort BoundedValueCallback(ushort upperBound);

        public static ushort EncodeNGRealTimer(TriggerTargetType targetType, TriggerType triggerType, ushort target, ushort upperBound, BoundedValueCallback timer, BoundedValueCallback extra)
        {
            ushort timerUpperBound = (ushort)(upperBound & 255);
            ushort extraUpperBound = (ushort)(upperBound >> 8);
            switch (triggerType)
            {
                case TriggerType.ConditionNg:
                    // Bit 8 is one shot in trigger setup so we must shift by 9
                    return (ushort)(timer(timerUpperBound) | (extra(extraUpperBound) << 9));

                default:
                    switch (targetType)
                    {
                        case TriggerTargetType.ActionNg:
                            return (ushort)(timer(timerUpperBound) | (extra(extraUpperBound) << 8));

                        case TriggerTargetType.TimerfieldNg:
                            return timer(upperBound);

                        case TriggerTargetType.FlipEffect:
                            NgTriggerSubtype flipEffectSubtriggerType = NgCatalog.FlipEffectTrigger.MainList.TryGetOrDefault(target);
                            if (flipEffectSubtriggerType != null && flipEffectSubtriggerType.Extra.IsEmpty)
                                return timer(upperBound);
                            else
                                return (ushort)(timer(timerUpperBound) | (extra(extraUpperBound) << 8));

                        default:
                            return timer(upperBound);
                    }
            }
        }

        public static void DecodeNGRealTimer(TriggerTargetType targetType, TriggerType triggerType, ushort target, ushort realTimer, short triggerFlags, out ushort? timer, out ushort? extra)
        {
            switch (triggerType)
            {
                case TriggerType.ConditionNg:
                    timer = (ushort)(realTimer & 255);
                    var conditionTrigger = NgCatalog.ConditionTrigger.MainList.TryGetOrDefault(timer.Value);
                    if (conditionTrigger != null && conditionTrigger.Extra.IsEmpty)
                        extra = null;
                    else
                        extra = (ushort)(~(triggerFlags >> 1) & 0x1f); //   (realTimer >> 8);
                    return;

                default:
                    switch (targetType)
                    {
                        case TriggerTargetType.ActionNg:
                            timer = (ushort)(realTimer & 255);
                            var actionTrigger = NgCatalog.ActionTrigger.MainList.TryGetOrDefault(timer.Value);
                            if (actionTrigger != null && actionTrigger.Extra.IsEmpty)
                                extra = null;
                            else
                                extra = (ushort)(realTimer >> 8);
                            return;

                        case TriggerTargetType.TimerfieldNg:
                            timer = realTimer;
                            extra = null;
                            return;

                        case TriggerTargetType.FlipEffect:
                            var flipEffectTrigger = NgCatalog.FlipEffectTrigger.MainList.TryGetOrDefault(target);
                            if (flipEffectTrigger != null && flipEffectTrigger.Extra.IsEmpty)
                            {
                                timer = realTimer;
                                extra = null;
                            }
                            else
                            {
                                timer = (ushort)(realTimer & 255);
                                extra = (ushort)(realTimer >> 8);
                            }
                            return;

                        default:
                            timer = realTimer;
                            extra = null;
                            return;
                    }
            }
        }

        public class ExceptionScriptIdMissing : Exception
        {
            public ExceptionScriptIdMissing()
                : base("ScriptID is missing")
            { }
        }
        public class ExceptionScriptNotSupported : NotSupportedException
        {
            public ExceptionScriptNotSupported()
                : base("Script not supported")
            { }
        }
        private static ushort GetValue(Level level, ITriggerParameter parameter)
        {
            if (parameter == null)
                return 0;
            else if (parameter is TriggerParameterUshort)
                return ((TriggerParameterUshort)parameter).Key;
            else if (parameter is IHasScriptID)
            {
                uint? Id = ((IHasScriptID)parameter).ScriptId;
                if (Id == null)
                    throw new ExceptionScriptIdMissing();
                return checked((ushort)Id);
            }
            else if (parameter is Room)
                return (ushort)level.GetRearrangedRooms().ReferenceIndexOf(parameter);
            else
                throw new Exception("Trigger parameter of invalid type!");
        }

        public static ITriggerParameter FixTriggerParameter(Level level, TriggerInstance instance, ITriggerParameter parameter, NgParameterRange range, Dictionary<uint, PositionBasedObjectInstance> objectLookup, IProgressReporter progressReporter = null)
        {
            List<FlybyCameraInstance> flyByLookup = null;

            if (!(parameter is TriggerParameterUshort))
                return parameter;
            ushort index = ((TriggerParameterUshort)parameter).Key;
            if (range.IsObject)
            {
                // Special lookup for flybys.
                // Triggers marked as flyby can point to a sequence directly instead an object ID
                if (instance.TargetType == TriggerTargetType.FlyByCamera)
                {
                    if (flyByLookup == null)
                        flyByLookup = objectLookup.Values
                            .OfType<FlybyCameraInstance>()
                            .GroupBy(flyBy => flyBy.Sequence)
                            .Select(flyByGroup => flyByGroup.OrderBy(flyBy => flyBy.Number).First())
                            .ToList();
                    FlybyCameraInstance foundFlyBy = flyByLookup.FirstOrDefault(flyBy => flyBy.Sequence == index);
                    if (foundFlyBy != null)
                        return foundFlyBy;
                }

                // Undo object indexing
                PositionBasedObjectInstance @object;
                if (!objectLookup.TryGetValue(index, out @object))
                {
                    progressReporter?.ReportWarn("Trigger '" + instance + "' in '" + instance.Room + "' refers to an object with ID " + index + " that is unavailable.");
                    return null;
                }
                return @object;
            }
            else if (range.IsRoom)
            {
                // Undo room indexing
                Room room3 = level.ExistingRooms.ElementAtOrDefault(index);
                if (room3 == null)
                {
                    progressReporter?.ReportWarn("Trigger '" + instance + "' in '" + instance.Room + "' refers to a room with ID " + index + " that is unavailable.");
                    return parameter;
                }
                return room3;
            }
            else
                return parameter;
        }

        public static TriggerInstance ImportFromScriptTrigger(Level level, string script)
        {
            // Build object lookup table
            var objectLookup = level.ExistingRooms
                                          .SelectMany(room => room.Objects)
                                          .Where(instance => instance is IHasScriptID && ((IHasScriptID)instance).ScriptId.HasValue)
                                          .ToDictionary(instance => ((IHasScriptID)instance).ScriptId.Value);
            // Make dummy trigger
            var result = new TriggerInstance(RectangleInt2.Zero) { TriggerType = TriggerType.Trigger };

            // Split script string into tokens and clear spaces
            var tokens = script.Split(',').Select(s => s.Trim(' ')).ToList();

            if (tokens.Count != 3)
                return null; // Incorrect amount of operands!

            ushort[] operands = new ushort[tokens.Count];

            // Parse tokens
            for (int i = 0; i < tokens.Count; i++)
            {
                var token = tokens[i];
                if (token.StartsWith("$") && ushort.TryParse(token.Trim('$'), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out operands[i]))
                    continue;
                if (ushort.TryParse(token, out operands[i]))
                    continue;
                return null;  // Incorrect operand!
            }

            // Should we splice timer into extra?
            bool decode = false;

            try
            {
                switch (operands[0])
                {
                    case 0x8000:
                    case 0x9000:
                        result.TriggerType = TriggerType.ConditionNg;
                        result.TargetType = TriggerTargetType.ParameterNg;
                        decode = !NgCatalog.ConditionTrigger.MainList[(ushort)(operands[2] & 0xFF)].Extra.IsEmpty;
                        break;

                    case 0x2000:
                        result.TargetType = TriggerTargetType.FlipEffect;
                        decode = !NgCatalog.FlipEffectTrigger.MainList[operands[1]].Extra.IsEmpty;
                        break;

                    case 0x4000:
                    case 0x5000:
                        result.TargetType = TriggerTargetType.ActionNg;
                        decode = !NgCatalog.ActionTrigger.MainList[(ushort)(operands[2] & 0xFF)].Extra.IsEmpty;
                        break;

                    default:
                        return null; // Incorrect first operand!
                }
            }
            catch
            {
                return null;
            }

            // Target is always raw
            result.Target = new TriggerParameterUshort(operands[1]);

            // Splice timer into extra if needed
            if (decode)
            {
                result.Timer = new TriggerParameterUshort((ushort)(operands[2] & 0xFF));
                result.Extra = new TriggerParameterUshort((ushort)(operands[2] >> 8));
            }
            else
                result.Timer = new TriggerParameterUshort(operands[2]);

            // Link actual objects
            result.Target = FixTriggerParameter(level, result, result.Target,
                GetTargetRange(level.Settings, result.TriggerType, result.TargetType, result.Timer), objectLookup);
            result.Timer = FixTriggerParameter(level, result, result.Timer,
                GetTimerRange(level.Settings, result.TriggerType, result.TargetType, result.Target), objectLookup);
            result.Extra = FixTriggerParameter(level, result, result.Extra,
                GetExtraRange(level.Settings, result.TriggerType, result.TargetType, result.Target, result.Timer), objectLookup);

            return result;
        }

        public static string ExportToScriptTrigger(Level level, TriggerInstance trigger, int? animCommandNumber, bool withComment = false)
        {
            checked
            {
                string result = null;
                ushort mask   = 0;
                ushort firstValue  = 0;
                ushort secondValue = 0;

                if (animCommandNumber.HasValue)
                {
                    mask |= 0x8000;

                    if (animCommandNumber.Value < 0)
                        mask |= 0xFF;
                    else
                        mask |= (byte)Math.Min(animCommandNumber.Value, byte.MaxValue - 1);
                }
                else
                    mask = 0;

                switch (trigger.TriggerType)
                {
                    case TriggerType.ConditionNg:
                        {
                            // Condition triggers can't be exported as anim command because they use same 
                            // bit flag as animcommand exporter flag

                            if (!TriggerIsValid(level.Settings, trigger) || animCommandNumber.HasValue)
                                throw new Exception("Trigger is invalid.");

                            ushort conditionId = GetValue(level, trigger.Timer);
                            NgTriggerSubtype conditionTrigger = NgCatalog.ConditionTrigger.MainList[conditionId];

                            mask |= (ushort)(trigger.Target is ObjectInstance ? 0x9000 : 0x8000);
                            firstValue = GetValue(level, trigger.Target);
                            secondValue = conditionId;

                            if (!conditionTrigger.Extra.IsEmpty)
                                secondValue |= (ushort)(GetValue(level, trigger.Extra) << 8);
                            break;
                        }
                    default:
                        switch (trigger.TargetType)
                        {
                            case TriggerTargetType.FlipEffect:
                                {
                                    if (!TriggerIsValid(level.Settings, trigger))
                                        throw new Exception("Trigger is invalid.");

                                    ushort flipeffectId = GetValue(level, trigger.Target);
                                    NgTriggerSubtype flipeffectTrigger = NgCatalog.FlipEffectTrigger.MainList[flipeffectId];

                                    mask |= 0x2000;
                                    firstValue = flipeffectId;
                                    secondValue = GetValue(level, trigger.Timer);

                                    if (!flipeffectTrigger.Extra.IsEmpty)
                                        secondValue |= (ushort)(GetValue(level, trigger.Extra) << 8);
                                    break;
                                }

                            case TriggerTargetType.ActionNg:
                                {
                                    if (!TriggerIsValid(level.Settings, trigger))
                                        throw new Exception("Trigger is invalid.");

                                    ushort actionId = GetValue(level, trigger.Timer);
                                    NgTriggerSubtype actionTrigger = NgCatalog.ActionTrigger.MainList[actionId];

                                    mask |= (ushort)(trigger.Target is StaticInstance ? 0x4000 : 0x5000);
                                    firstValue = GetValue(level, trigger.Target);
                                    secondValue = actionId;

                                    if (!actionTrigger.Extra.IsEmpty)
                                        secondValue |= (ushort)(GetValue(level, trigger.Extra) << 8);
                                    break;
                                }
                        }
                        break;
                }

                if (mask != 0)
                    result = animCommandNumber.HasValue ? unchecked((short)mask) + " ," + firstValue + " ," + secondValue :
                        "$" + mask.ToString("X4") + "," + firstValue + ",$" + secondValue.ToString("X4");

                if (!string.IsNullOrEmpty(result))
                    return (withComment ?
                            "; "       + trigger.TriggerType + " for " + trigger.TargetType +
                            "\n; <#> " + trigger.Target +
                            "\n; <&> " + trigger.Timer  +
                            (trigger.Extra == null ? "" : "\n; <E> " + trigger.Extra) +
                            "\n; Copy following values to your script:" +
                            "\n; "
                        : "")
                        + result;
            }
            throw new ExceptionScriptNotSupported();
        }

    }
}