using Model;
using RollingStock;
using System.Collections.Generic;


namespace RouteManager.v2.dataStructures
{
    public class LocoTelem
    {

        

        public static Dictionary<Car, bool>     locomotiveCoroutines            { get; private set; } = new Dictionary<Car, bool>();
        public static Dictionary<Car, bool>     RouteMode                       { get; private set; } = new Dictionary<Car, bool>();
        public static Dictionary<Car, bool>     TransitMode                     { get; private set; } = new Dictionary<Car, bool>();
        public static Dictionary<Car, Car>      CenterCar                       { get; private set; } = new Dictionary<Car, Car>();
        public static Dictionary<Car, float>    RMMaxSpeed                      { get; private set; } = new Dictionary<Car, float>();
        public static Dictionary<Car, bool>     initialSpeedSliderSet           { get; private set; } = new Dictionary<Car, bool>();
        public static Dictionary<Car, bool>     approachWhistleSounded          { get; private set; } = new Dictionary<Car, bool>();
        public static Dictionary<Car, bool>     clearedForDeparture             { get; private set; } = new Dictionary<Car, bool>();
        public static Dictionary<Car, bool>     locoTravelingEastWard           { get; private set; } = new Dictionary<Car, bool>();
        public static Dictionary<Car, bool>     needToUpdatePassengerCoaches    { get; private set; } = new Dictionary<Car, bool>();
        public static Dictionary<Car, bool>     closestStationNeedsUpdated      { get; private set; } = new Dictionary<Car, bool>();


        public static Dictionary<Car, (PassengerStop, float)>    closestStation          { get; private set; } = new Dictionary<Car, (PassengerStop, float)>();
        public static Dictionary<Car, PassengerStop>             currentDestination      { get; private set; } = new Dictionary<Car, PassengerStop>();
        public static Dictionary<Car, List<PassengerStop>>       previousDestinations     { get; private set; } = new Dictionary<Car, List<PassengerStop>>();
        public static Dictionary<Car, Dictionary<string, float>> lowFuelQuantities       { get; private set; } = new Dictionary<Car, Dictionary<string, float>>();
        public static Dictionary<Car, Dictionary<string, bool>>  UIStationSelections     { get; private set; } = new Dictionary<Car, Dictionary<string, bool>>();
        public static Dictionary<Car, List<PassengerStop>>       SelectedStations        { get; private set; } = new Dictionary<Car, List<PassengerStop>>();
    }
}
