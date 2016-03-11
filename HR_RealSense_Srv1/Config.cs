using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace HR_RealSense_Srv1
{
    class Config
    {
        public static string http_face_url="http://127.0.0.1:8000/FaceRec";
        public static string http_hand_url = "http://127.0.0.1:8000/Hand";
        public PXCMSession Session;
        private const int LandmarkAlignment = -3;
        private const int DefaultNumberOfFaces = 4;
        public int MaxFaces = 4;
        public bool Register = true;
        public bool Unregister = false;
        public int image_width_f = 1280;
        public int image_height_f = 720;
        private FaceOrganisation m_faceOrg;
        //private FaceDataSerializer fs;
        private FaceJSON fs;
        //public tcpServe m_comm;//change to json http
        public httpClient m_comm;

        //private readonly Dictionary<PXCMFaceData.ExpressionsData.FaceExpression, Bitmap> m_cachedExpressions =
        //    new Dictionary<PXCMFaceData.ExpressionsData.FaceExpression, Bitmap>();
        private readonly Dictionary<PXCMFaceData.ExpressionsData.FaceExpression, string> m_expressionDictionary =
            new Dictionary<PXCMFaceData.ExpressionsData.FaceExpression, string>
            {
                {PXCMFaceData.ExpressionsData.FaceExpression.EXPRESSION_MOUTH_OPEN, @"MouthOpen"},
                {PXCMFaceData.ExpressionsData.FaceExpression.EXPRESSION_SMILE, @"Smile"},
                {PXCMFaceData.ExpressionsData.FaceExpression.EXPRESSION_KISS, @"Kiss"},
                {PXCMFaceData.ExpressionsData.FaceExpression.EXPRESSION_EYES_UP, @"Eyes_Turn_Up"},
                {PXCMFaceData.ExpressionsData.FaceExpression.EXPRESSION_EYES_DOWN, @"Eyes_Turn_Down"},
                {PXCMFaceData.ExpressionsData.FaceExpression.EXPRESSION_EYES_TURN_LEFT, @"Eyes_Turn_Left"},
                {PXCMFaceData.ExpressionsData.FaceExpression.EXPRESSION_EYES_TURN_RIGHT, @"Eyes_Turn_Right"},
                {PXCMFaceData.ExpressionsData.FaceExpression.EXPRESSION_EYES_CLOSED_LEFT, @"Eyes_Closed_Left"},
                {PXCMFaceData.ExpressionsData.FaceExpression.EXPRESSION_EYES_CLOSED_RIGHT, @"Eyes_Closed_Right"},
                {PXCMFaceData.ExpressionsData.FaceExpression.EXPRESSION_BROW_LOWERER_RIGHT, @"Brow_Lowerer_Right"},
                {PXCMFaceData.ExpressionsData.FaceExpression.EXPRESSION_BROW_LOWERER_LEFT, @"Brow_Lowerer_Left"},
                {PXCMFaceData.ExpressionsData.FaceExpression.EXPRESSION_BROW_RAISER_RIGHT, @"Brow_Raiser_Right"},
                {PXCMFaceData.ExpressionsData.FaceExpression.EXPRESSION_BROW_RAISER_LEFT, @"Brow_Raiser_Left"}
            };

        public Config(PXCMSession session,httpClient comm)
        {
            Session = session;
            m_faceOrg = new FaceOrganisation();
            fs = new FaceJSON(m_expressionDictionary.Count());//new FaceDataSerializer(m_expressionDictionary.Count());
            m_comm = comm;
        }
        public string ToJSON(FacesArray faj)
        {
            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(FacesArray));
            MemoryStream ms = new MemoryStream();
            ser.WriteObject(ms, faj);
            ms.Position = 0;
            StreamReader sr = new StreamReader(ms);
            string result = sr.ReadToEnd();
            Console.WriteLine(result);
            return result;
        }
        public string ToJSON(HandJSON hnd)
        {
            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(HandJSON));
            MemoryStream ms = new MemoryStream();
            ser.WriteObject(ms, hnd);
            ms.Position = 0;
            StreamReader sr = new StreamReader(ms);
            string result = sr.ReadToEnd();
            Console.WriteLine(result);
            return result;
        }

        public bool readConfig() { return true; }
        public string GetFileName() { return "facefile.dat"; }
        public string GetGestureName() { return "wave"; }
        public bool IsExpressionsEnabled() { return true; }
        public bool IsPulseEnabled() { return false; }
        public bool IsRecognitionChecked() { return true; }

        public void setImageSize(int wdth, int ht)
        {
            image_width_f = wdth;
            image_height_f = ht;
        }
        public bool publishFaceData(PXCMFaceData moduleOutput)
        {
            Debug.Assert(moduleOutput != null);
            FacesArray farr = new FacesArray();
            int j = moduleOutput.QueryNumberOfDetectedFaces();
            if (j < 1) return false;
            farr.image_height = image_height_f;
            farr.image_width = image_width_f;
            farr.faces = new List<FaceJSON>(m_expressionDictionary.Count);

            for (var i = 0; i < j; i++)
            {
                fs = new FaceJSON(m_expressionDictionary.Count());
                PXCMFaceData.Face face = moduleOutput.QueryFaceByIndex(i);
                if (face == null)
                {
                    throw new Exception("publishFaceData::PXCMFaceData.Face null");
                }

                //lock (m_faceOrg)
                //{
                m_faceOrg.ChangeFace(i, face, image_height_f, image_width_f);
                //}

                getLocation(face);//should also pass fs?
                //getLandmark(face);
                getPose(face);
                //getPulse(face);
                getExpressions(face);
                getRecognition(face);
                //now send fs.ToString() on tcp .. mandeep
                //var fst = new FaceJSON(m_expressionDictionary.Count);
                if (fs.expr_array[0].expr_name!=null)
                farr.faces.Add(fs);
            }
            if (farr.faces.Count > 0)
            {
                m_comm.SendFace(ToJSON(farr));
                return true;
            }
            return false;
        }//publish face
        public void getLocation(PXCMFaceData.Face face)
        {
            Debug.Assert(face != null);
            //if (m_bitmap == null || !Detection.Checked) return;

            PXCMFaceData.DetectionData detection = face.QueryDetection();
            if (detection == null)
                return;

            /*
            lock (m_bitmapLock)
            {
                using (Graphics graphics = Graphics.FromImage(m_bitmap))
                using (var pen = new Pen(m_faceTextOrganizer.Colour, 3.0f))
                using (var brush = new SolidBrush(m_faceTextOrganizer.Colour))
                using (var font = new Font(FontFamily.GenericMonospace, m_faceTextOrganizer.FontSize, FontStyle.Bold))
                {
                    graphics.DrawRectangle(pen, m_faceTextOrganizer.RectangleLocation);
                    String faceId = String.Format("Face ID: {0}",
                        face.QueryUserID().ToString(CultureInfo.InvariantCulture));
                    graphics.DrawString(faceId, font, brush, m_faceTextOrganizer.FaceIdLocation);
                }
            }*/
            ////Console.WriteLine("Face Id: {0} = {1}", face.QueryUserID(),m_faceOrg.RectangleLocation.ToString());
            fs.face_id = face.QueryUserID();
            //fs.face_rect = m_faceOrg.RectangleLocation;
            fs.set_face_rect(m_faceOrg.RectangleLocation);
            fs.set_face_location(m_faceOrg.RecognitionLocation);
        }

        public void getLandmark(PXCMFaceData.Face face)
        {
            Debug.Assert(face != null);
            PXCMFaceData.LandmarksData landmarks = face.QueryLandmarks();
            if (landmarks == null) return;

            //lock (m_bitmapLock)
            //{
            /*
                using (Graphics graphics = Graphics.FromImage(m_bitmap))
                using (var brush = new SolidBrush(Color.White))
                using (var lowConfidenceBrush = new SolidBrush(Color.Red))
                using (var font = new Font(FontFamily.GenericMonospace, m_faceTextOrganizer.FontSize, FontStyle.Bold))
              */
            //  {

            PXCMFaceData.LandmarkPoint[] points;
            bool res = landmarks.QueryPoints(out points);
            Debug.Assert(res);

            var point = new PointF();

            foreach (PXCMFaceData.LandmarkPoint landmark in points)
            {
                point.X = landmark.image.x + LandmarkAlignment;
                point.Y = landmark.image.y + LandmarkAlignment;

                if (landmark.confidenceImage == 0)
                    Console.WriteLine("x - {0}", point);
                else
                    Console.WriteLine("• - {0}", point);
            }
            //  }
            //}
        }

        public void getPose(PXCMFaceData.Face face)
        {
            Debug.Assert(face != null);
            PXCMFaceData.PoseEulerAngles poseAngles;
            PXCMFaceData.PoseData pdata = face.QueryPose();
            if (pdata == null)
            {
                return;
            }
            if (!pdata.QueryPoseAngles(out poseAngles)) return;

            //lock (m_bitmapLock)
            //{
            /* using (Graphics graphics = Graphics.FromImage(m_bitmap))
             using (var brush = new SolidBrush(m_faceTextOrganizer.Colour))
             using (var font = new Font(FontFamily.GenericMonospace, m_faceTextOrganizer.FontSize, FontStyle.Bold))

             {
                 string yawText = String.Format("Yaw = {0}",
                     Convert.ToInt32(poseAngles.yaw).ToString(CultureInfo.InvariantCulture));
                 graphics.DrawString(yawText, font, brush, m_faceTextOrganizer.PoseLocation.X,
                     m_faceTextOrganizer.PoseLocation.Y);

                 string pitchText = String.Format("Pitch = {0}",
                     Convert.ToInt32(poseAngles.pitch).ToString(CultureInfo.InvariantCulture));
                 graphics.DrawString(pitchText, font, brush, m_faceTextOrganizer.PoseLocation.X,
                     m_faceTextOrganizer.PoseLocation.Y + m_faceTextOrganizer.FontSize);

                 string rollText = String.Format("Roll = {0}",
                     Convert.ToInt32(poseAngles.roll).ToString(CultureInfo.InvariantCulture));
                 graphics.DrawString(rollText, font, brush, m_faceTextOrganizer.PoseLocation.X,
                     m_faceTextOrganizer.PoseLocation.Y + 2 * m_faceTextOrganizer.FontSize);
             }
             */
            //Console.WriteLine("Yaw - {0}", poseAngles.yaw);
            //Console.WriteLine("Pitch - {0}", poseAngles.pitch);
            //Console.WriteLine("Roll - {0}", poseAngles.roll);
            fs.roll = poseAngles.roll;
            fs.yaw = poseAngles.yaw;
            fs.pitch = poseAngles.pitch;
            //}
        }

        public void getExpressions(PXCMFaceData.Face face)
        {
            Debug.Assert(face != null);
            if (!IsExpressionsEnabled()) return;

            PXCMFaceData.ExpressionsData expressionsOutput = face.QueryExpressions();

            if (expressionsOutput == null) return;

            //lock (m_bitmapLock)
            //{
            //using (Graphics graphics = Graphics.FromImage(m_bitmap))
            //using (var brush = new SolidBrush(m_faceTextOrganizer.Colour))
            //{
            ////const int imageSizeWidth = 18;
            ////const int imageSizeHeight = 18;

            int positionX = m_faceOrg.ExpressionsLocation.X;
            //int positionXText = positionX + imageSizeWidth;
            int positionY = m_faceOrg.ExpressionsLocation.Y;
            //int positionYText = positionY + imageSizeHeight / 4;
            int cnt = 0;
            foreach (var expressionEntry in m_expressionDictionary)
            {
                PXCMFaceData.ExpressionsData.FaceExpression expression = expressionEntry.Key;
                PXCMFaceData.ExpressionsData.FaceExpressionResult result;
                bool status = expressionsOutput.QueryExpression(expression, out result);
                if (!status) continue;

                //Bitmap cachedExpressionBitmap;
                //bool hasCachedExpressionBitmap = m_cachedExpressions.TryGetValue(expression, out cachedExpressionBitmap);
                //if (!hasCachedExpressionBitmap)
                //{
                //    cachedExpressionBitmap = (Bitmap)m_resources.GetObject(expressionEntry.Value);
                //    m_cachedExpressions.Add(expression, cachedExpressionBitmap);
                //}

                //using (var font = new Font(FontFamily.GenericMonospace, m_faceTextOrganizer.FontSize, FontStyle.Bold))
                //{
                //    Debug.Assert(cachedExpressionBitmap != null, "cachedExpressionBitmap != null");
                //graphics.DrawImage(cachedExpressionBitmap, new Rectangle(positionX, positionY, imageSizeWidth, imageSizeHeight));
                var expressionText = String.Format("= {0}", result.intensity);
                //graphics.DrawString(expressionText, font, brush, positionXText, positionYText);

                //positionY += imageSizeHeight;
                //positionYText += imageSizeHeight;
                //}
                ////Console.Write(m_expressionDictionary[expression]);
                ///Console.WriteLine(expressionText);
                fs.expr_array[cnt].expr_name = expression.ToString();//m_expressionDictionary[expression];
                fs.expr_array[cnt].intensity = result.intensity;
                cnt++;
                // }
                //}
            }
        }

        public void getRecognition(PXCMFaceData.Face face)
        {
            Debug.Assert(face != null);
            if (!IsRecognitionChecked()) return;

            PXCMFaceData.RecognitionData qrecognition = face.QueryRecognition();
            if (qrecognition == null)
            {
                throw new Exception(" PXCMFaceData.RecognitionData null");
            }
            var userId = qrecognition.QueryUserID();
            var recognitionText = userId == -1 ? "Not Registered" : String.Format("Registered ID: {0}", userId);

            //lock (m_bitmapLock)
            //{
            //    using (Graphics graphics = Graphics.FromImage(m_bitmap))
            //    using (var brush = new SolidBrush(m_faceTextOrganizer.Colour))
            //    using (var font = new Font(FontFamily.GenericMonospace, m_faceTextOrganizer.FontSize, FontStyle.Bold))
            //    {
            fs.known_face = !(userId == -1);
            fs.rec_id = userId;
            ////Console.Write(recognitionText);
            ////Console.WriteLine("= {0}", m_faceOrg.RecognitionLocation);
            //    }
            //}
        }

        private void getPulse(PXCMFaceData.Face face)
        {
            Debug.Assert(face != null);
            if (!IsPulseEnabled()) return;

            var pulseData = face.QueryPulse();
            if (pulseData == null)
                return;

            //lock (m_bitmapLock)
            //{
            var pulseString = "Pulse: " + pulseData.QueryHeartRate();
            /*
                using (var graphics = Graphics.FromImage(m_bitmap))
                using (var brush = new SolidBrush(m_faceTextOrganizer.Colour))
                using (var font = new Font(FontFamily.GenericMonospace, m_faceTextOrganizer.FontSize, FontStyle.Bold))
                */
            //{
            ////Console.Write(pulseString);
            ////Console.WriteLine("= {0}", m_faceOrg.PulseLocation);
            //}
            //}

        }
    }
}
