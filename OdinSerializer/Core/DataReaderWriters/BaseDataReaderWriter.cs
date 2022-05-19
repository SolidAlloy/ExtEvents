//-----------------------------------------------------------------------
// <copyright file="BaseDataReaderWriter.cs" company="Sirenix IVS">
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

    /// <summary>
    /// Implements functionality that is shared by both data readers and data writers.
    /// </summary>
    public abstract class BaseDataReaderWriter
    {
        // Once, there was a stack here. But stacks are slow, so now there's no longer
        //  a stack here and we just do it ourselves.
        private NodeInfo[] nodes = new NodeInfo[32];
        private int nodesLength;

        /// <summary>
        /// Gets a value indicating whether the reader or writer is in an array node.
        /// </summary>
        /// <value>
        /// <c>true</c> if the reader or writer is in an array node; otherwise, <c>false</c>.
        /// </value>
        public bool IsInArrayNode => nodesLength == 0 ? false : nodes[nodesLength - 1].IsArray;

        /// <summary>
        /// Gets the current node depth. In other words, the current count of the node stack.
        /// </summary>
        /// <value>
        /// The current node depth.
        /// </value>
        protected int NodeDepth => nodesLength;

        /// <summary>
        /// Gets the current node, or <see cref="NodeInfo.Empty"/> if there is no current node.
        /// </summary>
        /// <value>
        /// The current node.
        /// </value>
        protected NodeInfo CurrentNode => nodesLength == 0 ? NodeInfo.Empty : nodes[nodesLength - 1];

        /// <summary>
        /// Pushes a node onto the node stack.
        /// </summary>
        /// <param name="node">The node to push.</param>
        protected void PushNode(NodeInfo node)
        {
            if (nodesLength == nodes.Length)
            {
                ExpandNodes();
            }

            nodes[nodesLength] = node;
            nodesLength++;
        }

        /// <summary>
        /// Pushes a node with the given name, id and type onto the node stack.
        /// </summary>
        /// <param name="name">The name of the node.</param>
        /// <param name="id">The id of the node.</param>
        /// <param name="type">The type of the node.</param>
        protected void PushNode(string name, int id, Type type)
        {
            if (nodesLength == nodes.Length)
            {
                ExpandNodes();
            }

            nodes[nodesLength] = new NodeInfo(name, id, type, false);
            nodesLength++;
        }

        /// <summary>
        /// Pushes an array node onto the node stack. This uses values from the current node to provide extra info about the array node.
        /// </summary>
        protected void PushArray()
        {
            if (nodesLength == nodes.Length)
            {
                ExpandNodes();
            }

            if (nodesLength == 0 || nodes[nodesLength - 1].IsArray)
            {
                nodes[nodesLength] = new NodeInfo(null, -1, null, true);
            }
            else
            {
                var current = nodes[nodesLength - 1];
                nodes[nodesLength] = new NodeInfo(current.Name, current.Id, current.Type, true);
            }

            nodesLength++;
        }

        private void ExpandNodes()
        {
            var newArr = new NodeInfo[nodes.Length * 2];

            var oldNodes = nodes;

            for (int i = 0; i < oldNodes.Length; i++)
            {
                newArr[i] = oldNodes[i];
            }

            nodes = newArr;
        }

        /// <summary>
        /// Pops the current node off of the node stack.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">
        /// There are no nodes to pop.
        /// or
        /// Tried to pop node with given name, but the current node's name was different.
        /// </exception>
        protected void PopNode()
        {
            if (nodesLength == 0)
            {
                throw new InvalidOperationException("There are no nodes to pop.");
            }

            // @Speedup - this safety isn't worth the performance hit, and never happens with properly written writers
            //var current = this.CurrentNode;

            //if (current.Name != name)
            //{
            //    throw new InvalidOperationException("Tried to pop node with name " + name + " but current node's name is " + current.Name);
            //}

            nodesLength--;
        }

        /// <summary>
        /// Pops the current node if the current node is an array node.
        /// </summary>
        protected void PopArray()
        {
            if (nodesLength == 0)
            {
                throw new InvalidOperationException("There are no nodes to pop.");
            }

            if (nodes[nodesLength - 1].IsArray == false)
            {
                throw new InvalidOperationException("Was not in array when exiting array.");
            }

            nodesLength--;
        }

        protected void ClearNodes()
        {
            nodesLength = 0;
        }
    }
}