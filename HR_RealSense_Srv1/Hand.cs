using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HR_RealSense_Srv1
{
    class Hand
    {
        Config cfg;
        PXCMSession session;
        PXCMSenseManager instance;
        PXCMCaptureManager captureManager;
        //bool _disconnected;
        //PXCMHandModule handAnalysis;
        //PXCMHandCursorModule handCursorAnalysis;
        //PXCMHandConfiguration handConfiguration = null;
        //PXCMHandData handData = null;
        //PXCMSenseManager.Handler handler;
        bool liveCamera;

        Dictionary<string, PXCMCapture.DeviceInfo> Devices;
        PXCMCapture.DeviceInfo info;

        private void PopulateDevice()
        {
            Devices = new Dictionary<string, PXCMCapture.DeviceInfo>();

            PXCMSession.ImplDesc desc = new PXCMSession.ImplDesc();
            desc.group = PXCMSession.ImplGroup.IMPL_GROUP_SENSOR;
            desc.subgroup = PXCMSession.ImplSubgroup.IMPL_SUBGROUP_VIDEO_CAPTURE;
            for (int i = 0; ; i++)
            {
                PXCMSession.ImplDesc desc1;
                if (session.QueryImpl(desc, i, out desc1) < pxcmStatus.PXCM_STATUS_NO_ERROR) break;
                PXCMCapture capture;
                if (session.CreateImpl<PXCMCapture>(desc1, out capture) < pxcmStatus.PXCM_STATUS_NO_ERROR) continue;
                for (int j = 0; ; j++)
                {
                    PXCMCapture.DeviceInfo dinfo;
                    if (capture.QueryDeviceInfo(j, out dinfo) < pxcmStatus.PXCM_STATUS_NO_ERROR) break;
                    string name = dinfo.name;
                    if (Devices.ContainsKey(dinfo.name))
                    {
                        name += j;
                    }
                    Devices.Add(name, dinfo);
                    //Console.WriteLine("Name: " + name);
                    if (dinfo.model == PXCMCapture.DeviceModel.DEVICE_MODEL_SR300) info = dinfo;
                }
                capture.Dispose();
            }
        }



        public Hand(Config config)
        {
            cfg = config;
            instance = null;
            info = null;
            session = PXCMSession.CreateInstance();
            instance = session.CreateSenseManager();
            if (instance == null)
            {
                Console.WriteLine("Failed creating SenseManager");
                return;
            }

            captureManager = instance.captureManager;
            PopulateDevice();
            //_form.Devices.TryGetValue(_form.GetCheckedDevice(), out info);
            if (captureManager != null)
            {
                //if (_form.GetRecordState())
                //{
                //    captureManager.SetFileName(_form.GetFileName(), true);
                //    if (_form.Devices.TryGetValue(_form.GetCheckedDevice(), out info))
                //    {
                //        captureManager.FilterByDeviceInfo(info);
                //    }

                //}
                //else if (_form.GetPlaybackState())
                //{
                //    captureManager.SetFileName(_form.GetFileName(), false);
                //    info = _form.GetDeviceFromFileMenu(_form.GetFileName());

                //}
                //else
                //{
                captureManager.FilterByDeviceInfo(info);
                ////captureManager.FilterByStreamProfiles(PXCMCapture.StreamType.STREAM_TYPE_COLOR, 1280, 720, 0);
                liveCamera = true;
                Console.WriteLine("capture manager live cam");
                //}
            }
            /* Set Module */

            // handler = new PXCMSenseManager.Handler();
            // handler.onModuleProcessedFrame = new PXCMSenseManager.Handler.OnModuleProcessedFrameDelegate(OnNewFrame);

            //full hand gesture
            /*
            pxcmStatus status = instance.EnableHand();
            handAnalysis = instance.QueryHand();

            if (status != pxcmStatus.PXCM_STATUS_NO_ERROR || handAnalysis == null)
            {
                Console.WriteLine("Failed Loading Module");
                return;
            }

            handConfiguration = handAnalysis.CreateActiveConfiguration();
            if (handConfiguration == null)
            {
                Console.WriteLine("Failed Create Configuration");
                instance.Close();
                instance.Dispose();
                return;
            }
            handData = handAnalysis.CreateOutput();
            if (handData == null)
            {
                Console.WriteLine("Failed Create Output");
                handConfiguration.Dispose();
                instance.Close();
                instance.Dispose();
                return;
            }
            Console.WriteLine("Ïnit Hand started");
            */
        }

        public static pxcmStatus OnNewFrame(Int32 mid, PXCMBase module, PXCMCapture.Sample sample)
        {
            return pxcmStatus.PXCM_STATUS_NO_ERROR;
        }

        public void callOnceInLoop()
        {
            PXCMHandModule handAnalysis;
            //PXCMHandCursorModule handCursorAnalysis;
            PXCMHandConfiguration handConfiguration = null;
            PXCMHandData handData = null;
            //PXCMSenseManager.Handler handler;
            pxcmStatus status = instance.EnableHand();
            handAnalysis = instance.QueryHand();

            if (status != pxcmStatus.PXCM_STATUS_NO_ERROR || handAnalysis == null)
            {
                Console.WriteLine("Failed Loading Module");
                return;
            }

            handConfiguration = handAnalysis.CreateActiveConfiguration();
            if (handConfiguration == null)
            {
                Console.WriteLine("Failed Create Configuration");
                instance.Close();
                instance.Dispose();
                return;
            }
            handData = handAnalysis.CreateOutput();
            if (handData == null)
            {
                Console.WriteLine("Failed Create Output");
                handConfiguration.Dispose();
                instance.Close();
                instance.Dispose();
                return;
            }
            Console.WriteLine("Ïnit Hand started");
            float _maxRange;
            Console.WriteLine("in hand loop");
            PXCMSenseManager.Handler handler = new PXCMSenseManager.Handler();
            handler.onModuleProcessedFrame = new PXCMSenseManager.Handler.OnModuleProcessedFrameDelegate(OnNewFrame);

            if (instance.Init(handler) == pxcmStatus.PXCM_STATUS_NO_ERROR)
            {
                Console.WriteLine("hand instance init");
                PXCMCapture.DeviceInfo dinfo;
                PXCMCapture.DeviceModel dModel = PXCMCapture.DeviceModel.DEVICE_MODEL_F200;
                PXCMCapture.Device device = instance.captureManager.device;
                if (device != null)
                {
                    device.QueryDeviceInfo(out dinfo);
                    dModel = dinfo.model;
                    _maxRange = device.QueryDepthSensorRange().max;
                    Console.WriteLine(dModel.ToString());

                }
                if (handConfiguration != null)
                {
                    PXCMHandData.TrackingModeType trackingMode;// = PXCMHandData.TrackingModeType.TRACKING_MODE_FULL_HAND;

                    trackingMode = PXCMHandData.TrackingModeType.TRACKING_MODE_FULL_HAND;

                    handConfiguration.SetTrackingMode(trackingMode);

                    handConfiguration.EnableAllAlerts();
                    handConfiguration.EnableSegmentationImage(true);
                    bool isEnabled = handConfiguration.IsSegmentationImageEnabled();

                    handConfiguration.ApplyChanges();

                    /*
                    int totalNumOfGestures = handConfiguration.QueryGesturesTotalNumber();
                    
                    if (totalNumOfGestures > 0)
                    {
                        for (int i = 0; i < totalNumOfGestures; i++)
                        {
                            string gestureName = string.Empty;
                            if (handConfiguration.QueryGestureNameByIndex(i, out gestureName) ==
                                pxcmStatus.PXCM_STATUS_NO_ERROR)
                            {
                                Console.WriteLine(gestureName+" .. "+ (i + 1).ToString()+"  "+(gestureName=="wave").ToString());
                            }
                        }
                    }
                    */
                }
            }


            Console.WriteLine("Streaming Hand");
            int frameCounter = 0;
            int frameNumber = 0;

            string gestureNm = cfg.GetGestureName();
            if (handConfiguration != null)
            {
                if (string.IsNullOrEmpty(gestureNm) == false)                {
                    if (handConfiguration.IsGestureEnabled(gestureNm) == false)
                    {
                        Console.WriteLine("Enabling " + gestureNm);
                        handConfiguration.DisableAllGestures();
                        handConfiguration.EnableGesture(gestureNm, true);
                        handConfiguration.ApplyChanges();
                    }
                }
                else
                {
                    handConfiguration.DisableAllGestures();
                    handConfiguration.ApplyChanges();
                }
            }

            //Console.WriteLine("handData=" + (handData != null).ToString());
            while (true)
            {
                if (instance.AcquireFrame(true) < pxcmStatus.PXCM_STATUS_NO_ERROR)
                {
                    Console.WriteLine("Breaking Hand Loop");
                    break;
                }

                frameCounter++;
                PXCMCapture.Sample sample = instance.QueryHandSample();
                //Console.WriteLine(((sample != null && sample.depth != null)).ToString());
                if (sample != null && sample.depth != null)
                {
                    frameNumber = liveCamera ? frameCounter : instance.captureManager.QueryFrameIndex();
                    if (handData != null)
                    {
                        handData.Update();

                        //DisplayPicture(sample.depth, handData);
                        getGesture(handData, frameNumber);
                        //DisplayJoints(handData);
                        DisplayAlerts(handData, frameNumber);
                    }
                }

                instance.ReleaseFrame();
            }//while

        }

        private void getGesture(PXCMHandData handAnalysis, int frameNumber)
        {

            int firedGesturesNumber = handAnalysis.QueryFiredGesturesNumber();
            string gestureStatusLeft = string.Empty;
            string gestureStatusRight = string.Empty;

            if (firedGesturesNumber == 0)
            {
                return;
            }
            Console.WriteLine(firedGesturesNumber.ToString());

            for (int i = 0; i < firedGesturesNumber; i++)
            {
                PXCMHandData.GestureData gestureData;
                if (handAnalysis.QueryFiredGestureData(i, out gestureData) == pxcmStatus.PXCM_STATUS_NO_ERROR)
                {
                    PXCMHandData.IHand handData;
                    if (handAnalysis.QueryHandDataById(gestureData.handId, out handData) != pxcmStatus.PXCM_STATUS_NO_ERROR)
                        return;

                    PXCMHandData.BodySideType bodySideType = handData.QueryBodySide();
                    if (bodySideType == PXCMHandData.BodySideType.BODY_SIDE_LEFT)
                    {
                        gestureStatusLeft += "Left Hand Gesture: " + gestureData.name;
                    }
                    else if (bodySideType == PXCMHandData.BodySideType.BODY_SIDE_RIGHT)
                    {
                        gestureStatusRight += "Right Hand Gesture: " + gestureData.name;
                    }

                }

            }
            if (gestureStatusLeft != String.Empty)
            {
                Console.WriteLine(gestureStatusLeft);//Console.WriteLine("Frame " + frameNumber + ") " + gestureStatusRight);
                cfg.m_comm.Send("{wave:left}");
            }
            if (gestureStatusRight != String.Empty)
            {
                Console.WriteLine(gestureStatusRight);//Console.WriteLine("Frame " + frameNumber + ") " + gestureStatusLeft + ", " + gestureStatusRight);
                cfg.m_comm.Send("{wave:right}");
            }
        }//gesture

        /* Displaying current frame alerts */
        private void DisplayAlerts(PXCMHandData handAnalysis, int frameNumber)
        {
            bool isChanged = false;
            string sAlert = "Alert: ";
            for (int i = 0; i < handAnalysis.QueryFiredAlertsNumber(); i++)
            {
                PXCMHandData.AlertData alertData;
                if (handAnalysis.QueryFiredAlertData(i, out alertData) != pxcmStatus.PXCM_STATUS_NO_ERROR)
                    continue;

                //See PXCMHandAnalysis.AlertData.AlertType for all available alerts
                switch (alertData.label)
                {
                    case PXCMHandData.AlertType.ALERT_HAND_DETECTED:
                        {

                            sAlert += "Hand Detected, ";
                            isChanged = true;
                            break;
                        }
                    case PXCMHandData.AlertType.ALERT_HAND_NOT_DETECTED:
                        {

                            sAlert += "Hand Not Detected, ";
                            isChanged = true;
                            break;
                        }
                    case PXCMHandData.AlertType.ALERT_HAND_CALIBRATED:
                        {

                            sAlert += "Hand Calibrated, ";
                            isChanged = true;
                            break;
                        }
                    case PXCMHandData.AlertType.ALERT_HAND_NOT_CALIBRATED:
                        {

                            sAlert += "Hand Not Calibrated, ";
                            isChanged = true;
                            break;
                        }
                    case PXCMHandData.AlertType.ALERT_HAND_INSIDE_BORDERS:
                        {

                            sAlert += "Hand Inside Border, ";
                            isChanged = true;
                            break;
                        }
                    case PXCMHandData.AlertType.ALERT_HAND_OUT_OF_BORDERS:
                        {

                            sAlert += "Hand Out Of Borders, ";
                            isChanged = true;
                            break;
                        }

                }
            }
            if (isChanged)
            {
                Console.WriteLine("Frame " + frameNumber + ") " + sAlert );
            }
        }


    }
}
