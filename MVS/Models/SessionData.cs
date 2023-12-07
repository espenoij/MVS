using System;

namespace MVS
{
    public class SessionData
    {
        public int id;
        public DateTime timestamp;

        public double refPitch;
        public double refRoll;
        public double refHeaveAmplitude;

        public double testPitch;
        public double testRoll;
        public double testHeaveAmplitude;

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
                refHeaveAmplitude = session.refHeaveAmplitude;
                testPitch = session.testPitch;
                testRoll = session.testRoll;
                testHeaveAmplitude = session.testHeaveAmplitude;
            }
        }
    }
}