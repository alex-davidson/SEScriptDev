using System;
using System.Linq;
using Sandbox.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame;

namespace IngameScript
{
    public class PistonStack
    {
        private readonly IMyPistonBase[] pistons;

        public PistonStack(IMyPistonBase[] pistons)
        {
            if (!pistons.Any()) throw new ArgumentException("Empty piston stack.");
            this.pistons = pistons;
            for (var i = 1; i < pistons.Length; i++)
            {
                if (pistons[i - 1].TopGrid != pistons[i].CubeGrid)
                {
                    throw new ArgumentException("Pistons in the stack should be in order, from base to top.");
                }
            }
            UpdateState();
        }

        public IMyCubeGrid TopGrid => pistons[pistons.Length - 1].TopGrid;
        public IMyCubeGrid BaseGrid => pistons[0].CubeGrid;

        public int Total => pistons.Length;
        public int Operable { get; private set; }

        public float MaxExtension { get; private set; }
        public float MinExtension { get; private set; }
        public float Extension { get; private set; }
        public float ExtensionPercentage => (Extension - MinExtension) / (MaxExtension - MinExtension);
        public float Velocity { get; private set; }

        public bool IsExtending => Velocity > 0.001f;
        public bool IsRetracting => Velocity < -0.001f;

        public void UpdateState()
        {
            BeginUpdate();
            foreach (var piston in pistons)
            {
                UpdateState(piston);
            }
        }

        public void Extend(float targetVelocity)
        {
            BeginUpdate();
            var perPistonVelocity = targetVelocity / pistons.Length;
            foreach (var piston in pistons)
            {
                piston.Velocity = perPistonVelocity;
                UpdateState(piston);
            }
        }

        public void Retract(float targetVelocity)
        {
            BeginUpdate();
            var perPistonVelocity = targetVelocity / pistons.Length;
            foreach (var piston in pistons)
            {
                piston.Velocity = -perPistonVelocity;
                UpdateState(piston);
            }
        }

        public void Stop()
        {
            BeginUpdate();
            foreach (var piston in pistons)
            {
                piston.Velocity = 0;
                UpdateState(piston);
            }
        }

        private void BeginUpdate()
        {
            Operable = 0;
            MaxExtension = 0;
            MinExtension = 0;
            Extension = 0;
            Velocity = 0;
        }

        private void UpdateState(IMyPistonBase piston)
        {
            MaxExtension += piston.MaxLimit;
            MinExtension += piston.MinLimit;
            Extension += piston.CurrentPosition;
            if (!piston.IsOperational()) return;
            Operable++;
            Velocity += piston.Velocity;
        }
    }
}
