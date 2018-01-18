using System;
using System.Collections.Generic;
#if NET35
#else   
using System.Collections.ObjectModel;
#endif
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;

namespace JsonSubTypes.Tests
{
    public class HiearachyWithCollectionTests
    {
        [TestFixture]
        public class HiearachyWithListsTests
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
                public List<Node> Children { get; set; }
            }

            public class ElemNode : Node
            {
                public sealed override int NodeType { get; set; } = 2;
                public long Size { get; set; }
            }

            [Test]
            public void SerializeHierachyTest()
            {
                var root = new Hierachy
                {
                    Root = new FolderNode
                    {
                        Children = new List<Node>
                        {
                            new FolderNode
                            {
                                Children = new List<Node> {new ElemNode {Size = 3}}
                            }
                        }
                    }
                };

                var str = JsonConvert.SerializeObject(root);

                Assert.AreEqual(
                    "{\"Root\":{\"NodeType\":1,\"Children\":[{\"NodeType\":1,\"Children\":[{\"NodeType\":2,\"Size\":3}]}]}}",
                    str);
            }

            [Test]
            public void DeserializeHierachyTest()
            {
                var input = "{\"Root\":{\"NodeType\":1,\"Children\":[{\"NodeType\":2,\"Size\":3}]}}";

                var deserialized = JsonConvert.DeserializeObject<Hierachy>(input);

                var elemNode = ((deserialized?.Root as FolderNode)?.Children.First() as ElemNode)?.Size;
                Assert.AreEqual(3, elemNode);
            }



            [Test]
            public void DeserializeHierachyDeeperTest()
            {
                var input =
                    "{\"Root\":{\"NodeType\":1,\"Children\":[{\"NodeType\":1,\"Children\":[{\"NodeType\":1,\"Children\":[{\"NodeType\":2,\"Size\":3},{\"NodeType\":2,\"Size\":13}, null]}]}]}}";

                var deserialized = JsonConvert.DeserializeObject<Hierachy>(input);

                Assert.NotNull(deserialized);

                Assert.AreEqual(13,
                    ((ElemNode)((FolderNode)((FolderNode)((FolderNode)deserialized.Root).Children[0]).Children[0])
                        .Children[1]).Size);
            }

            [Test]
            public void ConcurrentThreadDeserializeHierachyDeeperTest()
            {
                Action test = () =>
                {
                    var input =
                        "{\"Root\":{\"NodeType\":1,\"Children\":[{\"NodeType\":1,\"Children\":[{\"NodeType\":1,\"Children\":[{\"NodeType\":2,\"Size\":3},{\"NodeType\":2,\"Size\":13}, null]}]}]}}";

                    var deserialized = JsonConvert.DeserializeObject<Hierachy>(input);

                    Assert.NotNull(deserialized);

                    Assert.AreEqual(13,
                        ((ElemNode)((FolderNode)((FolderNode)((FolderNode)deserialized.Root).Children[0]).Children[0])
                            .Children[1]).Size);
                };

                Parallel.For(0, 100, index => test());

            }
        }
        [TestFixture]
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

            [Test]
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
                                Children = new[] {new ElemNode {Size = 3}}
                            }
                        }
                    }
                };

                var str = JsonConvert.SerializeObject(root);

                Assert.AreEqual(
                    "{\"Root\":{\"NodeType\":1,\"Children\":[{\"NodeType\":1,\"Children\":[{\"NodeType\":2,\"Size\":3}]}]}}",
                    str);
            }

            [Test]
            public void DeserializeHierachyTest()
            {
                var input = "{\"Root\":{\"NodeType\":1,\"Children\":[{\"NodeType\":2,\"Size\":3}]}}";

                var deserialized = JsonConvert.DeserializeObject<Hierachy>(input);

                var elemNode = ((deserialized?.Root as FolderNode)?.Children.First() as ElemNode)?.Size;
                Assert.AreEqual(3, elemNode);
            }

            [Test]
            public void DeserializeHierachyDeeperTest()
            {
                var input =
                    "{\"Root\":{\"NodeType\":1,\"Children\":[{\"NodeType\":1,\"Children\":[{\"NodeType\":1,\"Children\":[{\"NodeType\":2,\"Size\":3},{\"NodeType\":2,\"Size\":13}]}]}]}}";

                var deserialized = JsonConvert.DeserializeObject<Hierachy>(input);

                Assert.NotNull(deserialized);

                Assert.AreEqual(13,
                    ((ElemNode)((FolderNode)((FolderNode)((FolderNode)deserialized.Root).Children[0]).Children[0])
                        .Children[1]).Size);
            }
        }

        [TestFixture]
        public class HiearachyWithObservableCollectionTests
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


#if NET35
                public List<Node> Children { get; set; }
#else
                public ObservableCollection<Node> Children { get; set; }
#endif
            }

            public class ElemNode : Node
            {
                public sealed override int NodeType { get; set; } = 2;
                public long Size { get; set; }
            }

            [Test]
            public void SerializeHierachyTest()
            {
                var root = new Hierachy
                {
                    Root = new FolderNode
                    {

#if NET35
            Children = new List<Node>
#else
                        Children = new ObservableCollection<Node>
#endif

                        {
                            new FolderNode
                            {
#if NET35
                                Children = new List<Node> {new ElemNode {Size = 3}}
#else    
                                Children = new ObservableCollection<Node> {new ElemNode {Size = 3}}
#endif
                                
                            }
                        }
                    }
                };

                var str = JsonConvert.SerializeObject(root);

                Assert.AreEqual(
                    "{\"Root\":{\"NodeType\":1,\"Children\":[{\"NodeType\":1,\"Children\":[{\"NodeType\":2,\"Size\":3}]}]}}",
                    str);
            }

            [Test]
            public void DeserializeHierachyTest()
            {
                var input = "{\"Root\":{\"NodeType\":1,\"Children\":[{\"NodeType\":2,\"Size\":3}]}}";

                var deserialized = JsonConvert.DeserializeObject<Hierachy>(input);

                var elemNode = ((deserialized?.Root as FolderNode)?.Children.First() as ElemNode)?.Size;
                Assert.AreEqual(3, elemNode);
            }

            [Test]
            public void DeserializeHierachyDeeperTest()
            {
                var input =
                    "{\"Root\":{\"NodeType\":1,\"Children\":[{\"NodeType\":1,\"Children\":[{\"NodeType\":1,\"Children\":[{\"NodeType\":2,\"Size\":3},{\"NodeType\":2,\"Size\":13}]}]}]}}";

                var deserialized = JsonConvert.DeserializeObject<Hierachy>(input);

                Assert.NotNull(deserialized);

                Assert.AreEqual(13,
                    ((ElemNode)((FolderNode)((FolderNode)((FolderNode)deserialized.Root).Children[0]).Children[0])
                        .Children[1]).Size);
            }
        }
        [TestFixture]
        public class HiearachyWithIEnumerableCollectionTests
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
                public IEnumerable<Node> Children { get; set; }
            }

            public class ElemNode : Node
            {
                public sealed override int NodeType { get; set; } = 2;
                public long Size { get; set; }
            }

            [Test]
            public void SerializeHierachyTest()
            {
                var root = new Hierachy
                {
                    Root = new FolderNode
                    {
#if NET35
                        Children = new List<Node>
#else
                        Children = new ObservableCollection<Node>
#endif
                        {
                            new FolderNode
                            {
#if NET35
                                Children = new List<Node> {new ElemNode {Size = 3}}
#else    
                                Children = new ObservableCollection<Node> {new ElemNode {Size = 3}}
#endif

                            }
                        }
                    }
                };

                var str = JsonConvert.SerializeObject(root);

                Assert.AreEqual(
                    "{\"Root\":{\"NodeType\":1,\"Children\":[{\"NodeType\":1,\"Children\":[{\"NodeType\":2,\"Size\":3}]}]}}",
                    str);
            }

            [Test]
            public void DeserializeHierachyTest()
            {
                var input = "{\"Root\":{\"NodeType\":1,\"Children\":[{\"NodeType\":2,\"Size\":3}]}}";

                var deserialized = JsonConvert.DeserializeObject<Hierachy>(input);

                var elemNode = ((deserialized?.Root as FolderNode)?.Children.First() as ElemNode)?.Size;
                Assert.AreEqual(3, elemNode);
            }

            [Test]
            public void DeserializeHierachyDeeperTest()
            {
                var input =
                    "{\"Root\":{\"NodeType\":1,\"Children\":[{\"NodeType\":1,\"Children\":[{\"NodeType\":1,\"Children\":[{\"NodeType\":2,\"Size\":3},{\"NodeType\":2,\"Size\":13}]}]}]}}";

                var deserialized = JsonConvert.DeserializeObject<Hierachy>(input);

                Assert.NotNull(deserialized);

                Assert.AreEqual(13,
                    ((ElemNode)((FolderNode)((FolderNode)((FolderNode)deserialized.Root).Children.First()).Children
                        .First()).Children.Skip(1).First()).Size);
            }

            [Test]
            public void DeserializeHierachyDeeperTestWithComments()
            {
                var input = "/* foo bar */{/* foo bar */\"Root\":" +
                            "/* foo bar */ /* foo bar */ {/* foo bar */\"NodeType\":1,/* foo bar */\"Children\":" +
                            "/* foo bar */[/* foo bar */{\"NodeType\":1,/* foo bar */\"Children\":" +
                            "/* foo bar */[/* foo bar */{\"NodeType\":1,/* foo bar */\"Children\":" +
                            "/* foo bar */[" +
                            "/* foo bar */{/* foo bar */\"NodeType\":2,\"Size\":3}/* foo bar */," +
                            "/* foo bar */{/* foo bar */\"NodeType\":2,\"Size\":13}/* foo bar */]" +
                            "/* foo bar */}/* foo bar */]/* foo bar */}/* foo bar */]/* foo bar */}/* foo bar */}/* foo bar */";

                var deserialized = JsonConvert.DeserializeObject<Hierachy>(input);

                Assert.NotNull(deserialized);

                Assert.AreEqual(13,
                    ((ElemNode)((FolderNode)((FolderNode)((FolderNode)deserialized.Root).Children.First()).Children
                        .First()).Children.Skip(1).First()).Size);
            }
        }
    }
}