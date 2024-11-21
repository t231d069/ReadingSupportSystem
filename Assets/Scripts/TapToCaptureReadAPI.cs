using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;
using Microsoft.MixedReality.Toolkit.Utilities;
using System.Threading.Tasks;

// SpeechSDK ここから
using System.IO;
using System.Text;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
// SpeechSDK ここまで

public class TapToCaptureReadAPI : MonoBehaviour
{
    // Read ここから
    private string read_endpoint = "https://<Insert Your Endpoint>/vision/v3.1/read/analyze";
    private string read_subscription_key = "<Insert Your API Key>";

    // {"status":"succeeded","createdDateTime":"2020-12-13T03:05:57Z","lastUpdatedDateTime":"2020-12-13T03:05:58Z","analyzeResult":{"version":"3.0.0","readResults":[{"page":1,"angle":-1.4203,"width":1280,"height":720,"unit":"pixel","lines":[{"boundingBox":[417,351,481,348,482,365,417,367],"text":"BANANA","words":[{"boundingBox":[420,352,481,348,482,365,421,367],"text":"BANANA","confidence":0.595}]},{"boundingBox":[541,348,604,347,605,360,542,362],"text":"ALMOND","words":[{"boundingBox":[542,349,605,347,605,360,543,363],"text":"ALMOND","confidence":0.916}]},{"boundingBox":[644,345,724,344,724,358,645,359],"text":"STRAWBERRY","words":[{"boundingBox":[647,346,724,344,724,358,647,359],"text":"STRAWBERRY","confidence":0.638}]},{"boundingBox":[757,334,843,337,843,356,757,353],"text":"PINEAPPLE","words":[{"boundingBox":[763,335,842,338,843,355,763,353],"text":"PINEAPPLE","confidence":0.623}]},{"boundingBox":[385,382,748,374,750,465,387,475],"text":"LOOK","words":[{"boundingBox":[385,384,746,374,746,467,387,475],"text":"LOOK","confidence":0.983}]},{"boundingBox":[479,474,668,468,670,501,480,508],"text":"A La Mode","words":[{"boundingBox":[482,475,512,474,513,508,483,509],"text":"A","confidence":0.987},{"boundingBox":[519,474,567,472,567,506,520,508],"text":"La","confidence":0.987},{"boundingBox":[573,472,669,468,670,501,574,505],"text":"Mode","confidence":0.984}]},{"boundingBox":[489,508,666,501,666,518,490,526],"text":"CHOCOLATE","words":[{"boundingBox":[489,509,666,501,666,519,490,527],"text":"CHOCOLATE","confidence":0.981}]}]}]}}
    [System.Serializable]
    public class Read
    {
        public string status;
        public string createdDateTime;
        public string lastUpdatedDateTime;
        public AnalyzeResult analyzeResult;
    }

    [System.Serializable]
    public class AnalyzeResult
    {
        public ReadResults[] readResults;
    }

    [System.Serializable]
    public class ReadResults
    {
        public Lines[] lines;
    }

    [System.Serializable]
    public class Lines
    {
        public string text;
    }
    // Read ここまで

    // SpeechSDK ここから
    public AudioSource audioSource;

    public async Task SynthesizeAudioAsync(string text)
    {
        var config = SpeechConfig.FromSubscription("YourSubscriptionKey", "YourServiceRegion");
        var synthesizer = new SpeechSynthesizer(config, null); // nullを省略するとPCのスピーカーから出力されるが、HoloLensでは出力されない。

        string ssml = "<speak version=\"1.0\" xmlns=\"https://www.w3.org/2001/10/synthesis\" xml:lang=\"ja-JP\"> <voice name=\"ja-JP-Ichiro\">" + text + "</voice> </speak>";

        // Starts speech synthesis, and returns after a single utterance is synthesized.
        // using (var result = synthesizer.SpeakTextAsync(text).Result)
        using (var result = synthesizer.SpeakSsmlAsync(ssml).Result)
        {
            // Checks result.
            if (result.Reason == ResultReason.SynthesizingAudioCompleted)
            {
                // Native playback is not supported on Unity yet (currently only supported on Windows/Linux Desktop).
                // Use the Unity API to play audio here as a short term solution.
                // Native playback support will be added in the future release.
                var sampleCount = result.AudioData.Length / 2;
                var audioData = new float[sampleCount];
                for (var i = 0; i < sampleCount; ++i)
                {
                    audioData[i] = (short)(result.AudioData[i * 2 + 1] << 8 | result.AudioData[i * 2]) / 32768.0F;
                }

                // The output audio format is 16K 16bit mono
                var audioClip = AudioClip.Create("SynthesizedAudio", sampleCount, 1, 16000, false);
                audioClip.SetData(audioData, 0);
                audioSource.clip = audioClip;
                audioSource.Play();

                // newMessage = "Speech synthesis succeeded!";
            }
            else if (result.Reason == ResultReason.Canceled)
            {
                var cancellation = SpeechSynthesisCancellationDetails.FromResult(result);
                // newMessage = $"CANCELED:\nReason=[{cancellation.Reason}]\nErrorDetails=[{cancellation.ErrorDetails}]\nDid you update the subscription info?";
            }
        }
    }
    // SpeechSDK ここまで

    public GameObject quad;
    UnityEngine.Windows.WebCam.PhotoCapture photoCaptureObject = null;
    Texture2D targetTexture = null;
    private bool waitingForCapture;

    void Start()
    {
        waitingForCapture = false;
    }

    public void AirTap()
    {
        if (waitingForCapture) return;
        waitingForCapture = true;

        Resolution cameraResolution = UnityEngine.Windows.WebCam.PhotoCapture.SupportedResolutions.OrderByDescending((res) => res.width * res.height).First();
        targetTexture = new Texture2D(cameraResolution.width, cameraResolution.height);

        // PhotoCapture オブジェクトを作成します
        UnityEngine.Windows.WebCam.PhotoCapture.CreateAsync(false, delegate (UnityEngine.Windows.WebCam.PhotoCapture captureObject) {
            photoCaptureObject = captureObject;
            UnityEngine.Windows.WebCam.CameraParameters cameraParameters = new UnityEngine.Windows.WebCam.CameraParameters();
            cameraParameters.hologramOpacity = 0.0f;
            cameraParameters.cameraResolutionWidth = cameraResolution.width;
            cameraParameters.cameraResolutionHeight = cameraResolution.height;
            cameraParameters.pixelFormat = UnityEngine.Windows.WebCam.CapturePixelFormat.BGRA32;

            // カメラをアクティベートします
            photoCaptureObject.StartPhotoModeAsync(cameraParameters, delegate (UnityEngine.Windows.WebCam.PhotoCapture.PhotoCaptureResult result) {
                // 写真を撮ります
                photoCaptureObject.TakePhotoAsync(OnCapturedPhotoToMemoryAsync);
            });
        });
    }

    async void OnCapturedPhotoToMemoryAsync(UnityEngine.Windows.WebCam.PhotoCapture.PhotoCaptureResult result, UnityEngine.Windows.WebCam.PhotoCaptureFrame photoCaptureFrame)
    {
        // ターゲットテクスチャに RAW 画像データをコピーします
        photoCaptureFrame.UploadImageDataToTexture(targetTexture);
        byte[] bodyData = targetTexture.EncodeToJPG();

        Response response = new Response();
        Dictionary<string, string> headers = new Dictionary<string, string>();
        headers.Add("Ocp-Apim-Subscription-Key", read_subscription_key);

        try
        {
            string query = read_endpoint;
            // headers.Add("Content-Type": "application/octet-stream");
            response = await Rest.PostAsync(query, bodyData, headers, -1, true);
        }
        catch (Exception e)
        {
            photoCaptureObject.StopPhotoModeAsync(OnStoppedPhotoMode);
            return;
        }

        if (!response.Successful)
        {
            photoCaptureObject.StopPhotoModeAsync(OnStoppedPhotoMode);
            return;
        }

        Debug.Log(response.ResponseCode);
        Debug.Log(response.ResponseBody);

        string operation_url = response.ResponseBody;
        bool poll = true;
        int i = 0;
        Read read;

        do
        {
            System.Threading.Thread.Sleep(1000);
            // https://docs.microsoft.com/ja-jp/azure/cognitive-services/computer-vision/concept-recognizing-text
            // https://github.com/Azure-Samples/cognitive-services-quickstart-code/blob/master/dotnet/ComputerVision/REST/CSharp-hand-text.md
            // {"status":"running","createdDateTime":"2020-12-12T19:39:44Z","lastUpdatedDateTime":"2020-12-12T19:39:44Z"}
            response = await Rest.GetAsync(operation_url, headers, -1, null, true);
            Debug.Log(response.ResponseCode);
            Debug.Log(response.ResponseBody);
            read = JsonUtility.FromJson<Read>(response.ResponseBody);
            if (read.status == "succeeded")
            {
                poll = false;
            }
            ++i;
        } while (i < 60 && poll);

        if (i == 60 && poll == true)
        {
            photoCaptureObject.StopPhotoModeAsync(OnStoppedPhotoMode);
            return;
        }

        string read_text = "";
        foreach (ReadResults readResult in read.analyzeResult.readResults)
        {
            foreach (Lines line in readResult.lines)
            {
                read_text = read_text + line.text + "\n";
                // Debug.Log(line.text);
            }
        }
        read_text += "以上です。";

        // SpeechSDK 追加分ここから
        Debug.Log(read_text);
        await SynthesizeAudioAsync(read_text); // jp 
        // SpeechSDK 追加分ここまで

        // カメラを非アクティブにします
        photoCaptureObject.StopPhotoModeAsync(OnStoppedPhotoMode);
    }

    void OnStoppedPhotoMode(UnityEngine.Windows.WebCam.PhotoCapture.PhotoCaptureResult result)
    {
        // photo capture のリソースをシャットダウンします
        photoCaptureObject.Dispose();
        photoCaptureObject = null;
        waitingForCapture = false;
    }
}
