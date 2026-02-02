using System.Xml.Serialization;

namespace PlmXmlSyntax.Models;

[XmlRoot("PLMXML", Namespace = "http://www.plmxml.org/Schemas/PLMXMLSchema", IsNullable = false)]
public class PlmXml
{
    [XmlElement("Instance")]
    public List<Instance> Instances { get; set; } = new();

    [XmlElement("ExternalFile")]
    public List<ExternalFile> ExternalFiles { get; set; } = new();

    [XmlElement("DataSet")]
    public List<DataSet> DataSets { get; set; } = new();

    [XmlElement("Product")]
    public List<Product> Products { get; set; } = new();

    [XmlElement("ProductRevision")]
    public List<ProductRevision> ProductRevisions { get; set; } = new();

    [XmlElement("ProductDef")]
    public ProductDef ProductDef { get; set; } = new();
}

public class Product
{
    [XmlAttribute("id")]
    public string Id { get; set; } = string.Empty;

    [XmlAttribute("productId")]
    public string ProductId { get; set; } = string.Empty;

    [XmlAttribute("subType")]
    public string SubType { get; set; } = string.Empty;

    [XmlAttribute("name")]
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;
}

public class AssociatedDataSet
{
    [XmlAttribute("id")]
    public string Id { get; set; } = string.Empty;

    [XmlAttribute("dataSetRef")]
    public string DataSetRef { get; set; } = string.Empty;

    [XmlAttribute("role")]
    public string Role { get; set; } = string.Empty;
}

public class ProductRevision
{
    [XmlAttribute("id")]
    public string Id { get; set; } = string.Empty;

    [XmlAttribute("revision")]
    public string Revision { get; set; } = string.Empty;

    [XmlAttribute("subType")]
    public string SubType { get; set; } = string.Empty;

    [XmlAttribute("name")]
    public string Name { get; set; } = string.Empty;

    [XmlAttribute("masterRef")]
    public string MasterRef { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    [XmlElement("instanceRefs")]
    public string InstanceRefs { get; set; } = string.Empty;

    public AssociatedDataSet AssociatedDataSet { get; set; } = new();
}

public class UserValue
{
    [XmlAttribute("title")]
    public string Title { get; set; } = string.Empty;

    [XmlAttribute("value")]
    public string Value { get; set; } = string.Empty;
}

public class Instance
{
    [XmlAttribute("id")]
    public string Id { get; set; } = string.Empty;

    [XmlAttribute("instanceType")]
    public string InstanceType { get; set; } = string.Empty;

    [XmlArray("UserData")]
    [XmlArrayItem("UserValue")]
    public UserValue[] UserData { get; set; } = Array.Empty<UserValue>();
}

public class ExternalFile
{
    [XmlAttribute("id")]
    public string Id { get; set; } = string.Empty;

    [XmlAttribute("locationRef")]
    public string Location { get; set; } = string.Empty;

    [XmlAttribute("format")]
    public string Format { get; set; } = string.Empty;
}

public class DataSet
{
    [XmlAttribute("id")]
    public string Id { get; set; } = string.Empty;

    [XmlAttribute("version")]
    public string Version { get; set; } = string.Empty;

    [XmlAttribute("memberRefs")]
    public string MemberRefs { get; set; } = string.Empty;

    [XmlAttribute("type")]
    public string Type { get; set; } = string.Empty;

    public string? Description { get; set; }
}

public class ProductDef
{
    [XmlElement("InstanceGraph")]
    public InstanceGraph InstanceGraph { get; set; } = new();
}

public class InstanceGraph
{
    [XmlAttribute("rootInstanceRef")]
    public string RootInstanceRef { get; set; } = string.Empty;

    [XmlElement("ProductInstance")]
    public List<ProductInstance> ProductInstances { get; set; } = new();

    [XmlElement("ProductRevisionView")]
    public List<ProductRevisionView> ProductRevisionViews { get; set; } = new();
}

public class ProductRevisionView
{
    [XmlAttribute("id")]
    public string Id { get; set; } = string.Empty;

    [XmlAttribute("type")]
    public string Type { get; set; } = string.Empty;

    [XmlAttribute("revisionRef")]
    public string RevisionRef { get; set; } = string.Empty;

    [XmlAttribute("instanceRefs")]
    public string InstanceRefs { get; set; } = string.Empty;

    [XmlAttribute("name")]
    public string Name { get; set; } = string.Empty;

    [XmlArray("UserData")]
    [XmlArrayItem("UserValue")]
    public UserValue[] UserData { get; set; } = Array.Empty<UserValue>();

    public bool ShouldSerializeUserData()
    {
        return UserData != null && UserData.Length > 0;
    }

    [XmlElement("Representation")]
    public Representation? Representation { get; set; }
}

public class ProductInstance
{
    [XmlAttribute("id")]
    public string Id { get; set; } = string.Empty;

    [XmlAttribute("sequenceNumber")]
    public string SequenceNumber { get; set; } = string.Empty;

    [XmlAttribute("partRef")]
    public string PartRef { get; set; } = string.Empty;

    [XmlAttribute("name")]
    public string Name { get; set; } = string.Empty;

    [XmlElement("Transform")]
    public Transform? Transform { get; set; }
}

public class Transform
{
    [XmlAttribute("id")]
    public string Id { get; set; } = string.Empty;

    [XmlText]
    public string Value { get; set; } = string.Empty;
}

public class Representation
{
    [XmlAttribute("id")]
    public string Id { get; set; } = string.Empty;

    [XmlAttribute("format")]
    public string Format { get; set; } = string.Empty;

    [XmlAttribute("location")]
    public string Location { get; set; } = string.Empty;
}
