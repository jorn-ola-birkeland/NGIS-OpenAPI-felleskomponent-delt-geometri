﻿using NetTopologySuite.Features;
using NetTopologySuite.Geometries;

namespace DeltGeometriFelleskomponent.Models;

public static class NgisFeatureHelper
{

    public static string? GetLokalId(NgisFeature feature)
    {
        if (!feature.Properties.Exists("identifikasjon"))
        {
            return null;
        }

        var identifikasjon = (IAttributesTable) feature.Properties["identifikasjon"];
        return  (string)identifikasjon["lokalId"];
    }

    public static Operation? GetOperation(NgisFeature feature)
        => feature.Update?.Action;

    public static NgisFeature CreateFeature(Geometry geometry, string? lokalId)
        => new ()
        {
            Geometry = geometry,
            Properties = lokalId != null ?new AttributesTable(new Dictionary<string, object>()
            {
                {
                    "identifikasjon", new AttributesTable(new Dictionary<string, object>()
                    {
                        {
                            "lokalId", lokalId
                        }
                    })
                }
            }) : new AttributesTable()
        };

    public static NgisFeature CreateFeature(Geometry geometry, string? lokalId, Operation operation)
    {
        var feature = CreateFeature(geometry, lokalId);
        SetOperation(feature, operation);
        return feature;
    }

    public static NgisFeature CreateFeature(Geometry geometry, string lokalId, Operation operation, IEnumerable<string> exterior, IEnumerable<IEnumerable<string>>? interiors)
    {
        var feature = CreateFeature(geometry, lokalId, operation);
        SetReferences(feature, exterior, interiors);
        return feature;
    }
    
    
    public static void SetReferences(NgisFeature feature, IEnumerable<string> exterior, IEnumerable<IEnumerable<string>>? interiors)
    {
        feature.Geometry_Properties ??= new GeometryProperties();
        feature.Geometry_Properties.Exterior = exterior.ToList();
        feature.Geometry_Properties.Interiors = interiors?.Select(i => i.ToList()).ToList();
    }

    public static List<string> GetExteriors(NgisFeature feature) => feature.Geometry_Properties?.Exterior ?? new List<string>();

    public static List<List<string>> GetInteriors(NgisFeature feature) => feature.Geometry_Properties?.Interiors ?? new List<List<string>>();

    public static void SetReferences(NgisFeature feature, IEnumerable<NgisFeature> exterior,
        IEnumerable<IEnumerable<NgisFeature>>? interiors)
        => SetReferences(feature, exterior.Select(GetLokalId),
            interiors?.Select(i => i.Select(GetLokalId)));

    public static void SetOperation(NgisFeature feature, Operation operation)
    {
        feature.Update ??= new UpdateAction();
        feature.Update.Action = operation;
    }

    public static void EnsureLocalId(NgisFeature feature)
    {
        if (GetLokalId(feature) == null)
        {
            SetLokalId(feature, Guid.NewGuid().ToString());
        }
    }

    public static void SetLokalId(NgisFeature feature, string lokalId)
    {
        //This just looks plain stupid, why copy the attributes table?
        //well, we are using NetTopologySuite.IO.GeoJSON4STJ for geojson (de)serialization, 
        //and for some reason that implementation provides its own Attributes table: StjAttributesTable
        //this does not support setting attributes
        // as can be seen here: https://github.com/NetTopologySuite/NetTopologySuite.IO.GeoJSON/blob/77aa684748086d741d8a15db2b089b6b9ddf0848/src/NetTopologySuite.IO.GeoJSON4STJ/Converters/StjAttributesTable.cs#L29
        var props = feature.Properties.GetNames().Aggregate(new AttributesTable(), (acc, key) =>
        {
            acc.Add(key, feature.Properties.GetOptionalValue(key));
            return acc;
        } );

        if (!props.Exists("identifikasjon"))
        {
            props.Add("identifikasjon",new AttributesTable());
        }
        ((IAttributesTable)props["identifikasjon"]).Add("lokalId", lokalId);

        feature.Properties = props;
        
    }

}