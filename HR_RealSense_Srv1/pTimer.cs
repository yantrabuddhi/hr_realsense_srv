using System.Runtime.InteropServices;

namespace HR_RealSense_Srv1
{
    class pTimer
    {
        [DllImport("Kernel32.dll")]
        private static extern bool QueryPerformanceCounter(out long data);
        [DllImport("Kernel32.dll")]
        private static extern bool QueryPerformanceFrequency(out long data);

        private long freq, last;
        private int fps;

        public pTimer()
        {
            QueryPerformanceFrequency(out freq);
            fps = 0;
            QueryPerformanceCounter(out last);
        }

        public bool Tick()
        {
            long now;
            QueryPerformanceCounter(out now);
            fps++;
            if (now - last > freq) // update every second
            {
                last = now;
                fps = 0;
                return true;
            }
            return false;
        }

    }
}
