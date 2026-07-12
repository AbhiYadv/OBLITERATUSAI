using UnityEngine;

namespace ObliteratusAI.Core
{
    /// <summary>
    /// Named physics layers used by the city-life simulation. The layers are
    /// added to TagManager by the editor bootstrap; runtime code degrades to
    /// the Default layer when they are missing.
    /// </summary>
    public static class SimulationLayers
    {
        public const string TrafficVehicle = "TrafficVehicle";
        public const string Pedestrian = "Pedestrian";

        /// <summary>Layer index for traffic vehicles, or 0 (Default) when unset.</summary>
        public static int TrafficVehicleLayer()
        {
            int layer = LayerMask.NameToLayer(TrafficVehicle);
            return layer >= 0 ? layer : 0;
        }

        /// <summary>Layer index for pedestrians, or 0 (Default) when unset.</summary>
        public static int PedestrianLayer()
        {
            int layer = LayerMask.NameToLayer(Pedestrian);
            return layer >= 0 ? layer : 0;
        }

        /// <summary>
        /// Mask for forward obstacle sensing: Default (player, player car,
        /// props, buildings) plus the simulation layers when they exist.
        /// </summary>
        public static int SenseMask()
        {
            int mask = 1 << 0;
            int traffic = LayerMask.NameToLayer(TrafficVehicle);
            if (traffic >= 0)
            {
                mask |= 1 << traffic;
            }
            int pedestrian = LayerMask.NameToLayer(Pedestrian);
            if (pedestrian >= 0)
            {
                mask |= 1 << pedestrian;
            }
            return mask;
        }
    }
}
