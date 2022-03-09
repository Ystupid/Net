using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Net.General.Flag
{
    public class MultipleFlag
    {
        private CustomFlag[] m_FlagArray;
        private int m_MaxCapacity;

        public CustomFlag this[int index]
        {
            get
            {
                if (m_FlagArray[index] == null)
                    m_FlagArray[index] = new CustomFlag(m_MaxCapacity);
                return m_FlagArray[index];
            }
        }

        public MultipleFlag(int flagCount, int maxCapacity = 32)
        {
            m_MaxCapacity = maxCapacity;
            m_FlagArray = new CustomFlag[flagCount];
        }
    }
}
