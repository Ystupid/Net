using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Net.General.Flag
{
    public class CustomFlag
    {
        private delegate void ForeachAction(ref int value);

        public const int MaxFlagBit = 32;

        private int m_ArrayLength;
        private int[] m_FlagArray;

        protected int this[int index]
        {
            get => m_FlagArray[index];
            set => m_FlagArray[index] = value;
        }

        protected int ToIndex(float flag) => (int)Math.Ceiling(flag / MaxFlagBit) - 1;
        protected int ToBit(int flag) => 1 << ((flag - 1) % MaxFlagBit);

        public CustomFlag(int maxCapacity = 32)
        {
            m_ArrayLength = ToIndex(maxCapacity) + 1;
            m_FlagArray = new int[m_ArrayLength];
        }

        public void Active(int flag) => this[ToIndex(flag)] |= ToBit(flag);
        public void Disable(int flag) => this[ToIndex(flag)] &= ~ToBit(flag);
        public bool IsActive(int flag) => (this[ToIndex(flag)] & ToBit(flag)) == flag;
        public void Clear() => Foreach((ref int value) => value = 0);

        private void Foreach(ForeachAction action)
        {
            if (action == null) return;

            for (int i = m_ArrayLength - 1; i >= 0; i--)
                action(ref m_FlagArray[i]);
        }

        public override string ToString()
        {
            var content = "";
            for (int i = 0; i < m_ArrayLength; i++)
            {
                var bitString = Convert.ToString(this[i], 2);
                var remainingBit = MaxFlagBit - bitString.Length;

                if (remainingBit > 0)
                {
                    var remainingString = new string('0', remainingBit);
                    bitString = remainingString + bitString;
                }

                content += bitString + " ";
            }

            return content;
        }
    }
}
