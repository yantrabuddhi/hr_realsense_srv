using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
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

        private Tuple<PXCMImage.ImageInfo, PXCMRangeF32> m_selectedColorResolution;
        public Dictionary<string, IEnumerable<Tuple<PXCMImage.ImageInfo, PXCMRangeF32>>> ColorResolutions { get; set; }
        PXCMRangeF32 def_frame_rate;

        private readonly List<Tuple<int, int>> SupportedColorResolutions = new List<Tuple<int, int>>
        {
            Tuple.Create(1920, 1080),
            Tuple.Create(1280, 720),
            Tuple.Create(960, 540),
            Tuple.Create(640, 480),
            Tuple.Create(640, 360),
        };

        Dictionary<string, PXCMCapture.DeviceInfo> Devices;
        PXCMCapture.DeviceInfo infoD;

        private void PopulateDevice()
        {
            Devices = new Dictionary<string, PXCMCapture.DeviceInfo>();

            PXCMSession.ImplDesc desc = new PXCMSession.ImplDesc();
            desc.group = PXCMSession.ImplGroup.IMPL_GROUP_SENSOR;
            desc.subgroup = PXCMSession.ImplSubgroup.IMPL_SUBGROUP_VIDEO_CAPTURE;
            for (int i = 0; ; i++)
            {
                PXCMSession.ImplDesc desc1;
                if (cfg.Session.QueryImpl(desc, i, out desc1) < pxcmStatus.PXCM_STATUS_NO_ERROR) break;
                PXCMCapture capture;
                if (cfg.Session.CreateImpl<PXCMCapture>(desc1, out capture) < pxcmStatus.PXCM_STATUS_NO_ERROR) continue;
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
                    if (dinfo.model == PXCMCapture.DeviceModel.DEVICE_MODEL_SR300) infoD = dinfo;
                    PopulateColorResolutionMenu(name);
                }
                capture.Dispose();
            }
        }

        private static bool IsProfileSupported(PXCMCapture.Device.StreamProfileSet profileSet, PXCMCapture.DeviceInfo dinfo)
        {
            return
                (profileSet.color.frameRate.min < 30) ||
                (dinfo != null && dinfo.model == PXCMCapture.DeviceModel.DEVICE_MODEL_DS4 &&
                (profileSet.color.imageInfo.width == 1920 || profileSet.color.frameRate.min > 30 || profileSet.color.imageInfo.format == PXCMImage.PixelFormat.PIXEL_FORMAT_YUY2)) ||
                (profileSet.color.options == PXCMCapture.Device.StreamOption.STREAM_OPTION_UNRECTIFIED);
        }

        private void CreateResolutionMap()
        {
            ColorResolutions = new Dictionary<string, IEnumerable<Tuple<PXCMImage.ImageInfo, PXCMRangeF32>>>();
            var desc = new PXCMSession.ImplDesc
            {
                group = PXCMSession.ImplGroup.IMPL_GROUP_SENSOR,
                subgroup = PXCMSession.ImplSubgroup.IMPL_SUBGROUP_VIDEO_CAPTURE
            };

            for (int i = 0; ; i++)
            {
                PXCMSession.ImplDesc desc1;
                if (cfg.Session.QueryImpl(desc, i, out desc1) < pxcmStatus.PXCM_STATUS_NO_ERROR) break;

                PXCMCapture capture;
                if (cfg.Session.CreateImpl(desc1, out capture) < pxcmStatus.PXCM_STATUS_NO_ERROR) continue;

                for (int j = 0; ; j++)
                {
                    PXCMCapture.DeviceInfo info;
                    if (capture.QueryDeviceInfo(j, out info) < pxcmStatus.PXCM_STATUS_NO_ERROR) break;

                    PXCMCapture.Device device = capture.CreateDevice(j);
                    if (device == null)
                    {
                        throw new Exception("PXCMCapture.Device null");
                    }
                    var deviceResolutions = new List<Tuple<PXCMImage.ImageInfo, PXCMRangeF32>>();

                    for (int k = 0; k < device.QueryStreamProfileSetNum(PXCMCapture.StreamType.STREAM_TYPE_COLOR); k++)
                    {
                        PXCMCapture.Device.StreamProfileSet profileSet;
                        device.QueryStreamProfileSet(PXCMCapture.StreamType.STREAM_TYPE_COLOR, k, out profileSet);
                        PXCMCapture.DeviceInfo dinfo;
                        device.QueryDeviceInfo(out dinfo);

                        var currentRes = new Tuple<PXCMImage.ImageInfo, PXCMRangeF32>(profileSet.color.imageInfo, profileSet.color.frameRate);

                        if (IsProfileSupported(profileSet, dinfo))
                            continue;

                        if (SupportedColorResolutions.Contains(new Tuple<int, int>(currentRes.Item1.width, currentRes.Item1.height)))
                            deviceResolutions.Add(currentRes);

                    }

                    try
                    {
                        ColorResolutions.Add(info.name, deviceResolutions);
                    }
                    catch (Exception e)
                    {
                    }
                    device.Dispose();
                }

                capture.Dispose();
            }
        }

        public void PopulateColorResolutionMenu(string deviceName)
        {
            bool foundDefaultResolution = false;
            //var sm = new ToolStripMenuItem("Color");
            foreach (var resolution in ColorResolutions[deviceName])
            {
                //var resText = PixelFormat2String(resolution.Item1.format) + " " + resolution.Item1.width + "x"
                //              + resolution.Item1.height + " " + resolution.Item2.max + " fps";
                var selectedResolution = resolution;
                m_selectedColorResolution = selectedResolution;
                //sm1.Click += (sender, eventArgs) =>
                //{
                //    m_selectedColorResolution = selectedResolution;
                //    ColorResolution_Item_Click(sender);
                //};

                //sm.DropDownItems.Add(sm1);

                int width = selectedResolution.Item1.width;
                int height = selectedResolution.Item1.height;
                PXCMImage.PixelFormat format = selectedResolution.Item1.format;
                float fps = selectedResolution.Item2.min;

                if (DefaultCameraConfig.IsDefaultDeviceConfig(deviceName, width, height, format, fps))
                {
                    foundDefaultResolution = true;
                    //sm1.Checked = true;
                    //sm1.PerformClick();
                    def_frame_rate = resolution.Item2;
                }
            }

        }

        public class DefaultCameraConfig
        {
            public static bool IsDefaultDeviceConfig(string deviceName, int width, int height, PXCMImage.PixelFormat format, float fps)
            {
                if (deviceName.Contains("R200"))
                {
                    return width == DefaultDs4Width && height == DefaultDs4Height && format == DefaultDs4PixelFormat && fps == DefaultDs4Fps;
                }

                if (deviceName.Contains("F200") || deviceName.Contains("SR300"))
                {
                    return width == DefaultIvcamWidth && height == DefaultIvcamHeight && format == DefaultIvcamPixelFormat && fps == DefaultIvcamFps;
                }

                return false;
            }

            private static readonly int DefaultDs4Width = 640;
            private static readonly int DefaultDs4Height = 480;
            private static readonly PXCMImage.PixelFormat DefaultDs4PixelFormat = PXCMImage.PixelFormat.PIXEL_FORMAT_RGB32;
            private static readonly float DefaultDs4Fps = 30f;

            public static readonly int DefaultIvcamWidth = 640;
            public static readonly int DefaultIvcamHeight = 360;
            public static readonly PXCMImage.PixelFormat DefaultIvcamPixelFormat = PXCMImage.PixelFormat.PIXEL_FORMAT_YUY2;
            public static readonly float DefaultIvcamFps = 30f;
        }


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

            CreateResolutionMap();
            PopulateDevice();
            Console.WriteLine(def_frame_rate.min);
            //if (selectedRes != null)
            //{
            //    // activate filter only live/record mode , no need in playback mode
                var set = new PXCMCapture.Device.StreamProfileSet
                {
                    color =
                    {
                        frameRate = def_frame_rate,
                        imageInfo =
                        {
                            format = DefaultCameraConfig.DefaultIvcamPixelFormat,
                            height = DefaultCameraConfig.DefaultIvcamHeight,
                            width = DefaultCameraConfig.DefaultIvcamWidth
                        }
                    }
                };

            //    if (m_form.IsPulseEnabled() && (set.color.imageInfo.width < 1280 || set.color.imageInfo.height < 720))
            //    {
            //        captureMgr.FilterByStreamProfiles(PXCMCapture.StreamType.STREAM_TYPE_COLOR, 1280, 720, 0);
            //    }
                captureMgr.FilterByStreamProfiles(set);
            //}
            ////
            ////captureMgr.FilterByStreamProfiles(PXCMCapture.StreamType.STREAM_TYPE_COLOR, 1280, 720, 0);
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
        private void CheckForDepthStream(PXCMCapture.Device.StreamProfileSet profiles, PXCMFaceModule faceModule)
        {
            PXCMFaceConfiguration faceConfiguration = faceModule.CreateActiveConfiguration();
            if (faceConfiguration == null)
            {
                Debug.Assert(faceConfiguration != null);
                return;
            }

            PXCMFaceConfiguration.TrackingModeType trackingMode = faceConfiguration.GetTrackingMode();
            faceConfiguration.Dispose();

            if (trackingMode != PXCMFaceConfiguration.TrackingModeType.FACE_MODE_COLOR_PLUS_DEPTH) return;
            if (profiles.depth.imageInfo.format == PXCMImage.PixelFormat.PIXEL_FORMAT_DEPTH) return;
            Console.WriteLine("Problem .. device may not be correctly selected..");
            /*
            PXCMCapture.DeviceInfo dinfo;
            m_form.Devices.TryGetValue(m_form.GetCheckedDevice(), out dinfo);

            if (dinfo != null)
                MessageBox.Show(
                    String.Format("Depth stream is not supported for device: {0}. \nUsing 2D tracking", dinfo.name),
                    @"Face Tracking",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                    */
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
                CheckForDepthStream(profiles, faceModule);//was missing

                Console.WriteLine("Streaming");
                pTimer tmr = new pTimer();
                int cnt = 0;
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

                        if (tmr.Tick()) cnt++;
                        if (cnt > 2)
                        {
                            cnt = 0;
                            cfg.Register = true;
                        }
//                        if (moduleOutput.QueryNumberOfDetectedFaces() > 0)
//                        {
//                            if (recognition.properties.isEnabled)
//                            {

//                               registerAll(moduleOutput);
                                //cfg.Register = true;
                                //UpdateRecognition(moduleOutput);
//                            }
                            cfg.publishFaceData(moduleOutput);//call register here
//                        }
                        //if (!cfg.publishFaceData(moduleOutput)) Console.WriteLine("did not publish");
                        //m_form.UpdatePanel();
                        Thread.Sleep(10);
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
        private void registerAll(PXCMFaceData faceOutput)
        {
            //cfg.Register = false;
            int i = faceOutput.QueryNumberOfDetectedFaces();
            if (i <= 0)
                return;
            for (int j = 0; j < i; j++)
            {
                PXCMFaceData.Face qface = faceOutput.QueryFaceByIndex(j);
                if (qface == null)
                {
                    throw new Exception("PXCMFaceData.Face null");
                }
                if (getRecognition(qface)) continue;
                PXCMFaceData.RecognitionData rdata = qface.QueryRecognition();
                if (rdata == null)
                {
                    throw new Exception(" PXCMFaceData.RecognitionData null");
                }
                rdata.RegisterUser();
                Console.WriteLine("Registered");
            }
        }
        public bool getRecognition(PXCMFaceData.Face face)
        {
            Debug.Assert(face != null);

            PXCMFaceData.RecognitionData qrecognition = face.QueryRecognition();
            if (qrecognition == null)
            {
                throw new Exception(" PXCMFaceData.RecognitionData null");
            }
            var userId = qrecognition.QueryUserID();
            //var recognitionText = userId == -1 ? "Not Registered" : String.Format("Registered ID: {0}", userId);
            return (!(userId == -1));

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
            Console.WriteLine("Registered...");
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
