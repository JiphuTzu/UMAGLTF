using System;
using System.Collections;
using UniGLTF;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
//============================================================
//支持中文，文件使用UTF-8编码
//@author	JiphuTzu
//@create	#CREATEDATE#
//@company	#COMPANY#
//
//@description:
//============================================================
namespace UMa
{
    public class WebLoader : MonoBehaviour
    {
        public Text text;
        public string url = "http://47.92.208.125:8080/files/BrainStem/BrainStem.gltf";
        public bool loadOnStart = false;
        private ImporterContext _loader;
        private void Start()
        {
            if (loadOnStart && !string.IsNullOrEmpty(url))
            {
                Load(url, p => text.text = $"{p * 100:f2}%", null);
            }
        }
        public void Load(string url, Action<float> progress, Action<GameObject> complete)
        {
            this.url = url;
            StartCoroutine(Load(progress, complete));
        }
        public IEnumerator Load(Action<float> progress, Action<GameObject> complete)
        {
            yield return null;
            var www = UnityWebRequest.Get(url);
            var ao = www.SendWebRequest();
            while (!www.isDone)
            {
                progress?.Invoke(ao.progress * 0.1f);
                yield return null;
            }
            _loader = new ImporterContext();
            //Debug.Log(www.downloadHandler.text);
            _loader.ParseJson(www.downloadHandler.text);
            var storage = new WebStorage(url.Substring(0, url.LastIndexOf("/")));
            int total = _loader.GLTF.buffers.Count + _loader.GLTF.images.Count;
            int current = 0;
            foreach (var buffer in _loader.GLTF.buffers)
            {
                Debug.Log(buffer.uri);
                yield return storage.Load(buffer.uri, p => progress?.Invoke(0.1f + 0.8f * (current + p) / total));
                //Debug.Log(buffer.uri + " loaded");
                buffer.OpenStorage(storage);
                current++;
            }
            foreach (var image in _loader.GLTF.images)
            {
                yield return storage.Load(image.uri, p => progress?.Invoke(0.1f + 0.8f * (current + p) / total));
                current++;
            }
            yield return _loader.Load(storage, p => progress?.Invoke(0.9f + p * 0.1f));
            //loader.Parse(url,www.downloadHandler.data);
            _loader.ShowMeshes();
            _loader.Root.SetActive(false);
            _loader.Root.transform.SetParent(transform);
            _loader.Root.SetActive(true);
            complete?.Invoke(_loader.Root);
        }
        public void Unload()
        {
            StopAllCoroutines();
            if (_loader == null) return;
            _loader.Dispose();
            _loader = null;
        }
    }
}