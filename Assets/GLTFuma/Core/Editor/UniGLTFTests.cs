﻿using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using UniJSON;
using UnityEngine;


namespace UMa.GLTF
{
    public class UniGLTFTests
    {
        static GameObject CreateSimpelScene()
        {
            var root = new GameObject("gltfRoot").transform;

            var scene = new GameObject("scene0").transform;
            scene.SetParent(root, false);
            scene.localPosition = new Vector3(1, 2, 3);

            return root.gameObject;
        }

        void AssertAreEqual(Transform go, Transform other)
        {
            var lt = go.Traverse().GetEnumerator();
            var rt = go.Traverse().GetEnumerator();

            while (lt.MoveNext())
            {
                if (!rt.MoveNext())
                {
                    throw new Exception("rt shorter");
                }

                MonoBehaviourComparator.AssertAreEquals(lt.Current.gameObject, rt.Current.gameObject);
            }

            if (rt.MoveNext())
            {
                throw new Exception("rt longer");
            }
        }

        [Test]
        public void UniGLTFSimpleSceneTest()
        {
            var go = CreateSimpelScene();
            var context = new GLTFImporter();

            try
            {
                // export
                var gltf = new GLTFRoot();

                string json = null;
                using (var exporter = new GLTFExporter(gltf))
                {
                    exporter.Prepare(go);
                    exporter.Export();

                    // remove empty buffer
                    gltf.buffers.Clear();

                    json = gltf.ToJson();
                }

                // import
                context.ParseJson(json, new SimpleStorage(new ArraySegment<byte>()));
                //Debug.LogFormat("{0}", context.Json);
                //context.Load();

                AssertAreEqual(go.transform, context.root.transform);
            }
            finally
            {
                //Debug.LogFormat("Destory, {0}", go.name);
                GameObject.DestroyImmediate(go);
                context.EditorDestroyRootAndAssets();
            }
        }

        void BufferTest(int init, params int[] size)
        {
            var initBytes = init == 0 ? null : new byte[init];
            var storage = new ArrayByteBuffer(initBytes);
            var buffer = new GLTFBuffer(storage);

            var values = new List<byte>();
            int offset = 0;
            foreach (var x in size)
            {
                var nums = Enumerable.Range(offset, x).Select(y => (Byte)y).ToArray();
                values.AddRange(nums);
                var bytes = new ArraySegment<Byte>(nums);
                offset += x;
                buffer.Append(bytes, GLTFBufferTarget.NONE);
            }

            Assert.AreEqual(values.Count, buffer.byteLength);
            Assert.True(Enumerable.SequenceEqual(values, buffer.GetBytes().ToArray()));
        }

        [Test]
        public void BufferTest()
        {
            BufferTest(0, 0, 100, 200);
            BufferTest(0, 128);
            BufferTest(0, 256);

            BufferTest(1024, 0);
            BufferTest(1024, 128);
            BufferTest(1024, 2048);
            BufferTest(1024, 900, 900);
        }

        [Test]
        public void UnityPathTest()
        {
            var root = UnityPath.FromUnityPath(".");
            Assert.IsFalse(root.isNull);
            Assert.IsFalse(root.isUnderAssetsFolder);
            Assert.AreEqual(UnityPath.FromUnityPath("."), root);

            var assets = UnityPath.FromUnityPath("Assets");
            Assert.IsFalse(assets.isNull);
            Assert.IsTrue(assets.isUnderAssetsFolder);

            var rootChild = root.Child("Assets");
            Assert.AreEqual(assets, rootChild);

            var assetsChild = assets.Child("Hoge");
            var hoge = UnityPath.FromUnityPath("Assets/Hoge");
            Assert.AreEqual(assetsChild, hoge);

            //var children = root.TravserseDir().ToArray();
        }

        [Test]
        public void VersionChecker()
        {
            Assert.False(GLTFImporter.IsGeneratedGLTFumaAndOlderThan("hoge", 1, 16));
            Assert.False(GLTFImporter.IsGeneratedGLTFumaAndOlderThan("UniGLTF-1.16", 1, 16));
            Assert.True(GLTFImporter.IsGeneratedGLTFumaAndOlderThan("UniGLTF-1.15", 1, 16));
            Assert.False(GLTFImporter.IsGeneratedGLTFumaAndOlderThan("UniGLTF-11.16", 1, 16));
            Assert.True(GLTFImporter.IsGeneratedGLTFumaAndOlderThan("UniGLTF-0.16", 1, 16));
            Assert.True(GLTFImporter.IsGeneratedGLTFumaAndOlderThan("UniGLTF", 1, 16));
        }

        [Test]
        public void MeshTest()
        {
            var mesh = new GLTFMesh("mesh")
            {
                primitives = new List<GLTFPrimitives>
                {
                    new GLTFPrimitives
                    {
                        attributes=new GLTFAttributes
                        {
                            POSITION=0,
                        }
                    }
                }
            };

            var f = new JsonFormatter();
            f.Serialize(mesh);

            var json = new Utf8String(f.GetStoreBytes()).ToString();
            Debug.Log(json);
        }

        [Test]
        public void PrimitiveTest()
        {
            var prims = new List<GLTFPrimitives> {
                new GLTFPrimitives
                {
                    attributes = new GLTFAttributes
                    {
                        POSITION = 0,
                    }
                }
            };

            var f = new JsonFormatter();
            f.Serialize(prims);

            var json = new Utf8String(f.GetStoreBytes()).ToString();
            Debug.Log(json);
        }

    }
}
