using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace HR_RealSense_Srv1
{
    class Faces
    {
        Config cfg;
        PXCMSenseManager pp;
        PXCMCaptureManager captureMgr;
        PXCMFaceModule faceModule;
        PXCMFaceConfiguration moduleConfiguration;
        PXCMFaceData moduleOutput;

        public Faces(Config config)
        {
            cfg = config;
            pp = cfg.Session.CreateSenseManager();

            if (pp == null)
            {
                throw new Exception("PXCMSenseManager null");
            }

            // Set Resolution
            //var selectedRes = m_form.GetCheckedColorResolution();

            captureMgr = pp.captureManager;
            if (captureMgr == null)
            {
                throw new Exception("PXCMCaptureManager null");
            }
            //if (selectedRes != null)
            //{
            //    // activate filter only live/record mode , no need in playback mode
            //    var set = new PXCMCapture.Device.StreamProfileSet
            //    {
            //        color =
            //        {
            //            frameRate = selectedRes.Item2,
            //            imageInfo =
            //            {
            //                format = selectedRes.Item1.format,
            //                height = selectedRes.Item1.height,
            //                width = selectedRes.Item1.width
            //            }
            //        }
            //    };

            //    if (m_form.IsPulseEnabled() && (set.color.imageInfo.width < 1280 || set.color.imageInfo.height < 720))
            //    {
            //        captureMgr.FilterByStreamProfiles(PXCMCapture.StreamType.STREAM_TYPE_COLOR, 1280, 720, 0);
            //    }
            //    captureMgr.FilterByStreamProfiles(set);
            //}
            ////
            captureMgr.FilterByStreamProfiles(PXCMCapture.StreamType.STREAM_TYPE_COLOR, 1280, 720, 0);
            ////
            // Set Source & Landmark Profile Index 
            ////captureMgr.SetFileName(cfg.GetFileName(), true);//do not record .. mandeep

            // Set Module            
            pp.EnableFace();
            faceModule = pp.QueryFace();
            if (faceModule == null)
            {
                Debug.Assert(faceModule != null);
                return;
            }

            moduleConfiguration = faceModule.CreateActiveConfiguration();

            if (moduleConfiguration == null)
            {
                Debug.Assert(moduleConfiguration != null);
                return;
            }

            //PXCMFaceConfiguration.TrackingModeType mode = m_form.GetCheckedProfile().Contains("3D")
            //    ? PXCMFaceConfiguration.TrackingModeType.FACE_MODE_COLOR_PLUS_DEPTH
            //    : PXCMFaceConfiguration.TrackingModeType.FACE_MODE_COLOR;
            PXCMFaceConfiguration.TrackingModeType mode = PXCMFaceConfiguration.TrackingModeType.FACE_MODE_COLOR_PLUS_DEPTH;//may need to change this?
            ////PXCMFaceConfiguration.TrackingModeType mode = PXCMFaceConfiguration.TrackingModeType.FACE_MODE_COLOR;

            moduleConfiguration.SetTrackingMode(mode);

            moduleConfiguration.strategy = PXCMFaceConfiguration.TrackingStrategyType.STRATEGY_RIGHT_TO_LEFT;

            moduleConfiguration.detection.maxTrackedFaces = cfg.MaxFaces;//verify
            moduleConfiguration.landmarks.maxTrackedFaces = cfg.MaxFaces;//verify
            moduleConfiguration.pose.maxTrackedFaces = cfg.MaxFaces;//verify

            PXCMFaceConfiguration.ExpressionsConfiguration econfiguration = moduleConfiguration.QueryExpressions();
            if (econfiguration == null)
            {
                throw new Exception("ExpressionsConfiguration null");
            }
            econfiguration.properties.maxTrackedFaces = 4;// m_form.NumExpressions;

            econfiguration.EnableAllExpressions();
            moduleConfiguration.detection.isEnabled = true;
            moduleConfiguration.landmarks.isEnabled = false;
            moduleConfiguration.pose.isEnabled = true;
            if (cfg.IsExpressionsEnabled())
            {
                econfiguration.Enable();
            }

            PXCMFaceConfiguration.PulseConfiguration pulseConfiguration = moduleConfiguration.QueryPulse();
            if (pulseConfiguration == null)
            {
                throw new Exception("pulseConfiguration null");
            }

            pulseConfiguration.properties.maxTrackedFaces = cfg.MaxFaces;
            if (cfg.IsPulseEnabled())
            {
                pulseConfiguration.Enable();
            }

            PXCMFaceConfiguration.RecognitionConfiguration qrecognition = moduleConfiguration.QueryRecognition();
            if (qrecognition == null)
            {
                throw new Exception("PXCMFaceConfiguration.RecognitionConfiguration null");
            }
            if (cfg.IsRecognitionChecked())
            {
                qrecognition.Enable();
            }

            moduleConfiguration.EnableAllAlerts();
            moduleConfiguration.SubscribeAlert(FaceAlertHandler);

            pxcmStatus applyChangesStatus = moduleConfiguration.ApplyChanges();

            Console.WriteLine("Init Started");

            if (applyChangesStatus < pxcmStatus.PXCM_STATUS_NO_ERROR || pp.Init() < pxcmStatus.PXCM_STATUS_NO_ERROR)
            {
                Console.WriteLine("Init Failed");
            }
            else
            {
                //using (moduleOutput = faceModule.CreateOutput())
                //{
                //    Debug.Assert(moduleOutput != null);
                //    PXCMCapture.Device.StreamProfileSet profiles;
                //    PXCMCapture.Device device = captureMgr.QueryDevice();

                //    if (device == null)
                //    {
                //        throw new Exception("device null");
                //    }

                //    device.QueryStreamProfileSet(PXCMCapture.StreamType.STREAM_TYPE_DEPTH, 0, out profiles);
                //    //CheckForDepthStream(profiles, faceModule);

                //    Console.WriteLine("Streaming");

                //}
            }
        }//faces constructor

        private void FaceAlertHandler(PXCMFaceData.AlertData alert)
        {
            Console.WriteLine(alert.label.ToString());
        }

        public void callOnceInLoop()
        {
            //using (moduleOutput)
            using (moduleOutput = faceModule.CreateOutput())
            {
                Debug.Assert(moduleOutput != null);
                PXCMCapture.Device.StreamProfileSet profiles;
                PXCMCapture.Device device = captureMgr.QueryDevice();

                if (device == null)
                {
                    throw new Exception("device null");
                }

                device.QueryStreamProfileSet(PXCMCapture.StreamType.STREAM_TYPE_DEPTH, 0, out profiles);
                //CheckForDepthStream(profiles, faceModule);

                Console.WriteLine("Streaming");
                while (true)
                {
                    if (pp.AcquireFrame(true) < pxcmStatus.PXCM_STATUS_NO_ERROR) return;
                    var isConnected = pp.IsConnected();
                    //DisplayDeviceConnection(isConnected);
                    if (isConnected)
                    {
                        var sample = pp.QueryFaceSample();
                        if (sample == null)
                        {
                            pp.ReleaseFrame();
                            return;
                        }
                        DisplayPicture(sample.color);

                        moduleOutput.Update();
                        PXCMFaceConfiguration.RecognitionConfiguration recognition = moduleConfiguration.QueryRecognition();
                        if (recognition == null)
                        {
                            pp.ReleaseFrame();
                            return;
                        }

                        if (recognition.properties.isEnabled)
                        {
                            UpdateRecognition(moduleOutput);
                        }

                        cfg.publishFaceData(moduleOutput);
                        //m_form.UpdatePanel();
                    }
                    pp.ReleaseFrame();
                }
            }
        }//call once in loop
        private void DisplayPicture(PXCMImage image)
        {
            PXCMImage.ImageData data;
            if (image.AcquireAccess(PXCMImage.Access.ACCESS_READ, PXCMImage.PixelFormat.PIXEL_FORMAT_RGB32, out data) <
                pxcmStatus.PXCM_STATUS_NO_ERROR) return;
            cfg.setImageSize(image.info.width, image.info.height);
            image.ReleaseAccess(data);
        }

        private void UpdateRecognition(PXCMFaceData faceOutput)
        {
            //TODO: add null checks
            if (cfg.Register) RegisterUser(faceOutput);
            if (cfg.Unregister) UnregisterUser(faceOutput);
        }
        private void RegisterUser(PXCMFaceData faceOutput)
        {
            cfg.Register = false;
            if (faceOutput.QueryNumberOfDetectedFaces() <= 0)
                return;

            PXCMFaceData.Face qface = faceOutput.QueryFaceByIndex(0);
            if (qface == null)
            {
                throw new Exception("PXCMFaceData.Face null");
            }
            PXCMFaceData.RecognitionData rdata = qface.QueryRecognition();
            if (rdata == null)
            {
                throw new Exception(" PXCMFaceData.RecognitionData null");
            }
            rdata.RegisterUser();
        }

        private void UnregisterUser(PXCMFaceData faceOutput)
        {
            cfg.Unregister = false;
            if (faceOutput.QueryNumberOfDetectedFaces() <= 0)
            {
                return;
            }

            var qface = faceOutput.QueryFaceByIndex(0);
            if (qface == null)
            {
                throw new Exception("PXCMFaceData.Face null");
            }

            PXCMFaceData.RecognitionData rdata = qface.QueryRecognition();
            if (rdata == null)
            {
                throw new Exception(" PXCMFaceData.RecognitionData null");
            }

            if (!rdata.IsRegistered())
            {
                return;
            }
            rdata.UnregisterUser();
        }

    }
}
