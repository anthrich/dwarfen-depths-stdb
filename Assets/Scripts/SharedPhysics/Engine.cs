using System;
using System.Collections.Generic;
using System.Linq;

namespace SharedPhysics
{
    public static class Engine
    {
        [ThreadStatic] private static List<Line>? _nearbyLinesBuffer;
        private const float Gravity = -27.62f;
        public const float JumpImpulse = 8f;
        private const float MaxSlopeAngle = 60f;
        private const float GroundSnapDistance = 0.1f;
        private const float TerminalVelocity = -50f;

        private readonly struct GroundContact
        {
            public readonly float? Height;       // null = off mesh
            public readonly float SnapDistance;

            public GroundContact(float? height, float snapDistance)
            {
                Height = height;
                SnapDistance = snapDistance;
            }
        }

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

        private static Vector2 ProjectMovementOntoSurface(
            Vector2 normalizedDirectionXz, float surfaceSpeed, Triangle triangle, float maxSlopeAngle)
        {
            var normal = Triangle.GetNormal(triangle);
            var slopeAngle = Triangle.GetSlopeAngle(triangle);

            var direction3D = new Vector3(normalizedDirectionXz.X, 0, normalizedDirectionXz.Y);

            var projected = direction3D - normal * Vector3.Dot(direction3D, normal);
            var projectedMag = projected.GetMagnitude();

            if (projectedMag < 0.0001f)
                return Vector2.Zero;

            var surfaceDir = projected / projectedMag;

            if (slopeAngle > maxSlopeAngle && surfaceDir.Y > 0.001f)
            {
                var contour = Vector3.Cross(normal, Vector3.Up).Normalized();
                var contourDot = Vector3.Dot(surfaceDir, contour);
                if (MathF.Abs(contourDot) < 0.0001f)
                    return Vector2.Zero;
                // contourDot naturally scales the speed: walking straight into
                // the slope yields ~0, walking along it yields ~1
                var movement3D = contour * (contourDot * surfaceSpeed);
                return new Vector2(movement3D.X, movement3D.Z);
            }

            // Use the surface projection for speed (accounts for slope steepness)
            // but preserve the original XZ direction to prevent drift on slopes
            var movement3D2 = surfaceDir * surfaceSpeed;
            float xzSpeed = new Vector2(movement3D2.X, movement3D2.Z).GetMagnitude();
            return normalizedDirectionXz * xzSpeed;
        }

        private static Vector2 ComputeXzMovement(
            Vector2 normalizedDirection, float surfaceSpeed,
            Triangle? currentTriangle, bool isGrounded, bool hasTerrain, float deltaTime)
        {
            if (!hasTerrain)
                return normalizedDirection * surfaceSpeed;

            Vector2 targetMovement;
            if (currentTriangle.HasValue && normalizedDirection.SqrMagnitude > 0.0001f)
            {
                targetMovement = ProjectMovementOntoSurface(
                    normalizedDirection, surfaceSpeed, currentTriangle.Value, MaxSlopeAngle);
            }
            else
            {
                targetMovement = normalizedDirection * surfaceSpeed;
            }

            // Gravity-based slide on slopes exceeding max angle
            if (currentTriangle.HasValue && isGrounded &&
                Triangle.GetSlopeAngle(currentTriangle.Value) > MaxSlopeAngle)
            {
                var normal = Triangle.GetNormal(currentTriangle.Value);
                var down = new Vector3(0, -1, 0);
                var gravityOnSurface = down - normal * Vector3.Dot(down, normal);
                targetMovement += new Vector2(gravityOnSurface.X, gravityOnSurface.Z) * MathF.Abs(Gravity) * deltaTime;
            }

            return targetMovement;
        }

        private static Vector2 ApplyWallCollisions(
            Vector2 currentPositionXz, Vector2 targetPositionXz,
            LineGrid lineGrid, List<Line> nearbyLinesBuffer)
        {
            var targetMovement = targetPositionXz - currentPositionXz;
            var movementLine = new Line(currentPositionXz, targetPositionXz);
            lineGrid.GetNearbyLines(BoundingBox.FromLine(movementLine), nearbyLinesBuffer);

            foreach (var line in nearbyLinesBuffer)
            {
                var intersection = GetIntersection(movementLine, line);
                if (!intersection.HasValue) continue;
                var safeDistance = (intersection.Value - currentPositionXz).Normalized() * 0.05f;
                var safeMovement = intersection.Value - safeDistance - currentPositionXz;
                var safePosition = currentPositionXz + safeMovement;
                var remainingMovement = targetMovement - safeMovement;
                var glideMovement = Line.GlideAlong(line, remainingMovement);
                targetPositionXz = safePosition + glideMovement;
                movementLine = new Line(currentPositionXz, targetPositionXz);
            }

            return targetPositionXz;
        }

        private static GroundContact ResolveGroundContact(
            ITerrain terrain, Vector2 currentPositionXz, float currentY,
            Vector2 targetPositionXz, Triangle? currentTriangle)
        {
            var height = terrain.GetGroundHeight(targetPositionXz);

            // Dynamic snap distance: on slopes, the per-tick height change can far exceed
            // GroundSnapDistance. Use the current slope angle to compute the expected height
            // drop, so smooth downhill movement stays grounded while actual cliffs (where the
            // current surface is flat) still trigger airborne.
            float snapDistance = GroundSnapDistance;
            if (height.HasValue && currentTriangle.HasValue)
            {
                float heightDifference = currentY - height.Value;
                if (heightDifference > 0)
                {
                    float slopeRad = Triangle.GetSlopeAngle(currentTriangle.Value) * MathF.PI / 180f;
                    float xzDist = (targetPositionXz - currentPositionXz).GetMagnitude();
                    snapDistance = MathF.Max(GroundSnapDistance, xzDist * MathF.Tan(slopeRad) + GroundSnapDistance);
                }
            }

            return new GroundContact(height, snapDistance);
        }

        private static (Vector3 position, bool isGrounded, float verticalVelocity) ComputeVerticalPosition(
            Entity entity, Vector2 targetPositionXz, GroundContact ground, float deltaTime)
        {
            if (entity.IsGrounded)
            {
                if (!ground.Height.HasValue) return (Vector3.FromXz(targetPositionXz, entity.Position.Y), false, 0);
                var heightDifference = entity.Position.Y - ground.Height.Value;
                return heightDifference > ground.SnapDistance ?
                    // Walking off a cliff - become airborne
                    (Vector3.FromXz(targetPositionXz, entity.Position.Y), false, 0) :
                    // Normal ground following
                    (Vector3.FromXz(targetPositionXz, ground.Height.Value), true, 0);

                // Off-mesh while grounded: become airborne at current height
            }

            // Airborne: apply gravity
            var verticalVelocity = entity.VerticalVelocity + Gravity * deltaTime;
            verticalVelocity = Math.Max(verticalVelocity, TerminalVelocity);
            var newY = entity.Position.Y + verticalVelocity * deltaTime;

            if (ground.Height.HasValue && newY <= ground.Height.Value)
            {
                // Landing
                return (Vector3.FromXz(targetPositionXz, ground.Height.Value), true, 0);
            }

            return (Vector3.FromXz(targetPositionXz, newY), false, verticalVelocity);
        }

        public static Entity[] Simulate(
            float deltaTime, ulong sequenceId, Entity[] entities, Line[] lines)
        {
            return Simulate(deltaTime, sequenceId, entities, new LineGrid(lines), null);
        }

        public static Entity[] Simulate(
            float deltaTime, ulong sequenceId, Entity[] entities, Line[] lines, ITerrain? terrain)
        {
            return Simulate(deltaTime, sequenceId, entities, new LineGrid(lines), terrain);
        }

        public static Entity[] Simulate(
            float deltaTime, ulong sequenceId, Entity[] entities, LineGrid lineGrid, ITerrain? terrain)
        {
            var processed = new Entity[entities.Length];
            var i = 0;
            _nearbyLinesBuffer ??= new List<Line>();

            foreach (var entity in entities)
            {
                var forwardDirection = Entity.GetForwardDirection(entity);
                var normalizedDirection = entity.Direction.Normalized();
                var isBackpedalling = Vector2.Dot(normalizedDirection, forwardDirection) < -0.01f;
                var surfaceSpeed = entity.Speed * (isBackpedalling ? 0.5f : 1f) * deltaTime;

                var currentPositionXz = entity.Position.ToXz();
                var currentTriangle = terrain?.GetTriangle(currentPositionXz);

                var targetMovement = ComputeXzMovement(
                    normalizedDirection, surfaceSpeed, currentTriangle, entity.IsGrounded, terrain != null, deltaTime);
                var targetPositionXz = ApplyWallCollisions(
                    currentPositionXz, currentPositionXz + targetMovement, lineGrid, _nearbyLinesBuffer);

                processed[i] = entity;
                processed[i].SequenceId = sequenceId;

                if (terrain != null)
                {
                    var ground = ResolveGroundContact(
                        terrain, currentPositionXz, entity.Position.Y, targetPositionXz, currentTriangle);
                    var (pos, grounded, vel) = ComputeVerticalPosition(entity, targetPositionXz, ground, deltaTime);
                    processed[i].Position = pos;
                    processed[i].IsGrounded = grounded;
                    processed[i].VerticalVelocity = vel;
                }
                else
                {
                    // Legacy 2D mode: position stays as Vector2-equivalent
                    processed[i].Position = Vector3.FromXz(targetPositionXz, entity.Position.Y);
                }

                i++;
            }

            return processed;
        }
    }
}
