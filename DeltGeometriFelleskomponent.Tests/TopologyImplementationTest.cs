using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using DeltGeometriFelleskomponent.Models;
using DeltGeometriFelleskomponent.TopologyImplementation;
using Microsoft.VisualStudio.TestPlatform.Utilities;
using NetTopologySuite.Algorithm;
using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Implementation;
using Xunit;
using Xunit.Abstractions;

namespace DeltGeometriFelleskomponent.Tests
{
    public class TopologyImplementationTest
    {
        private readonly ITopologyImplementation _topologyImplementation =
            new TopologyImplementation.TopologyImplementation();

        private readonly ITestOutputHelper output;

        public TopologyImplementationTest(ITestOutputHelper output)
        {
            // Capturing output in unit tests
            this.output = output;
        }

        [Fact]
        public void ReturnsPointWhenCreatingPoint()
        {
            var point = new Point(1, 2);
            var id = Guid.NewGuid().ToString();

            var res = _topologyImplementation.ResolveReferences(new ToplogyRequest()
            {
                Feature = NgisFeatureHelper.CreateFeature(point, id, Operation.Create)
                
            });

            Assert.Single(res.AffectedFeatures);
            var feature = res.AffectedFeatures.First();
            Assert.Equal(point,feature.Geometry);
            Assert.Equal(id, NgisFeatureHelper.GetLokalId(feature));
            Assert.Equal(Operation.Create, NgisFeatureHelper.GetOperation(feature));
        }

        [Fact]
        public void ReturnsLineAndPolygonWhenCreatingPolygonFromPolygon()
        {
            var linearRing = new LinearRing(new[]
            {
                new Coordinate(0, 0), 
                new Coordinate(0, 1), 
                new Coordinate(1, 1),
                new Coordinate(1, 0),
                new Coordinate(0, 0),
            });
            var polygon = new Polygon(linearRing);
            var id = Guid.NewGuid().ToString();
            
            var res = _topologyImplementation.ResolveReferences(new ToplogyRequest()
            {
                //Type = "Kaiomr�de",
                Feature = NgisFeatureHelper.CreateFeature(polygon, id, Operation.Create)
            });

            Assert.Equal(2, res.AffectedFeatures.Count());
            var feature1 = res.AffectedFeatures.First();

            Assert.Equal("Polygon", feature1.Geometry!.GeometryType);
            Assert.Equal(id, NgisFeatureHelper.GetLokalId(feature1));
            Assert.Equal(Operation.Create, NgisFeatureHelper.GetOperation(feature1));

            var feature2 = res.AffectedFeatures.ElementAt(1);

            Assert.Equal("LineString", feature2.Geometry!.GeometryType);
            Assert.Equal(Operation.Create, NgisFeatureHelper.GetOperation(feature2));

            var references = NgisFeatureHelper.GetExteriors(feature1);
            Assert.Single(references);
            Assert.Equal(references.First(), NgisFeatureHelper.GetLokalId(feature2));
        }


        [Fact]
        public void ReturnsLineAndPolygonWhenCreatingPolygonFromLine()
        {

            var id = Guid.NewGuid().ToString();

            var linearRing = new LinearRing(new[]
            {
                new Coordinate(0, 0),
                new Coordinate(0, 1),
                new Coordinate(1, 1),
                new Coordinate(1, 0),
                new Coordinate(0, 0),
            });

            var linestring = new LineString(linearRing.Coordinates);

            //Type = "Kaiomr�deGrense"
            var lineFeature = NgisFeatureHelper.CreateFeature(linestring, id, Operation.Create);

            //Type = "Kaiomr�de",
            var res = _topologyImplementation.ResolveReferences(new ToplogyRequest()
            {
                Feature = NgisFeatureHelper.CreateFeature(new Polygon(null), null, Operation.Create, new List<string>(){id}, new List<IEnumerable<string>>()),
                AffectedFeatures = new List<NgisFeature>() { lineFeature}
            });

            Assert.Equal(2, res.AffectedFeatures.Count());
            var feature1 = res.AffectedFeatures.First();

            Assert.Equal("LineString", feature1.Geometry!.GeometryType);
            Assert.Equal(id, NgisFeatureHelper.GetLokalId(feature1));
            Assert.Equal(Operation.Create, NgisFeatureHelper.GetOperation(feature1));

            var feature2 = res.AffectedFeatures.ElementAt(1);

            Assert.Equal("Polygon", feature2.Geometry!.GeometryType);
            Assert.Equal(Operation.Create, NgisFeatureHelper.GetOperation(feature2));

            var references = NgisFeatureHelper.GetExteriors(feature1);
            Assert.Single(references);
            Assert.Equal(references.First(), NgisFeatureHelper.GetLokalId(feature2));
        }

        [Fact]
        public void ReturnsLinesAndPolygonWhenCreatingPolygonFrom2Lines()
        {
            output.WriteLine("GetLinesAndPolygonWhenCreatingPolygonFrom2Lines with multiple ordered linestrings");
            GetLinesAndPolygonWhenCreatingPolygonFrom2Lines(ordered:true);
            output.WriteLine("GetLinesAndPolygonWhenCreatingPolygonFrom2Lines with multiple unordered linestrings");
            GetLinesAndPolygonWhenCreatingPolygonFrom2Lines(ordered:false);

            output.WriteLine("GetLinesAndPolygonWhenCreatingPolygonFrom2Lines with multiple unordered linestrings and check for Point Inside Area");
            GetLinesAndPolygonWhenCreatingPolygonFrom2Lines(ordered: false, insideCheck:true);
        }
        
        private void GetLinesAndPolygonWhenCreatingPolygonFrom2Lines(bool ordered=true, bool insideCheck = false)
        // private void GetLinesAndPolygonWhenCreatingPolygonFrom2Lines(bool ordered=true, double? x=null, double? y=null)
        {
            var id = Guid.NewGuid().ToString();

            var linestring = new LineString(new[]
            {
                new Coordinate(0, 0),
                new Coordinate(100, 0),
                new Coordinate(100, 100)
            });

            //Type = "Kaiomr�deGrense"
            var lineFeature = NgisFeatureHelper.CreateFeature(linestring, id, Operation.Create);
            


            var id2 = Guid.NewGuid().ToString();

            LineString linestring2;
            if (ordered)
            {
                linestring2 = new LineString(new[]
                {
                    new Coordinate(100, 100),
                    new Coordinate(0, 100),
                    new Coordinate(0, 0)
                });
            }
            else
            {
                // Unordered direction (could use linestring2.reverse)
                linestring2 = new LineString(new[]
                {
                    new Coordinate(0, 0),
                    new Coordinate(0, 100),
                    new Coordinate(100, 100)
                });

            }
            // Type = "Kaiomr�deGrense"
            var lineFeature2 = NgisFeatureHelper.CreateFeature(linestring2, id2, Operation.Create);
            

            Point? centroid = null;
            if (insideCheck)
            {
                // Check if polygon  entirely contains the given coordinate location
                centroid = new Point(new Coordinate(50, 50));

            }
            // Centroid = centroid
            //Type = "Kaiomr�de"
            var feature = NgisFeatureHelper.CreateFeature(new Polygon(null), null, Operation.Create, new List<string>(){id, id2}, new List<IEnumerable<string>>());
            var res = _topologyImplementation.ResolveReferences(new ToplogyRequest()
            {
                Feature = feature,
                AffectedFeatures = new List<NgisFeature>() { lineFeature, lineFeature2 }
            });

            Assert.Equal(3, res.AffectedFeatures.Count());
            var feature1 = res.AffectedFeatures.First();

            Assert.Equal("LineString", feature1.Geometry!.GeometryType);
            Assert.Equal(id, NgisFeatureHelper.GetLokalId(feature1));
            Assert.Equal(Operation.Create, NgisFeatureHelper.GetOperation(feature1));

            var feature2 = res.AffectedFeatures.ElementAt(1);

            Assert.Equal("LineString", feature2.Geometry!.GeometryType);
            Assert.Equal(Operation.Create, NgisFeatureHelper.GetOperation(feature1));

            var feature3 = res.AffectedFeatures.ElementAt(2); // polygon

            Assert.Equal("Polygon", feature3.Geometry!.GeometryType);
            Assert.Equal(Operation.Create, NgisFeatureHelper.GetOperation(feature1));


            var feature1References = NgisFeatureHelper.GetExteriors(feature1);
            Assert.Single(feature1References);
            Assert.Equal(feature1References.First(), NgisFeatureHelper.GetLokalId(feature3));

            var feature2References = NgisFeatureHelper.GetExteriors(feature2);
            Assert.Single(feature2References);
            Assert.Equal(feature2References.First(), NgisFeatureHelper.GetLokalId(feature3));


            var feature3References = NgisFeatureHelper.GetExteriors(feature3);
            Assert.Equal(2, feature3References.Count);
            Assert.Equal(feature3References.First(), NgisFeatureHelper.GetLokalId(feature1));
            Assert.Equal(feature3References.Last(), NgisFeatureHelper.GetLokalId(feature2));

            if (insideCheck)
            {
                output.WriteLine("InsideCheck for Point Inside Area:{0}", res.IsValid);
                // Assert.True(res.IsValid);
            }
        }

        [Fact]

        void CheckTopologyForRecreateAreaIsValid()
        {
            output.WriteLine("CheckTopologyForRecreateAreaIsValid valid geometries");
            Check2TopologyForRecreateArea(ordered: true, inputValid: true);

            output.WriteLine("CheckTopologyForRecreateAreaIsValid invalid geometries");
            Check2TopologyForRecreateArea(ordered: true, inputValid: false);
        }

        private void Check2TopologyForRecreateArea(bool ordered = true, bool inputValid = true)
        {
            var id = Guid.NewGuid().ToString();

            LineString linestring;
            if (inputValid)
            {
                linestring = new LineString(new[]
                {
                    new Coordinate(0, 0),
                    new Coordinate(100, 0),
                    new Coordinate(100, 100)
                });
            }
            else
            {
                linestring = new LineString(new[]
                {
                    new Coordinate(0, 0),
                    new Coordinate(100, 0),
                    new Coordinate(100, 200)
                });

            }

            //Type = "Kaiomr�deGrense"
            var lineFeature = NgisFeatureHelper.CreateFeature(linestring, id, Operation.Create);
            

            var id2 = Guid.NewGuid().ToString();

            LineString linestring2;
            if (ordered)
            {
                if (inputValid)
                {
                    linestring2 = new LineString(new[]
                    {
                        new Coordinate(100, 100),
                        new Coordinate(0, 100),
                        new Coordinate(0, 0)
                    });
                }
                else
                {
                    linestring2 = new LineString(new[]
                    {
                        new Coordinate(200, 100),
                        new Coordinate(0, 100),
                        new Coordinate(0, 0)
                    });
                }
            }
            else
            {
                // Unordered direction (could use linestring2.reverse)
                linestring2 = new LineString(new[]
                {
                    new Coordinate(0, 0),
                    new Coordinate(0, 100),
                    new Coordinate(100, 100)
                });

            }

            //Type = "Kaiomr�deGrense"
            var lineFeature2 = NgisFeatureHelper.CreateFeature(linestring2, id2, Operation.Create);
            

            Point? centroid = null;

            var res = _topologyImplementation.ResolveReferences(new ToplogyRequest()
            {
                //Type = "Kaiomr�de",
                //Centroid = centroid
                Feature = NgisFeatureHelper.CreateFeature(new Polygon(null), null, Operation.Create, new List<string>(){id, id2}, new List<IEnumerable<string>>()),
                AffectedFeatures = new List<NgisFeature>() { lineFeature, lineFeature2 }
            });

            bool hasValidPolygon = false;
            foreach (var feat in res.AffectedFeatures)
            {
                if (feat.Geometry != null && feat.Geometry.GeometryType == "Polygon")
                {
                    hasValidPolygon = true;
                    break;

                }
            }
            output.WriteLine("Valid input lines: {0}", hasValidPolygon);
        }

    }
}