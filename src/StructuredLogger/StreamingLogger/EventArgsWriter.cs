﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Build.Framework;

namespace Microsoft.Build.Logging.Serialization
{
    public class EventArgsWriter
    {
        private BinaryWriter binaryWriter;

        private static FieldInfo lazyFormattedArgumentsField =
            typeof(LazyFormattedBuildEventArgs).GetField("arguments", BindingFlags.Instance | BindingFlags.NonPublic);
        private static FieldInfo buildEventArgsMessageField =
            typeof(BuildEventArgs).GetField("message", BindingFlags.Instance | BindingFlags.NonPublic);
        private static Func<CultureInfo, string, object[], string> formatStringDelegate =
            (Func<CultureInfo, string, object[], string>)Delegate.CreateDelegate(
                typeof(Func<CultureInfo, string, object[], string>),
                typeof(LazyFormattedBuildEventArgs).GetMethod("FormatString", BindingFlags.Static | BindingFlags.NonPublic));

        public EventArgsWriter(BinaryWriter binaryWriter)
        {
            this.binaryWriter = binaryWriter;
        }

        public void Write(BuildEventArgs e)
        {
            if (e is BuildStartedEventArgs)
            {
                Write((BuildStartedEventArgs)e);
            }
            else if (e is BuildFinishedEventArgs)
            {
                Write((BuildFinishedEventArgs)e);
            }
            else if (e is ProjectStartedEventArgs)
            {
                Write((ProjectStartedEventArgs)e);
            }
            else if (e is ProjectFinishedEventArgs)
            {
                Write((ProjectFinishedEventArgs)e);
            }
            else if (e is TargetStartedEventArgs)
            {
                Write((TargetStartedEventArgs)e);
            }
            else if (e is TargetFinishedEventArgs)
            {
                Write((TargetFinishedEventArgs)e);
            }
            else if (e is TaskStartedEventArgs)
            {
                Write((TaskStartedEventArgs)e);
            }
            else if (e is TaskFinishedEventArgs)
            {
                Write((TaskFinishedEventArgs)e);
            }
            else if (e is BuildErrorEventArgs)
            {
                Write((BuildErrorEventArgs)e);
            }
            else if (e is BuildWarningEventArgs)
            {
                Write((BuildWarningEventArgs)e);
            }
            else if (e is BuildMessageEventArgs)
            {
                Write((BuildMessageEventArgs)e);
            }
            else if (e is CustomBuildEventArgs)
            {
                Write((CustomBuildEventArgs)e);
            }
        }

        private void Write(BuildStartedEventArgs e)
        {
            Write(LogRecordKind.BuildStarted);
            WriteBuildEventArgsFields(e);
            Write(e.BuildEnvironment);
        }

        private void Write(BuildFinishedEventArgs e)
        {
            Write(LogRecordKind.BuildFinished);
            WriteBuildEventArgsFields(e);
            Write(e.Succeeded);
        }

        private void Write(ProjectStartedEventArgs e)
        {
            Write(LogRecordKind.ProjectStarted);
            WriteBuildEventArgsFields(e);

            if (e.ParentProjectBuildEventContext == null)
            {
                Write(false);
            }
            else
            {
                Write(true);
                Write(e.ParentProjectBuildEventContext);
            }

            WriteOptionalString(e.ProjectFile);

            Write(e.ProjectId);
            Write(e.TargetNames);
            WriteOptionalString(e.ToolsVersion);

            WriteProperties(e.Properties);

            WriteItems(e.Items);
        }

        private void Write(ProjectFinishedEventArgs e)
        {
            Write(LogRecordKind.ProjectFinished);
            WriteBuildEventArgsFields(e);
            WriteOptionalString(e.ProjectFile);
            Write(e.Succeeded);
        }

        private void Write(TargetStartedEventArgs e)
        {
            Write(LogRecordKind.TargetStarted);
            WriteBuildEventArgsFields(e);
            WriteOptionalString(e.TargetName);
            WriteOptionalString(e.ProjectFile);
            WriteOptionalString(e.TargetFile);
            WriteOptionalString(e.ParentTarget);
        }

        private void Write(TargetFinishedEventArgs e)
        {
            Write(LogRecordKind.TargetFinished);
            WriteBuildEventArgsFields(e);
            Write(e.Succeeded);
            WriteOptionalString(e.ProjectFile);
            WriteOptionalString(e.TargetFile);
            WriteOptionalString(e.TargetName);
            WriteItems(e.TargetOutputs);
        }

        private void Write(TaskStartedEventArgs e)
        {
            Write(LogRecordKind.TaskStarted);
            WriteBuildEventArgsFields(e);
            WriteOptionalString(e.TaskName);
            WriteOptionalString(e.ProjectFile);
            WriteOptionalString(e.TaskFile);
        }

        private void Write(TaskFinishedEventArgs e)
        {
            Write(LogRecordKind.TaskFinished);
            WriteBuildEventArgsFields(e);
            Write(e.Succeeded);
            WriteOptionalString(e.TaskName);
            WriteOptionalString(e.ProjectFile);
            WriteOptionalString(e.TaskFile);
        }

        private void Write(BuildErrorEventArgs e)
        {
            Write(LogRecordKind.Error);
            WriteBuildEventArgsFields(e);
            WriteOptionalString(e.Subcategory);
            WriteOptionalString(e.Code);
            WriteOptionalString(e.File);
            WriteOptionalString(e.ProjectFile);
            Write(e.LineNumber);
            Write(e.ColumnNumber);
            Write(e.EndLineNumber);
            Write(e.EndColumnNumber);
        }

        private void Write(BuildWarningEventArgs e)
        {
            Write(LogRecordKind.Warning);
            WriteBuildEventArgsFields(e);
            WriteOptionalString(e.Subcategory);
            WriteOptionalString(e.Code);
            WriteOptionalString(e.File);
            WriteOptionalString(e.ProjectFile);
            Write(e.LineNumber);
            Write(e.ColumnNumber);
            Write(e.EndLineNumber);
            Write(e.EndColumnNumber);
        }

        private void Write(BuildMessageEventArgs e)
        {
            Write(LogRecordKind.Message);
            WriteMessageFields(e);
        }

        private void Write(CustomBuildEventArgs e)
        {
            Write(LogRecordKind.CustomEvent);
            WriteBuildEventArgsFields(e);
        }

        private void WriteBuildEventArgsFields(BuildEventArgs e)
        {
            var flags = GetBuildEventArgsFieldFlags(e);
            Write((int)flags);
            WriteBaseFields(e, flags);
        }

        private void WriteBaseFields(BuildEventArgs e, BuildEventArgsFieldFlags flags)
        {
            if ((flags & BuildEventArgsFieldFlags.Message) != 0)
            {
                WriteMessage((LazyFormattedBuildEventArgs)e);
            }

            if ((flags & BuildEventArgsFieldFlags.BuildEventContext) != 0)
            {
                Write(e.BuildEventContext);
            }

            if ((flags & BuildEventArgsFieldFlags.ThreadId) != 0)
            {
                Write(e.ThreadId);
            }

            if ((flags & BuildEventArgsFieldFlags.HelpHeyword) != 0)
            {
                Write(e.HelpKeyword);
            }

            if ((flags & BuildEventArgsFieldFlags.SenderName) != 0)
            {
                Write(e.SenderName);
            }

            if ((flags & BuildEventArgsFieldFlags.Timestamp) != 0)
            {
                Write(e.Timestamp);
            }
        }

        private void WriteMessageFields(BuildMessageEventArgs e)
        {
            var flags = GetBuildEventArgsFieldFlags(e);
            flags = GetMessageFlags(e, flags);

            Write((int)e.Importance);
            Write((int)flags);

            WriteBaseFields(e, flags);

            if ((flags & BuildEventArgsFieldFlags.Subcategory) != 0)
            {
                Write(e.Subcategory);
            }

            if ((flags & BuildEventArgsFieldFlags.Code) != 0)
            {
                Write(e.Code);
            }

            if ((flags & BuildEventArgsFieldFlags.File) != 0)
            {
                Write(e.File);
            }

            if ((flags & BuildEventArgsFieldFlags.ProjectFile) != 0)
            {
                Write(e.ProjectFile);
            }

            if ((flags & BuildEventArgsFieldFlags.LineNumber) != 0)
            {
                Write(e.LineNumber);
            }

            if ((flags & BuildEventArgsFieldFlags.ColumnNumber) != 0)
            {
                Write(e.ColumnNumber);
            }

            if ((flags & BuildEventArgsFieldFlags.EndLineNumber) != 0)
            {
                Write(e.EndLineNumber);
            }

            if ((flags & BuildEventArgsFieldFlags.EndColumnNumber) != 0)
            {
                Write(e.EndColumnNumber);
            }
        }

        private static BuildEventArgsFieldFlags GetMessageFlags(BuildMessageEventArgs e, BuildEventArgsFieldFlags flags)
        {
            if (e.Subcategory != null)
            {
                flags |= BuildEventArgsFieldFlags.Subcategory;
            }

            if (e.Code != null)
            {
                flags |= BuildEventArgsFieldFlags.Code;
            }

            if (e.File != null)
            {
                flags |= BuildEventArgsFieldFlags.File;
            }

            if (e.ProjectFile != null)
            {
                flags |= BuildEventArgsFieldFlags.ProjectFile;
            }

            if (e.LineNumber != 0)
            {
                flags |= BuildEventArgsFieldFlags.LineNumber;
            }

            if (e.ColumnNumber != 0)
            {
                flags |= BuildEventArgsFieldFlags.ColumnNumber;
            }

            if (e.EndLineNumber != 0)
            {
                flags |= BuildEventArgsFieldFlags.EndLineNumber;
            }

            if (e.EndColumnNumber != 0)
            {
                flags |= BuildEventArgsFieldFlags.EndColumnNumber;
            }

            return flags;
        }

        private static BuildEventArgsFieldFlags GetBuildEventArgsFieldFlags(BuildEventArgs e)
        {
            var flags = BuildEventArgsFieldFlags.None;
            if (e.BuildEventContext != null)
            {
                flags |= BuildEventArgsFieldFlags.BuildEventContext;
            }

            if (e.HelpKeyword != null)
            {
                flags |= BuildEventArgsFieldFlags.HelpHeyword;
            }

            if (e.Message != null)
            {
                flags |= BuildEventArgsFieldFlags.Message;
            }

            if (e.SenderName != null)
            {
                flags |= BuildEventArgsFieldFlags.SenderName;
            }

            if (e.ThreadId != -1)
            {
                flags |= BuildEventArgsFieldFlags.ThreadId;
            }

            if (e.Timestamp != default(DateTime))
            {
                flags |= BuildEventArgsFieldFlags.Timestamp;
            }

            return flags;
        }

        private void WriteItems(IEnumerable items)
        {
            if (items == null)
            {
                Write(0);
                return;
            }

            var entries = items.OfType<DictionaryEntry>()
                .Where(e => e.Key is string && e.Value is ITaskItem);
            Write(entries.Count());

            foreach (DictionaryEntry entry in entries)
            {
                string key = entry.Key as string;
                ITaskItem item = entry.Value as ITaskItem;
                Write(key);
                Write(item);
            }
        }

        private void Write(ITaskItem item)
        {
            Write(item.ItemSpec);
            Write(item.MetadataCount);

            foreach (string metadataName in item.MetadataNames)
            {
                Write(metadataName);
                Write(item.GetMetadata(metadataName));
            }
        }

        private void WriteProperties(IEnumerable properties)
        {
            if (properties == null)
            {
                Write(0);
                return;
            }

            Write(properties.Cast<object>().Count());

            foreach (DictionaryEntry entry in properties)
            {
                if (entry.Key is string && entry.Value is string)
                {
                    Write((string)entry.Key);
                    Write((string)entry.Value);
                }
                else
                {
                    // to keep the count accurate
                    Write("");
                    Write("");
                }
            }
        }

        private void Write(BuildEventContext buildEventContext)
        {
            Write(buildEventContext.NodeId);
            Write(buildEventContext.ProjectContextId);
            Write(buildEventContext.TargetId);
            Write(buildEventContext.TaskId);
            Write(buildEventContext.SubmissionId);
            Write(buildEventContext.ProjectInstanceId);
        }

        private void WriteMessage(LazyFormattedBuildEventArgs e)
        {
            string message = buildEventArgsMessageField.GetValue(e) as string;

            var arguments = (object[])lazyFormattedArgumentsField.GetValue(e);
            if (arguments != null && arguments.Length > 0)
            {
                message = formatStringDelegate(CultureInfo.CurrentCulture, message, arguments);
            }

            Write(message);
        }

        private void Write<TKey, TValue>(IEnumerable<KeyValuePair<TKey, TValue>> keyValuePairs)
        {
            if (keyValuePairs != null && keyValuePairs.Any())
            {
                Write(keyValuePairs.Count());
                foreach (var kvp in keyValuePairs)
                {
                    Write(kvp.Key.ToString());
                    Write(kvp.Value.ToString());
                }
            }
            else
            {
                Write(false);
            }
        }

        private void Write(LogRecordKind kind)
        {
            Write((int)kind);
        }

        private void Write(int number)
        {
            binaryWriter.Write(number);
        }

        private void Write(bool boolean)
        {
            binaryWriter.Write(boolean);
        }

        private void Write(string text)
        {
            if (text != null)
            {
                binaryWriter.Write(text);
            }
            else
            {
                binaryWriter.Write(0);
            }
        }

        private void WriteOptionalString(string text)
        {
            if (text == null)
            {
                Write(false);
            }
            else
            {
                Write(true);
                Write(text);
            }
        }

        private void Write(DateTime timestamp)
        {
            binaryWriter.Write(timestamp.Ticks);
            binaryWriter.Write((int)timestamp.Kind);
        }
    }
}