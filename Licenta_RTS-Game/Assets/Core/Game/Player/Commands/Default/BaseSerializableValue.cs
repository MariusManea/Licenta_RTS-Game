﻿using RTSLockstep.Data;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using RTSLockstep.Utility;

namespace RTSLockstep.Player.Commands
{
    public abstract class BaseSerializableValue : ICommandData
    {
        protected static readonly BinaryFormatter binaryFormatter = new BinaryFormatter();

        public abstract object ObjectValue { get; protected set; }

        public void Write(Writer writer)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                binaryFormatter.Serialize(stream, ObjectValue);
                byte[] streamBytes = stream.ToArray();
                ushort size = (ushort)streamBytes.Length;
                writer.Write(size);
                writer.Write(streamBytes);
            }
        }
        public void Read(Reader reader)
        {
            ushort size = reader.ReadUShort();
            using (MemoryStream stream = new MemoryStream(reader.Source, reader.Position, (int)size))
            {
                object o = binaryFormatter.Deserialize(stream);
                ObjectValue = (object[])o;
            }
        }

    }
}