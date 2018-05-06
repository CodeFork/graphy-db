﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace GraphyDb.IO
{
    internal static class DbReader
    {
        internal static readonly Dictionary<string, FileStream>
            ReadFileStreamDictionary = new Dictionary<string, FileStream>();

        internal static void InitializeDbReader()
        {
            foreach (var filePath in DbControl.DbFilePaths)
            {
                ReadFileStreamDictionary[filePath] = new FileStream(Path.Combine(DbControl.DbPath, filePath),
                    FileMode.OpenOrCreate, FileAccess.Read, FileShare.ReadWrite);
            }
        }

        internal static void CloseIOStreams()
        {
            foreach (var filePath in DbControl.DbFilePaths)
            {
                ReadFileStreamDictionary?[filePath].Dispose();
                ReadFileStreamDictionary[filePath] = null;
            }
        }


        public static NodeBlock ReadNodeBlock(int nodeId)
        {
            var buffer = new byte[DbControl.BlockByteSize[DbControl.NodePath]];
            ReadBlock(DbControl.NodePath, nodeId, buffer);
            var used = BitConverter.ToBoolean(buffer, 0);
            var firstInRelationId = BitConverter.ToInt32(buffer.Skip(1).Take(4).ToArray(), 0);
            var firstOutRelationId = BitConverter.ToInt32(buffer.Skip(5).Take(4).ToArray(), 0);
            var nextPropertyId = BitConverter.ToInt32(buffer.Skip(9).Take(4).ToArray(), 0);
            var labelId = BitConverter.ToInt32(buffer.Skip(13).Take(4).ToArray(), 0);
            return new NodeBlock(used, nodeId, firstInRelationId, firstOutRelationId, nextPropertyId, labelId);
        }

        public static EdgeBlock ReadEdgeBlock(int edgeId)
        {
            var buffer = new byte[DbControl.BlockByteSize[DbControl.EdgePath]];
            ReadBlock(DbControl.EdgePath, edgeId, buffer);
            EdgeBlock e = new EdgeBlock
            {
                Used = BitConverter.ToBoolean(buffer, 0),
                FirstNode = BitConverter.ToInt32(buffer.Skip(1).Take(4).ToArray(), 0),
                SecondNode = BitConverter.ToInt32(buffer.Skip(5).Take(4).ToArray(), 0),
                FirstNodePreviousRelation = BitConverter.ToInt32(buffer.Skip(9).Take(4).ToArray(), 0),
                FirstNodeNextRelation = BitConverter.ToInt32(buffer.Skip(13).Take(4).ToArray(), 0),
                SecondNodePreviousRelation = BitConverter.ToInt32(buffer.Skip(17).Take(4).ToArray(), 0),
                SecondNodeNextRelation = BitConverter.ToInt32(buffer.Skip(21).Take(4).ToArray(), 0),
                NextProperty = BitConverter.ToInt32(buffer.Skip(25).Take(4).ToArray(), 0),
                LabelId = BitConverter.ToInt32(buffer.Skip(29).Take(4).ToArray(), 0)
            };
            return e;
        }

        public static GenericStringBlock ReadGenericStringBlock(string storagePath, int id)
        {
            var buffer = new byte[DbControl.BlockByteSize[storagePath]];
            ReadBlock(storagePath, id, buffer);
            var used = BitConverter.ToBoolean(buffer, 0);
            var bitsUsed = buffer[1];
            var text = Encoding.UTF8.GetString(buffer.Skip(2).Take(bitsUsed).ToArray());
            switch (storagePath)
            {
                case DbControl.LabelPath:
                    return new LabelBlock(used, text, id);
                case DbControl.StringPath:
                    return new StringBlock(used, text, id);
                case DbControl.PropertyNamePath:
                    return new PropertyNameBlock(used, text, id);
                default:
                    throw new ArgumentException("Storage path is invalid.");
            }
        }

        public static PropertyBlock ReadPropertyBlock(string storagePath, int id)
        {
            var buffer = new byte[DbControl.BlockByteSize[storagePath]];
            ReadBlock(storagePath, id, buffer);
            var used = buffer[0] % 2 == 1;
            var dtype = (PropertyType) (buffer[0] >> 1);
            var propertyName = BitConverter.ToInt32(buffer.Skip(1).Take(4).ToArray(), 0);
            var propertyValue = buffer.Skip(5).Take(4).ToArray();
            var nextProperty = BitConverter.ToInt32(buffer.Skip(9).Take(4).ToArray(), 0);
            var nodeId = BitConverter.ToInt32(buffer.Skip(13).Take(4).ToArray(), 0);
            switch (storagePath)
            {
                case DbControl.NodePropertyPath:
                    return new NodePropertyBlock(id, used, dtype, propertyName, propertyValue, nextProperty, nodeId);
                case DbControl.EdgePropertyPath:
                    return new EdgePropertyBlock(id, used, dtype, propertyName, propertyValue, nextProperty, nodeId);
                default:
                    throw new ArgumentException("Storage path is invalid.");
            }
        }

        /// <summary>
        /// Read specific block from file
        /// </summary>
        /// <param name="filePath">Path to the file with byte-record structure</param>
        /// <param name="blockNumber">Block position from the beggining of the file</param>
        /// <param name="block"> Buffer to which result is written</param>
        public static void ReadBlock(string filePath, int blockNumber, byte[] block)
        {
            var offset = blockNumber * DbControl.BlockByteSize[filePath];
            ReadFileStreamDictionary[filePath].Seek(offset, SeekOrigin.Begin);
            ReadFileStreamDictionary[filePath].Read(block, 0, DbControl.BlockByteSize[filePath]);
        }
    }
}