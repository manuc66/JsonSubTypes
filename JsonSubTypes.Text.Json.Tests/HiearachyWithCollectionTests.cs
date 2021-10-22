using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using JsonSubTypes.Text.Json;
using NewApi;
using NUnit.Framework;
#if NET35
#else   
using System.Collections.ObjectModel;
#endif

namespace JsonSubTypes.Tests
{
    public class HiearachyWithCollectionTests
    {
        [TestFixture]
        public class HiearachyWithListsTests
        {
            public class Hierachy
            {
                [JsonSubTypeConverter(typeof(JsonSubtypes<Node>), "NodeType")]
                public Node Root { get; set; }
            }

            [KnownSubType(typeof(FolderNode), 1)]
            [KnownSubType(typeof(ElemNode), 2)]
            public class Node
            {
                public virtual int NodeType { get; set; }
            }

            public class FolderNode : Node
            {
                public sealed override int NodeType { get; set; } = 1;

                [JsonSubTypeConverter(typeof(JsonSubtypes<List<Node>>), "NodeType")]
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

                var str = JsonSerializer.Serialize(root);

                Assert.AreEqual(
                    "{\"Root\":{\"NodeType\":1,\"Children\":[{\"NodeType\":1,\"Children\":[{\"NodeType\":2,\"Size\":3}]}]}}",
                    str);
            }

            [Test]
            public void DeserializeHierachyTest()
            {
                var input = "{\"Root\":{\"NodeType\":1,\"Children\":[{\"NodeType\":2,\"Size\":3}]}}";

                var deserialized = JsonSerializer.Deserialize<Hierachy>(input);

                var elemNode = ((deserialized?.Root as FolderNode)?.Children.First() as ElemNode)?.Size;
                Assert.AreEqual(3, elemNode);
            }



            [Test]
            public void DeserializeHierachyDeeperTest()
            {
                var input =
                    "{\"Root\":{\"NodeType\":1,\"Children\":[{\"NodeType\":1,\"Children\":[{\"NodeType\":1,\"Children\":[{\"NodeType\":2,\"Size\":3},{\"NodeType\":2,\"Size\":13}, null]}]}]}}";

                var deserialized = JsonSerializer.Deserialize<Hierachy>(input);

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

                    var deserialized = JsonSerializer.Deserialize<Hierachy>(input);

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
                [JsonSubTypeConverter(typeof(JsonSubtypes<Node>), "NodeType")]
                public Node Root { get; set; }
            }

            [KnownSubType(typeof(FolderNode), 1)]
            [KnownSubType(typeof(ElemNode), 2)]
            public class Node
            {
                public virtual int NodeType { get; set; }
            }

            public class FolderNode : Node
            {
                public sealed override int NodeType { get; set; } = 1;

                [JsonSubTypeConverter(typeof(JsonSubtypes<Node[]>), "NodeType")]
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

                var str = JsonSerializer.Serialize(root);

                Assert.AreEqual(
                    "{\"Root\":{\"NodeType\":1,\"Children\":[{\"NodeType\":1,\"Children\":[{\"NodeType\":2,\"Size\":3}]}]}}",
                    str);
            }

            [Test]
            public void DeserializeHierachyTest()
            {
                var input = "{\"Root\":{\"NodeType\":1,\"Children\":[{\"NodeType\":2,\"Size\":3}]}}";

                var deserialized = JsonSerializer.Deserialize<Hierachy>(input);

                var elemNode = ((deserialized?.Root as FolderNode)?.Children.First() as ElemNode)?.Size;
                Assert.AreEqual(3, elemNode);
            }

            [Test]
            public void DeserializeBadDocument()
            {
                var exception = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Root>("{\"Content\": []}"), "Unrecognized token: Integer");

                Assert.AreEqual("Impossible to read JSON array to fill type: Base", exception.Message);
            }


            [Test]
            public void DeserializeHierachyDeeperTest()
            {
                var input =
                    "{\"Root\":{\"NodeType\":1,\"Children\":[{\"NodeType\":1,\"Children\":[{\"NodeType\":1,\"Children\":[{\"NodeType\":2,\"Size\":3},{\"NodeType\":2,\"Size\":13}]}]}]}}";

                var deserialized = JsonSerializer.Deserialize<Hierachy>(input);

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
                [JsonSubTypeConverter(typeof(JsonSubtypes<Node>), "NodeType")]
                public Node Root { get; set; }
            }

            [KnownSubType(typeof(FolderNode), 1)]
            [KnownSubType(typeof(ElemNode), 2)]
            public class Node
            {
                public virtual int NodeType { get; set; }
            }

            public class FolderNode : Node
            {
                public sealed override int NodeType { get; set; } = 1;

                [JsonSubTypeConverter(typeof(JsonSubtypes<ObservableCollection<Node>>), "NodeType")]


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

                var str = JsonSerializer.Serialize(root);

                Assert.AreEqual(
                    "{\"Root\":{\"NodeType\":1,\"Children\":[{\"NodeType\":1,\"Children\":[{\"NodeType\":2,\"Size\":3}]}]}}",
                    str);
            }

            [Test]
            public void DeserializeHierachyTest()
            {
                var input = "{\"Root\":{\"NodeType\":1,\"Children\":[{\"NodeType\":2,\"Size\":3}]}}";

                var deserialized = JsonSerializer.Deserialize<Hierachy>(input);

                var elemNode = ((deserialized?.Root as FolderNode)?.Children.First() as ElemNode)?.Size;
                Assert.AreEqual(3, elemNode);
            }

            [Test]
            public void DeserializeHierachyDeeperTest()
            {
                var input =
                    "{\"Root\":{\"NodeType\":1,\"Children\":[{\"NodeType\":1,\"Children\":[{\"NodeType\":1,\"Children\":[{\"NodeType\":2,\"Size\":3},{\"NodeType\":2,\"Size\":13}]}]}]}}";

                var deserialized = JsonSerializer.Deserialize<Hierachy>(input);

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
                [JsonSubTypeConverter(typeof(JsonSubtypes<Node>), "NodeType")]
                public Node Root { get; set; }
            }

            [KnownSubType(typeof(FolderNode), 1)]
            [KnownSubType(typeof(ElemNode), 2)]
            public class Node
            {
                public virtual int NodeType { get; set; }
            }

            public class FolderNode : Node
            {
                public sealed override int NodeType { get; set; } = 1;

                [JsonSubTypeConverter(typeof(JsonSubtypes<IEnumerable<Node>>), "NodeType")]
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

                var str = JsonSerializer.Serialize(root);

                Assert.AreEqual(
                    "{\"Root\":{\"NodeType\":1,\"Children\":[{\"NodeType\":1,\"Children\":[{\"NodeType\":2,\"Size\":3}]}]}}",
                    str);
            }

            [Test]
            public void DeserializeHierachyTest()
            {
                var input = "{\"Root\":{\"NodeType\":1,\"Children\":[{\"NodeType\":2,\"Size\":3}]}}";

                var deserialized = JsonSerializer.Deserialize<Hierachy>(input);

                var elemNode = ((deserialized?.Root as FolderNode)?.Children.First() as ElemNode)?.Size;
                Assert.AreEqual(3, elemNode);
            }

            [Test]
            public void DeserializeHierachyDeeperTest()
            {
                var input =
                    "{\"Root\":{\"NodeType\":1,\"Children\":[{\"NodeType\":1,\"Children\":[{\"NodeType\":1,\"Children\":[{\"NodeType\":2,\"Size\":3},{\"NodeType\":2,\"Size\":13}]}]}]}}";

                var deserialized = JsonSerializer.Deserialize<Hierachy>(input);

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

                var deserialized = JsonSerializer.Deserialize<Hierachy>(input);

                Assert.NotNull(deserialized);

                Assert.AreEqual(13,
                    ((ElemNode)((FolderNode)((FolderNode)((FolderNode)deserialized.Root).Children.First()).Children
                        .First()).Children.Skip(1).First()).Size);
            }
        }
    }
}
