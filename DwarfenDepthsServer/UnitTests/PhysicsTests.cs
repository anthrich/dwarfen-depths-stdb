using SharedPhysics;

namespace UnitTests;

public class PhysicsTests
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
}