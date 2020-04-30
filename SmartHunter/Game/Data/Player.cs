using System.Runtime.InteropServices;
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

        float m_rollingAvgDamage;
        float m_rollingTotalDamage;
        long m_rollingTotalTime;

        float[] rollingDPSWindow = new float[60];
        long[] rollingDPSTimes = new long[60]; // parallel array to above
        long nextWindowIndex = 0;

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

        long lastHitAt = 0;
        float comboTotalSoFar = 0;
        float m_LastComboDamage;
        public int LastComboDamage
        {
            get { return (int)m_LastComboDamage; }
            set { SetProperty(ref m_LastComboDamage, value); }
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

            long timeSinceLastHit = currentTime - lastHitAt;
            
            if (timeSinceLastHit > 6000)
            {
                comboTotalSoFar = 0;

            } else if (timeSinceLastHit <= 6000 && damageSinceLastTick > 0.01)
            {
                comboTotalSoFar += damageSinceLastTick;
            }

            if (damageSinceLastTick <= 0.01)
            {
                CalculateAndSetComboDamage();
                CalculateAndSetAverages();
                return;
            }

            // Calculate combo damage
            lastHitAt = currentTime;
            CalculateAndSetComboDamage();

            // Rolling window damage calculation and logic
            rollingDPSWindow[nextWindowIndex] = damageSinceLastTick;
            rollingDPSTimes[nextWindowIndex] = currentTime;
            nextWindowIndex += 1;
            if (nextWindowIndex >= rollingDPSWindow.Length)
            {
                nextWindowIndex = 0;
            }

            CalculateAndSetAverages();

        }

        private void CalculateAndSetComboDamage()
        {
            LastComboDamage = (int)comboTotalSoFar;
        }

        private void CalculateAndSetAverages()
        {
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
                if (values[i] <= 0.1)
                { continue; }
                totalValue += values[i];
                numValuesUsed++;
            }

            if (numValuesUsed == 0)
            {
                return 0;
            } 

            return totalValue / numValuesUsed;
        }

        private float Average(float[] values, long[] times, bool altStyle = false) {
            float totalValue = 0;
            long totalTime = 0;
            int numValuesUsed = 0;
            long now = System.DateTimeOffset.Now.ToUnixTimeMilliseconds();
            for (int i = 0; i < values.Length; i++)
            {
                // We don't care if the time is 0
                if (values[i] < 0.1 || times[i] == 0 || now - times[i] > 8000) // also ignore entries more than some seconds ago. Only include the most recent some seconds
                {
                    continue;
                }
                
                var t = now - times[i];
                if (t > 0)
                {
                    totalTime += t;
                    numValuesUsed++;
                    totalValue += values[i];
                }
            }

            if (numValuesUsed == 0 || totalValue < 0.1)
            {
                return -1;
            }

            long adjustedTime = (totalTime/numValuesUsed)/1000; // subtract the lowest time in the array because we want relative time -- that is the total time that has elapsed in the window captured
            if (adjustedTime <= 0)
            {
                return totalValue / numValuesUsed;
            }

            if (altStyle)
            {
                return totalValue / (totalTime/1000);
            }

            return totalValue / adjustedTime;
        }
    }
}
