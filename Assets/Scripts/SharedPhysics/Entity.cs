using System;

namespace SharedPhysics
{
    [Serializable]
    public struct Entity
    {
        public uint Id;
        public float Speed;
        public Vector2 Position;
        public Vector2 Direction;
        public float Rotation;
        public ulong SequenceId;

        public override string ToString()
        {
            return $"{{Id: {Id}, Seq: {SequenceId}, Speed: {Speed}, Pos: {Position}, Dir: {Direction}, Rot: {Rotation}}}";
        }
    }
}