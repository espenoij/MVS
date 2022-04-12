using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telerik.Windows.Controls.ChartView;

namespace HMS_Client
{
    public class MyAnnotationsDescriptorSelector : ChartAnnotationDescriptorSelector
    {
        public ChartAnnotationDescriptor MarkedZoneAnnotationDescriptor { get; set; }

        public override ChartAnnotationDescriptor SelectDescriptor(ChartAnnotationsProvider provider, object context)
        {
            if (context is MarkedZoneAnnotationModel)
            {
                return this.MarkedZoneAnnotationDescriptor;
            }

            return base.SelectDescriptor(provider, context);
        }
    }
}
