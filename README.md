# SpoutToOpenCvPointCreate

Spoutで受信した映像をUnity上でRenderTextureとして取り込み、OpenCVでレーザープロジェクター用の点群データへ変換してUDP送信するアプリです。

## 概要

- Spout入力の映像をUnityで受信
- RenderTextureをTexture2Dへ変換
- OpenCV処理で輪郭/ポイントデータを生成
- UDPで外部デバイスへ座標データを送信

レーザープロジェクター制御プロジェクトの送信側アプリとして作成しています。

## 主なファイル

- `Assets/Scripts/RenderTextureToUDP.cs` - RenderTextureの取得、OpenCV変換、UDP送信処理
- `Assets/Scripts/UdpSender.cs` - UDP送信用の補助クラス
- `SpoutToOpenCvPointCreate.sln` - Unity/C#プロジェクト

## 使用技術

- Unity
- C#
- OpenCV+Unity / OpenCvSharp
- Spout
- UDP通信

## 関連プロジェクト

受信側/ハードウェア制御側として、TeensyでUDPを受信してGalvoとLaserを制御するプロジェクトがあります。

- [UdpRecivePointTeensyOutput](https://github.com/nishi10000/UdpRecivePointTeensyOutput)

## Demo

![SpoutToOpenCvPointCreate](https://user-images.githubusercontent.com/34505055/178178209-6844a0d6-07e7-4894-a676-37b193d3b555.gif)
