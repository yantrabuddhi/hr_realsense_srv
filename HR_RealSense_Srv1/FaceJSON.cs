using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
//using System.Collections.Generic;

namespace HR_RealSense_Srv1
{

    [DataContract]
    class FacesArray
    {
        [DataMember]
        public int image_width;
        [DataMember]
        public int image_height;
        [DataMember]
        public List<FaceJSON> faces;
    }
    [DataContract]
    class FaceJSON
    {
        [DataMember]
        public int face_id;
        [DataMember]
        public bool known_face;
        [DataMember]
        public int rec_id;
        [DataMember]
        public float xF;//face rec location x
        [DataMember]
        public float yF;//face rec location y
        //[DataMember]
        //public float zF;
        [DataMember]
        public int xRect;//face rectangle relative to image width in faces array
        [DataMember]
        public int yRect;
        [DataMember]
        public int wRect;
        [DataMember]
        public int hRect;
        [DataMember]
        public float roll;//face orientation
        [DataMember]
        public float yaw;
        [DataMember]
        public float pitch;
        [DataContract]
        public struct expr
        {
            [DataMember]
            public string expr_name;
            [DataMember]
            public int intensity;
        };
        [DataMember]
        public expr[] expr_array;//array of expression and intensity

        private int n_expr;

        public void set_face_location(PointF loc)
        {
            xF = loc.X;
            yF = loc.Y;
        }
        public void set_face_rect(Rectangle rc)
        {
            xRect = rc.X;
            yRect = rc.Y;
            wRect = rc.Width;
            hRect = rc.Height;
        }

        public FaceJSON(int expr_num)
        {
            n_expr = expr_num;
            expr_array = new expr[n_expr];
        }
    }
}
