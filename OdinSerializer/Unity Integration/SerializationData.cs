//-----------------------------------------------------------------------
// <copyright file="SerializationData.cs" company="Sirenix IVS">
// Copyright (c) 2018 Sirenix IVS
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//-----------------------------------------------------------------------
namespace ExtEvents.OdinSerializer
{
    using System;
    using System.Collections.Generic;
    using Object = UnityEngine.Object;

    [Serializable]
    public struct SerializationData
    {
        public DataFormat DataFormat;
        public List<Object> ReferencedUnityObjects;
        public List<SerializationNode> SerializationNodes;
        public byte[] Bytes;

        public bool BytesAreFilled => Bytes != null && Bytes.Length > 0;
        public bool ReferencedUnityObjectsAreFilled => ReferencedUnityObjects != null && ReferencedUnityObjects.Count > 0;
        public bool SerializationNodesAreFilled => SerializationNodes != null && SerializationNodes.Count > 0;

        public void Reset()
        {
            DataFormat = DataFormat.Binary;

            if (BytesAreFilled)
                Bytes = Array.Empty<byte>();

            if (ReferencedUnityObjectsAreFilled)
                ReferencedUnityObjects.Clear();

            if (SerializationNodesAreFilled)
                SerializationNodes.Clear();
        }
    }
}