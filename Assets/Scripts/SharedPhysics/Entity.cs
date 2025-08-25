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
        
        public static Vector2 GetForwardDirection(Entity entity)
        {
            var normalizedRotation = (entity.Rotation % 360 + 360) % 360;
            var rotationRadians = normalizedRotation * (float)(Math.PI / 180.0);
            var forwardDirection = new Vector2((float)Math.Sin(rotationRadians), (float)Math.Cos(rotationRadians));
            return forwardDirection;
        }
    }
}