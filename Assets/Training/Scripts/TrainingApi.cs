using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace UnityTrain
{
    public class TrainingApi : MonoBehaviour
    {
        public TrainingSettings settings;
        public IEnumerator Send(string method, string path, string body, Action<string, string> completed)
        {
            string baseUrl = settings ? settings.baseUrl.TrimEnd('/') : "http://127.0.0.1:8080";
            using (var request = new UnityWebRequest(baseUrl + path, method))
            {
                request.downloadHandler = new DownloadHandlerBuffer();
                request.timeout = settings ? settings.timeoutSeconds : 5;
                if (body != null)
                {
                    request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
                    request.SetRequestHeader("Content-Type", "application/json; charset=utf-8");
                }
                yield return request.SendWebRequest();
                string error = request.result == UnityWebRequest.Result.Success ? null :
                    "HTTP " + request.responseCode + "：" + request.error + " " + request.downloadHandler.text;
                completed(request.downloadHandler.text, error);
            }
        }
    }
}
