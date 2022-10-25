﻿using Xunit.Abstractions;
using Xunit;
using DeltGeometriFelleskomponent.TopologyImplementation;
using DeltGeometriFelleskomponent.Models;
using System.Collections.Generic;
using System.Linq;
using NetTopologySuite.Geometries;

namespace DeltGeometriFelleskomponent.Tests;

public class PolygonEditorTest : TestBase
{
    private readonly ITestOutputHelper output;
    const string id = "id";

    public PolygonEditorTest(ITestOutputHelper output)
    {
        // Capturing output in unit tests
        this.output = output;
    }

    [Fact]
    public void OneLineReferencedEditShellVertex()
    {
        //arrange        
        //build a polygon from a line
        var (polygon, lines) = GetPolygonFrom(new List<string>() { "8" });

        output.WriteLine($"original: {polygon.Geometry}");

        //move one of the vertices of the line and create a new polygon
        var ring = ((Polygon)polygon.Geometry).Shell.Copy().Coordinates;
        var oldVertex = ring[1];
        var newVertex = new Coordinate(oldVertex.X + 0.00001, oldVertex.Y);
        ring[1] = newVertex;
        var geometry = new Polygon(new LinearRing(ring));

        //act
        //ie: apply this change
        var result = PolygonEditor.EditPolygon(new EditPolygonRequest() { 
            Feature = NgisFeatureHelper.Copy(polygon), 
            AffectedFeatures = lines,
            EditedGeometry = geometry
        });

        //assert
        Assert.True(result.IsValid);

        var (editedFeature, editedPolygon) = GetEdited(result.AffectedFeatures);
        output.WriteLine($"edited:   {editedPolygon}");

        Assert.Equal(Operation.Replace, NgisFeatureHelper.GetOperation(editedFeature));
        Assert.Equal(id, NgisFeatureHelper.GetLokalId(editedFeature));

        Assert.Equal(((Polygon)polygon.Geometry).Shell.Coordinates.Length, editedPolygon.Shell.Coordinates.Length);
        Assert.Contains(editedPolygon.Shell.Coordinates, c => c.Equals(newVertex));
        Assert.DoesNotContain(editedPolygon.Shell.Coordinates, c => c.Equals(oldVertex));

        var editedLines = result.AffectedFeatures.Where(f => f.Update?.Action == Operation.Replace && f.Geometry.GeometryType == "LineString");
        Assert.Single(editedLines);

    }

    [Fact]
    public void OneLineReferencedDeleteShellVertex()
    {
        //arrange        
        //build a polygon from a line
        var (polygon, lines) = GetPolygonFrom(new List<string>() { "8" });

        output.WriteLine($"original: {polygon.Geometry}");

        //delete vertex at index 1  and create a new polygon
        var ring = ((Polygon)polygon.Geometry).Shell.Copy().Coordinates.ToList();
        var oldVertex = ring[1];
        ring.RemoveAt(1);
        var geometry = new Polygon(new LinearRing(ring.ToArray()));

        //act
        //ie: apply this change
        var result = PolygonEditor.EditPolygon(new EditPolygonRequest()
        {
            Feature = NgisFeatureHelper.Copy(polygon),
            AffectedFeatures = lines,
            EditedGeometry = geometry
        });

        //assert
        Assert.True(result.IsValid);
        var (editedFeature, editedPolygon) = GetEdited(result.AffectedFeatures);

        Assert.Equal(Operation.Replace, NgisFeatureHelper.GetOperation(editedFeature));
        Assert.Equal(id, NgisFeatureHelper.GetLokalId(editedFeature));

        output.WriteLine($"edited:   {editedPolygon}");
        Assert.Equal(((Polygon)polygon.Geometry).Shell.Coordinates.Length - 1, editedPolygon.Shell.Coordinates.Length);
        Assert.DoesNotContain(editedPolygon.Shell.Coordinates, c => c.Equals(oldVertex));

        var editedLines = result.AffectedFeatures.Where(f => f.Update?.Action == Operation.Replace && f.Geometry.GeometryType == "LineString");
        Assert.Single(editedLines);
    }

    [Fact]
    public void OneLineReferencedAddShellVertex()
    {
        //arrange        
        //build a polygon from a line        
        var (polygon, lines) = GetPolygonFrom(new List<string>() { "8" });

        output.WriteLine($"original: {polygon.Geometry}");

        //insert a vertex at index = 1 and create a new polygon
        var ring = ((Polygon)polygon.Geometry).Shell.Copy().Coordinates.ToList();
        var oldVertex = ring[1];
        var newVertex = new Coordinate(oldVertex.X + 0.00001, oldVertex.Y + 0.00001);

        ring.Insert(1, newVertex);
        var geometry = new Polygon(new LinearRing(ring.ToArray()));
        //act
        //ie: apply this change
        var result = PolygonEditor.EditPolygon(new EditPolygonRequest()
        {
            Feature = NgisFeatureHelper.Copy(polygon),
            AffectedFeatures = lines,
            EditedGeometry = geometry
        });

        //assert
        Assert.True(result.IsValid);

        var (editedFeature, editedPolygon) = GetEdited(result.AffectedFeatures);
        Assert.Equal(Operation.Replace, NgisFeatureHelper.GetOperation(editedFeature));
        Assert.Equal(id, NgisFeatureHelper.GetLokalId(editedFeature));

        Assert.Equal(((Polygon)polygon.Geometry).Shell.Coordinates.Length + 1, editedPolygon.Shell.Coordinates.Length);
        Assert.Contains(editedPolygon.Shell.Coordinates, c => c.Equals(newVertex));
        Assert.Contains(editedPolygon.Shell.Coordinates, c => c.Equals(oldVertex));

        var editedLines = result.AffectedFeatures.Where(f => f.Update?.Action == Operation.Replace && f.Geometry.GeometryType == "LineString");
        Assert.Single(editedLines);
    }

    [Fact]
    public void TwoLinesReferencedEditShellVertex()
    {
        //arrange        
        //build a polygon from a line
        var (polygon, lines) = GetPolygonFrom(new List<string>() { "1", "2" });

        output.WriteLine($"original: {polygon.Geometry}");
        foreach (var line in  lines)
        {
            output.WriteLine($"line {NgisFeatureHelper.GetLokalId(line)}: {line.Geometry}");
        }

        //move one of the vertices of the line and create a new polygon
        var ring = ((Polygon)polygon.Geometry).Shell.Copy().Coordinates;
        var oldVertex = ring[1];
        var newVertex = new Coordinate(oldVertex.X + 0.0001, oldVertex.Y);
        ring[1] = newVertex; 
        var geometry = new Polygon(new LinearRing(ring));

        //act
        //ie: apply this change
        var result = PolygonEditor.EditPolygon(new EditPolygonRequest()
        {
            Feature = NgisFeatureHelper.Copy(polygon),
            AffectedFeatures = lines,
            EditedGeometry = geometry
        });

        //assert

        Assert.True(result.IsValid);

        var (editedFeature, editedPolygon) = GetEdited(result.AffectedFeatures);
        output.WriteLine($"edited:   {editedPolygon}");

        Assert.Equal(Operation.Replace, NgisFeatureHelper.GetOperation(editedFeature));
        Assert.Equal(id, NgisFeatureHelper.GetLokalId(editedFeature));

        Assert.Equal(((Polygon)polygon.Geometry).Shell.Coordinates.Length, editedPolygon.Shell.Coordinates.Length);

        Assert.Contains(editedPolygon.Shell.Coordinates, c => c.Equals(newVertex));
        Assert.DoesNotContain(editedPolygon.Shell.Coordinates, c => c.Equals(oldVertex));

        var editedLines = result.AffectedFeatures.Where(f => f.Update?.Action == Operation.Replace && f.Geometry.GeometryType == "LineString");
        Assert.Single(editedLines);
    }

    [Fact]
    public void TwoLinesReferencedEditFirstShellVertex()
    {
        //arrange        
        //build a polygon from a line
        var (polygon, lines) = GetPolygonFrom(new List<string>() { "1", "2" });

        output.WriteLine($"original: {polygon.Geometry}");
        foreach (var line in lines)
        {
            output.WriteLine($"line {NgisFeatureHelper.GetLokalId(line)}: {line.Geometry}");
        }

        //move one of the vertices of the line and create a new polygon
        var ring = ((Polygon)polygon.Geometry).Shell.Copy().Coordinates;
        var oldVertex = ring[0];
        var newVertex = new Coordinate(oldVertex.X + 0.0001, oldVertex.Y);
        ring[0] = newVertex;
        ring[ring.Length - 1] = newVertex;
        var geometry = new Polygon(new LinearRing(ring));

        //act
        //ie: apply this change
        var result = PolygonEditor.EditPolygon(new EditPolygonRequest()
        {
            Feature = NgisFeatureHelper.Copy(polygon),
            AffectedFeatures = lines,
            EditedGeometry = geometry
        });

        //assert

        Assert.True(result.IsValid);

        var (editedFeature, editedPolygon) = GetEdited(result.AffectedFeatures);
        output.WriteLine($"edited:   {editedPolygon}");
        Assert.Equal(Operation.Replace, NgisFeatureHelper.GetOperation(editedFeature));
        Assert.Equal(id, NgisFeatureHelper.GetLokalId(editedFeature));
        Assert.Equal(((Polygon)polygon.Geometry).Shell.Coordinates.Length, editedPolygon.Shell.Coordinates.Length);

        Assert.Contains(editedPolygon.Shell.Coordinates, c => c.Equals(newVertex));
        Assert.DoesNotContain(editedPolygon.Shell.Coordinates, c => c.Equals(oldVertex));

        var editedLines = result.AffectedFeatures.Where(f => f.Update?.Action == Operation.Replace && f.Geometry.GeometryType == "LineString");
        Assert.Equal(2, editedLines.Count());
    }

    [Fact]
    public void TwoLinesReferencedDeleteShellVertex()
    {
        //arrange        
        //build a polygon from a line
        var (polygon, lines) = GetPolygonFrom(new List<string>() { "1", "2" });

        output.WriteLine($"original: {polygon.Geometry}");
        foreach (var line in lines)
        {
            output.WriteLine($"line {NgisFeatureHelper.GetLokalId(line)}: {line.Geometry}");
        }

        //move one of the vertices of the line and create a new polygon
        var ring = ((Polygon)polygon.Geometry).Shell.Copy().Coordinates.ToList(); 
        var oldVertex = ring[1];
        ring.RemoveAt(1);
        var geometry = new Polygon(new LinearRing(ring.ToArray()));

        //act
        //ie: apply this change
        var result = PolygonEditor.EditPolygon(new EditPolygonRequest()
        {
            Feature = NgisFeatureHelper.Copy(polygon),
            AffectedFeatures = lines,
            EditedGeometry = geometry
        });

        //assert

        Assert.True(result.IsValid);

        var (editedFeature, editedPolygon) = GetEdited(result.AffectedFeatures);
        output.WriteLine($"edited:   {editedPolygon}");
        Assert.Equal(Operation.Replace, NgisFeatureHelper.GetOperation(editedFeature));
        Assert.Equal(id, NgisFeatureHelper.GetLokalId(editedFeature));

        Assert.Equal(((Polygon)polygon.Geometry).Shell.Coordinates.Length - 1, editedPolygon.Shell.Coordinates.Length);

        Assert.DoesNotContain(editedPolygon.Shell.Coordinates, c => c.Equals(oldVertex));

        var editedLines = result.AffectedFeatures.Where(f => f.Update?.Action == Operation.Replace && f.Geometry.GeometryType == "LineString");
        Assert.Single(editedLines);
    }

    [Fact]
    public void TwoLinesReferencedAddShellVertex()
    {
        //arrange        
        //build a polygon from a line
        var (polygon, lines) = GetPolygonFrom(new List<string>() { "1", "2" });

        output.WriteLine($"original: {polygon.Geometry}");
        foreach (var line in lines)
        {
            output.WriteLine($"line {NgisFeatureHelper.GetLokalId(line)}: {line.Geometry}");
        }

        //move one of the vertices of the line and create a new polygon
        var ring = ((Polygon)polygon.Geometry).Shell.Copy().Coordinates.ToList();
        var oldVertex = ring[1];
        var newVertex = new Coordinate(oldVertex.X + 0.00001, oldVertex.Y + 0.00001);
        ring.Insert(1, newVertex);
        var geometry = new Polygon(new LinearRing(ring.ToArray()));

        //act
        //ie: apply this change
        var result = PolygonEditor.EditPolygon(new EditPolygonRequest()
        {
            Feature = NgisFeatureHelper.Copy(polygon),
            AffectedFeatures = lines,
            EditedGeometry = geometry
        });

        //assert

        Assert.True(result.IsValid);

        var (editedFeature, editedPolygon) = GetEdited(result.AffectedFeatures);
        output.WriteLine($"edited:   {editedPolygon}");
        Assert.Equal(Operation.Replace, NgisFeatureHelper.GetOperation(editedFeature));
        Assert.Equal(id, NgisFeatureHelper.GetLokalId(editedFeature));
        Assert.Equal(((Polygon)polygon.Geometry).Shell.Coordinates.Length + 1, editedPolygon.Shell.Coordinates.Length);

        Assert.Contains(editedPolygon.Shell.Coordinates, c => c.Equals(oldVertex));
        Assert.Contains(editedPolygon.Shell.Coordinates, c => c.Equals(newVertex));

        var editedLines = result.AffectedFeatures.Where(f => f.Update?.Action == Operation.Replace && f.Geometry.GeometryType == "LineString");
        Assert.Equal(2, editedLines.Count());
    }

    [Fact]
    public void AddsCwHoleToPolygonWithoutHoles()
    {
        //arrange        
        //build a polygon from a line
        var (polygon, lines) = GetPolygonFrom(new List<string>() { "1", "2" });

        var holeLine = GetExampleFeature("8").Geometry.Coordinates;

        output.WriteLine($"original: {polygon.Geometry}");
        foreach (var line in lines)
        {
            output.WriteLine($"line {NgisFeatureHelper.GetLokalId(line)}: {line.Geometry}");
        }

        //move one of the vertices of the line and create a new polygon
        var shell = ((Polygon)polygon.Geometry).Shell;
        var hole = new LinearRing(holeLine);

        var geometry = new Polygon(shell, new LinearRing[] { hole });
        output.WriteLine($"geometry:   {geometry}");
        //act
        //ie: apply this change
        var result = PolygonEditor.EditPolygon(new EditPolygonRequest()
        {
            Feature = NgisFeatureHelper.Copy(polygon),
            AffectedFeatures = lines,
            EditedGeometry = geometry
        });

        //assert

        Assert.True(result.IsValid);


        var(editedFeature, editedPolygon) = GetEdited(result.AffectedFeatures);
        output.WriteLine($"edited:   {editedPolygon}");

        var createdLines = result.AffectedFeatures.Where(f => f.Update?.Action == Operation.Create);
        Assert.Single(createdLines);

        Assert.Equal(id, NgisFeatureHelper.GetLokalId(editedFeature));
        Assert.Equal(Operation.Replace, editedFeature.Update.Action);

        Assert.True(geometry.Equals(editedPolygon));
        Assert.False(editedPolygon.Holes[0].IsCCW);
        Assert.Equal(NgisFeatureHelper.GetLokalId(createdLines.First()), NgisFeatureHelper.GetInteriors(editedFeature)[0][0]);
    }

    [Fact]
    public void AddsCwHoleToPolygonWithHole()
    {
        //arrange        
        //build a polygon from a line
        var (polygon, lines) = GetPolygonFrom(new List<string>() { "1", "2", "9" });

        var holeLine = GetExampleFeature("8").Geometry.Coordinates;

        output.WriteLine($"original: {polygon.Geometry}");
        foreach (var line in lines)
        {
            output.WriteLine($"line {NgisFeatureHelper.GetLokalId(line)}: {line.Geometry}");
        }

        //move one of the vertices of the line and create a new polygon
        var shell = ((Polygon)polygon.Geometry).Shell;

        var hole = new LinearRing(holeLine);
        var holes = ((Polygon)polygon.Geometry).Holes.ToList();
        holes.Add(hole);

        var geometry = new Polygon(shell, holes.ToArray());
        output.WriteLine($"geometry:   {geometry}");
        //act
        //ie: apply this change
        var result = PolygonEditor.EditPolygon(new EditPolygonRequest()
        {
            Feature = NgisFeatureHelper.Copy(polygon),
            AffectedFeatures = lines,
            EditedGeometry = geometry
        });

        //assert

        Assert.True(result.IsValid);


        var (editedFeature, editedPolygon) = GetEdited(result.AffectedFeatures);
        output.WriteLine($"edited:   {editedPolygon}");

        var createdLines = result.AffectedFeatures.Where(f => f.Update?.Action == Operation.Create);
        Assert.Single(createdLines);

        Assert.Equal(id, NgisFeatureHelper.GetLokalId(editedFeature));
        Assert.Equal(Operation.Replace, editedFeature.Update.Action);

        Assert.True(geometry.Equals(editedPolygon));
        Assert.False(editedPolygon.Holes[0].IsCCW);
        Assert.Equal(NgisFeatureHelper.GetLokalId(createdLines.First()), NgisFeatureHelper.GetInteriors(editedFeature)[1][0]);
    }

    [Fact]
    public void AddsCcwHoleToPolygonWithoutHoles()
    {
        //arrange        
        //build a polygon from a line
        var (polygon, lines) = GetPolygonFrom(new List<string>() { "1", "2" });

        var holeLine = GetExampleFeature("9").Geometry.Coordinates;

        output.WriteLine($"original: {polygon.Geometry}");
        foreach (var line in lines)
        {
            output.WriteLine($"line {NgisFeatureHelper.GetLokalId(line)}: {line.Geometry}");
        }

        //move one of the vertices of the line and create a new polygon
        var shell = ((Polygon)polygon.Geometry).Shell;
        var hole = new LinearRing(holeLine);

        var geometry = new Polygon(shell, new LinearRing[] { hole });
        output.WriteLine($"geometry:   {geometry}");
        //act
        //ie: apply this change
        var result = PolygonEditor.EditPolygon(new EditPolygonRequest()
        {
            Feature = NgisFeatureHelper.Copy(polygon),
            AffectedFeatures = lines,
            EditedGeometry = (Polygon) geometry.Copy()
        });

        //assert

        Assert.True(result.IsValid);


        var (editedFeature, editedPolygon) = GetEdited(result.AffectedFeatures);
        output.WriteLine($"edited:   {editedPolygon}");

        var createdLines = result.AffectedFeatures.Where(f => f.Update?.Action == Operation.Create);
        Assert.Single(createdLines);

        Assert.Equal(id, NgisFeatureHelper.GetLokalId(editedFeature));
        Assert.Equal(Operation.Replace, editedFeature.Update.Action);

        Assert.True(geometry.Equals(editedPolygon));
        Assert.False(editedPolygon.Holes[0].IsCCW);
        Assert.Equal($"-{NgisFeatureHelper.GetLokalId(createdLines.First())}", NgisFeatureHelper.GetInteriors(editedFeature)[0][0]);
    }

    [Fact]
    public void RemovesHoleFromPolygonWithOneHole()
    {
        //arrange        
        //build a polygon from a line
        var (polygon, lines) = GetPolygonFrom(new List<string>() { "1", "2", "8" });

        

        output.WriteLine($"original: {polygon.Geometry}");
        foreach (var line in lines)
        {
            output.WriteLine($"line {NgisFeatureHelper.GetLokalId(line)}: {line.Geometry}");
        }

        //move one of the vertices of the line and create a new polygon
        var shell = ((Polygon)polygon.Geometry).Shell;
        

        var geometry = new Polygon(shell, new LinearRing[] {  });
        output.WriteLine($"geometry:   {geometry}");
        //act
        //ie: apply this change
        var result = PolygonEditor.EditPolygon(new EditPolygonRequest()
        {
            Feature = NgisFeatureHelper.Copy(polygon),
            AffectedFeatures = lines,
            EditedGeometry = geometry
        });

        //assert

        Assert.True(result.IsValid);


        var (editedFeature, editedPolygon) = GetEdited(result.AffectedFeatures);
        output.WriteLine($"edited:   {editedPolygon}");

        var createdLines = result.AffectedFeatures.Where(f => f.Update?.Action == Operation.Create);
        Assert.Empty(createdLines);

        Assert.Equal(id, NgisFeatureHelper.GetLokalId(editedFeature));
        Assert.Equal(Operation.Replace, editedFeature.Update.Action);

        Assert.True(geometry.Equals(editedPolygon));
        Assert.Empty(NgisFeatureHelper.GetInteriors(editedFeature));
    }

    [Fact]
    public void RemovesHoleFromPolygonWithTwoHoles()
    {
        //arrange        
        //build a polygon from a line
        var (polygon, lines) = GetPolygonFrom(new List<string>() { "1", "2", "8", "9" });



        output.WriteLine($"original: {polygon.Geometry}");
        foreach (var line in lines)
        {
            output.WriteLine($"line {NgisFeatureHelper.GetLokalId(line)}: {line.Geometry}");
        }

        //move one of the vertices of the line and create a new polygon
        var shell = ((Polygon)polygon.Geometry).Shell;

        var holes = ((Polygon)polygon.Geometry).Holes.ToList();
        var geometry = new Polygon(shell, new LinearRing[] {holes.First() });
        output.WriteLine($"geometry:   {geometry}");
        //act
        //ie: apply this change
        var result = PolygonEditor.EditPolygon(new EditPolygonRequest()
        {
            Feature = NgisFeatureHelper.Copy(polygon),
            AffectedFeatures = lines,
            EditedGeometry = geometry
        });

        //assert

        Assert.True(result.IsValid);


        var (editedFeature, editedPolygon) = GetEdited(result.AffectedFeatures);
        output.WriteLine($"edited:   {editedPolygon}");

        var createdLines = result.AffectedFeatures.Where(f => f.Update?.Action == Operation.Create);
        Assert.Empty(createdLines);

        Assert.Equal(id, NgisFeatureHelper.GetLokalId(editedFeature));
        Assert.Equal(Operation.Replace, editedFeature.Update.Action);

        Assert.True(geometry.Equals(editedPolygon));
        Assert.Single(NgisFeatureHelper.GetInteriors(editedFeature));
    }

    [Fact]
    public void EditsHoleInPolygonWithSingleHole()
    {
        //arrange        
        //build a polygon from a line
        var (polygon, lines) = GetPolygonFrom(new List<string>() {"1", "2", "8" });

        output.WriteLine($"original: {polygon.Geometry}");

        //move one of the vertices of the line and create a new polygon
        var shell = ((Polygon)polygon.Geometry).Shell;
        var holes = ((Polygon)polygon.Geometry).Holes;

        var editedHole = ((Polygon)polygon.Geometry).Holes[0].Copy().Coordinates;
        var oldVertex = editedHole[1];
        var newVertex = new Coordinate(oldVertex.X + 0.00001, oldVertex.Y);
        editedHole[1] = newVertex;
        var geometry = new Polygon(shell, new LinearRing[] { new LinearRing(editedHole) });

        //act
        //ie: apply this change
        var result = PolygonEditor.EditPolygon(new EditPolygonRequest()
        {
            Feature = NgisFeatureHelper.Copy(polygon),
            AffectedFeatures = lines,
            EditedGeometry = geometry
        });

        //assert
        Assert.True(result.IsValid);

        var (editedFeature, editedPolygon) = GetEdited(result.AffectedFeatures);
        output.WriteLine($"edited:   {editedPolygon}");

        Assert.Equal(Operation.Replace, NgisFeatureHelper.GetOperation(editedFeature));
        Assert.Equal(id, NgisFeatureHelper.GetLokalId(editedFeature));

        Assert.False(editedPolygon.Holes[0].IsCCW);
        Assert.Equal(((Polygon)polygon.Geometry).Holes[0].Coordinates.Length, editedPolygon.Holes[0].Coordinates.Length);
        Assert.Contains(editedPolygon.Holes[0].Coordinates, c => c.Equals(newVertex));
        Assert.DoesNotContain(editedPolygon.Holes[0].Coordinates, c => c.Equals(oldVertex));


        var editedLines = result.AffectedFeatures.Where(f => f.Update?.Action == Operation.Replace && f.Geometry.GeometryType == "LineString");
        Assert.Single(editedLines);

    }

    private (NgisFeature, Polygon) GetEdited(List<NgisFeature> affectedFeatures)
    {
        var editedFeature = affectedFeatures.FirstOrDefault(f => f.Geometry.GeometryType == "Polygon");
        var editedPolygon = (Polygon)editedFeature.Geometry;
        return (editedFeature, editedPolygon);
    }

    private (NgisFeature, List<NgisFeature>) GetPolygonFrom(List<string> ids)
    {
        var lines = ids.Select(GetExampleFeature).Select(l => {
            l.Update = null;
            return l;
        }).ToList();
        var res = new PolygonCreator().CreatePolygonFromLines(lines.ToList(), null);
        var polygon = res.First().AffectedFeatures.First(f => f.Geometry.GeometryType == "Polygon");
        polygon.Update = null;
        NgisFeatureHelper.SetLokalId(polygon, id);
        return (polygon, lines);

    }
}
