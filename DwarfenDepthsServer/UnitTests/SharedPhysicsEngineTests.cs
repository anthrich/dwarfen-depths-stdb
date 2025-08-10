using SharedPhysics;

namespace UnitTests;

public class SharedPhysicsEngineTests
{
    [Fact]
    public void Line_intersection_is_detected_correctly()
    {
        // Arrange
        var line1 = new Line { Start = new Vector2(2, 4), End = new Vector2(0, 6)};
        var line2 = new Line { Start = new Vector2(1, 1), End = new Vector2(1, 10)};

        // Act
        var intersection = Engine.GetIntersection(line1, line2);
        
        // Assert
        Assert.Equal(new Vector2(1, 5), intersection);
    }

    [Fact]
    public void Line_nearness_is_detected_correctly()
    {
        // Arrange
        var line1 = new Line { Start = new Vector2(2, 4), End = new Vector2(0, 6)};
        var nearLine = new Line { Start = new Vector2(1, 1), End = new Vector2(1, 10)};
        
        // Act
        var nearbyLines = Engine.GetNearbyLines(line1, [nearLine]);
        
        // Assert
        Assert.Equal([nearLine], nearbyLines);
    }
    
    [Fact]
    public void Far_lines_are_detected_correctly()
    {
        // Arrange
        var line1 = new Line { Start = new Vector2(2, 4), End = new Vector2(0, 6)};
        var nearLine = new Line { Start = new Vector2(1, 1), End = new Vector2(1, 10)};
        var farLine = new Line { Start = new Vector2(1, 10), End = new Vector2(10, 10)};
        
        // Act
        var nearbyLines = Engine.GetNearbyLines(line1, [nearLine, farLine]);
        
        // Assert
        Assert.Equal([nearLine], nearbyLines);
    }

    [Fact]
    public void Simulating_moves_entities()
    {
        // Arrange
        var entities = new Entity[]
        {
            new()
            {
                Id = 1,
                Position = new Vector2(1, 1),
                SequenceId = 1,
                Direction = new Vector2(1, 1).Normalized(),
                Speed = 10f
            }
        };
        const float deltaTime = 0.05f;
        
        // Act
        var result = Engine.Simulate(deltaTime, 2, entities, []);
        
        // Assert
        Assert.Equal(
            new Vector2
            {
                X = 1.3535533905932737622004221810524f,
                Y = 1.3535533905932737622004221810524f
            },
            result[0].Position
        );
    }
    
    [Fact]
    public void Simulating_glides_entities_along_lines()
    {
        // Arrange
        var entities = new Entity[]
        {
            new()
            {
                Id = 1,
                Position = new Vector2(2, 4),
                SequenceId = 1,
                Direction = new Vector2(-2, 2).Normalized(),
                Speed = 2f
            }
        };

        var lines = new Line[]
        {
            new() { Start = new Vector2(1, 1), End = new Vector2(1, 10) },
        };
        
        const float deltaTime = 1f;
        
        // Act
        var result = Engine.Simulate(deltaTime, 2, entities, lines);
        
        // Assert
        Assert.Equal(
            new Vector2
            {
                X = 1.0353553f,
                Y = 5.4142137f
            },
            result[0].Position
        );
    }

    [Fact]
    public void Simulating_with_wall_glide_does_not_trap_entites_inside_walls()
    {
        // Arrange
        var entities = new Entity[]
        {
            new()
            {
                Id = 1,
                Position = new Vector2(2, 4),
                SequenceId = 1,
                Direction = new Vector2(-2, 2).Normalized(),
                Speed = 2f
            }
        };

        var lines = new Line[]
        {
            new() { Start = new Vector2(1, 1), End = new Vector2(1, 10) },
        };
        
        const float deltaTime = 1f;
        
        entities = Engine.Simulate(deltaTime, 2, entities, lines);
        entities[0].Direction = new Vector2(1, 0);
        
        // Act
        var result = Engine.Simulate(deltaTime, 2, entities, lines);
        
        // Assert
        Assert.True(result[0].Position.X > 1, "X position should not be stuck in the wall");
    }
}