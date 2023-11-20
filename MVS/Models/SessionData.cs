using System;

namespace MVS
{
    public class SessionData
    {
        private int id;
        private DateTime timestamp;

        private double refPitch;
        private double refRoll;
        private double refHeave;

        private double testPitch;
        private double testRoll;
        private double testHeave;

        // Initialize
        public SessionData()
        {
        }

        public SessionData(SessionData session)
        {
            Set(session);
        }

        public void Set(SessionData session)
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