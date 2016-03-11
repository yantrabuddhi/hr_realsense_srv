using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
namespace HR_RealSense_Srv1
{
    [DataContract]
    class HandJSON
    {
        [DataMember]
        public bool leftHandWave;
        [DataMember]
        public bool rightHandWave;

        public HandJSON()
        {
            reset();
        }
        public HandJSON(bool leftWave,bool rightWave)
        {
            leftHandWave = leftWave;
            rightHandWave = rightWave;
        }
        public void reset()
        {
            leftHandWave = false;
            rightHandWave = false;
        }
    }
}
