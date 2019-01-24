// Copyright (C) Sergey Grigorev
// Web site: http://ssdc.getdev.tk
// This addon SSDC, the deformable car for NeoAxis Engine.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Design;
using System.Text;
using Engine;
using Engine.SoundSystem;
using System.IO;
using Engine.FileSystem;
using Engine.EntitySystem;
using Engine.MapSystem;
using Engine.PhysicsSystem;
using Engine.Renderer;
using Engine.MathEx;
using Engine.Utils;
using ProjectCommon;
using System.Runtime.InteropServices;

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace ProjectEntities
{
    /// <summary>
    /// The deformable car type.
    /// </summary>
    public class SSDCType : CarType
    {
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public class SSDC_CONF
        {
            string name;
            internal SSDC_CONF(string name)
            {
                this.name = name;
            }
            public override string ToString()
            {
                return name;
            }

            [FieldSerialize]
            float heightResetCar = 1.5f;

            [DefaultValue(1.5f)]
            public float HeightResetCar
            {
                get { return heightResetCar; }
                set { heightResetCar = value; }
            }

            [FieldSerialize]
            bool deformation = true;

            [Editor(typeof(EditorSoundUITypeEditor), typeof(UITypeEditor))]
            [DefaultValue(true)]
            public bool Deformation
            {
                get { return deformation; }
                set { deformation = value; }
            }

            [FieldSerialize]
            bool parallelComputing = true;

            [Editor(typeof(EditorSoundUITypeEditor), typeof(UITypeEditor))]
            [DefaultValue(true)]
            public bool ParallelComputing
            {
                get { return parallelComputing; }
                set { parallelComputing = value; }
            }

            [FieldSerialize]
            float deformationRadius = 0.8f;

            [DefaultValue(0.8f)]
            public float DeformationRadius
            {
                get { return deformationRadius; }
                set { deformationRadius = value; }
            }

            [FieldSerialize]
            float maxStrengthDeformation = 0.2f;

            [DefaultValue(0.2f)]
            public float MaxStrengthDeformation
            {
                get { return maxStrengthDeformation; }
                set { maxStrengthDeformation = value; }
            }

            [FieldSerialize]
            string soundOn;

            [FieldSerialize]
            string soundBrake;

            [FieldSerialize]
            string soundBackfire;

            [FieldSerialize]
            string soundOff;

            [FieldSerialize]
            string soundGearUp;

            [FieldSerialize]
            string soundGearDown;

            public class SoundGear
            {
                [FieldSerialize]
                int number;

                [FieldSerialize]
                Range speedRange;

                [FieldSerialize]
                string soundMotor;

                [FieldSerialize]
                [DefaultValue(typeof(Range), "1 1.2")]
                Range soundMotorPitchRange = new Range(1, 1.2f);

                [DefaultValue(0)]
                public int Number
                {
                    get { return number; }
                    set { number = value; }
                }

                [DefaultValue(typeof(Range), "0 0")]
                public Range SpeedRange
                {
                    get { return speedRange; }
                    set { speedRange = value; }
                }

                [Editor(typeof(EditorSoundUITypeEditor), typeof(UITypeEditor))]
                [SupportRelativePath]
                public string SoundMotor
                {
                    get { return soundMotor; }
                    set { soundMotor = value; }
                }

                [DefaultValue(typeof(Range), "1 1.2")]
                public Range SoundMotorPitchRange
                {
                    get { return soundMotorPitchRange; }
                    set { soundMotorPitchRange = value; }
                }

                public override string ToString()
                {
                    return string.Format("Gear {0}", number);
                }
            }

            public class RandomMaterial
            {
                [FieldSerialize]
                string path;

                [Editor(typeof(EditorMaterialUITypeEditor), typeof(UITypeEditor))]
                [SupportRelativePath]
                public string Path
                {
                    get { return path; }
                    set { path = value; }
                }

                public override string ToString()
                {
                    return string.Format("Material {0}", path);
                }
            }

            [Editor(typeof(EditorSoundUITypeEditor), typeof(UITypeEditor))]
            [SupportRelativePath]
            public string SoundOn
            {
                get { return soundOn; }
                set { soundOn = value; }
            }

            [Editor(typeof(EditorSoundUITypeEditor), typeof(UITypeEditor))]
            [SupportRelativePath]
            public string SoundBrake
            {
                get { return soundBrake; }
                set { soundBrake = value; }
            }

            [Editor(typeof(EditorSoundUITypeEditor), typeof(UITypeEditor))]
            [SupportRelativePath]
            public string SoundOff
            {
                get { return soundOff; }
                set { soundOff = value; }
            }

            [Editor(typeof(EditorSoundUITypeEditor), typeof(UITypeEditor))]
            [SupportRelativePath]
            public string SoundGearUp
            {
                get { return soundGearUp; }
                set { soundGearUp = value; }
            }

            [Editor(typeof(EditorSoundUITypeEditor), typeof(UITypeEditor))]
            [SupportRelativePath]
            public string SoundGearDown
            {
                get { return soundGearDown; }
                set { soundGearDown = value; }
            }
            [FieldSerialize]
            List<SoundGear> soundgears = new List<SoundGear>();
            public List<SoundGear> SoundGears
            {
                get { return soundgears; }
            }

            [FieldSerialize]
            List<RandomMaterial> randomMaterials = new List<RandomMaterial>();
            public List<RandomMaterial> RandomMaterials
            {
                get { return randomMaterials; }
            }
        }
        [FieldSerialize]
        SSDC_CONF conf = new SSDC_CONF("config");
        public SSDC_CONF CONF
        {
            get { return conf; }
        }

        protected override void OnPreloadResources()
        {
            base.OnPreloadResources();

            PreloadSound(CONF.SoundOn, SoundMode.Mode3D);
            PreloadSound(CONF.SoundBrake, SoundMode.Mode3D);
            PreloadSound(CONF.SoundOff, SoundMode.Mode3D);
            PreloadSound(CONF.SoundGearUp, SoundMode.Mode3D);
            PreloadSound(CONF.SoundGearDown, SoundMode.Mode3D);
            foreach (SSDC_CONF.SoundGear gear in CONF.SoundGears)
                PreloadSound(gear.SoundMotor, SoundMode.Mode3D | SoundMode.Loop);
        }
    }

    public class SSDC : Car
    {
        bool first_tick = true;
        bool first_init_mesh = false;
        
        SSDCType.SSDC_CONF.SoundGear current_gears;
        bool motor_on;
        string current_motor_sound_name;
        VirtualChannel motor_sound_channel;
        
        List<RibbonTrail> rtbrake = new List<RibbonTrail>();

        Mesh mesh;
        string mesh_name;
        MapObjectAttachedMesh attached_mesh;
        
        Body car_body;
        Shape dmd_shape;

        Vec3 hit_pos = Vec3.Zero;
        Vec3 hit_norm = Vec3.Zero;
        float hit_dep = 0;
        Shape hit_this = null;
        Shape hit_other = null;

        int vertex_count = 0;
        int index_count = 0;
        List<Vec3> original_vertex_pos = new List<Vec3>();
        List<Vec3> original_vertex_norm = new List<Vec3>();
        List<Vec2> original_vertex_tc = new List<Vec2>();
        List<int> original_vertex_ind = new List<int>();
        List<Vec3> changeable_vertex_pos = new List<Vec3>();
        List<Vec3> changeable_vertex_norm = new List<Vec3>();

        [StructLayout(LayoutKind.Sequential)]
        struct Vertex
        {
            public Vec3 position;
            public Vec3 normal;
            public Vec2 texCoord;
        }

        SSDCType _type = null; public new SSDCType Type { get { return _type; } }
        
        protected override void OnPostCreate(bool loaded)
        {
            base.OnPostCreate(loaded);
            current_gears = Type.CONF.SoundGears.Find(delegate(SSDCType.SSDC_CONF.SoundGear gear)
            {
                return gear.Number == 0;
            });
            if (EngineApp.Instance.ApplicationType != EngineApp.ApplicationTypes.ResourceEditor)
            {
                if (PhysicsModel == null)
                {
                    Log.Error("Car: Physics model not exists.");
                    return;
                }

                car_body = PhysicsModel.GetBody("chassis");
                dmd_shape = car_body.GetShape("dmd");
                if (car_body == null)
                {
                    Log.Error("Car: \"chassis\" body not exists.");
                    return;
                }
                if (dmd_shape == null)
                {
                    Log.Error("Car: \"dmd\" shape not exists.");
                    return;
                }
                attached_mesh = GetFirstAttachedObjectByAlias("dmd") as MapObjectAttachedMesh;
                if (attached_mesh == null)
                {
                    Log.Error("Car: mesh by \"dmd\" alias not exists.");
                    return;
                }
                else
                {
                    mesh_name = this.Name;
                    attached_mesh.MeshObject.Mesh.Save("Data\\" + mesh_name + ".mesh");
                    attached_mesh.MeshName = mesh_name + ".mesh";
                    mesh = attached_mesh.MeshObject.Mesh;
                    if (Type.CONF.RandomMaterials.Any())
                    {
                        attached_mesh.MeshObject.SetMaterialNameForAllSubObjects(Type.CONF.RandomMaterials[World.Instance.Random.Next(0, Type.CONF.RandomMaterials.Count)].Path);
                    }
                }

                if (Type.CONF.Deformation) ParsingMesh();
            }
            if (EntitySystemWorld.Instance.WorldSimulationType != WorldSimulationTypes.Editor)
            {
                SubscribeToTickEvent();

                MapObjectAttachedObject[] ortbrake = GetAllAttachedObjectsByAlias("brake");
                for (int i = 0; i < ortbrake.Length; i++)
                {
                    MapObjectAttachedRibbonTrail rt = ortbrake[i] as MapObjectAttachedRibbonTrail;
                    if (rt != null) rtbrake.Add(rt.RibbonTrail);
                }
            }

            if (car_body != null)
            {
                car_body.Collision += OnCollision;
            }
        }

        protected override void OnDestroy()
        {
            DestroyMesh();
            if (car_body != null)
            {
                car_body.Collision -= OnCollision;
            }
            base.OnDestroy();
        }

        protected override void OnTick()
        {
            base.OnTick();
            ResetCar();
            TickCurrentGear();
            TickMotorSound();
            if (Type.CONF.Deformation)
            {
                if (attached_mesh != null)
                {
                    attached_mesh.MeshObject.Mesh.SubMeshes[0].VertexData.GetSomeGeometry(ref changeable_vertex_pos,
                        ref changeable_vertex_norm);
                    if (changeable_vertex_pos.Count != 0)
                    {
                        if (Type.CONF.ParallelComputing)
                        {
                            DeformationMeshParallel();
                        }
                        else
                        {
                            DeformationMesh();
                        }
                    }
                }
            }
            if (PhysicsModel != null)
            {
                float handbrake = 0;
                if (Intellect != null)
                {
                    handbrake = Intellect.GetControlKeyStrength(GameControlKeys.VehicleHandbrake);
                }
                else
                {
                    handbrake = 1;
                }
                if (!IsInAir() & handbrake != 0 &
                    (PhysicsModel.Bodies[0].LinearVelocity.Length() * 3600.0f / 1000.0f) > 30)
                {
                    SoundPlay3D(Type.CONF.SoundBrake, .7f, true);
                    foreach (RibbonTrail rt in rtbrake)
                    {
                        rt.Visible = true;
                    }
                }
                else
                {
                    foreach (RibbonTrail rt in rtbrake)
                    {
                        rt.Visible = false;
                    }
                }
            }
            first_tick = false;
        }

        protected override void Client_OnTick()
        {
            base.Client_OnTick();
            ResetCar();
            TickCurrentGear();
            TickMotorSound();
            first_tick = false;
        }

        void ResetCar()
        {
            if (Intellect != null)
            {
                if (Intellect.IsControlKeyPressed(GameControlKeys.Reload))
                {                  
                    PhysicsModel.Bodies[0].ClearForces();
                    PhysicsModel.Bodies[0].LinearVelocity = Vec3.Zero;
                    PhysicsModel.Bodies[0].AngularVelocity = Vec3.Zero;

                    Ray ray = new Ray(Position, new Vec3(0, 0, -Type.CONF.HeightResetCar));
                    if (!Single.IsNaN(ray.Direction.X) && !Single.IsNaN(ray.Origin.X))
                    {
                        RayCastResult[] piercingResult = PhysicsWorld.Instance.RayCastPiercing(
                        ray, (int)ContactGroup.CastOnlyContact);
                        bool collision = false;
                        foreach (RayCastResult result in piercingResult)
                        {
                            if (Array.IndexOf(PhysicsModel.Bodies, result.Shape.Body) != -1)
                                continue;
                            collision = true;
                            break;
                        }
                        if (collision)
                        {
                            Position = new Vec3(OldPosition.X, OldPosition.Y, OldPosition.Z + 0.1f);
                        }
                    }                    
                    Rotation = new Quat(0, 0, OldRotation.Z, OldRotation.W);
                }
            }
        }

        void TickMotorSound()
        {
            bool last_motor_on = motor_on;
            motor_on = Intellect != null && Intellect.IsActive();

            //sound on, off
            if (motor_on != last_motor_on)
            {
                if (!first_tick && Health != 0)
                {
                    if (motor_on)
                        SoundPlay3D(Type.CONF.SoundOn, .7f, true);
                    else
                        SoundPlay3D(Type.CONF.SoundOff, .7f, true);
                }
            }

            string need_sound_name = null;
            if (motor_on && current_gears != null)
                need_sound_name = current_gears.SoundMotor;

            if (need_sound_name != current_motor_sound_name)
            {
                //change motor sound
                if (motor_sound_channel != null)
                {
                    motor_sound_channel.Stop();
                    motor_sound_channel = null;
                }

                current_motor_sound_name = need_sound_name;

                if (!string.IsNullOrEmpty(need_sound_name))
                {
                    Sound sound = SoundWorld.Instance.SoundCreate(
                        RelativePathUtils.ConvertToFullPath(Path.GetDirectoryName(Type.FilePath), need_sound_name),
                        SoundMode.Mode3D | SoundMode.Loop);

                    if (sound != null)
                    {
                        motor_sound_channel = SoundWorld.Instance.SoundPlay(
                            sound, EngineApp.Instance.DefaultSoundChannelGroup, .3f, true);
                        motor_sound_channel.Position = Position;
                        switch (Type.SoundRolloffMode)
                        {
                            case DynamicType.SoundRolloffModes.Logarithmic:
                                motor_sound_channel.SetLogarithmicRolloff(Type.SoundMinDistance, Type.SoundMaxDistance,
                                    Type.SoundRolloffLogarithmicFactor);
                                break;
                            case DynamicType.SoundRolloffModes.Linear:
                                motor_sound_channel.SetLinearRolloff(Type.SoundMinDistance, Type.SoundMaxDistance);
                                break;
                        }
                        motor_sound_channel.Pause = false;
                    }
                }
            }

            //update motor channel position and pitch
            if (motor_sound_channel != null)
            {
                Range speed_range_abs = current_gears.SpeedRange;
                if (speed_range_abs.Minimum < 0 && speed_range_abs.Maximum < 0)
                    speed_range_abs = new Range(-speed_range_abs.Maximum, -speed_range_abs.Minimum);
                Range pitch_range = current_gears.SoundMotorPitchRange;

                float speed_abs = (PhysicsModel.Bodies[0].LinearVelocity.Length() * 3600.0f / 1000.0f);

                float speed_coef = 0;
                if (speed_range_abs.Size() != 0)
                    speed_coef = (speed_abs - speed_range_abs.Minimum) / speed_range_abs.Size();
                MathFunctions.Clamp(ref speed_coef, 0, 1);

                //update channel
                motor_sound_channel.Pitch = pitch_range.Minimum + speed_coef * pitch_range.Size();
                motor_sound_channel.Position = Position;
            }
        }

        void TickCurrentGear()
        {
            if (current_gears == null)
                return;

            if (motor_on)
            {
                //if (World.Instance.Random.NextBool()) { SoundPlay3D(Type.CONF.SoundBackfire, .7f, true); } //backfire
                float speed = (PhysicsModel.Bodies[0].LinearVelocity.Length() * 3600.0f / 1000.0f);

                SSDCType.SSDC_CONF.SoundGear new_gear = null;

                if (speed < current_gears.SpeedRange.Minimum || speed > current_gears.SpeedRange.Maximum)
                {
                    //find new gear
                    new_gear = Type.CONF.SoundGears.Find(delegate(SSDCType.SSDC_CONF.SoundGear gear)
                    {
                        return speed >= gear.SpeedRange.Minimum && speed <= gear.SpeedRange.Maximum;
                    });                    
                }

                if (new_gear != null && current_gears != new_gear)
                {
                    //change gear
                    SSDCType.SSDC_CONF.SoundGear old_gear = current_gears;
                    OnGearChange(old_gear, new_gear);
                    current_gears = new_gear;                    
                }
            }
            else
            {
                if (current_gears.Number != 0)
                {
                    current_gears = Type.CONF.SoundGears.Find(delegate(SSDCType.SSDC_CONF.SoundGear gear)
                    {
                        return gear.Number == 0;
                    });
                }
            }
        }

        void OnGearChange(SSDCType.SSDC_CONF.SoundGear old_gear, SSDCType.SSDC_CONF.SoundGear new_gear)
        {
            if (!first_tick && Health != 0)
            {
                bool up = Math.Abs(new_gear.Number) > Math.Abs(old_gear.Number);
                string sound_name = up ? Type.CONF.SoundGearUp : Type.CONF.SoundGearDown;
                SoundPlay3D(sound_name, .7f, true);                
            }
        }

        void ParsingMesh()
        {
            if (mesh != null)
            {
                mesh.SubMeshes[0].VertexData.GetSomeGeometry(ref original_vertex_pos, ref original_vertex_norm,
                    ref original_vertex_tc);
                mesh.SubMeshes[0].IndexData.GetIndices(ref original_vertex_ind);
                vertex_count = mesh.SubMeshes[0].VertexData.VertexCount;
                index_count = mesh.SubMeshes[0].IndexData.IndexCount;

                SubMesh sub_mesh = mesh.SubMeshes[0];
                sub_mesh.UseSharedVertices = false;
                HardwareBuffer.Usage usage = HardwareBuffer.Usage.DynamicWriteOnly;
                HardwareVertexBuffer vertexBuffer = HardwareBufferManager.Instance.CreateVertexBuffer(
                    Marshal.SizeOf(typeof(Vertex)), vertex_count, usage);
                sub_mesh.VertexData.VertexBufferBinding.SetBinding(0, vertexBuffer, true);
                sub_mesh.VertexData.VertexCount = vertex_count;
                HardwareIndexBuffer indexBuffer = HardwareBufferManager.Instance.CreateIndexBuffer(
                    HardwareIndexBuffer.IndexType._16Bit, index_count, usage);
                sub_mesh.IndexData.SetIndexBuffer(indexBuffer, true);
                sub_mesh.IndexData.IndexCount = index_count;
            }
        }

        void DestroyMesh()
        {
            if (mesh != null)
            {
                mesh.Dispose();
                mesh = null;
                System.IO.File.Delete(Engine.FileSystem.VirtualFileSystem.GetRealPathByVirtual(attached_mesh.MeshName));
            }
        }

        protected override void OnRender(Camera camera)
        {
            base.OnRender(camera);
            /*PlayerIntellect playerIntellect = Intellect as PlayerIntellect;
            if (EntitySystemWorld.Instance.Simulation && playerIntellect != null)
            {
                Font font = FontManager.Instance.LoadFont("Default", .03f);
                EngineApp.Instance.ScreenGuiRenderer.AddText(font, "Depth: " + hit_dep.ToString() + "\nDforce: " + d_force.ToString(),
                        new Vec2(.5f, .9f), HorizontalAlign.Center,
                        VerticalAlign.Bottom, new ColorValue(1, 1, 1));
            }*/
        }

        private void OnCollision(ref CollisionEvent collisonEvent)
        {
            if (collisonEvent.ThisShape.Name == "dmd")
            {
                hit_pos = collisonEvent.Position;
                hit_dep = collisonEvent.Depth;
                hit_norm = collisonEvent.Normal;
                hit_this = collisonEvent.ThisShape;
                hit_other = collisonEvent.OtherShape;
            }

            //light crash
            if (collisonEvent.ThisShape.Name == "flr" || collisonEvent.ThisShape.Name == "fll"
                || collisonEvent.ThisShape.Name == "blr" || collisonEvent.ThisShape.Name == "bll")
            {
                if (collisonEvent.OtherShape.Name != "flr" || collisonEvent.OtherShape.Name != "fll"
                || collisonEvent.OtherShape.Name != "blr" || collisonEvent.OtherShape.Name != "bll")
                {
                    MapObjectAttachedMesh obj = GetFirstAttachedObjectByAlias(collisonEvent.ThisShape.Name)
                        as MapObjectAttachedMesh;
                    if (obj != null)
                    {
                        if (obj.TextUserData != "")
                        {
                            obj.TextUserData = (Convert.ToInt32(obj.TextUserData) + 1).ToString();
                        }
                        else
                        {
                            obj.TextUserData = "1";
                        }

                        if (Convert.ToInt32(obj.TextUserData) > 20)
                        {
                            Detach(obj);
                        }
                    }
                }
            }

            //glass crash
            if (collisonEvent.ThisShape.Name == "gforward" || collisonEvent.ThisShape.Name == "gbackward"
                || collisonEvent.ThisShape.Name == "gleft" || collisonEvent.ThisShape.Name == "gright")
            {
                if (collisonEvent.OtherShape.Name != "gforward" || collisonEvent.OtherShape.Name != "gbackward"
                || collisonEvent.OtherShape.Name != "gleft" || collisonEvent.OtherShape.Name != "gright")
                {
                    MapObjectAttachedMesh obj = GetFirstAttachedObjectByAlias(collisonEvent.ThisShape.Name)
                        as MapObjectAttachedMesh;
                    if (obj != null)
                    {
                        if (obj.TextUserData != "")
                        {
                            obj.TextUserData = (Convert.ToInt32(obj.TextUserData) + 1).ToString();
                        }
                        else
                        {
                            obj.TextUserData = "1";
                        }

                        if (Convert.ToInt32(obj.TextUserData) < 100)
                        {
                            obj.MeshObject.SetMaterialNameForAllSubObjects("glass_crash");
                        }
                        else
                        {
                            Detach(obj);
                        }
                    }
                }
            }
        }

        unsafe void DeformationMesh()
        {
            Vertex[] vertices = new Vertex[vertex_count];
            ushort[] indices = new ushort[index_count];
            mesh.SubMeshes[0].VertexData.GetSomeGeometry(ref changeable_vertex_pos, ref changeable_vertex_norm);

            if (first_init_mesh == false)
            {
                for (int i = 0; i < vertex_count; i++)
                {
                    Vertex vertex = new Vertex();
                    vertex.position = original_vertex_pos[i];
                    vertex.normal = original_vertex_norm[i];
                    vertex.texCoord = original_vertex_tc[i];
                    vertices[i] = vertex;
                }
                for (int i = 0; i < index_count; i++)
                {
                    indices[i] = (ushort)(original_vertex_ind[i]);
                }

                SubMesh sub_mesh = mesh.SubMeshes[0];
                {
                    HardwareVertexBuffer vertex_buffer = sub_mesh.VertexData.VertexBufferBinding.GetBuffer(0);

                    IntPtr buffer = vertex_buffer.Lock(HardwareBuffer.LockOptions.Discard);
                    fixed (Vertex* pvertices = vertices)
                    {
                        NativeUtils.CopyMemory(buffer, (IntPtr)pvertices, vertices.Length * sizeof(Vertex));
                    }
                    vertex_buffer.Unlock();
                }
                {
                    HardwareIndexBuffer index_buffer = sub_mesh.IndexData.IndexBuffer;
                    IntPtr buffer = index_buffer.Lock(HardwareBuffer.LockOptions.Discard);
                    fixed (ushort* pindices = indices)
                    {
                        NativeUtils.CopyMemory(buffer, (IntPtr)pindices, indices.Length * sizeof(ushort));
                    }
                    index_buffer.Unlock();
                }
                first_init_mesh = true;
            }

            else
            {
                if (hit_other != null)
                {
                    for (int i = 0; i < vertex_count; i++)
                    {
                        Vertex vertex = new Vertex();
                        Vec3 p = ((original_vertex_pos[i] * attached_mesh.ScaleOffset) * Rotation) +
                            (Position + attached_mesh.PositionOffset);
                        Vec3 pp = hit_pos;
                        Sphere sp = new Sphere(pp, Type.CONF.DeformationRadius);
                        Vec3 nvec = Vec3.Zero;
                        Vec3 nnorm = Vec3.Zero;

                        if (sp.IsContainsPoint(p))
                        {
                            Ray ray = new Ray(p, changeable_vertex_norm[i] * .01f);
                            if (!Single.IsNaN(ray.Direction.X) && !Single.IsNaN(ray.Origin.X))
                            {
                                RayCastResult[] piercingResult = PhysicsWorld.Instance.RayCastPiercing(
                                ray, (int)ContactGroup.CastOnlyDynamic);/*(int)ContactGroup.CastOnlyContact);*/

                                Vec3 collision_pos = Vec3.Zero;
                                Vec3 collision_nor = Vec3.Zero;
                                bool collision = false;

                                foreach (RayCastResult result in piercingResult)
                                {
                                    if (Array.IndexOf(PhysicsModel.Bodies, result.Shape.Body) != -1)
                                        continue;
                                    collision = true;
                                    collision_pos = result.Position;
                                    collision_nor = result.Normal;
                                    break;
                                }
                                if (collision)
                                {
                                    float old_x = 0, old_y = 0, old_z = 0, new_x = 0, new_y = 0, new_z = 0;
                                    float deformation = 0, force = 0, max_deformation = Type.CONF.MaxStrengthDeformation, mass = 0, vel = 0;

                                    mass = hit_other.Body.Mass;
                                    vel = hit_other.Body.LastStepLinearVelocity.Length();

                                    force = (((hit_this.Body.Mass * hit_this.Body.LastStepLinearVelocity.Length()) +
                                        (mass * vel)) / hit_this.Body.Mass) / 100.0f;

                                    if (force > max_deformation) deformation = max_deformation;
                                    else deformation = force;

                                    //Deform X                            
                                    if (changeable_vertex_pos[i].X > 0)
                                    {
                                        old_x = original_vertex_pos[i].X - deformation;
                                        if (old_x < changeable_vertex_pos[i].X) new_x = old_x;
                                        else new_x = changeable_vertex_pos[i].X;
                                    }
                                    else
                                    {
                                        old_x = original_vertex_pos[i].X + deformation;
                                        if (old_x > changeable_vertex_pos[i].X) new_x = old_x;
                                        else new_x = changeable_vertex_pos[i].X;
                                    }

                                    //Deform Y                           
                                    if (changeable_vertex_pos[i].Y > 0)
                                    {
                                        old_y = original_vertex_pos[i].Y - deformation;
                                        if (old_y < changeable_vertex_pos[i].Y) new_y = old_y;
                                        else new_y = changeable_vertex_pos[i].Y;
                                    }
                                    else
                                    {
                                        old_y = original_vertex_pos[i].Y + deformation;
                                        if (old_y > changeable_vertex_pos[i].Y) new_y = old_y;
                                        else new_y = changeable_vertex_pos[i].Y;
                                    }

                                    //Deform Z                            
                                    if (changeable_vertex_pos[i].Z > 0)
                                    {
                                        old_z = original_vertex_pos[i].Z - deformation;
                                        if (old_z < changeable_vertex_pos[i].Z) new_z = old_z;
                                        else new_z = changeable_vertex_pos[i].Z;
                                    }
                                    else
                                    {
                                        old_z = original_vertex_pos[i].Z + deformation;
                                        if (old_z > changeable_vertex_pos[i].Z) new_z = old_z;
                                        else new_z = changeable_vertex_pos[i].Z;
                                    }

                                    nvec = new Vec3(new_x, new_y, new_z);
                                    nnorm = -collision_nor;
                                }
                                else
                                {
                                    nvec = changeable_vertex_pos[i];
                                    nnorm = changeable_vertex_norm[i];
                                }
                            }
                        }
                        else
                        {
                            nvec = changeable_vertex_pos[i];
                            nnorm = changeable_vertex_norm[i];
                        }
                        vertex.position = nvec;
                        vertex.normal = nnorm;
                        vertex.texCoord = original_vertex_tc[i];
                        vertices[i] = vertex;
                    }

                    for (int i = 0; i < index_count; i++)
                        {
                            indices[i] = (ushort)(original_vertex_ind[i]);
                        }

                        SubMesh sub_mesh = mesh.SubMeshes[0];
                        {
                            HardwareVertexBuffer vertex_buffer = sub_mesh.VertexData.VertexBufferBinding.GetBuffer(0);

                            IntPtr buffer = vertex_buffer.Lock(HardwareBuffer.LockOptions.Discard);
                            fixed (Vertex* pvertices = vertices)
                            {
                                NativeUtils.CopyMemory(buffer, (IntPtr)pvertices, vertices.Length * sizeof(Vertex));
                            }
                            vertex_buffer.Unlock();
                        }
                        {
                            HardwareIndexBuffer index_buffer = sub_mesh.IndexData.IndexBuffer;
                            IntPtr buffer = index_buffer.Lock(HardwareBuffer.LockOptions.Discard);
                            fixed (ushort* pindices = indices)
                            {
                                NativeUtils.CopyMemory(buffer, (IntPtr)pindices, indices.Length * sizeof(ushort));
                            }
                            index_buffer.Unlock();
                        }
                }
                hit_other = null;
            }
        }

        unsafe void DeformationMeshParallel()
        {
            Vertex[] vertices = new Vertex[vertex_count];
            ushort[] indices = new ushort[index_count];
            mesh.SubMeshes[0].VertexData.GetSomeGeometry(ref changeable_vertex_pos, ref changeable_vertex_norm);

            if (first_init_mesh == false)
            {
                /*for (int i = 0; i < vertex_count; i++)
                {
                    Vertex vertex = new Vertex();
                    vertex.position = original_vertex_pos[i];
                    vertex.normal = original_vertex_norm[i];
                    vertex.texCoord = original_vertex_tc[i];
                    vertices[i] = vertex;
                }
                for (int i = 0; i < index_count; i++)
                {
                    indices[i] = (ushort)(original_vertex_ind[i]);
                }*/
                 
                ParallelLoopResult parallel_result = Parallel.For(0, vertex_count, i =>
                {
                    Vertex vertex = new Vertex();
                    vertex.position = original_vertex_pos[i];
                    vertex.normal = original_vertex_norm[i];
                    vertex.texCoord = original_vertex_tc[i];
                    vertices[i] = vertex;
                });

                if (parallel_result.IsCompleted)
                {
                    for (int i = 0; i < index_count; i++)
                    {
                        indices[i] = (ushort)(original_vertex_ind[i]);
                    }
                }

                SubMesh sub_mesh = mesh.SubMeshes[0];
                {
                    HardwareVertexBuffer vertex_buffer = sub_mesh.VertexData.VertexBufferBinding.GetBuffer(0);

                    IntPtr buffer = vertex_buffer.Lock(HardwareBuffer.LockOptions.Discard);
                    fixed (Vertex* pvertices = vertices)
                    {
                        NativeUtils.CopyMemory(buffer, (IntPtr)pvertices, vertices.Length * sizeof(Vertex));
                    }
                    vertex_buffer.Unlock();
                }
                {
                    HardwareIndexBuffer index_buffer = sub_mesh.IndexData.IndexBuffer;
                    IntPtr buffer = index_buffer.Lock(HardwareBuffer.LockOptions.Discard);
                    fixed (ushort* pindices = indices)
                    {
                        NativeUtils.CopyMemory(buffer, (IntPtr)pindices, indices.Length * sizeof(ushort));
                    }
                    index_buffer.Unlock();
                }
                first_init_mesh = true;
            }

            else
            {
                if (hit_other != null)
                {
                    ParallelLoopResult parallel_result = Parallel.For(0, vertex_count, i =>
                    {
                        Vertex vertex = new Vertex();
                        Vec3 p = ((original_vertex_pos[i] * attached_mesh.ScaleOffset) * Rotation) +
                            (Position + attached_mesh.PositionOffset);
                        Vec3 pp = hit_pos;
                        Sphere sp = new Sphere(pp, Type.CONF.DeformationRadius);
                        Vec3 nvec = Vec3.Zero;
                        Vec3 nnorm = Vec3.Zero;

                        if (sp.IsContainsPoint(p))
                        {
                            Ray ray = new Ray(p, changeable_vertex_norm[i] * .01f);
                            if (!Single.IsNaN(ray.Direction.X) && !Single.IsNaN(ray.Origin.X))
                            {
                                RayCastResult[] piercingResult = PhysicsWorld.Instance.RayCastPiercing(
                                ray, (int)ContactGroup.CastOnlyDynamic);/*(int)ContactGroup.CastOnlyContact);*/
                                
                                Vec3 collision_pos = Vec3.Zero;
                                Vec3 collision_nor = Vec3.Zero;
                                bool collision = false;

                                foreach (RayCastResult result in piercingResult)
                                {
                                    if (Array.IndexOf(PhysicsModel.Bodies, result.Shape.Body) != -1)
                                        continue;
                                    collision = true;
                                    collision_pos = result.Position;
                                    collision_nor = result.Normal;
                                    break;
                                }
                                if (collision)
                                {
                                    float old_x = 0, old_y = 0, old_z = 0, new_x = 0, new_y = 0, new_z = 0;
                                    float deformation = 0, force = 0, max_deformation = Type.CONF.MaxStrengthDeformation, mass = 0, vel = 0;

                                    mass = hit_other.Body.Mass;
                                    vel = hit_other.Body.LastStepLinearVelocity.Length();

                                    force = (((hit_this.Body.Mass * hit_this.Body.LastStepLinearVelocity.Length()) +
                                        (mass * vel)) / hit_this.Body.Mass) / 100.0f;
                                    
                                    if (force > max_deformation) deformation = max_deformation;
                                    else deformation = force;

                                    //Deform X                            
                                    if (changeable_vertex_pos[i].X > 0)
                                    {
                                        old_x = original_vertex_pos[i].X - deformation;
                                        if (old_x < changeable_vertex_pos[i].X) new_x = old_x;
                                        else new_x = changeable_vertex_pos[i].X;
                                    }
                                    else
                                    {
                                        old_x = original_vertex_pos[i].X + deformation;
                                        if (old_x > changeable_vertex_pos[i].X) new_x = old_x;
                                        else new_x = changeable_vertex_pos[i].X;
                                    }

                                    //Deform Y                           
                                    if (changeable_vertex_pos[i].Y > 0)
                                    {
                                        old_y = original_vertex_pos[i].Y - deformation;
                                        if (old_y < changeable_vertex_pos[i].Y) new_y = old_y;
                                        else new_y = changeable_vertex_pos[i].Y;
                                    }
                                    else
                                    {
                                        old_y = original_vertex_pos[i].Y + deformation;
                                        if (old_y > changeable_vertex_pos[i].Y) new_y = old_y;
                                        else new_y = changeable_vertex_pos[i].Y;
                                    }

                                    //Deform Z                            
                                    if (changeable_vertex_pos[i].Z > 0)
                                    {
                                        old_z = original_vertex_pos[i].Z - deformation;
                                        if (old_z < changeable_vertex_pos[i].Z) new_z = old_z;
                                        else new_z = changeable_vertex_pos[i].Z;
                                    }
                                    else
                                    {
                                        old_z = original_vertex_pos[i].Z + deformation;
                                        if (old_z > changeable_vertex_pos[i].Z) new_z = old_z;
                                        else new_z = changeable_vertex_pos[i].Z;
                                    }

                                    nvec = new Vec3(new_x, new_y, new_z);
                                    nnorm = -collision_nor;
                                }
                                else
                                {
                                    nvec = changeable_vertex_pos[i];
                                    nnorm = changeable_vertex_norm[i];
                                }
                            }
                        }
                        else
                        {
                            nvec = changeable_vertex_pos[i];
                            nnorm = changeable_vertex_norm[i];
                        }
                        vertex.position = nvec;
                        vertex.normal = nnorm;
                        vertex.texCoord = original_vertex_tc[i];
                        vertices[i] = vertex;
                    });

                    if (parallel_result.IsCompleted)
                    {
                        for (int i = 0; i < index_count; i++)
                        {
                            indices[i] = (ushort)(original_vertex_ind[i]);
                        }
                        
                        SubMesh sub_mesh = mesh.SubMeshes[0];
                        {
                            HardwareVertexBuffer vertex_buffer = sub_mesh.VertexData.VertexBufferBinding.GetBuffer(0);

                            IntPtr buffer = vertex_buffer.Lock(HardwareBuffer.LockOptions.Discard);
                            fixed (Vertex* pvertices = vertices)
                            {
                                NativeUtils.CopyMemory(buffer, (IntPtr)pvertices, vertices.Length * sizeof(Vertex));
                            }
                            vertex_buffer.Unlock();
                        }
                        {
                            HardwareIndexBuffer index_buffer = sub_mesh.IndexData.IndexBuffer;
                            IntPtr buffer = index_buffer.Lock(HardwareBuffer.LockOptions.Discard);
                            fixed (ushort* pindices = indices)
                            {
                                NativeUtils.CopyMemory(buffer, (IntPtr)pindices, indices.Length * sizeof(ushort));
                            }
                            index_buffer.Unlock();
                        }
                    }
                }
                hit_other = null;
            }
        }
    }
}
