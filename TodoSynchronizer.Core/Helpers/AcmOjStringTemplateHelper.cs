using System;
using System.Linq;
using System.Text;
using TodoSynchronizer.Core.Config;
using TodoSynchronizer.Core.Models.AcmOjModels;

namespace TodoSynchronizer.Core.Helpers
{
    public static class AcmOjStringTemplateHelper
    {
        public static string GetTitle(AcmOjProblemset problemset)
        {
            return SyncConfig.Default.AcmOjAssignmentConfig.TitleTemplate
                .ReplaceCourse(problemset.Course)
                .Replace("{problemset.name}", problemset.Name ?? string.Empty);
        }

        public static string ReplaceCourse(this string s, AcmOjCourse course)
        {
            if (course == null)
                return s.Replace("{course.name}", string.Empty);

            return s.Replace("{course.name}", course.Name ?? string.Empty);
        }

        public static string GetContent(AcmOjProblemset problemset)
        {
            if (string.IsNullOrWhiteSpace(problemset.Description))
                return string.Empty;
            return problemset.Description.Trim();
        }

        public static string GetProblemStepTitle(AcmOjProblemBrief problem)
        {
            if (problem == null)
                return string.Empty;
            var title = string.IsNullOrWhiteSpace(problem.Title) ? string.Empty : problem.Title.Trim();
            if (string.IsNullOrWhiteSpace(title))
                return problem.Id.ToString();
            return $"{problem.Id} {title}";
        }
    }
}
