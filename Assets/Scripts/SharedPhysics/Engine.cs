using System;
using System.Linq;

namespace SharedPhysics
{
    public static class Engine
    {
        public static Vector2? GetIntersection(Line line1, Line line2)
        {
            var p1 = line1.Start;
            var p2 = line1.End;
            var p3 = line2.Start;
            var p4 = line2.End;
    
            var d1 = p2 - p1; // Direction of line 1
            var d2 = p4 - p3; // Direction of line 2
    
            var cross = d1.X * d2.Y - d1.Y * d2.X;
    
            if (Math.Abs(cross) < 0.0001f)
                return default; // Parallel lines
    
            var t = ((p3.X - p1.X) * d2.Y - (p3.Y - p1.Y) * d2.X) / cross;
            var u = ((p3.X - p1.X) * d1.Y - (p3.Y - p1.Y) * d1.X) / cross;
    
            if (t >= 0 && t <= 1 && u >= 0 && u <= 1)
            {
                return p1 + d1 * t;
            }
    
            return default;
        }

        public static Line[] GetNearbyLines(Line line1, Line[] allLines)
        {
            var movementBounds = BoundingBox.FromLine(line1);
            return allLines.Where(line => BoundingBox.FromLine(line).Overlaps(movementBounds)).ToArray();
        }

        public static Entity[] Simulate(
            float deltaTime, ulong sequenceId, Entity[] entities, Line[] lines)
        {
            var processed = new Entity[entities.Length];
            var i = 0;
            
            foreach (var entity in entities)
            {
                var forwardDirection = Entity.GetForwardDirection(entity);
                var normalizedDirection = entity.Direction.Normalized();
                var isBackpedalling = Vector2.Dot(normalizedDirection, forwardDirection) < -0.01f;
                var backpedalMultiplier = isBackpedalling ? 0.5f : 1f;
                var movementPerInterval = entity.Speed * backpedalMultiplier * deltaTime;
                var targetMovement = normalizedDirection * movementPerInterval;
                var targetPosition = entity.Position + targetMovement;
                var movementLine = new Line(entity.Position, targetPosition);
                var nearbyLines = GetNearbyLines(movementLine, lines);
        
                foreach (var line in nearbyLines)
                {
                    var intersection = GetIntersection(movementLine, line);
                    if (!intersection.HasValue) continue;
                    var safeDistance = (intersection.Value - entity.Position).Normalized() * 0.05f;
                    var safeMovement = intersection.Value - safeDistance - entity.Position;
                    var safePosition = entity.Position + safeMovement;
                    var remainingMovement = targetMovement - safeMovement;
                    var glideMovement = Line.GlideAlong(line, remainingMovement);
                    targetPosition = safePosition + glideMovement;
                    movementLine = new Line(entity.Position, targetPosition);
                }

                processed[i] = entity;
                processed[i].Position = targetPosition;
                processed[i].SequenceId = sequenceId;

                i++;
            }
            
            return processed;
        }    }
}