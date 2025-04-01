using Godot;
using System;
using System.Collections.Generic;

// [Tool]
public partial class RobotLoader : Node3D
{
    [Export]
    public string URDFPath { get; set; } = "res://robots/g1_29dof.urdf";
    
    [Export]
    public string MeshBasePath { get; set; } = "res://robots/meshes/";

    private URDFParser parser;
    private Dictionary<string, Node3D> linkNodes = new Dictionary<string, Node3D>();

    // The dictionary is keyed by joint index (int) rather than joint name.
    public Dictionary<int, (Node3D JointNode, Basis InitialRotation, Vector3 Axis)> RevoluteJoints = 
        new Dictionary<int, (Node3D, Basis, Vector3)>();
    
    public void ExportAsGLB(string outputPath)
    {
        var gltfDocument = new GltfDocument();
        var gltfState = new GltfState();
        
        // Get the root node of the robot
        var robotRoot = GetNode<Node3D>("RobotRoot");
        
        // Export the robot hierarchy
        Error err = gltfDocument.AppendFromScene(robotRoot, gltfState);
        if (err != Error.Ok)
        {
            GD.PrintErr("Failed to append node to GLTF: ", err);
            return;
        }
        
        // Save to filesystem
        err = gltfDocument.WriteToFilesystem(gltfState, outputPath);
        if (err == Error.Ok)
            GD.Print("Robot exported to: ", outputPath);
        else
            GD.PrintErr("Export failed: ", err);
    }

    // Convert URDF (ROS) position to Godot position.
    private Vector3 UrdfToGodotPosition(Vector3 urdfPosition)
    {
        return new Vector3(urdfPosition.Y, urdfPosition.Z, urdfPosition.X); // Applied a -90 rotation on the Z-axis to the blender meshes 
    }

    // Convert URDF (ROS) XYZ Euler angles to Godot rotation.
    private Basis UrdfToGodotRotation(Vector3 urdfEuler)
    {
        Basis rotation = Basis.Identity;

        // Convert URDF's XYZ (roll, pitch, yaw) to Godot's coordinate system:
        rotation = rotation.Rotated(new Vector3(0, 1, 0).Normalized(), urdfEuler.Z);
        rotation = rotation.Rotated(new Vector3(-1, 0, 0).Normalized(), urdfEuler.Y);
        rotation = rotation.Rotated(new Vector3(0, 0, -1).Normalized(), urdfEuler.X);

        return rotation;
    }

    public override void _Ready()
    {
        LoadRobot();
        // ExportAsGLB("res://exported_g1_robot.glb"); // Uncomment if the robot model needs to be exported
        Position = new Vector3(0, 0.78f, 1); // Spawning robot at this position
        Rotation = new Vector3(0, Mathf.DegToRad(0), 0); // Rotates the robot
    }

    private void LoadRobot()
    {
        var urdfFile = FileAccess.Open(URDFPath, FileAccess.ModeFlags.Read);
        if (urdfFile == null)
        {
            GD.PrintErr($"Failed to open URDF file: {URDFPath}");
            return;
        }

        string urdfContent = urdfFile.GetAsText();
        parser = new URDFParser();
        parser.ParseURDF(urdfContent);

        CreateRobotStructure();
    }

    private void CreateRobotStructure()
    {
        var rootNode = new Node3D();
        rootNode.Name = "RobotRoot";
        AddChild(rootNode);
        
        // Define main body collision capsule
        var collisionShape = new CollisionShape3D();
        collisionShape.Name = "BodyCollision";
        
        var capsule = new CapsuleShape3D
        {
            Height = 1.4f, // Height is set to 1.4 meters (typical humanoid height minus legs)
            Radius = 0.25f // Radius is set to 0.25 meters (reasonable width for torso)
        };
        
        collisionShape.Shape = capsule;
        
        // Position the capsule to cover the main body
        // Offset upward by half the capsule height to align with the robot's torso
        collisionShape.Position = new Vector3(0, 0.7f, 0);
        
        // Add collision shape to the root node
        rootNode.AddChild(collisionShape);
        
        // Start building hierarchy from the root link ("pelvis").
        CreateLinkHierarchy("pelvis", rootNode);
    }

    private void CreateLinkHierarchy(string linkName, Node3D parentNode)
    {
        var links = parser.GetLinks();
        var joints = parser.GetJoints();
        var childJoints = parser.GetChildJoints();

        if (!links.ContainsKey(linkName)) return;

        // Create the link node and attach it to the parent.
        var linkNode = new Node3D();
        linkNode.Name = linkName;
        parentNode.AddChild(linkNode);
        linkNodes[linkName] = linkNode;

        // Add visuals and collisions to this link.
        CreateVisualsAndCollisions(linkName, linkNode);
        
        // Process child joints.
        if (childJoints.TryGetValue(linkName, out var jointNames))
        {
            foreach (var jointName in jointNames)
            {
                if (!parser.GetJoints().TryGetValue(jointName, out var joint))
                    continue;

                var jointNode = new Node3D();
                jointNode.Name = joint.Name;
                
                Vector3 godotPos = UrdfToGodotPosition(joint.Origin);
                
                // For revolute joints, we want to initialize them in a neutral position
                // by removing the initial rotation from the joint's origin
                Basis godotRot;
                if (joint.Type == "revolute")
                {
                    // Only keep Z rotation for alignment, zero out X and Y rotations
                    Vector3 modifiedRotation = new Vector3(0, 0, joint.Rotation.Z);
                    godotRot = UrdfToGodotRotation(modifiedRotation);
                }
                else
                {
                    godotRot = UrdfToGodotRotation(joint.Rotation);
                }
                
                jointNode.Transform = new Transform3D(godotRot, godotPos);

                linkNode.AddChild(jointNode);

                if (joint.Type == "revolute")
                {
                    RevoluteJoints[joint.Index] = (jointNode, godotRot, joint.Axis);
                }
                
                // Recursively create child links.
                CreateLinkHierarchy(joint.ChildLink, jointNode);
            }
        }
    }

    private void CreateVisualsAndCollisions(string linkName, Node3D linkNode)
    {
        var link = parser.GetLinks()[linkName];
        
        foreach (var visual in link.Visuals)
        {
            if (string.IsNullOrEmpty(visual.MeshPath))
                continue;
            
            var meshInstance = new MeshInstance3D();
            var meshScene = GD.Load<PackedScene>(MeshBasePath + visual.MeshPath);
            if (meshScene == null)
                continue;

            meshInstance.Mesh = meshScene.Instantiate<Node3D>().GetChild<MeshInstance3D>(0).Mesh;
            
            // Apply material.
            if (visual.Material != null)
            {
                var material = new StandardMaterial3D { AlbedoColor = visual.Material.Color };
                meshInstance.MaterialOverride = material;
            }

            Vector3 godotPos = UrdfToGodotPosition(visual.Origin);
            Basis godotRot = UrdfToGodotRotation(visual.Rotation);
            meshInstance.Transform = new Transform3D(godotRot, godotPos);

            linkNode.AddChild(meshInstance);
        }

        foreach (var collision in link.Collisions)
        {
            var collisionShape = new CollisionShape3D();
            collisionShape.Name = $"{linkName}_collision";

            Shape3D shape = null;
            switch (collision.GeometryType)
            {
                case "mesh":
                    var meshScene = GD.Load<PackedScene>(MeshBasePath + collision.MeshPath);
                    if (meshScene != null)
                    {
                        var mesh = meshScene.Instantiate<Node3D>().GetChild<MeshInstance3D>(0).Mesh;
                        shape = mesh.CreateConvexShape();
                    }
                    break;
                case "sphere":
                    var sphereShape = new SphereShape3D { Radius = collision.Radius };
                    shape = sphereShape;
                    break;
                case "cylinder":
                    var cylinderShape = new CylinderShape3D { Radius = collision.Radius, Height = collision.Length };
                    shape = cylinderShape;
                    break;
            }

            if (shape != null)
            {
                collisionShape.Shape = shape;
                Vector3 godotPosition = UrdfToGodotPosition(collision.Origin);
                Basis godotRotation = UrdfToGodotRotation(collision.Rotation);
                var collisionTransform = new Transform3D(godotRotation, godotPosition);
                collisionShape.Transform = collisionTransform;
                linkNode.AddChild(collisionShape);
            }
        }
    }
}