using Model;
using Model.Definition;
using Model.OpsNew;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Logger = RouteManager.v2.Logging.Logger;

namespace RouteManager.v2.core
{
    internal class TrainManager
    {


        //Get Fuel Load information for the Requested locomotive 
        public static float GetLoadInfoForLoco(Car car, String loadIdent)
        {
            int slotIndex;
            //Check for diesel first as its cheaper computationally
            if (loadIdent == "diesel-fuel")
            {
                CarLoadInfo? loadInfo = car.GetLoadInfo(loadIdent, out slotIndex);

                if (loadInfo.HasValue)
                {

                    return loadInfo.Value.Quantity;
                }
                else
                {
                    //Debugging
                    Logger.LogToDebug($"{car.DisplayName} No Diesel load information found for {loadIdent}.");
                }
            }
            //Only enumerate and iterate through the cars in the train if/when we need to. 
            else
            {
                var cars = car.EnumerateCoupled().ToList();
                foreach (var trainCar in cars)
                {
                    if (trainCar.Archetype == CarArchetype.Tender)
                    {
                        Car Tender = trainCar;
                        CarLoadInfo? loadInfo = Tender.GetLoadInfo(loadIdent, out slotIndex);

                        if (loadInfo.HasValue)
                        {
                            return loadInfo.Value.Quantity;
                        }
                        else
                        {
                            //Debugging
                            Logger.LogToDebug($"{car.DisplayName} No Steam load information found for {loadIdent}.");
                        }
                    }
                }
            }

            //Something went wrong so assume 0 fuel
            return 0f;
        }


    }
}
