using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace JsonSubTypes.Tests
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

        [JsonConverter(typeof(JsonSubtypesWithNested), "NodeType")]
        public Node Child { get; set; }
    }
    public class ElemNode : Node
    {
        public sealed override int NodeType { get; set; } = 2;
        public long Size { get; set; }
    }

    [TestClass]
    public class HiearachyTests
    {
        [TestMethod]
        public void SerializeHierachyTest()
        {
            var root = new Hierachy
            {
                Root = new FolderNode
                {
                    Child = new FolderNode
                    {
                        Child = new ElemNode { Size = 3 }
                    }
                }
            };

            string str = JsonConvert.SerializeObject(root);

            Assert.AreEqual("{\"Root\":{\"NodeType\":1,\"Child\":{\"NodeType\":1,\"Child\":{\"NodeType\":2,\"Size\":3}}}}", str);
        }

        [TestMethod]
        public void DeserializeHierachyTest()
        {
            var expected = "{\"Root\":{\"NodeType\":1,\"Child\":{\"NodeType\":2,\"Size\":3}}}";

            var deserialized = JsonConvert.DeserializeObject<Hierachy>(expected);

            var elemNode = ((deserialized?.Root as FolderNode)?.Child as ElemNode)?.Size;
            Assert.AreEqual(3, elemNode);
        }

        [TestMethod]
        public void DeserializeHierachyDeeperTest()
        {
            var expected = "{\"Root\":{\"NodeType\":1,\"Child\":{\"NodeType\":1,\"Child\":{\"NodeType\":1,\"Child\":{\"NodeType\":2,\"Size\":3}}}}}";

            var deserialized = JsonConvert.DeserializeObject<Hierachy>(expected);

            var nodeType = (((deserialized?.Root as FolderNode)?.Child as FolderNode)?.Child as FolderNode)?.Child?.NodeType;
            Assert.AreEqual(2, nodeType);
            var elemNode = ((((deserialized?.Root as FolderNode)?.Child as FolderNode)?.Child as FolderNode)?.Child as ElemNode)?.Size;
            Assert.AreEqual(3, elemNode);
        }
    }

}
