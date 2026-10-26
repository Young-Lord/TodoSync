using System;

namespace TodoSynchronizer.Core.Helpers
{
    public static class DueDateRule
    {
        // China Standard Time has no DST, so a fixed offset keeps the result identical
        // whether the sync runs in GitHub Actions (UTC) or on a machine in another zone.
        private static readonly TimeSpan ChinaOffset = TimeSpan.FromHours(8);
        private static readonly TimeSpan LastMinuteOfDay = new TimeSpan(23, 59, 0);

        /// <summary>
        /// Moves every deadline that does not fall in the last minute of its day back by
        /// one day, so a daytime deadline is not mistaken for "late that night" and put off.
        /// </summary>
        public static DateTime? Apply(DateTime? due)
        {
            if (!due.HasValue)
                return null;
            return ToChinaTime(due.Value).TimeOfDay < LastMinuteOfDay ? due.Value.AddDays(-1) : due.Value;
        }

        private static DateTime ToChinaTime(DateTime value)
        {
            switch (value.Kind)
            {
                case DateTimeKind.Utc:
                    return value + ChinaOffset;
                case DateTimeKind.Local:
                    return value.ToUniversalTime() + ChinaOffset;
                default:
                    return value;
            }
        }
    }
}
