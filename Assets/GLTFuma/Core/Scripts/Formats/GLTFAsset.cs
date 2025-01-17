﻿using System;
using UniJSON;

namespace UMa.GLTF
{
    [Serializable]
    public class GLTFAsset : JsonSerializableBase
    {
        public string generator;

        [JsonSchema(Required = true, Pattern = "^[0-9]+\\.[0-9]+$")]
        public string version;

        public string copyright;

        [JsonSchema(Pattern = "^[0-9]+\\.[0-9]+$")]
        public string minVersion;

        // empty schemas
        public object extensions;
        public object extras;

        protected override void SerializeMembers(GLTFJsonFormatter f)
        {
            f.Key("generator"); f.Value(generator);
            f.Key("version"); f.Value(version);
        }

        public override string ToString()
        {
            return string.Format("GLTF-{0} generated by {1}", version, generator);
        }
    }
}
