using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
//============================================================
//支持中文，文件使用UTF-8编码
//@author	JiphuTzu
//@create	20210323
//@company	UMa
//
//@description:
//============================================================
namespace UMa.GLTF
{
    public class WebStorage : IStorage
    {
        private string _root;
        private Dictionary<string, ArraySegment<byte>> _data;
        private Dictionary<string, Texture2D> _textures;
        private Dictionary<string, Action<float>> _progress;
        public WebStorage(string root)
        {
            if (!root.StartsWith("http") && !root.StartsWith("file"))
            {
#if UNITY_ANDROID
                root = "jar://"+root;
#else
                root = "file://" + root;
#endif
            }
            _root = root;
            Debug.Log("web storage root = " + _root);
            _data = new Dictionary<string, ArraySegment<byte>>();
            _textures = new Dictionary<string, Texture2D>();
            _progress = new Dictionary<string, Action<float>>();
        }
        public async Task<ArraySegment<byte>> Load(string url, Action<float> progress)
        {
            if (!_data.ContainsKey(url))
            {
                //正在加载中
                if (progress == null) progress = f => { };
                if (_progress.ContainsKey(url))
                {
                    _progress[url] += progress;
                    while (!_data.ContainsKey(url))
                    {
                        await Task.Yield();
                    }
                }
                else
                {
                    _progress.Add(url,progress);
                    var data = await LoadData(url);
                    _data.Add(url, new ArraySegment<byte>(data));
                    _progress.Remove(url);
                }
            }
            return _data[url];
        }
        public async Task<Texture2D> LoadTexture(string url, Action<float> progress)
        {
            if (!_textures.ContainsKey(url))
            {
                //正在加载中
                if (progress == null) progress = f => { };
                if (_progress.ContainsKey(url))
                {
                    _progress[url] += progress;
                    while (!_textures.ContainsKey(url))
                    {
                        await Task.Yield();
                    }
                }
                else
                {
                    // Debug.Log("load texture ...");
                    _progress.Add(url,progress);
                    var data = await LoadTexture(url);
                    _textures.Add(url, data);
                    _progress.Remove(url);
                }
            }
            // Debug.Log("loaded texture ... "+_textures[url]);
            return _textures[url];
        }
        public ArraySegment<byte> Get(string url)
        {
            if(!_data.ContainsKey(url)){
                if (url.StartsWith("data:")) _data.Add(url,new ArraySegment<byte>(url.ReadEmbeded()));
            }
            if (_data.ContainsKey(url)) return _data[url];
            //Debug.Log("get storage ... "+url + " ======= "+bytes.Length);
            return new ArraySegment<byte>(new byte[0]);
        }

        public string GetPath(string url)
        {
            if (url.StartsWith("data:")) return null;
            return Path.Combine(_root, url).Replace("\\", "/");
        }
        private async Task<byte[]> LoadData(string url)
        {
            var path = GetPath(url);
            //Debug.Log("load path = " + path);
            var www = UnityWebRequest.Get(path);
            var ao = www.SendWebRequest();
            while (!www.isDone)
            {
                _progress[url].Invoke(ao.progress);
                await Task.Yield();
            }
            if(!string.IsNullOrEmpty(www.error)){
                Debug.Log("load fail :: "+path);
            }
            return www.downloadHandler.data;
        }
        private async Task<Texture2D> LoadTexture(string url)
        {
            var path = GetPath(url);
            //Debug.Log("load path = " + path);
            var www = UnityWebRequestTexture.GetTexture(path);
            var ao = www.SendWebRequest();
            while (!www.isDone)
            {
                //Debug.Log(url+"..."+ao.progress);
                _progress[url].Invoke(ao.progress);
                await Task.Yield();
            }
            if(!string.IsNullOrEmpty(www.error)){
                Debug.Log("load fail :: "+path);
            }
            return ((DownloadHandlerTexture)www.downloadHandler).texture;
        }
    }
}