using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace JsonSubTypes.Tests
{
    [TestClass]
    public class HiearachyWithArrayTests
    {
        public class Hierachy
        {
            [JsonConverter(typeof(JsonSubtypes), "NodeType")]
            public Node Root { get; set; }
        }

        [JsonSubtypes.KnownSubType(typeof(FolderNode), 1)]
        [JsonSubtypes.KnownSubType(typeof(ElemNode), 2)]
        public class Node
        {
            public virtual int NodeType { get; set; }
        }

        public class FolderNode : Node
        {
            public sealed override int NodeType { get; set; } = 1;

            [JsonConverter(typeof(JsonSubtypes), "NodeType")]
            public Node[] Children { get; set; }
        }

        public class ElemNode : Node
        {
            public sealed override int NodeType { get; set; } = 2;
            public long Size { get; set; }
        }

        [TestMethod]
        public void SerializeHierachyTest()
        {
            var root = new Hierachy
            {
                Root = new FolderNode
                {
                    Children = new[]
                    {
                        new FolderNode
                        {
                            Children = new [] {new ElemNode {Size = 3}}
                        }
                    }
                }
            };

            var str = JsonConvert.SerializeObject(root);

            Assert.AreEqual("{\"Root\":{\"NodeType\":1,\"Children\":[{\"NodeType\":1,\"Children\":[{\"NodeType\":2,\"Size\":3}]}]}}", str);
        }

        [TestMethod]
        public void DeserializeHierachyTest()
        {
            var input = "{\"Root\":{\"NodeType\":1,\"Children\":[{\"NodeType\":2,\"Size\":3}]}}";

            var deserialized = JsonConvert.DeserializeObject<Hierachy>(input);

            var elemNode = ((deserialized?.Root as FolderNode)?.Children.First() as ElemNode)?.Size;
            Assert.AreEqual(3, elemNode);
        }

        [TestMethod]
        public void DeserializeHierachyDeeperTest()
        {
            var input = "{\"Root\":{\"NodeType\":1,\"Children\":[{\"NodeType\":1,\"Children\":[{\"NodeType\":1,\"Children\":[{\"NodeType\":2,\"Size\":3},{\"NodeType\":2,\"Size\":13}]}]}]}}";

            var deserialized = JsonConvert.DeserializeObject<Hierachy>(input);

            Assert.IsNotNull(deserialized);

            Assert.AreEqual(13, ((ElemNode)new List<Node>(((FolderNode)new List<Node>(((FolderNode)new List<Node>(((FolderNode)deserialized.Root).Children)[0]).Children)[0]).Children)[1]).Size);
        }
    }
}