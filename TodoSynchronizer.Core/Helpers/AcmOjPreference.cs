using System;
using TodoSynchronizer.Core.Config;
using TodoSynchronizer.Core.Models.AcmOjModels;

namespace TodoSynchronizer.Core.Helpers
{
    public static class AcmOjPreference
    {
        public static DateTime? GetDueTime(AcmOjProblemset problemset)
        {
            var config = SyncConfig.Default.AcmOjAssignmentConfig;
            if (config.DueDateModeFallback)
            {
                switch (config.DueDateMode)
                {
                    case AcmOjDueDateMode.end_time:
                        return problemset.EndTime ?? problemset.LateSubmissionDeadline;
                    case AcmOjDueDateMode.late_submission_deadline:
                        return problemset.LateSubmissionDeadline ?? problemset.EndTime;
                    default:
                        return null;
                }
            }

            switch (config.DueDateMode)
            {
                case AcmOjDueDateMode.end_time:
                    return problemset.EndTime;
                case AcmOjDueDateMode.late_submission_deadline:
                    return problemset.LateSubmissionDeadline;
                default:
                    return null;
            }
        }

        public static DateTime? GetRemindTime(AcmOjProblemset problemset)
        {
            var due = GetDueTime(problemset);
            if (!due.HasValue)
                return null;
            return due.Value - SyncConfig.Default.AcmOjAssignmentConfig.BeforeTimeSpan;
        }

        public static double GetRemindBefore(AcmOjAssignmentConfig config)
        {
            return config.BeforeTimeSpan.TotalMinutes;
        }
    }
}
