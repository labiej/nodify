﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace Nodify.Playground
{
    public struct NodesGeneratorSettings
    {
        private static readonly Random _rand = new Random();

        public NodesGeneratorSettings(int count)
        {
            GridSnap = 15;
            MinNodesCount = MaxNodesCount = count;
            MinInputCount = MinOutputCount = 0;
            MaxInputCount = MaxOutputCount = 7;

            ConnectorNameGenerator = (s, i) => $"C {i}";
            NodeNameGenerator = (s, i) => $"Node {i}";
            NodeLocationGenerator = (s, i) =>
            {
                double EaseOut(double percent, double increment, double start, double end, double total)
                    => -end * (increment /= total) * (increment - 2) + start;

                var xDistanceBetweenNodes = _rand.Next(150, 350);
                var yDistanceBetweenNodes = _rand.Next(200, 350);
                var randSignX = _rand.Next(0, 100) > 50 ? 1 : -1;
                var randSignY = _rand.Next(0, 100) > 50 ? 1 : -1;
                var gridOffsetX = i * xDistanceBetweenNodes;
                var gridOffsetY = i * yDistanceBetweenNodes;

                var x = gridOffsetX * Math.Sin(xDistanceBetweenNodes * randSignX/ (i+1));
                var y = gridOffsetY * Math.Sin(yDistanceBetweenNodes * randSignY/ (i+1));
                var easeX = x * EaseOut(i / count, i, 1, 0.01, count);
                var easeY = y * EaseOut(i / count, i, 1, 0.01, count);

                x = s.Snap((int)easeX);
                y = s.Snap((int)easeY);

                return new Point(x, y);
            };
        }

        public int GridSnap;
        public int MinNodesCount;
        public int MaxNodesCount;
        public int MinInputCount;
        public int MaxInputCount;
        public int MinOutputCount;
        public int MaxOutputCount;

        public Func<NodesGeneratorSettings, int, string?> ConnectorNameGenerator;
        public Func<NodesGeneratorSettings, int, string?> NodeNameGenerator;
        public Func<NodesGeneratorSettings, int, Point> NodeLocationGenerator;

        public int Snap(int x)
            => x / GridSnap * GridSnap;
    }

    public static class RandomNodesGenerator
    {
        private static readonly Random _rand = new Random();

        public static IList<T> GenerateNodes<T>(NodesGeneratorSettings settings)
            where T : FlowNodeViewModel, new()
        {
            var nodes = new List<T>();
            var count = _rand.Next(settings.MinNodesCount, settings.MaxNodesCount);

            for (int i = 0; i < count; i++)
            {
                var node = new T
                {
                    Title = settings.NodeNameGenerator(settings, i),
                    IsCompact = i % 2 == 0,
                    Location = settings.NodeLocationGenerator(settings, i)
                };

                nodes.Add(node);
                node.Input.AddRange(GenerateConnectors(settings, _rand.Next(settings.MinInputCount, settings.MaxInputCount)));
                node.Output.AddRange(GenerateConnectors(settings, _rand.Next(settings.MinOutputCount, settings.MaxOutputCount)));
            }

            return nodes;
        }

        public static IList<ConnectionViewModel> GenerateConnections<TSchema>(IList<NodeViewModel> nodes)
            where TSchema : GraphSchema, new()
        {
            HashSet<NodeViewModel> visited = new HashSet<NodeViewModel>(nodes.Count);
            List<ConnectionViewModel> connections = new List<ConnectionViewModel>(nodes.Count);
            var schema = new TSchema();

            for (int i = 0; i < nodes.Count; i++)
            {
                var n1 = nodes[_rand.Next(0, nodes.Count)];
                var n2 = nodes[_rand.Next(0, nodes.Count)];

                if (n1 == n2 && !(visited.Add(n1) && visited.Add(n2)))
                {
                    continue;
                }

                List<ConnectorViewModel> input = n1 is FlowNodeViewModel flow ? flow.Input.ToList() :
                                                 n1 is KnotNodeViewModel knot ? new List<ConnectorViewModel> { knot.Connector } : new List<ConnectorViewModel>();

                List<ConnectorViewModel> output = n2 is FlowNodeViewModel flow2 ? flow2.Output.ToList() :
                                                  n2 is KnotNodeViewModel knot2 ? new List<ConnectorViewModel> { knot2.Connector } : new List<ConnectorViewModel>();

                connections.AddRange(ConnectPins(schema, input, output));
            }

            return connections;
        }

        public static IList<ConnectionViewModel> ConnectPins(GraphSchema schema, IList<ConnectorViewModel> source, IList<ConnectorViewModel> target)
        {
            Dictionary<ConnectorViewModel, List<ConnectorViewModel>> connections = new Dictionary<ConnectorViewModel, List<ConnectorViewModel>>();
            List<ConnectionViewModel> result = new List<ConnectionViewModel>();

            for (int di = 0; di < target.Count; di++)
            {
                var outP = target[di];

                if (!connections.TryGetValue(outP, out var outConns))
                {
                    var newList = new List<ConnectorViewModel>();
                    connections.Add(outP, newList);
                    outConns = newList;
                }

                var conNum = _rand.Next(0, source.Count);
                for (int ci = 0; ci < conNum; ci++)
                {
                    var inP = source[_rand.Next(0, conNum)];

                    if (!connections.TryGetValue(inP, out var inConns))
                    {
                        var newList = new List<ConnectorViewModel>();
                        connections.Add(inP, newList);
                        inConns = newList;
                    }

                    if (schema.CanAddConnection(inP, outP) && !connections[inP].Contains(outP) && !connections[outP].Contains(inP))
                    {
                        var isInput = inP.Flow == ConnectorFlow.Input;

                        var connection = new ConnectionViewModel
                        {
                            Input = isInput ? inP : outP,
                            Output = isInput ? outP : inP
                        };
                        result.Add(connection);

                        inConns.Add(outP);
                        outConns.Add(inP);
                    }
                }
            }

            return result;
        }

        public static IList<ConnectorViewModel> GenerateConnectors(NodesGeneratorSettings settings, int count)
        {
            var list = new List<ConnectorViewModel>(count);

            for (int i = 0; i < count; i++)
            {
                var connector = new ConnectorViewModel
                {
                    Title = settings.ConnectorNameGenerator(settings, i)
                };

                list.Add(connector);
            }

            return list;
        }
    }
}
