using System;

namespace Data
{
    [Serializable]
    public class Score
    {
        public int Current;
        public int Highest;

        public Score(int current, int highest)
        {
            Current = current;
            Highest = highest;
        }
    }
}
