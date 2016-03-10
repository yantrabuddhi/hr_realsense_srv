using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
//using System.Collections.Generic;
namespace HR_RealSense_Srv1
{
    class FaceDataSerializer
    {
        public int face_id;
        public bool known_face;
        public Rectangle face_rect;
        public float roll, yaw, pitch;
        public struct expr { public string expr_name; public int intensity; };
        public expr[] expr_array;
        private int n_expr;
        public int rec_id;
        public PointF rec_location;

        public FaceDataSerializer(int expr_num)
        {
            n_expr = expr_num;
            expr_array = new expr[n_expr];
        }

        public override string ToString()
        {
            string result="[";
            result +="faceId="+ face_id.ToString()+",";
            result +="known_face=" + (known_face ? "1" : "0")+",";
            result += "rec_id=" + rec_id.ToString() + ",";
            result += "X=" + face_rect.X.ToString() + ",";
            result += "Y=" + face_rect.Y.ToString() + ",";
            result += "W=" + face_rect.Width.ToString() + ",";
            result += "H=" + face_rect.Height.ToString() + ",";
            result += "roll=" + roll.ToString() + ",";
            result += "yaw=" + yaw.ToString() + ",";
            result += "pitch=" + pitch.ToString()+",";
            for (int i=0;i<expr_array.Length;i++)
            {
                result += "fe=" + expr_array[i].expr_name + ":" + expr_array[i].intensity.ToString()+",";
            }
            result += "]";
            return result;
        }
    }
}
