namespace OpenCvSharp
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using OpenCvSharp;
    using UnityEngine.UI;
    using System.Net.Sockets;
    using System.Text;
    using System;

    public class RenderTextureToUDP : MonoBehaviour
    {
        [SerializeField] private RenderTexture ColorSourceRT; //Spoutで描画されるRenderTexture
        Texture2D ColorSourceTexture;  //ColorSourceRTをTexture2Dに変換したものを格納する変数。

        // broadcast address
        public string host = "127.0.0.1";
        public int port = 3333;
        private UdpClient client;

        void Start()
        {
            client = new UdpClient();
            client.Connect(host, port);

        }

        // Update is called once per frame
        void Update()
        {
            ColorSourceTexture = GetRTPixels(ColorSourceRT);
            OpenCVTexture(ColorSourceTexture);
        }

        void OnApplicationQuit()
        {
            client.Close();
        }

        //Contoursの要素数を確認する。
        void countContoursNum(Point[][] contours)
        {
            int contoursNum= 0;  //要素数をカウントする。
            for (int i = 0; i < contours.GetLength(0); i++)
            {
                contoursNum = contoursNum + contours[i].Length;
                for (int j=0;j<contours[i].Length;j++) {
                }
            }
            Debug.Log(contoursNum);  //グループの中の要素数
        }

        //TODO:UDPを実装する。
        //UDPでContoursを送付する。
        void UDPSendContours(Point[][] contours)
        {
            byte[] dgram = new byte[3];  //[0]X座標,[1]Y座標,[2]明るさ
            int contoursNum = 0;  //要素数をカウントする。
            byte brightness;  //明るさを保持する。
            for (int i = 0; i < contours.GetLength(0); i++)  //グループの数
            {
                brightness = 0;  //グループが始まったときは移動のみ。
                contoursNum = contoursNum + contours[i].Length;
                for (int j = 0; j < contours[i].Length; j++)
                {
                    dgram[0] = (byte)contours[i][j].X;
                    dgram[1] = (byte)contours[i][j].Y;
                    dgram[2] = brightness;
                    client.Send(dgram, dgram.Length);
                    if (brightness != 255)
                    {
                        brightness = 255;  //移動後は点灯する。
                    }
                }
            }
        }

        //OpenCVを使用して、座標を求める。
        void OpenCVTexture(Texture2D texture)
        {
            Mat mat = Unity.TextureToMat(texture);
            Mat changedMat = new Mat();

            //Convert image to grayscale
            Mat imgGray = new Mat();
            Cv2.CvtColor(mat, imgGray, ColorConversionCodes.BGR2GRAY);

            // Clean up image using Gaussian Blur
            Mat imgGrayBlur = new Mat();
            Cv2.GaussianBlur(imgGray, imgGrayBlur, new Size(5, 5), 0);

            //Extract edges
            Mat cannyEdges = new Mat();
            Cv2.Canny(imgGrayBlur, cannyEdges, 10.0, 70.0);

            //Do an invert binarize the image
            Mat mask = new Mat();
            Cv2.Threshold(cannyEdges, mask, 70.0, 255.0, ThresholdTypes.BinaryInv);

            //表示用texture
            //Texture2D changedTex = Unity.MatToTexture(mask);
            //GetComponent<RawImage>().texture = changedTex;

            // Extract Contours
            Point[][] contours;  //特徴点が格納される変数。
            HierarchyIndex[] hierarchy;  //特徴点の階層が格納される。
            Cv2.FindContours(cannyEdges, out contours, out hierarchy, RetrievalModes.Tree, ContourApproximationModes.ApproxSimple, null);  //特徴点を検出する。
            #if UNITY_EDITOR
                countContoursNum(contours);
            #endif
            UDPSendContours(contours);



            //輪郭描画
            int width = (int)transform.GetComponent<RectTransform>().sizeDelta.x;
            int height = (int)transform.GetComponent<RectTransform>().sizeDelta.y;
            Mat Contours = new Mat(width, height, MatType.CV_8UC3, new Scalar(0, 0, 0));  //初期値として黒い画面を作成する。
            Cv2.DrawContours(Contours, contours, -1, new Scalar(0, 255, 0, 255), 1);  //MatにCountours(特徴点)を描画する。
            Texture2D changedTex = Unity.MatToTexture(Contours);  //MatをTexture2Dへ変更
            GetComponent<RawImage>().texture = changedTex;  //RaxImageにTexture2Dを書き込み。
        }


        //RenderTextureをTexture2Dに変換する関数。
        static public Texture2D GetRTPixels(RenderTexture rt)
        {
            // Remember currently active render texture
            RenderTexture currentActiveRT = RenderTexture.active;

            // Set the supplied RenderTexture as the active one
            RenderTexture.active = rt;

            // Create a new Texture2D and read the RenderTexture image into it
            Texture2D tex = new Texture2D(rt.width, rt.height);
            tex.ReadPixels(new UnityEngine.Rect(0, 0, tex.width, tex.height), 0, 0);

            // Restorie previously active render texture
            RenderTexture.active = currentActiveRT;
            return tex;
        }
    }
}
