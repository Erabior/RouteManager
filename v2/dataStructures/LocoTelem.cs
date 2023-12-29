using Model;
using RollingStock;
using System.Collections.Generic;


namespace RouteManager.v2.dataStructures
{
    public class LocoTelem
    {

        public static Dictionary<Car, bool> locomotiveCoroutines { get; private set; } = new Dictionary<Car, bool>();
        public static Dictionary<Car, Dictionary<string, bool>> UIStationSelections = new Dictionary<Car, Dictionary<string, bool>>();
        public static Dictionary<Car, string> LocomotiveDestination { get; private set; } = new Dictionary<Car, string>();
        public static Dictionary<Car, string> LocomotivePrevDestination { get; private set; } = new Dictionary<Car, string>();
        public static Dictionary<Car, List<PassengerStop>> SelectedStations { get; private set; } = new Dictionary<Car, List<PassengerStop>>();
        public static Dictionary<Car, float> RMMaxSpeed { get; private set; } = new Dictionary<Car, float>();
        public static Dictionary<Car, bool> RouteMode { get; private set; } = new Dictionary<Car, bool>();
        public static Dictionary<Car, bool> TransitMode { get; private set; } = new Dictionary<Car, bool>();
        public static Dictionary<Car, bool> LineDirectionEastWest { get; private set; } = new Dictionary<Car, bool>();
        public static Dictionary<Car, bool> DriveForward { get; private set; } = new Dictionary<Car, bool>();
        public static Dictionary<Car, Car> CenterCar { get; private set; } = new Dictionary<Car, Car>();

        public static Dictionary<Car, bool> approachWhistleSounded { get; private set; } = new Dictionary<Car, bool>();
    }
}
