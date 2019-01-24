// Copyright (C) Sergey Grigorev
// Web site: http://ssdc.getdev.tk
// This addon SSDC, the deformable car for NeoAxis Engine.
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.ComponentModel;
using Engine;
using Engine.Utils;
using Engine.MathEx;
using Engine.EntitySystem;
using Engine.MapSystem;
using Engine.PhysicsSystem;
using Engine.Renderer;
using ProjectCommon;

namespace ProjectEntities
{
    /// <summary>
    /// Defines the <see cref="SSDCAI"/> entity type.
    /// </summary>

    public class SSDCAIType : AIType
    {
    }

    public class SSDCAI : AI
    {
        [FieldSerialize]
        MapCurve task_way;
        [FieldSerialize]
        MapCurvePoint task_current_waypoint;
        [FieldSerialize]
        bool task_enabled;
        [FieldSerialize]
        Vec3 task_position;

        SSDCAIType _type = null; public new SSDCAIType Type { get { return _type; } }

        /// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnPostCreate(Boolean)"/>.</summary>
        protected override void OnPostCreate(bool loaded)
        {
            base.OnPostCreate(loaded);
            SubscribeToTickEvent();

            MapCurve way = null;
            {
                EntityComponent_SSDC component = (EntityComponent_SSDC)
                    ControlledObject.Component_GetFirstWithType(typeof(EntityComponent_SSDC));
                if (component != null)
                    way = component.Way;
            }

            if (way != null)
                Task(way);
        }

        /// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnTick()"/>.</summary>
        protected override void OnTick()
        {
            base.OnTick();
            UpdateTask();
            UpdateTaskControlKeys();
        }

        void Task(MapCurve way)
        {
            task_way = way;
            task_current_waypoint = null;

            if (task_way != null)
                task_current_waypoint = task_way;

            ResetTask();
        }

        void UpdateTask()
        {
            const float waypoint_check_distance = 20;
            float waypoint_distance = (ControlledObject.Position -
                task_current_waypoint.Position).Length();

            if (waypoint_distance <= waypoint_check_distance)
            {
                int index = task_way.Points.IndexOf(task_current_waypoint);
                index++;

                if (index < task_way.Points.Count)
                {
                    task_current_waypoint = task_way.Points[index];
                }
                else
                {
                    task_current_waypoint = task_way.Points[0];
                }
            }
            DoTask(task_current_waypoint.Position);
        }

        void DoTask(Vec3 pos)
        {
            task_enabled = true;
            task_position = pos;
        }

        void ResetTask()
        {
            task_enabled = false;
        }

        void UpdateTaskControlKeys()
        {
            if (task_enabled)
            {
                Vec3 unit_pos = ControlledObject.Position;
                Vec3 unit_dir = ControlledObject.Rotation.GetForward();
                Vec3 need_dir = task_position - unit_pos;
                Angles angle =
                    new Angles(ControlledObject.Rotation.ToAngles().Roll, ControlledObject.Rotation.ToAngles().Pitch,
                        Quat.FromDirectionZAxisUp(need_dir).ToAngles().Yaw);
                ControlledObject.Rotation = Quat.Slerp(ControlledObject.Rotation, angle.ToQuat(), 6.0f * TickDelta);

                ControlKeyPress(GameControlKeys.Forward, 1);

                if (ControlledObject.Rotation.GetUp().Z < .4f)
                    ControlKeyPress(GameControlKeys.Reload, 1);
                else
                    ControlKeyRelease(GameControlKeys.Reload);
            }
            else
                ControlKeyRelease(GameControlKeys.Forward);
        }

        public override bool IsActive()
        {
            return task_enabled;
        }
    }
}