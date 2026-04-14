using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using HarmonyLib;
using LitJson;
using VRage;
using VRage.Utils;

namespace ClientPlugin.Patches;

internal static class JsonLogHelper
{
    [ThreadStatic] private static JsonWriter threadJsonWriter;
    [ThreadStatic] private static bool threadIdAssigned;
    [ThreadStatic] private static int threadSequenceId;
    private static int nextSequenceId;

    public static void WriteJsonEntry(bool enabled, FastResourceLock @lock, StreamWriter writer, bool alwaysFlush,
        string severity, string message, object[] data, string exception)
    {
        var now = Config.Current.UtcTimestamps ? DateTimeOffset.UtcNow : DateTimeOffset.Now;
        if (!threadIdAssigned)
        {
            threadSequenceId = System.Threading.Interlocked.Increment(ref nextSequenceId) - 1;
            threadIdAssigned = true;
        }

        var jw = threadJsonWriter ??= new JsonWriter { Validate = false };
        jw.Reset();

        jw.WriteObjectStart();

        jw.WritePropertyName("timestamp");
        jw.Write(now.ToString("yyyy-MM-ddTHH:mm:ss.fffK"));

        jw.WritePropertyName("threadId");
        jw.Write(threadSequenceId);

        if (severity != null)
        {
            jw.WritePropertyName("severity");
            jw.Write(severity);
        }

        jw.WritePropertyName("message");
        jw.Write(message ?? "");

        if (data != null && data.Length > 0)
        {
            jw.WritePropertyName("data");
            jw.WriteArrayStart();
            foreach (var item in data)
                WriteJsonValue(jw, item);
            jw.WriteArrayEnd();
        }

        if (exception != null)
        {
            jw.WritePropertyName("exception");
            jw.Write(exception);
        }

        jw.WriteObjectEnd();

        WriteRawLine(enabled, @lock, writer, alwaysFlush, jw.ToString());
    }

    public static string FormatMessage(string format, object[] args)
    {
        if (args == null || args.Length == 0)
            return format ?? "";

        try
        {
            return string.Format(format, args);
        }
        catch
        {
            return format + " " + string.Join(";", args);
        }
    }

    private static void WriteRawLine(bool enabled, FastResourceLock @lock, StreamWriter writer, bool alwaysFlush, string line)
    {
        if (!enabled) return;
        try
        {
            using (@lock.AcquireExclusiveUsing())
            {
                if (writer == null) return;
                writer.WriteLine(line);
                if (alwaysFlush)
                    writer.Flush();
            }
        }
        catch (Exception e)
        {
            Console.Error.WriteLine($"JSONL log write failed: {e}");
        }
    }

    private static void WriteJsonValue(JsonWriter jw, object value)
    {
        switch (value)
        {
            case null:
                jw.Write(null);
                break;
            case bool b:
                jw.Write(b);
                break;
            case int i:
                jw.Write(i);
                break;
            case long l:
                jw.Write(l);
                break;
            case ulong ul:
                jw.Write(ul);
                break;
            case double d:
                jw.Write(double.IsNaN(d) || double.IsInfinity(d) ? 0.0 : d);
                break;
            case float f:
                jw.Write(float.IsNaN(f) || float.IsInfinity(f) ? 0.0 : f);
                break;
            case decimal m:
                jw.Write(m);
                break;
            default:
                jw.Write(value.ToString());
                break;
        }
    }
}

[HarmonyPatch(typeof(MyLog), "Log", typeof(MyLogSeverity), typeof(string), typeof(object[]))]
[SuppressMessage("ReSharper", "UnusedType.Global")]
public static class JsonLogFormatPatch
{
    private static Config Config => Config.Current;

    [HarmonyPrefix]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static bool Prefix(MyLogSeverity severity, string format, object[] args,
        bool ___m_enabled, FastResourceLock ___m_lock, StreamWriter ___m_streamWriter, bool ___m_alwaysFlush)
    {
        if (!Config.JsonlFormat) return true;

        var message = JsonLogHelper.FormatMessage(format, args);
        JsonLogHelper.WriteJsonEntry(___m_enabled, ___m_lock, ___m_streamWriter, ___m_alwaysFlush,
            severity.ToString(), message, args, null);

        // Preserve OnLog callback
        var onLog = MyLog.OnLog;
        if (onLog != null)
        {
            var sb = new StringBuilder();
            sb.AppendFormat("{0}: ", severity);
            sb.Append(message);
            sb.Append('\n');
            onLog(severity, sb);
        }

        // Preserve assert behavior
        if (severity >= MyLog.AssertLevel)
            Trace.Fail($"{severity}: {message}");

        return false;
    }
}

[HarmonyPatch(typeof(MyLog), "Log", typeof(MyLogSeverity), typeof(StringBuilder))]
[SuppressMessage("ReSharper", "UnusedType.Global")]
public static class JsonLogBuilderPatch
{
    private static Config Config => Config.Current;

    [HarmonyPrefix]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static bool Prefix(MyLogSeverity severity, StringBuilder builder,
        bool ___m_enabled, FastResourceLock ___m_lock, StreamWriter ___m_streamWriter, bool ___m_alwaysFlush)
    {
        if (!Config.JsonlFormat) return true;

        var message = builder?.ToString() ?? "";
        JsonLogHelper.WriteJsonEntry(___m_enabled, ___m_lock, ___m_streamWriter, ___m_alwaysFlush,
            severity.ToString(), message, null, null);

        // Preserve OnLog callback
        var onLog = MyLog.OnLog;
        if (onLog != null)
        {
            var sb = new StringBuilder();
            sb.AppendFormat("{0}: ", severity);
            sb.AppendStringBuilder(builder);
            sb.Append('\n');
            onLog(severity, sb);
        }

        // Preserve assert behavior
        if (severity >= MyLog.AssertLevel)
            Trace.Fail($"{severity}: {message}");

        return false;
    }
}

[HarmonyPatch(typeof(MyLog), "WriteLine", typeof(string))]
[SuppressMessage("ReSharper", "UnusedType.Global")]
public static class JsonLogWriteLinePatch
{
    private static Config Config => Config.Current;

    [HarmonyPrefix]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static bool Prefix(string msg,
        bool ___m_enabled, FastResourceLock ___m_lock, StreamWriter ___m_streamWriter, bool ___m_alwaysFlush)
    {
        if (!Config.JsonlFormat) return true;

        JsonLogHelper.WriteJsonEntry(___m_enabled, ___m_lock, ___m_streamWriter, ___m_alwaysFlush,
            null, msg ?? "", null, null);
        return false;
    }
}

[HarmonyPatch(typeof(MyLog), "WriteLine", typeof(Exception))]
[SuppressMessage("ReSharper", "UnusedType.Global")]
public static class JsonLogWriteLineExceptionPatch
{
    private static Config Config => Config.Current;

    [HarmonyPrefix]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static bool Prefix(Exception ex,
        bool ___m_enabled, FastResourceLock ___m_lock, StreamWriter ___m_streamWriter, bool ___m_alwaysFlush)
    {
        if (!Config.JsonlFormat) return true;

        var message = ex != null ? "Exception occurred: " + ex.Message : "Exception occurred: null";
        var exception = ex?.ToString();
        JsonLogHelper.WriteJsonEntry(___m_enabled, ___m_lock, ___m_streamWriter, ___m_alwaysFlush,
            "Error", message, null, exception);

        // Preserve flush behavior from the original
        try
        {
            if (___m_enabled && ___m_streamWriter != null)
            {
                using (___m_lock.AcquireExclusiveUsing())
                    ___m_streamWriter.Flush();
            }
        }
        catch (Exception e)
        {
            Console.Error.WriteLine($"JSONL log flush failed: {e}");
        }

        return false;
    }
}
