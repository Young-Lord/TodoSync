using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace TodoSynchronizer.Core.Helpers
{
    /// <summary>
    /// Remembers the task bodies written by earlier runs. The To Do API returns a plain text
    /// projection of an HTML body, which cannot be compared with what was written, so without
    /// this state every run would rewrite every task.
    /// </summary>
    public static class SyncStateStore
    {
        // File holding the state; without one there is no state and every body is written.
        public static string FilePath { get; set; }
        // Entries that were not used for this long are dropped when the state is saved.
        private static readonly TimeSpan Retention = TimeSpan.FromDays(90);

        private static Dictionary<string, Entry> _tasks;
        private static DateTime _now;

        // Hash of a body, tagged with the version of the renderer that produced it, so that
        // changing the renderer invalidates the state instead of keeping stale bodies.
        public static string Hash(string content)
        {
            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(content ?? ""));
                var sb = new StringBuilder("v1:");
                foreach (var b in hash)
                    sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }

        // True when this exact body was written to the task by an earlier run.
        public static bool IsCurrent(string taskid, string content)
        {
            if (taskid == null || FilePath == null)
                return false;
            EnsureLoaded();
            Entry entry;
            if (!_tasks.TryGetValue(taskid, out entry) || entry.Hash != Hash(content))
                return false;
            entry.Seen = _now;
            return true;
        }

        // Records a body that was just written to a task.
        public static void Remember(string taskid, string content)
        {
            if (taskid == null || content == null || FilePath == null)
                return;
            EnsureLoaded();
            _tasks[taskid] = new Entry() { Hash = Hash(content), Seen = _now };
        }

        // Writes the state back, reporting false when it could not be saved.
        public static bool Save()
        {
            if (FilePath == null || _tasks == null)
                return true;
            try
            {
                var state = new StateFile() { Version = 1, Tasks = new Dictionary<string, Entry>() };
                foreach (var task in _tasks)
                    if (_now - task.Value.Seen < Retention)
                        state.Tasks[task.Key] = task.Value;
                var directory = Path.GetDirectoryName(FilePath);
                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);
                File.WriteAllText(FilePath, JsonConvert.SerializeObject(state, Formatting.Indented));
                return true;
            }
            catch (Exception)
            {
                // Losing the state only costs rewriting the task bodies once.
                return false;
            }
        }

        private static void EnsureLoaded()
        {
            if (_tasks != null)
                return;
            _now = DateTime.UtcNow;
            _tasks = new Dictionary<string, Entry>();
            if (FilePath == null)
                return;
            try
            {
                var state = JsonConvert.DeserializeObject<StateFile>(File.ReadAllText(FilePath));
                if (state != null && state.Version == 1 && state.Tasks != null)
                    _tasks = state.Tasks;
            }
            catch (Exception)
            {
                // A missing or unreadable state means every body is written again.
            }
        }

        private class StateFile
        {
            public int Version { get; set; }
            public Dictionary<string, Entry> Tasks { get; set; }
        }

        private class Entry
        {
            public string Hash { get; set; }
            public DateTime Seen { get; set; }
        }
    }
}
