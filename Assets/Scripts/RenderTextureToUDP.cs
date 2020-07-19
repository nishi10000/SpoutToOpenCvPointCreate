namespace OpenCvSharp
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using OpenCvSharp;
    using UnityEngine.UI;

    public class RenderTextureToUDP : MonoBehaviour
    {
        [SerializeField] private RenderTexture ColorSourceRT; //Spoutで描画されるRenderTexture
        Texture2D ColorSourceTexture;  //ColorSourceRTをTexture2Dに変換したものを格納する変数。

        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            ColorSourceTexture = GetRTPixels(ColorSourceRT);
            OpenCVTexture(ColorSourceTexture);
            

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
            Point[][] contours;
            HierarchyIndex[] hierarchy;
            Cv2.FindContours(cannyEdges, out contours, out hierarchy, RetrievalModes.Tree, ContourApproximationModes.ApproxNone, null);

            //輪郭描画
            int  width = (int)transform.GetComponent<RectTransform>().sizeDelta.x;
            int height = (int)transform.GetComponent<RectTransform>().sizeDelta.y;
            Mat Contours = new Mat(width, height, MatType.CV_8UC3, new Scalar(0, 0, 0));  //初期値として黒い画面を作成する。
            Cv2.DrawContours(Contours, contours, -1, new Scalar(0, 255, 0, 255), 1);
            Texture2D changedTex = Unity.MatToTexture(Contours);
            GetComponent<RawImage>().texture = changedTex;
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
