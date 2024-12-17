using System;

namespace MVS
{
    public class ProjectData
    {
        public int id;
        public DateTime timestamp;

        public double refPitch;
        public double refRoll;
        public double refHeave;

        public double testPitch;
        public double testRoll;
        public double testHeave;

        // Initialize
        public ProjectData()
        {
        }

        public ProjectData(ProjectData session)
        {
            Set(session);
        }

        public void Set(ProjectData session)
        {
            if (session != null)
            {
                id = session.id;
                timestamp = session.timestamp;
                refPitch = session.refPitch;
                refRoll = session.refRoll;
                refHeave = session.refHeave;
                testPitch = session.testPitch;
                testRoll = session.testRoll;
                testHeave = session.testHeave;
            }
        }
    }
}