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
    using System.Windows;

    public class RenderTextureToUDP : MonoBehaviour
    {
        [SerializeField] private RenderTexture ColorSourceRT; //Spoutで描画されるRenderTexture
        Texture2D ColorSourceTexture;  //ColorSourceRTをTexture2Dに変換したものを格納する変数。

        //[SerializeField]
        //UdpSender udpSender;  //非同期での送信では順番が確保されなかったため、描画の順番がずれてしまった。

        // broadcast address
        public string host = "127.0.0.1";
        public int port = 3333;
        private UdpClient client;

        //UdpSendWaitTime/ms
        private float timeOut = 0.003f;//0.003f;  //s  0.001 ms 0.000001 us

        private float timeElapsed;


        void Start()
        {
            client = new UdpClient();
            client.Connect(host, port);

            //udpSender.ConnectClient(host,port);
            //udpSender.Test_Send();

            

        }

        // Update is called once per frame
        void Update()
        {
            //フレームにかかる秒数を測定する。
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            ColorSourceTexture = GetRTPixels(ColorSourceRT);


            OpenCVTexture(ColorSourceTexture);

            //フレームにかかる秒数を測定する
            sw.Stop();
            //Debug.Log(sw.Elapsed.TotalMilliseconds + "ms");  //60Frameなら16ms 30Frameなら32ms以下で処理が終わらないといけない。
        }

        void OnApplicationQuit()
        {
            //udpSender.CloseClient();
            client.Close();
        }


        
        //UDP送信用コルーチン
        IEnumerator udpSendCoroutine(Point[][] contours)
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
                    double distance = decrementPoints(contours[i][j]);
                    if (distance <= 1)  //ポイントとポイントの距離が短い場合は処理を行わない。
                    {
                        //Debug.Log("DistanceShort");
                    }
                    else
                    {
                        dgram[0] = (byte)contours[i][j].X;
                        dgram[1] = (byte)contours[i][j].Y;
                        dgram[2] = brightness;
                        try
                        {

                            client.Send(dgram, dgram.Length);
                            
                        }
                        catch { }
                        if (brightness != 255)
                        {
                            brightness = 255;  //移動後は点灯する。
                        }
                        yield return new WaitForSeconds(timeOut);

                    }


                }
            }
            
        }

        //Contoursの要素数を確認する。
        int countContoursNum(Point[][] contours)
        {
            int contoursNum= 0;  //要素数をカウントする。
            for (int i = 0; i < contours.GetLength(0); i++)
            {
                contoursNum = contoursNum + contours[i].Length;
                for (int j=0;j<contours[i].Length;j++) {
                }
            }
            return contoursNum;  //グループの中の要素数
        }

        private Point oldPoint;  //ひとつ前のpointを保存する。
        double decrementPoints(Point nowPoint)  //Point数を減らす。差が1しかない場合は消す。
        {
            double distance = Point.Distance(nowPoint, oldPoint);
            oldPoint = nowPoint;
            //Debug.Log(distance);
            return distance;
        }

        //ポイント数算出し変化があるまでポイントを送信する。
        void PointChangeNumSendUDP(Point[][] contours)
        {
            int contoursNum;
            contoursNum = countContoursNum(contours);

            if (oldContoursNums == contoursNum)
            {
                UDPSendContours(contours);
            }
            else
            {
                oldContoursNums = contoursNum;
            }
        }

        //TODO:送信する速度が速すぎるかもしれない。その場合はスレッドか？
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
                    timeElapsed += Time.deltaTime;

                    if (timeElapsed >= timeOut)
                    {
                        // Do anything
                        double distance = decrementPoints(contours[i][j]);
                        if (distance <= 1)  //ポイントとポイントの距離が短い場合は処理を行わない。
                        {
                            //Debug.Log("DistanceShort");
                        }
                        else
                        {
                            dgram[0] = (byte)contours[i][j].X;
                            dgram[1] = (byte)contours[i][j].Y;
                            dgram[2] = brightness;
                            try
                            {
                                //udpSender.SendData(dgram);
                                client.Send(dgram, dgram.Length);
                            }
                            catch { }
                            if (brightness != 255)
                            {
                                brightness = 255;  //移動後は点灯する。
                            }

                        }

                        //Debug.Log("time:"+timeElapsed);
                        //Debug.Log("timeout:" + timeOut);

                        timeElapsed = 0.0f;

                    }
                    
                    
                }
            }
        }

        private int oldContoursNums;
        private Texture2D oldChangedTex;


        //OpenCVを使用して、座標を求める。
        void OpenCVTexture(Texture2D texture)
        {


            Mat newMat = Unity.TextureToMat(texture);

            //画像をCv2.Equalsで変化があるかグローバルのoldMatと比較して検知しようとしたが、できなかった。

            //Convert image to grayscale
            Mat imgGray = new Mat();
            Cv2.CvtColor(newMat, imgGray, ColorConversionCodes.BGR2GRAY);

            //Debug.Log(Cv2.Equals(imgGray, imgGray));


            // Clean up image using Gaussian Blur
            Mat imgGrayBlur = new Mat();
            Cv2.GaussianBlur(imgGray, imgGrayBlur, new Size(5, 5), 0);

            //Extract edges
            Mat cannyEdges = new Mat();
            Cv2.Canny(imgGrayBlur, cannyEdges, 10.0, 70.0);
           
            //Do an invert binarize the image
            Mat mask = new Mat();
            Cv2.Threshold(cannyEdges, mask, 70.0, 255.0, ThresholdTypes.BinaryInv);

            // Extract Contours
            Point[][] contours;  //特徴点が格納される変数。
            HierarchyIndex[] hierarchy;  //特徴点の階層が格納される。
            Cv2.FindContours(cannyEdges, out contours, out hierarchy, RetrievalModes.Tree, ContourApproximationModes.ApproxSimple, null);  //特徴点を検出する。


            PointChangeNumSendUDP(contours);  //Pointが変化しなければ送信を実装しようとしている途中。
            //StartCoroutine(udpSendCoroutine(contours));


            //輪郭描画
            int width = (int)transform.GetComponent<RectTransform>().sizeDelta.x;
            int height = (int)transform.GetComponent<RectTransform>().sizeDelta.y;
            Mat Contours = new Mat(width, height, MatType.CV_8UC3, new Scalar(0, 0, 0));  //初期値として黒い画面を作成する。
            Cv2.DrawContours(Contours, contours, -1, new Scalar(0, 255, 0, 255), 1);  //MatにCountours(特徴点)を描画する。
            Texture2D changedTex = Unity.MatToTexture(Contours);  //MatをTexture2Dへ変更
            GetComponent<RawImage>().texture = changedTex;  //RaxImageにTexture2Dを書き込み。

            //MatをDisposeする。
            newMat.Dispose();
            imgGray.Dispose();
            imgGrayBlur.Dispose();
            cannyEdges.Dispose();
            mask.Dispose();
            Contours.Dispose();

            //TextureをDestryしないとメモリーリークを送りました。
            MonoBehaviour.Destroy(texture);
            if (changedTex != oldChangedTex)
            {
                MonoBehaviour.Destroy(oldChangedTex);
                oldChangedTex = changedTex;
            }


        }



        private void OnDestroy()
        {
            Resources.UnloadUnusedAssets();
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
