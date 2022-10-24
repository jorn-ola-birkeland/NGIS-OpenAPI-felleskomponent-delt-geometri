﻿using DeltGeometriFelleskomponent.Models;
using NetTopologySuite.Geometries;

namespace DeltGeometriFelleskomponent.TopologyImplementation;

public static class PolygonEditor
{
    public static TopologyResponse EditPolygon(EditPolygonRequest request)
    {

        if (request.Feature.Geometry.GeometryType != "Polygon")
        {
            throw new Exception("Can only edit polygons");
        }
        var oldPolygon = (Polygon)request.Feature.Geometry;
        var newPolygon = request.EditedGeometry; ;

        var pairs = GetPairs(oldPolygon.Shell, newPolygon.Shell);
        var lines = GetShellFeatures(request.Feature, request.AffectedFeatures);
        var edits = ToEdits(pairs, lines, request.Feature).OfType<EditLineRequest>();


        if (edits.Count() == 1)
        {
            return LineEditor.EditLine(edits.First());
        }


        throw new NotImplementedException();
    }


    private static IEnumerable<EditLineRequest> ToEdits(IEnumerable<Pair> pairs, IEnumerable<NgisFeature> referencedFeatures, NgisFeature editedPolygonFeature)
    {
        var res = new List<EditLineRequest>();
        foreach (var pair in pairs)
        {
            var f = referencedFeatures.FirstOrDefault(f => f.Geometry.Coordinates.Any(c2 => c2.Equals(pair.oldCoord)));
            if (f != null) { 
                var edit = ToEdit(pair, f);
                edit.AffectedFeatures = new List<NgisFeature>() { editedPolygonFeature }.Concat( referencedFeatures.Where(f => NgisFeatureHelper.GetLokalId(f) != NgisFeatureHelper.GetLokalId(f))).ToList();
                res.Add(edit);
            }
        }
        return res;
    }
     

    private static IEnumerable<NgisFeature> GetShellFeatures(NgisFeature feature, List<NgisFeature> affectedFeatures)
    {
        var exteriors = NgisFeatureHelper.GetExteriors(feature).Select(NgisFeatureHelper.RemoveSign);
        return affectedFeatures.FindAll(f => exteriors.Any(id => id == NgisFeatureHelper.GetLokalId(f)));
    }

    private static EditLineRequest? ToEdit(Pair pair, NgisFeature feature)
    {
        if (pair.newCoord != null && pair.oldCoord != null) { 

            return new EditLineRequest()
            {
                Feature = feature,
                Edit = new EditLineOperation()
                {
                    Operation = EditOperation.Edit,
                    NodeValue = new List<double>() { pair.newCoord.X, pair.newCoord.Y },
                    NodeIndex = Array.FindIndex(feature.Geometry.Coordinates, c => c.Equals(pair.oldCoord))
                }
            };
        }
        if (pair.newCoord == null && pair.oldCoord != null)
        {
            return new EditLineRequest()
            {
                Feature = feature,
                Edit = new EditLineOperation()
                {
                    Operation = EditOperation.Delete,                    
                    NodeIndex = Array.FindIndex(feature.Geometry.Coordinates, c => c.Equals(pair.oldCoord))
                }
            };
        }
        return null;
    }

    private static IEnumerable<Coordinate> GetCoordsNotIn(LinearRing a, LinearRing b) 
        => a.Coordinates.Where(c => !b.Coordinates.Any(c2 => c.Equals(c2)));

    private static IEnumerable<Pair> GetPairs (LinearRing oldRing, LinearRing newRing)
    {
        var deletedPoints = GetCoordsNotIn(oldRing, newRing);
        var newPoints = GetCoordsNotIn(newRing, oldRing);

        var pairs = new List<Pair>();
        
            foreach (var deletedPoint in deletedPoints)
            {
                pairs.Add(new Pair()
                {
                    oldCoord = deletedPoint,
                    newCoord = GetClosest(newPoints, deletedPoint),
                });
            }

        
        return pairs;
    }

    private static Coordinate? GetClosest (IEnumerable<Coordinate> points, Coordinate point)
    {
        var res = points.Select(p => (p.Distance(point), p));
        return res.Count() > 0 ? res.Min().Item2 : null;

    }

    internal class Pair {
        public Coordinate? oldCoord { get; set; }
        public Coordinate? newCoord { get; set; }
    }

    
    
}


