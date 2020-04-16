using SmartHunter.Core.Data;

namespace SmartHunter.Game.Data
{
    public class Player : Bindable
    {
        int m_Index;

        public int Index
        {
            get { return m_Index; }
            set { SetProperty(ref m_Index, value); }
        }

        string m_Name;
        public string Name
        {
            get { return m_Name; }
            set { SetProperty(ref m_Name, value); }
        }

        int m_Damage;
        public int Damage
        {
            get { return m_Damage; }
            set {
                SetProperty(ref m_Damage, value);
                CalculateAndUpdateDPS();
            }
        }

        float m_DamageFraction;
        public float DamageFraction
        {
            get { return m_DamageFraction; }
            set { SetProperty(ref m_DamageFraction, value); }
        }

        float m_BarFraction;
        public float BarFraction
        {
            get { return m_BarFraction; }
            set { SetProperty(ref m_BarFraction, value); }
        }

        // Variables to calculate and store DPS
        long m_LastTick;
        float m_DamageAtLastTick;
        float m_AccumulatedDamage;
        long m_AccumulatedTime;

        float[] rollingDPSWindow = new float[15];
        long[] rollingDPSTimes = new long[15]; // parallel array to above
        long nextWindowIndex = 0;

        float m_TrueDPS;
        public int TrueDPS
        {
            get { return (int)m_TrueDPS; }
            set { SetProperty(ref m_TrueDPS, value); }
        }

        float m_MostRecentDPS;
        public int MostRecentDPS
        {
            get { return (int)m_MostRecentDPS; }
            set { SetProperty(ref m_MostRecentDPS, value); }
        }

        float m_AvgDamagePerHit;
        public int AvgDamagePerHit
        {
            get { return (int)m_AvgDamagePerHit; }
            set { SetProperty(ref m_AvgDamagePerHit, value); }
        }

        void CalculateAndUpdateDPS()
        {
            long currentTime = System.DateTimeOffset.Now.ToUnixTimeMilliseconds();
            long timeDiff = currentTime - m_LastTick;
            m_LastTick = currentTime;

            var damageSinceLastTick = m_Damage - m_DamageAtLastTick;
            m_DamageAtLastTick = m_Damage; // update the last damage
            m_AccumulatedDamage += damageSinceLastTick;
            m_AccumulatedTime += timeDiff; // track damage time events
            TrueDPS = (int)(m_AccumulatedDamage / m_AccumulatedTime);
            if (damageSinceLastTick <= 0.01)
            { return; }
            var damageDoneInTimeInterval = damageSinceLastTick / timeDiff;

            // Rolling window damage calculation and logic
            rollingDPSWindow[nextWindowIndex] = damageSinceLastTick;
            rollingDPSTimes[nextWindowIndex] = currentTime;
            nextWindowIndex += 1;
            if (nextWindowIndex >= 15)
            {
                nextWindowIndex = 0;
            }

            // calculate avg dmage per hit
            AvgDamagePerHit = (int)Average(rollingDPSWindow);

            // Calculate the rolling dps
            MostRecentDPS = (int)Average(rollingDPSWindow, rollingDPSTimes);

        }

        private float Average(float[] values)
        {

            if (values.Length == 0)
            {
                return 0;
            }

            float totalValue = 0;
            int numValuesUsed = 0;
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] <= 0.01)
                { continue; }
                totalValue += values[i];
                numValuesUsed++;
            }

            return totalValue / numValuesUsed;
        }

        private float Average(float[] values, long[] times) {
            if (values.Length == 0)
            {
                return 0;
            }

            float totalValue = 0;
            long totalTime = 0;
            long lowestTime = long.MaxValue;
            int numValuesUsed = 0;
            long now = System.DateTimeOffset.Now.ToUnixTimeMilliseconds();
            for (int i = 0; i < values.Length; i++)
            {
                // We don't care if the time is 0
                if (times[i] == 0 || times[i] - now > 15000) // also ignore entries more than 15 seconds ago. Only include the most recent 15 seconds
                {
                    continue;
                }
                numValuesUsed++;
                totalValue += values[i];
                totalTime += times[i];
                if (times[i] < lowestTime) {
                    lowestTime = times[i];
                }
            }

            long adjustedTime = ((totalTime/numValuesUsed) - lowestTime)/1000; // subtract the lowest time in the array because we want relative time -- that is the total time that has elapsed in the window captured
            return totalValue / adjustedTime;
        }
    }
}
