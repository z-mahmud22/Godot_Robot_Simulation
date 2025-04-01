using Godot;
using System;
using System.Xml.Linq;
using System.Collections.Generic;

public class URDFParser
{
    public class Joint
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string ParentLink { get; set; }
        public string ChildLink { get; set; }
        public Vector3 Origin { get; set; }
        public Vector3 Rotation { get; set; }
        public Vector3 Axis { get; set; }
        public Dictionary<string, float> Limits { get; set; }
        // New property to hold the joint index.
        public int Index { get; set; } = -1;  // default if not found
    }

    public class Material
    {
        public string Name { get; set; }
        public Color Color { get; set; }
    }

    public class Visual
    {
        public Vector3 Origin { get; set; }
        public Vector3 Rotation { get; set; }
        public string MeshPath { get; set; }
        public Material Material { get; set; }
    }

    public class Collision
    {
        public Vector3 Origin { get; set; }
        public Vector3 Rotation { get; set; }
        public string GeometryType { get; set; } // "mesh", "sphere", "cylinder"
        public string MeshPath { get; set; }
        public float Radius { get; set; }  // for sphere and cylinder
        public float Length { get; set; }  // for cylinder
    }

    public class Link
    {
        public string Name { get; set; }
        public List<Visual> Visuals { get; set; } = new List<Visual>();
        public List<Collision> Collisions { get; set; } = new List<Collision>();
        public Dictionary<string, float> Inertial { get; set; }
        public Vector3 InertialOrigin { get; set; }
    }

    private Dictionary<string, Link> links = new Dictionary<string, Link>();
    private Dictionary<string, Joint> joints = new Dictionary<string, Joint>();
    private Dictionary<string, List<string>> childJoints = new Dictionary<string, List<string>>();
    private Dictionary<string, Material> materials = new Dictionary<string, Material>();

    // Mapping from canonical joint names to indices (from the unitree Python SDK)
    private static readonly Dictionary<string, int> jointNameToIndex = new Dictionary<string, int>()
    {
        // Left leg
        { "LeftHipPitch", 0 },
        { "LeftHipRoll", 1 },
        { "LeftHipYaw", 2 },
        { "LeftKnee", 3 },
        { "LeftAnklePitch", 4 },
        { "LeftAnkleRoll", 5 },
        // Right leg
        { "RightHipPitch", 6 },
        { "RightHipRoll", 7 },
        { "RightHipYaw", 8 },
        { "RightKnee", 9 },
        { "RightAnklePitch", 10 },
        { "RightAnkleRoll", 11 },
        // Waist
        { "WaistYaw", 12 },
        { "WaistRoll", 13 },
        { "WaistPitch", 14 },
        // Left arm
        { "LeftShoulderPitch", 15 },
        { "LeftShoulderRoll", 16 },
        { "LeftShoulderYaw", 17 },
        { "LeftElbow", 18 },
        { "LeftWristRoll", 19 },
        { "LeftWristPitch", 20 },
        { "LeftWristYaw", 21 },
        // Right arm
        { "RightShoulderPitch", 22 },
        { "RightShoulderRoll", 23 },
        { "RightShoulderYaw", 24 },
        { "RightElbow", 25 },
        { "RightWristRoll", 26 },
        { "RightWristPitch", 27 },
        { "RightWristYaw", 28 },
        // Special
        { "KNotUsedJoint", 29 }, // NOTE: Weight
        // Dexterous hand: Left
        {"LeftHandThumb0", 30},
        {"LeftHandThumb1", 31},
        {"LeftHandThumb2", 32},
        {"LeftHandMiddle0", 33},
        {"LeftHandMiddle1", 34},
        {"LeftHandIndex0", 35},
        {"LeftHandIndex1", 36},
        // Dexterous hand: Right
        {"RightHandThumb0", 37},
        {"RightHandThumb1", 38},
        {"RightHandThumb2", 39},
        {"RightHandMiddle0", 40},
        {"RightHandMiddle1", 41},
        {"RightHandIndex0", 42},
        {"RightHandIndex1", 43}
    };

    public void ParseURDF(string urdfContent)
    {
        var doc = XDocument.Parse(urdfContent);
        var root = doc.Root;

        // Parse all links.
        foreach (var linkElement in root.Elements("link"))
        {
            var link = new Link
            {
                Name = linkElement.Attribute("name")?.Value,
                Inertial = new Dictionary<string, float>()
            };

            // Parse inertial.
            var inertialElement = linkElement.Element("inertial");
            if (inertialElement != null)
            {
                var inertialOriginElement = inertialElement.Element("origin");
                if (inertialOriginElement != null)
                {
                    link.InertialOrigin = ParseOrigin(inertialOriginElement.Attribute("xyz")?.Value);
                }
            }

            // Parse visual elements.
            foreach (var visualElement in linkElement.Elements("visual"))
            {
                var visual = new Visual();

                var originElement = visualElement.Element("origin");
                if (originElement != null)
                {
                    visual.Origin = ParseOrigin(originElement.Attribute("xyz")?.Value);
                    visual.Rotation = ParseOrigin(originElement.Attribute("rpy")?.Value);
                }

                var meshElement = visualElement.Element("geometry")?.Element("mesh");
                if (meshElement != null)
                {
                    string meshPath = meshElement.Attribute("filename")?.Value;
                    visual.MeshPath = meshPath?.Replace(".STL", ".glb").Replace("meshes/", "");
                }

                var materialElement = visualElement.Element("material");
                if (materialElement != null)
                {
                    var material = ParseMaterial(materialElement);
                    visual.Material = material;
                }

                link.Visuals.Add(visual);
            }

            // Parse collision elements.
            foreach (var collisionElement in linkElement.Elements("collision"))
            {
                var collision = new Collision();

                var originElement = collisionElement.Element("origin");
                if (originElement != null)
                {
                    collision.Origin = ParseOrigin(originElement.Attribute("xyz")?.Value);
                    collision.Rotation = ParseOrigin(originElement.Attribute("rpy")?.Value);
                }

                var geometryElement = collisionElement.Element("geometry");
                if (geometryElement != null)
                {
                    if (geometryElement.Element("mesh") != null)
                    {
                        collision.GeometryType = "mesh";
                        string meshPath = geometryElement.Element("mesh").Attribute("filename")?.Value;
                        collision.MeshPath = meshPath?.Replace(".STL", ".glb").Replace("meshes/", "");
                    }
                    else if (geometryElement.Element("sphere") != null)
                    {
                        collision.GeometryType = "sphere";
                        collision.Radius = float.Parse(geometryElement.Element("sphere").Attribute("radius")?.Value ?? "0");
                    }
                    else if (geometryElement.Element("cylinder") != null)
                    {
                        collision.GeometryType = "cylinder";
                        var cylinderElement = geometryElement.Element("cylinder");
                        collision.Radius = float.Parse(cylinderElement.Attribute("radius")?.Value ?? "0");
                        collision.Length = float.Parse(cylinderElement.Attribute("length")?.Value ?? "0");
                    }
                }

                link.Collisions.Add(collision);
            }

            links[link.Name] = link;
        }

        // Parse all joints.
        foreach (var jointElement in root.Elements("joint"))
        {
            // Get the joint name from the URDF.
            string urdfJointName = jointElement.Attribute("name")?.Value;
            // Convert it into the canonical form.
            string canonicalName = ConvertURDFNameToCanonical(urdfJointName);

            // Determine the joint index.
            int index = -1; // Default index for joints not in the mapping.
            if (jointNameToIndex.TryGetValue(canonicalName, out int mappedIndex))
            {
                index = mappedIndex;
            }
            else
            {
                GD.Print($"Joint '{urdfJointName}' (canonical: '{canonicalName}') is not in the mapping. Assigning index -1.");
            }

            var joint = new Joint
            {
                Name = urdfJointName,
                Type = jointElement.Attribute("type")?.Value,
                ParentLink = jointElement.Element("parent")?.Attribute("link")?.Value,
                ChildLink = jointElement.Element("child")?.Attribute("link")?.Value,
                Limits = new Dictionary<string, float>(),
                Index = index
            };

            var originElement = jointElement.Element("origin");
            if (originElement != null)
            {
                joint.Origin = ParseOrigin(originElement.Attribute("xyz")?.Value);
                joint.Rotation = ParseOrigin(originElement.Attribute("rpy")?.Value);
            }

            var axisElement = jointElement.Element("axis");
            if (axisElement != null)
            {
                Vector3 urdfAxis = ParseOrigin(axisElement.Attribute("xyz")?.Value);
                // Modify the conversion to maintain correct rotation direction
                Vector3 godotAxis = new Vector3(urdfAxis.Y, urdfAxis.Z, urdfAxis.X);
                joint.Axis = godotAxis.Normalized();
            }

            var limitsElement = jointElement.Element("limit");
            if (limitsElement != null)
            {
                foreach (var attr in limitsElement.Attributes())
                {
                    if (float.TryParse(attr.Value, out float value))
                    {
                        joint.Limits[attr.Name.LocalName] = value;
                    }
                }
            }

            joints[joint.Name] = joint;

            if (!childJoints.ContainsKey(joint.ParentLink))
            {
                childJoints[joint.ParentLink] = new List<string>();
            }
            childJoints[joint.ParentLink].Add(joint.Name);
        }
    }


    /// Converts a URDF joint name (e.g. "left_hip_pitch_joint") into the canonical name ("LeftHipPitch").
    private string ConvertURDFNameToCanonical(string urdfName)
    {
        if (string.IsNullOrEmpty(urdfName))
            return string.Empty;

        // Remove the trailing "_joint" (assuming it is always present).
        const string suffix = "_joint";
        if (urdfName.EndsWith(suffix))
        {
            urdfName = urdfName.Substring(0, urdfName.Length - suffix.Length);
        }

        // Split by underscore and capitalize each part.
        string[] parts = urdfName.Split('_');
        string canonical = "";
        foreach (var part in parts)
        {
            if (!string.IsNullOrEmpty(part))
            {
                canonical += char.ToUpper(part[0]) + part.Substring(1).ToLower();
            }
        }
        return canonical;
    }

    private Material ParseMaterial(XElement materialElement)
    {
        var material = new Material
        {
            Name = materialElement.Attribute("name")?.Value
        };

        var colorElement = materialElement.Element("color");
        if (colorElement != null)
        {
            var rgba = colorElement.Attribute("rgba")?.Value.Split(' ');
            if (rgba?.Length == 4)
            {
                material.Color = new Color(
                    float.Parse(rgba[0]),
                    float.Parse(rgba[1]),
                    float.Parse(rgba[2]),
                    float.Parse(rgba[3])
                );
            }
        }

        return material;
    }

    private Vector3 ParseOrigin(string value)
    {
        if (string.IsNullOrEmpty(value))
            return Vector3.Zero;

        var parts = value.Split(' ');
        if (parts.Length >= 3)
        {
            return new Vector3(
                float.Parse(parts[0]),
                float.Parse(parts[1]),
                float.Parse(parts[2])
            );
        }
        return Vector3.Zero;
    }

    public Dictionary<string, Link> GetLinks() => links;
    public Dictionary<string, Joint> GetJoints() => joints;
    public Dictionary<string, List<string>> GetChildJoints() => childJoints;
}
