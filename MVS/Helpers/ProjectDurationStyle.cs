using System.Windows;
using System.Windows.Controls;

namespace MVS.Helpers
{
    internal class ProjectDurationStyle : StyleSelector
    {
        public override Style SelectStyle(object item, DependencyObject container)
        {
            if (item is Project)
            {
                Project project = item as Project;
                switch (project.projectStatus)
                {
                    case ProjectStatusType.GREEN:
                        return ProjectStatusGreenStyle;

                    case ProjectStatusType.AMBER:
                        return ProjectStatusAmberStyle;

                    case ProjectStatusType.RED:
                        return ProjectStatusRedStyle;

                    default:
                        return ProjectStatusOffStyle;
                }
            }
            return null;
        }
        public Style ProjectStatusGreenStyle { get; set; }
        public Style ProjectStatusAmberStyle { get; set; }
        public Style ProjectStatusRedStyle { get; set; }
        public Style ProjectStatusOffStyle { get; set; }
    }
}
