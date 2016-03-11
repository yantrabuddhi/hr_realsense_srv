using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
namespace HR_RealSense_Srv1
{
    class FaceOrganisation
    {
        //private Color m_color;
        private PXCMRectI32 m_rectangle;

        public PointF RecognitionLocation
        {
            get { return new PointF((m_rectangle.x + m_rectangle.w) / 2, (m_rectangle.y + m_rectangle.h) / 2); }
        }

        public Rectangle RectangleLocation
        {
            get { return new Rectangle(m_rectangle.x, m_rectangle.y, m_rectangle.w, m_rectangle.h); }
        }


        public void ChangeFace(int faceIndex, PXCMFaceData.Face face, int imageHeight, int imageWidth)
        {

            PXCMFaceData.DetectionData fdetectionData = face.QueryDetection();
            //m_color = m_colorList[faceIndex % m_colorList.Length];

            if (fdetectionData != null)
            {
                fdetectionData.QueryBoundingRect(out m_rectangle);
            }
        }

    }
}
